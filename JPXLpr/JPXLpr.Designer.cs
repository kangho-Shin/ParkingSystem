namespace JPXLpr
{
    partial class JPXLpr
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
            tlpMain = new TableLayoutPanel();
            lbLog = new ListBox();
            listBox2 = new ListBox();
            camTV1 = new CameraView();
            camTV2 = new CameraView();
            camTV3 = new CameraView();
            camTV4 = new CameraView();
            panMenu = new Panel();
            btnExit = new Button();
            btnTest = new Button();
            btnOutFile = new Button();
            btnInFile = new Button();
            tlpMain.SuspendLayout();
            panMenu.SuspendLayout();
            SuspendLayout();
            // 
            // tlpMain
            // 
            tlpMain.ColumnCount = 3;
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            tlpMain.Controls.Add(lbLog, 0, 2);
            tlpMain.Controls.Add(listBox2, 2, 0);
            tlpMain.Controls.Add(camTV1, 0, 0);
            tlpMain.Controls.Add(camTV2, 1, 0);
            tlpMain.Controls.Add(camTV3, 0, 1);
            tlpMain.Controls.Add(camTV4, 1, 1);
            tlpMain.Controls.Add(panMenu, 2, 2);
            tlpMain.Dock = DockStyle.Fill;
            tlpMain.Location = new Point(0, 0);
            tlpMain.Name = "tlpMain";
            tlpMain.RowCount = 3;
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F));
            tlpMain.Size = new Size(895, 667);
            tlpMain.TabIndex = 5;
            // 
            // lbLog
            // 
            tlpMain.SetColumnSpan(lbLog, 2);
            lbLog.Dock = DockStyle.Fill;
            lbLog.FormattingEnabled = true;
            lbLog.ItemHeight = 15;
            lbLog.Location = new Point(3, 519);
            lbLog.Name = "lbLog";
            lbLog.Size = new Size(738, 145);
            lbLog.TabIndex = 5;
            // 
            // listBox2
            // 
            listBox2.Dock = DockStyle.Fill;
            listBox2.FormattingEnabled = true;
            listBox2.ItemHeight = 15;
            listBox2.Location = new Point(747, 3);
            listBox2.Name = "listBox2";
            tlpMain.SetRowSpan(listBox2, 2);
            listBox2.Size = new Size(145, 510);
            listBox2.TabIndex = 6;
            // 
            // camTV1
            // 
            camTV1._bracketCnt = 1;
            camTV1._bracketMode = 0;
            camTV1._isBusy = false;
            camTV1._oldExposure = -1;
            camTV1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            camTV1.BorderStyle = BorderStyle.FixedSingle;
            camTV1.CameraNo = 0;
            camTV1.Dock = DockStyle.Fill;
            camTV1.Ip = null;
            camTV1.isRunning = false;
            camTV1.isVideoMode = false;
            camTV1.Location = new Point(3, 3);
            camTV1.Name = "camTV1";
            camTV1.Size = new Size(366, 252);
            camTV1.TabIndex = 7;
            // 
            // camTV2
            // 
            camTV2._bracketCnt = 1;
            camTV2._bracketMode = 0;
            camTV2._isBusy = false;
            camTV2._oldExposure = -1;
            camTV2.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            camTV2.BorderStyle = BorderStyle.FixedSingle;
            camTV2.CameraNo = 0;
            camTV2.Dock = DockStyle.Fill;
            camTV2.Ip = null;
            camTV2.isRunning = false;
            camTV2.isVideoMode = false;
            camTV2.Location = new Point(375, 3);
            camTV2.Name = "camTV2";
            camTV2.Size = new Size(366, 252);
            camTV2.TabIndex = 8;
            // 
            // camTV3
            // 
            camTV3._bracketCnt = 1;
            camTV3._bracketMode = 0;
            camTV3._isBusy = false;
            camTV3._oldExposure = -1;
            camTV3.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            camTV3.BorderStyle = BorderStyle.FixedSingle;
            camTV3.CameraNo = 0;
            camTV3.Dock = DockStyle.Fill;
            camTV3.Ip = null;
            camTV3.isRunning = false;
            camTV3.isVideoMode = false;
            camTV3.Location = new Point(3, 261);
            camTV3.Name = "camTV3";
            camTV3.Size = new Size(366, 252);
            camTV3.TabIndex = 9;
            // 
            // camTV4
            // 
            camTV4._bracketCnt = 1;
            camTV4._bracketMode = 0;
            camTV4._isBusy = false;
            camTV4._oldExposure = -1;
            camTV4.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            camTV4.BorderStyle = BorderStyle.FixedSingle;
            camTV4.CameraNo = 0;
            camTV4.Dock = DockStyle.Fill;
            camTV4.Ip = null;
            camTV4.isRunning = false;
            camTV4.isVideoMode = false;
            camTV4.Location = new Point(375, 261);
            camTV4.Name = "camTV4";
            camTV4.Size = new Size(366, 252);
            camTV4.TabIndex = 10;
            // 
            // panMenu
            // 
            panMenu.Controls.Add(btnExit);
            panMenu.Controls.Add(btnTest);
            panMenu.Controls.Add(btnOutFile);
            panMenu.Controls.Add(btnInFile);
            panMenu.Dock = DockStyle.Fill;
            panMenu.Location = new Point(744, 516);
            panMenu.Margin = new Padding(0);
            panMenu.Name = "panMenu";
            panMenu.Size = new Size(151, 151);
            panMenu.TabIndex = 11;
            panMenu.MouseClick += panMenu_MouseClick;
            // 
            // btnExit
            // 
            btnExit.Location = new Point(8, 107);
            btnExit.Name = "btnExit";
            btnExit.Size = new Size(131, 38);
            btnExit.TabIndex = 3;
            btnExit.Text = "종료";
            btnExit.UseVisualStyleBackColor = true;
            btnExit.Click += btnExit_Click;
            // 
            // btnTest
            // 
            btnTest.Location = new Point(8, 72);
            btnTest.Name = "btnTest";
            btnTest.Size = new Size(131, 23);
            btnTest.TabIndex = 2;
            btnTest.Text = "테스트";
            btnTest.UseVisualStyleBackColor = true;
            btnTest.Click += btnTest_Click;
            // 
            // btnOutFile
            // 
            btnOutFile.Location = new Point(8, 43);
            btnOutFile.Name = "btnOutFile";
            btnOutFile.Size = new Size(131, 23);
            btnOutFile.TabIndex = 1;
            btnOutFile.Text = "출구파일인식";
            btnOutFile.UseVisualStyleBackColor = true;
            btnOutFile.Click += btnOutFile_Click;
            // 
            // btnInFile
            // 
            btnInFile.Location = new Point(8, 14);
            btnInFile.Name = "btnInFile";
            btnInFile.Size = new Size(131, 23);
            btnInFile.TabIndex = 0;
            btnInFile.Text = "입구파일인식";
            btnInFile.UseVisualStyleBackColor = true;
            btnInFile.Click += btnInFile_Click;
            // 
            // JPXLpr
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(895, 667);
            Controls.Add(tlpMain);
            Name = "JPXLpr";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "JPXLPR Ver 0.01";
            FormClosing += JPXLpr_FormClosing;
            Load += JPXLpr_Load;
            tlpMain.ResumeLayout(false);
            panMenu.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tlpMain;
        private ListBox lbLog;
        private ListBox listBox2;
        private CameraView camTV1;
        private CameraView camTV2;
        private CameraView camTV3;
        private CameraView camTV4;
        private Panel panMenu;
        private Button btnTest;
        private Button btnOutFile;
        private Button btnInFile;
        private Button btnExit;
    }
}
