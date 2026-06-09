using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Vortex.Addin.PartData.Core
{
    public class SQLCommands
    {
        private Dictionary<string, DataTable> dadosEmMemoria = new Dictionary<string, DataTable>();

        public SQLCommands() { }

        internal SQLCommands(Dictionary<string, DataTable> dadosTeste)
        {
            dadosEmMemoria = dadosTeste;
        }

        private SqlConnection Connect()
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource     = "192.168.2.248\\ERASQL",
                UserID         = "pdb_user",
                Password       = "eng.2003",
                InitialCatalog = "PDB_BANCO_V2"
            };
            return new SqlConnection(builder.ConnectionString);
        }

        // ── Queries do cache ─────────────────────────────────────────────────────
        // Aliases preservam os nomes antigos de coluna para que os formulários
        // não precisem ser alterados.

        private static readonly Dictionary<string, string> _cacheQueries =
            new Dictionary<string, string>
        {
            ["TIPOS"] = @"
                SELECT Id AS TIPO,
                       M1, M2, M3,
                       ISNULL(M4, '-') AS M4
                FROM TIPOS",

            ["CATEGORIAS"] = @"
                SELECT Id,
                       NOME    AS MATERIAL,
                       TIPO_ID AS TIPO
                FROM CATEGORIAS",

            ["USERS"] = @"
                SELECT u.Id, u.IDPDM, p.NOME AS PERMISSAO
                FROM USERS u JOIN PERMISSOES p ON u.PERMISSAO_ID = p.Id",

            ["MEDIDAS"] = @"
                SELECT d.Id,
                       c.NOME AS CATEGORIA,
                       d.M1, d.M2, d.M3, d.M4
                FROM MEDIDAS d
                JOIN CATEGORIAS c ON d.CATEGORIA_ID = c.Id",

            ["MATERIAIS"] = @"
                SELECT m.Id,
                       c.NOME                                  AS CATEGORIA,
                       CAST(m.M1 AS VARCHAR(50))               AS DIAMETRO,
                       CAST(m.M2 AS VARCHAR(50))               AS ESPESSURA,
                       CAST(m.M3 AS VARCHAR(50))               AS COMPRIMENTO,
                       ISNULL(CAST(m.M4 AS VARCHAR(50)), '0')  AS M4,
                       m.COD1, m.COD2, m.COD3,
                       u.IDPDM                                 AS CAD_POR,
                       CONVERT(VARCHAR(10), m.DATE_PROJ, 103)  AS DATE_PROJ
                FROM MATERIAIS m
                JOIN CATEGORIAS c ON m.CATEGORIA_ID = c.Id
                JOIN USERS      u ON m.CAD_POR      = u.Id"
        };

        // ── Carregamento ─────────────────────────────────────────────────────────

        public void CarregarDadosIniciais()
        {
            try
            {
                using (var conn = Connect())
                {
                    conn.Open();
                    foreach (var kv in _cacheQueries)
                    {
                        using (var adapter = new SqlDataAdapter(kv.Value, conn))
                        {
                            var dt = new DataTable();
                            adapter.Fill(dt);
                            dadosEmMemoria[kv.Key] = dt;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar dados iniciais: {ex.Message}", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public DataTable ObterTabela(string nomeTabela) =>
            dadosEmMemoria.TryGetValue(nomeTabela, out var dt) ? dt : null;

        public async Task AtualizarMemoriaAsync(string tabela)
        {
            if (!_cacheQueries.TryGetValue(tabela, out string query))
            {
                MessageBox.Show($"Tabela {tabela} não encontrada.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                using (var conn = Connect())
                {
                    await conn.OpenAsync();
                    using (var adapter = new SqlDataAdapter(query, conn))
                    {
                        var dt = new DataTable();
                        adapter.Fill(dt);
                        dadosEmMemoria[tabela] = dt;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao atualizar {tabela}: {ex.Message}", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public bool CanConnect()
        {
            try { using (var conn = Connect()) { conn.Open(); return true; } }
            catch { return false; }
        }

        // ── Helpers de cache ─────────────────────────────────────────────────────

        private int? GetCategoriaId(string nome)
        {
            if (!dadosEmMemoria.TryGetValue("CATEGORIAS", out var dt)) return null;
            var row = dt.AsEnumerable().FirstOrDefault(r =>
                string.Equals(r["MATERIAL"]?.ToString().Trim(), nome,
                    StringComparison.OrdinalIgnoreCase));
            return row == null ? (int?)null : Convert.ToInt32(row["Id"]);
        }

        private int? GetUserId(string idpdm)
        {
            if (!dadosEmMemoria.TryGetValue("USERS", out var dt)) return null;
            var row = dt.AsEnumerable().FirstOrDefault(r =>
                string.Equals(r["IDPDM"]?.ToString().Trim(), idpdm,
                    StringComparison.OrdinalIgnoreCase));
            return row == null ? (int?)null : Convert.ToInt32(row["Id"]);
        }

        public string GetUserPermissao(string idpdm)
        {
            if (!dadosEmMemoria.TryGetValue("USERS", out var dt)) return "leitor";
            var row = dt.AsEnumerable().FirstOrDefault(r =>
                string.Equals(r["IDPDM"]?.ToString().Trim(), idpdm,
                    StringComparison.OrdinalIgnoreCase));
            return row == null ? "leitor" : (row["PERMISSAO"]?.ToString() ?? "leitor");
        }

        private async Task<int> GetOrCreateUserAsync(SqlConnection conn, string idpdm)
        {
            int? cached = GetUserId(idpdm);
            if (cached != null) return cached.Value;

            var check = new SqlCommand("SELECT Id FROM USERS WHERE IDPDM = @u", conn);
            check.Parameters.AddWithValue("@u", idpdm);
            object r = await check.ExecuteScalarAsync();
            if (r != null && r != DBNull.Value) return Convert.ToInt32(r);

            var ins = new SqlCommand(
                "INSERT INTO USERS (IDPDM, PERMISSAO_ID) OUTPUT INSERTED.Id VALUES (@u, 2)", conn);
            ins.Parameters.AddWithValue("@u", idpdm);
            int newId = Convert.ToInt32(await ins.ExecuteScalarAsync());

            if (dadosEmMemoria.ContainsKey("USERS"))
            {
                var row = dadosEmMemoria["USERS"].NewRow();
                row["Id"] = newId; row["IDPDM"] = idpdm; row["PERMISSAO"] = "usuario";
                dadosEmMemoria["USERS"].Rows.Add(row);
            }
            return newId;
        }

        // ── USERS CRUD ───────────────────────────────────────────────────────────

        public async Task<bool> InsertUserAsync(string idpdm, string permissao)
        {
            try
            {
                using (var conn = Connect())
                {
                    await conn.OpenAsync();
                    int permId = await GetPermissaoIdAsync(conn, permissao);
                    var cmd = new SqlCommand(
                        "INSERT INTO USERS (IDPDM, PERMISSAO_ID) VALUES (@u, @p)", conn);
                    cmd.Parameters.AddWithValue("@u", idpdm ?? "");
                    cmd.Parameters.AddWithValue("@p", permId);
                    await cmd.ExecuteNonQueryAsync();
                }
                await AtualizarMemoriaAsync("USERS");
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao adicionar usuário: {ex.Message}", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public async Task<bool> UpdateUserPermissaoAsync(int userId, string permissao)
        {
            try
            {
                using (var conn = Connect())
                {
                    await conn.OpenAsync();
                    int permId = await GetPermissaoIdAsync(conn, permissao);
                    var cmd = new SqlCommand(
                        "UPDATE USERS SET PERMISSAO_ID = @p WHERE Id = @id", conn);
                    cmd.Parameters.AddWithValue("@p", permId);
                    cmd.Parameters.AddWithValue("@id", userId);
                    await cmd.ExecuteNonQueryAsync();
                }
                await AtualizarMemoriaAsync("USERS");
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao alterar permissão: {ex.Message}", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            try
            {
                using (var conn = Connect())
                {
                    await conn.OpenAsync();
                    var cmd = new SqlCommand("DELETE FROM USERS WHERE Id = @id", conn);
                    cmd.Parameters.AddWithValue("@id", userId);
                    await cmd.ExecuteNonQueryAsync();
                }
                await AtualizarMemoriaAsync("USERS");
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao excluir usuário: {ex.Message}", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private async Task<int> GetPermissaoIdAsync(SqlConnection conn, string nome)
        {
            var cmd = new SqlCommand("SELECT Id FROM PERMISSOES WHERE NOME = @n", conn);
            cmd.Parameters.AddWithValue("@n", nome ?? "usuario");
            object r = await cmd.ExecuteScalarAsync();
            return (r != null && r != DBNull.Value) ? Convert.ToInt32(r) : 2;
        }

        // ── INSERT ───────────────────────────────────────────────────────────────

        public async Task<bool> InsertCategoriaAsync(string categoria, string tipo)
        {
            try
            {
                using (var conn = Connect())
                {
                    const string sql = "INSERT INTO CATEGORIAS (NOME, TIPO_ID) VALUES (@nome, @tipo)";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@nome", categoria ?? "");
                        cmd.Parameters.AddWithValue("@tipo",
                            int.TryParse(tipo, out int t) ? (object)t : DBNull.Value);
                        await conn.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                if (dadosEmMemoria.ContainsKey("CATEGORIAS"))
                {
                    var row = dadosEmMemoria["CATEGORIAS"].NewRow();
                    row["MATERIAL"] = categoria;
                    row["TIPO"]     = tipo;
                    dadosEmMemoria["CATEGORIAS"].Rows.Add(row);
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao cadastrar categoria: {ex.Message}", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public async Task<bool> CadastrarItensDataGridAsync(DataGridView dataGrid, string user)
        {
            try
            {
                using (var conn = Connect())
                {
                    await conn.OpenAsync();
                    int userId = await GetOrCreateUserAsync(conn, user);

                    using (var tx = conn.BeginTransaction())
                    {
                        const string sql = @"
                            INSERT INTO MATERIAIS
                                (CATEGORIA_ID, M1, M2, M3, M4, COD1, COD2, COD3, CAD_POR, DATE_PROJ)
                            VALUES
                                (@catId, @m1, @m2, @m3, @m4, @c1, @c2, @c3, @cad, @dt)";

                        foreach (DataGridViewRow row in dataGrid.Rows)
                        {
                            if (row.IsNewRow) continue;

                            string catNome = row.Cells[1].Value?.ToString() ?? "";
                            int catId = GetCategoriaId(catNome)
                                ?? throw new Exception($"Categoria '{catNome}' não encontrada.");

                            string c1 = row.Cells[6].Value?.ToString() ?? "";
                            string c2 = row.Cells[7].Value?.ToString() ?? "";
                            string c3 = row.Cells[8].Value?.ToString() ?? "";

                            decimal  m1 = ToDecimal(row.Cells[2].Value?.ToString());
                            decimal  m2 = ToDecimal(row.Cells[3].Value?.ToString());
                            decimal  m3 = ToDecimal(row.Cells[4].Value?.ToString());
                            decimal? m4 = ToDecimalNull(row.Cells[5].Value?.ToString());

                            using (var cmd = new SqlCommand(sql, conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@catId", catId);
                                cmd.Parameters.AddWithValue("@m1",   m1);
                                cmd.Parameters.AddWithValue("@m2",   m2);
                                cmd.Parameters.AddWithValue("@m3",   m3);
                                cmd.Parameters.Add(DecimalParam("@m4", m4));
                                cmd.Parameters.AddWithValue("@c1",   c1);
                                cmd.Parameters.AddWithValue("@c2",   c2);
                                cmd.Parameters.AddWithValue("@c3",   c3);
                                cmd.Parameters.AddWithValue("@cad",  userId);
                                cmd.Parameters.AddWithValue("@dt",   DateTime.Today);
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                        tx.Commit();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao cadastrar itens: {ex.Message}", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // ── UPDATE ───────────────────────────────────────────────────────────────

        public async Task<bool> UpdateMaterialAsync(string cod1, string cod2, string cod3,
            string categoria, string diametro, string espessura, string comprimento, string m4)
        {
            try
            {
                using (var conn = Connect())
                {
                    const string sql = @"
                        UPDATE MATERIAIS
                        SET CATEGORIA_ID = (SELECT Id FROM CATEGORIAS WHERE NOME = @cat),
                            M1 = @m1, M2 = @m2, M3 = @m3, M4 = @m4
                        WHERE COD1 = @cod1 AND COD2 = @cod2 AND COD3 = @cod3";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@cat",  categoria ?? "");
                        cmd.Parameters.AddWithValue("@m1",   ToDecimal(diametro));
                        cmd.Parameters.AddWithValue("@m2",   ToDecimal(espessura));
                        cmd.Parameters.AddWithValue("@m3",   ToDecimal(comprimento));
                        cmd.Parameters.Add(DecimalParam("@m4", ToDecimalNull(m4)));
                        cmd.Parameters.AddWithValue("@cod1", cod1);
                        cmd.Parameters.AddWithValue("@cod2", cod2);
                        cmd.Parameters.AddWithValue("@cod3", cod3);
                        await conn.OpenAsync();
                        int rows = await cmd.ExecuteNonQueryAsync();
                        if (rows > 0)
                            SyncMaterialCache(cod1, cod2, cod3, categoria, diametro, espessura, comprimento, m4);
                        return rows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao atualizar material: {ex.Message}", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void SyncMaterialCache(string cod1, string cod2, string cod3,
            string categoria, string diametro, string espessura, string comprimento, string m4)
        {
            if (!dadosEmMemoria.ContainsKey("MATERIAIS")) return;
            var row = dadosEmMemoria["MATERIAIS"].AsEnumerable().FirstOrDefault(r =>
                r["COD1"]?.ToString().Trim() == cod1 &&
                r["COD2"]?.ToString().Trim() == cod2 &&
                r["COD3"]?.ToString().Trim() == cod3);
            if (row == null) return;
            row["CATEGORIA"]   = categoria;
            row["DIAMETRO"]    = diametro;
            row["ESPESSURA"]   = espessura;
            row["COMPRIMENTO"] = comprimento;
            row["M4"]          = m4 ?? "0";
        }

        public async Task<bool> UpdateCategoriaAsync(string materialAtual, string novoMaterial, string novoTipo)
        {
            try
            {
                using (var conn = Connect())
                {
                    const string sql =
                        "UPDATE CATEGORIAS SET NOME = @novo, TIPO_ID = @tipo WHERE NOME = @atual";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@novo",  novoMaterial ?? "");
                        cmd.Parameters.AddWithValue("@tipo",
                            int.TryParse(novoTipo, out int t) ? (object)t : DBNull.Value);
                        cmd.Parameters.AddWithValue("@atual", materialAtual);
                        await conn.OpenAsync();
                        int rows = await cmd.ExecuteNonQueryAsync();

                        if (rows > 0 && dadosEmMemoria.ContainsKey("CATEGORIAS"))
                        {
                            var row = dadosEmMemoria["CATEGORIAS"].AsEnumerable()
                                .FirstOrDefault(r => r["MATERIAL"]?.ToString().Trim() == materialAtual);
                            if (row != null) { row["MATERIAL"] = novoMaterial; row["TIPO"] = novoTipo; }
                        }
                        return rows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao atualizar categoria: {ex.Message}", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // ── DELETE ───────────────────────────────────────────────────────────────

        public async Task<bool> DeleteItemAsync(string tabela, string item, string columnTable)
        {
            // Mapeia aliases para nomes reais (os forms passam o alias, o banco precisa do nome real)
            string realCol = (tabela == "CATEGORIAS" && columnTable == "MATERIAL") ? "NOME" : columnTable;

            try
            {
                using (var conn = Connect())
                {
                    using (var cmd = new SqlCommand(
                        $"DELETE FROM {tabela} WHERE {realCol} = @item", conn))
                    {
                        cmd.Parameters.AddWithValue("@item", item);
                        await conn.OpenAsync();
                        int rows = await cmd.ExecuteNonQueryAsync();

                        if (rows > 0 && dadosEmMemoria.ContainsKey(tabela))
                        {
                            var dt = dadosEmMemoria[tabela];
                            foreach (var row in dt.AsEnumerable()
                                .Where(r => r[columnTable]?.ToString().Trim() == item).ToList())
                                dt.Rows.Remove(row);
                            dt.AcceptChanges();
                        }
                        else if (rows == 0)
                        {
                            MessageBox.Show("Nenhum item encontrado para exclusão!", "Aviso",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        return rows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao excluir item: {ex.Message}", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public async Task ExcluirMateriaisPorCategoriaAsync(string categoria)
        {
            try
            {
                using (var conn = Connect())
                {
                    const string sql = @"
                        DELETE FROM MATERIAIS
                        WHERE CATEGORIA_ID = (SELECT Id FROM CATEGORIAS WHERE NOME = @cat)";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@cat", categoria);
                        await conn.OpenAsync();
                        int rows = await cmd.ExecuteNonQueryAsync();

                        MessageBox.Show($"{rows} materiais da categoria '{categoria}' excluídos.",
                            "Exclusão Concluída", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        if (dadosEmMemoria.ContainsKey("MATERIAIS"))
                        {
                            var dt = dadosEmMemoria["MATERIAIS"];
                            foreach (var row in dt.AsEnumerable()
                                .Where(r => r["CATEGORIA"]?.ToString().Trim() == categoria).ToList())
                                dt.Rows.Remove(row);
                            dt.AcceptChanges();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao excluir materiais: {ex.Message}", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public async Task RemoverDuplicatasAsync(string tableName, string idColumn, List<string> colunas)
        {
            string cols = string.Join(", ", colunas);
            string sql  = $@"
                WITH CTE AS (
                    SELECT {idColumn},
                           ROW_NUMBER() OVER (PARTITION BY {cols} ORDER BY {idColumn}) rn
                    FROM {tableName}
                )
                DELETE FROM {tableName} WHERE {idColumn} IN (
                    SELECT {idColumn} FROM CTE WHERE rn > 1
                );";

            using (var conn = Connect())
            using (var cmd  = new SqlCommand(sql, conn))
            {
                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
        }

        // ── SELECT (cache) ───────────────────────────────────────────────────────

        public List<List<string>> GetAllValues(List<string> colunasDesejadas, string tabela)
        {
            var result = new List<List<string>>();
            if (dadosEmMemoria.TryGetValue(tabela, out var dt))
            {
                foreach (DataRow row in dt.Rows)
                    result.Add(colunasDesejadas.Select(c => row[c]?.ToString().Trim()).ToList());
            }
            else
            {
                MessageBox.Show($"Tabela {tabela} não carregada em memória.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return result;
        }

        public List<string> GetValColumn(string coluna, string tabela)
        {
            if (dadosEmMemoria.TryGetValue(tabela, out var dt))
            {
                return dt.AsEnumerable()
                    .Select(r => r[coluna]?.ToString().Trim())
                    .Where(v => !string.IsNullOrEmpty(v))
                    .Distinct()
                    .ToList();
            }
            MessageBox.Show($"Tabela {tabela} não carregada em memória.", "Aviso",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return new List<string>();
        }

        public List<string> GetRowValues(Dictionary<string, object> filtros,
            List<string> colunasDesejadas, string tabela)
        {
            var resultado = new HashSet<string>();
            if (!dadosEmMemoria.TryGetValue(tabela, out var dt))
            {
                MessageBox.Show($"Tabela {tabela} não carregada em memória.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return resultado.ToList();
            }

            var query = dt.AsEnumerable();
            foreach (var filtro in filtros)
            {
                string key       = filtro.Key;
                string filterVal = filtro.Value.ToString();

                query = query.Where(row =>
                {
                    string rowVal = row[key]?.ToString().Trim() ?? "";
                    // Comparação decimal: "50.800" == "50.8" sem depender de formato
                    if (decimal.TryParse(rowVal.Replace(",", "."),
                            NumberStyles.Any, CultureInfo.InvariantCulture, out decimal rv) &&
                        decimal.TryParse(filterVal.Replace(",", "."),
                            NumberStyles.Any, CultureInfo.InvariantCulture, out decimal fv))
                        return rv == fv;
                    return string.Equals(rowVal, filterVal, StringComparison.OrdinalIgnoreCase);
                });
            }

            foreach (var row in query)
                foreach (var col in colunasDesejadas)
                {
                    string val = row[col]?.ToString().Trim();
                    if (!string.IsNullOrEmpty(val) && val != "0.0" && val != "0")
                        resultado.Add(val);
                }

            return resultado.ToList();
        }

        // ── UPDATE completo de material (permite alterar códigos) ────────────────

        public async Task<bool> UpdateMaterialFullAsync(
            string origCod1, string origCod2, string origCod3,
            string newCod1,  string newCod2,  string newCod3,
            string categoria, string m1, string m2, string m3, string m4)
        {
            try
            {
                using (var conn = Connect())
                {
                    const string sql = @"
                        UPDATE MATERIAIS
                        SET CATEGORIA_ID = (SELECT Id FROM CATEGORIAS WHERE NOME = @cat),
                            M1 = @m1, M2 = @m2, M3 = @m3, M4 = @m4,
                            COD1 = @nc1, COD2 = @nc2, COD3 = @nc3
                        WHERE COD1 = @oc1 AND COD2 = @oc2 AND COD3 = @oc3";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@cat", categoria ?? "");
                        cmd.Parameters.AddWithValue("@m1",  ToDecimal(m1));
                        cmd.Parameters.AddWithValue("@m2",  ToDecimal(m2));
                        cmd.Parameters.AddWithValue("@m3",  ToDecimal(m3));
                        cmd.Parameters.Add(DecimalParam("@m4", ToDecimalNull(m4)));
                        cmd.Parameters.AddWithValue("@nc1", newCod1 ?? "");
                        cmd.Parameters.AddWithValue("@nc2", newCod2 ?? "");
                        cmd.Parameters.AddWithValue("@nc3", newCod3 ?? "");
                        cmd.Parameters.AddWithValue("@oc1", origCod1 ?? "");
                        cmd.Parameters.AddWithValue("@oc2", origCod2 ?? "");
                        cmd.Parameters.AddWithValue("@oc3", origCod3 ?? "");
                        await conn.OpenAsync();
                        int rows = await cmd.ExecuteNonQueryAsync();
                        if (rows > 0) await AtualizarMemoriaAsync("MATERIAIS");
                        return rows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao atualizar material: {ex.Message}", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // ── MEDIDAS CRUD ─────────────────────────────────────────────────────────

        public async Task<bool> InsertMedidaAsync(
            string categoria, string m1, string m2, string m3, string m4)
        {
            try
            {
                using (var conn = Connect())
                {
                    const string sql = @"
                        INSERT INTO MEDIDAS (CATEGORIA_ID, M1, M2, M3, M4)
                        VALUES ((SELECT Id FROM CATEGORIAS WHERE NOME = @cat),
                                @m1, @m2, @m3, @m4)";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@cat", categoria ?? "");
                        cmd.Parameters.AddWithValue("@m1",  ToDecimal(m1));
                        cmd.Parameters.AddWithValue("@m2",  ToDecimal(m2));
                        cmd.Parameters.AddWithValue("@m3",  ToDecimal(m3));
                        cmd.Parameters.Add(DecimalParam("@m4", ToDecimalNull(m4)));
                        await conn.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                await AtualizarMemoriaAsync("MEDIDAS");
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao inserir medida: {ex.Message}", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public async Task<bool> UpdateMedidaAsync(
            int id, string categoria, string m1, string m2, string m3, string m4)
        {
            try
            {
                using (var conn = Connect())
                {
                    const string sql = @"
                        UPDATE MEDIDAS
                        SET CATEGORIA_ID = (SELECT Id FROM CATEGORIAS WHERE NOME = @cat),
                            M1 = @m1, M2 = @m2, M3 = @m3, M4 = @m4
                        WHERE Id = @id";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@cat", categoria ?? "");
                        cmd.Parameters.AddWithValue("@m1",  ToDecimal(m1));
                        cmd.Parameters.AddWithValue("@m2",  ToDecimal(m2));
                        cmd.Parameters.AddWithValue("@m3",  ToDecimal(m3));
                        cmd.Parameters.Add(DecimalParam("@m4", ToDecimalNull(m4)));
                        cmd.Parameters.AddWithValue("@id",  id);
                        await conn.OpenAsync();
                        int rows = await cmd.ExecuteNonQueryAsync();
                        if (rows > 0) await AtualizarMemoriaAsync("MEDIDAS");
                        return rows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao atualizar medida: {ex.Message}", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public async Task<bool> DeleteMedidaAsync(int id)
        {
            try
            {
                using (var conn = Connect())
                {
                    using (var cmd = new SqlCommand("DELETE FROM MEDIDAS WHERE Id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        await conn.OpenAsync();
                        int rows = await cmd.ExecuteNonQueryAsync();
                        if (rows > 0) await AtualizarMemoriaAsync("MEDIDAS");
                        return rows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao excluir medida: {ex.Message}", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // ── Conversões ───────────────────────────────────────────────────────────

        private static decimal ToDecimal(string v)
        {
            if (string.IsNullOrWhiteSpace(v)) return 0m;
            return decimal.TryParse(v.Replace(",", "."), NumberStyles.Any,
                CultureInfo.InvariantCulture, out decimal r) ? r : 0m;
        }

        private static decimal? ToDecimalNull(string v)
        {
            string s = v?.Trim();
            if (string.IsNullOrEmpty(s)) return null;
            if (!decimal.TryParse(s.Replace(",", "."), NumberStyles.Any,
                CultureInfo.InvariantCulture, out decimal r)) return null;
            return r == 0m ? (decimal?)null : r;
        }

        private static SqlParameter DecimalParam(string name, decimal? value) =>
            new SqlParameter(name, SqlDbType.Decimal)
            {
                Value     = (object)value ?? DBNull.Value,
                Precision = 10,
                Scale     = 3
            };
    }
}
