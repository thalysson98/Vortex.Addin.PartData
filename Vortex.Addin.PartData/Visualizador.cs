using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using BrightIdeasSoftware;
using Vortex.Addin.PartData.Core;

namespace Vortex.Addin.PartData
{
    public partial class Visualizador : Form
    {
        EPDMHandler pdmcommand;
        SQLCommands sqlcommand;

        private const string RaizPdm = "C:\\Cardall\\PROJETOS";

        public Visualizador(SQLCommands sql)
        {
            InitializeComponent();
            pdmcommand = new EPDMHandler();
            pdmcommand.Connect();
            sqlcommand = sql;
            ConfigurarLista();
        }

        // Configura colunas, carregamento sob demanda e a coloração verde.
        private void ConfigurarLista()
        {
            olvColCodigo.AspectGetter = m => ((PdmItem)m).Name;
            olvColDenom .AspectGetter = m => ((PdmItem)m).Denominacao ?? "";

            // Só pastas podem expandir
            treeListView1.CanExpandGetter = m => ((PdmItem)m).IsFolder;
            // Filhos carregados sob demanda quando o nó é expandido (lazy)
            treeListView1.ChildrenGetter = m => CarregarFilhos((PdmItem)m);

            // Pinta de verde o que estiver cadastrado no banco
            treeListView1.RowFormatter = olvItem =>
            {
                PdmItem p = olvItem.RowObject as PdmItem;
                if (p != null && p.Registered)
                    olvItem.BackColor = Color.LightGreen;
            };
        }

        // Ao abrir: carrega só as pastas-pai (nível superior).
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
                var raiz = new PdmItem { LocalPath = RaizPdm, IsFolder = true };
                treeListView1.Roots = CarregarFilhos(raiz);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        // Lista o conteúdo imediato de uma pasta e marca o que está cadastrado no banco.
        // Pastas-pai (000) → verifica COD1; subpastas (000.000) → COD1+COD2; arquivos → código completo.
        private IEnumerable CarregarFilhos(PdmItem pasta)
        {
            var itens = pdmcommand.ListarConteudo(pasta.LocalPath);
            foreach (var it in itens)
                it.Registered = it.IsFolder ? PastaCadastrada(it.Name) : ArquivoCadastrado(it.Name);
            return itens;
        }

        // Confere o código da pasta no banco (000 → COD1; 000.000 → COD1+COD2).
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

            var vals = sqlcommand.GetRowValues(
                new Dictionary<string, object> { { "COD1", cod1 }, { "COD2", cod2 }, { "COD3", cod3 } },
                new List<string> { "COD1" }, "MATERIAIS");

            return vals.Count > 0;
        }
    }
}
