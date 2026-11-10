namespace U_Shoot.Tools.UnityPluginConfigs
{
    partial class MechaTornado
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
            this.label5 = new System.Windows.Forms.Label();
            this.Chk_WaterGun = new System.Windows.Forms.CheckBox();
            this.Cbox_Players = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // Btn_Save
            // 
            this.Btn_Save.Location = new System.Drawing.Point(12, 334);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.Cbox_Players);
            this.groupBox2.Controls.Add(this.Chk_WaterGun);
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Size = new System.Drawing.Size(221, 141);
            this.groupBox2.Controls.SetChildIndex(this.Lbl_LocalSave, 0);
            this.groupBox2.Controls.SetChildIndex(this.Chk_LocalSave, 0);
            this.groupBox2.Controls.SetChildIndex(this.label5, 0);
            this.groupBox2.Controls.SetChildIndex(this.label6, 0);
            this.groupBox2.Controls.SetChildIndex(this.Chk_WaterGun, 0);
            this.groupBox2.Controls.SetChildIndex(this.Cbox_Players, 0);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(7, 67);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(76, 13);
            this.label5.TabIndex = 6;
            this.label5.Text = "Use water gun";
            // 
            // Chk_WaterGun
            // 
            this.Chk_WaterGun.AutoSize = true;
            this.Chk_WaterGun.Location = new System.Drawing.Point(123, 67);
            this.Chk_WaterGun.Name = "Chk_WaterGun";
            this.Chk_WaterGun.Size = new System.Drawing.Size(15, 14);
            this.Chk_WaterGun.TabIndex = 7;
            this.Chk_WaterGun.UseVisualStyleBackColor = true;
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
            this.Cbox_Players.Location = new System.Drawing.Point(123, 96);
            this.Cbox_Players.Name = "Cbox_Players";
            this.Cbox_Players.Size = new System.Drawing.Size(51, 21);
            this.Cbox_Players.TabIndex = 96;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(7, 99);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(41, 13);
            this.label6.TabIndex = 97;
            this.label6.Text = "Players";
            // 
            // MechaTornado
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(245, 386);
            this.Name = "MechaTornado";
            this.Text = "MechaTornado";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.CheckBox Chk_WaterGun;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox Cbox_Players;
        private System.Windows.Forms.Label label6;
    }
}