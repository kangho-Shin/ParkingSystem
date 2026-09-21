namespace APSMain
{
    partial class CarInNumForm
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
            pZoomContent = new Panel();
            panCarNum = new APSMain.BaseClass.PanLabel();
            btnPre = new Button();
            btnHome = new Button();
            lblTitel = new Label();
            btnOk = new Button();
            picCarImage = new PictureBox();
            lblCarNum = new Label();
            btnDelChar = new Button();
            btnCancelNum = new Button();
            btnNum0 = new Button();
            btnNum9 = new Button();
            btnNum8 = new Button();
            btnNum7 = new Button();
            btnNum6 = new Button();
            btnNum5 = new Button();
            btnNum4 = new Button();
            btnNum3 = new Button();
            btnNum2 = new Button();
            btnNum1 = new Button();
            pZoomContent.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picCarImage).BeginInit();
            SuspendLayout();
            // 
            // pZoomContent
            // 
            pZoomContent.BackColor = Color.FromArgb(236, 239, 241);
            pZoomContent.BackgroundImage = Properties.Resources.background;
            pZoomContent.BackgroundImageLayout = ImageLayout.Stretch;
            pZoomContent.Controls.Add(panCarNum);
            pZoomContent.Controls.Add(btnPre);
            pZoomContent.Controls.Add(btnHome);
            pZoomContent.Controls.Add(lblTitel);
            pZoomContent.Controls.Add(btnOk);
            pZoomContent.Controls.Add(picCarImage);
            pZoomContent.Controls.Add(lblCarNum);
            pZoomContent.Controls.Add(btnDelChar);
            pZoomContent.Controls.Add(btnCancelNum);
            pZoomContent.Controls.Add(btnNum0);
            pZoomContent.Controls.Add(btnNum9);
            pZoomContent.Controls.Add(btnNum8);
            pZoomContent.Controls.Add(btnNum7);
            pZoomContent.Controls.Add(btnNum6);
            pZoomContent.Controls.Add(btnNum5);
            pZoomContent.Controls.Add(btnNum4);
            pZoomContent.Controls.Add(btnNum3);
            pZoomContent.Controls.Add(btnNum2);
            pZoomContent.Controls.Add(btnNum1);
            pZoomContent.Dock = DockStyle.Fill;
            pZoomContent.Location = new Point(0, 0);
            pZoomContent.Margin = new Padding(0);
            pZoomContent.Name = "pZoomContent";
            pZoomContent.Size = new Size(1060, 795);
            pZoomContent.TabIndex = 0;
            // 
            // panCarNum
            // 
            panCarNum.BackColor = Color.Transparent;
            panCarNum.BackgroundImage = Properties.Resources.platenum;
            panCarNum.BackgroundImageLayout = ImageLayout.Stretch;
            panCarNum.Font = new Font("맑은 고딕", 40F, FontStyle.Bold);
            panCarNum.Location = new Point(32, 124);
            panCarNum.Name = "panCarNum";
            panCarNum.Size = new Size(562, 83);
            panCarNum.TabIndex = 17;
            panCarNum.TextAlign = ContentAlignment.MiddleCenter;
            panCarNum.TextYOffset = 0;
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
            btnPre.TabIndex = 13;
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
            btnHome.TabIndex = 12;
            btnHome.Tag = "btnHome.mp3";
            btnHome.Text = "  홈";
            btnHome.UseVisualStyleBackColor = false;
            btnHome.Click += btnHome_Click;
            btnHome.MouseDown += btn_MouseDown;
            btnHome.MouseUp += btn_MouseUp;
            // 
            // lblTitel
            // 
            lblTitel.BackColor = Color.Transparent;
            lblTitel.Font = new Font("맑은 고딕", 36F, FontStyle.Bold, GraphicsUnit.Point, 129);
            lblTitel.ForeColor = SystemColors.Control;
            lblTitel.Location = new Point(80, 30);
            lblTitel.Name = "lblTitel";
            lblTitel.Size = new Size(900, 69);
            lblTitel.TabIndex = 16;
            lblTitel.Text = "차량번호 4자리를 입력 하세요.";
            lblTitel.TextAlign = ContentAlignment.MiddleCenter;
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
            btnOk.TabIndex = 14;
            btnOk.Tag = "btnOk.mp3";
            btnOk.Text = "  확 인";
            btnOk.UseVisualStyleBackColor = false;
            btnOk.Click += btnOk_Click;
            btnOk.MouseDown += btn_MouseDown;
            btnOk.MouseUp += btn_MouseUp;
            // 
            // picCarImage
            // 
            picCarImage.InitialImage = Properties.Resources.carImage;
            picCarImage.Location = new Point(38, 218);
            picCarImage.Name = "picCarImage";
            picCarImage.Size = new Size(546, 424);
            picCarImage.SizeMode = PictureBoxSizeMode.StretchImage;
            picCarImage.TabIndex = 15;
            picCarImage.TabStop = false;
            // 
            // lblCarNum
            // 
            lblCarNum.BackColor = Color.FromArgb(236, 239, 241);
            lblCarNum.Font = new Font("Consolas", 60F, FontStyle.Bold);
            lblCarNum.ForeColor = Color.Black;
            lblCarNum.Location = new Point(605, 124);
            lblCarNum.Margin = new Padding(0);
            lblCarNum.Name = "lblCarNum";
            lblCarNum.Size = new Size(413, 89);
            lblCarNum.TabIndex = 15;
            lblCarNum.Text = "0 0 0 0";
            lblCarNum.TextAlign = ContentAlignment.TopCenter;
            lblCarNum.Paint += lblCarNum_Paint;
            // 
            // btnDelChar
            // 
            btnDelChar.BackColor = Color.FromArgb(60, 70, 84);
            btnDelChar.FlatAppearance.BorderColor = Color.FromArgb(51, 51, 51);
            btnDelChar.FlatAppearance.BorderSize = 3;
            btnDelChar.Font = new Font("맑은 고딕", 24.75F, FontStyle.Bold);
            btnDelChar.ForeColor = Color.White;
            btnDelChar.Location = new Point(890, 544);
            btnDelChar.Margin = new Padding(0);
            btnDelChar.Name = "btnDelChar";
            btnDelChar.Size = new Size(128, 100);
            btnDelChar.TabIndex = 11;
            btnDelChar.Tag = "BS0.mp3";
            btnDelChar.Text = "정정  ( # )";
            btnDelChar.UseVisualStyleBackColor = false;
            btnDelChar.Click += btnDelChar_Click;
            // 
            // btnCancelNum
            // 
            btnCancelNum.BackColor = Color.FromArgb(60, 70, 84);
            btnCancelNum.FlatAppearance.BorderColor = Color.FromArgb(51, 51, 51);
            btnCancelNum.FlatAppearance.BorderSize = 3;
            btnCancelNum.Font = new Font("맑은 고딕", 24.75F, FontStyle.Bold, GraphicsUnit.Point, 129);
            btnCancelNum.ForeColor = Color.White;
            btnCancelNum.Location = new Point(605, 544);
            btnCancelNum.Margin = new Padding(0);
            btnCancelNum.Name = "btnCancelNum";
            btnCancelNum.Size = new Size(128, 100);
            btnCancelNum.TabIndex = 10;
            btnCancelNum.Tag = "DEL0.mp3";
            btnCancelNum.Text = "초기화( * )";
            btnCancelNum.UseVisualStyleBackColor = false;
            btnCancelNum.Click += btnCancelNum_Click;
            // 
            // btnNum0
            // 
            btnNum0.BackColor = Color.FromArgb(60, 70, 84);
            btnNum0.FlatAppearance.BorderColor = Color.FromArgb(51, 51, 51);
            btnNum0.FlatAppearance.BorderSize = 3;
            btnNum0.Font = new Font("Consolas", 52F, FontStyle.Bold);
            btnNum0.ForeColor = Color.White;
            btnNum0.Location = new Point(747, 544);
            btnNum0.Margin = new Padding(0);
            btnNum0.Name = "btnNum0";
            btnNum0.Size = new Size(128, 100);
            btnNum0.TabIndex = 9;
            btnNum0.Tag = "00.mp3";
            btnNum0.Text = "0";
            btnNum0.UseVisualStyleBackColor = false;
            btnNum0.Click += btnNum_Click;
            // 
            // btnNum9
            // 
            btnNum9.BackColor = Color.FromArgb(60, 70, 84);
            btnNum9.FlatAppearance.BorderColor = Color.FromArgb(51, 51, 51);
            btnNum9.FlatAppearance.BorderSize = 3;
            btnNum9.Font = new Font("Consolas", 52F, FontStyle.Bold);
            btnNum9.ForeColor = Color.White;
            btnNum9.Location = new Point(890, 435);
            btnNum9.Margin = new Padding(0);
            btnNum9.Name = "btnNum9";
            btnNum9.Size = new Size(128, 100);
            btnNum9.TabIndex = 8;
            btnNum9.Tag = "90.mp3";
            btnNum9.Text = "9";
            btnNum9.UseVisualStyleBackColor = false;
            btnNum9.Click += btnNum_Click;
            // 
            // btnNum8
            // 
            btnNum8.BackColor = Color.FromArgb(60, 70, 84);
            btnNum8.FlatAppearance.BorderColor = Color.FromArgb(51, 51, 51);
            btnNum8.FlatAppearance.BorderSize = 3;
            btnNum8.Font = new Font("Consolas", 52F, FontStyle.Bold);
            btnNum8.ForeColor = Color.White;
            btnNum8.Location = new Point(747, 435);
            btnNum8.Margin = new Padding(0);
            btnNum8.Name = "btnNum8";
            btnNum8.Size = new Size(128, 100);
            btnNum8.TabIndex = 7;
            btnNum8.Tag = "80.mp3";
            btnNum8.Text = "8";
            btnNum8.UseVisualStyleBackColor = false;
            btnNum8.Click += btnNum_Click;
            // 
            // btnNum7
            // 
            btnNum7.BackColor = Color.FromArgb(60, 70, 84);
            btnNum7.FlatAppearance.BorderColor = Color.FromArgb(51, 51, 51);
            btnNum7.FlatAppearance.BorderSize = 3;
            btnNum7.Font = new Font("Consolas", 52F, FontStyle.Bold);
            btnNum7.ForeColor = Color.White;
            btnNum7.Location = new Point(605, 435);
            btnNum7.Margin = new Padding(0);
            btnNum7.Name = "btnNum7";
            btnNum7.Size = new Size(128, 100);
            btnNum7.TabIndex = 6;
            btnNum7.Tag = "70.mp3";
            btnNum7.Text = "7";
            btnNum7.UseVisualStyleBackColor = false;
            btnNum7.Click += btnNum_Click;
            // 
            // btnNum6
            // 
            btnNum6.BackColor = Color.FromArgb(60, 70, 84);
            btnNum6.FlatAppearance.BorderColor = Color.FromArgb(51, 51, 51);
            btnNum6.FlatAppearance.BorderSize = 3;
            btnNum6.Font = new Font("Consolas", 52F, FontStyle.Bold);
            btnNum6.ForeColor = Color.White;
            btnNum6.Location = new Point(890, 328);
            btnNum6.Margin = new Padding(0);
            btnNum6.Name = "btnNum6";
            btnNum6.Size = new Size(128, 100);
            btnNum6.TabIndex = 5;
            btnNum6.Tag = "60.mp3";
            btnNum6.Text = "6";
            btnNum6.UseVisualStyleBackColor = false;
            btnNum6.Click += btnNum_Click;
            // 
            // btnNum5
            // 
            btnNum5.BackColor = Color.FromArgb(60, 70, 84);
            btnNum5.FlatAppearance.BorderColor = Color.FromArgb(51, 51, 51);
            btnNum5.FlatAppearance.BorderSize = 3;
            btnNum5.Font = new Font("Consolas", 52F, FontStyle.Bold);
            btnNum5.ForeColor = Color.White;
            btnNum5.Location = new Point(747, 328);
            btnNum5.Margin = new Padding(0);
            btnNum5.Name = "btnNum5";
            btnNum5.Size = new Size(128, 100);
            btnNum5.TabIndex = 4;
            btnNum5.Tag = "50.mp3";
            btnNum5.Text = "5";
            btnNum5.UseVisualStyleBackColor = false;
            btnNum5.Click += btnNum_Click;
            // 
            // btnNum4
            // 
            btnNum4.BackColor = Color.FromArgb(60, 70, 84);
            btnNum4.FlatAppearance.BorderColor = Color.FromArgb(51, 51, 51);
            btnNum4.FlatAppearance.BorderSize = 3;
            btnNum4.Font = new Font("Consolas", 52F, FontStyle.Bold);
            btnNum4.ForeColor = Color.White;
            btnNum4.Location = new Point(605, 328);
            btnNum4.Margin = new Padding(0);
            btnNum4.Name = "btnNum4";
            btnNum4.Size = new Size(128, 100);
            btnNum4.TabIndex = 3;
            btnNum4.Tag = "40.mp3";
            btnNum4.Text = "4";
            btnNum4.UseVisualStyleBackColor = false;
            btnNum4.Click += btnNum_Click;
            // 
            // btnNum3
            // 
            btnNum3.BackColor = Color.FromArgb(60, 70, 84);
            btnNum3.FlatAppearance.BorderColor = Color.FromArgb(51, 51, 51);
            btnNum3.FlatAppearance.BorderSize = 3;
            btnNum3.Font = new Font("Consolas", 52F, FontStyle.Bold);
            btnNum3.ForeColor = Color.White;
            btnNum3.Location = new Point(890, 220);
            btnNum3.Margin = new Padding(0);
            btnNum3.Name = "btnNum3";
            btnNum3.Size = new Size(128, 100);
            btnNum3.TabIndex = 2;
            btnNum3.Tag = "30.mp3";
            btnNum3.Text = "3";
            btnNum3.UseVisualStyleBackColor = false;
            btnNum3.Click += btnNum_Click;
            // 
            // btnNum2
            // 
            btnNum2.BackColor = Color.FromArgb(60, 70, 84);
            btnNum2.FlatAppearance.BorderColor = Color.FromArgb(51, 51, 51);
            btnNum2.FlatAppearance.BorderSize = 3;
            btnNum2.Font = new Font("Consolas", 52F, FontStyle.Bold);
            btnNum2.ForeColor = Color.White;
            btnNum2.Location = new Point(747, 220);
            btnNum2.Margin = new Padding(0);
            btnNum2.Name = "btnNum2";
            btnNum2.Size = new Size(128, 100);
            btnNum2.TabIndex = 1;
            btnNum2.Tag = "20.mp3";
            btnNum2.Text = "2";
            btnNum2.UseVisualStyleBackColor = false;
            btnNum2.Click += btnNum_Click;
            // 
            // btnNum1
            // 
            btnNum1.BackColor = Color.FromArgb(60, 70, 84);
            btnNum1.FlatAppearance.BorderColor = Color.FromArgb(51, 51, 51);
            btnNum1.FlatAppearance.BorderSize = 3;
            btnNum1.Font = new Font("Consolas", 52F, FontStyle.Bold);
            btnNum1.ForeColor = Color.White;
            btnNum1.Location = new Point(605, 220);
            btnNum1.Margin = new Padding(0);
            btnNum1.Name = "btnNum1";
            btnNum1.Size = new Size(128, 100);
            btnNum1.TabIndex = 0;
            btnNum1.Tag = "10.mp3";
            btnNum1.Text = "1";
            btnNum1.UseVisualStyleBackColor = false;
            btnNum1.Click += btnNum_Click;
            // 
            // CarInNumForm
            // 
            AutoScaleDimensions = new SizeF(15F, 37F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1060, 795);
            Controls.Add(pZoomContent);
            DoubleBuffered = true;
            Font = new Font("맑은 고딕", 20.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            FormBorderStyle = FormBorderStyle.None;
            KeyPreview = true;
            Margin = new Padding(6, 7, 6, 7);
            Name = "CarInNumForm";
            StartPosition = FormStartPosition.Manual;
            Text = "CarInNum";
            TopMost = true;
            FormClosing += CarInNumForm_FormClosing;
            Load += CarInNum_Load;
            Shown += CarInNumForm_Shown;
            KeyDown += CarInNumForm_KeyDown;
            pZoomContent.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)picCarImage).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel pZoomContent;
        private Button btnDelChar;
        private Button btnCancelNum;
        private Button btnNum0;
        private Button btnNum9;
        private Button btnNum8;
        private Button btnNum7;
        private Button btnNum6;
        private Button btnNum5;
        private Button btnNum4;
        private Button btnNum3;
        private Button btnNum2;
        private Button btnNum1;
        private Label lblCarNum;
        private Button btnOk;
        private PictureBox picCarImage;
        private Label lblTitel;
        private Button btnPre;
        private Button btnHome;
        private BaseClass.PanLabel panCarNum;
    }
}