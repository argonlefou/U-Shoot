namespace U_Shoot.GUI.Controls
{
    partial class Ctrl_OptionText
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
            this.SuspendLayout();
            // 
            // Txt_OptionValue
            // 
            this.Txt_OptionValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Txt_OptionValue.Location = new System.Drawing.Point(309, 7);
            this.Txt_OptionValue.Name = "Txt_OptionValue";
            this.Txt_OptionValue.Size = new System.Drawing.Size(182, 20);
            this.Txt_OptionValue.TabIndex = 0;
            // 
            // Ctrl_OptionText
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.Txt_OptionValue);
            this.Name = "Ctrl_OptionText";
            this.Controls.SetChildIndex(this.Lbl_OptionName, 0);
            this.Controls.SetChildIndex(this.Txt_OptionValue, 0);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox Txt_OptionValue;
    }
}
