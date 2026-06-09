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

        // Estado de runtime (não controles UI)
        private string _editOrigCod1, _editOrigCod2, _editOrigCod3;
        private int    _selectedMedidaId = -1;
        private string _editTipo = "1";
        private string _userPermissao = "leitor";

        public Cadastro_main(SldWorks app, SQLCommands sql_comm)
        {
            InitializeComponent();
            swApp = app;
            PdmEvents = new EPDMHandler();
            sqlCommand = sql_comm;
            //AddMigrationButton();
            WireUpEvents();
        }

        // ─────────────────────────────────────────────────────────────────────────
        // WIRE UP — conecta eventos aos controles declarados no Designer
        // ─────────────────────────────────────────────────────────────────────────

        private void WireUpEvents()
        {
            // Formato decimal para os comboboxes cascade
            BancoCat_cb.Format += OnDecimalCbFormat;
            BancoM1_cb .Format += OnDecimalCbFormat;
            BancoM2_cb .Format += OnDecimalCbFormat;
            BancoM3_cb .Format += OnDecimalCbFormat;
            BancoM4_cb .Format += OnDecimalCbFormat;

            // Cascade - Banco de Dados
            BancoCat_cb.SelectedIndexChanged += OnBancoCatChanged;
            BancoM1_cb .SelectedIndexChanged += OnBancoM1Changed;
            BancoM2_cb .SelectedIndexChanged += OnBancoM2Changed;
            BancoM3_cb .SelectedIndexChanged += OnBancoM3Changed;
            BancoM4_cb .SelectedIndexChanged += OnBancoM4Changed;
            BancoLimpar_bt.Click += (s, e) => LimparFiltrosBanco();
            BancoSalvar_bt.Click += OnSalvarMaterial;

            // Edit Categoria — nomes conforme o Designer
            EditCat_cb.SelectedIndexChanged += EditCatCb_SelectedIndexChanged;
            Altertype1_chk.CheckedChanged += (s, e) => { if (Altertype1_chk.Checked) { _editTipo = "1"; SelecTipoEdit("1"); } };
            Altertype2_chk.CheckedChanged += (s, e) => { if (Altertype2_chk.Checked) { _editTipo = "2"; SelecTipoEdit("2"); } };
            Altertype3_chk.CheckedChanged += (s, e) => { if (Altertype3_chk.Checked) { _editTipo = "3"; SelecTipoEdit("3"); } };
            Altertype4_chk.CheckedChanged += (s, e) => { if (Altertype4_chk.Checked) { _editTipo = "4"; SelecTipoEdit("4"); } };
            altertype.Click += OnAlterarCategoria;

            // Medidas
            MedidasGrid.SelectionChanged += MedidasGrid_SelectionChanged;
            MedAdd_bt .Click += OnMedAdicionarAsync;
            MedEdit_bt.Click += OnMedAlterarAsync;
            MedDel_bt .Click += OnMedExcluirAsync;

            // Incremento
            incremento_check.CheckedChanged += incremento_check_CheckedChanged;
            gerarSeq_bt.Click += GerarSequencia_bt_Click;

            // Usuários
            UsersGrid.SelectionChanged += UsersGrid_SelectionChanged;
            UserAdd_bt .Click += OnUserAdicionarAsync;
            UserEdit_bt.Click += OnUserAlterarAsync;
            UserDel_bt .Click += OnUserExcluirAsync;

            // Lazy load ao trocar de aba
            tabControl1.SelectedIndexChanged += TabControl1_SelectedIndexChanged;
        }

        private void TabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var tab = tabControl1.SelectedTab;
            if      (tab == tabPage2)  PopulateBancoCascade();
            else if (tab == tabPage5)  PopulateEditCatCb();
            else if (tab == tabPage6)  CarregarMedidasGrid();
            else if (tab == tabPage7)  CarregarUsersGrid();
        }

        // ─────────────────────────────────────────────────────────────────────────
        // ABA: BANCO DE DADOS — cascade filter + salvar
        // ─────────────────────────────────────────────────────────────────────────

        private void OnDecimalCbFormat(object sender, ListControlConvertEventArgs e)
        {
            if (e.Value is string v && decimal.TryParse(v.Replace(",", "."),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal d))
                e.Value = d.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        }

        private void PopulateBancoCascade()
        {
            // Sempre recarrega do cache (já atualizado após qualquer mutação)
            BancoCat_cb.Items.Clear();
            foreach (var cat in sqlCommand.GetValColumn("CATEGORIA", "MATERIAIS"))
                BancoCat_cb.Items.Add(cat);

            FCategoria_cb.Items.Clear();
            foreach (var cat in sqlCommand.GetValColumn("MATERIAL", "CATEGORIAS"))
                FCategoria_cb.Items.Add(cat.Trim());

            // Limpa os filtros em cascata para evitar exibir dados stale
            ClearCbs(BancoM1_cb, BancoM2_cb, BancoM3_cb, BancoM4_cb);
            DataListGrid.Rows.Clear();
            BancoSalvar_bt.Enabled = false;
        }

        private void OnBancoCatChanged(object sender, EventArgs e)
        {
            ClearCbs(BancoM1_cb, BancoM2_cb, BancoM3_cb, BancoM4_cb);
            DataListGrid.Rows.Clear();
            BancoSalvar_bt.Enabled = false;
            if (BancoCat_cb.SelectedItem == null) return;
            PopulateDecimalCb(BancoM1_cb,
                sqlCommand.GetRowValues(
                    new Dictionary<string, object> { { "CATEGORIA", BancoCat_cb.SelectedItem.ToString() } },
                    new List<string> { "DIAMETRO" }, "MATERIAIS"));
        }

        private void OnBancoM1Changed(object sender, EventArgs e)
        {
            ClearCbs(BancoM2_cb, BancoM3_cb, BancoM4_cb);
            FiltrarBancoDados();
            if (BancoM1_cb.SelectedItem == null) return;
            PopulateDecimalCb(BancoM2_cb,
                sqlCommand.GetRowValues(BuildBancoFiltros(1), new List<string> { "ESPESSURA" }, "MATERIAIS"));
        }

        private void OnBancoM2Changed(object sender, EventArgs e)
        {
            ClearCbs(BancoM3_cb, BancoM4_cb);
            FiltrarBancoDados();
            if (BancoM2_cb.SelectedItem == null) return;
            PopulateDecimalCb(BancoM3_cb,
                sqlCommand.GetRowValues(BuildBancoFiltros(2), new List<string> { "COMPRIMENTO" }, "MATERIAIS"));
        }

        private void OnBancoM3Changed(object sender, EventArgs e)
        {
            ClearCbs(BancoM4_cb);
            FiltrarBancoDados();
            if (BancoM3_cb.SelectedItem == null) return;
            PopulateDecimalCb(BancoM4_cb,
                sqlCommand.GetRowValues(BuildBancoFiltros(3), new List<string> { "M4" }, "MATERIAIS"));
        }

        private void OnBancoM4Changed(object sender, EventArgs e) => FiltrarBancoDados();

        private Dictionary<string, object> BuildBancoFiltros(int max = 4)
        {
            var f = new Dictionary<string, object>();
            if (max >= 0 && BancoCat_cb.SelectedItem != null) f["CATEGORIA"]   = BancoCat_cb.SelectedItem.ToString();
            if (max >= 1 && BancoM1_cb .SelectedItem != null) f["DIAMETRO"]    = BancoM1_cb.SelectedItem.ToString();
            if (max >= 2 && BancoM2_cb .SelectedItem != null) f["ESPESSURA"]   = BancoM2_cb.SelectedItem.ToString();
            if (max >= 3 && BancoM3_cb .SelectedItem != null) f["COMPRIMENTO"] = BancoM3_cb.SelectedItem.ToString();
            if (max >= 4 && BancoM4_cb .SelectedItem != null) f["M4"]          = BancoM4_cb.SelectedItem.ToString();
            return f;
        }

        private void FiltrarBancoDados()
        {
            DataListGrid.Rows.Clear();
            BancoSalvar_bt.Enabled = false;
            if (BancoCat_cb.SelectedItem == null || BancoM1_cb.SelectedItem == null) return;

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
            BancoCat_cb.SelectedIndex = -1;
            ClearCbs(BancoM1_cb, BancoM2_cb, BancoM3_cb, BancoM4_cb);
            DataListGrid.Rows.Clear();
            BancoSalvar_bt.Enabled = false;
        }

        private static double ToSortDouble(string v)
        {
            double d;
            double.TryParse((v ?? "").Replace(",", "."),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out d);
            return d;
        }

        private void PopulateDecimalCb(ComboBox cb, List<string> vals)
        {
            cb.Items.Clear();
            var items = vals.Where(v => !string.IsNullOrEmpty(v)).Distinct().ToList();
            items.Sort((a, b) => ToSortDouble(a).CompareTo(ToSortDouble(b)));
            foreach (var v in items)
                cb.Items.Add(v);
        }

        private void ClearCbs(params ComboBox[] cbs)
        {
            foreach (var cb in cbs) { cb.Items.Clear(); cb.SelectedIndex = -1; }
        }

        private async void OnSalvarMaterial(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_editOrigCod1)) return;
            BancoSalvar_bt.Enabled = false;
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
            BancoSalvar_bt.Enabled = true;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // ABA: CATEGORIAS — CRUD completo
        // ─────────────────────────────────────────────────────────────────────────

        private void PopulateEditCatCb()
        {
            EditCat_cb.Items.Clear();
            foreach (var cat in sqlCommand.GetValColumn("MATERIAL", "CATEGORIAS"))
                EditCat_cb.Items.Add(cat.Trim());
        }

        // Atualiza label19 com a descrição do tipo selecionado (como ConcatString_lb no Create)
        private void SelecTipoEdit(string tipo)
        {
            var vals = sqlCommand.GetRowValues(
                new Dictionary<string, object> { { "TIPO", tipo } },
                new List<string> { "M1", "M2", "M3", "M4" }, "TIPOS");
            if (vals.Count >= 3)
                label19.Text = $"{vals[0]}; {vals[1]}; {vals[2]}; {vals[3]}";
        }

        private void EditCatCb_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (EditCat_cb.SelectedItem == null) return;
            string cat = EditCat_cb.SelectedItem.ToString();
            EditCatName_txt.Text = cat;
            var vals = sqlCommand.GetRowValues(
                new Dictionary<string, object> { { "MATERIAL", cat } },
                new List<string> { "TIPO" }, "CATEGORIAS");
            if (vals.Count > 0)
            {
                switch (vals[0])
                {
                    case "1": Altertype1_chk.Checked = true; break;
                    case "2": Altertype2_chk.Checked = true; break;
                    case "3": Altertype3_chk.Checked = true; break;
                    case "4": Altertype4_chk.Checked = true; break;
                }
            }
        }

        private async void OnAlterarCategoria(object sender, EventArgs e)
        {
            if (EditCat_cb.SelectedItem == null || string.IsNullOrWhiteSpace(EditCatName_txt.Text))
            {
                MessageBox.Show("Selecione uma categoria e informe o novo nome.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string atual = EditCat_cb.SelectedItem.ToString();
            string novo  = EditCatName_txt.Text.Trim().ToUpper();
            bool ok = await sqlCommand.UpdateCategoriaAsync(atual, novo, _editTipo);
            if (ok)
            {
                MessageBox.Show($"Categoria '{atual}' → '{novo}' (Tipo {_editTipo}).", "OK",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                AtualizarListaCategorias();
                PopulateEditCatCb();
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // ABA: MEDIDAS — CRUD
        // ─────────────────────────────────────────────────────────────────────────

        private void CarregarMedidasGrid()
        {
            if (MedCat_cb.Items.Count == 0)
                foreach (var cat in sqlCommand.GetValColumn("MATERIAL", "CATEGORIAS"))
                    MedCat_cb.Items.Add(cat.Trim());

            MedidasGrid.Rows.Clear();
            var dt = sqlCommand.ObterTabela("MEDIDAS");
            if (dt == null) return;
            foreach (DataRow row in dt.Rows)
                MedidasGrid.Rows.Add(row["Id"], row["CATEGORIA"]?.ToString()?.Trim(),
                    row["M1"], row["M2"], row["M3"], row["M4"]);
        }

        private void MedidasGrid_SelectionChanged(object sender, EventArgs e)
        {
            if (MedidasGrid.SelectedRows.Count == 0)
                { MedEdit_bt.Enabled = false; MedDel_bt.Enabled = false; return; }
            var r = MedidasGrid.SelectedRows[0];
            int.TryParse(r.Cells["medColId"].Value?.ToString(), out _selectedMedidaId);

            // DropDownList exige SelectedIndex — Text= não funciona para selecionar
            string catName = r.Cells["medColCat"].Value?.ToString() ?? "";
            int idx = MedCat_cb.Items.IndexOf(catName);
            if (idx >= 0) MedCat_cb.SelectedIndex = idx;

            MedM1_txt.Text = r.Cells["medColM1"].Value?.ToString() ?? "";
            MedM2_txt.Text = r.Cells["medColM2"].Value?.ToString() ?? "";
            MedM3_txt.Text = r.Cells["medColM3"].Value?.ToString() ?? "";
            MedM4_txt.Text = r.Cells["medColM4"].Value?.ToString() ?? "";
            MedEdit_bt.Enabled = MedDel_bt.Enabled = true;
        }

        private async void OnMedAdicionarAsync(object sender, EventArgs e)
        {
            if (MedCat_cb.SelectedItem == null)
            {
                MessageBox.Show("Selecione uma categoria.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (await sqlCommand.InsertMedidaAsync(MedCat_cb.SelectedItem.ToString(),
                    MedM1_txt.Text, MedM2_txt.Text, MedM3_txt.Text, MedM4_txt.Text))
                CarregarMedidasGrid();
        }

        private async void OnMedAlterarAsync(object sender, EventArgs e)
        {
            if (_selectedMedidaId < 0 || MedCat_cb.SelectedItem == null) return;
            if (await sqlCommand.UpdateMedidaAsync(_selectedMedidaId, MedCat_cb.SelectedItem.ToString(),
                    MedM1_txt.Text, MedM2_txt.Text, MedM3_txt.Text, MedM4_txt.Text))
                CarregarMedidasGrid();
        }

        private async void OnMedExcluirAsync(object sender, EventArgs e)
        {
            if (_selectedMedidaId < 0) return;
            if (MessageBox.Show("Excluir esta medida?", "Confirmar",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                if (await sqlCommand.DeleteMedidaAsync(_selectedMedidaId))
                    { _selectedMedidaId = -1; CarregarMedidasGrid(); }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // CONTROLES EXISTENTES
        // ─────────────────────────────────────────────────────────────────────────

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
            data_lb.Text = DateTime.Now.ToString("dd/MM/yyyy");

            if (PdmEvents.Connect())
            {
                string idpdm = PdmEvents.GetUser();
                User_PDM_lb.Text = idpdm;
                _userPermissao = sqlCommand.GetUserPermissao(idpdm);
            }
            else
            {
                // PDM indisponível — permite acesso como usuario padrão
                _userPermissao = "admin";
            }
            if (_userPermissao == "leitor")
            {
                MessageBox.Show("Você não tem permissão para acessar o cadastro.",
                    "Acesso Negado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                // BeginInvoke adia o Close para depois do Load terminar,
                // evitando fechar a aplicação inteira durante o evento de carga
                this.BeginInvoke(new Action(() => this.Close()));
            }
            else
            {
                List<string> categorias = sqlCommand.GetValColumn("MATERIAL", "CATEGORIAS");
                foreach (string categoria in categorias)
                {
                    Categoria_CBox.Items.Add(categoria.Trim());
                    ExcCategoria_cb.Items.Add(categoria.Trim());
                }
                Cadastrar_bt.Enabled = false;
                manual_check.Checked = true;
                type1_chk.Checked = true;

                AplicarPermissoes();
            }



        }

        private void AplicarPermissoes()
        {
            bool isAdmin = _userPermissao == "admin";

            // Excluir duplicatas e excluir categoria/medida são funções exclusivas de admin
            ExcluiDUP_bt.Enabled = isAdmin;
            ExcluiCat_bt.Enabled = isAdmin;
            MedDel_bt.Enabled    = isAdmin;

            // Aba Usuários só visível para admin
            if (!isAdmin)
                tabControl1.TabPages.Remove(tabPage7);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // ABA: USUÁRIOS (admin only)
        // ─────────────────────────────────────────────────────────────────────────

        private int _selectedUserId = -1;

        private void CarregarUsersGrid()
        {
            UsersGrid.Rows.Clear();
            var dt = sqlCommand.ObterTabela("USERS");
            if (dt == null) return;
            foreach (System.Data.DataRow row in dt.Rows)
            {
                UsersGrid.Rows.Add(
                    row["Id"]?.ToString(),
                    row["IDPDM"]?.ToString(),
                    row["PERMISSAO"]?.ToString());
            }
        }

        private void UsersGrid_SelectionChanged(object sender, EventArgs e)
        {
            if (UsersGrid.SelectedRows.Count == 0) { _selectedUserId = -1; UserEdit_bt.Enabled = false; UserDel_bt.Enabled = false; return; }
            var row = UsersGrid.SelectedRows[0];
            int.TryParse(row.Cells["usrColId"].Value?.ToString(), out _selectedUserId);
            UserIdpdm_txt.Text = row.Cells["usrColIdpdm"].Value?.ToString() ?? "";
            string perm = row.Cells["usrColPerm"].Value?.ToString() ?? "usuario";
            int idx = UserPerm_cb.Items.IndexOf(perm);
            if (idx >= 0) UserPerm_cb.SelectedIndex = idx;
            UserEdit_bt.Enabled = true;
            UserDel_bt.Enabled  = true;
        }

        private async void OnUserAdicionarAsync(object sender, EventArgs e)
        {
            string idpdm = UserIdpdm_txt.Text.Trim();
            string perm  = UserPerm_cb.SelectedItem?.ToString() ?? "usuario";
            if (string.IsNullOrEmpty(idpdm)) { MessageBox.Show("Informe o usuário PDM.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (await sqlCommand.InsertUserAsync(idpdm, perm))
            {
                CarregarUsersGrid();
                UserIdpdm_txt.Clear();
            }
        }

        private async void OnUserAlterarAsync(object sender, EventArgs e)
        {
            if (_selectedUserId < 0) return;
            string perm = UserPerm_cb.SelectedItem?.ToString() ?? "usuario";
            if (await sqlCommand.UpdateUserPermissaoAsync(_selectedUserId, perm))
                CarregarUsersGrid();
        }

        private async void OnUserExcluirAsync(object sender, EventArgs e)
        {
            if (_selectedUserId < 0) return;
            string nome = UsersGrid.SelectedRows[0].Cells["usrColIdpdm"].Value?.ToString() ?? "";
            var confirm = MessageBox.Show($"Excluir o usuário '{nome}'?", "Confirmação",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;
            if (await sqlCommand.DeleteUserAsync(_selectedUserId))
            {
                _selectedUserId = -1;
                UserEdit_bt.Enabled = false;
                UserDel_bt.Enabled  = false;
                UserIdpdm_txt.Clear();
                CarregarUsersGrid();
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e) { }

        private async void Cadastrar_bt_Click(object sender, EventArgs e)
        {
            if (PdmEvents.Connect() && RowMask(dataGridView1) && DestacarLinhasDuplicadas(dataGridView1))
            {
                Cadastrar_bt.Enabled = false;
                if (await sqlCommand.CadastrarItensDataGridAsync(dataGridView1, User_PDM_lb.Text))
                {
                    // Cache de MATERIAIS já atualizado dentro de CadastrarItensDataGridAsync
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
            if (dataGridView.SelectedRows.Count > 0)
            {
                DataGridViewRow linha = dataGridView.SelectedRows[0];
                FCategoria_cb.Text  = linha.Cells[1].Value?.ToString() ?? "";
                FM1_txt.Text        = linha.Cells[2].Value?.ToString() ?? "";
                FM2_txt.Text        = linha.Cells[3].Value?.ToString() ?? "";
                FM3_txt.Text        = linha.Cells[4].Value?.ToString() ?? "";
                FM4_txt.Text        = linha.Cells[5].Value?.ToString() ?? "";
                FCod1_txt.Text      = linha.Cells[6].Value?.ToString() ?? "";
                FCod2_txt.Text      = linha.Cells[7].Value?.ToString() ?? "";
                FCod3_txt.Text      = linha.Cells[8].Value?.ToString() ?? "";
            }
        }

        private void dataGridView_SelectionChanged(object sender, EventArgs e)
        {
            var grid = (DataGridView)sender;
            PreencherTextBoxComLinhaSelecionada(grid);

            if (grid == DataListGrid && grid.SelectedRows.Count > 0)
            {
                var row = grid.SelectedRows[0];
                _editOrigCod1          = row.Cells[6].Value?.ToString() ?? "";
                _editOrigCod2          = row.Cells[7].Value?.ToString() ?? "";
                _editOrigCod3          = row.Cells[8].Value?.ToString() ?? "";
                BancoSalvar_bt.Enabled = true;
            }
        }


        private async void ExcluiDUP_bt_Click(object sender, EventArgs e)
        {
            List<string> colunas = new List<string> { "CATEGORIA_ID", "M1", "M2", "M3", "M4", "COD1", "COD2", "COD3" };
            await sqlCommand.RemoverDuplicatasAsync("MATERIAIS", "Id", colunas);
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
                    (dataGridView.RowCount+1).ToString(), Categoria_CBox.Text,
                    m1_txt?.Text, m2_txt?.Text, m3_txt?.Text, "0",
                    Name1_txt?.Text, Name2_txt?.Text, Name3_txt?.Text };
            }
            else
            {
                valores = new string[] {
                    (dataGridView.RowCount+1).ToString(), Categoria_CBox.Text,
                    m1_txt?.Text, m2_txt?.Text, m3_txt?.Text, m4_txt?.Text,
                    Name1_txt?.Text, Name2_txt?.Text, Name3_txt?.Text };
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
                    dataGridView.Rows.Add(valoresComLinha);
            }
        }

        private bool DestacarLinhasDuplicadas(DataGridView dataGridView)
        {
            Dictionary<string, List<int>> linhas = new Dictionary<string, List<int>>();
            bool error = true;
            for (int i = 0; i < dataGridView.Rows.Count; i++)
            {
                string linha = string.Join("|", dataGridView.Rows[i].Cells.Cast<DataGridViewCell>()
                    .Skip(1).Select(c => c.Value?.ToString() ?? "").ToArray());
                if (!linhas.ContainsKey(linha)) linhas[linha] = new List<int>();
                linhas[linha].Add(i);
            }
            foreach (var item in linhas)
            {
                List<int> indices = item.Value;
                if (indices.Count > 1)
                {
                    dataGridView.Rows[indices[0]].DefaultCellStyle.BackColor = Color.Yellow;
                    for (int j = 1; j < indices.Count; j++)
                        dataGridView.Rows[indices[j]].DefaultCellStyle.BackColor = Color.Red;
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
                    { row.DefaultCellStyle.BackColor = Color.Red; err = true; error = false; }

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
            Dictionary<string, object> filtros = new Dictionary<string, object> { { "MATERIAL", material } };
            List<string> val = sqlCommand.GetRowValues(filtros, colunasDesejadas, "CATEGORIAS");
            if (val.Count > 0)
            {
                filtros = new Dictionary<string, object> { { "TIPO", val[0].ToString() } };
                colunasDesejadas = new List<string> { "M1", "M2", "M3", "M4" };
                val = sqlCommand.GetRowValues(filtros, colunasDesejadas, "TIPOS");
                if (val.Count >= 4)
                {
                    LB1.Text = val[0]; LB2.Text = val[1]; LB3.Text = val[2]; LB4.Text = val[3];
                    bool hasM4 = val[3] != "-";
                    m4_txt.Visible = hasM4; LB4.Visible = hasM4;
                    sk4_txt.Visible = hasM4; label6.Visible = hasM4;
                }
            }
        }

        private string ObterMedida(string nomeDimensao, bool ultimo = false)
        {
            ModelDoc2 modelo = ObterModeloAtivo();
            bool isAngulo = LB4.Text == "Ângulo" && ultimo;
            if (modelo != null)
            {
                Dimension dim = (Dimension)modelo.Parameter(nomeDimensao);
                if (dim != null)
                {
                    double valor = dim.SystemValue;
                    if (isAngulo) return (valor * (180 / Math.PI)).ToString("0.##");
                    return (valor * 1000).ToString("0.###");
                }
            }
            return "0.0";
        }

        private ModelDoc2 ObterModeloAtivo()
        {
            if (swApp == null)
                swApp = (SldWorks)Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application"));

            swModel = (ModelDoc2)swApp.ActiveDoc;
            if (swModel != null)
            {
                string name = sw_GetNameFile(swModel);
                string[] partes = name.Split('.');
                GerarHashArquivo(swModel.GetPathName());
                if (partes.Length == 3)
                {
                    Name1_txt.Text = partes[0];
                    Name2_txt.Text = partes[1];
                    Name3_txt.Text = partes[2];
                }
            }
            else { Name1_txt.Clear(); Name2_txt.Clear(); Name3_txt.Clear(); }
            return swModel;
        }

        private string GerarHashArquivo(string caminhoArquivo)
        {
            if (File.Exists(caminhoArquivo))
            {
                using (var md5 = MD5.Create())
                using (var stream = File.OpenRead(caminhoArquivo))
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
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
            if (automatico_check.Checked)
            { sk1_txt.Clear(); sk2_txt.Clear(); sk3_txt.Clear(); sk4_txt.Clear(); coletarEsboco(); }
        }

        private void sk2_txt_TextChanged(object sender, EventArgs e) { }

        private void manual_check_CheckedChanged(object sender, EventArgs e)
        {
            AutomaticoPane_pn.Visible = false;
            IncrementoPane_pn.Visible = false;
        }

        private void automatico_check_CheckedChanged(object sender, EventArgs e)
        {
            AutomaticoPane_pn.Visible = true;
            IncrementoPane_pn.Visible = false;
        }

        private void incremento_check_CheckedChanged(object sender, EventArgs e)
        {
            AutomaticoPane_pn.Visible = false;
            IncrementoPane_pn.Visible = incremento_check.Checked;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // MODO INCREMENTO — gera sequência de linhas no DataGrid
        // ─────────────────────────────────────────────────────────────────────────

        private void GerarSequencia_bt_Click(object sender, EventArgs e)
        {
            string categoria = Categoria_CBox.Text.Trim();
            string m1 = m1_txt.Text.Trim();
            string m2 = m2_txt.Text.Trim();
            string m4 = m4_txt.Visible ? m4_txt.Text.Trim() : "0";
            string cod1 = Name1_txt.Text.Trim();
            string cod2 = Name2_txt.Text.Trim();

            if (!decimal.TryParse(m3_txt.Text.Replace(",", "."),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal m3Start))
            {
                MessageBox.Show("Valor de Medida 3 inválido.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(Name3_txt.Text.Trim(), out int cod3Start) ||
                !decimal.TryParse(incremento_txt.Text.Replace(",", "."),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal inc) ||
                inc <= 0 ||
                !decimal.TryParse(maxInc_txt.Text.Replace(",", "."),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal maxVal))
            {
                MessageBox.Show("Preencha Código 3 inicial, Incremento e Valor máximo corretamente.",
                    "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int added = 0;
            decimal cod3Current = cod3Start;
            decimal m3Current   = m3Start;

            while (cod3Current <= maxVal)
            {
                // COD3 formatado com 4 dígitos
                string cod3Str = ((int)cod3Current).ToString().PadLeft(4, '0');
                string m3Str   = m3Current.ToString("0.###",
                    System.Globalization.CultureInfo.InvariantCulture);

                dataGridView1.Rows.Add(
                    (dataGridView1.RowCount + 1).ToString(),
                    categoria, m1, m2, m3Str, m4, cod1, cod2, cod3Str);

                cod3Current += inc;
                m3Current   += inc;
                added++;

                if (added > 5000) break; // guarda contra loop infinito
            }

            if (RowMask(dataGridView1) && DestacarLinhasDuplicadas(dataGridView1))
                Cadastrar_bt.Enabled = true;
        }

        private void coletarEsboco()
        {
            string categoriaSelecionada = Categoria_CBox.SelectedItem.ToString();
            List<string> colunasDesejadas = new List<string> { "M1", "M2", "M3", "M4" };
            Dictionary<string, object> filtros = new Dictionary<string, object> { { "CATEGORIA", categoriaSelecionada } };
            List<string> valoresLinha = sqlCommand.GetRowValues(filtros, colunasDesejadas, "MEDIDAS");
            if (valoresLinha.Count > 0)
            {
                sk1_txt.Text = valoresLinha[0]; sk2_txt.Text = valoresLinha[1];
                sk3_txt.Text = valoresLinha[2]; sk4_txt.Text = valoresLinha[3];
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
                Categoria_CBox.Text = Categoria_CBox.Items[0].ToString();
        }

        private void sk1_txt_TextChanged(object sender, EventArgs e)   { m1_txt.Text = ObterMedida(sk1_txt.Text); }
        private void sk2_txt_TextChanged_1(object sender, EventArgs e) { m2_txt.Text = ObterMedida(sk2_txt.Text); }
        private void sk3_txt_TextChanged(object sender, EventArgs e)   { m3_txt.Text = ObterMedida(sk3_txt.Text); }
        private void sk4_txt_TextChanged(object sender, EventArgs e)   { m4_txt.Text = ObterMedida(sk4_txt.Text, true); }

        private void excluir_bt_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                if (!row.IsNewRow) dataGridView1.Rows.Remove(row);
            if (RowMask(dataGridView1) && DestacarLinhasDuplicadas(dataGridView1)) { Cadastrar_bt.Enabled = true; }
            else { Cadastrar_bt.Enabled = false; }
        }

        string tipoSelecionado = "1";
        private void type1_chk_CheckedChanged(object sender, EventArgs e) { SelecTipo2("1"); tipoSelecionado = "1"; }
        private void type2_chk_CheckedChanged(object sender, EventArgs e) { SelecTipo2("2"); tipoSelecionado = "2"; }
        private void type3_chk_CheckedChanged(object sender, EventArgs e) { SelecTipo2("3"); tipoSelecionado = "3"; }
        private void type4_chk_CheckedChanged(object sender, EventArgs e) { SelecTipo2("4"); tipoSelecionado = "4"; }

        private void SelecTipo2(string selecao)
        {
            Dictionary<string, object> filtros = new Dictionary<string, object> { { "TIPO", selecao } };
            List<string> val = sqlCommand.GetRowValues(filtros, new List<string> { "M1", "M2", "M3", "M4" }, "TIPOS");
            if (val.Count >= 3)
                ConcatString_lb.Text = $"{val[0]}; {val[1]}; {val[2]}; {val[3]}";
        }

        private async void CadCat_bt_Click(object sender, EventArgs e)
        {
            if (NewCatName_txt.Text != "")
            {
                await sqlCommand.InsertCategoriaAsync(NewCatName_txt.Text.ToUpper(), tipoSelecionado);
                NewCatName_txt.Clear();
            }
            // InsertCategoriaAsync já atualizou o cache — apenas atualiza a UI
            AtualizarListaCategorias();
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
            var cats = sqlCommand.GetValColumn("MATERIAL", "CATEGORIAS");

            // Aba Cadastro
            Categoria_CBox.Items.Clear();
            ExcCategoria_cb.Text = "";
            ExcCategoria_cb.Items.Clear();

            // Aba Banco de dados
            BancoCat_cb.Items.Clear();
            FCategoria_cb.Items.Clear();

            // Aba Medidas
            MedCat_cb.Items.Clear();

            foreach (string cat in cats)
            {
                string c = cat.Trim();
                Categoria_CBox.Items.Add(c);
                ExcCategoria_cb.Items.Add(c);
                BancoCat_cb.Items.Add(c);
                FCategoria_cb.Items.Add(c);
                MedCat_cb.Items.Add(c);
            }

            // Edit Categoria (Categorias tab)
            PopulateEditCatCb();
        }

        private void FCategoria_cb_SelectedIndexChanged(object sender, EventArgs e) { }

        private void button1_Click(object sender, EventArgs e)
        {
            Visualizador form = new Visualizador(sqlCommand);
            form.Show();
        }
    }
}
