namespace U_Shoot.Tools
{
    partial class BaseUnityConfig
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BaseUnityConfig));
            this.Btn_Save = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.Txt_ResHeight = new System.Windows.Forms.TextBox();
            this.Txt_ResWidth = new System.Windows.Forms.TextBox();
            this.Cbox_FullScreen = new System.Windows.Forms.CheckBox();
            this.label4 = new System.Windows.Forms.Label();
            this.Cbox_ForceResolution = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.Chk_LocalSave = new System.Windows.Forms.CheckBox();
            this.Lbl_LocalSave = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // Btn_Save
            // 
            this.Btn_Save.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Btn_Save.Image = global::U_Shoot.Properties.Resources.Save_32px;
            this.Btn_Save.Location = new System.Drawing.Point(12, 258);
            this.Btn_Save.Name = "Btn_Save";
            this.Btn_Save.Size = new System.Drawing.Size(221, 40);
            this.Btn_Save.TabIndex = 90;
            this.Btn_Save.UseVisualStyleBackColor = true;
            this.Btn_Save.Click += new System.EventHandler(this.Btn_Save_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.Txt_ResHeight);
            this.groupBox1.Controls.Add(this.Txt_ResWidth);
            this.groupBox1.Controls.Add(this.Cbox_FullScreen);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.Cbox_ForceResolution);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(221, 154);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Video";
            // 
            // Txt_ResHeight
            // 
            this.Txt_ResHeight.Location = new System.Drawing.Point(125, 101);
            this.Txt_ResHeight.Name = "Txt_ResHeight";
            this.Txt_ResHeight.Size = new System.Drawing.Size(50, 20);
            this.Txt_ResHeight.TabIndex = 3;
            this.Txt_ResHeight.Text = "1080";
            this.Txt_ResHeight.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.Txt_ResHeight.Click += new System.EventHandler(this.Txt_Res_Enter);
            this.Txt_ResHeight.Enter += new System.EventHandler(this.Txt_Res_Enter);
            // 
            // Txt_ResWidth
            // 
            this.Txt_ResWidth.Location = new System.Drawing.Point(125, 75);
            this.Txt_ResWidth.Name = "Txt_ResWidth";
            this.Txt_ResWidth.Size = new System.Drawing.Size(50, 20);
            this.Txt_ResWidth.TabIndex = 2;
            this.Txt_ResWidth.Text = "1920";
            this.Txt_ResWidth.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.Txt_ResWidth.Click += new System.EventHandler(this.Txt_Res_Enter);
            this.Txt_ResWidth.Enter += new System.EventHandler(this.Txt_Res_Enter);
            // 
            // Cbox_FullScreen
            // 
            this.Cbox_FullScreen.AutoSize = true;
            this.Cbox_FullScreen.Checked = true;
            this.Cbox_FullScreen.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Cbox_FullScreen.Location = new System.Drawing.Point(125, 127);
            this.Cbox_FullScreen.Name = "Cbox_FullScreen";
            this.Cbox_FullScreen.Size = new System.Drawing.Size(15, 14);
            this.Cbox_FullScreen.TabIndex = 4;
            this.Cbox_FullScreen.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(8, 127);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(55, 13);
            this.label4.TabIndex = 3;
            this.label4.Text = "Fullscreen";
            // 
            // Cbox_ForceResolution
            // 
            this.Cbox_ForceResolution.AutoSize = true;
            this.Cbox_ForceResolution.Location = new System.Drawing.Point(125, 38);
            this.Cbox_ForceResolution.Name = "Cbox_ForceResolution";
            this.Cbox_ForceResolution.Size = new System.Drawing.Size(15, 14);
            this.Cbox_ForceResolution.TabIndex = 1;
            this.Cbox_ForceResolution.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(8, 104);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(91, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "Resolution Height";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(7, 78);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(88, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Resolution Width";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 38);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(87, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Force Resolution";
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.Chk_LocalSave);
            this.groupBox2.Controls.Add(this.Lbl_LocalSave);
            this.groupBox2.Location = new System.Drawing.Point(12, 172);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(221, 64);
            this.groupBox2.TabIndex = 4;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "System";
            // 
            // Chk_LocalSave
            // 
            this.Chk_LocalSave.AutoSize = true;
            this.Chk_LocalSave.Checked = true;
            this.Chk_LocalSave.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Chk_LocalSave.Enabled = false;
            this.Chk_LocalSave.Location = new System.Drawing.Point(124, 36);
            this.Chk_LocalSave.Name = "Chk_LocalSave";
            this.Chk_LocalSave.Size = new System.Drawing.Size(15, 14);
            this.Chk_LocalSave.TabIndex = 5;
            this.Chk_LocalSave.UseVisualStyleBackColor = true;
            // 
            // Lbl_LocalSave
            // 
            this.Lbl_LocalSave.AutoSize = true;
            this.Lbl_LocalSave.Enabled = false;
            this.Lbl_LocalSave.Location = new System.Drawing.Point(7, 36);
            this.Lbl_LocalSave.Name = "Lbl_LocalSave";
            this.Lbl_LocalSave.Size = new System.Drawing.Size(59, 13);
            this.Lbl_LocalSave.TabIndex = 0;
            this.Lbl_LocalSave.Text = "Local save";
            // 
            // BaseUnityConfig
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(245, 310);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.Btn_Save);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "BaseUnityConfig";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "BaseUnityConfig";
            this.Load += new System.EventHandler(this.BaseUnityConfig_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        protected System.Windows.Forms.Button Btn_Save;
        protected System.Windows.Forms.GroupBox groupBox1;
        protected System.Windows.Forms.TextBox Txt_ResHeight;
        protected System.Windows.Forms.TextBox Txt_ResWidth;
        protected System.Windows.Forms.CheckBox Cbox_FullScreen;
        protected System.Windows.Forms.Label label4;
        protected System.Windows.Forms.CheckBox Cbox_ForceResolution;
        protected System.Windows.Forms.Label label3;
        protected System.Windows.Forms.Label label2;
        protected System.Windows.Forms.Label label1;
        protected System.Windows.Forms.GroupBox groupBox2;
        protected System.Windows.Forms.CheckBox Chk_LocalSave;
        protected System.Windows.Forms.Label Lbl_LocalSave;
    }
}