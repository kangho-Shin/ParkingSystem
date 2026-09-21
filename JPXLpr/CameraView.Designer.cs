namespace JPXLpr
{
    partial class CameraView
    {
        /// <summary> 
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 구성 요소 디자이너에서 생성한 코드

        /// <summary> 
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            tplMain = new TableLayoutPanel();
            panButtom = new Panel();
            lblStatus = new Label();
            panBody = new Panel();
            picBox = new PictureBox();
            panTop = new Panel();
            btnSetup = new Button();
            chVideo = new CheckBox();
            btnTrigger = new Button();
            tplMain.SuspendLayout();
            panButtom.SuspendLayout();
            panBody.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picBox).BeginInit();
            panTop.SuspendLayout();
            SuspendLayout();
            // 
            // tplMain
            // 
            tplMain.ColumnCount = 1;
            tplMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tplMain.Controls.Add(panButtom, 0, 2);
            tplMain.Controls.Add(panBody, 0, 1);
            tplMain.Controls.Add(panTop, 0, 0);
            tplMain.Dock = DockStyle.Fill;
            tplMain.Location = new Point(0, 0);
            tplMain.Name = "tplMain";
            tplMain.RowCount = 3;
            tplMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tplMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tplMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tplMain.Size = new Size(364, 250);
            tplMain.TabIndex = 0;
            // 
            // panButtom
            // 
            panButtom.BackColor = SystemColors.ActiveBorder;
            panButtom.Controls.Add(lblStatus);
            panButtom.Dock = DockStyle.Fill;
            panButtom.Location = new Point(0, 220);
            panButtom.Margin = new Padding(0);
            panButtom.Name = "panButtom";
            panButtom.Size = new Size(364, 30);
            panButtom.TabIndex = 0;
            // 
            // lblStatus
            // 
            lblStatus.Dock = DockStyle.Fill;
            lblStatus.Location = new Point(0, 0);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(364, 30);
            lblStatus.TabIndex = 0;
            lblStatus.Text = "초기화중";
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // panBody
            // 
            panBody.Controls.Add(picBox);
            panBody.Dock = DockStyle.Fill;
            panBody.Location = new Point(3, 33);
            panBody.Name = "panBody";
            panBody.Size = new Size(358, 184);
            panBody.TabIndex = 1;
            // 
            // picBox
            // 
            picBox.BorderStyle = BorderStyle.FixedSingle;
            picBox.Dock = DockStyle.Fill;
            picBox.Location = new Point(0, 0);
            picBox.Name = "picBox";
            picBox.Size = new Size(358, 184);
            picBox.SizeMode = PictureBoxSizeMode.StretchImage;
            picBox.TabIndex = 0;
            picBox.TabStop = false;
            picBox.DoubleClick += picBox_DoubleClick;
            // 
            // panTop
            // 
            panTop.BackColor = SystemColors.ActiveBorder;
            panTop.Controls.Add(btnSetup);
            panTop.Controls.Add(chVideo);
            panTop.Controls.Add(btnTrigger);
            panTop.Dock = DockStyle.Fill;
            panTop.Location = new Point(0, 0);
            panTop.Margin = new Padding(0);
            panTop.Name = "panTop";
            panTop.Size = new Size(364, 30);
            panTop.TabIndex = 2;
            // 
            // btnSetup
            // 
            btnSetup.Location = new Point(277, 3);
            btnSetup.Name = "btnSetup";
            btnSetup.Size = new Size(75, 23);
            btnSetup.TabIndex = 2;
            btnSetup.Text = "설정";
            btnSetup.UseVisualStyleBackColor = true;
            btnSetup.Click += btnSetup_Click;
            // 
            // chVideo
            // 
            chVideo.AutoSize = true;
            chVideo.Location = new Point(25, 8);
            chVideo.Name = "chVideo";
            chVideo.Size = new Size(15, 14);
            chVideo.TabIndex = 1;
            chVideo.UseVisualStyleBackColor = true;
            chVideo.CheckedChanged += chVideo_CheckedChanged;
            // 
            // btnTrigger
            // 
            btnTrigger.Location = new Point(59, 3);
            btnTrigger.Name = "btnTrigger";
            btnTrigger.Size = new Size(106, 23);
            btnTrigger.TabIndex = 0;
            btnTrigger.Text = "트리거";
            btnTrigger.UseVisualStyleBackColor = true;
            btnTrigger.Click += btnTrigger_Click;
            // 
            // CameraView
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(tplMain);
            DoubleBuffered = true;
            Name = "CameraView";
            Size = new Size(364, 250);
            Load += CameraView_Load;
            tplMain.ResumeLayout(false);
            panButtom.ResumeLayout(false);
            panBody.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)picBox).EndInit();
            panTop.ResumeLayout(false);
            panTop.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tplMain;
        private Panel panButtom;
        private Panel panBody;
        private Panel panTop;
        private Button btnSetup;
        private CheckBox chVideo;
        private Button btnTrigger;
        private PictureBox picBox;
        private Label lblStatus;
    }
}
