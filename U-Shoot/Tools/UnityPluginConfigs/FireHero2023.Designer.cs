namespace U_Shoot.Tools.UnityPluginConfigs
{
    partial class FireHero2023
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
            this.Cbox_Language = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.Cbox_Players = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.Cbox_Type = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // Btn_Save
            // 
            this.Btn_Save.Location = new System.Drawing.Point(12, 374);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.Cbox_Type);
            this.groupBox2.Controls.Add(this.label7);
            this.groupBox2.Controls.Add(this.Cbox_Players);
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Controls.Add(this.Cbox_Language);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Size = new System.Drawing.Size(221, 168);
            this.groupBox2.Controls.SetChildIndex(this.Lbl_LocalSave, 0);
            this.groupBox2.Controls.SetChildIndex(this.Chk_LocalSave, 0);
            this.groupBox2.Controls.SetChildIndex(this.label5, 0);
            this.groupBox2.Controls.SetChildIndex(this.Cbox_Language, 0);
            this.groupBox2.Controls.SetChildIndex(this.label6, 0);
            this.groupBox2.Controls.SetChildIndex(this.Cbox_Players, 0);
            this.groupBox2.Controls.SetChildIndex(this.label7, 0);
            this.groupBox2.Controls.SetChildIndex(this.Cbox_Type, 0);
            // 
            // Cbox_Language
            // 
            this.Cbox_Language.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Cbox_Language.FormattingEnabled = true;
            this.Cbox_Language.Items.AddRange(new object[] {
            "CH",
            "EN"});
            this.Cbox_Language.Location = new System.Drawing.Point(124, 67);
            this.Cbox_Language.Name = "Cbox_Language";
            this.Cbox_Language.Size = new System.Drawing.Size(51, 21);
            this.Cbox_Language.TabIndex = 96;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(8, 70);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(55, 13);
            this.label5.TabIndex = 97;
            this.label5.Text = "Language";
            // 
            // Cbox_Players
            // 
            this.Cbox_Players.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Cbox_Players.FormattingEnabled = true;
            this.Cbox_Players.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4"});
            this.Cbox_Players.Location = new System.Drawing.Point(125, 94);
            this.Cbox_Players.Name = "Cbox_Players";
            this.Cbox_Players.Size = new System.Drawing.Size(51, 21);
            this.Cbox_Players.TabIndex = 98;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(9, 97);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(41, 13);
            this.label6.TabIndex = 99;
            this.label6.Text = "Players";
            // 
            // Cbox_Type
            // 
            this.Cbox_Type.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Cbox_Type.FormattingEnabled = true;
            this.Cbox_Type.Items.AddRange(new object[] {
            "A",
            "B"});
            this.Cbox_Type.Location = new System.Drawing.Point(125, 121);
            this.Cbox_Type.Name = "Cbox_Type";
            this.Cbox_Type.Size = new System.Drawing.Size(51, 21);
            this.Cbox_Type.TabIndex = 100;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(9, 124);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(70, 13);
            this.label7.TabIndex = 101;
            this.label7.Text = "Cabinet Type";
            // 
            // FireHero2023
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(245, 426);
            this.Name = "FireHero2023";
            this.Text = "FireHero2023";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox Cbox_Language;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox Cbox_Players;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox Cbox_Type;
        private System.Windows.Forms.Label label7;
    }
}