using EPDM.Interop.epdm;
using System;
using System.Windows.Controls;
using System.Windows;
using System.Collections.Generic;

namespace Vortex.Addin.PartData.Core
{
    public class EPDMHandler
    {
        IEdmUserMgr10 UsrMgr;
        IEdmVault5 vault;
        public bool Connect()
        {
            try
            {
                vault = new EdmVault5();

                if (!vault.IsLoggedIn)
                {
                    vault.LoginAuto("Cardall", 0);
                    return true;
                }
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                System.Windows.Forms.MessageBox.Show("HRESULT = 0x" + ex.ErrorCode.ToString("X") + " " + ex.Message);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message);
            }
            return false;
        }
        public string GetUser()
        {
            if (vault.IsLoggedIn)
            {
                //vault.LoginAuto("Cardall", 0);
                UsrMgr = (IEdmUserMgr10)vault;
                IEdmUser5 user1 = UsrMgr.GetLoggedInUser();
                return user1.Name;
            }

            return "";
        }
        // Lista APENAS o conteúdo imediato de uma pasta (subpastas + arquivos),
        // lendo a "Denominação" só dos arquivos. Carregamento sob demanda (lazy).
        public List<PdmItem> ListarConteudo(string localPath)
        {
            var result = new List<PdmItem>();
            try
            {
                if (vault == null || !vault.IsLoggedIn) return result;
                IEdmFolder5 pasta = vault.GetFolderFromPath(localPath);
                if (pasta == null) return result;

                // Subpastas (sem recursão)
                IEdmPos5 folderPos = pasta.GetFirstSubFolderPosition();
                while (!folderPos.IsNull)
                {
                    IEdmFolder5 sub = pasta.GetNextSubFolder(folderPos);
                    result.Add(new PdmItem
                    {
                        Name      = System.IO.Path.GetFileName(sub.LocalPath.TrimEnd('\\')),
                        LocalPath = sub.LocalPath,
                        IsFolder  = true
                    });
                }

                // Arquivos — apenas peça (.sldprt) e montagem (.sldasm)
                IEdmPos5 filePos = pasta.GetFirstFilePosition();
                while (!filePos.IsNull)
                {
                    IEdmFile5 file = pasta.GetNextFile(filePos);
                    string nome = file.Name;
                    string ext = nome.ToLower();
                    if (!ext.EndsWith(".sldprt") && !ext.EndsWith(".sldasm"))
                        continue; // ignora desenhos e outros tipos

                    result.Add(new PdmItem
                    {
                        Name        = nome,
                        IsFolder    = false,
                        Denominacao = ObterVariavel(file, "Denominação")
                    });
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Erro ao listar pasta do PDM: " + ex.Message);
            }
            return result;
        }

        // Lê uma variável do cartão de dados do arquivo (tenta a config "@" e depois a vazia).
        private string ObterVariavel(IEdmFile5 file, string variavel)
        {
            try
            {
                IEdmEnumeratorVariable5 ev = (IEdmEnumeratorVariable5)file.GetEnumeratorVariable();
                object valor;
                if (ev.GetVar(variavel, "@", out valor) && valor != null)
                    return valor.ToString();
                if (ev.GetVar(variavel, "", out valor) && valor != null)
                    return valor.ToString();
            }
            catch { }
            return "";
        }

        public List<string> CarregarPastasRaiz(string local)
        {
            try
            {

                //if (vault == null || !vault.IsLoggedIn)
                //{
                //    System.Windows.Forms.MessageBox.Show("Não está conectado ao PDM.");
                //    return null;
                //}

                List<string> items = new List<string>();
                //vault.GetFolderFromPath("C:\\Cardall\\PROJETOS");
                items = TraverseFolder(vault.GetFolderFromPath(local));
                return items;
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                //MessageBox.Show("HRESULT = 0x" + ex.ErrorCode.ToString("X") + " " + ex.Message);
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
            }
            return null;
        }

        private List<string> TraverseFolder(IEdmFolder5 CurFolder)
        {
            try
            {
                //Enumerate the files in the folder
                IEdmPos5 FilePos = default(IEdmPos5);
                if(CurFolder == null) { return null; }
                FilePos = CurFolder.GetFirstFilePosition();
                IEdmFile5 file = default(IEdmFile5);
                List<string> list = new List<string>();
                //while (!FilePos.IsNull)
                //{
                //    file = CurFolder.GetNextFile(FilePos);
                //    //Get its checked out status
                //    //if (file.IsLocked)
                //    //{
                //        //listBox1.Items.Add(file.LockPath);
                //        list.Add(file.GetLocalPath(CurFolder.ID));
                //    //}
                //}
                ////
                //Enumerate the sub - folders in the folder
                IEdmPos5 FolderPos = default(IEdmPos5);
                FolderPos = CurFolder.GetFirstSubFolderPosition();
                while (!FolderPos.IsNull)
                {
                    IEdmFolder5 SubFolder = default(IEdmFolder5);
                    SubFolder = CurFolder.GetNextSubFolder(FolderPos);
                    list.Add(SubFolder.LocalPath);
                }
                return list;
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                //System.Windows.MessageBox.Show("HRESULT = 0x" + ex.ErrorCode.ToString("X") + ex.Message);
            }
            catch (Exception ex)
            {
                //System.Windows.MessageBox.Show(ex.Message);
            }
            return null;
        }


        public List<string> CarregarPastasRaiz2(string local)
        {
            try
            {

                //if (vault == null || !vault.IsLoggedIn)
                //{
                //    System.Windows.Forms.MessageBox.Show("Não está conectado ao PDM.");
                //    return null;
                //}

                List<string> items = new List<string>();
                //vault.GetFolderFromPath("C:\\Cardall\\PROJETOS");
                items = TraverseFolder2(vault.GetFolderFromPath(local));
                return items;
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                //MessageBox.Show("HRESULT = 0x" + ex.ErrorCode.ToString("X") + " " + ex.Message);
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
            }
            return null;
        }

        private List<string> TraverseFolder2(IEdmFolder5 CurFolder)
        {
            try
            {
                //Enumerate the files in the folder
                IEdmPos5 FilePos = default(IEdmPos5);
                if (CurFolder == null) { return null; }
                FilePos = CurFolder.GetFirstFilePosition();
                IEdmFile5 file = default(IEdmFile5);
                List<string> list = new List<string>();
                while (!FilePos.IsNull)
                {
                    file = CurFolder.GetNextFile(FilePos);
                    //Get its checked out status
                    //if (file.IsLocked)
                    //{
                    //listBox1.Items.Add(file.LockPath);
                    list.Add(file.GetLocalPath(CurFolder.ID)+"@@");
                    //}
                }
                ////
                //Enumerate the sub - folders in the folder
                IEdmPos5 FolderPos = default(IEdmPos5);
                FolderPos = CurFolder.GetFirstSubFolderPosition();
                while (!FolderPos.IsNull)
                {
                    IEdmFolder5 SubFolder = default(IEdmFolder5);
                    SubFolder = CurFolder.GetNextSubFolder(FolderPos);
                    list.Add(SubFolder.LocalPath);
                }
                return list;
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                //System.Windows.MessageBox.Show("HRESULT = 0x" + ex.ErrorCode.ToString("X") + ex.Message);
            }
            catch (Exception ex)
            {
                //System.Windows.MessageBox.Show(ex.Message);
            }
            return null;
        }

    }
}
