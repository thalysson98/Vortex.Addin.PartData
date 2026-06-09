using SolidWorks.Interop.sldworks;
//using SolidWorks.Interop.swdimxpert;
using SolidWorks.Interop.swconst;
using SolidWorks.Interop;
using System;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using System.Windows.Forms;
using Vortex.Addin.PartData.Core;
using System.Linq;
using static Vortex.Addin.PartData.SwAddin;

namespace Vortex.Addin.PartData
{

    public partial class MainForm : Form
    {
        SQLCommands sql_comm;
        public SldWorks swApp;
        string Codigo;
        string Cod1, Cod2;

        public MainForm(SldWorks app, SQLCommands sql)
        {
            swApp = app;
            sql_comm = sql;
            InitializeComponent();
            InitializeFoms();
        }

        public void InitializeFoms()
        {
            Inserir_bt.Enabled = false;
            if (sql_comm.CanConnect())
            {
                List<string> categorias = sql_comm.GetValColumn("CATEGORIA", "MATERIAIS");
                foreach (string categoria in categorias)
                {
                    Categoria_CBox2.Items.Add(categoria.Trim());
                }
            }
            else
            {
                menu.Enabled = false;
            }
        }
        private void clear(List<ComboBox> items)
        {
            if (items.Count > 0)
            {
                foreach(var item in items)
                {
                    item.Items.Clear();
                    item.Text = string.Empty;
                }
            }
        }
        private void SelectTipo(string material)
        {
            List<string> colunasDesejadas = new List<string> { "TIPO" };
            Dictionary<string, object> filtros = new Dictionary<string, object>
            {
                { "MATERIAL", material }
            };
            List<string> val = sql_comm.GetRowValues(filtros, colunasDesejadas, "CATEGORIAS");
            if (val.Count > 0)
            {
                filtros = new Dictionary<string, object>
                {
                    { "TIPO", val[0].ToString() }
                };
                colunasDesejadas = new List<string> { "M1", "M2", "M3", "M4" };
                val = sql_comm.GetRowValues(filtros, colunasDesejadas, "TIPOS");
                if (val.Count >= 4)
                {
                    LB2.Text = val[0];
                    LB3.Text = val[1];
                    LB4.Text = val[2];
                    LB5.Text = val[3];
                    if (val[3] == "-")
                    {
                        M4_Cbox.Visible = false;
                        LB5.Visible = false;
                    }
                    else
                    {
                        M4_Cbox.Visible = true;
                        LB5.Visible = true;
                    }

                }

            }


        }

        private void Categoria_CBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            List<ComboBox> items = new List<ComboBox> { Diametro_CBox, Espessura_Cbox , Comprimento_Cbox , M4_Cbox };
            clear(items);

            if (Categoria_CBox2.SelectedItem != null)
            {
                string categoriaSelecionada = Categoria_CBox2.SelectedItem.ToString();
                List<string> colunasDesejadas = new List<string> { "DIAMETRO" };

                Dictionary<string, object> filtros = new Dictionary<string, object>
                    {
                        { "CATEGORIA", categoriaSelecionada }
                    };

                List<string> valoresLinha = sql_comm.GetRowValues(filtros, colunasDesejadas, "MATERIAIS");

                // Chama a função para preencher o ComboBox em ordem decrescente
                PreencherComboBoxOrdenado(Diametro_CBox, valoresLinha);
                if (Diametro_CBox.Items.Count > 0) Diametro_CBox.SelectedIndex = 0;

                SelectTipo(categoriaSelecionada);
                Diametro_CBox.Focus();
            }
        }
        private string FormatDecimalForComboBox(object value)
        {
            if (value == null) return null; // Retorno seguro

            string valueStr = value.ToString().Replace(",", "."); // Garante o ponto decimal correto
            string[] partes = valueStr.Split('.'); // Separa a parte inteira da decimal

            if (partes.Length > 1) // Se tem casas decimais
            {
                if (partes[1].Length == 2) // Se já tem duas casas decimais, mantém como está
                {
                    return valueStr;
                }
                else // Se não tem duas casas decimais, formata para "0.0"
                {
                    if (decimal.TryParse(valueStr, System.Globalization.NumberStyles.Any,
                                         System.Globalization.CultureInfo.InvariantCulture, out decimal res))
                    {
                        return res.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
                    }
                }
            }
            else // Se for número inteiro, formata como "0.0"
            {
                if (decimal.TryParse(valueStr, System.Globalization.NumberStyles.Any,
                                     System.Globalization.CultureInfo.InvariantCulture, out decimal res))
                {
                    return res.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
                }
            }
            return null; // Retorno seguro se houver erro
        }

