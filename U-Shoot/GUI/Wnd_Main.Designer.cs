namespace U_Shoot
{
    partial class Wnd_Main
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Wnd_Main));
            this.Flp_Games = new System.Windows.Forms.FlowLayoutPanel();
            this.Lbl_PlatformFilter = new System.Windows.Forms.Label();
            this.Cbox_Platform = new System.Windows.Forms.ComboBox();
            this.Txt_FilterName = new System.Windows.Forms.TextBox();
            this.Lbl_NameFilter = new System.Windows.Forms.Label();
            this.Btn_DefaultControls = new System.Windows.Forms.Button();
            this.Btn_Outputs = new System.Windows.Forms.Button();
            this.Btn_DefaultPath = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // Flp_Games
            // 
            this.Flp_Games.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Flp_Games.BackColor = System.Drawing.Color.White;
            this.Flp_Games.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Flp_Games.Location = new System.Drawing.Point(12, 33);
            this.Flp_Games.Margin = new System.Windows.Forms.Padding(0);
            this.Flp_Games.Name = "Flp_Games";
            this.Flp_Games.Size = new System.Drawing.Size(600, 355);
            this.Flp_Games.TabIndex = 4;
            this.Flp_Games.SizeChanged += new System.EventHandler(this.Flp_Games_SizeChanged);
            // 
            // Lbl_PlatformFilter
            // 
            this.Lbl_PlatformFilter.AutoSize = true;
            this.Lbl_PlatformFilter.Location = new System.Drawing.Point(12, 9);
            this.Lbl_PlatformFilter.Name = "Lbl_PlatformFilter";
            this.Lbl_PlatformFilter.Size = new System.Drawing.Size(51, 13);
            this.Lbl_PlatformFilter.TabIndex = 5;
            this.Lbl_PlatformFilter.Text = "Platform :";
            // 
            // Cbox_Platform
            // 
            this.Cbox_Platform.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Cbox_Platform.FormattingEnabled = true;
            this.Cbox_Platform.Items.AddRange(new object[] {
            "- ALL -"});
            this.Cbox_Platform.Location = new System.Drawing.Point(69, 6);
            this.Cbox_Platform.Name = "Cbox_Platform";
            this.Cbox_Platform.Size = new System.Drawing.Size(121, 21);
            this.Cbox_Platform.Sorted = true;
            this.Cbox_Platform.TabIndex = 6;
            this.Cbox_Platform.SelectedIndexChanged += new System.EventHandler(this.Cbox_Platform_SelectedIndexChanged);
            // 
            // Txt_FilterName
            // 
            this.Txt_FilterName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Txt_FilterName.Location = new System.Drawing.Point(474, 6);
            this.Txt_FilterName.Name = "Txt_FilterName";
            this.Txt_FilterName.Size = new System.Drawing.Size(138, 20);
            this.Txt_FilterName.TabIndex = 7;
            this.Txt_FilterName.TextChanged += new System.EventHandler(this.Txt_FilterName_TextChanged);
            // 
            // Lbl_NameFilter
            // 
            this.Lbl_NameFilter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Lbl_NameFilter.AutoSize = true;
            this.Lbl_NameFilter.Location = new System.Drawing.Point(427, 9);
            this.Lbl_NameFilter.Name = "Lbl_NameFilter";
            this.Lbl_NameFilter.Size = new System.Drawing.Size(41, 13);
            this.Lbl_NameFilter.TabIndex = 8;
            this.Lbl_NameFilter.Text = "Name :";
            // 
            // Btn_DefaultControls
            // 
            this.Btn_DefaultControls.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.Btn_DefaultControls.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.Btn_DefaultControls.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Btn_DefaultControls.Image = global::U_Shoot.Properties.Resources.controls_24px;
            this.Btn_DefaultControls.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.Btn_DefaultControls.Location = new System.Drawing.Point(12, 394);
            this.Btn_DefaultControls.Name = "Btn_DefaultControls";
            this.Btn_DefaultControls.Size = new System.Drawing.Size(120, 38);
            this.Btn_DefaultControls.TabIndex = 9;
            this.Btn_DefaultControls.Text = "Default Inputs";
            this.Btn_DefaultControls.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.Btn_DefaultControls.UseVisualStyleBackColor = true;
            this.Btn_DefaultControls.Click += new System.EventHandler(this.Btn_DefaultControls_Click);
            // 
            // Btn_Outputs
            // 
            this.Btn_Outputs.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.Btn_Outputs.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Btn_Outputs.Image = global::U_Shoot.Properties.Resources.rgb_24px;
            this.Btn_Outputs.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.Btn_Outputs.Location = new System.Drawing.Point(326, 394);
            this.Btn_Outputs.Name = "Btn_Outputs";
            this.Btn_Outputs.Size = new System.Drawing.Size(140, 38);
            this.Btn_Outputs.TabIndex = 10;
            this.Btn_Outputs.Text = "Outputs Settings";
            this.Btn_Outputs.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.Btn_Outputs.UseVisualStyleBackColor = true;
            this.Btn_Outputs.Click += new System.EventHandler(this.Btn_Outputs_Click);
            // 
            // Btn_DefaultPath
            // 
            this.Btn_DefaultPath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.Btn_DefaultPath.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Btn_DefaultPath.Image = ((System.Drawing.Image)(resources.GetObject("Btn_DefaultPath.Image")));
            this.Btn_DefaultPath.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.Btn_DefaultPath.Location = new System.Drawing.Point(138, 394);
            this.Btn_DefaultPath.Name = "Btn_DefaultPath";
            this.Btn_DefaultPath.Size = new System.Drawing.Size(120, 38);
            this.Btn_DefaultPath.TabIndex = 13;
            this.Btn_DefaultPath.Text = "Default Paths";
            this.Btn_DefaultPath.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.Btn_DefaultPath.UseVisualStyleBackColor = true;
            this.Btn_DefaultPath.Click += new System.EventHandler(this.Btn_DefaultPath_Click);
            // 
            // Wnd_Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(624, 441);
            this.Controls.Add(this.Btn_DefaultPath);
            this.Controls.Add(this.Btn_Outputs);
            this.Controls.Add(this.Btn_DefaultControls);
            this.Controls.Add(this.Lbl_NameFilter);
            this.Controls.Add(this.Txt_FilterName);
            this.Controls.Add(this.Cbox_Platform);
            this.Controls.Add(this.Lbl_PlatformFilter);
            this.Controls.Add(this.Flp_Games);
            this.DoubleBuffered = true;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(448, 39);
            this.Name = "Wnd_Main";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "U-Shoot GUI | v1.0";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Wnd_Main_FormClosed);
            this.Load += new System.EventHandler(this.Wnd_GuiMain_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.FlowLayoutPanel Flp_Games;
        private System.Windows.Forms.Label Lbl_PlatformFilter;
        private System.Windows.Forms.ComboBox Cbox_Platform;
        private System.Windows.Forms.TextBox Txt_FilterName;
        private System.Windows.Forms.Label Lbl_NameFilter;
        private System.Windows.Forms.Button Btn_DefaultControls;
        private System.Windows.Forms.Button Btn_Outputs;
        private System.Windows.Forms.Button Btn_DefaultPath;
    }
}