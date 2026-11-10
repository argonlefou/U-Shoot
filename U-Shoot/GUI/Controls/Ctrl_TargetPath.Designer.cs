namespace U_Shoot.GUI.Controls
{
    partial class Ctrl_TargetPath
    {
        /// <summary> 
        /// Variable nécessaire au concepteur.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Nettoyage des ressources utilisées.
        /// </summary>
        /// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Code généré par le Concepteur de composants

        /// <summary> 
        /// Méthode requise pour la prise en charge du concepteur - ne modifiez pas 
        /// le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
            this.Lbl_Text = new System.Windows.Forms.Label();
            this.Txt_Value = new System.Windows.Forms.TextBox();
            this.Btn_Open = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.Pnl_Separator = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // Lbl_Text
            // 
            this.Lbl_Text.AutoSize = true;
            this.Lbl_Text.Location = new System.Drawing.Point(10, 10);
            this.Lbl_Text.Name = "Lbl_Text";
            this.Lbl_Text.Size = new System.Drawing.Size(135, 13);
            this.Lbl_Text.TabIndex = 0;
            this.Lbl_Text.Text = "Select your \'game.exe\' file :";
            this.Lbl_Text.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Txt_Value
            // 
            this.Txt_Value.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Txt_Value.Location = new System.Drawing.Point(13, 26);
            this.Txt_Value.Name = "Txt_Value";
            this.Txt_Value.Size = new System.Drawing.Size(612, 20);
            this.Txt_Value.TabIndex = 1;
            // 
            // Btn_Open
            // 
            this.Btn_Open.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Btn_Open.Location = new System.Drawing.Point(631, 25);
            this.Btn_Open.Name = "Btn_Open";
            this.Btn_Open.Size = new System.Drawing.Size(25, 22);
            this.Btn_Open.TabIndex = 2;
            this.Btn_Open.Text = "...";
            this.Btn_Open.UseVisualStyleBackColor = true;
            this.Btn_Open.Click += new System.EventHandler(this.Btn_Open_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // Pnl_Separator
            // 
            this.Pnl_Separator.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Pnl_Separator.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Pnl_Separator.Location = new System.Drawing.Point(13, 60);
            this.Pnl_Separator.Name = "Pnl_Separator";
            this.Pnl_Separator.Size = new System.Drawing.Size(642, 1);
            this.Pnl_Separator.TabIndex = 3;
            // 
            // Ctrl_TargetPath
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.Pnl_Separator);
            this.Controls.Add(this.Btn_Open);
            this.Controls.Add(this.Txt_Value);
            this.Controls.Add(this.Lbl_Text);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "Ctrl_TargetPath";
            this.Size = new System.Drawing.Size(669, 64);
            this.Load += new System.EventHandler(this.Ctrl_TargetPath_Load);
            this.SizeChanged += new System.EventHandler(this.Ctrl_TargetPath_SizeChanged);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label Lbl_Text;
        private System.Windows.Forms.TextBox Txt_Value;
        private System.Windows.Forms.Button Btn_Open;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Panel Pnl_Separator;
    }
}
