namespace U_Shoot.Tools.UnityPluginConfigs
{
    partial class NerfArcade
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
            this.Cbox_CabinetType = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // Btn_Save
            // 
            this.Btn_Save.Location = new System.Drawing.Point(12, 297);
            this.Btn_Save.Size = new System.Drawing.Size(274, 40);
            // 
            // groupBox1
            // 
            this.groupBox1.Size = new System.Drawing.Size(274, 154);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.Cbox_CabinetType);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Size = new System.Drawing.Size(274, 110);
            this.groupBox2.Controls.SetChildIndex(this.Lbl_LocalSave, 0);
            this.groupBox2.Controls.SetChildIndex(this.Chk_LocalSave, 0);
            this.groupBox2.Controls.SetChildIndex(this.label5, 0);
            this.groupBox2.Controls.SetChildIndex(this.Cbox_CabinetType, 0);
            // 
            // Cbox_CabinetType
            // 
            this.Cbox_CabinetType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Cbox_CabinetType.FormattingEnabled = true;
            this.Cbox_CabinetType.Items.AddRange(new object[] {
            "USA_CARD_SWIPE",
            "USA_COIN",
            "CEC_COIN",
            "CEC_CARD",
            "DB_CARD",
            "UK",
            "EUROPE",
            "JAPAN",
            "CHINA",
            "AUSTRALI"});
            this.Cbox_CabinetType.Location = new System.Drawing.Point(124, 67);
            this.Cbox_CabinetType.Name = "Cbox_CabinetType";
            this.Cbox_CabinetType.Size = new System.Drawing.Size(144, 21);
            this.Cbox_CabinetType.TabIndex = 96;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(8, 70);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(70, 13);
            this.label5.TabIndex = 97;
            this.label5.Text = "Cabinet Type";
            // 
            // NerfArcade
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(298, 349);
            this.Name = "NerfArcade";
            this.Text = "NerfArcade";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox Cbox_CabinetType;
        private System.Windows.Forms.Label label5;
    }
}