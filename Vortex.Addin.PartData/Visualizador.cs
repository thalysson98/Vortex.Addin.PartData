using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Vortex.Addin.PartData.Core;

namespace Vortex.Addin.PartData
{
    public partial class Visualizador : Form
    {
        EPDMHandler pdmcommand;
        SQLCommands sqlcommand;

        private const int Col1Width = 380;
        private const string RaizPdm = "C:\\Cardall\\PROJETOS";

        public Visualizador(SQLCommands sql)
        {
            InitializeComponent();
            pdmcommand = new EPDMHandler();
            pdmcommand.Connect();
            sqlcommand = sql;
            treeView1.Sorted = true;
        }

        private const string Dummy = "DUMMY";

        // Ao abrir, carrega só as pastas-pai (nível superior). Os demais níveis
        // são carregados sob demanda quando a pasta é expandida.
        private void Visualizador_Load(object sender, EventArgs e)
        {
            CarregarRaiz();
        }

        private void btn_loadFiles_Click(object sender, EventArgs e)
        {
            CarregarRaiz();
        }

        private void CarregarRaiz()
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                treeView1.BeginUpdate();
                treeView1.Nodes.Clear();
                foreach (var item in pdmcommand.ListarConteudo(RaizPdm))
                    AdicionarNo(item, treeView1.Nodes);
            }
            finally
            {
                treeView1.EndUpdate();
                this.Cursor = Cursors.Default;
            }
        }

        // Carrega o conteúdo da pasta quando ela é expandida (lazy).
        private void treeView1_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            var node = e.Node;
            if (node.Nodes.Count != 1 || (node.Nodes[0].Tag as string) != Dummy) return;

            var vn = node.Tag as VisualNode;
            if (vn == null) return;

            this.Cursor = Cursors.WaitCursor;
            try
            {
                treeView1.BeginUpdate();
                node.Nodes.Clear(); // remove o nó fictício
                foreach (var item in pdmcommand.ListarConteudo(vn.LocalPath))
                    AdicionarNo(item, node.Nodes);
            }
            finally
            {
                treeView1.EndUpdate();
                this.Cursor = Cursors.Default;
            }
        }

        // Cria um único nó (sem recursão). Pastas recebem um nó fictício para
        // exibir a seta de expansão e são pintadas de verde se o código existir no banco.
        private void AdicionarNo(PdmItem item, TreeNodeCollection parent)
        {
            var node = new TreeNode(item.Name);
            var vn = new VisualNode
            {
                IsFolder    = item.IsFolder,
                LocalPath   = item.LocalPath,
                Denominacao = item.Denominacao ?? ""
            };
            node.Tag = vn;
            parent.Add(node);

            if (item.IsFolder)
            {
                vn.FolderStatus = PastaCadastrada(item.Name) ? 1 : 0; // verde se código achado
                node.Nodes.Add(new TreeNode("Carregando...") { Tag = Dummy });
            }
            else
            {
                vn.FileRegistered = ArquivoCadastrado(item.Name);
            }
        }

        // Confere se o código da pasta existe no banco (000 → COD1; 000.000 → COD1+COD2).
        private bool PastaCadastrada(string nome)
        {
            if (Regex.IsMatch(nome, @"^\d{3}$"))
            {
                var vals = sqlcommand.GetRowValues(
                    new Dictionary<string, object> { { "COD1", nome } },
                    new List<string> { "COD1" }, "MATERIAIS");
                return vals.Count > 0;
            }
            if (Regex.IsMatch(nome, @"^\d{3}\.\d{3}$"))
            {
                string c1 = nome.Substring(0, 3);
                string c2 = nome.Substring(4, 3);
                var vals = sqlcommand.GetRowValues(
                    new Dictionary<string, object> { { "COD1", c1 }, { "COD2", c2 } },
                    new List<string> { "COD1" }, "MATERIAIS");
                return vals.Count > 0;
            }
            return false;
        }

        // Confere se o arquivo (000.000.0000.sldprt/.sldasm) está cadastrado no banco.
        private bool ArquivoCadastrado(string fileName)
        {
            string nome = (fileName ?? "").ToLower();
            if (nome.EndsWith(".sldprt") || nome.EndsWith(".sldasm"))
                nome = nome.Substring(0, nome.Length - 7);
            else
                return false;

            if (!Regex.IsMatch(nome, @"^\d{3}\.\d{3}\.\d{4}$")) return false;

            string cod1 = nome.Substring(0, 3);
            string cod2 = nome.Substring(4, 3);
            string cod3 = nome.Substring(8, 4);

            var filtros = new Dictionary<string, object>
            {
                { "COD1", cod1 }, { "COD2", cod2 }, { "COD3", cod3 }
            };
            var vals = sqlcommand.GetRowValues(filtros,
                new List<string> { "COD1", "COD2", "COD3" }, "MATERIAIS");

            // O filtro já exige COD1+COD2+COD3 — qualquer linha encontrada é a peça correta.
            return vals.Count > 0;
        }

        // ── Desenho das duas colunas (árvore + Denominação) ──────────────────────
        private void treeView1_DrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            var vn = e.Node.Tag as VisualNode;

            Color back = treeView1.BackColor;
            if (vn != null)
            {
                if (vn.IsFolder)
                {
                    if (vn.FolderStatus == 2)      back = Color.Khaki;      // amarelo
                    else if (vn.FolderStatus == 1) back = Color.LightGreen; // verde
                }
                else if (vn.FileRegistered)
                {
                    back = Color.White; // arquivo cadastrado → destaque branco
                }
            }

            bool selected = (e.State & TreeNodeStates.Selected) != 0;

            // Amplia o clip para a linha inteira (col1 + col2)
            e.Graphics.SetClip(new Rectangle(0, e.Bounds.Top,
                treeView1.ClientRectangle.Width, e.Bounds.Height));

            // Coluna 1 — do texto do nó até o limite da coluna
            var col1 = Rectangle.FromLTRB(e.Bounds.Left, e.Bounds.Top, Col1Width, e.Bounds.Bottom);
            using (var b = new SolidBrush(selected ? SystemColors.Highlight : back))
                e.Graphics.FillRectangle(b, col1);

            TextRenderer.DrawText(e.Graphics, e.Node.Text, treeView1.Font, col1,
                selected ? SystemColors.HighlightText : Color.Black,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);

            // Separador
            e.Graphics.DrawLine(Pens.Gainsboro, Col1Width, e.Bounds.Top, Col1Width, e.Bounds.Bottom);

            // Coluna 2 — Denominação
            var col2 = Rectangle.FromLTRB(Col1Width + 4, e.Bounds.Top,
                treeView1.ClientRectangle.Width, e.Bounds.Bottom);
            using (var b2 = new SolidBrush(treeView1.BackColor))
                e.Graphics.FillRectangle(b2, col2);

            string denom = vn != null ? (vn.Denominacao ?? "") : "";
            TextRenderer.DrawText(e.Graphics, denom, treeView1.Font, col2, Color.Black,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);

            e.Graphics.ResetClip();
        }

        private void headerPanel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(SystemColors.Control);

            var rect1 = Rectangle.FromLTRB(2, 0, Col1Width, headerPanel.Height);
            var rect2 = Rectangle.FromLTRB(Col1Width + 4, 0, headerPanel.Width, headerPanel.Height);

            TextRenderer.DrawText(e.Graphics, "Estrutura (Código)", this.Font, rect1, Color.Black,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(e.Graphics, "Denominação", this.Font, rect2, Color.Black,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

            e.Graphics.DrawLine(Pens.Gray, Col1Width, 0, Col1Width, headerPanel.Height);
            e.Graphics.DrawLine(Pens.Gray, 0, headerPanel.Height - 1, headerPanel.Width, headerPanel.Height - 1);
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e) { }
    }
}
