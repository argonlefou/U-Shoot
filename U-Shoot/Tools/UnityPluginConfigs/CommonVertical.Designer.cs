namespace U_Shoot.Tools.UnityPluginConfigs
{
    partial class CommonVertical
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
            this.Chk_HorizontalScreen = new System.Windows.Forms.CheckBox();
            this.label5 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // Btn_Save
            // 
            this.Btn_Save.Location = new System.Drawing.Point(12, 302);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.Chk_HorizontalScreen);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Size = new System.Drawing.Size(221, 108);
            this.groupBox2.Controls.SetChildIndex(this.Lbl_LocalSave, 0);
            this.groupBox2.Controls.SetChildIndex(this.Chk_LocalSave, 0);
            this.groupBox2.Controls.SetChildIndex(this.label5, 0);
            this.groupBox2.Controls.SetChildIndex(this.Chk_HorizontalScreen, 0);
            // 
            // Chk_HorizontalScreen
            // 
            this.Chk_HorizontalScreen.AutoSize = true;
            this.Chk_HorizontalScreen.Location = new System.Drawing.Point(124, 71);
            this.Chk_HorizontalScreen.Name = "Chk_HorizontalScreen";
            this.Chk_HorizontalScreen.Size = new System.Drawing.Size(15, 14);
            this.Chk_HorizontalScreen.TabIndex = 11;
            this.Chk_HorizontalScreen.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(8, 71);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(91, 13);
            this.label5.TabIndex = 10;
            this.label5.Text = "Horizontal Screen";
            // 
            // CommonVertical
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(245, 354);
            this.Name = "CommonVertical";
            this.Text = "CommonVertical";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.CheckBox Chk_HorizontalScreen;
        private System.Windows.Forms.Label label5;
    }
}