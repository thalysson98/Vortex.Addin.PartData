using SolidWorks.Interop.sldworks;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vortex.Addin.PartData.Core;

namespace Vortex.Addin.PartData
{
    public partial class Cadastro_main : Form
    {
        EPDMHandler PdmEvents;
        SQLCommands sqlCommand;
        SldWorks swApp;
        private ModelDoc2 swModel;

        public Cadastro_main(SldWorks app, SQLCommands sql_comm)
        {
            InitializeComponent();
            swApp = app;
            PdmEvents = new EPDMHandler();
            sqlCommand = sql_comm;
        }

        public void Cadastro_main_Load(object sender, EventArgs e)
        {
            if (PdmEvents.Connect())
            {
                User_PDM_lb.Text = PdmEvents.GetUser();
                data_lb.Text = DateTime.Now.ToString("dd/MM/yyyy");
                List<string> categorias = sqlCommand.GetValColumn("MATERIAL", "CATEGORIAS");
                foreach (string categoria in categorias)
                {
                    Categoria_CBox.Items.Add(categoria.Trim());
                    ExcCategoria_cb.Items.Add(categoria.Trim());
                }
                Cadastrar_bt.Enabled = false;
                manual_check.Checked = true;
                type1_chk.Checked = true;
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void Cadastrar_bt_Click(object sender, EventArgs e)
        {
            if (PdmEvents.Connect()&& RowMask(dataGridView1) && DestacarLinhasDuplicadas(dataGridView1))
            {
                if (sqlCommand.CadastrarItensDataGrid(dataGridView1, User_PDM_lb.Text))
                {
                    
                    sqlCommand.CarregarDadosIniciais();
                    dataGridView1.Rows.Clear();
                    MessageBox.Show("Itens cadastrados com sucesso!", "Operação Finalizada", MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
                else { MessageBox.Show("Não foi possível cadastrar os itens.", "Operação Finalizada", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
            else
            {
                Cadastrar_bt.Enabled = false;
            }


        }
        public void PreencherTextBoxComLinhaSelecionada(DataGridView dataGridView)
        {
            if (dataGridView.SelectedRows.Count > 0) // Verifica se há uma linha selecionada
            {
                DataGridViewRow linhaSelecionada = dataGridView.SelectedRows[0]; // Obtém a primeira linha selecionada

                // Preenche os TextBoxes com os valores das colunas do DataGridView
                FCategoria_cb.Text = linhaSelecionada.Cells[1].Value?.ToString() ?? "";
                FM1_txt.Text = linhaSelecionada.Cells[2].Value?.ToString() ?? "";
                FM2_txt.Text = linhaSelecionada.Cells[3].Value?.ToString() ?? "";
                FM3_txt.Text = linhaSelecionada.Cells[4].Value?.ToString() ?? "";
                FM4_txt.Text = linhaSelecionada.Cells[5].Value?.ToString() ?? "";
                FCod1_txt.Text = linhaSelecionada.Cells[6].Value?.ToString() ?? "";
                FCod2_txt.Text = linhaSelecionada.Cells[7].Value?.ToString() ?? "";
                FCod3_txt.Text = linhaSelecionada.Cells[8].Value?.ToString() ?? "";
            }
        }

        private void dataGridView_SelectionChanged(object sender, EventArgs e)
        {
            PreencherTextBoxComLinhaSelecionada((DataGridView)sender);
        }
        public void Buscar_bt_Click(object sender, EventArgs e)
        {
            if (PdmEvents.Connect())
            {
                List<string> colunas = new List<string> { "CATEGORIA", "DIAMETRO", "ESPESSURA", "COMPRIMENTO", "M4", "COD1", "COD2", "COD3", "CAD_POR", "DATE_PROJ" };
                List<List<string>> items = sqlCommand.GetAllValues(colunas, "MATERIAIS");
                DataListGrid.Rows.Clear();

                if (items.Count > 0)
                {
                    int contador = 1; // Inicia a contagem
                    foreach (var row in items)
                    {
                        List<object> novaLinha = new List<object> { contador }; // Adiciona o contador como primeira coluna
                        novaLinha.AddRange(row); // Adiciona os outros valores
                        DataListGrid.Rows.Add(novaLinha.ToArray()); // Adiciona a linha completa
                        contador++; // Incrementa a contagem
                    }
                }
                DestacarLinhasDuplicadas(DataListGrid);
            }
            else
            {
                MessageBox.Show("Usuário não conectado, por favor realize o login no PDM", "Login inválido", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExcluiDUP_bt_Click(object sender, EventArgs e)
        {
            List<string> colunas = new List<string> { "CATEGORIA", "DIAMETRO", "ESPESSURA", "COMPRIMENTO", "M4", "COD1", "COD2", "COD3" };
            sqlCommand.RemoverDuplicatas("MATERIAIS", "ID", colunas);
            Buscar_bt_Click(sender, e);
        }

        private void Validate_bt_Click(object sender, EventArgs e)
        {
            if (PdmEvents.Connect())
            {
                if (RowMask(dataGridView1) && DestacarLinhasDuplicadas(dataGridView1)) { Cadastrar_bt.Enabled = true; }
                else { Cadastrar_bt.Enabled = false; }
            }
            else { MessageBox.Show("Usuario não conectado, por favor realize o login no PDM", "Login invalido", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void Add2_bt_Click(object sender, EventArgs e)
        {
            AddLine(dataGridView1);
            PreencherDataGrid(dataGridView1, textBox1);
            if (RowMask(dataGridView1) && DestacarLinhasDuplicadas(dataGridView1)) { Cadastrar_bt.Enabled = true; }
            else { Cadastrar_bt.Enabled = false; }
        }

        private void AddLine(DataGridView dataGridView)
        {
            string[] valores;
            if (!m4_txt.Visible)
            {
                valores = new string[] {
                (dataGridView.RowCount+1).ToString(),Categoria_CBox.Text,
                m1_txt?.Text,m2_txt?.Text,m3_txt?.Text,"0" ,
                Name1_txt?.Text, Name2_txt?.Text , Name3_txt?.Text };
            }
            else
            {
                valores = new string[] {
                (dataGridView.RowCount+1).ToString(),Categoria_CBox.Text,
                m1_txt?.Text,m2_txt?.Text,m3_txt?.Text,m4_txt?.Text ,
                Name1_txt?.Text, Name2_txt?.Text , Name3_txt?.Text };
            }
            foreach (var val in valores) if (val == string.Empty) { return; }

            if (valores.Length >= 8) dataGridView.Rows.Add(valores);
        }

        private async Task PreencherDataGrid(DataGridView dataGridView, TextBox textBox)
        {
            string[] linhas = textBox.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string linha in linhas)
            {
                string[] valores = linha.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                string[] valoresComLinha = new string[] { (dataGridView.RowCount + 1).ToString() }.Concat(valores).ToArray();
                if (valores.Length == 8)
                {
                    dataGridView.Rows.Add(valoresComLinha);

                }
            }
        }

        private bool DestacarLinhasDuplicadas(DataGridView dataGridView)
        {
            // Dicionário para armazenar a contagem de cada linha (excluindo a primeira coluna)
            Dictionary<string, List<int>> linhas = new Dictionary<string, List<int>>();
            bool error = true;

            for (int i = 0; i < dataGridView.Rows.Count; i++)
            {
                // Criando uma chave única para cada linha baseada em todas as colunas (exceto a primeira)
                string linha = string.Join("|", dataGridView.Rows[i].Cells.Cast<DataGridViewCell>()
                    .Skip(1) // Ignorar a primeira coluna
                    .Select(c => c.Value?.ToString() ?? "").ToArray());

                // Adiciona ao dicionário
                if (!linhas.ContainsKey(linha))
                {
                    linhas[linha] = new List<int>();
                }
                linhas[linha].Add(i);
            }
            foreach (var item in linhas)
            {
                List<int> indices = item.Value;

                if (indices.Count > 1) // Se houver mais de uma ocorrência (duplicado)
                {
                    dataGridView.Rows[indices[0]].DefaultCellStyle.BackColor = Color.Yellow; // Primeira ocorrência em amarelo

                    for (int j = 1; j < indices.Count; j++)
                    {
                        dataGridView.Rows[indices[j]].DefaultCellStyle.BackColor = Color.Red; // Outras ocorrências em vermelho
                    }
                    error = false;
                }
            }

            return error;
        }

        private bool RowMask(DataGridView dataGrid)
        {
            bool error = true;
            List<string> colunas = new List<string> { "COD1", "COD2", "COD3" };
            List<List<string>> items = sqlCommand.GetAllValues(colunas, "MATERIAIS");
            List<string> categorias = sqlCommand.GetValColumn("MATERIAL", "CATEGORIAS");

            foreach (DataGridViewRow row in dataGrid.Rows)
            {
                if (!row.IsNewRow) // Evita erro ao processar a linha vazia no final
                {
                    bool err = false;
                    if (!ValidateCOD($"{row.Cells[6]?.Value}.{row.Cells[7]?.Value}.{row.Cells[8]?.Value}", items))
                    {
                        row.Cells[1].Value = row.Cells[1].Value?.ToString().Trim(); // CATEGORIA
                        if (ValidateCat(row.Cells[1].Value?.ToString().Trim(), categorias))
                        {
                            row.Cells[1].Value = row.Cells[1].Value?.ToString().Trim();
                            row.DefaultCellStyle.BackColor = Color.Red; err = true; error = false;
                        }

                        for (int i = 2; i <= 5; i++)
                        {
                            string result;
                            if (!FormatDecimal(row.Cells[i].Value, out result)) row.Cells[i].Value = result;
                            else { row.DefaultCellStyle.BackColor = Color.Red; err = true; error = false; }
                        }

                        for (int i = 6; i <= 8; i++)
                        {
                            string result;
                            int v = 3;
                            if (i == 8) v = 4;
                            if (!FormatInteger(row.Cells[i].Value, v, out result)) row.Cells[i].Value = result;
                            else { row.DefaultCellStyle.BackColor = Color.Red; err = true; error = false; }
                        }

                    }
                    else { row.DefaultCellStyle.BackColor = Color.Red; err = true; error = false; }
                    if (!err) row.DefaultCellStyle.BackColor = Color.White;
                }
            }
            return error;
        }

        private bool ValidateCOD(string value, List<List<string>> items)
        {
            if (!string.IsNullOrEmpty(value) && items.Count > 0)
            {
                foreach (var row in items)
                {

                    if (row.Count >= 3)
                    {
                        string vCod = $"{row[0].Trim()}.{row[1].Trim()}.{row[2].Trim()}";
                        if (vCod == value)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool ValidateCat(string value, List<string> categorias)
        {

            foreach (string val in categorias)
            {
                if (val.Trim() == value)
                {
                    return false;
                }
            }
            return true;
        }

        private bool FormatDecimal(object value, out string result)
        {
            result = string.Empty;
            if (value == null) return true; // Valor padrão


            string[] vsa = value.ToString().Replace(",", ".").Split('.');
            string v = string.Empty;
            if (vsa.Length > 1)
            {
                if (vsa[1].Count() > 1)
                {
                    if (decimal.TryParse(value.ToString().Replace(",", "."), System.Globalization.NumberStyles.Any,
                                         System.Globalization.CultureInfo.InvariantCulture, out decimal res))
                    {
                        result = res.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
                        return false;// Mantém a casa decimal correta
                    }
                }
                else
                {
                    if (decimal.TryParse(value.ToString().Replace(",", "."), System.Globalization.NumberStyles.Any,
                                         System.Globalization.CultureInfo.InvariantCulture, out decimal res))
                    {
                        result = res.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture); // Mantém a casa decimal correta
                        return false;
                    }
                }
            }
            else
            {
                if (decimal.TryParse(value.ToString().Replace(",", "."), System.Globalization.NumberStyles.Any,
                                         System.Globalization.CultureInfo.InvariantCulture, out decimal res))
                {
                    result = res.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
                    return false;// Mantém a casa decimal correta
                }
            }
            return true; // Retorno seguro em caso de erro
        }

        private bool FormatInteger(object value, int digits, out string result)
        {
            result = string.Empty;
            if (value == null) return true;

            if (int.TryParse(value.ToString(), out int res))
            {
                if (res > 0)
                {
                    result = res.ToString(new string('0', digits));
                    if (result.Count() > digits) return true;
                    return false;
                }
            }

            return true;
        }

        private void SelectTipo(string material)
        {
            List<string> colunasDesejadas = new List<string> { "TIPO" };
            Dictionary<string, object> filtros = new Dictionary<string, object>
            {
                { "MATERIAL", material }
            };
            List<string> val = sqlCommand.GetRowValues(filtros, colunasDesejadas, "CATEGORIAS");
            if (val.Count > 0)
            {
                filtros = new Dictionary<string, object>
                {
                    { "TIPO", val[0].ToString() }
                };
                colunasDesejadas = new List<string> { "M1", "M2", "M3", "M4" };
                val = sqlCommand.GetRowValues(filtros, colunasDesejadas, "TIPOS");
                if (val.Count >= 4)
                {
                    LB1.Text = val[0];
                    LB2.Text = val[1];
                    LB3.Text = val[2];
                    LB4.Text = val[3];
                    if (val[3] == "-")
                    {
                        m4_txt.Visible = false;
                        LB4.Visible = false;
                        sk4_txt.Visible = false;
                        label6.Visible = false;
                    }
                    else
                    {
                        m4_txt.Visible = true;
                        LB4.Visible = true;
                        sk4_txt.Visible = true;
                        label6.Visible = true;
                    }

                }

            }


        }
        private string ObterMedida(string nomeDimensao, bool ultimo = false)
        {
            ModelDoc2 modelo = ObterModeloAtivo();
            bool isAngulo = false;
            if (LB4.Text == "Ângulo" && ultimo) { isAngulo = true; }

            if (modelo != null)
            {
                Dimension dim = (Dimension)modelo.Parameter(nomeDimensao);
                if (dim != null)
                {
                    double valor = dim.SystemValue; // O SolidWorks armazena todas as medidas em metros/radianos

                    if (isAngulo) // Se for um ângulo, converte de radianos para graus
                    {
                        valor = valor * (180 / Math.PI); // Conversão de radianos para graus
                        return valor.ToString("0.##"); // Formata corretamente com símbolo de graus
                    }
                    return (valor * 1000).ToString("0.###"); // Mantém a formatação padrão para outras medidas
                }
            }
            return "0.0"; // Retorno padrão caso a medida não seja encontrada
        }
        private ModelDoc2 ObterModeloAtivo()
        {
            if (swApp == null)
            {
                swApp = (SldWorks)Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application"));
            }

            swModel = (ModelDoc2)swApp.ActiveDoc;
            if (swModel != null)
            {
                string name = sw_GetNameFile(swModel);
                string[] partes = name.Split('.');
                string id = GerarHashArquivo(swModel.GetPathName());
                if (partes.Length == 3)
                {
                    string variavel1 = partes[0];
                    string variavel2 = partes[1];
                    string variavel3 = partes[2];

                    Name1_txt.Text = variavel1;
                    Name2_txt.Text = variavel2;
                    Name3_txt.Text = variavel3;
                }

            }
            else
            {
                Name1_txt.Clear();
                Name2_txt.Clear();
                Name3_txt.Clear();

            }
            return swModel;
        }
        private string GerarHashArquivo(string caminhoArquivo)
        {
            if (File.Exists(caminhoArquivo))
            {
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(caminhoArquivo))
                    {
                        byte[] hash = md5.ComputeHash(stream);
                        return BitConverter.ToString(hash).Replace("-", "").ToLower(); // Retorna um hash único
                    }
                }
            }
            return "Arquivo não encontrado!";
        }
        public string sw_GetNameFile(ModelDoc2 model)
        {
            try
            {
                string NameFile = model.GetPathName();
                string extractedName = NameFile.Substring(NameFile.LastIndexOf('\\') + 1);
                return extractedName.Substring(0, extractedName.Length - 7);
            }
            catch { return ""; }

        }
        private void Categoria_CBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectTipo(Categoria_CBox.Text);
            if (automatico_check.Checked == true)
            {
                sk1_txt.Clear();
                sk2_txt.Clear();
                sk3_txt.Clear();
                sk4_txt.Clear();
                coletarEsboco();

            }
        }


        private void sk2_txt_TextChanged(object sender, EventArgs e)
        {

        }

        private void manual_check_CheckedChanged(object sender, EventArgs e)
        {
            AutomaticoPane_pn.Visible = false;

        }

        private void automatico_check_CheckedChanged(object sender, EventArgs e)
        {
            AutomaticoPane_pn.Visible = true;

        }
        private void coletarEsboco()
        {
            string categoriaSelecionada = Categoria_CBox.SelectedItem.ToString();
            List<string> colunasDesejadas = new List<string> { "M1", "M2", "M3", "M4" };

            Dictionary<string, object> filtros = new Dictionary<string, object>
                    {
                        { "CATEGORIA", categoriaSelecionada }
                    };

            List<string> valoresLinha = sqlCommand.GetRowValues(filtros, colunasDesejadas, "MEDIDAS");
            if (valoresLinha.Count() > 0)
            {
                sk1_txt.Text = valoresLinha[0];
                sk2_txt.Text = valoresLinha[1];
                sk3_txt.Text = valoresLinha[2];
                sk4_txt.Text = valoresLinha[3];
            }
        }
        private void Coletar_bt_Click(object sender, EventArgs e)
        {

            if (Categoria_CBox.SelectedItem != null)
            {
                coletarEsboco();
                m1_txt.Text = ObterMedida(sk1_txt.Text);
                m2_txt.Text = ObterMedida(sk2_txt.Text);
                m3_txt.Text = ObterMedida(sk3_txt.Text);
                m4_txt.Text = ObterMedida(sk4_txt.Text, true);
            }
            else if (Categoria_CBox.Items.Count > 0)
            {
                Categoria_CBox.Text = Categoria_CBox.Items[0].ToString();
            }

        }

        private void sk1_txt_TextChanged(object sender, EventArgs e)
        {
            m1_txt.Text = ObterMedida(sk1_txt.Text);
        }

        private void sk2_txt_TextChanged_1(object sender, EventArgs e)
        {
            m2_txt.Text = ObterMedida(sk2_txt.Text);
        }

        private void sk3_txt_TextChanged(object sender, EventArgs e)
        {
            m3_txt.Text = ObterMedida(sk3_txt.Text);
        }

        private void sk4_txt_TextChanged(object sender, EventArgs e)
        {
            m4_txt.Text = ObterMedida(sk4_txt.Text, true);
        }

        private void excluir_bt_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.SelectedRows)
            {
                if (!row.IsNewRow) // Garante que a linha vazia não seja removida
                {
                    dataGridView1.Rows.Remove(row);
                }
            }
            if (RowMask(dataGridView1) && DestacarLinhasDuplicadas(dataGridView1)) { Cadastrar_bt.Enabled = true; }
            else { Cadastrar_bt.Enabled = false; }
        }
        string tipoSelecionado = "1";
        private void type1_chk_CheckedChanged(object sender, EventArgs e)
        {
            SelecTipo2("1");
            tipoSelecionado = "1";
        }

        private void type2_chk_CheckedChanged(object sender, EventArgs e)
        {
            SelecTipo2("2");
            tipoSelecionado = "2";
        }

        private void type3_chk_CheckedChanged(object sender, EventArgs e)
        {
            SelecTipo2("3");
            tipoSelecionado = "3";
        }

        private void type4_chk_CheckedChanged(object sender, EventArgs e)
        {
            SelecTipo2("4");
            tipoSelecionado = "4";
        }
        private void SelecTipo2(string selecao)
        {
            List<string> colunasDesejadas = new List<string> { "TIPO" };
            Dictionary<string, object> filtros = new Dictionary<string, object>
            {
                { "TIPO", selecao }
            };
            colunasDesejadas = new List<string> { "M1", "M2", "M3", "M4" };
            List<string> val = sqlCommand.GetRowValues(filtros, colunasDesejadas, "TIPOS");
            if (val.Count >= 3)
            {
                ConcatString_lb.Text = $"{val[0]}; {val[1]}; {val[2]}; {val[3]}";
            }
        }

        private void CadCat_bt_Click(object sender, EventArgs e)
        {
            if (NewCatName_txt.Text != "")
            {
                sqlCommand.InsertCategoria(NewCatName_txt.Text.ToUpper(), tipoSelecionado);
            }

            Categoria_CBox.Items.Clear();
            ExcCategoria_cb.Items.Clear();
            List<string> categorias = sqlCommand.GetValColumn("MATERIAL", "CATEGORIAS");
            foreach (string categoria in categorias)
            {
                Categoria_CBox.Items.Add(categoria.Trim());
                ExcCategoria_cb.Items.Add(categoria.Trim());
                NewCatName_txt.Clear();
            }
        }

        private void ExcluiCat_bt_Click(object sender, EventArgs e)
        {
            if (Allitems_ch.Checked)
            {
                DialogResult result = MessageBox.Show($"Tem certeza que deseja excluir todos os materiais da categoria '{ExcCategoria_cb.Text}'?",
            "Confirmação", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    sqlCommand.ExcluirMateriaisPorCategoria(ExcCategoria_cb.Text);
                    sqlCommand.DeleteItem("CATEGORIAS", ExcCategoria_cb.Text, "MATERIAL");
                    Categoria_CBox.Items.Clear();
                    ExcCategoria_cb.Text = "";
                    ExcCategoria_cb.Items.Clear();

                    List<string> categorias = sqlCommand.GetValColumn("MATERIAL", "CATEGORIAS");
                    foreach (string categoria in categorias)
                    {
                        Categoria_CBox.Items.Add(categoria.Trim());
                        ExcCategoria_cb.Items.Add(categoria.Trim());
                    }
                    //sqlCommand.CarregarDadosIniciais();
                }
            }
            else
            {
                DialogResult = MessageBox.Show("Deseja excluir a categoria selecionada?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (DialogResult.Yes == DialogResult)
                {
                    if (ExcCategoria_cb.Text != "")
                    {
                        sqlCommand.DeleteItem("CATEGORIAS", ExcCategoria_cb.Text, "MATERIAL");
                        Categoria_CBox.Items.Clear();
                        ExcCategoria_cb.Text = "";
                        ExcCategoria_cb.Items.Clear();

                        List<string> categorias = sqlCommand.GetValColumn("MATERIAL", "CATEGORIAS");
                        foreach (string categoria in categorias)
                        {
                            Categoria_CBox.Items.Add(categoria.Trim());
                            ExcCategoria_cb.Items.Add(categoria.Trim());
                        }
                    }

                }
            }

        }

        private void FCategoria_cb_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Visualizador form = new Visualizador(sqlCommand);
            form.Show();
        }
    }
}