        // Armazena valores brutos (ex: "50.800") e exibe formatados (ex: "50.8")
        // O SelectedItem.ToString() retorna o valor bruto → comparação exata com o cache
        private static double ToSortDouble(string v)
        {
            double d;
            double.TryParse((v ?? "").Replace(",", "."),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out d);
            return d;
        }

        private void PreencherComboBoxOrdenado(ComboBox comboBox, List<string> valores)
        {
            comboBox.Items.Clear();
            comboBox.FormattingEnabled = true;
            comboBox.Format -= OnDecimalComboBoxFormat;
            comboBox.Format += OnDecimalComboBoxFormat;

            if (valores.Count > 0)
            {
                var items = valores.Where(v => !string.IsNullOrEmpty(v)).Distinct().ToList();
                items.Sort((a, b) => ToSortDouble(a).CompareTo(ToSortDouble(b)));
                comboBox.Items.AddRange(items.Cast<object>().ToArray());
            }
        }

        // Formata para exibição (o value interno continua o valor bruto do cache)
        private void OnDecimalComboBoxFormat(object sender, ListControlConvertEventArgs e)
        {
            if (e.Value is string v)
            {
                string fmt = FormatDecimalForComboBox(v);
                if (fmt != null) e.Value = fmt;
            }
        }

        private void Diametro_CBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            List<ComboBox> items = new List<ComboBox> { Espessura_Cbox, Comprimento_Cbox, M4_Cbox };
            clear(items);

