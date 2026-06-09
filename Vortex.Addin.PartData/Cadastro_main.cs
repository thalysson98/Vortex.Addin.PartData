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
using Vortex.Addin.PartData.Migration;

namespace Vortex.Addin.PartData
{
    public partial class Cadastro_main : Form
    {
        EPDMHandler PdmEvents;
        SQLCommands sqlCommand;
        SldWorks swApp;
        private ModelDoc2 swModel;

        // ── Banco de dados tab — cascade filter + salvar ─────────────────────────
        private ComboBox _bancoCatCb, _bancoM1Cb, _bancoM2Cb, _bancoM3Cb, _bancoM4Cb;
        private Button   _salvarBt;
        private string   _editOrigCod1, _editOrigCod2, _editOrigCod3;

        // ── Medidas tab ──────────────────────────────────────────────────────────
        private TabPage      _tabMedidas;
        private DataGridView _medidasGrid;
        private ComboBox     _medCatCb;
        private TextBox      _medM1Txt, _medM2Txt, _medM3Txt, _medM4Txt;
        private Button       _medAddBt, _medEditBt, _medDelBt;
        private int          _selectedMedidaId = -1;

        // ── Edit Category (groupBox3) ────────────────────────────────────────────
        private ComboBox     _editCatCb;
        private TextBox      _editCatNameTxt;
        private RadioButton  _editType1, _editType2, _editType3, _editType4;
        private Button       _alterTypeBt;
        private string       _editTipo = "1";

        public Cadastro_main(SldWorks app, SQLCommands sql_comm)
        {
            InitializeComponent();
            swApp = app;
            PdmEvents = new EPDMHandler();
            sqlCommand = sql_comm;
            AddMigrationButton();
            InitializeExtraControls();
        }

        // ─────────────────────────────────────────────────────────────────────────
        // CONTROLES CRIADOS EM CÓDIGO (não no Designer)
        // ─────────────────────────────────────────────────────────────────────────

        private void InitializeExtraControls()
        {
            InitializeBancoDadosTab();
            InitializeEditCatSection();
            InitializeMedidasTab();
            tabControl1.SelectedIndexChanged += TabControl1_SelectedIndexChanged;
        }

        private void TabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var tab = tabControl1.SelectedTab;
            if      (tab == tabPage2)     PopulateBancoCascade();
            else if (tab == tabPage5)     PopulateEditCatCb();
            else if (tab == _tabMedidas)  CarregarMedidasGrid();
        }

        // ── ABA: BANCO DE DADOS — cascade + salvar ───────────────────────────────

