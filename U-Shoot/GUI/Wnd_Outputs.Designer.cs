namespace U_Shoot.GUI
{
    partial class Wnd_Outputs
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Wnd_Outputs));
            this.groupBox19 = new System.Windows.Forms.GroupBox();
            this.Rdo_OutputsNetwork = new System.Windows.Forms.RadioButton();
            this.Rdo_OutputsWm = new System.Windows.Forms.RadioButton();
            this.Rdo_OutputsNone = new System.Windows.Forms.RadioButton();
            this.Grp_Outputs = new System.Windows.Forms.GroupBox();
            this.Txt_OutputDelay = new System.Windows.Forms.NumericUpDown();
            this.Txt_OutputRecoilOff = new System.Windows.Forms.NumericUpDown();
            this.Txt_OutputRecoilOn = new System.Windows.Forms.NumericUpDown();
            this.Txt_OutputDamaged = new System.Windows.Forms.NumericUpDown();
            this.label21 = new System.Windows.Forms.Label();
            this.label24 = new System.Windows.Forms.Label();
            this.label23 = new System.Windows.Forms.Label();
            this.label20 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label22 = new System.Windows.Forms.Label();
            this.label25 = new System.Windows.Forms.Label();
            this.label26 = new System.Windows.Forms.Label();
            this.Btn_Save = new System.Windows.Forms.Button();
            this.groupBox19.SuspendLayout();
            this.Grp_Outputs.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Txt_OutputDelay)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Txt_OutputRecoilOff)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Txt_OutputRecoilOn)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Txt_OutputDamaged)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox19
            // 
            this.groupBox19.Controls.Add(this.Rdo_OutputsNetwork);
            this.groupBox19.Controls.Add(this.Rdo_OutputsWm);
            this.groupBox19.Controls.Add(this.Rdo_OutputsNone);
            this.groupBox19.Location = new System.Drawing.Point(9, 12);
            this.groupBox19.Name = "groupBox19";
            this.groupBox19.Size = new System.Drawing.Size(462, 106);
            this.groupBox19.TabIndex = 59;
            this.groupBox19.TabStop = false;
            this.groupBox19.Text = "MAME-Type Outputs";
            // 
            // Rdo_OutputsNetwork
            // 
            this.Rdo_OutputsNetwork.AutoSize = true;
            this.Rdo_OutputsNetwork.Location = new System.Drawing.Point(13, 79);
            this.Rdo_OutputsNetwork.Name = "Rdo_OutputsNetwork";
            this.Rdo_OutputsNetwork.Size = new System.Drawing.Size(103, 17);
            this.Rdo_OutputsNetwork.TabIndex = 63;
            this.Rdo_OutputsNetwork.Text = "Network outputs";
            this.Rdo_OutputsNetwork.UseVisualStyleBackColor = true;
            this.Rdo_OutputsNetwork.CheckedChanged += new System.EventHandler(this.Rdo_Outputs_CheckedChanged);
            // 
            // Rdo_OutputsWm
            // 
            this.Rdo_OutputsWm.AutoSize = true;
            this.Rdo_OutputsWm.Location = new System.Drawing.Point(13, 53);
            this.Rdo_OutputsWm.Name = "Rdo_OutputsWm";
            this.Rdo_OutputsWm.Size = new System.Drawing.Size(221, 17);
            this.Rdo_OutputsWm.TabIndex = 62;
            this.Rdo_OutputsWm.Text = "Window Message outputs (MameHooker)";
            this.Rdo_OutputsWm.UseVisualStyleBackColor = true;
            this.Rdo_OutputsWm.CheckedChanged += new System.EventHandler(this.Rdo_Outputs_CheckedChanged);
            // 
            // Rdo_OutputsNone
            // 
            this.Rdo_OutputsNone.AutoSize = true;
            this.Rdo_OutputsNone.Checked = true;
            this.Rdo_OutputsNone.Location = new System.Drawing.Point(13, 28);
            this.Rdo_OutputsNone.Name = "Rdo_OutputsNone";
            this.Rdo_OutputsNone.Size = new System.Drawing.Size(66, 17);
            this.Rdo_OutputsNone.TabIndex = 61;
            this.Rdo_OutputsNone.TabStop = true;
            this.Rdo_OutputsNone.Text = "Disabled";
            this.Rdo_OutputsNone.UseVisualStyleBackColor = true;
            this.Rdo_OutputsNone.CheckedChanged += new System.EventHandler(this.Rdo_Outputs_CheckedChanged);
            // 
            // Grp_Outputs
            // 
            this.Grp_Outputs.Controls.Add(this.Txt_OutputDelay);
            this.Grp_Outputs.Controls.Add(this.Txt_OutputRecoilOff);
            this.Grp_Outputs.Controls.Add(this.Txt_OutputRecoilOn);
            this.Grp_Outputs.Controls.Add(this.Txt_OutputDamaged);
            this.Grp_Outputs.Controls.Add(this.label21);
            this.Grp_Outputs.Controls.Add(this.label24);
            this.Grp_Outputs.Controls.Add(this.label23);
            this.Grp_Outputs.Controls.Add(this.label20);
            this.Grp_Outputs.Controls.Add(this.label10);
            this.Grp_Outputs.Controls.Add(this.label22);
            this.Grp_Outputs.Controls.Add(this.label25);
            this.Grp_Outputs.Controls.Add(this.label26);
            this.Grp_Outputs.Location = new System.Drawing.Point(9, 124);
            this.Grp_Outputs.Name = "Grp_Outputs";
            this.Grp_Outputs.Size = new System.Drawing.Size(569, 157);
            this.Grp_Outputs.TabIndex = 58;
            this.Grp_Outputs.TabStop = false;
            // 
            // Txt_OutputDelay
            // 
            this.Txt_OutputDelay.Location = new System.Drawing.Point(310, 26);
            this.Txt_OutputDelay.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.Txt_OutputDelay.Name = "Txt_OutputDelay";
            this.Txt_OutputDelay.Size = new System.Drawing.Size(56, 20);
            this.Txt_OutputDelay.TabIndex = 73;
            this.Txt_OutputDelay.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.Txt_OutputDelay.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.Txt_OutputDelay.ValueChanged += new System.EventHandler(this.Txt_OutputDelay_ValueChanged);
            // 
            // Txt_OutputRecoilOff
            // 
            this.Txt_OutputRecoilOff.Location = new System.Drawing.Point(334, 122);
            this.Txt_OutputRecoilOff.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.Txt_OutputRecoilOff.Name = "Txt_OutputRecoilOff";
            this.Txt_OutputRecoilOff.Size = new System.Drawing.Size(56, 20);
            this.Txt_OutputRecoilOff.TabIndex = 72;
            this.Txt_OutputRecoilOff.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.Txt_OutputRecoilOff.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.Txt_OutputRecoilOff.ValueChanged += new System.EventHandler(this.Txt_OutputRecoilOff_ValueChanged);
            // 
            // Txt_OutputRecoilOn
            // 
            this.Txt_OutputRecoilOn.Location = new System.Drawing.Point(334, 94);
            this.Txt_OutputRecoilOn.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.Txt_OutputRecoilOn.Name = "Txt_OutputRecoilOn";
            this.Txt_OutputRecoilOn.Size = new System.Drawing.Size(56, 20);
            this.Txt_OutputRecoilOn.TabIndex = 71;
            this.Txt_OutputRecoilOn.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.Txt_OutputRecoilOn.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
            this.Txt_OutputRecoilOn.ValueChanged += new System.EventHandler(this.Txt_OutputRecoilOn_ValueChanged);
            // 
            // Txt_OutputDamaged
            // 
            this.Txt_OutputDamaged.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.Txt_OutputDamaged.Location = new System.Drawing.Point(310, 54);
            this.Txt_OutputDamaged.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.Txt_OutputDamaged.Name = "Txt_OutputDamaged";
            this.Txt_OutputDamaged.Size = new System.Drawing.Size(56, 20);
            this.Txt_OutputDamaged.TabIndex = 70;
            this.Txt_OutputDamaged.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.Txt_OutputDamaged.Value = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.Txt_OutputDamaged.ValueChanged += new System.EventHandler(this.Txt_OutputDamaged_ValueChanged);
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(396, 124);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(64, 13);
            this.label21.TabIndex = 69;
            this.label21.Text = "Milliseconds";
            // 
            // label24
            // 
            this.label24.AutoSize = true;
            this.label24.Location = new System.Drawing.Point(10, 124);
            this.label24.Name = "label24";
            this.label24.Size = new System.Drawing.Size(262, 13);
            this.label24.TabIndex = 68;
            this.label24.Text = "Length of custom [recoil] generated output OFF state :\r\n";
            // 
            // label23
            // 
            this.label23.AutoSize = true;
            this.label23.Location = new System.Drawing.Point(372, 56);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(64, 13);
            this.label23.TabIndex = 66;
            this.label23.Text = "Milliseconds";
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.Location = new System.Drawing.Point(10, 56);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(232, 13);
            this.label20.TabIndex = 65;
            this.label20.Text = "Length of custom [damaged] generated output :";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(396, 96);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(64, 13);
            this.label10.TabIndex = 64;
            this.label10.Text = "Milliseconds";
            // 
            // label22
            // 
            this.label22.AutoSize = true;
            this.label22.Location = new System.Drawing.Point(11, 96);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(258, 13);
            this.label22.TabIndex = 61;
            this.label22.Text = "Length of custom [recoil] generated output ON state :";
            // 
            // label25
            // 
            this.label25.AutoSize = true;
            this.label25.Location = new System.Drawing.Point(372, 28);
            this.label25.Name = "label25";
            this.label25.Size = new System.Drawing.Size(64, 13);
            this.label25.TabIndex = 4;
            this.label25.Text = "Milliseconds";
            // 
            // label26
            // 
            this.label26.AutoSize = true;
            this.label26.Location = new System.Drawing.Point(11, 28);
            this.label26.Name = "label26";
            this.label26.Size = new System.Drawing.Size(152, 13);
            this.label26.TabIndex = 2;
            this.label26.Text = "Delay between output refresh :\r\n";
            // 
            // Btn_Save
            // 
            this.Btn_Save.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Btn_Save.Image = global::U_Shoot.Properties.Resources.Save_32px;
            this.Btn_Save.Location = new System.Drawing.Point(496, 29);
            this.Btn_Save.Name = "Btn_Save";
            this.Btn_Save.Size = new System.Drawing.Size(70, 70);
            this.Btn_Save.TabIndex = 60;
            this.Btn_Save.UseVisualStyleBackColor = true;
            this.Btn_Save.Click += new System.EventHandler(this.Btn_Save_Click);
            // 
            // Wnd_Outputs
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(590, 293);
            this.Controls.Add(this.Btn_Save);
            this.Controls.Add(this.groupBox19);
            this.Controls.Add(this.Grp_Outputs);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Wnd_Outputs";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Outputs Settings";
            this.groupBox19.ResumeLayout(false);
            this.groupBox19.PerformLayout();
            this.Grp_Outputs.ResumeLayout(false);
            this.Grp_Outputs.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Txt_OutputDelay)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Txt_OutputRecoilOff)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Txt_OutputRecoilOn)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Txt_OutputDamaged)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.GroupBox groupBox19;
        private System.Windows.Forms.GroupBox Grp_Outputs;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.Label label24;
        private System.Windows.Forms.Label label23;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.Label label25;
        private System.Windows.Forms.Label label26;
        private System.Windows.Forms.NumericUpDown Txt_OutputDamaged;
        private System.Windows.Forms.NumericUpDown Txt_OutputDelay;
        private System.Windows.Forms.NumericUpDown Txt_OutputRecoilOff;
        private System.Windows.Forms.NumericUpDown Txt_OutputRecoilOn;
        private System.Windows.Forms.Button Btn_Save;
        private System.Windows.Forms.RadioButton Rdo_OutputsNetwork;
        private System.Windows.Forms.RadioButton Rdo_OutputsWm;
        private System.Windows.Forms.RadioButton Rdo_OutputsNone;
    }
}