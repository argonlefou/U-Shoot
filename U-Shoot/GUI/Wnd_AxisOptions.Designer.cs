namespace U_Shoot.GUI
{
    partial class Wnd_AxisOptions
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Wnd_AxisOptions));
            this.Chk_InvertY = new System.Windows.Forms.CheckBox();
            this.Chk_InvertX = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.Cbox_HID_YAxis = new System.Windows.Forms.ComboBox();
            this.label29 = new System.Windows.Forms.Label();
            this.Cbox_HID_XAxis = new System.Windows.Forms.ComboBox();
            this.Gbox_P1_Calib = new System.Windows.Forms.GroupBox();
            this.Btn_Init_Calib = new System.Windows.Forms.Button();
            this.Btn_Stop_Calib = new System.Windows.Forms.Button();
            this.Btn_Start_Calib = new System.Windows.Forms.Button();
            this.label28 = new System.Windows.Forms.Label();
            this.Txt_Calib_Ymax = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.Txt_Calib_Ymin = new System.Windows.Forms.TextBox();
            this.label32 = new System.Windows.Forms.Label();
            this.Txt_Calib_Xmax = new System.Windows.Forms.TextBox();
            this.label36 = new System.Windows.Forms.Label();
            this.Txt_Calib_Xmin = new System.Windows.Forms.TextBox();
            this.Chk_Calibration = new System.Windows.Forms.CheckBox();
            this.Pnl_Separator = new System.Windows.Forms.Panel();
            this.Btn_Save = new System.Windows.Forms.Button();
            this.Lbl_Manufacturer = new System.Windows.Forms.Label();
            this.Lbl_Product = new System.Windows.Forms.Label();
            this.Pnl_Separator0 = new System.Windows.Forms.Panel();
            this.Btn_Cancel = new System.Windows.Forms.Button();
            this.Gbox_P1_Calib.SuspendLayout();
            this.SuspendLayout();
            // 
            // Chk_InvertY
            // 
            this.Chk_InvertY.AutoSize = true;
            this.Chk_InvertY.Location = new System.Drawing.Point(228, 132);
            this.Chk_InvertY.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Chk_InvertY.Name = "Chk_InvertY";
            this.Chk_InvertY.Size = new System.Drawing.Size(86, 20);
            this.Chk_InvertY.TabIndex = 54;
            this.Chk_InvertY.Text = "Invert Axis";
            this.Chk_InvertY.UseVisualStyleBackColor = true;
            // 
            // Chk_InvertX
            // 
            this.Chk_InvertX.AutoSize = true;
            this.Chk_InvertX.Location = new System.Drawing.Point(228, 100);
            this.Chk_InvertX.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Chk_InvertX.Name = "Chk_InvertX";
            this.Chk_InvertX.Size = new System.Drawing.Size(86, 20);
            this.Chk_InvertX.TabIndex = 53;
            this.Chk_InvertX.Text = "Invert Axis";
            this.Chk_InvertX.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(21, 133);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(91, 16);
            this.label1.TabIndex = 52;
            this.label1.Text = "Select Y Axis :";
            // 
            // Cbox_HID_YAxis
            // 
            this.Cbox_HID_YAxis.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Cbox_HID_YAxis.FormattingEnabled = true;
            this.Cbox_HID_YAxis.Items.AddRange(new object[] {
            "X",
            "Y",
            "Z",
            "Rx",
            "Ry",
            "Rz"});
            this.Cbox_HID_YAxis.Location = new System.Drawing.Point(120, 128);
            this.Cbox_HID_YAxis.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Cbox_HID_YAxis.Name = "Cbox_HID_YAxis";
            this.Cbox_HID_YAxis.Size = new System.Drawing.Size(75, 24);
            this.Cbox_HID_YAxis.TabIndex = 51;
            // 
            // label29
            // 
            this.label29.AutoSize = true;
            this.label29.Location = new System.Drawing.Point(22, 98);
            this.label29.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label29.Name = "label29";
            this.label29.Size = new System.Drawing.Size(90, 16);
            this.label29.TabIndex = 50;
            this.label29.Text = "Select X Axis :";
            // 
            // Cbox_HID_XAxis
            // 
            this.Cbox_HID_XAxis.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Cbox_HID_XAxis.FormattingEnabled = true;
            this.Cbox_HID_XAxis.Items.AddRange(new object[] {
            "X",
            "Y",
            "Z",
            "Rx",
            "Ry",
            "Rz"});
            this.Cbox_HID_XAxis.Location = new System.Drawing.Point(120, 95);
            this.Cbox_HID_XAxis.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Cbox_HID_XAxis.Name = "Cbox_HID_XAxis";
            this.Cbox_HID_XAxis.Size = new System.Drawing.Size(75, 24);
            this.Cbox_HID_XAxis.TabIndex = 49;
            // 
            // Gbox_P1_Calib
            // 
            this.Gbox_P1_Calib.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.Gbox_P1_Calib.Controls.Add(this.Btn_Init_Calib);
            this.Gbox_P1_Calib.Controls.Add(this.Btn_Stop_Calib);
            this.Gbox_P1_Calib.Controls.Add(this.Btn_Start_Calib);
            this.Gbox_P1_Calib.Controls.Add(this.label28);
            this.Gbox_P1_Calib.Controls.Add(this.Txt_Calib_Ymax);
            this.Gbox_P1_Calib.Controls.Add(this.label2);
            this.Gbox_P1_Calib.Controls.Add(this.Txt_Calib_Ymin);
            this.Gbox_P1_Calib.Controls.Add(this.label32);
            this.Gbox_P1_Calib.Controls.Add(this.Txt_Calib_Xmax);
            this.Gbox_P1_Calib.Controls.Add(this.label36);
            this.Gbox_P1_Calib.Controls.Add(this.Txt_Calib_Xmin);
            this.Gbox_P1_Calib.Controls.Add(this.Chk_Calibration);
            this.Gbox_P1_Calib.Location = new System.Drawing.Point(20, 204);
            this.Gbox_P1_Calib.Margin = new System.Windows.Forms.Padding(5, 5, 5, 5);
            this.Gbox_P1_Calib.Name = "Gbox_P1_Calib";
            this.Gbox_P1_Calib.Padding = new System.Windows.Forms.Padding(5, 5, 5, 5);
            this.Gbox_P1_Calib.Size = new System.Drawing.Size(355, 148);
            this.Gbox_P1_Calib.TabIndex = 57;
            this.Gbox_P1_Calib.TabStop = false;
            this.Gbox_P1_Calib.Text = "                                          ";
            // 
            // Btn_Init_Calib
            // 
            this.Btn_Init_Calib.Enabled = false;
            this.Btn_Init_Calib.Location = new System.Drawing.Point(11, 107);
            this.Btn_Init_Calib.Margin = new System.Windows.Forms.Padding(5, 5, 5, 5);
            this.Btn_Init_Calib.Name = "Btn_Init_Calib";
            this.Btn_Init_Calib.Size = new System.Drawing.Size(99, 31);
            this.Btn_Init_Calib.TabIndex = 66;
            this.Btn_Init_Calib.Text = "Default";
            this.Btn_Init_Calib.UseVisualStyleBackColor = true;
            this.Btn_Init_Calib.Click += new System.EventHandler(this.Btn_Init_Calib_Click);
            // 
            // Btn_Stop_Calib
            // 
            this.Btn_Stop_Calib.Enabled = false;
            this.Btn_Stop_Calib.Location = new System.Drawing.Point(247, 108);
            this.Btn_Stop_Calib.Margin = new System.Windows.Forms.Padding(5, 5, 5, 5);
            this.Btn_Stop_Calib.Name = "Btn_Stop_Calib";
            this.Btn_Stop_Calib.Size = new System.Drawing.Size(99, 31);
            this.Btn_Stop_Calib.TabIndex = 65;
            this.Btn_Stop_Calib.Text = "Stop";
            this.Btn_Stop_Calib.UseVisualStyleBackColor = true;
            this.Btn_Stop_Calib.Click += new System.EventHandler(this.Btn_Stop_Calib_Click);
            // 
            // Btn_Start_Calib
            // 
            this.Btn_Start_Calib.Enabled = false;
            this.Btn_Start_Calib.Location = new System.Drawing.Point(137, 108);
            this.Btn_Start_Calib.Margin = new System.Windows.Forms.Padding(5, 5, 5, 5);
            this.Btn_Start_Calib.Name = "Btn_Start_Calib";
            this.Btn_Start_Calib.Size = new System.Drawing.Size(99, 31);
            this.Btn_Start_Calib.TabIndex = 64;
            this.Btn_Start_Calib.Text = "Start";
            this.Btn_Start_Calib.UseVisualStyleBackColor = true;
            this.Btn_Start_Calib.Click += new System.EventHandler(this.Btn_Start_Calib_Click);
            // 
            // label28
            // 
            this.label28.AutoSize = true;
            this.label28.Location = new System.Drawing.Point(201, 64);
            this.label28.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label28.Name = "label28";
            this.label28.Size = new System.Drawing.Size(50, 16);
            this.label28.TabIndex = 63;
            this.label28.Text = "Y Max :";
            // 
            // Txt_Calib_Ymax
            // 
            this.Txt_Calib_Ymax.Enabled = false;
            this.Txt_Calib_Ymax.Location = new System.Drawing.Point(273, 60);
            this.Txt_Calib_Ymax.Margin = new System.Windows.Forms.Padding(5, 5, 5, 5);
            this.Txt_Calib_Ymax.Name = "Txt_Calib_Ymax";
            this.Txt_Calib_Ymax.ReadOnly = true;
            this.Txt_Calib_Ymax.Size = new System.Drawing.Size(71, 22);
            this.Txt_Calib_Ymax.TabIndex = 62;
            this.Txt_Calib_Ymax.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(201, 34);
            this.label2.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(46, 16);
            this.label2.TabIndex = 61;
            this.label2.Text = "Y Min :";
            // 
            // Txt_Calib_Ymin
            // 
            this.Txt_Calib_Ymin.Enabled = false;
            this.Txt_Calib_Ymin.Location = new System.Drawing.Point(273, 31);
            this.Txt_Calib_Ymin.Margin = new System.Windows.Forms.Padding(5, 5, 5, 5);
            this.Txt_Calib_Ymin.Name = "Txt_Calib_Ymin";
            this.Txt_Calib_Ymin.ReadOnly = true;
            this.Txt_Calib_Ymin.Size = new System.Drawing.Size(71, 22);
            this.Txt_Calib_Ymin.TabIndex = 60;
            this.Txt_Calib_Ymin.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label32
            // 
            this.label32.AutoSize = true;
            this.label32.Location = new System.Drawing.Point(13, 64);
            this.label32.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label32.Name = "label32";
            this.label32.Size = new System.Drawing.Size(49, 16);
            this.label32.TabIndex = 4;
            this.label32.Text = "X Max :";
            // 
            // Txt_Calib_Xmax
            // 
            this.Txt_Calib_Xmax.Enabled = false;
            this.Txt_Calib_Xmax.Location = new System.Drawing.Point(85, 60);
            this.Txt_Calib_Xmax.Margin = new System.Windows.Forms.Padding(5, 5, 5, 5);
            this.Txt_Calib_Xmax.Name = "Txt_Calib_Xmax";
            this.Txt_Calib_Xmax.ReadOnly = true;
            this.Txt_Calib_Xmax.Size = new System.Drawing.Size(71, 22);
            this.Txt_Calib_Xmax.TabIndex = 3;
            this.Txt_Calib_Xmax.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label36
            // 
            this.label36.AutoSize = true;
            this.label36.Location = new System.Drawing.Point(13, 34);
            this.label36.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label36.Name = "label36";
            this.label36.Size = new System.Drawing.Size(45, 16);
            this.label36.TabIndex = 2;
            this.label36.Text = "X Min :";
            // 
            // Txt_Calib_Xmin
            // 
            this.Txt_Calib_Xmin.Enabled = false;
            this.Txt_Calib_Xmin.Location = new System.Drawing.Point(85, 31);
            this.Txt_Calib_Xmin.Margin = new System.Windows.Forms.Padding(5, 5, 5, 5);
            this.Txt_Calib_Xmin.Name = "Txt_Calib_Xmin";
            this.Txt_Calib_Xmin.ReadOnly = true;
            this.Txt_Calib_Xmin.Size = new System.Drawing.Size(71, 22);
            this.Txt_Calib_Xmin.TabIndex = 1;
            this.Txt_Calib_Xmin.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // Chk_Calibration
            // 
            this.Chk_Calibration.AutoSize = true;
            this.Chk_Calibration.Location = new System.Drawing.Point(23, 0);
            this.Chk_Calibration.Margin = new System.Windows.Forms.Padding(5, 5, 5, 5);
            this.Chk_Calibration.Name = "Chk_Calibration";
            this.Chk_Calibration.Size = new System.Drawing.Size(173, 20);
            this.Chk_Calibration.TabIndex = 0;
            this.Chk_Calibration.Text = "Override Axis Calibration";
            this.Chk_Calibration.UseVisualStyleBackColor = true;
            this.Chk_Calibration.CheckedChanged += new System.EventHandler(this.Chk_Calibration_CheckedChanged);
            // 
            // Pnl_Separator
            // 
            this.Pnl_Separator.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Pnl_Separator.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Pnl_Separator.Location = new System.Drawing.Point(16, 177);
            this.Pnl_Separator.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Pnl_Separator.Name = "Pnl_Separator";
            this.Pnl_Separator.Size = new System.Drawing.Size(367, 1);
            this.Pnl_Separator.TabIndex = 58;
            // 
            // Btn_Save
            // 
            this.Btn_Save.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Btn_Save.BackgroundImage = global::U_Shoot.Properties.Resources.Save_32px;
            this.Btn_Save.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.Btn_Save.Location = new System.Drawing.Point(15, 391);
            this.Btn_Save.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Btn_Save.Name = "Btn_Save";
            this.Btn_Save.Size = new System.Drawing.Size(181, 44);
            this.Btn_Save.TabIndex = 59;
            this.Btn_Save.UseVisualStyleBackColor = true;
            this.Btn_Save.Click += new System.EventHandler(this.Btn_Save_Click);
            // 
            // Lbl_Manufacturer
            // 
            this.Lbl_Manufacturer.AutoSize = true;
            this.Lbl_Manufacturer.Location = new System.Drawing.Point(12, 11);
            this.Lbl_Manufacturer.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Lbl_Manufacturer.Name = "Lbl_Manufacturer";
            this.Lbl_Manufacturer.Size = new System.Drawing.Size(84, 16);
            this.Lbl_Manufacturer.TabIndex = 60;
            this.Lbl_Manufacturer.Text = "Manufacturer";
            // 
            // Lbl_Product
            // 
            this.Lbl_Product.AutoSize = true;
            this.Lbl_Product.Location = new System.Drawing.Point(12, 38);
            this.Lbl_Product.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Lbl_Product.Name = "Lbl_Product";
            this.Lbl_Product.Size = new System.Drawing.Size(53, 16);
            this.Lbl_Product.TabIndex = 61;
            this.Lbl_Product.Text = "Product";
            // 
            // Pnl_Separator0
            // 
            this.Pnl_Separator0.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Pnl_Separator0.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Pnl_Separator0.Location = new System.Drawing.Point(16, 74);
            this.Pnl_Separator0.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Pnl_Separator0.Name = "Pnl_Separator0";
            this.Pnl_Separator0.Size = new System.Drawing.Size(367, 1);
            this.Pnl_Separator0.TabIndex = 59;
            // 
            // Btn_Cancel
            // 
            this.Btn_Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Btn_Cancel.BackgroundImage = global::U_Shoot.Properties.Resources.Close_24px;
            this.Btn_Cancel.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.Btn_Cancel.Location = new System.Drawing.Point(202, 391);
            this.Btn_Cancel.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Btn_Cancel.Name = "Btn_Cancel";
            this.Btn_Cancel.Size = new System.Drawing.Size(181, 44);
            this.Btn_Cancel.TabIndex = 62;
            this.Btn_Cancel.UseVisualStyleBackColor = true;
            this.Btn_Cancel.Click += new System.EventHandler(this.Btn_Cancel_Click);
            // 
            // Wnd_AxisOptions
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(400, 449);
            this.Controls.Add(this.Btn_Cancel);
            this.Controls.Add(this.Pnl_Separator0);
            this.Controls.Add(this.Lbl_Product);
            this.Controls.Add(this.Lbl_Manufacturer);
            this.Controls.Add(this.Pnl_Separator);
            this.Controls.Add(this.Btn_Save);
            this.Controls.Add(this.Gbox_P1_Calib);
            this.Controls.Add(this.Chk_InvertY);
            this.Controls.Add(this.Chk_InvertX);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.Cbox_HID_YAxis);
            this.Controls.Add(this.label29);
            this.Controls.Add(this.Cbox_HID_XAxis);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Wnd_AxisOptions";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Controller Axes Options";
            this.TopMost = true;
            this.Gbox_P1_Calib.ResumeLayout(false);
            this.Gbox_P1_Calib.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox Chk_InvertY;
        private System.Windows.Forms.CheckBox Chk_InvertX;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox Cbox_HID_YAxis;
        private System.Windows.Forms.Label label29;
        private System.Windows.Forms.ComboBox Cbox_HID_XAxis;
        private System.Windows.Forms.GroupBox Gbox_P1_Calib;
        private System.Windows.Forms.Button Btn_Init_Calib;
        private System.Windows.Forms.Button Btn_Stop_Calib;
        private System.Windows.Forms.Button Btn_Start_Calib;
        private System.Windows.Forms.Label label28;
        private System.Windows.Forms.TextBox Txt_Calib_Ymax;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox Txt_Calib_Ymin;
        private System.Windows.Forms.Label label32;
        private System.Windows.Forms.TextBox Txt_Calib_Xmax;
        private System.Windows.Forms.Label label36;
        private System.Windows.Forms.TextBox Txt_Calib_Xmin;
        private System.Windows.Forms.CheckBox Chk_Calibration;
        private System.Windows.Forms.Panel Pnl_Separator;
        private System.Windows.Forms.Button Btn_Save;
        private System.Windows.Forms.Label Lbl_Manufacturer;
        private System.Windows.Forms.Label Lbl_Product;
        private System.Windows.Forms.Panel Pnl_Separator0;
        private System.Windows.Forms.Button Btn_Cancel;
    }
}