namespace U_Shoot.Tools
{
    partial class BaseCopyWindow
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
            this.Btn_Close = new System.Windows.Forms.Button();
            this.Btn_ShowLog = new System.Windows.Forms.Button();
            this._BgWorker = new System.ComponentModel.BackgroundWorker();
            this.Pbar_Patch = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // Btn_Close
            // 
            this.Btn_Close.Location = new System.Drawing.Point(135, 41);
            this.Btn_Close.Name = "Btn_Close";
            this.Btn_Close.Size = new System.Drawing.Size(156, 30);
            this.Btn_Close.TabIndex = 15;
            this.Btn_Close.Text = "Close";
            this.Btn_Close.UseVisualStyleBackColor = true;
            this.Btn_Close.Click += new System.EventHandler(this.Btn_Close_Click);
            // 
            // Btn_ShowLog
            // 
            this.Btn_ShowLog.Location = new System.Drawing.Point(12, 41);
            this.Btn_ShowLog.Name = "Btn_ShowLog";
            this.Btn_ShowLog.Size = new System.Drawing.Size(87, 30);
            this.Btn_ShowLog.TabIndex = 14;
            this.Btn_ShowLog.Text = "Show Log";
            this.Btn_ShowLog.UseVisualStyleBackColor = true;
            this.Btn_ShowLog.Click += new System.EventHandler(this.Btn_ShowLog_Click);
            // 
            // _BgWorker
            // 
            this._BgWorker.WorkerReportsProgress = true;
            this._BgWorker.WorkerSupportsCancellation = true;
            this._BgWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.BgWorker_DoWork);
            this._BgWorker.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.BgWorker_ProgressChanged);
            this._BgWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.BgWorker_RunWorkerCompleted);
            // 
            // Pbar_Patch
            // 
            this.Pbar_Patch.Location = new System.Drawing.Point(12, 12);
            this.Pbar_Patch.Name = "Pbar_Patch";
            this.Pbar_Patch.Size = new System.Drawing.Size(279, 23);
            this.Pbar_Patch.TabIndex = 16;
            // 
            // BaseCopyWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(303, 83);
            this.Controls.Add(this.Btn_Close);
            this.Controls.Add(this.Btn_ShowLog);
            this.Controls.Add(this.Pbar_Patch);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "BaseCopyWindow";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "BaseCopyWindow";
            this.Load += new System.EventHandler(this.BaseCopyWindow_Load);
            this.ResumeLayout(false);

        }

        #endregion

        protected System.Windows.Forms.Button Btn_Close;
        protected System.Windows.Forms.Button Btn_ShowLog;
        protected System.ComponentModel.BackgroundWorker _BgWorker;
        protected System.Windows.Forms.ProgressBar Pbar_Patch;
    }
}