        private void InitializeBancoDadosTab()
        {
            // Move o grid para baixo para abrir espaço para os filtros
            DataListGrid.Location = new Point(3, 48);
            DataListGrid.Size     = new Size(726, 407);

            _bancoCatCb = MakeLabeledCb("Categoria", 3,   190, tabPage2);
            _bancoM1Cb  = MakeLabeledCb("Medida 1",  198,  80, tabPage2);
            _bancoM2Cb  = MakeLabeledCb("Medida 2",  283,  80, tabPage2);
            _bancoM3Cb  = MakeLabeledCb("Medida 3",  368,  80, tabPage2);
            _bancoM4Cb  = MakeLabeledCb("Medida 4",  453,  80, tabPage2);

            var btnLimpar = new Button { Text = "Limpar", Location = new Point(538, 22), Size = new Size(55, 21) };
            btnLimpar.Click += (s, ev) => LimparFiltrosBanco();
            tabPage2.Controls.Add(btnLimpar);

            _salvarBt = new Button
            {
                Text      = "Salvar Alteração",
                Location  = new Point(200, 540),
                Size      = new Size(130, 23),
                Enabled   = false,
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _salvarBt.FlatAppearance.BorderSize = 0;
            _salvarBt.Click += OnSalvarMaterial;
            tabPage2.Controls.Add(_salvarBt);

            _bancoCatCb.SelectedIndexChanged += OnBancoCatChanged;
            _bancoM1Cb .SelectedIndexChanged += OnBancoM1Changed;
            _bancoM2Cb .SelectedIndexChanged += OnBancoM2Changed;
            _bancoM3Cb .SelectedIndexChanged += OnBancoM3Changed;
            _bancoM4Cb .SelectedIndexChanged += OnBancoM4Changed;
        }

        private ComboBox MakeLabeledCb(string label, int x, int w, Control parent)
        {
            parent.Controls.Add(new Label { Text = label, Location = new Point(x, 5), AutoSize = true });
            var cb = new ComboBox
            {
                Location          = new Point(x, 22),
                Size              = new Size(w, 21),
                FormattingEnabled = true,
                DropDownStyle     = ComboBoxStyle.DropDownList
            };
            cb.Format += OnDecimalCbFormat;
            parent.Controls.Add(cb);
            return cb;
        }

        private void OnDecimalCbFormat(object sender, ListControlConvertEventArgs e)
        {
            if (e.Value is string v && decimal.TryParse(v.Replace(",", "."),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal d))
                e.Value = d.ToString("0.###",
                    System.Globalization.CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
        }

        private void PopulateBancoCascade()
        {
            if (_bancoCatCb.Items.Count > 0) return;
            _bancoCatCb.Items.Clear();
            foreach (var cat in sqlCommand.GetValColumn("CATEGORIA", "MATERIAIS"))
                _bancoCatCb.Items.Add(cat);
            // Popula também o FCategoria_cb (painel de edição) se ainda vazio
            if (FCategoria_cb.Items.Count == 0)
                foreach (var cat in sqlCommand.GetValColumn("MATERIAL", "CATEGORIAS"))
                    FCategoria_cb.Items.Add(cat.Trim());
        }

        private void OnBancoCatChanged(object sender, EventArgs e)
        {
            ClearCbs(_bancoM1Cb, _bancoM2Cb, _bancoM3Cb, _bancoM4Cb);
            DataListGrid.Rows.Clear();
            _salvarBt.Enabled = false;
            if (_bancoCatCb.SelectedItem == null) return;
            PopulateDecimalCb(_bancoM1Cb,
                sqlCommand.GetRowValues(
                    new Dictionary<string, object> { { "CATEGORIA", _bancoCatCb.SelectedItem.ToString() } },
                    new List<string> { "DIAMETRO" }, "MATERIAIS"));
        }

        private void OnBancoM1Changed(object sender, EventArgs e)
        {
            ClearCbs(_bancoM2Cb, _bancoM3Cb, _bancoM4Cb);
            FiltrarBancoDados();
            if (_bancoM1Cb.SelectedItem == null) return;
            PopulateDecimalCb(_bancoM2Cb,
                sqlCommand.GetRowValues(BuildBancoFiltros(1),
                    new List<string> { "ESPESSURA" }, "MATERIAIS"));
        }

        private void OnBancoM2Changed(object sender, EventArgs e)
        {
            ClearCbs(_bancoM3Cb, _bancoM4Cb);
            FiltrarBancoDados();
            if (_bancoM2Cb.SelectedItem == null) return;
            PopulateDecimalCb(_bancoM3Cb,
                sqlCommand.GetRowValues(BuildBancoFiltros(2),
                    new List<string> { "COMPRIMENTO" }, "MATERIAIS"));
        }

        private void OnBancoM3Changed(object sender, EventArgs e)
        {
            ClearCbs(_bancoM4Cb);
            FiltrarBancoDados();
            if (_bancoM3Cb.SelectedItem == null) return;
            PopulateDecimalCb(_bancoM4Cb,
                sqlCommand.GetRowValues(BuildBancoFiltros(3),
                    new List<string> { "M4" }, "MATERIAIS"));
        }

        private void OnBancoM4Changed(object sender, EventArgs e) => FiltrarBancoDados();

        private Dictionary<string, object> BuildBancoFiltros(int max = 4)
        {
            var f = new Dictionary<string, object>();
            if (max >= 0 && _bancoCatCb.SelectedItem != null) f["CATEGORIA"]   = _bancoCatCb.SelectedItem.ToString();
            if (max >= 1 && _bancoM1Cb .SelectedItem != null) f["DIAMETRO"]    = _bancoM1Cb.SelectedItem.ToString();
            if (max >= 2 && _bancoM2Cb .SelectedItem != null) f["ESPESSURA"]   = _bancoM2Cb.SelectedItem.ToString();
            if (max >= 3 && _bancoM3Cb .SelectedItem != null) f["COMPRIMENTO"] = _bancoM3Cb.SelectedItem.ToString();
            if (max >= 4 && _bancoM4Cb .SelectedItem != null) f["M4"]          = _bancoM4Cb.SelectedItem.ToString();
            return f;
        }

        private void FiltrarBancoDados()
        {
            DataListGrid.Rows.Clear();
            _salvarBt.Enabled = false;
            if (_bancoCatCb.SelectedItem == null || _bancoM1Cb.SelectedItem == null) return;

            var dt = sqlCommand.ObterTabela("MATERIAIS");
            if (dt == null) return;

            var filtros = BuildBancoFiltros();
            var query   = dt.AsEnumerable();

            foreach (var kv in filtros)
            {
                string key = kv.Key, fval = kv.Value.ToString();
                query = query.Where(r =>
                {
                    string rval = r[key]?.ToString().Trim() ?? "";
                    if (decimal.TryParse(rval.Replace(",", "."), System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out decimal rv) &&
                        decimal.TryParse(fval.Replace(",", "."), System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out decimal fv))
                        return rv == fv;
                    return string.Equals(rval, fval, StringComparison.OrdinalIgnoreCase);
                });
            }

            var cols = new[] { "CATEGORIA","DIAMETRO","ESPESSURA","COMPRIMENTO","M4","COD1","COD2","COD3","CAD_POR","DATE_PROJ" };
            int cnt = 1;
            foreach (var row in query)
            {
                var cell = new List<object> { cnt++ };
                cell.AddRange(cols.Select(c => (object)(row[c]?.ToString().Trim() ?? "")));
                DataListGrid.Rows.Add(cell.ToArray());
            }
            DestacarLinhasDuplicadas(DataListGrid);
        }

        private void LimparFiltrosBanco()
        {
            _bancoCatCb.SelectedIndex = -1;
            ClearCbs(_bancoM1Cb, _bancoM2Cb, _bancoM3Cb, _bancoM4Cb);
            DataListGrid.Rows.Clear();
            _salvarBt.Enabled = false;
        }

        private void PopulateDecimalCb(ComboBox cb, List<string> vals)
        {
            cb.Items.Clear();
            foreach (var v in vals.Where(v => !string.IsNullOrEmpty(v))
                                  .Distinct()
                                  .OrderBy(v => {
                                      double.TryParse(v.Replace(",", "."),
                                          System.Globalization.NumberStyles.Any,
                                          System.Globalization.CultureInfo.InvariantCulture, out double d);
                                      return d; }))
                cb.Items.Add(v);
        }

        private void ClearCbs(params ComboBox[] cbs)
        {
            foreach (var cb in cbs) { cb.Items.Clear(); cb.SelectedIndex = -1; }
        }

        private async void OnSalvarMaterial(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_editOrigCod1)) return;
            _salvarBt.Enabled = false;
            bool ok = await sqlCommand.UpdateMaterialFullAsync(
                _editOrigCod1, _editOrigCod2, _editOrigCod3,
                FCod1_txt.Text.Trim(), FCod2_txt.Text.Trim(), FCod3_txt.Text.Trim(),
                FCategoria_cb.Text.Trim(),
                FM1_txt.Text.Trim(), FM2_txt.Text.Trim(),
                FM3_txt.Text.Trim(), FM4_txt.Text.Trim());
            if (ok)
            {
                MessageBox.Show("Material atualizado com sucesso!", "OK",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                FiltrarBancoDados();
            }
            _salvarBt.Enabled = true;
        }

        // ── ABA: CATEGORIAS — Edit groupBox ──────────────────────────────────────

        private void InitializeEditCatSection()
        {
            var gb = new GroupBox
            {
                Text      = "Editar categoria",
                Location  = new Point(6, 298),
                Size      = new Size(720, 130),
                FlatStyle = FlatStyle.Popup
            };

            gb.Controls.Add(new Label { Text = "Selecionar categoria", Location = new Point(6, 22), AutoSize = true });
            _editCatCb = new ComboBox { Location = new Point(6, 38), Size = new Size(229, 21), DropDownStyle = ComboBoxStyle.DropDownList, Sorted = true };
            _editCatCb.SelectedIndexChanged += EditCatCb_SelectedIndexChanged;
            gb.Controls.Add(_editCatCb);

            gb.Controls.Add(new Label { Text = "Novo nome", Location = new Point(245, 22), AutoSize = true });
            _editCatNameTxt = new TextBox { Location = new Point(245, 38), Size = new Size(200, 20) };
            gb.Controls.Add(_editCatNameTxt);

            gb.Controls.Add(new Label { Text = "Novo tipo", Location = new Point(6, 68), AutoSize = true });
            _editType1 = new RadioButton { Text = "Tipo 1", Location = new Point(6,   85), AutoSize = true, Checked = true };
            _editType2 = new RadioButton { Text = "Tipo 2", Location = new Point(65,  85), AutoSize = true };
            _editType3 = new RadioButton { Text = "Tipo 3", Location = new Point(124, 85), AutoSize = true };
            _editType4 = new RadioButton { Text = "Tipo 4", Location = new Point(183, 85), AutoSize = true };
            _editType1.CheckedChanged += (s, ev) => { if (_editType1.Checked) _editTipo = "1"; };
            _editType2.CheckedChanged += (s, ev) => { if (_editType2.Checked) _editTipo = "2"; };
            _editType3.CheckedChanged += (s, ev) => { if (_editType3.Checked) _editTipo = "3"; };
            _editType4.CheckedChanged += (s, ev) => { if (_editType4.Checked) _editTipo = "4"; };
            gb.Controls.AddRange(new Control[] { _editType1, _editType2, _editType3, _editType4 });

            _alterTypeBt = new Button { Name = "altertype", Text = "Alterar Categoria", Location = new Point(453, 35), Size = new Size(130, 23) };
            _alterTypeBt.Click += OnAlterarCategoria;
            gb.Controls.Add(_alterTypeBt);

            tabPage5.Controls.Add(gb);
        }

        private void PopulateEditCatCb()
        {
            _editCatCb.Items.Clear();
            foreach (var cat in sqlCommand.GetValColumn("MATERIAL", "CATEGORIAS"))
                _editCatCb.Items.Add(cat.Trim());
        }

        private void EditCatCb_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_editCatCb.SelectedItem == null) return;
            string cat = _editCatCb.SelectedItem.ToString();
            _editCatNameTxt.Text = cat;
            var vals = sqlCommand.GetRowValues(
                new Dictionary<string, object> { { "MATERIAL", cat } },
                new List<string> { "TIPO" }, "CATEGORIAS");
            if (vals.Count > 0)
            {
                switch (vals[0]) {
                    case "1": _editType1.Checked = true; break;
                    case "2": _editType2.Checked = true; break;
                    case "3": _editType3.Checked = true; break;
                    case "4": _editType4.Checked = true; break;
                }
            }
        }

        private async void OnAlterarCategoria(object sender, EventArgs e)
        {
            if (_editCatCb.SelectedItem == null || string.IsNullOrWhiteSpace(_editCatNameTxt.Text))
            {
                MessageBox.Show("Selecione uma categoria e informe o novo nome.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string atual = _editCatCb.SelectedItem.ToString();
            string novo  = _editCatNameTxt.Text.Trim().ToUpper();
            bool ok = await sqlCommand.UpdateCategoriaAsync(atual, novo, _editTipo);
            if (ok)
            {
                MessageBox.Show($"Categoria '{atual}' → '{novo}' (Tipo {_editTipo}).", "OK",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                AtualizarListaCategorias();
                PopulateEditCatCb();
            }
        }

        // ── ABA: MEDIDAS — CRUD ───────────────────────────────────────────────────

        private void InitializeMedidasTab()
        {
            _tabMedidas = new TabPage { Text = "Medidas", Name = "tabMedidas" };
            tabControl1.Controls.Add(_tabMedidas);

            _medidasGrid = new DataGridView
            {
                Location = new Point(3, 3), Size = new Size(726, 430),
                AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false
            };
            var medCols = new[] {
                new[] {"medId","Id","40"}, new[] {"medCat","Categoria","200"},
                new[] {"medM1","M1","70"}, new[] {"medM2","M2","70"},
                new[] {"medM3","M3","70"}, new[] {"medM4","M4","70"} };
            foreach (var col in medCols)
                _medidasGrid.Columns.Add(new DataGridViewTextBoxColumn
                    { Name = col[0], HeaderText = col[1], Width = int.Parse(col[2]) });
            _medidasGrid.SelectionChanged += MedidasGrid_SelectionChanged;
            _tabMedidas.Controls.Add(_medidasGrid);

            int y = 440;
            _tabMedidas.Controls.Add(new Label { Text = "Categoria", Location = new Point(3, y), AutoSize = true });
            _medCatCb = new ComboBox { Location = new Point(3, y+14), Size = new Size(200, 21), DropDownStyle = ComboBoxStyle.DropDownList, Sorted = true };
            _tabMedidas.Controls.Add(_medCatCb);

            int mx = 210;
            int[] medOffsets = { 0, 80, 160, 240 };
            string[] medLbls = { "M1", "M2", "M3", "M4" };
            for (int i = 0; i < 4; i++)
                _tabMedidas.Controls.Add(new Label { Text = medLbls[i], Location = new Point(mx + medOffsets[i], y), AutoSize = true });

            _medM1Txt = new TextBox { Location = new Point(mx,      y+14), Size = new Size(70,20) };
            _medM2Txt = new TextBox { Location = new Point(mx+80,   y+14), Size = new Size(70,20) };
            _medM3Txt = new TextBox { Location = new Point(mx+160,  y+14), Size = new Size(70,20) };
            _medM4Txt = new TextBox { Location = new Point(mx+240,  y+14), Size = new Size(70,20) };
            _tabMedidas.Controls.AddRange(new Control[] { _medM1Txt, _medM2Txt, _medM3Txt, _medM4Txt });

            _medAddBt  = new Button { Text="Adicionar", Location=new Point(480, y+10), Size=new Size(80,23) };
            _medEditBt = new Button { Text="Alterar",   Location=new Point(565, y+10), Size=new Size(80,23), Enabled=false };
            _medDelBt  = new Button { Text="Excluir",   Location=new Point(650, y+10), Size=new Size(70,23), Enabled=false };
            _medAddBt .Click += OnMedAdicionarAsync;
            _medEditBt.Click += OnMedAlterarAsync;
            _medDelBt .Click += OnMedExcluirAsync;
            _tabMedidas.Controls.AddRange(new Control[] { _medAddBt, _medEditBt, _medDelBt });
        }

        private void CarregarMedidasGrid()
        {
            if (_medCatCb.Items.Count == 0)
                foreach (var cat in sqlCommand.GetValColumn("MATERIAL", "CATEGORIAS"))
                    _medCatCb.Items.Add(cat.Trim());

            _medidasGrid.Rows.Clear();
            var dt = sqlCommand.ObterTabela("MEDIDAS");
            if (dt == null) return;
            foreach (DataRow row in dt.Rows)
                _medidasGrid.Rows.Add(row["Id"], row["CATEGORIA"]?.ToString()?.Trim(),
                    row["M1"], row["M2"], row["M3"], row["M4"]);
        }

        private void MedidasGrid_SelectionChanged(object sender, EventArgs e)
        {
            if (_medidasGrid.SelectedRows.Count == 0)
                { _medEditBt.Enabled = false; _medDelBt.Enabled = false; return; }
            var r = _medidasGrid.SelectedRows[0];
            int.TryParse(r.Cells["medId"].Value?.ToString(), out _selectedMedidaId);
            _medCatCb.Text = r.Cells["medCat"].Value?.ToString() ?? "";
            _medM1Txt.Text = r.Cells["medM1"].Value?.ToString() ?? "";
            _medM2Txt.Text = r.Cells["medM2"].Value?.ToString() ?? "";
            _medM3Txt.Text = r.Cells["medM3"].Value?.ToString() ?? "";
            _medM4Txt.Text = r.Cells["medM4"].Value?.ToString() ?? "";
            _medEditBt.Enabled = _medDelBt.Enabled = true;
        }

        private async void OnMedAdicionarAsync(object sender, EventArgs e)
        {
            if (_medCatCb.SelectedItem == null) { MessageBox.Show("Selecione uma categoria.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (await sqlCommand.InsertMedidaAsync(_medCatCb.SelectedItem.ToString(),
                    _medM1Txt.Text, _medM2Txt.Text, _medM3Txt.Text, _medM4Txt.Text))
                CarregarMedidasGrid();
        }

        private async void OnMedAlterarAsync(object sender, EventArgs e)
        {
            if (_selectedMedidaId < 0 || _medCatCb.SelectedItem == null) return;
            if (await sqlCommand.UpdateMedidaAsync(_selectedMedidaId, _medCatCb.SelectedItem.ToString(),
                    _medM1Txt.Text, _medM2Txt.Text, _medM3Txt.Text, _medM4Txt.Text))
                CarregarMedidasGrid();
        }

        private async void OnMedExcluirAsync(object sender, EventArgs e)
        {
            if (_selectedMedidaId < 0) return;
            if (MessageBox.Show("Excluir esta medida?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                if (await sqlCommand.DeleteMedidaAsync(_selectedMedidaId))
                    { _selectedMedidaId = -1; CarregarMedidasGrid(); }
        }

        private void AddMigrationButton()
        {
            var btn = new System.Windows.Forms.Button
            {
                Text      = "Migrar Banco",
                Size      = new System.Drawing.Size(110, 26),
                Anchor    = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right,
                FlatStyle = System.Windows.Forms.FlatStyle.Flat,
                BackColor = System.Drawing.Color.FromArgb(240, 240, 240),
                Cursor    = System.Windows.Forms.Cursors.Hand,
                TabStop   = false
            };
            btn.Location = new System.Drawing.Point(
                this.ClientSize.Width  - btn.Width  - 8,
                this.ClientSize.Height - btn.Height - 8);
            btn.Click += (s, e) => new MigrationForm().Show();
            this.Controls.Add(btn);
            btn.BringToFront();
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

        private async void Cadastrar_bt_Click(object sender, EventArgs e)
        {
            if (PdmEvents.Connect() && RowMask(dataGridView1) && DestacarLinhasDuplicadas(dataGridView1))
            {
                Cadastrar_bt.Enabled = false;
                if (await sqlCommand.CadastrarItensDataGridAsync(dataGridView1, User_PDM_lb.Text))
                {
                    sqlCommand.CarregarDadosIniciais();
                    dataGridView1.Rows.Clear();
                    MessageBox.Show("Itens cadastrados com sucesso!", "Operação Finalizada", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Não foi possível cadastrar os itens.", "Operação Finalizada", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                Cadastrar_bt.Enabled = true;
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
            var grid = (DataGridView)sender;
            PreencherTextBoxComLinhaSelecionada(grid);

            // Armazena os códigos originais para a operação de Salvar Alteração
            if (grid == DataListGrid && grid.SelectedRows.Count > 0)
            {
                var row = grid.SelectedRows[0];
                _editOrigCod1     = row.Cells[6].Value?.ToString() ?? "";
                _editOrigCod2     = row.Cells[7].Value?.ToString() ?? "";
                _editOrigCod3     = row.Cells[8].Value?.ToString() ?? "";
                if (_salvarBt != null) _salvarBt.Enabled = true;
            }
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

        private async void ExcluiDUP_bt_Click(object sender, EventArgs e)
        {
            // Nomes reais das colunas no PDB_BANCO_V2
            List<string> colunas = new List<string> { "CATEGORIA_ID", "M1", "M2", "M3", "M4", "COD1", "COD2", "COD3" };
            await sqlCommand.RemoverDuplicatasAsync("MATERIAIS", "Id", colunas);
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

        private async void Add2_bt_Click(object sender, EventArgs e)
        {
            AddLine(dataGridView1);
            await PreencherDataGrid(dataGridView1, textBox1);
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
                if (row.IsNewRow) continue;

                bool err = false;
                string cod = $"{row.Cells[6]?.Value}.{row.Cells[7]?.Value}.{row.Cells[8]?.Value}";

                if (!PartValidator.ValidateCOD(cod, items))
                {
                    row.Cells[1].Value = row.Cells[1].Value?.ToString().Trim();
                    if (PartValidator.ValidateCat(row.Cells[1].Value?.ToString().Trim(), categorias))
                    {
                        row.DefaultCellStyle.BackColor = Color.Red; err = true; error = false;
                    }

                    for (int i = 2; i <= 5; i++)
                    {
                        if (!PartValidator.FormatDecimal(row.Cells[i].Value, out string result))
                            row.Cells[i].Value = result;
                        else { row.DefaultCellStyle.BackColor = Color.Red; err = true; error = false; }
                    }

                    for (int i = 6; i <= 8; i++)
                    {
                        int v = (i == 8) ? 4 : 3;
                        if (!PartValidator.FormatInteger(row.Cells[i].Value, v, out string result))
                            row.Cells[i].Value = result;
                        else { row.DefaultCellStyle.BackColor = Color.Red; err = true; error = false; }
                    }
                }
                else { row.DefaultCellStyle.BackColor = Color.Red; err = true; error = false; }

                if (!err) row.DefaultCellStyle.BackColor = Color.White;
            }
            return error;
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

        private async void CadCat_bt_Click(object sender, EventArgs e)
        {
            if (NewCatName_txt.Text != "")
            {
                await sqlCommand.InsertCategoriaAsync(NewCatName_txt.Text.ToUpper(), tipoSelecionado);
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

        private async void ExcluiCat_bt_Click(object sender, EventArgs e)
        {
            if (Allitems_ch.Checked)
            {
                DialogResult confirm = MessageBox.Show(
                    $"Tem certeza que deseja excluir todos os materiais da categoria '{ExcCategoria_cb.Text}'?",
                    "Confirmação", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (confirm == DialogResult.Yes)
                {
                    await sqlCommand.ExcluirMateriaisPorCategoriaAsync(ExcCategoria_cb.Text);
                    await sqlCommand.DeleteItemAsync("CATEGORIAS", ExcCategoria_cb.Text, "MATERIAL");
                    AtualizarListaCategorias();
                }
            }
            else
            {
                DialogResult confirm = MessageBox.Show("Deseja excluir a categoria selecionada?", "",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (confirm == DialogResult.Yes && ExcCategoria_cb.Text != "")
                {
                    await sqlCommand.DeleteItemAsync("CATEGORIAS", ExcCategoria_cb.Text, "MATERIAL");
                    AtualizarListaCategorias();
                }
            }
        }

        private void AtualizarListaCategorias()
        {
            Categoria_CBox.Items.Clear();
            ExcCategoria_cb.Text = "";
            ExcCategoria_cb.Items.Clear();
            foreach (string categoria in sqlCommand.GetValColumn("MATERIAL", "CATEGORIAS"))
            {
                Categoria_CBox.Items.Add(categoria.Trim());
                ExcCategoria_cb.Items.Add(categoria.Trim());
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
