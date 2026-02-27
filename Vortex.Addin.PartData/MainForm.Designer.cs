using System.Windows.Forms;

namespace Vortex.Addin.PartData
{
    partial class MainForm
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
            this.Categoria_CBox2 = new System.Windows.Forms.ComboBox();
            this.menu = new System.Windows.Forms.MenuStrip();
            this.Cadastro_menu = new System.Windows.Forms.ToolStripMenuItem();
            this.Inserir_bt = new System.Windows.Forms.Button();
            this.LB1 = new System.Windows.Forms.Label();
            this.Diametro_CBox = new System.Windows.Forms.ComboBox();
            this.Espessura_Cbox = new System.Windows.Forms.ComboBox();
            this.Comprimento_Cbox = new System.Windows.Forms.ComboBox();
            this.LB2 = new System.Windows.Forms.Label();
            this.LB3 = new System.Windows.Forms.Label();
            this.LB4 = new System.Windows.Forms.Label();
            this.M4_Cbox = new System.Windows.Forms.ComboBox();
            this.LB5 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.menu.SuspendLayout();
            this.SuspendLayout();
            // 
            // Categoria_CBox2
            // 
            this.Categoria_CBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Categoria_CBox2.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.Categoria_CBox2.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.Categoria_CBox2.BackColor = System.Drawing.SystemColors.Window;
            this.Categoria_CBox2.FormattingEnabled = true;
            this.Categoria_CBox2.Location = new System.Drawing.Point(27, 112);
            this.Categoria_CBox2.Name = "Categoria_CBox2";
            this.Categoria_CBox2.Size = new System.Drawing.Size(232, 21);
            this.Categoria_CBox2.TabIndex = 0;
            this.Categoria_CBox2.SelectedIndexChanged += new System.EventHandler(this.Categoria_CBox2_SelectedIndexChanged);
            // 
            // menu
            // 
            this.menu.BackColor = System.Drawing.SystemColors.Window;
            this.menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Cadastro_menu});
            this.menu.Location = new System.Drawing.Point(0, 0);
            this.menu.Name = "menu";
            this.menu.Size = new System.Drawing.Size(290, 24);
            this.menu.TabIndex = 122;
            this.menu.Text = "menu";
            // 
            // Cadastro_menu
            // 
            this.Cadastro_menu.Name = "Cadastro_menu";
            this.Cadastro_menu.Size = new System.Drawing.Size(69, 20);
            this.Cadastro_menu.Text = "Cadastrar";
            this.Cadastro_menu.Click += new System.EventHandler(this.Cadastro_menu_Click);
            // 
            // Inserir_bt
            // 
            this.Inserir_bt.Location = new System.Drawing.Point(106, 343);
            this.Inserir_bt.Name = "Inserir_bt";
            this.Inserir_bt.Size = new System.Drawing.Size(75, 23);
            this.Inserir_bt.TabIndex = 5;
            this.Inserir_bt.Text = "Inserir";
            this.Inserir_bt.UseVisualStyleBackColor = true;
            this.Inserir_bt.Click += new System.EventHandler(this.Inserir_bt_Click);
            // 
            // LB1
            // 
            this.LB1.AutoSize = true;
            this.LB1.Location = new System.Drawing.Point(27, 96);
            this.LB1.Name = "LB1";
            this.LB1.Size = new System.Drawing.Size(52, 13);
            this.LB1.TabIndex = 33;
            this.LB1.Text = "Categoria";
            // 
            // Diametro_CBox
            // 
            this.Diametro_CBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Diametro_CBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.Diametro_CBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.Diametro_CBox.FormattingEnabled = true;
            this.Diametro_CBox.Location = new System.Drawing.Point(27, 157);
            this.Diametro_CBox.Name = "Diametro_CBox";
            this.Diametro_CBox.Size = new System.Drawing.Size(232, 21);
            this.Diametro_CBox.TabIndex = 1;
            this.Diametro_CBox.SelectedIndexChanged += new System.EventHandler(this.Diametro_CBox_SelectedIndexChanged);
            // 
            // Espessura_Cbox
            // 
            this.Espessura_Cbox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Espessura_Cbox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.Espessura_Cbox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.Espessura_Cbox.FormattingEnabled = true;
            this.Espessura_Cbox.Location = new System.Drawing.Point(27, 202);
            this.Espessura_Cbox.Name = "Espessura_Cbox";
            this.Espessura_Cbox.Size = new System.Drawing.Size(232, 21);
            this.Espessura_Cbox.TabIndex = 2;
            this.Espessura_Cbox.SelectedIndexChanged += new System.EventHandler(this.Espessura_Cbox_SelectedIndexChanged);
            // 
            // Comprimento_Cbox
            // 
            this.Comprimento_Cbox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Comprimento_Cbox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.Comprimento_Cbox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.Comprimento_Cbox.FormattingEnabled = true;
            this.Comprimento_Cbox.Location = new System.Drawing.Point(27, 247);
            this.Comprimento_Cbox.Name = "Comprimento_Cbox";
            this.Comprimento_Cbox.Size = new System.Drawing.Size(232, 21);
            this.Comprimento_Cbox.TabIndex = 3;
            this.Comprimento_Cbox.SelectedIndexChanged += new System.EventHandler(this.Comprimento_Cbox_SelectedIndexChanged);
            // 
            // LB2
            // 
            this.LB2.AutoSize = true;
            this.LB2.Location = new System.Drawing.Point(27, 141);
            this.LB2.Name = "LB2";
            this.LB2.Size = new System.Drawing.Size(49, 13);
            this.LB2.TabIndex = 33;
            this.LB2.Text = "Diametro";
            // 
            // LB3
            // 
            this.LB3.AutoSize = true;
            this.LB3.Location = new System.Drawing.Point(27, 185);
            this.LB3.Name = "LB3";
            this.LB3.Size = new System.Drawing.Size(56, 13);
            this.LB3.TabIndex = 34;
            this.LB3.Text = "Espessura";
            // 
            // LB4
            // 
            this.LB4.AutoSize = true;
            this.LB4.Location = new System.Drawing.Point(27, 231);
            this.LB4.Name = "LB4";
            this.LB4.Size = new System.Drawing.Size(68, 13);
            this.LB4.TabIndex = 35;
            this.LB4.Text = "Comprimento";
            // 
            // M4_Cbox
            // 
            this.M4_Cbox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.M4_Cbox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.M4_Cbox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.M4_Cbox.FormattingEnabled = true;
            this.M4_Cbox.Location = new System.Drawing.Point(27, 292);
            this.M4_Cbox.Name = "M4_Cbox";
            this.M4_Cbox.Size = new System.Drawing.Size(232, 21);
            this.M4_Cbox.TabIndex = 4;
            this.M4_Cbox.SelectedIndexChanged += new System.EventHandler(this.M4_Cbox_SelectedIndexChanged);
            // 
            // LB5
            // 
            this.LB5.AutoSize = true;
            this.LB5.Location = new System.Drawing.Point(27, 276);
            this.LB5.Name = "LB5";
            this.LB5.Size = new System.Drawing.Size(68, 13);
            this.LB5.TabIndex = 39;
            this.LB5.Text = "Comprimento";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("TT Supermolot Neue Exp DBold", 32F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.MidnightBlue;
            this.label1.Location = new System.Drawing.Point(66, 35);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(165, 50);
            this.label1.TabIndex = 123;
            this.label1.Text = "cardall";
            // 
            // MainForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.ClientSize = new System.Drawing.Size(290, 396);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.M4_Cbox);
            this.Controls.Add(this.Comprimento_Cbox);
            this.Controls.Add(this.Espessura_Cbox);
            this.Controls.Add(this.Diametro_CBox);
            this.Controls.Add(this.Categoria_CBox2);
            this.Controls.Add(this.LB5);
            this.Controls.Add(this.LB4);
            this.Controls.Add(this.LB3);
            this.Controls.Add(this.LB2);
            this.Controls.Add(this.LB1);
            this.Controls.Add(this.Inserir_bt);
            this.Controls.Add(this.menu);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MainMenuStrip = this.menu;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Inserir Peça";
            this.TopMost = true;
            this.menu.ResumeLayout(false);
            this.menu.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox Categoria_CBox2;
        private System.Windows.Forms.MenuStrip menu;
        private System.Windows.Forms.ToolStripMenuItem Cadastro_menu;
        private System.Windows.Forms.Button Inserir_bt;
        private System.Windows.Forms.Label LB1;
        private System.Windows.Forms.ComboBox Diametro_CBox;
        private System.Windows.Forms.ComboBox Espessura_Cbox;
        private System.Windows.Forms.ComboBox Comprimento_Cbox;
        private System.Windows.Forms.Label LB2;
        private System.Windows.Forms.Label LB3;
        private System.Windows.Forms.Label LB4;
        private System.Windows.Forms.ComboBox M4_Cbox;
        private System.Windows.Forms.Label LB5;
        private Label label1;
    }
}