namespace U_Shoot.GUI
{
    partial class Wnd_GameOptions
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Wnd_GameOptions));
            this.Flp_Options = new System.Windows.Forms.FlowLayoutPanel();
            this.Btn_Save = new System.Windows.Forms.Button();
            this.Btn_Cancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // Flp_Options
            // 
            this.Flp_Options.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Flp_Options.AutoScroll = true;
            this.Flp_Options.BackColor = System.Drawing.Color.White;
            this.Flp_Options.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Flp_Options.Location = new System.Drawing.Point(12, 12);
            this.Flp_Options.Name = "Flp_Options";
            this.Flp_Options.Size = new System.Drawing.Size(315, 314);
            this.Flp_Options.TabIndex = 0;
            this.Flp_Options.SizeChanged += new System.EventHandler(this.Flp_Options_SizeChanged);
            // 
            // Btn_Save
            // 
            this.Btn_Save.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Btn_Save.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Btn_Save.Image = global::U_Shoot.Properties.Resources.Save_32px;
            this.Btn_Save.Location = new System.Drawing.Point(12, 332);
            this.Btn_Save.Name = "Btn_Save";
            this.Btn_Save.Size = new System.Drawing.Size(154, 36);
            this.Btn_Save.TabIndex = 1;
            this.Btn_Save.UseVisualStyleBackColor = true;
            this.Btn_Save.Click += new System.EventHandler(this.Btn_Save_Click);
            // 
            // Btn_Cancel
            // 
            this.Btn_Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Btn_Cancel.BackgroundImage = global::U_Shoot.Properties.Resources.Close_24px;
            this.Btn_Cancel.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.Btn_Cancel.Location = new System.Drawing.Point(172, 332);
            this.Btn_Cancel.Margin = new System.Windows.Forms.Padding(2);
            this.Btn_Cancel.Name = "Btn_Cancel";
            this.Btn_Cancel.Size = new System.Drawing.Size(154, 36);
            this.Btn_Cancel.TabIndex = 63;
            this.Btn_Cancel.UseVisualStyleBackColor = true;
            this.Btn_Cancel.Click += new System.EventHandler(this.Btn_Cancel_Click);
            // 
            // Wnd_GameOptions
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(339, 379);
            this.Controls.Add(this.Btn_Cancel);
            this.Controls.Add(this.Btn_Save);
            this.Controls.Add(this.Flp_Options);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Wnd_GameOptions";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Wnd_GameOptions";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel Flp_Options;
        private System.Windows.Forms.Button Btn_Save;
        private System.Windows.Forms.Button Btn_Cancel;
    }
}