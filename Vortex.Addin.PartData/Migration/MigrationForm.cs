using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Vortex.Addin.PartData.Migration
{
    public class MigrationForm : Form
    {
        // Contadores por tabela
        private readonly Label _lblTipos;
        private readonly Label _lblCategorias;
        private readonly Label _lblUsers;
        private readonly Label _lblMedidas;
        private readonly Label _lblMateriais;

        private readonly ProgressBar _progress;
        private readonly Label       _lblStatus;
        private readonly RichTextBox _log;
        private readonly Button      _btnIniciar;

        public MigrationForm()
        {
            Text            = "Migração de Banco de Dados";
            Size            = new Size(620, 580);
            MinimumSize     = new Size(620, 580);
            StartPosition   = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;

            // ── Cabeçalho ────────────────────────────────────────────────────────
            var lblHeader = new Label
            {
                Text      = "PDB_BANCO   →   PDB_BANCO_V2",
                Font      = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 84, 166),
                Location  = new Point(20, 18),
                Size      = new Size(560, 28),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var separator = new Label
            {
                BorderStyle = BorderStyle.Fixed3D,
                Location    = new Point(20, 52),
                Size        = new Size(560, 2)
            };

            // ── Status por tabela ─────────────────────────────────────────────────
            int y = 64;
            _lblTipos      = MakeTableLabel("TIPOS",      ref y);
            _lblCategorias = MakeTableLabel("CATEGORIAS", ref y);
            _lblUsers      = MakeTableLabel("USERS",      ref y);
            _lblMedidas    = MakeTableLabel("MEDIDAS",    ref y);
            _lblMateriais  = MakeTableLabel("MATERIAIS",  ref y);

            y += 8;

            // ── Barra de progresso ───────────────────────────────────────────────
            _progress = new ProgressBar
            {
                Location = new Point(20, y),
                Size     = new Size(560, 18),
                Minimum  = 0,
                Maximum  = 100
            };
            y += 28;

            // ── Status geral ─────────────────────────────────────────────────────
            _lblStatus = new Label
            {
                Text      = "Clique em 'Iniciar' para começar a migração.",
                Location  = new Point(20, y),
                Size      = new Size(560, 18),
                ForeColor = Color.Gray,
                Font      = new Font("Segoe UI", 9, FontStyle.Italic)
            };
            y += 28;

            // ── Log ──────────────────────────────────────────────────────────────
            _log = new RichTextBox
            {
                Location    = new Point(20, y),
                Size        = new Size(560, 210),
                ReadOnly    = true,
                BackColor   = Color.FromArgb(22, 22, 22),
                ForeColor   = Color.FromArgb(180, 230, 180),
                Font        = new Font("Consolas", 9),
                ScrollBars  = RichTextBoxScrollBars.Vertical,
                BorderStyle = BorderStyle.FixedSingle
            };
            y += 220;

            // ── Botões ───────────────────────────────────────────────────────────
            _btnIniciar = new Button
            {
                Text      = "▶  Iniciar Migração",
                Location  = new Point(350, y),
                Size      = new Size(150, 32),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            _btnIniciar.FlatAppearance.BorderSize = 0;
            _btnIniciar.Click += OnIniciar;

            var btnFechar = new Button
            {
                Text      = "Fechar",
                Location  = new Point(510, y),
                Size      = new Size(80, 32),
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            btnFechar.Click += (s, e) => Close();

            Controls.AddRange(new Control[]
            {
                lblHeader, separator,
                _lblTipos, _lblCategorias, _lblUsers, _lblMedidas, _lblMateriais,
                _progress, _lblStatus, _log,
                _btnIniciar, btnFechar
            });
        }

        // ── Lógica da migração ────────────────────────────────────────────────────

        private async void OnIniciar(object sender, EventArgs e)
        {
            _btnIniciar.Enabled = false;
            _log.Clear();
            ResetTableLabels();

            var svc = new MigrationService(AppendLog);

            try
            {
                SetStatus("Criando banco de dados...", Color.DodgerBlue);
                await svc.CreateDatabaseAsync();

                SetStatus("Criando tabelas...", Color.DodgerBlue);
                await svc.CreateTablesAsync();
                SetProgress(10);

                SetStatus("Migrando TIPOS...", Color.DodgerBlue);
                int tipos = await svc.MigrateTiposAsync();
                SetTableOk(_lblTipos, "TIPOS", tipos);
                SetProgress(25);

                SetStatus("Migrando CATEGORIAS...", Color.DodgerBlue);
                int cats = await svc.MigrateCategoriasAsync();
                SetTableOk(_lblCategorias, "CATEGORIAS", cats);
                SetProgress(40);

                SetStatus("Migrando USERS...", Color.DodgerBlue);
                int users = await svc.MigrateUsersAsync();
                SetTableOk(_lblUsers, "USERS", users);
                SetProgress(55);

                SetStatus("Migrando MEDIDAS...", Color.DodgerBlue);
                int med = await svc.MigrateMedidasAsync();
                SetTableOk(_lblMedidas, "MEDIDAS", med);
                SetProgress(65);

                SetStatus("Migrando MATERIAIS...", Color.DodgerBlue);
                var progressRpt = new Progress<(int cur, int tot)>(p =>
                {
                    if (p.tot <= 0) return;
                    int pct = 65 + (int)(p.cur * 35.0 / p.tot);
                    SafeSet(_progress, () => _progress.Value = Math.Min(pct, 100));
                    SafeSet(_lblStatus, () =>
                        _lblStatus.Text = $"Migrando MATERIAIS... {p.cur}/{p.tot}");
                });

                var (mig, skip) = await svc.MigrateMateriaisAsync(progressRpt);
                SetTableLabel(_lblMateriais, "MATERIAIS", mig, skip == 0
                    ? $"{mig} registros  ✓"
                    : $"{mig} migrados, {skip} pulados  ⚠",
                    skip == 0 ? Color.Green : Color.Orange);
                SetProgress(100);

                SetStatus("Migração concluída com sucesso!", Color.Green);
                AppendLog("══════════════════════════════════════");
                AppendLog("  MIGRAÇÃO CONCLUÍDA — PDB_BANCO_V2 pronto.");
                AppendLog("══════════════════════════════════════");

                MessageBox.Show(
                    $"Migração concluída!\n\n" +
                    $"TIPOS:      {tipos}\n" +
                    $"CATEGORIAS: {cats}\n" +
                    $"USERS:      {users}\n" +
                    $"MEDIDAS:    {med}\n" +
                    $"MATERIAIS:  {mig} migrados, {skip} pulados",
                    "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                SetStatus($"Erro fatal: {ex.Message}", Color.Red);
                AppendLog($"ERRO FATAL: {ex.Message}");
                MessageBox.Show($"Erro durante a migração:\n\n{ex.Message}",
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _btnIniciar.Enabled = true;
            }
        }

        // ── Helpers de UI ─────────────────────────────────────────────────────────

        private Label MakeTableLabel(string nome, ref int y)
        {
            var lbl = new Label
            {
                Text      = $"{nome,-12}  aguardando...",
                Location  = new Point(20, y),
                Size      = new Size(560, 18),
                Font      = new Font("Consolas", 9),
                ForeColor = Color.Gray
            };
            Controls.Add(lbl);
            y += 20;
            return lbl;
        }

        private void ResetTableLabels()
        {
            foreach (var lbl in new[] { _lblTipos, _lblCategorias, _lblUsers, _lblMedidas, _lblMateriais })
                SafeSet(lbl, () => { lbl.Text = $"{lbl.Text.Split(' ')[0],-12}  aguardando..."; lbl.ForeColor = Color.Gray; });
        }

        private void SetTableOk(Label lbl, string nome, int count) =>
            SetTableLabel(lbl, nome, count, $"{count} registros  ✓", Color.Green);

        private void SetTableLabel(Label lbl, string nome, int count, string detail, Color color) =>
            SafeSet(lbl, () => { lbl.Text = $"{nome,-12}  {detail}"; lbl.ForeColor = color; });

        private void SetProgress(int pct) =>
            SafeSet(_progress, () => _progress.Value = pct);

        private void SetStatus(string msg, Color color) =>
            SafeSet(_lblStatus, () => { _lblStatus.Text = msg; _lblStatus.ForeColor = color; _lblStatus.Font = new Font("Segoe UI", 9); });

        private void AppendLog(string msg)
        {
            if (_log.InvokeRequired) { _log.Invoke(new Action(() => AppendLog(msg))); return; }
            _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\n");
            _log.ScrollToCaret();
        }

        private static void SafeSet(Control c, Action act)
        {
            if (c.InvokeRequired) c.Invoke(act);
            else act();
        }
    }
}
