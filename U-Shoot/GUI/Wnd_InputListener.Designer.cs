namespace U_Shoot.GUI
{
    partial class Wnd_InputListener
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
            this.components = new System.ComponentModel.Container();
            this.Lbl_Message = new System.Windows.Forms.Label();
            this.Tmr_AutoClose = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // Lbl_Message
            // 
            this.Lbl_Message.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Lbl_Message.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Lbl_Message.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Lbl_Message.Location = new System.Drawing.Point(0, 0);
            this.Lbl_Message.Name = "Lbl_Message";
            this.Lbl_Message.Size = new System.Drawing.Size(324, 79);
            this.Lbl_Message.TabIndex = 0;
            this.Lbl_Message.Text = "Press any key ...";
            this.Lbl_Message.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // Tmr_AutoClose
            // 
            this.Tmr_AutoClose.Enabled = true;
            this.Tmr_AutoClose.Interval = 1000;
            this.Tmr_AutoClose.Tick += new System.EventHandler(this.Tmr_AutoClose_Tick);
            // 
            // Wnd_InputListener
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(324, 79);
            this.Controls.Add(this.Lbl_Message);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Wnd_InputListener";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Wnd_InputListener";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Wnd_InputListener_FormClosed);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label Lbl_Message;
        private System.Windows.Forms.Timer Tmr_AutoClose;
    }
}