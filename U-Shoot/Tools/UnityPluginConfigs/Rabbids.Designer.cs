namespace U_Shoot.Tools.UnityPluginConfigs
{
    partial class Rabbids
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
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // Btn_Save
            // 
            this.Btn_Save.Location = new System.Drawing.Point(12, 297);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.Cbox_Language);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Size = new System.Drawing.Size(221, 110);
            this.groupBox2.Controls.SetChildIndex(this.Lbl_LocalSave, 0);
            this.groupBox2.Controls.SetChildIndex(this.Chk_LocalSave, 0);
            this.groupBox2.Controls.SetChildIndex(this.label5, 0);
            this.groupBox2.Controls.SetChildIndex(this.Cbox_Language, 0);
            // 
            // Cbox_Language
            // 
            this.Cbox_Language.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Cbox_Language.FormattingEnabled = true;
            this.Cbox_Language.Items.AddRange(new object[] {
            "EN",
            "FR",
            "JA",
            "ZH"});
            this.Cbox_Language.Location = new System.Drawing.Point(124, 67);
            this.Cbox_Language.Name = "Cbox_Language";
            this.Cbox_Language.Size = new System.Drawing.Size(51, 21);
            this.Cbox_Language.TabIndex = 94;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(8, 70);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(55, 13);
            this.label5.TabIndex = 95;
            this.label5.Text = "Language";
            // 
            // Rabbids
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(245, 349);
            this.Name = "Rabbids";
            this.Text = "Rabbids";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox Cbox_Language;
        private System.Windows.Forms.Label label5;
    }
}