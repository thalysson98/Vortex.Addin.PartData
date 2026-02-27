using EPDM.Interop.epdm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Forms;
using Vortex.Addin.PartData.Core;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Drawing;

namespace Vortex.Addin.PartData
{
    public partial class Visualizador : Form
    {
        EPDMHandler pdmcommand;
        SQLCommands sqlcommand;
        public Visualizador(SQLCommands sql)
        {
            InitializeComponent();
            pdmcommand = new EPDMHandler();
            pdmcommand.Connect();
            sqlcommand = sql;
        }

        private void btn_loadFiles_Click(object sender, EventArgs e)
        {
            treeView1.Nodes.Clear();
            string local = "C:\\Cardall\\PROJETOS";
            addFiles(local, treeView1.Nodes);

        }

        void addFiles(string local, TreeNodeCollection treeNodes)
        {
            List<string> items = new List<string>();
            items = pdmcommand.CarregarPastasRaiz(local);
            if(items != null)
            {
                if (items.Count > 0)
                {
                    items.Sort();
                    foreach (var item in items)
                    {
                        TreeNode newNode = treeNodes.Add(item.Replace(local+"\\", ""));
                        newNode.Tag = item;
                        treeView1.Tag = item;
                        //addFiles(item, newNode.Nodes);
                        newNode.Nodes.Add("Carregando...");
                    }
                }
            }

        }

