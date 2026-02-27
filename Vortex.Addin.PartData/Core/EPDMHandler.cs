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
