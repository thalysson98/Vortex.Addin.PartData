using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Threading.Tasks;

namespace Vortex.Addin.PartData.Migration
{
    public class MigrationService
    {
        private readonly string _srcConnStr;
        private readonly string _dstConnStr;
        private readonly Action<string> _log;

        public MigrationService(Action<string> log)
        {
            _log = log;

            var b = new SqlConnectionStringBuilder
            {
                DataSource = "192.168.2.248\\ERASQL",
                UserID = "pdb_admin",
                Password = "eng.2003"
            };

            b.InitialCatalog = "PDB_BANCO";
            _srcConnStr = b.ConnectionString;

            b.InitialCatalog = "PDB_BANCO_V2";
            _dstConnStr = b.ConnectionString;
        }

        private SqlConnection Src() => new SqlConnection(_srcConnStr);
        private SqlConnection Dst() => new SqlConnection(_dstConnStr);

        // ── Criação do banco ─────────────────────────────────────────────────────

        public async Task CreateDatabaseAsync()
        {
            var masterStr = new SqlConnectionStringBuilder(_srcConnStr) { InitialCatalog = "master" }.ConnectionString;

            _log("Conectando ao servidor...");
            using (var conn = new SqlConnection(masterStr))
            {
                await conn.OpenAsync();

                var check = new SqlCommand("SELECT COUNT(1) FROM sys.databases WHERE name = 'PDB_BANCO_V2'", conn);
                bool exists = (int)await check.ExecuteScalarAsync() > 0;

                if (exists)
                {
                    _log("PDB_BANCO_V2 já existe — recriando do zero...");
                    await Exec(conn, "ALTER DATABASE PDB_BANCO_V2 SET SINGLE_USER WITH ROLLBACK IMMEDIATE");
                    await Exec(conn, "DROP DATABASE PDB_BANCO_V2");
                }

                await Exec(conn, "CREATE DATABASE PDB_BANCO_V2");
                _log("Banco PDB_BANCO_V2 criado.");
            }
        }

        public async Task CreateTablesAsync()
        {
            _log("Criando tabelas...");
            using (var conn = Dst())
            {
                await conn.OpenAsync();
                foreach (var sql in TableScripts())
                    await Exec(conn, sql);
            }
            _log("Tabelas criadas.");
        }

        // ── Migração por tabela ──────────────────────────────────────────────────

        public async Task<int> MigrateTiposAsync()
        {
            _log("Migrando TIPOS...");

            var dt = await LoadAsync(_srcConnStr,
                "SELECT TIPO, LTRIM(RTRIM(M1)) M1, LTRIM(RTRIM(M2)) M2, LTRIM(RTRIM(M3)) M3, LTRIM(RTRIM(M4)) M4 FROM TIPOS ORDER BY TIPO");

            using (var conn = Dst())
            {
                await conn.OpenAsync();
                // Preserva os mesmos IDs para manter compatibilidade com CATEGORIAS.TIPO_ID
                await Exec(conn, "SET IDENTITY_INSERT TIPOS ON");

                foreach (DataRow r in dt.Rows)
                {
                    string m4 = r["M4"].ToString().Trim();
                    var cmd = new SqlCommand(
                        "INSERT INTO TIPOS (Id,M1,M2,M3,M4) VALUES (@id,@m1,@m2,@m3,@m4)", conn);
                    cmd.Parameters.AddWithValue("@id", r["TIPO"]);
                    cmd.Parameters.AddWithValue("@m1", r["M1"].ToString().Trim());
                    cmd.Parameters.AddWithValue("@m2", r["M2"].ToString().Trim());
                    cmd.Parameters.AddWithValue("@m3", r["M3"].ToString().Trim());
                    cmd.Parameters.AddWithValue("@m4", (m4 == "-" || m4 == "") ? (object)DBNull.Value : m4);
                    await cmd.ExecuteNonQueryAsync();
                }

                await Exec(conn, "SET IDENTITY_INSERT TIPOS OFF");
            }

            _log($"  → {dt.Rows.Count} tipos migrados.");
            return dt.Rows.Count;
        }

