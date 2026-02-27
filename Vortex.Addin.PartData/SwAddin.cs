using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xarial.XCad.Extensions;
using Xarial.XCad.SolidWorks;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using Xarial.XCad.Base.Attributes;
using Xarial.XCad.UI.Commands.Attributes;
using Xarial.XCad.UI.Commands;
using Xarial.XCad.Documents.Extensions;
using Xarial.XCad.Documents;
using DWM.ExSw.Addin.Core;
using Vortex.Addin.PartData.Core;
using System.Windows.Shapes;
using System.Threading;

namespace Vortex.Addin.PartData
{
    [System.Runtime.InteropServices.ComVisible(true)]
    [System.Runtime.InteropServices.Guid("b74e1178-0e6c-4acb-9efa-0997fe7d3380")]
    [Title("Part Data Base")]
    public class SwAddin : SwAddInEx
    {
        [System.Runtime.InteropServices.ComRegisterFunction]
        public static void RegisterFunction(Type t)
        {
            SwAddInEx.RegisterFunction(t);
        }

        [System.Runtime.InteropServices.ComUnregisterFunction]
        public static void UnregisterFunction(Type t)
        {
            SwAddInEx.UnregisterFunction(t);
        }

        public MainForm controlFormsM_control;
        SQLCommands sql_comm;

        public override void OnConnect()
        {
            this.CommandManager.AddCommandGroup<Commands_e>().CommandClick += OnButtonClick;
            this.Application.Documents.RegisterHandler<SwDocHandler>();
            Application.Documents.DocumentActivated += OnDocumentActivated;
            sql_comm = new SQLCommands();

            Task task1 = Task.Run(() => sql_comm.CarregarDadosIniciais());
            
        }
        public override void OnDisconnect() 
        {
            sql_comm.Disconnect();
            //this.CommandManager.AddCommandGroup<Commands_e>().CommandClick -= OnButtonClick;
            //Application.Documents.DocumentActivated -= OnDocumentActivated;
        }
        private void OnDocumentActivated(IXDocument doc)
        {
            //ModelDoc2 model = (ModelDoc2)Application.Documents.Active.Model;
            //SldWorks app = (SldWorks)Application.Sw;
        }

        [Title("Cardall")]
        public enum Commands_e
        {
            [Title("Inserir Peça")]
            [Icon(typeof(Properties.Resources), nameof(Properties.Resources.PDB_icon))]
            Insert
        }

        public MainForm mainforms { get; private set; }

        private void OnButtonClick(Commands_e cmd)
        {
            if (mainforms == null || mainforms.IsDisposed) // Se o formulário não existe ou foi fechado
            {
                mainforms = new MainForm((SldWorks)Application.Sw, sql_comm);
                mainforms.Show();
            }
            else
            {
                mainforms.Focus(); // Se já está aberto, apenas traz para frente
            }
        }
    }
}
