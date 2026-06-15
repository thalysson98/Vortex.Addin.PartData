namespace Vortex.Addin.PartData.Core
{
    // Dados de exibição anexados a cada nó da árvore do Visualizador (node.Tag).
    public class VisualNode
    {
        public bool   IsFolder;
        public string Denominacao;
        public bool   FileRegistered;  // arquivos
        public int    FolderStatus;    // pastas: 0 normal | 1 verde | 2 amarelo
    }
}
