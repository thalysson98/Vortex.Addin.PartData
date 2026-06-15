namespace Vortex.Addin.PartData
{
    partial class Visualizador
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.headerPanel = new System.Windows.Forms.Panel();
            this.bottomPanel = new System.Windows.Forms.Panel();
            this.btn_loadFiles = new System.Windows.Forms.Button();
            this.bottomPanel.SuspendLayout();
            this.SuspendLayout();
            //
            // treeView1
            //
            this.treeView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeView1.DrawMode = System.Windows.Forms.TreeViewDrawMode.OwnerDrawText;
            this.treeView1.FullRowSelect = true;
            this.treeView1.HideSelection = false;
            this.treeView1.Location = new System.Drawing.Point(0, 22);
            this.treeView1.Name = "treeView1";
            this.treeView1.Size = new System.Drawing.Size(692, 502);
            this.treeView1.TabIndex = 0;
            this.treeView1.DrawNode += new System.Windows.Forms.DrawTreeNodeEventHandler(this.treeView1_DrawNode);
            this.treeView1.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.treeView1_BeforeExpand);
            this.treeView1.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeView1_AfterSelect);
            //
            // headerPanel
            //
            this.headerPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.headerPanel.Location = new System.Drawing.Point(0, 0);
            this.headerPanel.Name = "headerPanel";
            this.headerPanel.Size = new System.Drawing.Size(692, 22);
            this.headerPanel.TabIndex = 2;
            this.headerPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.headerPanel_Paint);
            //
            // bottomPanel
            //
            this.bottomPanel.Controls.Add(this.btn_loadFiles);
            this.bottomPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.bottomPanel.Location = new System.Drawing.Point(0, 524);
            this.bottomPanel.Name = "bottomPanel";
            this.bottomPanel.Size = new System.Drawing.Size(692, 30);
            this.bottomPanel.TabIndex = 3;
            //
            // btn_loadFiles
            //
            this.btn_loadFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_loadFiles.Location = new System.Drawing.Point(605, 4);
            this.btn_loadFiles.Name = "btn_loadFiles";
            this.btn_loadFiles.Size = new System.Drawing.Size(75, 23);
            this.btn_loadFiles.TabIndex = 0;
            this.btn_loadFiles.Text = "Recarregar";
            this.btn_loadFiles.UseVisualStyleBackColor = true;
            this.btn_loadFiles.Click += new System.EventHandler(this.btn_loadFiles_Click);
            //
            // Visualizador
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(692, 554);
            this.Controls.Add(this.treeView1);
            this.Controls.Add(this.headerPanel);
            this.Controls.Add(this.bottomPanel);
            this.Name = "Visualizador";
            this.Text = "Visualizador PDM x Banco";
            this.Load += new System.EventHandler(this.Visualizador_Load);
            this.bottomPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.Panel headerPanel;
        private System.Windows.Forms.Panel bottomPanel;
        private System.Windows.Forms.Button btn_loadFiles;
    }
}
