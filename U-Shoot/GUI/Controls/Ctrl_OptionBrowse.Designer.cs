namespace U_Shoot.GUI.Controls
{
    partial class Ctrl_OptionBrowse
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
            this.Txt_OptionValue = new System.Windows.Forms.TextBox();
            this.Btn_Browse = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.SuspendLayout();
            // 
            // Txt_OptionValue
            // 
            this.Txt_OptionValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Txt_OptionValue.Location = new System.Drawing.Point(200, 6);
            this.Txt_OptionValue.Name = "Txt_OptionValue";
            this.Txt_OptionValue.Size = new System.Drawing.Size(267, 20);
            this.Txt_OptionValue.TabIndex = 6;
            // 
            // Btn_Browse
            // 
            this.Btn_Browse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Btn_Browse.Location = new System.Drawing.Point(472, 4);
            this.Btn_Browse.Margin = new System.Windows.Forms.Padding(2);
            this.Btn_Browse.Name = "Btn_Browse";
            this.Btn_Browse.Size = new System.Drawing.Size(25, 22);
            this.Btn_Browse.TabIndex = 7;
            this.Btn_Browse.Text = "...";
            this.Btn_Browse.UseVisualStyleBackColor = true;
            this.Btn_Browse.Click += new System.EventHandler(this.Btn_Browse_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // Ctrl_OptionBrowse
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.Btn_Browse);
            this.Controls.Add(this.Txt_OptionValue);
            this.Name = "Ctrl_OptionBrowse";
            this.Controls.SetChildIndex(this.Lbl_OptionName, 0);
            this.Controls.SetChildIndex(this.Txt_OptionValue, 0);
            this.Controls.SetChildIndex(this.Btn_Browse, 0);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox Txt_OptionValue;
        private System.Windows.Forms.Button Btn_Browse;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
    }
}
