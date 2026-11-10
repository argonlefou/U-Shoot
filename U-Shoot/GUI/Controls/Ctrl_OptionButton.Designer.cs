namespace U_Shoot.GUI.Controls
{
    partial class Ctrl_OptionButton
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
            this.Btn_Option = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // Btn_Option
            // 
            this.Btn_Option.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Btn_Option.Location = new System.Drawing.Point(202, 5);
            this.Btn_Option.Name = "Btn_Option";
            this.Btn_Option.Size = new System.Drawing.Size(75, 23);
            this.Btn_Option.TabIndex = 6;
            this.Btn_Option.Text = "button1";
            this.Btn_Option.UseVisualStyleBackColor = true;
            this.Btn_Option.Click += new System.EventHandler(this.Btn_Option_Click);
            // 
            // Ctrl_OptionButton
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.Btn_Option);
            this.Name = "Ctrl_OptionButton";
            this.Controls.SetChildIndex(this.Lbl_OptionName, 0);
            this.Controls.SetChildIndex(this.Btn_Option, 0);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button Btn_Option;
    }
}
