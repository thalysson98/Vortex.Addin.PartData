using System.Collections.Generic;

namespace Vortex.Addin.PartData.Core
{
    // Representa um nó (pasta ou arquivo) lido do vault do PDM.
    public class PdmItem
    {
        public string Name;        // nome de exibição (pasta ou arquivo)
        public string LocalPath;   // caminho local completo
        public bool   IsFolder;
        public string Denominacao; // propriedade "Denominação" do PDM (arquivos)
        public List<PdmItem> Children = new List<PdmItem>();
    }
}
