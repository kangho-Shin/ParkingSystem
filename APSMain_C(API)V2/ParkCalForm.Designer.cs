using APSMain.BaseClass;

namespace APSMain
{
    partial class ParkCalForm
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
            lblName1 = new CenterLabel();
            lblName2 = new CenterLabel();
            lblName3 = new CenterLabel();
            lblName4 = new CenterLabel();
            lblName5 = new CenterLabel();
            lblName7 = new CenterLabel();
            lblName6 = new CenterLabel();
            lblInTime = new CenterLabel();
            lblOutTime = new CenterLabel();
            lblParkTime = new CenterLabel();
            lblPrePay = new CenterLabel();
            lblTotalFee = new CenterLabel();
            lblDisFee = new CenterLabel();
            lblParkFee = new CenterLabel();
            pZoomContent = new Panel();
            lblDisTitle = new CenterLabel();
            panCarNum = new PanLabel();
            btnTest = new Button();
            btnOk = new Button();
            btnPre = new Button();
            btnHome = new Button();
            lblTitle = new Label();
            picCarImage = new PictureBox();
            pZoomContent.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picCarImage).BeginInit();
            SuspendLayout();
            // 
            // lblName1
            // 
            lblName1.BackColor = Color.FromArgb(218, 217, 210);
            lblName1.Font = new Font("맑은 고딕", 32F, FontStyle.Bold);
            lblName1.ForeColor = Color.FromArgb(11, 18, 32);
            lblName1.Location = new Point(465, 154);
            lblName1.Name = "lblName1";
            lblName1.OffsetY = -3;
            lblName1.Size = new Size(223, 56);
            lblName1.TabIndex = 0;
            lblName1.Text = "입차시간 :";
            lblName1.TextAlign = ContentAlignment.TopCenter;
            // 
            // lblName2
            // 
            lblName2.BackColor = Color.FromArgb(218, 217, 210);
            lblName2.Font = new Font("맑은 고딕", 32F, FontStyle.Bold);
            lblName2.ForeColor = Color.FromArgb(11, 18, 32);
            lblName2.Location = new Point(465, 215);
            lblName2.Name = "lblName2";
            lblName2.OffsetY = -3;
            lblName2.Size = new Size(223, 56);
            lblName2.TabIndex = 0;
            lblName2.Text = "현재시간 :";
            lblName2.TextAlign = ContentAlignment.TopCenter;
            // 
            // lblName3
            // 
            lblName3.BackColor = Color.FromArgb(218, 217, 210);
            lblName3.Font = new Font("맑은 고딕", 32F, FontStyle.Bold);
            lblName3.ForeColor = Color.FromArgb(11, 18, 32);
            lblName3.Location = new Point(465, 276);
            lblName3.Name = "lblName3";
            lblName3.OffsetY = -3;
            lblName3.Size = new Size(223, 56);
            lblName3.TabIndex = 0;
            lblName3.Text = "주차시간 :";
            lblName3.TextAlign = ContentAlignment.TopCenter;
            // 
            // lblName4
            // 
            lblName4.BackColor = Color.FromArgb(218, 217, 210);
            lblName4.Font = new Font("맑은 고딕", 32F, FontStyle.Bold);
            lblName4.ForeColor = Color.FromArgb(11, 18, 32);
            lblName4.Location = new Point(465, 337);
            lblName4.Name = "lblName4";
            lblName4.OffsetY = -3;
            lblName4.Size = new Size(223, 56);
            lblName4.TabIndex = 0;
            lblName4.Text = "주차요금 :";
            lblName4.TextAlign = ContentAlignment.TopCenter;
            // 
            // lblName5
            // 
            lblName5.BackColor = Color.FromArgb(218, 217, 210);
            lblName5.Font = new Font("맑은 고딕", 32F, FontStyle.Bold);
            lblName5.ForeColor = Color.FromArgb(11, 18, 32);
            lblName5.Location = new Point(465, 398);
            lblName5.Name = "lblName5";
            lblName5.OffsetY = -3;
            lblName5.Size = new Size(223, 56);
            lblName5.TabIndex = 0;
            lblName5.Text = "할인요금 :";
            lblName5.TextAlign = ContentAlignment.TopCenter;
            // 
            // lblName7
            // 
            lblName7.BackColor = Color.FromArgb(218, 217, 210);
            lblName7.Font = new Font("맑은 고딕", 32F, FontStyle.Bold);
            lblName7.ForeColor = Color.FromArgb(11, 18, 32);
            lblName7.Location = new Point(465, 520);
            lblName7.Name = "lblName7";
            lblName7.OffsetY = -3;
            lblName7.Size = new Size(223, 56);
            lblName7.TabIndex = 0;
            lblName7.Text = "납부요금 :";
            lblName7.TextAlign = ContentAlignment.TopCenter;
            // 
            // lblName6
            // 
            lblName6.BackColor = Color.FromArgb(218, 217, 210);
            lblName6.Font = new Font("맑은 고딕", 32F, FontStyle.Bold);
            lblName6.ForeColor = Color.FromArgb(11, 18, 32);
            lblName6.Location = new Point(465, 459);
            lblName6.Name = "lblName6";
            lblName6.OffsetY = -3;
            lblName6.Size = new Size(223, 56);
            lblName6.TabIndex = 0;
            lblName6.Text = "사전정산 :";
            lblName6.TextAlign = ContentAlignment.TopCenter;
            // 
            // lblInTime
            // 
            lblInTime.BackColor = Color.FromArgb(239, 236, 223);
            lblInTime.Font = new Font("맑은 고딕", 34F, FontStyle.Bold);
            lblInTime.ForeColor = Color.FromArgb(11, 18, 32);
            lblInTime.Location = new Point(696, 154);
            lblInTime.Name = "lblInTime";
            lblInTime.OffsetY = -3;
            lblInTime.Size = new Size(337, 56);
            lblInTime.TabIndex = 0;
            lblInTime.Text = "01-01 12:30";
            lblInTime.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblOutTime
            // 
            lblOutTime.BackColor = Color.FromArgb(239, 236, 223);
            lblOutTime.Font = new Font("맑은 고딕", 34F, FontStyle.Bold);
            lblOutTime.ForeColor = Color.FromArgb(11, 18, 32);
            lblOutTime.ImageAlign = ContentAlignment.TopCenter;
            lblOutTime.Location = new Point(696, 215);
            lblOutTime.Name = "lblOutTime";
            lblOutTime.OffsetY = -3;
            lblOutTime.Size = new Size(337, 56);
            lblOutTime.TabIndex = 0;
            lblOutTime.Text = "12-31 12:30";
            lblOutTime.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblParkTime
            // 
            lblParkTime.BackColor = Color.FromArgb(239, 236, 223);
            lblParkTime.Font = new Font("맑은 고딕", 34F, FontStyle.Bold);
            lblParkTime.ForeColor = Color.FromArgb(11, 18, 32);
            lblParkTime.Location = new Point(696, 276);
            lblParkTime.Name = "lblParkTime";
            lblParkTime.OffsetY = -3;
            lblParkTime.Size = new Size(337, 56);
            lblParkTime.TabIndex = 0;
            lblParkTime.Text = "140 분";
            lblParkTime.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblPrePay
            // 
            lblPrePay.BackColor = Color.FromArgb(239, 236, 223);
            lblPrePay.Font = new Font("맑은 고딕", 34F, FontStyle.Bold);
            lblPrePay.ForeColor = Color.FromArgb(58, 122, 254);
            lblPrePay.Location = new Point(696, 459);
            lblPrePay.Name = "lblPrePay";
            lblPrePay.OffsetY = -3;
            lblPrePay.Size = new Size(337, 56);
            lblPrePay.TabIndex = 0;
            lblPrePay.Text = "0 원";
            lblPrePay.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblTotalFee
            // 
            lblTotalFee.BackColor = Color.FromArgb(239, 236, 223);
            lblTotalFee.Font = new Font("맑은 고딕", 34F, FontStyle.Bold);
            lblTotalFee.ForeColor = Color.FromArgb(11, 18, 32);
            lblTotalFee.Location = new Point(696, 337);
            lblTotalFee.Name = "lblTotalFee";
            lblTotalFee.OffsetY = -3;
            lblTotalFee.Size = new Size(337, 56);
            lblTotalFee.TabIndex = 0;
            lblTotalFee.Text = "2,000 원";
            lblTotalFee.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblDisFee
            // 
            lblDisFee.BackColor = Color.FromArgb(239, 236, 223);
            lblDisFee.Font = new Font("맑은 고딕", 34F, FontStyle.Bold);
            lblDisFee.ForeColor = Color.FromArgb(0, 192, 0);
            lblDisFee.Location = new Point(696, 398);
            lblDisFee.Name = "lblDisFee";
            lblDisFee.OffsetY = -3;
            lblDisFee.Size = new Size(337, 56);
            lblDisFee.TabIndex = 0;
            lblDisFee.Text = "1,000 원";
            lblDisFee.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblParkFee
            // 
            lblParkFee.BackColor = Color.FromArgb(255, 224, 192);
            lblParkFee.Font = new Font("맑은 고딕", 36F, FontStyle.Bold);
            lblParkFee.ForeColor = Color.Black;
            lblParkFee.Location = new Point(696, 520);
            lblParkFee.Name = "lblParkFee";
            lblParkFee.OffsetY = -3;
            lblParkFee.Size = new Size(337, 56);
            lblParkFee.TabIndex = 0;
            lblParkFee.Text = "1,000 원";
            lblParkFee.TextAlign = ContentAlignment.MiddleRight;
            // 
            // pZoomContent
            // 
            pZoomContent.BackColor = Color.FromArgb(90, 103, 116);
            pZoomContent.BackgroundImage = Properties.Resources.background;
            pZoomContent.BackgroundImageLayout = ImageLayout.Stretch;
            pZoomContent.Controls.Add(lblDisTitle);
            pZoomContent.Controls.Add(panCarNum);
            pZoomContent.Controls.Add(btnTest);
            pZoomContent.Controls.Add(btnOk);
            pZoomContent.Controls.Add(btnPre);
            pZoomContent.Controls.Add(btnHome);
            pZoomContent.Controls.Add(lblTitle);
            pZoomContent.Controls.Add(picCarImage);
            pZoomContent.Controls.Add(lblName1);
            pZoomContent.Controls.Add(lblParkFee);
            pZoomContent.Controls.Add(lblDisFee);
            pZoomContent.Controls.Add(lblName2);
            pZoomContent.Controls.Add(lblTotalFee);
            pZoomContent.Controls.Add(lblName3);
            pZoomContent.Controls.Add(lblPrePay);
            pZoomContent.Controls.Add(lblName4);
            pZoomContent.Controls.Add(lblParkTime);
            pZoomContent.Controls.Add(lblName5);
            pZoomContent.Controls.Add(lblOutTime);
            pZoomContent.Controls.Add(lblName7);
            pZoomContent.Controls.Add(lblInTime);
            pZoomContent.Controls.Add(lblName6);
            pZoomContent.ForeColor = Color.FromArgb(11, 18, 32);
            pZoomContent.Location = new Point(0, 0);
            pZoomContent.Margin = new Padding(0);
            pZoomContent.Name = "pZoomContent";
            pZoomContent.Size = new Size(1060, 795);
            pZoomContent.TabIndex = 0;
            pZoomContent.MouseDoubleClick += pZoomContent_MouseDoubleClick;
            // 
            // lblDisTitle
            // 
            lblDisTitle.BackColor = Color.Transparent;
            lblDisTitle.Font = new Font("맑은 고딕", 32F, FontStyle.Bold);
            lblDisTitle.ForeColor = Color.White;
            lblDisTitle.Location = new Point(18, 579);
            lblDisTitle.Name = "lblDisTitle";
            lblDisTitle.OffsetY = -2;
            lblDisTitle.Size = new Size(425, 56);
            lblDisTitle.TabIndex = 36;
            lblDisTitle.Text = "일반차량";
            lblDisTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // panCarNum
            // 
            panCarNum.BackColor = Color.Transparent;
            panCarNum.BackgroundImage = Properties.Resources.platenum;
            panCarNum.BackgroundImageLayout = ImageLayout.Stretch;
            panCarNum.Font = new Font("맑은 고딕", 40F, FontStyle.Bold);
            panCarNum.Location = new Point(16, 154);
            panCarNum.Name = "panCarNum";
            panCarNum.Size = new Size(435, 83);
            panCarNum.TabIndex = 35;
            panCarNum.TextAlign = ContentAlignment.MiddleCenter;
            panCarNum.TextYOffset = 0;
            // 
            // btnTest
            // 
            btnTest.BackColor = Color.FromArgb(0, 0, 3, 51);
            btnTest.FlatAppearance.BorderColor = Color.FromArgb(51, 51, 51);
            btnTest.FlatAppearance.BorderSize = 3;
            btnTest.FlatStyle = FlatStyle.Flat;
            btnTest.Font = new Font("맑은 고딕", 12F, FontStyle.Bold);
            btnTest.ForeColor = Color.FromArgb(51, 51, 51);
            btnTest.Location = new Point(879, 608);
            btnTest.Name = "btnTest";
            btnTest.Size = new Size(121, 42);
            btnTest.TabIndex = 3;
            btnTest.TabStop = false;
            btnTest.Text = "할인하기";
            btnTest.UseVisualStyleBackColor = false;
            btnTest.Visible = false;
            btnTest.Click += btnTest_Click;
            // 
            // btnOk
            // 
            btnOk.BackColor = Color.FromArgb(239, 236, 223);
            btnOk.FlatAppearance.BorderColor = Color.FromArgb(51, 51, 51);
            btnOk.FlatAppearance.BorderSize = 3;
            btnOk.FlatStyle = FlatStyle.Flat;
            btnOk.Font = new Font("맑은 고딕", 44F, FontStyle.Bold);
            btnOk.ForeColor = Color.FromArgb(17, 17, 17);
            btnOk.Image = Properties.Resources.confirm;
            btnOk.ImageAlign = ContentAlignment.MiddleLeft;
            btnOk.Location = new Point(760, 655);
            btnOk.Name = "btnOk";
            btnOk.Padding = new Padding(10, 0, 0, 0);
            btnOk.Size = new Size(240, 100);
            btnOk.TabIndex = 2;
            btnOk.Tag = "btnOk.mp3";
            btnOk.Text = "  확인";
            btnOk.UseVisualStyleBackColor = false;
            btnOk.Click += btnOk_Click;
            btnOk.MouseDown += btn_MouseDown;
            btnOk.MouseUp += btn_MouseUp;
            // 
            // btnPre
            // 
            btnPre.BackColor = Color.FromArgb(239, 236, 223);
            btnPre.FlatAppearance.BorderColor = Color.FromArgb(51, 51, 51);
            btnPre.FlatAppearance.BorderSize = 3;
            btnPre.FlatStyle = FlatStyle.Flat;
            btnPre.Font = new Font("맑은 고딕", 44F, FontStyle.Bold);
            btnPre.ForeColor = Color.FromArgb(17, 17, 17);
            btnPre.Image = Properties.Resources.prev;
            btnPre.ImageAlign = ContentAlignment.MiddleLeft;
            btnPre.Location = new Point(344, 655);
            btnPre.Name = "btnPre";
            btnPre.Padding = new Padding(10, 0, 0, 0);
            btnPre.Size = new Size(240, 100);
            btnPre.TabIndex = 1;
            btnPre.Tag = "btnPre.mp3";
            btnPre.Text = "  이전";
            btnPre.UseVisualStyleBackColor = false;
            btnPre.Click += btnPre_Click;
            btnPre.MouseDown += btn_MouseDown;
            btnPre.MouseUp += btn_MouseUp;
            // 
            // btnHome
            // 
            btnHome.BackColor = Color.FromArgb(239, 236, 223);
            btnHome.FlatAppearance.BorderColor = Color.FromArgb(51, 51, 51);
            btnHome.FlatAppearance.BorderSize = 3;
            btnHome.FlatStyle = FlatStyle.Flat;
            btnHome.Font = new Font("맑은 고딕", 44F, FontStyle.Bold);
            btnHome.ForeColor = Color.FromArgb(17, 17, 17);
            btnHome.Image = Properties.Resources.home;
            btnHome.ImageAlign = ContentAlignment.MiddleLeft;
            btnHome.Location = new Point(60, 655);
            btnHome.Name = "btnHome";
            btnHome.Padding = new Padding(10, 0, 0, 0);
            btnHome.Size = new Size(240, 100);
            btnHome.TabIndex = 0;
            btnHome.Tag = "btnHome.mp3";
            btnHome.Text = "  홈";
            btnHome.UseVisualStyleBackColor = false;
            btnHome.Click += btnHome_Click;
            btnHome.MouseDown += btn_MouseDown;
            btnHome.MouseUp += btn_MouseUp;
            // 
            // lblTitle
            // 
            lblTitle.BackColor = Color.Transparent;
            lblTitle.Font = new Font("맑은 고딕", 36F, FontStyle.Bold, GraphicsUnit.Point, 129);
            lblTitle.ForeColor = SystemColors.Control;
            lblTitle.Location = new Point(80, 30);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(900, 69);
            lblTitle.TabIndex = 34;
            lblTitle.Text = "신용카드를 투입구에 넣어주십시오";
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // picCarImage
            // 
            picCarImage.BackColor = Color.Silver;
            picCarImage.Location = new Point(20, 240);
            picCarImage.Name = "picCarImage";
            picCarImage.Size = new Size(423, 336);
            picCarImage.SizeMode = PictureBoxSizeMode.StretchImage;
            picCarImage.TabIndex = 31;
            picCarImage.TabStop = false;
            // 
            // ParkCalForm
            // 
            AutoScaleDimensions = new SizeF(29F, 65F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1060, 795);
            Controls.Add(pZoomContent);
            DoubleBuffered = true;
            Font = new Font("맑은 고딕", 36F, FontStyle.Bold);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(12, 13, 12, 13);
            Name = "ParkCalForm";
            StartPosition = FormStartPosition.Manual;
            Text = "ParkCalFrm";
            TopMost = true;
            FormClosing += ParkCalForm_FormClosing;
            FormClosed += ParkCalForm_FormClosed;
            Load += ParkCalFrm_Load;
            Shown += ParkCalForm_Shown;
            pZoomContent.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)picCarImage).EndInit();
            ResumeLayout(false);
        }

        #endregion
        //private Label label1;
        //private Label label2;
        //private Label label3;
        //private Label label4;
        //private Label label5;
        //private Label label6;
        //private Label label7;
        private Panel pZoomContent;
        private PictureBox picCarImage;
        private Label lblTitle;
        private Button btnPre;
        private Button btnHome;
        private Button btnOk;
        private Button btnTest;
        private BaseClass.PanLabel panCarNum;
        private CenterLabel lblName1;
        private CenterLabel lblName2;
        private CenterLabel lblName3;
        private CenterLabel lblName4;
        private CenterLabel lblName5;
        private CenterLabel lblName7;
        private CenterLabel lblName6;
        private CenterLabel lblInTime;
        private CenterLabel lblOutTime;
        private CenterLabel lblParkTime;
        private CenterLabel lblPrePay;
        private CenterLabel lblTotalFee;
        private CenterLabel lblDisFee;
        private CenterLabel lblParkFee;
        private CenterLabel lblDisTitle;
    }
}