        void addFiles2(string local, TreeNodeCollection treeNodes)
        {
            List<string> items = new List<string>();
            items = pdmcommand.CarregarPastasRaiz2(local);
            if (items != null)
            {
                if (items.Count > 0)
                {
                    items.Sort();
                    foreach (var item in items)
                    {
                        TreeNode newNode;
                        //addFiles(item, newNode.Nodes);
                        string newstring = item.Substring(item.Length - 2, 2);
                        if(newstring != "@@") 
                        {
                            string PCod1 = @"^\d{3}$";
                            string PCod2 = @"^\d{3}\.\d{3}$";
                            newstring = item.Replace(local + "\\", "");
                            if (Regex.IsMatch(newstring, PCod1))
                            {
                                if (validateFolderCod(newstring, 1))
                                {
                                    newNode = treeNodes.Add(item.Replace(local + "\\", ""));
                                    newNode.BackColor = System.Drawing.Color.Green; // A propriedade BackColor deve ser acessada assim
                                    newNode.Tag = item;
                                    treeView1.Tag = item;
                                    newNode.Nodes.Add("Carregando...");
                                }
                            }
                            else if (Regex.IsMatch(newstring, PCod2))
                            {
                                if (validateFolderCod(newstring, 2))
                                {
                                    newNode = treeNodes.Add(item.Replace(local + "\\", ""));
                                    newNode.BackColor = System.Drawing.Color.Green; // A propriedade BackColor deve ser acessada assim
                                    newNode.Tag = item;
                                    treeView1.Tag = item;
                                    newNode.Nodes.Add("Carregando...");
                                }
                            }
                            else
                            {
                                newNode = treeNodes.Add(item.Replace(local + "\\", ""));
                            }
                        }
                        else
                        {
                            newstring = item.Replace(local + "\\", "");
                            newstring = newstring.Substring(0, Math.Max(0, newstring.Length - 2)); // Evita erro caso a string tenha menos de 2 caracteres
                            
                            if (isPartCod(newstring,out newstring))
                            {
                                if (validateCOD(newstring))
                                {
                                    newNode = treeNodes.Add(newstring);
                                    newNode.BackColor = System.Drawing.Color.Green; // A propriedade BackColor deve ser acessada assim
                                }
                                else
                                {
                                    newNode = treeNodes.Add(newstring);
                                }

                                if (newNode != null) // Garante que newNode não seja nulo antes de acessar suas propriedades
                                {
                                    newNode.Tag = newstring;
                                    treeView1.Tag = newstring;
                                }
                            }

                        }
                        
                    }
                }
            }

        }
        private bool isPartCod(string cod, out string nwString)
        {
            cod = cod.ToLower().Replace(".sldpart", "");
            string str = ".sldprt";
            nwString = cod;
            if (cod.ToLower().Contains(str))
            {
                return true;
            }
            return false;
        }
        private bool validateFolderCod(string cod, int type)
        {
            string cod1 = cod.Substring(0, 3);
            string cod2 = cod.Substring(4, 3);
            List<string> colunasDesejadas;
            Dictionary<string, object> filtros;
            List<string> valoresLinha;
            switch (type)
            {
                case 1:
                    colunasDesejadas = new List<string> { "COD1" };
                    filtros = new Dictionary<string, object>
                    {
                        { "COD1", cod1 }
                    };
                    valoresLinha = sqlcommand.GetRowValues(filtros, colunasDesejadas, "MATERIAIS");
                    if (valoresLinha.Count > 0)
                    {
                        if ($"{valoresLinha[0]}" == cod) { return true; }
                    }
                    break;
                case 2:
                    colunasDesejadas = new List<string> { "COD1", "COD2" };
                    filtros = new Dictionary<string, object>
                {
                    { "COD1", cod1 },
                    { "COD2", cod2 }
                };
                    valoresLinha = sqlcommand.GetRowValues(filtros, colunasDesejadas, "MATERIAIS");
                    if (valoresLinha.Count > 0)
                    {
                        if ($"{valoresLinha[0]}.{valoresLinha[1]}" == cod) { return true; }
                    }

                    break;
            }

            return false;
        }
        private bool validateCOD(string cod)
        {
            cod = cod.ToLower().Replace(".sldpart", "");
            string str =  ".sldprt" ;
            if (cod.ToLower().Contains(str)) 
            {
                cod=cod.Substring(0, cod.Length - str.Count());
                string PadraoCodigo = @"^\d{3}\.\d{3}\.\d{4}$";
                if (Regex.IsMatch(cod, PadraoCodigo))//texto codigo padrao
                {
                    string cod1 = cod.Substring(0, 3);
                    string cod2 = cod.Substring(4, 3);
                    string cod3 = cod.Substring(8, 4);

                    List<string> colunasDesejadas = new List<string> { "COD1", "COD2", "COD3" };
                    Dictionary<string, object> filtros = new Dictionary<string, object>
                {
                    { "COD1", cod1 },
                    { "COD2", cod2 },
                    { "COD3", cod3 }
                };
                    List<string> valoresLinha = sqlcommand.GetRowValues(filtros, colunasDesejadas, "MATERIAIS");
                    if (valoresLinha.Count > 0)
                    {
                        if ($"{valoresLinha[0]}.{valoresLinha[1]}.{valoresLinha[2]}" == cod) { return true; }
                    }
                }
            }

            return false;
        }
        private void Folder_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            TreeNode node = e.Node;

            // Evita que a pasta seja carregada mais de uma vez
            if (node.Nodes.Count == 1 && node.Nodes[0].Text == "Carregando...")
            {
                node.Nodes.Clear(); // Remove o nó fictício

                string pastaCaminho = node.Tag.ToString(); // Obtém o caminho da pasta

                // 1. Adicionar subpastas
                List<string> subPastas = pdmcommand.CarregarPastasRaiz(pastaCaminho);
                if (subPastas != null && subPastas.Count > 0)
                {
                    foreach (var subPasta in subPastas)
                    {
                        TreeNode subNode = new TreeNode(System.IO.Path.GetFileName(subPasta))
                        {
                            Tag = subPasta
                        };
                        subNode.Nodes.Add("Carregando..."); // Nó fictício para carregar sob demanda
                        node.Nodes.Add(subNode);
                    }
                }

                // 2. Adicionar arquivos
                addFiles2(pastaCaminho, node.Nodes);
            }
        }
        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            //List<string> items = new List<string>();
            //items = pdmcommand.CarregarPastasRaiz(treeView1.SelectedNode.Text);

            //if(items == null) { return; }
            //if (items.Count > 0)
            //{
            //    foreach (var item in items)
            //    {
            //        treeView1.SelectedNode.Nodes.Add(item);
            //    }
            //}

        }
    }
}
