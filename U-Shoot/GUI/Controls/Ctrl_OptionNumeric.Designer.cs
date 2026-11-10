namespace U_Shoot.GUI.Controls
{
    partial class Ctrl_OptionNumeric
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
            this.Txt_Value = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.Txt_Value)).BeginInit();
            this.SuspendLayout();
            // 
            // Txt_Value
            // 
            this.Txt_Value.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Txt_Value.Location = new System.Drawing.Point(415, 8);
            this.Txt_Value.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.Txt_Value.Name = "Txt_Value";
            this.Txt_Value.Size = new System.Drawing.Size(77, 20);
            this.Txt_Value.TabIndex = 3;
            this.Txt_Value.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.Txt_Value.Click += new System.EventHandler(this.Txt_Value_Click);
            this.Txt_Value.Enter += new System.EventHandler(this.Txt_Value_Enter);
            // 
            // Ctrl_OptionNumeric
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.Txt_Value);
            this.Name = "Ctrl_OptionNumeric";
            this.Controls.SetChildIndex(this.Lbl_OptionName, 0);
            this.Controls.SetChildIndex(this.Txt_Value, 0);
            ((System.ComponentModel.ISupportInitialize)(this.Txt_Value)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.NumericUpDown Txt_Value;
    }
}
