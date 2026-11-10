namespace U_Shoot.GUI.Controls
{
    partial class Ctrl_InputAxisItem
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
            this.Pnl_Icon = new System.Windows.Forms.Panel();
            this.Btn_Remove = new System.Windows.Forms.Button();
            this.Pnl_Button = new System.Windows.Forms.Panel();
            this.Lbl_DeviceName = new System.Windows.Forms.Label();
            this.Btn_Options = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // Pnl_Icon
            // 
            this.Pnl_Icon.BackgroundImage = global::U_Shoot.Properties.Resources.game_controller;
            this.Pnl_Icon.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.Pnl_Icon.Location = new System.Drawing.Point(6, 3);
            this.Pnl_Icon.Name = "Pnl_Icon";
            this.Pnl_Icon.Size = new System.Drawing.Size(20, 20);
            this.Pnl_Icon.TabIndex = 15;
            // 
            // Btn_Remove
            // 
            this.Btn_Remove.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Btn_Remove.BackgroundImage = global::U_Shoot.Properties.Resources.RemoveRed_16px;
            this.Btn_Remove.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.Btn_Remove.FlatAppearance.BorderSize = 0;
            this.Btn_Remove.FlatAppearance.MouseDownBackColor = System.Drawing.Color.White;
            this.Btn_Remove.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Crimson;
            this.Btn_Remove.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.Btn_Remove.Location = new System.Drawing.Point(420, 3);
            this.Btn_Remove.Name = "Btn_Remove";
            this.Btn_Remove.Size = new System.Drawing.Size(20, 20);
            this.Btn_Remove.TabIndex = 13;
            this.Btn_Remove.UseVisualStyleBackColor = true;
            this.Btn_Remove.Click += new System.EventHandler(this.Btn_Remove_Click);
            // 
            // Pnl_Button
            // 
            this.Pnl_Button.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Pnl_Button.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Pnl_Button.Location = new System.Drawing.Point(1, 25);
            this.Pnl_Button.Name = "Pnl_Button";
            this.Pnl_Button.Size = new System.Drawing.Size(442, 1);
            this.Pnl_Button.TabIndex = 12;
            this.Pnl_Button.Visible = false;
            // 
            // Lbl_DeviceName
            // 
            this.Lbl_DeviceName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Lbl_DeviceName.Location = new System.Drawing.Point(31, 0);
            this.Lbl_DeviceName.Name = "Lbl_DeviceName";
            this.Lbl_DeviceName.Size = new System.Drawing.Size(238, 26);
            this.Lbl_DeviceName.TabIndex = 14;
            this.Lbl_DeviceName.Text = "Device Name";
            this.Lbl_DeviceName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Btn_Options
            // 
            this.Btn_Options.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Btn_Options.BackgroundImage = global::U_Shoot.Properties.Resources.Options_16px;
            this.Btn_Options.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.Btn_Options.Location = new System.Drawing.Point(394, 3);
            this.Btn_Options.Name = "Btn_Options";
            this.Btn_Options.Size = new System.Drawing.Size(20, 20);
            this.Btn_Options.TabIndex = 16;
            this.Btn_Options.UseVisualStyleBackColor = true;
            this.Btn_Options.Click += new System.EventHandler(this.Btn_Options_Click);
            // 
            // Ctrl_InputAxisItem
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.Btn_Options);
            this.Controls.Add(this.Pnl_Button);
            this.Controls.Add(this.Pnl_Icon);
            this.Controls.Add(this.Btn_Remove);
            this.Controls.Add(this.Lbl_DeviceName);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "Ctrl_InputAxisItem";
            this.Size = new System.Drawing.Size(442, 26);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel Pnl_Icon;
        private System.Windows.Forms.Button Btn_Remove;
        private System.Windows.Forms.Panel Pnl_Button;
        private System.Windows.Forms.Label Lbl_DeviceName;
        private System.Windows.Forms.Button Btn_Options;
    }
}
