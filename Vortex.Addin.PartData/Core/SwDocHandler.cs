using SolidWorks.Interop.sldworks;
using Xarial.XCad.Documents;
using Xarial.XCad.SolidWorks;
using Xarial.XCad.SolidWorks.Documents;
using Xarial.XCad.SolidWorks.Documents.Services;

namespace DWM.ExSw.Addin.Core
{
    public class SwDocHandler : SwDocumentHandler
    {

        protected override void AttachAssemblyEvents(AssemblyDoc assm)
        {
            base.AttachAssemblyEvents(assm);
        }
        protected override void AttachPartEvents(PartDoc part)
        {
            part.AddItemNotify += OnAddItemNotify;

        }

        protected override void DetachPartEvents(PartDoc part)
        {
            part.AddItemNotify -= OnAddItemNotify;
        }

        protected override void OnInit(ISwApplication app, ISwDocument doc)
        {

            base.OnInit(app, doc);
        }
        private int OnAddItemNotify(int EntityType, string itemName)
        {
            //Implement
            return 0;
        }

    }
}
