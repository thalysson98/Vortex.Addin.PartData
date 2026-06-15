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

        // A busca dos arquivos é feita quando o formulário abre.
        private void Visualizador_Load(object sender, EventArgs e)
        {
            CarregarEstrutura();
        }

        private void btn_loadFiles_Click(object sender, EventArgs e)
        {
            CarregarEstrutura();
        }

        private void CarregarEstrutura()
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                treeView1.BeginUpdate();
                treeView1.Nodes.Clear();

                PdmItem arvore = pdmcommand.CarregarArvore(RaizPdm);
                if (arvore == null)
                {
                    MessageBox.Show("Não foi possível carregar a estrutura do PDM.", "Aviso",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // A raiz é a pasta PROJETOS — seus filhos viram o nível superior da árvore.
                foreach (var filho in arvore.Children)
                    AddNode(filho, treeView1.Nodes);

                foreach (TreeNode n in treeView1.Nodes) n.Expand();
            }
            finally
            {
                treeView1.EndUpdate();
                this.Cursor = Cursors.Default;
            }
        }

        // Cria o nó e calcula o status. Retorna flags do subitem:
        // bit 0 (1) = subárvore contém arquivo cadastrado
        // bit 1 (2) = subárvore contém arquivo NÃO cadastrado
        private int AddNode(PdmItem item, TreeNodeCollection parent)
        {
            var node = new TreeNode(item.Name);
            var vn = new VisualNode { IsFolder = item.IsFolder, Denominacao = item.Denominacao ?? "" };
            node.Tag = vn;
            parent.Add(node);

            if (!item.IsFolder)
            {
                vn.FileRegistered = ArquivoCadastrado(item.Name);
                return vn.FileRegistered ? 1 : 2;
            }

            int flags = 0;
            foreach (var child in item.Children)
                flags |= AddNode(child, node.Nodes);

            // "porém": amarelo tem prioridade quando há arquivos não cadastrados
            if ((flags & 2) != 0)      vn.FolderStatus = 2; // amarelo
            else if ((flags & 1) != 0) vn.FolderStatus = 1; // verde
            else                       vn.FolderStatus = 0; // sem arquivos → normal

            return flags;
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
