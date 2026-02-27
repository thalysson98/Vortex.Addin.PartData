using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Documents;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;
using System.Data.SqlTypes;
using System.Globalization;

namespace Vortex.Addin.PartData.Core
{

    public class SQLCommands
    {
        private SqlConnection connection;
        private Dictionary<string, DataTable> dadosEmMemoria = new Dictionary<string, DataTable>();

        public SqlConnection Connect()
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = "192.168.2.248\\ERASQL",
                UserID = "pdb_user",
                Password = "eng.2003",
                InitialCatalog = "PDB_BANCO"
                //IntegratedSecurity = true
            };
            var connectionString = builder.ConnectionString;
            try
            {
                connection = new SqlConnection(connectionString);
                return connection;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao conectar ao banco de dados: {ex.Message}", "Inseridor de Peças", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        public void CarregarDadosIniciais()
        {
            string[] tabelas = { "CATEGORIAS", "MATERIAIS", "MEDIDAS", "TIPOS", "USERS" }; // Adicione outras tabelas conforme necessário

            try
            {
                using (connection = Connect())
                {
                    connection.Open();

                    foreach (var tabela in tabelas)
                    {
                        string query = $"SELECT * FROM {tabela}";
                        using (SqlDataAdapter adapter = new SqlDataAdapter(query, connection))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);
                            dadosEmMemoria[tabela] = dt;
                        }
                    }
                }

                //MessageBox.Show("Dados carregados em memória!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar dados iniciais: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public DataTable ObterTabela(string nomeTabela)
        {
            return dadosEmMemoria.ContainsKey(nomeTabela) ? dadosEmMemoria[nomeTabela] : null;
        }
       
        public void AtualizarMemoria(string tabela)
        {
            if (!dadosEmMemoria.ContainsKey(tabela))
            {
                MessageBox.Show($"Tabela {tabela} não encontrada em memória!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (connection = Connect())
                {
                    connection.Open();
                    string query = $"SELECT * FROM {tabela}";

                    using (SqlDataAdapter adapter = new SqlDataAdapter(query, connection))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        dadosEmMemoria[tabela] = dt;
                    }
                }

                MessageBox.Show($"Tabela {tabela} atualizada na memória.", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao atualizar dados da tabela {tabela}: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public bool oncon()
        {
            using (connection = Connect())
            {
                if (connection != null) { return false; }
            }
            return true;
        }

        public void Disconnect()
        {
            try
            {
                if (connection != null && connection.State != ConnectionState.Closed)
                {
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao desconectar do banco de dados: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public bool InsertCategoria(string categoria, string tipo)
        {
            try
            {
                using (connection = Connect())
                {
                    if (connection == null) return false;

                    string query = "INSERT INTO CATEGORIAS (MATERIAL, TIPO) VALUES (@material, @tipo)";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@material", categoria ?? "");
                        command.Parameters.AddWithValue("@tipo", tipo ?? "");
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                }

                if (dadosEmMemoria.ContainsKey("CATEGORIAS"))
                {
                    DataRow newRow = dadosEmMemoria["CATEGORIAS"].NewRow();
                    newRow["MATERIAL"] = categoria;
                    newRow["TIPO"] = tipo;
                    dadosEmMemoria["CATEGORIAS"].Rows.Add(newRow);
                }

                MessageBox.Show("Categoria cadastrada com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao cadastrar categoria: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public List<List<string>> GetAllValues(List<string> colunasDesejadas, string tabela)
        {
            List<List<string>> valoresLinha = new List<List<string>>();

            if (dadosEmMemoria.ContainsKey(tabela))
            {
                DataTable dt = dadosEmMemoria[tabela];
                foreach (DataRow row in dt.Rows)
                {
                    List<string> linha = colunasDesejadas.Select(col => row[col]?.ToString().Trim()).ToList();
                    valoresLinha.Add(linha);
                }
            }
            else
            {
                MessageBox.Show($"Tabela {tabela} não carregada em memória.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            return valoresLinha;
        }

        public bool DeleteItem(string tabela, string item, string columnTable)
        {
            try
            {
                using (connection = Connect())
                {
                    if (connection == null) return false;

                    string query = $"DELETE FROM {tabela} WHERE {columnTable} = @item";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@item", item);
                        connection.Open();
                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Item excluído com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            // Atualizar a memória removendo o item deletado
                            if (dadosEmMemoria.ContainsKey(tabela))
                            {
                                DataTable dt = dadosEmMemoria[tabela];
                                var rowsToDelete = dt.AsEnumerable().Where(row => row[columnTable]?.ToString().Trim() == item).ToList();
                                foreach (var row in rowsToDelete)
                                {
                                    dt.Rows.Remove(row);
                                }
                                dt.AcceptChanges();
                            }
                        }
                        else
                        {
                            MessageBox.Show("Nenhum item encontrado para exclusão!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao excluir item: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        
        public List<string> GetValColumn(string coluna, string tabela)
        {
            HashSet<string> valoresUnicos = new HashSet<string>();

            if (dadosEmMemoria.ContainsKey(tabela))
            {
                DataTable dt = dadosEmMemoria[tabela];
                valoresUnicos = dt.AsEnumerable()
                    .Select(row => row[coluna]?.ToString().Trim())
                    .Where(valor => !string.IsNullOrEmpty(valor))
                    .ToHashSet();
            }
            else
            {
                MessageBox.Show($"Tabela {tabela} não carregada em memória.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            return valoresUnicos.ToList();
        }
        public List<string> GetRowValues(Dictionary<string, object> filtros, List<string> colunasDesejadas, string tabela)
        {
            HashSet<string> valoresLinha = new HashSet<string>();

            if (dadosEmMemoria.ContainsKey(tabela))
            {
                DataTable dt = dadosEmMemoria[tabela];

                var query = dt.AsEnumerable();
                foreach (var filtro in filtros)
                {
                    query = query.Where(row => row[filtro.Key]?.ToString().Trim() == filtro.Value.ToString());
                }

                foreach (var row in query)
                {
                    foreach (var coluna in colunasDesejadas)
                    {
                        string valor = row[coluna]?.ToString().Trim();
                        if (!string.IsNullOrEmpty(valor) && valor != "0.0")
                        {
                            valoresLinha.Add(valor);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show($"Tabela {tabela} não carregada em memória.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            return valoresLinha.ToList();
        }
        
        public void RemoverDuplicatas(string tableName, string idColumn, List<string> colunas)
        {
            using (connection = Connect())
            {
                string colunasJoin = string.Join(", ", colunas);

                string query = $@"
            WITH CTE_Duplicates AS (
                SELECT 
                    {idColumn},
                    ROW_NUMBER() OVER (
                        PARTITION BY {colunasJoin} 
                        ORDER BY {idColumn} -- Ordenando pela chave primária para manter o menor ID
                    ) AS row_num
                FROM {tableName}
            )
            DELETE FROM {tableName} WHERE {idColumn} IN (
                SELECT {idColumn} FROM CTE_Duplicates WHERE row_num > 1
            );";

                SqlCommand command = new SqlCommand(query, connection);

                connection.Open();
                int rowsAffected = command.ExecuteNonQuery();
                connection.Close();

                Console.WriteLine($"{rowsAffected} linhas duplicadas removidas.");
            }
        }

        public bool CadastrarItensDataGrid(DataGridView dataGrid, string User)
        {
            try
            {
                using (connection = Connect())
                {
                    connection.Open();
                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        string query = @"INSERT INTO MATERIAIS (CATEGORIA, DIAMETRO, ESPESSURA, COMPRIMENTO, M4, COD1, COD2, COD3, CAD_POR, DATE_PROJ, PATH_FILE, NAME_FILE) 
                                        VALUES (@CATEGORIA, @DIAMETRO, @ESPESSURA, @COMPRIMENTO, @M4, @COD1, @COD2, @COD3, @CAD_POR, @DATE_PROJ, @PATH_FILE, @NAME_FILE)";

                        foreach (DataGridViewRow row in dataGrid.Rows)
                        {
                            if (!row.IsNewRow)
                            {
                                using (SqlCommand command = new SqlCommand(query, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@CATEGORIA", row.Cells[1].Value?.ToString() ?? "");
                                    command.Parameters.AddWithValue("@DIAMETRO", row.Cells[2].Value?.ToString() ?? "");
                                    command.Parameters.AddWithValue("@ESPESSURA", row.Cells[3].Value?.ToString() ?? "");
                                    command.Parameters.AddWithValue("@COMPRIMENTO", row.Cells[4].Value?.ToString() ?? "");
                                    command.Parameters.AddWithValue("@M4", row.Cells[5].Value?.ToString() ?? "");
                                    command.Parameters.AddWithValue("@COD1", row.Cells[6].Value?.ToString() ?? "");
                                    command.Parameters.AddWithValue("@COD2", row.Cells[7].Value?.ToString() ?? "");
                                    command.Parameters.AddWithValue("@COD3", row.Cells[8].Value?.ToString() ?? "");
                                    command.Parameters.AddWithValue("@CAD_POR", User ?? "");
                                    string codigo = $"{row.Cells[6].Value?.ToString() ?? ""}.{row.Cells[7].Value?.ToString() ?? ""}.{row.Cells[8].Value?.ToString() ?? ""}";
                                    string path = $"C:\\Cardall\\PROJETOS\\{row.Cells[6].Value?.ToString() ?? ""}\\{row.Cells[6].Value?.ToString() ?? ""}.{row.Cells[7].Value?.ToString() ?? ""}\\{codigo}.sldprt";
                                    command.Parameters.AddWithValue("@DATE_PROJ", DateTime.Now.ToString(("dd/MM/yyyy")) ?? "");
                                    command.Parameters.AddWithValue("@PATH_FILE", path ?? "");
                                    command.Parameters.AddWithValue("@NAME_FILE", codigo ?? "");
                                    command.ExecuteNonQuery();
                                }
                            }
                        }
                        transaction.Commit();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao cadastrar itens: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }


        public void ExcluirMateriaisPorCategoria(string categoria)
        {
            try
            {
                using (connection = Connect())
                {
                    if (connection == null) return;

                    string query = "DELETE FROM MATERIAIS WHERE CATEGORIA = @CATEGORIA";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@CATEGORIA", categoria);
                        connection.Open();
                        int rowsAffected = command.ExecuteNonQuery();

                        MessageBox.Show($"{rowsAffected} materiais da categoria '{categoria}' foram excluídos.",
                            "Exclusão Concluída", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Atualizar a memória removendo os itens deletados
                        if (dadosEmMemoria.ContainsKey("MATERIAIS"))
                        {
                            DataTable dt = dadosEmMemoria["MATERIAIS"];
                            var rowsToDelete = dt.AsEnumerable().Where(row => row["CATEGORIA"].ToString().Trim() == categoria).ToList();
                            foreach (var row in rowsToDelete)
                            {
                                dt.Rows.Remove(row);
                            }
                            dt.AcceptChanges();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao excluir materiais da categoria: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
