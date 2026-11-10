namespace U_Shoot.GUI.Controls
{
    partial class Ctrl_InputButton
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
            this.Lbl_ButtonName = new System.Windows.Forms.Label();
            this.FLP_Input = new System.Windows.Forms.FlowLayoutPanel();
            this.Pnl_Expand = new System.Windows.Forms.Panel();
            this.Btn_Expand = new System.Windows.Forms.Button();
            this.Btn_AddEntry = new System.Windows.Forms.Button();
            this.Pnl_DefaultConfig = new System.Windows.Forms.Panel();
            this.Pnl_Expand.SuspendLayout();
            this.SuspendLayout();
            // 
            // Lbl_ButtonName
            // 
            this.Lbl_ButtonName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Lbl_ButtonName.BackColor = System.Drawing.Color.White;
            this.Lbl_ButtonName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Lbl_ButtonName.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Lbl_ButtonName.ForeColor = System.Drawing.Color.Black;
            this.Lbl_ButtonName.Location = new System.Drawing.Point(25, 0);
            this.Lbl_ButtonName.Name = "Lbl_ButtonName";
            this.Lbl_ButtonName.Padding = new System.Windows.Forms.Padding(25, 0, 0, 0);
            this.Lbl_ButtonName.Size = new System.Drawing.Size(357, 26);
            this.Lbl_ButtonName.TabIndex = 0;
            this.Lbl_ButtonName.Text = "label1";
            this.Lbl_ButtonName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // FLP_Input
            // 
            this.FLP_Input.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.FLP_Input.BackColor = System.Drawing.Color.White;
            this.FLP_Input.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.FLP_Input.Location = new System.Drawing.Point(25, 25);
            this.FLP_Input.Name = "FLP_Input";
            this.FLP_Input.Size = new System.Drawing.Size(357, 68);
            this.FLP_Input.TabIndex = 3;
            this.FLP_Input.SizeChanged += new System.EventHandler(this.FLP_Input_SizeChanged);
            // 
            // Pnl_Expand
            // 
            this.Pnl_Expand.BackColor = System.Drawing.Color.White;
            this.Pnl_Expand.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Pnl_Expand.Controls.Add(this.Btn_Expand);
            this.Pnl_Expand.Location = new System.Drawing.Point(0, 0);
            this.Pnl_Expand.Name = "Pnl_Expand";
            this.Pnl_Expand.Size = new System.Drawing.Size(26, 26);
            this.Pnl_Expand.TabIndex = 5;
            // 
            // Btn_Expand
            // 
            this.Btn_Expand.BackColor = System.Drawing.Color.White;
            this.Btn_Expand.BackgroundImage = global::U_Shoot.Properties.Resources.ExpandPlusGrey_16px;
            this.Btn_Expand.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.Btn_Expand.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Btn_Expand.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.Btn_Expand.FlatAppearance.BorderSize = 0;
            this.Btn_Expand.FlatAppearance.MouseDownBackColor = System.Drawing.Color.White;
            this.Btn_Expand.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Lavender;
            this.Btn_Expand.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.Btn_Expand.Location = new System.Drawing.Point(0, 0);
            this.Btn_Expand.Name = "Btn_Expand";
            this.Btn_Expand.Size = new System.Drawing.Size(24, 24);
            this.Btn_Expand.TabIndex = 4;
            this.Btn_Expand.UseVisualStyleBackColor = false;
            this.Btn_Expand.Click += new System.EventHandler(this.Btn_Expand_Click);
            // 
            // Btn_AddEntry
            // 
            this.Btn_AddEntry.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Btn_AddEntry.BackColor = System.Drawing.Color.White;
            this.Btn_AddEntry.BackgroundImage = global::U_Shoot.Properties.Resources.AddGreen_16px;
            this.Btn_AddEntry.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.Btn_AddEntry.FlatAppearance.BorderSize = 0;
            this.Btn_AddEntry.FlatAppearance.MouseDownBackColor = System.Drawing.Color.White;
            this.Btn_AddEntry.FlatAppearance.MouseOverBackColor = System.Drawing.Color.MediumSeaGreen;
            this.Btn_AddEntry.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.Btn_AddEntry.Location = new System.Drawing.Point(359, 3);
            this.Btn_AddEntry.Name = "Btn_AddEntry";
            this.Btn_AddEntry.Size = new System.Drawing.Size(20, 20);
            this.Btn_AddEntry.TabIndex = 2;
            this.Btn_AddEntry.UseVisualStyleBackColor = false;
            this.Btn_AddEntry.Click += new System.EventHandler(this.Btn_AddEntry_Click);
            // 
            // Pnl_DefaultConfig
            // 
            this.Pnl_DefaultConfig.BackColor = System.Drawing.Color.White;
            this.Pnl_DefaultConfig.BackgroundImage = global::U_Shoot.Properties.Resources.start_16px;
            this.Pnl_DefaultConfig.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.Pnl_DefaultConfig.Location = new System.Drawing.Point(32, 3);
            this.Pnl_DefaultConfig.Margin = new System.Windows.Forms.Padding(2);
            this.Pnl_DefaultConfig.Name = "Pnl_DefaultConfig";
            this.Pnl_DefaultConfig.Size = new System.Drawing.Size(19, 20);
            this.Pnl_DefaultConfig.TabIndex = 0;
            this.Pnl_DefaultConfig.Visible = false;
            // 
            // Ctrl_InputButton
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.Controls.Add(this.Pnl_DefaultConfig);
            this.Controls.Add(this.Pnl_Expand);
            this.Controls.Add(this.FLP_Input);
            this.Controls.Add(this.Btn_AddEntry);
            this.Controls.Add(this.Lbl_ButtonName);
            this.Name = "Ctrl_InputButton";
            this.Size = new System.Drawing.Size(382, 97);
            this.Load += new System.EventHandler(this.Ctrl_InputButton_Load);
            this.Pnl_Expand.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label Lbl_ButtonName;
        private System.Windows.Forms.Button Btn_AddEntry;
        private System.Windows.Forms.FlowLayoutPanel FLP_Input;
        private System.Windows.Forms.Button Btn_Expand;
        private System.Windows.Forms.Panel Pnl_Expand;
        private System.Windows.Forms.Panel Pnl_DefaultConfig;
    }
}
