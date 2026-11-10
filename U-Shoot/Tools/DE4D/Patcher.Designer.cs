namespace U_Shoot.Tools.DE4D
{
    partial class Patcher
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Patcher));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.Cbox_ErroMsg = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.Cbox_Crosshair = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.Btn_Patch = new System.Windows.Forms.Button();
            this.Btn_Close = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.Cbox_ErroMsg);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.Cbox_Crosshair);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(287, 100);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Patch Options :";
            // 
            // Cbox_ErroMsg
            // 
            this.Cbox_ErroMsg.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Cbox_ErroMsg.FormattingEnabled = true;
            this.Cbox_ErroMsg.Items.AddRange(new object[] {
            "DISABLE",
            "ENABLE"});
            this.Cbox_ErroMsg.Location = new System.Drawing.Point(168, 67);
            this.Cbox_ErroMsg.Name = "Cbox_ErroMsg";
            this.Cbox_ErroMsg.Size = new System.Drawing.Size(104, 21);
            this.Cbox_ErroMsg.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(7, 70);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(149, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "\"SENSOR ERROR\" message";
            // 
            // Cbox_Crosshair
            // 
            this.Cbox_Crosshair.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Cbox_Crosshair.FormattingEnabled = true;
            this.Cbox_Crosshair.Items.AddRange(new object[] {
            "DISABLE",
            "ENABLE"});
            this.Cbox_Crosshair.Location = new System.Drawing.Point(168, 37);
            this.Cbox_Crosshair.Name = "Cbox_Crosshair";
            this.Cbox_Crosshair.Size = new System.Drawing.Size(104, 21);
            this.Cbox_Crosshair.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 40);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(55, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Crosshairs";
            // 
            // Btn_Patch
            // 
            this.Btn_Patch.Location = new System.Drawing.Point(12, 134);
            this.Btn_Patch.Name = "Btn_Patch";
            this.Btn_Patch.Size = new System.Drawing.Size(224, 45);
            this.Btn_Patch.TabIndex = 1;
            this.Btn_Patch.Text = "Apply Patch";
            this.Btn_Patch.UseVisualStyleBackColor = true;
            this.Btn_Patch.Click += new System.EventHandler(this.Btn_Patch_Click);
            // 
            // Btn_Close
            // 
            this.Btn_Close.BackgroundImage = global::U_Shoot.Properties.Resources.Close_24px;
            this.Btn_Close.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.Btn_Close.Location = new System.Drawing.Point(254, 134);
            this.Btn_Close.Name = "Btn_Close";
            this.Btn_Close.Size = new System.Drawing.Size(45, 45);
            this.Btn_Close.TabIndex = 2;
            this.Btn_Close.UseVisualStyleBackColor = true;
            this.Btn_Close.Click += new System.EventHandler(this.Btn_Close_Click);
            // 
            // Patcher
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(312, 191);
            this.Controls.Add(this.Btn_Close);
            this.Controls.Add(this.Btn_Patch);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Patcher";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Dark Escape 4D - Patcher";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ComboBox Cbox_ErroMsg;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox Cbox_Crosshair;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button Btn_Patch;
        private System.Windows.Forms.Button Btn_Close;
    }
}