            if (Diametro_CBox.SelectedItem != null && Categoria_CBox2.SelectedItem != null)
            {
                string categoriaSelecionada = Categoria_CBox2.SelectedItem.ToString();
                string diametroSelecionado = Diametro_CBox.SelectedItem.ToString();
                List<string> colunasDesejadas = new List<string> { "ESPESSURA" };

                Dictionary<string, object> filtros = new Dictionary<string, object>
                        {
                            { "CATEGORIA", categoriaSelecionada },
                            { "DIAMETRO", diametroSelecionado }
                        };

                List<string> valoresLinha = sql_comm.GetRowValues(filtros, colunasDesejadas, "MATERIAIS");

                PreencherComboBoxOrdenado(Espessura_Cbox, valoresLinha);
                if (Espessura_Cbox.Items.Count > 0) Espessura_Cbox.SelectedIndex = 0;
                Espessura_Cbox.Focus();
            }
        }

        private void Espessura_Cbox_SelectedIndexChanged(object sender, EventArgs e)
        {
            List<ComboBox> items = new List<ComboBox> { Comprimento_Cbox, M4_Cbox };
            clear(items);

            if (Espessura_Cbox.SelectedItem != null && Diametro_CBox.SelectedItem != null && Categoria_CBox2.SelectedItem != null)
            {
                string categoriaSelecionada = Categoria_CBox2.SelectedItem.ToString();
                string diametroSelecionado = Diametro_CBox.SelectedItem.ToString();
                string espessuraSelecionada = Espessura_Cbox.SelectedItem.ToString();
                List<string> colunasDesejadas = new List<string> { "COMPRIMENTO" };

                Dictionary<string, object> filtros = new Dictionary<string, object>
        {
            { "CATEGORIA", categoriaSelecionada },
            { "DIAMETRO", diametroSelecionado },
            { "ESPESSURA", espessuraSelecionada }
        };

                List<string> valoresLinha = sql_comm.GetRowValues(filtros, colunasDesejadas, "MATERIAIS");

                PreencherComboBoxOrdenado(Comprimento_Cbox, valoresLinha);
                if (Comprimento_Cbox.Items.Count > 0) Comprimento_Cbox.SelectedIndex = 0;
                Comprimento_Cbox.Focus();
            }
        }

        private void Comprimento_Cbox_SelectedIndexChanged(object sender, EventArgs e)
        {
            List<ComboBox> items = new List<ComboBox> {  M4_Cbox };
            clear(items);

            if (Comprimento_Cbox.SelectedItem != null && Espessura_Cbox.SelectedItem != null && Diametro_CBox.SelectedItem != null && Categoria_CBox2.SelectedItem != null)
            {
                string categoriaSelecionada = Categoria_CBox2.SelectedItem.ToString();
                string diametroSelecionado = Diametro_CBox.SelectedItem.ToString();
                string espessuraSelecionada = Espessura_Cbox.SelectedItem.ToString();
                string comprimentoSelecionado = Comprimento_Cbox.SelectedItem.ToString();
                List<string> colunasDesejadas = new List<string> { "M4" };

                Dictionary<string, object> filtros = new Dictionary<string, object>
                {
                    { "CATEGORIA", categoriaSelecionada },
                    { "DIAMETRO", diametroSelecionado },
                    { "ESPESSURA", espessuraSelecionada },
                    { "COMPRIMENTO", comprimentoSelecionado }
                };

                List<string> valoresLinha = sql_comm.GetRowValues(filtros, colunasDesejadas, "MATERIAIS");

                if (valoresLinha.Count > 0)
                {
                    PreencherComboBoxOrdenado(M4_Cbox, valoresLinha);
                    if (M4_Cbox.Items.Count > 0) M4_Cbox.SelectedIndex = 0;
                    M4_Cbox.Focus();
                }
                else
                {
                    M4_Cbox.Visible = false;
                    colunasDesejadas = new List<string> { "COD1", "COD2", "COD3" };
                    valoresLinha = sql_comm.GetRowValues(filtros, colunasDesejadas, "MATERIAIS");
                    if (valoresLinha.Count <= 3)
                    {
                        Codigo = $"{valoresLinha[0]}.{valoresLinha[1]}.{valoresLinha[2]}";
                        Cod1 = valoresLinha[0];
                        Cod2 = valoresLinha[1];
                    }
                    Inserir_bt.Enabled = true;
                    Inserir_bt.Focus();
                }
            }
            else Inserir_bt.Enabled = false;
        }

        private void M4_Cbox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Comprimento_Cbox.SelectedItem != null && Espessura_Cbox.SelectedItem != null && Diametro_CBox.SelectedItem != null && Categoria_CBox2.SelectedItem != null && M4_Cbox.SelectedItem != null)
            {
                string categoriaSelecionada = Categoria_CBox2.SelectedItem.ToString();
                string diametroSelecionado = Diametro_CBox.SelectedItem.ToString();
                string espessuraSelecionada = Espessura_Cbox.SelectedItem.ToString();
                string comprimentoSelecionado = Comprimento_Cbox.SelectedItem.ToString();
                string m4 = M4_Cbox.SelectedItem.ToString();
                List<string> colunasDesejadas = new List<string> { "COD1", "COD2", "COD3" };

                Dictionary<string, object> filtros = new Dictionary<string, object>
                {
                    { "CATEGORIA", categoriaSelecionada },
                    { "DIAMETRO", diametroSelecionado },
                    { "ESPESSURA", espessuraSelecionada },
                    { "COMPRIMENTO", comprimentoSelecionado },
                    { "M4", m4 }
                };

                List<string> valoresLinha = sql_comm.GetRowValues(filtros, colunasDesejadas, "MATERIAIS");

                colunasDesejadas = new List<string> { "COD1", "COD2", "COD3" };
                valoresLinha = sql_comm.GetRowValues(filtros, colunasDesejadas, "MATERIAIS");
                if (valoresLinha.Count <= 3)
                {
                    Codigo = $"{valoresLinha[0]}.{valoresLinha[1]}.{valoresLinha[2]}";
                    Cod1 = valoresLinha[0];
                    Cod2 = valoresLinha[1];
                }
                Inserir_bt.Enabled = true;
                Inserir_bt.Focus();

            }
            else Inserir_bt.Enabled = false;

        }
        public Cadastro_main cadastro { get; private set; }

        private void Cadastro_menu_Click(object sender, EventArgs e)
        {
            if (cadastro == null || cadastro.IsDisposed) // Se o formulário não existe ou foi fechado
            {
                cadastro = new Cadastro_main(swApp, sql_comm);
                cadastro.Show();
                this.Close();
            }
            else
            {
                cadastro.Focus(); // Se já está aberto, apenas traz para frente
                this.WindowState = FormWindowState.Minimized;
            }

        }

        private void Inserir_bt_Click(object sender, EventArgs e)
        {
            MathUtility swMathUtils;
            AssemblyDoc swAssy;
            ModelDoc2 swModel;
            ModelView swModelView;
            Mouse TheMouse;
            SolidWorksMouseHandler obj;

            string path = $"C:\\Cardall\\PROJETOS\\{Cod1}\\{Cod1}.{Cod2}\\{Codigo}.SLDPRT";
            swMathUtils = (MathUtility)swApp.GetMathUtility();
            swModel = (ModelDoc2)swApp.ActiveDoc;

            if (swModel != null && swModel.GetType() == (int)swDocumentTypes_e.swDocASSEMBLY)
            {
                swAssy = (AssemblyDoc)swModel;

                if (swAssy != null)
                {
                    swModelView = (ModelView)swModel.GetFirstModelView();
                    TheMouse = (Mouse)swModelView.GetMouse();
                    obj = new SolidWorksMouseHandler();
                    obj.Init(TheMouse, path, swModelView, swModel, swApp);
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("Por favor, abra uma Montagem.", "Comando inválido", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Exclamation);
                }
            }
        }

        private void Inserir_bt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Inserir_bt_Click(sender, e); // Executa o clique do botão
                e.SuppressKeyPress = true; // Evita que o Enter afete outros controles
            }
        }
    }
}