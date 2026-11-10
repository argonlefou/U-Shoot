namespace U_Shoot.GUI.Controls
{
    partial class Ctrl_OptionBool
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
            this.Cbox_Option = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // Cbox_Option
            // 
            this.Cbox_Option.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Cbox_Option.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.Cbox_Option.Location = new System.Drawing.Point(474, 0);
            this.Cbox_Option.Name = "Cbox_Option";
            this.Cbox_Option.Size = new System.Drawing.Size(18, 32);
            this.Cbox_Option.TabIndex = 3;
            this.Cbox_Option.UseCompatibleTextRendering = true;
            this.Cbox_Option.UseVisualStyleBackColor = true;
            // 
            // Ctrl_OptionBool
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.Cbox_Option);
            this.Name = "Ctrl_OptionBool";
            this.Controls.SetChildIndex(this.Cbox_Option, 0);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.CheckBox Cbox_Option;
    }
}
