namespace U_Shoot.GUI
{
    partial class Wnd_DefaultPath
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Wnd_DefaultPath));
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.Btn_Flycast = new System.Windows.Forms.Button();
            this.Txt_Flycast = new System.Windows.Forms.TextBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.Txt_LinuxLoader = new System.Windows.Forms.TextBox();
            this.Btn_LinuxLoader = new System.Windows.Forms.Button();
            this.Btn_Save = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.Txt_Rpcs3Emulator = new System.Windows.Forms.TextBox();
            this.Btn_Rpcs3Emulator = new System.Windows.Forms.Button();
            this.pictureBox4 = new System.Windows.Forms.PictureBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.Txt_Pcsx6x6Emulator = new System.Windows.Forms.TextBox();
            this.Btn_Pcsx2x6Emulator = new System.Windows.Forms.Button();
            this.pictureBox3 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox4)).BeginInit();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).BeginInit();
            this.SuspendLayout();
            // 
            // Btn_Flycast
            // 
            this.Btn_Flycast.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Btn_Flycast.Location = new System.Drawing.Point(392, 18);
            this.Btn_Flycast.Margin = new System.Windows.Forms.Padding(2);
            this.Btn_Flycast.Name = "Btn_Flycast";
            this.Btn_Flycast.Size = new System.Drawing.Size(25, 22);
            this.Btn_Flycast.TabIndex = 3;
            this.Btn_Flycast.Text = "...";
            this.Btn_Flycast.UseVisualStyleBackColor = true;
            this.Btn_Flycast.Click += new System.EventHandler(this.Btn_Flycast_Click);
            // 
            // Txt_Flycast
            // 
            this.Txt_Flycast.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Txt_Flycast.Location = new System.Drawing.Point(32, 20);
            this.Txt_Flycast.Name = "Txt_Flycast";
            this.Txt_Flycast.Size = new System.Drawing.Size(355, 20);
            this.Txt_Flycast.TabIndex = 2;
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pictureBox1.BackgroundImage")));
            this.pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pictureBox1.Location = new System.Drawing.Point(6, 20);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(20, 20);
            this.pictureBox1.TabIndex = 17;
            this.pictureBox1.TabStop = false;
            // 
            // pictureBox2
            // 
            this.pictureBox2.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pictureBox2.BackgroundImage")));
            this.pictureBox2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pictureBox2.Location = new System.Drawing.Point(6, 20);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(20, 20);
            this.pictureBox2.TabIndex = 18;
            this.pictureBox2.TabStop = false;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.Txt_Flycast);
            this.groupBox1.Controls.Add(this.Btn_Flycast);
            this.groupBox1.Controls.Add(this.pictureBox2);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(422, 54);
            this.groupBox1.TabIndex = 10;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Flycast :";
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.Txt_LinuxLoader);
            this.groupBox2.Controls.Add(this.Btn_LinuxLoader);
            this.groupBox2.Controls.Add(this.pictureBox1);
            this.groupBox2.Location = new System.Drawing.Point(12, 72);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(422, 54);
            this.groupBox2.TabIndex = 11;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Linux-Loader :";
            // 
            // Txt_LinuxLoader
            // 
            this.Txt_LinuxLoader.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Txt_LinuxLoader.Location = new System.Drawing.Point(32, 20);
            this.Txt_LinuxLoader.Name = "Txt_LinuxLoader";
            this.Txt_LinuxLoader.Size = new System.Drawing.Size(355, 20);
            this.Txt_LinuxLoader.TabIndex = 4;
            // 
            // Btn_LinuxLoader
            // 
            this.Btn_LinuxLoader.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Btn_LinuxLoader.Location = new System.Drawing.Point(392, 18);
            this.Btn_LinuxLoader.Margin = new System.Windows.Forms.Padding(2);
            this.Btn_LinuxLoader.Name = "Btn_LinuxLoader";
            this.Btn_LinuxLoader.Size = new System.Drawing.Size(25, 22);
            this.Btn_LinuxLoader.TabIndex = 5;
            this.Btn_LinuxLoader.Text = "...";
            this.Btn_LinuxLoader.UseVisualStyleBackColor = true;
            this.Btn_LinuxLoader.Click += new System.EventHandler(this.Btn_LinuxLoader_Click);
            // 
            // Btn_Save
            // 
            this.Btn_Save.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Btn_Save.BackgroundImage = global::U_Shoot.Properties.Resources.Save_32px;
            this.Btn_Save.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.Btn_Save.Location = new System.Drawing.Point(12, 307);
            this.Btn_Save.Margin = new System.Windows.Forms.Padding(2);
            this.Btn_Save.Name = "Btn_Save";
            this.Btn_Save.Size = new System.Drawing.Size(422, 36);
            this.Btn_Save.TabIndex = 8;
            this.Btn_Save.UseVisualStyleBackColor = true;
            this.Btn_Save.Click += new System.EventHandler(this.Btn_Save_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // groupBox4
            // 
            this.groupBox4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox4.Controls.Add(this.Txt_Rpcs3Emulator);
            this.groupBox4.Controls.Add(this.Btn_Rpcs3Emulator);
            this.groupBox4.Controls.Add(this.pictureBox4);
            this.groupBox4.Location = new System.Drawing.Point(12, 192);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(422, 54);
            this.groupBox4.TabIndex = 20;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "RPCS3/System357 Emulator :";
            // 
            // Txt_Rpcs3Emulator
            // 
            this.Txt_Rpcs3Emulator.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Txt_Rpcs3Emulator.Location = new System.Drawing.Point(32, 20);
            this.Txt_Rpcs3Emulator.Name = "Txt_Rpcs3Emulator";
            this.Txt_Rpcs3Emulator.Size = new System.Drawing.Size(355, 20);
            this.Txt_Rpcs3Emulator.TabIndex = 6;
            // 
            // Btn_Rpcs3Emulator
            // 
            this.Btn_Rpcs3Emulator.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Btn_Rpcs3Emulator.Location = new System.Drawing.Point(392, 18);
            this.Btn_Rpcs3Emulator.Margin = new System.Windows.Forms.Padding(2);
            this.Btn_Rpcs3Emulator.Name = "Btn_Rpcs3Emulator";
            this.Btn_Rpcs3Emulator.Size = new System.Drawing.Size(25, 22);
            this.Btn_Rpcs3Emulator.TabIndex = 7;
            this.Btn_Rpcs3Emulator.Text = "...";
            this.Btn_Rpcs3Emulator.UseVisualStyleBackColor = true;
            this.Btn_Rpcs3Emulator.Click += new System.EventHandler(this.Btn_Rpcs3Emulator_Click);
            // 
            // pictureBox4
            // 
            this.pictureBox4.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pictureBox4.BackgroundImage")));
            this.pictureBox4.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pictureBox4.Location = new System.Drawing.Point(6, 20);
            this.pictureBox4.Name = "pictureBox4";
            this.pictureBox4.Size = new System.Drawing.Size(20, 20);
            this.pictureBox4.TabIndex = 19;
            this.pictureBox4.TabStop = false;
            // 
            // groupBox3
            // 
            this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox3.Controls.Add(this.Txt_Pcsx6x6Emulator);
            this.groupBox3.Controls.Add(this.Btn_Pcsx2x6Emulator);
            this.groupBox3.Controls.Add(this.pictureBox3);
            this.groupBox3.Location = new System.Drawing.Point(12, 132);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(422, 54);
            this.groupBox3.TabIndex = 21;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "PCSX2X6 :";
            // 
            // Txt_Pcsx6x6Emulator
            // 
            this.Txt_Pcsx6x6Emulator.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Txt_Pcsx6x6Emulator.Location = new System.Drawing.Point(32, 20);
            this.Txt_Pcsx6x6Emulator.Name = "Txt_Pcsx6x6Emulator";
            this.Txt_Pcsx6x6Emulator.Size = new System.Drawing.Size(355, 20);
            this.Txt_Pcsx6x6Emulator.TabIndex = 6;
            // 
            // Btn_Pcsx2x6Emulator
            // 
            this.Btn_Pcsx2x6Emulator.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Btn_Pcsx2x6Emulator.Location = new System.Drawing.Point(392, 18);
            this.Btn_Pcsx2x6Emulator.Margin = new System.Windows.Forms.Padding(2);
            this.Btn_Pcsx2x6Emulator.Name = "Btn_Pcsx2x6Emulator";
            this.Btn_Pcsx2x6Emulator.Size = new System.Drawing.Size(25, 22);
            this.Btn_Pcsx2x6Emulator.TabIndex = 7;
            this.Btn_Pcsx2x6Emulator.Text = "...";
            this.Btn_Pcsx2x6Emulator.UseVisualStyleBackColor = true;
            this.Btn_Pcsx2x6Emulator.Click += new System.EventHandler(this.Btn_Pcsx2x6Emulator_Click);
            // 
            // pictureBox3
            // 
            this.pictureBox3.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pictureBox3.BackgroundImage")));
            this.pictureBox3.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pictureBox3.Location = new System.Drawing.Point(6, 20);
            this.pictureBox3.Name = "pictureBox3";
            this.pictureBox3.Size = new System.Drawing.Size(20, 20);
            this.pictureBox3.TabIndex = 19;
            this.pictureBox3.TabStop = false;
            // 
            // Wnd_DefaultPath
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(446, 354);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.Btn_Save);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Wnd_DefaultPath";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Select default emulator path :";
            this.Load += new System.EventHandler(this.Wnd_DefaultPath_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox4)).EndInit();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.Button Btn_Flycast;
        private System.Windows.Forms.TextBox Txt_Flycast;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TextBox Txt_LinuxLoader;
        private System.Windows.Forms.Button Btn_LinuxLoader;
        private System.Windows.Forms.Button Btn_Save;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.TextBox Txt_Rpcs3Emulator;
        private System.Windows.Forms.Button Btn_Rpcs3Emulator;
        private System.Windows.Forms.PictureBox pictureBox4;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.TextBox Txt_Pcsx6x6Emulator;
        private System.Windows.Forms.Button Btn_Pcsx2x6Emulator;
        private System.Windows.Forms.PictureBox pictureBox3;
    }
}