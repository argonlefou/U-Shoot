namespace U_Shoot.GUI.Controls
{
    partial class Ctrl_Option
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
            this.Lbl_OptionName = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // Lbl_OptionName
            // 
            this.Lbl_OptionName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.Lbl_OptionName.Location = new System.Drawing.Point(13, 0);
            this.Lbl_OptionName.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Lbl_OptionName.Name = "Lbl_OptionName";
            this.Lbl_OptionName.Size = new System.Drawing.Size(187, 39);
            this.Lbl_OptionName.TabIndex = 5;
            this.Lbl_OptionName.Text = "label1";
            this.Lbl_OptionName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Ctrl_Option
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.Lbl_OptionName);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "Ctrl_Option";
            this.Size = new System.Drawing.Size(680, 39);
            this.Load += new System.EventHandler(this.Ctrl_Option_Load);
            this.SizeChanged += new System.EventHandler(this.Ctrl_Option_SizeChanged);
            this.ResumeLayout(false);

        }

        #endregion

        protected System.Windows.Forms.Label Lbl_OptionName;
    }
}
