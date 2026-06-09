using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
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
                DataSource = "192.168.2.248\\ERASQL",
                UserID = "pdb_user",
                Password = "eng.2003",
                InitialCatalog = "PDB_BANCO"
            };
            return new SqlConnection(builder.ConnectionString);
        }

        public void CarregarDadosIniciais()
        {
            string[] tabelas = { "CATEGORIAS", "MATERIAIS", "MEDIDAS", "TIPOS", "USERS" };
            try
            {
                using (var conn = Connect())
                {
                    conn.Open();
                    foreach (var tabela in tabelas)
                    {
                        using (var adapter = new SqlDataAdapter($"SELECT * FROM {tabela}", conn))
                        {
                            var dt = new DataTable();
                            adapter.Fill(dt);
                            dadosEmMemoria[tabela] = dt;
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

        public DataTable ObterTabela(string nomeTabela)
        {
            return dadosEmMemoria.TryGetValue(nomeTabela, out var dt) ? dt : null;
        }

        public async Task AtualizarMemoriaAsync(string tabela)
        {
            if (!dadosEmMemoria.ContainsKey(tabela))
            {
                MessageBox.Show($"Tabela {tabela} não encontrada em memória!", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                using (var conn = Connect())
                {
                    await conn.OpenAsync();
                    using (var adapter = new SqlDataAdapter($"SELECT * FROM {tabela}", conn))
                    {
                        var dt = new DataTable();
                        adapter.Fill(dt);
                        dadosEmMemoria[tabela] = dt;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao atualizar dados da tabela {tabela}: {ex.Message}", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public bool CanConnect()
        {
            try
            {
                using (var conn = Connect())
                {
                    conn.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        // ── INSERT ──────────────────────────────────────────────────────────────

        public async Task<bool> InsertCategoriaAsync(string categoria, string tipo)
        {
            try
            {
                using (var conn = Connect())
                {
                    const string query = "INSERT INTO CATEGORIAS (MATERIAL, TIPO) VALUES (@material, @tipo)";
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@material", categoria ?? "");
                        cmd.Parameters.AddWithValue("@tipo", tipo ?? "");
                        await conn.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                if (dadosEmMemoria.ContainsKey("CATEGORIAS"))
                {
                    var newRow = dadosEmMemoria["CATEGORIAS"].NewRow();
                    newRow["MATERIAL"] = categoria;
                    newRow["TIPO"] = tipo;
                    dadosEmMemoria["CATEGORIAS"].Rows.Add(newRow);
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
                    using (var transaction = conn.BeginTransaction())
                    {
                        const string query = @"INSERT INTO MATERIAIS
                            (CATEGORIA, DIAMETRO, ESPESSURA, COMPRIMENTO, M4, COD1, COD2, COD3, CAD_POR, DATE_PROJ, PATH_FILE, NAME_FILE)
                            VALUES (@CATEGORIA, @DIAMETRO, @ESPESSURA, @COMPRIMENTO, @M4, @COD1, @COD2, @COD3, @CAD_POR, @DATE_PROJ, @PATH_FILE, @NAME_FILE)";

                        foreach (DataGridViewRow row in dataGrid.Rows)
                        {
                            if (row.IsNewRow) continue;
                            using (var cmd = new SqlCommand(query, conn, transaction))
                            {
                                string c1 = row.Cells[6].Value?.ToString() ?? "";
                                string c2 = row.Cells[7].Value?.ToString() ?? "";
                                string c3 = row.Cells[8].Value?.ToString() ?? "";
                                string codigo = $"{c1}.{c2}.{c3}";
                                string path = $"C:\\Cardall\\PROJETOS\\{c1}\\{c1}.{c2}\\{codigo}.sldprt";

                                cmd.Parameters.AddWithValue("@CATEGORIA", row.Cells[1].Value?.ToString() ?? "");
                                cmd.Parameters.AddWithValue("@DIAMETRO", row.Cells[2].Value?.ToString() ?? "");
                                cmd.Parameters.AddWithValue("@ESPESSURA", row.Cells[3].Value?.ToString() ?? "");
                                cmd.Parameters.AddWithValue("@COMPRIMENTO", row.Cells[4].Value?.ToString() ?? "");
                                cmd.Parameters.AddWithValue("@M4", row.Cells[5].Value?.ToString() ?? "");
                                cmd.Parameters.AddWithValue("@COD1", c1);
                                cmd.Parameters.AddWithValue("@COD2", c2);
                                cmd.Parameters.AddWithValue("@COD3", c3);
                                cmd.Parameters.AddWithValue("@CAD_POR", user ?? "");
                                cmd.Parameters.AddWithValue("@DATE_PROJ", DateTime.Now.ToString("dd/MM/yyyy"));
                                cmd.Parameters.AddWithValue("@PATH_FILE", path);
                                cmd.Parameters.AddWithValue("@NAME_FILE", codigo);
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                        transaction.Commit();
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

        // ── UPDATE ──────────────────────────────────────────────────────────────

        public async Task<bool> UpdateMaterialAsync(string cod1, string cod2, string cod3,
            string categoria, string diametro, string espessura, string comprimento, string m4)
        {
            try
            {
                using (var conn = Connect())
                {
                    const string query = @"UPDATE MATERIAIS
                        SET CATEGORIA=@cat, DIAMETRO=@diam, ESPESSURA=@esp, COMPRIMENTO=@comp, M4=@m4
                        WHERE COD1=@cod1 AND COD2=@cod2 AND COD3=@cod3";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@cat", categoria ?? "");
                        cmd.Parameters.AddWithValue("@diam", diametro ?? "");
                        cmd.Parameters.AddWithValue("@esp", espessura ?? "");
                        cmd.Parameters.AddWithValue("@comp", comprimento ?? "");
                        cmd.Parameters.AddWithValue("@m4", m4 ?? "");
                        cmd.Parameters.AddWithValue("@cod1", cod1);
                        cmd.Parameters.AddWithValue("@cod2", cod2);
                        cmd.Parameters.AddWithValue("@cod3", cod3);
                        await conn.OpenAsync();
                        int rows = await cmd.ExecuteNonQueryAsync();

                        if (rows > 0) SyncUpdateMaterialCache(cod1, cod2, cod3, categoria, diametro, espessura, comprimento, m4);
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

        private void SyncUpdateMaterialCache(string cod1, string cod2, string cod3,
            string categoria, string diametro, string espessura, string comprimento, string m4)
        {
            if (!dadosEmMemoria.ContainsKey("MATERIAIS")) return;

            var row = dadosEmMemoria["MATERIAIS"].AsEnumerable().FirstOrDefault(r =>
                r["COD1"]?.ToString().Trim() == cod1 &&
                r["COD2"]?.ToString().Trim() == cod2 &&
                r["COD3"]?.ToString().Trim() == cod3);

            if (row == null) return;
            row["CATEGORIA"] = categoria;
            row["DIAMETRO"] = diametro;
            row["ESPESSURA"] = espessura;
            row["COMPRIMENTO"] = comprimento;
            row["M4"] = m4;
        }

        public async Task<bool> UpdateCategoriaAsync(string materialAtual, string novoMaterial, string novoTipo)
        {
            try
            {
                using (var conn = Connect())
                {
                    const string query = "UPDATE CATEGORIAS SET MATERIAL=@novo, TIPO=@tipo WHERE MATERIAL=@atual";
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@novo", novoMaterial ?? "");
                        cmd.Parameters.AddWithValue("@tipo", novoTipo ?? "");
                        cmd.Parameters.AddWithValue("@atual", materialAtual);
                        await conn.OpenAsync();
                        int rows = await cmd.ExecuteNonQueryAsync();

                        if (rows > 0 && dadosEmMemoria.ContainsKey("CATEGORIAS"))
                        {
                            var row = dadosEmMemoria["CATEGORIAS"].AsEnumerable()
                                .FirstOrDefault(r => r["MATERIAL"]?.ToString().Trim() == materialAtual);
                            if (row != null)
                            {
                                row["MATERIAL"] = novoMaterial;
                                row["TIPO"] = novoTipo;
                            }
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

        // ── DELETE ──────────────────────────────────────────────────────────────

        public async Task<bool> DeleteItemAsync(string tabela, string item, string columnTable)
        {
            try
            {
                using (var conn = Connect())
                {
                    string query = $"DELETE FROM {tabela} WHERE {columnTable} = @item";
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@item", item);
                        await conn.OpenAsync();
                        int rows = await cmd.ExecuteNonQueryAsync();

                        if (rows > 0)
                        {
                            if (dadosEmMemoria.ContainsKey(tabela))
                            {
                                var dt = dadosEmMemoria[tabela];
                                var toDelete = dt.AsEnumerable()
                                    .Where(r => r[columnTable]?.ToString().Trim() == item).ToList();
                                foreach (var row in toDelete) dt.Rows.Remove(row);
                                dt.AcceptChanges();
                            }
                            return true;
                        }
                        else
                        {
                            MessageBox.Show("Nenhum item encontrado para exclusão!", "Aviso",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return false;
                        }
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
                    const string query = "DELETE FROM MATERIAIS WHERE CATEGORIA = @CATEGORIA";
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@CATEGORIA", categoria);
                        await conn.OpenAsync();
                        int rows = await cmd.ExecuteNonQueryAsync();

                        MessageBox.Show($"{rows} materiais da categoria '{categoria}' foram excluídos.",
                            "Exclusão Concluída", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        if (dadosEmMemoria.ContainsKey("MATERIAIS"))
                        {
                            var dt = dadosEmMemoria["MATERIAIS"];
                            var toDelete = dt.AsEnumerable()
                                .Where(r => r["CATEGORIA"]?.ToString().Trim() == categoria).ToList();
                            foreach (var row in toDelete) dt.Rows.Remove(row);
                            dt.AcceptChanges();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao excluir materiais da categoria: {ex.Message}", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public async Task RemoverDuplicatasAsync(string tableName, string idColumn, List<string> colunas)
        {
            string colunasJoin = string.Join(", ", colunas);
            string query = $@"
                WITH CTE_Duplicates AS (
                    SELECT {idColumn},
                        ROW_NUMBER() OVER (
                            PARTITION BY {colunasJoin}
                            ORDER BY {idColumn}
                        ) AS row_num
                    FROM {tableName}
                )
                DELETE FROM {tableName} WHERE {idColumn} IN (
                    SELECT {idColumn} FROM CTE_Duplicates WHERE row_num > 1
                );";

            using (var conn = Connect())
            {
                using (var cmd = new SqlCommand(query, conn))
                {
                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        // ── SELECT (cache) ───────────────────────────────────────────────────────

        public List<List<string>> GetAllValues(List<string> colunasDesejadas, string tabela)
        {
            var resultado = new List<List<string>>();
            if (dadosEmMemoria.TryGetValue(tabela, out var dt))
            {
                foreach (DataRow row in dt.Rows)
                    resultado.Add(colunasDesejadas.Select(col => row[col]?.ToString().Trim()).ToList());
            }
            else
            {
                MessageBox.Show($"Tabela {tabela} não carregada em memória.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return resultado;
        }

        public List<string> GetValColumn(string coluna, string tabela)
        {
            if (dadosEmMemoria.TryGetValue(tabela, out var dt))
            {
                return dt.AsEnumerable()
                    .Select(row => row[coluna]?.ToString().Trim())
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
            var valoresLinha = new HashSet<string>();
            if (dadosEmMemoria.TryGetValue(tabela, out var dt))
            {
                var query = dt.AsEnumerable();
                foreach (var filtro in filtros)
                    query = query.Where(row => row[filtro.Key]?.ToString().Trim() == filtro.Value.ToString());

                foreach (var row in query)
                {
                    foreach (var coluna in colunasDesejadas)
                    {
                        string valor = row[coluna]?.ToString().Trim();
                        if (!string.IsNullOrEmpty(valor) && valor != "0.0")
                            valoresLinha.Add(valor);
                    }
                }
            }
            else
            {
                MessageBox.Show($"Tabela {tabela} não carregada em memória.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return valoresLinha.ToList();
        }
    }
}