        public async Task<int> MigrateCategoriasAsync()
        {
            _log("Migrando CATEGORIAS...");

            var dt = await LoadAsync(_srcConnStr,
                "SELECT LTRIM(RTRIM(MATERIAL)) NOME, TIPO FROM CATEGORIAS");

            using (var conn = Dst())
            {
                await conn.OpenAsync();
                foreach (DataRow r in dt.Rows)
                {
                    var cmd = new SqlCommand(
                        "INSERT INTO CATEGORIAS (NOME, TIPO_ID) VALUES (@nome, @tipo)", conn);
                    cmd.Parameters.AddWithValue("@nome", r["NOME"].ToString());
                    cmd.Parameters.AddWithValue("@tipo", r["TIPO"]);
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            _log($"  → {dt.Rows.Count} categorias migradas.");
            return dt.Rows.Count;
        }

        public async Task<int> MigrateUsersAsync()
        {
            _log("Migrando USERS...");

            var dt = await LoadAsync(_srcConnStr,
                "SELECT LTRIM(RTRIM(IDPDM)) IDPDM, LTRIM(RTRIM(PERMISSAO)) PERMISSAO FROM USERS");

            using (var conn = Dst())
            {
                await conn.OpenAsync();
                foreach (DataRow r in dt.Rows)
                {
                    string perm = r["PERMISSAO"].ToString().ToLower();
                    if (perm != "admin" && perm != "user") perm = "user";

                    var cmd = new SqlCommand(
                        "INSERT INTO USERS (IDPDM, PERMISSAO) VALUES (@idpdm, @perm)", conn);
                    cmd.Parameters.AddWithValue("@idpdm", r["IDPDM"].ToString());
                    cmd.Parameters.AddWithValue("@perm", perm);
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            _log($"  → {dt.Rows.Count} usuários migrados.");
            return dt.Rows.Count;
        }

        public async Task<int> MigrateMedidasAsync()
        {
            _log("Migrando MEDIDAS...");

            var dt = await LoadAsync(_srcConnStr,
                "SELECT LTRIM(RTRIM(CATEGORIA)) CAT, LTRIM(RTRIM(M1)) M1, LTRIM(RTRIM(M2)) M2, LTRIM(RTRIM(M3)) M3, LTRIM(RTRIM(M4)) M4 FROM MEDIDAS");

            int ok = 0, skip = 0;
            using (var conn = Dst())
            {
                await conn.OpenAsync();
                foreach (DataRow r in dt.Rows)
                {
                    int? catId = await LookupCatAsync(conn, r["CAT"].ToString());
                    if (catId == null)
                    {
                        _log($"  ⚠ Medida: categoria '{r["CAT"]}' não encontrada, pulando.");
                        skip++;
                        continue;
                    }

                    var cmd = new SqlCommand(
                        "INSERT INTO MEDIDAS (CATEGORIA_ID,M1,M2,M3,M4) VALUES (@cid,@m1,@m2,@m3,@m4)", conn);
                    cmd.Parameters.AddWithValue("@cid", catId.Value);
                    cmd.Parameters.AddWithValue("@m1", NullOrStr(r["M1"]));
                    cmd.Parameters.AddWithValue("@m2", NullOrStr(r["M2"]));
                    cmd.Parameters.AddWithValue("@m3", NullOrStr(r["M3"]));
                    cmd.Parameters.AddWithValue("@m4", NullOrStr(r["M4"]));
                    await cmd.ExecuteNonQueryAsync();
                    ok++;
                }
            }

            if (skip > 0) _log($"  ⚠ {skip} medidas puladas por categoria inexistente.");
            _log($"  → {ok} medidas migradas.");
            return ok;
        }

        public async Task<(int migrated, int skipped)> MigrateMateriaisAsync(
            IProgress<(int current, int total)> progress = null)
        {
            _log("Carregando MATERIAIS da origem...");

            var dt = await LoadAsync(_srcConnStr, @"
                SELECT LTRIM(RTRIM(CATEGORIA)) CATEGORIA,
                       DIAMETRO, ESPESSURA, COMPRIMENTO, M4,
                       COD1, COD2, COD3,
                       LTRIM(RTRIM(CAD_POR)) CAD_POR,
                       DATE_PROJ
                FROM MATERIAIS");

            int total = dt.Rows.Count;
            int ok = 0, skip = 0;
            _log($"  {total} registros encontrados.");

            // Caches para evitar round-trips repetidos
            var catCache  = new Dictionary<string, int?>(StringComparer.OrdinalIgnoreCase);
            var userCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            using (var conn = Dst())
            {
                await conn.OpenAsync();

                foreach (DataRow r in dt.Rows)
                {
                    progress?.Report((ok + skip, total));

                    string cod = $"{r["COD1"]}.{r["COD2"]}.{r["COD3"]}";
                    try
                    {
                        string cat    = r["CATEGORIA"].ToString();
                        string cadPor = r["CAD_POR"].ToString();

                        // Categoria ID
                        if (!catCache.ContainsKey(cat))
                            catCache[cat] = await LookupCatAsync(conn, cat);

                        if (catCache[cat] == null)
                        {
                            _log($"  ⚠ {cod}: categoria '{cat}' não encontrada, pulando.");
                            skip++; continue;
                        }

                        // User ID — cria automaticamente se não existir
                        if (!userCache.ContainsKey(cadPor))
                        {
                            int? uid = await LookupUserAsync(conn, cadPor);
                            if (uid == null)
                            {
                                var ins = new SqlCommand(
                                    "INSERT INTO USERS (IDPDM,PERMISSAO) OUTPUT INSERTED.Id VALUES (@u,'user')", conn);
                                ins.Parameters.AddWithValue("@u", cadPor);
                                uid = (int)await ins.ExecuteScalarAsync();
                                _log($"  ℹ Usuário '{cadPor}' criado automaticamente.");
                            }
                            userCache[cadPor] = uid.Value;
                        }

                        decimal  m1   = ToDecimal(r["DIAMETRO"].ToString());
                        decimal  m2   = ToDecimal(r["ESPESSURA"].ToString());
                        decimal  m3   = ToDecimal(r["COMPRIMENTO"].ToString());
                        decimal? m4   = ToDecimalNullable(r["M4"].ToString());
                        DateTime date = ToDate(r["DATE_PROJ"].ToString());

                        var cmd = new SqlCommand(@"
                            INSERT INTO MATERIAIS
                                (CATEGORIA_ID, M1, M2, M3, M4, COD1, COD2, COD3, CAD_POR, DATE_PROJ)
                            VALUES
                                (@cid, @m1, @m2, @m3, @m4, @c1, @c2, @c3, @cad, @dt)", conn);

                        cmd.Parameters.AddWithValue("@cid", catCache[cat].Value);
                        cmd.Parameters.AddWithValue("@m1",  m1);
                        cmd.Parameters.AddWithValue("@m2",  m2);
                        cmd.Parameters.AddWithValue("@m3",  m3);
                        cmd.Parameters.Add(new SqlParameter("@m4", SqlDbType.Decimal)
                            { Value = (object)m4 ?? DBNull.Value, Precision = 10, Scale = 3 });
                        cmd.Parameters.AddWithValue("@c1",  r["COD1"].ToString().Trim());
                        cmd.Parameters.AddWithValue("@c2",  r["COD2"].ToString().Trim());
                        cmd.Parameters.AddWithValue("@c3",  r["COD3"].ToString().Trim());
                        cmd.Parameters.AddWithValue("@cad", userCache[cadPor]);
                        cmd.Parameters.AddWithValue("@dt",  date);
                        await cmd.ExecuteNonQueryAsync();
                        ok++;
                    }
                    catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
                    {
                        // Chave duplicada — não deveria acontecer com a query deduplicada,
                        // mas registra caso haja outro motivo de unicidade.
                        _log($"  ⚠ {cod}: duplicata ignorada.");
                        skip++;
                    }
                    catch (Exception ex)
                    {
                        _log($"  ✗ {cod}: {ex.GetType().Name} — {ex.Message}");
                        skip++;
                    }
                }
            }

            progress?.Report((total, total));
            _log($"  → {ok} materiais migrados, {skip} pulados.");
            return (ok, skip);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static async Task<DataTable> LoadAsync(string connStr, string sql)
        {
            using (var conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                using (var adapter = new SqlDataAdapter(sql, conn))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    return dt;
                }
            }
        }

        private static Task Exec(SqlConnection conn, string sql) =>
            new SqlCommand(sql, conn).ExecuteNonQueryAsync();

        private static async Task<int?> LookupCatAsync(SqlConnection conn, string nome)
        {
            var cmd = new SqlCommand("SELECT Id FROM CATEGORIAS WHERE NOME = @n", conn);
            cmd.Parameters.AddWithValue("@n", nome);
            object r = await cmd.ExecuteScalarAsync();
            return (r == null || r == DBNull.Value) ? (int?)null : (int)r;
        }

        private static async Task<int?> LookupUserAsync(SqlConnection conn, string idpdm)
        {
            var cmd = new SqlCommand("SELECT Id FROM USERS WHERE IDPDM = @u", conn);
            cmd.Parameters.AddWithValue("@u", idpdm);
            object r = await cmd.ExecuteScalarAsync();
            return (r == null || r == DBNull.Value) ? (int?)null : (int)r;
        }

        private static object NullOrStr(object val)
        {
            string s = val?.ToString().Trim();
            return string.IsNullOrEmpty(s) ? (object)DBNull.Value : s;
        }

        private static decimal ToDecimal(string v)
        {
            if (string.IsNullOrWhiteSpace(v)) return 0m;
            return decimal.TryParse(v.Replace(",", "."), NumberStyles.Any,
                CultureInfo.InvariantCulture, out decimal r) ? r : 0m;
        }

        private static decimal? ToDecimalNullable(string v)
        {
            string s = v?.Trim();
            if (string.IsNullOrEmpty(s)) return null;
            if (!decimal.TryParse(s.Replace(",", "."), NumberStyles.Any,
                CultureInfo.InvariantCulture, out decimal r)) return null;
            return r == 0m ? (decimal?)null : r; // "0" era sentinel no schema antigo
        }

        private static DateTime ToDate(string v)
        {
            if (DateTime.TryParseExact(v, "dd/MM/yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d)) return d;
            if (DateTime.TryParse(v, out d)) return d;
            return DateTime.Today;
        }

        // ── DDL ──────────────────────────────────────────────────────────────────

        private static string[] TableScripts() => new[]
        {
            @"CREATE TABLE TIPOS (
                Id  INT           IDENTITY(1,1) PRIMARY KEY,
                M1  NVARCHAR(50)  NOT NULL,
                M2  NVARCHAR(50)  NOT NULL,
                M3  NVARCHAR(50)  NOT NULL,
                M4  NVARCHAR(50)  NULL
            )",
            @"CREATE TABLE CATEGORIAS (
                Id      INT           IDENTITY(1,1) PRIMARY KEY,
                NOME    NVARCHAR(100) NOT NULL,
                TIPO_ID INT           NOT NULL,
                CONSTRAINT UQ_CATEGORIAS_NOME  UNIQUE (NOME),
                CONSTRAINT FK_CATEGORIAS_TIPOS FOREIGN KEY (TIPO_ID) REFERENCES TIPOS(Id)
            )",
            @"CREATE TABLE USERS (
                Id        INT           IDENTITY(1,1) PRIMARY KEY,
                IDPDM     NVARCHAR(100) NOT NULL,
                PERMISSAO NVARCHAR(10)  NOT NULL DEFAULT 'user',
                CONSTRAINT UQ_USERS_IDPDM     UNIQUE (IDPDM),
                CONSTRAINT CK_USERS_PERMISSAO CHECK (PERMISSAO IN ('admin', 'user'))
            )",
            @"CREATE TABLE MEDIDAS (
                Id           INT           IDENTITY(1,1) PRIMARY KEY,
                CATEGORIA_ID INT           NOT NULL,
                M1           NVARCHAR(100) NULL,
                M2           NVARCHAR(100) NULL,
                M3           NVARCHAR(100) NULL,
                M4           NVARCHAR(100) NULL,
                CONSTRAINT UQ_MEDIDAS_CATEGORIA  UNIQUE (CATEGORIA_ID),
                CONSTRAINT FK_MEDIDAS_CATEGORIAS FOREIGN KEY (CATEGORIA_ID) REFERENCES CATEGORIAS(Id)
            )",
            @"CREATE TABLE MATERIAIS (
                Id           INT           IDENTITY(1,1) PRIMARY KEY,
                CATEGORIA_ID INT           NOT NULL,
                M1           DECIMAL(10,3) NOT NULL,
                M2           DECIMAL(10,3) NOT NULL,
                M3           DECIMAL(10,3) NOT NULL,
                M4           DECIMAL(10,3) NULL,
                COD1         CHAR(3)       NOT NULL,
                COD2         CHAR(3)       NOT NULL,
                COD3         CHAR(4)       NOT NULL,
                CAD_POR      INT           NOT NULL,
                DATE_PROJ    DATE          NOT NULL DEFAULT CAST(GETDATE() AS DATE),
                CONSTRAINT UQ_MATERIAIS_CODIGO     UNIQUE (COD1, COD2, COD3),
                CONSTRAINT FK_MATERIAIS_CATEGORIAS FOREIGN KEY (CATEGORIA_ID) REFERENCES CATEGORIAS(Id),
                CONSTRAINT FK_MATERIAIS_USERS      FOREIGN KEY (CAD_POR)      REFERENCES USERS(Id)
            )",
            "CREATE INDEX IX_MATERIAIS_CATEGORIA ON MATERIAIS(CATEGORIA_ID)",
            "CREATE INDEX IX_MATERIAIS_CODIGO    ON MATERIAIS(COD1, COD2, COD3)"
        };
    }
}
