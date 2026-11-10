namespace U_Shoot.GUI.Controls
{
    partial class Ctrl_Game
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
            this.components = new System.ComponentModel.Container();
            this.Lbl_GameName = new System.Windows.Forms.Label();
            this.Btn_Controls = new System.Windows.Forms.Button();
            this.Btn_Options = new System.Windows.Forms.Button();
            this.Btn_Run = new System.Windows.Forms.Button();
            this.Pnl_Separator = new System.Windows.Forms.Panel();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            // 
            // Lbl_GameName
            // 
            this.Lbl_GameName.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Lbl_GameName.Location = new System.Drawing.Point(10, 0);
            this.Lbl_GameName.Name = "Lbl_GameName";
            this.Lbl_GameName.Size = new System.Drawing.Size(259, 30);
            this.Lbl_GameName.TabIndex = 0;
            this.Lbl_GameName.Text = "label1";
            this.Lbl_GameName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Btn_Controls
            // 
            this.Btn_Controls.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Btn_Controls.BackgroundImage = global::U_Shoot.Properties.Resources.Controls_16px;
            this.Btn_Controls.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.Btn_Controls.Location = new System.Drawing.Point(382, 3);
            this.Btn_Controls.Name = "Btn_Controls";
            this.Btn_Controls.Size = new System.Drawing.Size(24, 24);
            this.Btn_Controls.TabIndex = 1;
            this.Btn_Controls.UseVisualStyleBackColor = true;
            this.Btn_Controls.Click += new System.EventHandler(this.Btn_Controls_Click);
            this.Btn_Controls.MouseHover += new System.EventHandler(this.Btn_Controls_MouseHover);
            // 
            // Btn_Options
            // 
            this.Btn_Options.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Btn_Options.BackgroundImage = global::U_Shoot.Properties.Resources.Options_16px;
            this.Btn_Options.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.Btn_Options.Location = new System.Drawing.Point(412, 3);
            this.Btn_Options.Name = "Btn_Options";
            this.Btn_Options.Size = new System.Drawing.Size(24, 24);
            this.Btn_Options.TabIndex = 2;
            this.Btn_Options.UseVisualStyleBackColor = true;
            this.Btn_Options.Click += new System.EventHandler(this.Btn_Options_Click);
            this.Btn_Options.MouseHover += new System.EventHandler(this.Btn_Options_MouseHover);
            // 
            // Btn_Run
            // 
            this.Btn_Run.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Btn_Run.BackgroundImage = global::U_Shoot.Properties.Resources.Start_2_16px;
            this.Btn_Run.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.Btn_Run.Location = new System.Drawing.Point(468, 3);
            this.Btn_Run.Name = "Btn_Run";
            this.Btn_Run.Size = new System.Drawing.Size(24, 24);
            this.Btn_Run.TabIndex = 3;
            this.Btn_Run.UseVisualStyleBackColor = true;
            this.Btn_Run.Click += new System.EventHandler(this.Btn_Run_Click);
            this.Btn_Run.MouseHover += new System.EventHandler(this.Btn_Run_MouseHover);
            // 
            // Pnl_Separator
            // 
            this.Pnl_Separator.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Pnl_Separator.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Pnl_Separator.Location = new System.Drawing.Point(3, 31);
            this.Pnl_Separator.Name = "Pnl_Separator";
            this.Pnl_Separator.Size = new System.Drawing.Size(499, 1);
            this.Pnl_Separator.TabIndex = 4;
            // 
            // Ctrl_Game
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.Pnl_Separator);
            this.Controls.Add(this.Btn_Run);
            this.Controls.Add(this.Btn_Options);
            this.Controls.Add(this.Btn_Controls);
            this.Controls.Add(this.Lbl_GameName);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "Ctrl_Game";
            this.Size = new System.Drawing.Size(505, 32);
            this.Load += new System.EventHandler(this.Ctrl_Game_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label Lbl_GameName;
        private System.Windows.Forms.Button Btn_Controls;
        private System.Windows.Forms.Button Btn_Options;
        private System.Windows.Forms.Button Btn_Run;
        private System.Windows.Forms.Panel Pnl_Separator;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}
