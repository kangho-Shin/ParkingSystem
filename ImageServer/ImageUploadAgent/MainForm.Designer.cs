namespace ImageUploadAgent
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            label1 = new Label();
            txtWatchPath = new TextBox();
            txtServerUrl = new TextBox();
            label2 = new Label();
            txtScanInterval = new TextBox();
            label3 = new Label();
            btnStart = new Button();
            btnStop = new Button();
            pictureBoxPreview = new PictureBox();
            lblStatus = new Label();
            lblCurrentFile = new Label();
            lblPendingCount = new Label();
            txtExtList = new TextBox();
            lstLog = new ListBox();
            label4 = new Label();
            _trayIcon = new NotifyIcon(components);
            ((System.ComponentModel.ISupportInitialize)pictureBoxPreview).BeginInit();
            SuspendLayout();
            // 
            // label1
            // 
            label1.BorderStyle = BorderStyle.FixedSingle;
            label1.Location = new Point(12, 37);
            label1.Name = "label1";
            label1.Size = new Size(100, 23);
            label1.TabIndex = 0;
            label1.Text = "이미지경로";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // txtWatchPath
            // 
            txtWatchPath.Location = new Point(118, 37);
            txtWatchPath.Name = "txtWatchPath";
            txtWatchPath.Size = new Size(289, 23);
            txtWatchPath.TabIndex = 1;
            // 
            // txtServerUrl
            // 
            txtServerUrl.Location = new Point(118, 9);
            txtServerUrl.Name = "txtServerUrl";
            txtServerUrl.Size = new Size(289, 23);
            txtServerUrl.TabIndex = 3;
            // 
            // label2
            // 
            label2.BorderStyle = BorderStyle.FixedSingle;
            label2.Location = new Point(12, 9);
            label2.Name = "label2";
            label2.Size = new Size(100, 23);
            label2.TabIndex = 2;
            label2.Text = "서버주소";
            label2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // txtScanInterval
            // 
            txtScanInterval.Location = new Point(118, 65);
            txtScanInterval.Name = "txtScanInterval";
            txtScanInterval.Size = new Size(67, 23);
            txtScanInterval.TabIndex = 5;
            // 
            // label3
            // 
            label3.BorderStyle = BorderStyle.FixedSingle;
            label3.Location = new Point(12, 65);
            label3.Name = "label3";
            label3.Size = new Size(100, 23);
            label3.TabIndex = 4;
            label3.Text = "스캔시간";
            label3.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // btnStart
            // 
            btnStart.Location = new Point(413, 9);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(86, 23);
            btnStart.TabIndex = 6;
            btnStart.Text = "시작";
            btnStart.UseVisualStyleBackColor = true;
            btnStart.Click += btnStart_Click;
            // 
            // btnStop
            // 
            btnStop.Location = new Point(413, 37);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(86, 23);
            btnStop.TabIndex = 7;
            btnStop.Text = "정지";
            btnStop.UseVisualStyleBackColor = true;
            btnStop.Click += btnStop_Click;
            // 
            // pictureBoxPreview
            // 
            pictureBoxPreview.BackgroundImageLayout = ImageLayout.Stretch;
            pictureBoxPreview.BorderStyle = BorderStyle.FixedSingle;
            pictureBoxPreview.Location = new Point(12, 94);
            pictureBoxPreview.Name = "pictureBoxPreview";
            pictureBoxPreview.Size = new Size(487, 358);
            pictureBoxPreview.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBoxPreview.TabIndex = 8;
            pictureBoxPreview.TabStop = false;
            // 
            // lblStatus
            // 
            lblStatus.BorderStyle = BorderStyle.FixedSingle;
            lblStatus.Location = new Point(12, 455);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(173, 23);
            lblStatus.TabIndex = 9;
            lblStatus.Text = "스캔시간";
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblCurrentFile
            // 
            lblCurrentFile.BorderStyle = BorderStyle.FixedSingle;
            lblCurrentFile.Location = new Point(12, 481);
            lblCurrentFile.Name = "lblCurrentFile";
            lblCurrentFile.Size = new Size(487, 23);
            lblCurrentFile.TabIndex = 10;
            lblCurrentFile.Text = "스캔시간";
            lblCurrentFile.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblPendingCount
            // 
            lblPendingCount.BorderStyle = BorderStyle.FixedSingle;
            lblPendingCount.Location = new Point(214, 455);
            lblPendingCount.Name = "lblPendingCount";
            lblPendingCount.Size = new Size(285, 23);
            lblPendingCount.TabIndex = 11;
            lblPendingCount.Text = "스캔시간";
            lblPendingCount.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // txtExtList
            // 
            txtExtList.Location = new Point(214, 65);
            txtExtList.Name = "txtExtList";
            txtExtList.Size = new Size(193, 23);
            txtExtList.TabIndex = 12;
            // 
            // lstLog
            // 
            lstLog.FormattingEnabled = true;
            lstLog.ItemHeight = 15;
            lstLog.Location = new Point(510, 35);
            lstLog.Name = "lstLog";
            lstLog.Size = new Size(500, 469);
            lstLog.TabIndex = 13;
            // 
            // label4
            // 
            label4.Location = new Point(510, 8);
            label4.Name = "label4";
            label4.Size = new Size(100, 23);
            label4.TabIndex = 14;
            label4.Text = "로그현황";
            label4.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // _trayIcon
            // 
            _trayIcon.Icon = (Icon)resources.GetObject("_trayIcon.Icon");
            _trayIcon.Text = "ImageUploadAgent";
            _trayIcon.Visible = true;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1022, 513);
            Controls.Add(label4);
            Controls.Add(lstLog);
            Controls.Add(txtExtList);
            Controls.Add(lblPendingCount);
            Controls.Add(lblCurrentFile);
            Controls.Add(lblStatus);
            Controls.Add(pictureBoxPreview);
            Controls.Add(btnStop);
            Controls.Add(btnStart);
            Controls.Add(txtScanInterval);
            Controls.Add(label3);
            Controls.Add(txtServerUrl);
            Controls.Add(label2);
            Controls.Add(txtWatchPath);
            Controls.Add(label1);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "차량이미지 전송프로그램";
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
            Shown += MainForm_Shown;
            ((System.ComponentModel.ISupportInitialize)pictureBoxPreview).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox txtWatchPath;
        private TextBox txtServerUrl;
        private Label label2;
        private TextBox txtScanInterval;
        private Label label3;
        private Button btnStart;
        private Button btnStop;
        private PictureBox pictureBoxPreview;
        private Label lblStatus;
        private Label lblCurrentFile;
        private Label lblPendingCount;
        private TextBox txtExtList;
        private ListBox lstLog;
        private Label label4;
        private NotifyIcon _trayIcon;
    }
}
