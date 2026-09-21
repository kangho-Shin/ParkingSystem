namespace APSMain
{
    partial class PeriodRenew
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
            if (disposing && (components != null)) {
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
            btnOk = new Button();
            lblTitel = new Label();
            btnHome = new Button();
            btnPre = new Button();
            pZoomContent = new Panel();
            panInfo = new Panel();
            btnPayment = new Button();
            btnSearch = new Button();
            txtCarNum = new TextBox();
            btnEu = new Button();
            btnDot = new Button();
            btnI = new Button();
            btnDelete = new Button();
            btnMode = new Button();
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
            SuspendLayout();
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
            lblTitel.Text = "기간 연장할 차량번호를 입력하세요.";
            lblTitel.TextAlign = ContentAlignment.MiddleCenter;
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
            // 
            // pZoomContent
            // 
            pZoomContent.BackColor = Color.FromArgb(236, 239, 241);
            pZoomContent.BackgroundImage = Properties.Resources.background;
            pZoomContent.BackgroundImageLayout = ImageLayout.Stretch;
            pZoomContent.Controls.Add(panInfo);
            pZoomContent.Controls.Add(btnPayment);
            pZoomContent.Controls.Add(btnSearch);
            pZoomContent.Controls.Add(txtCarNum);
            pZoomContent.Controls.Add(btnEu);
            pZoomContent.Controls.Add(btnDot);
            pZoomContent.Controls.Add(btnI);
            pZoomContent.Controls.Add(btnDelete);
            pZoomContent.Controls.Add(btnMode);
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
            pZoomContent.Controls.Add(btnPre);
            pZoomContent.Controls.Add(btnHome);
            pZoomContent.Controls.Add(lblTitel);
            pZoomContent.Controls.Add(btnOk);
            pZoomContent.Dock = DockStyle.Fill;
            pZoomContent.Location = new Point(0, 0);
            pZoomContent.Margin = new Padding(0);
            pZoomContent.Name = "pZoomContent";
            pZoomContent.Size = new Size(1060, 795);
            pZoomContent.TabIndex = 1;
            // 
            // panInfo
            // 
            panInfo.Location = new Point(49, 222);
            panInfo.Name = "panInfo";
            panInfo.Size = new Size(505, 308);
            panInfo.TabIndex = 40;
            // 
            // btnPayment
            // 
            btnPayment.BackColor = Color.FromArgb(239, 236, 223);
            btnPayment.FlatAppearance.BorderColor = Color.FromArgb(51, 51, 51);
            btnPayment.FlatAppearance.BorderSize = 3;
            btnPayment.FlatStyle = FlatStyle.Flat;
            btnPayment.Font = new Font("맑은 고딕", 36F, FontStyle.Bold);
            btnPayment.ForeColor = Color.FromArgb(17, 17, 17);
            btnPayment.Location = new Point(49, 554);
            btnPayment.Name = "btnPayment";
            btnPayment.Size = new Size(245, 80);
            btnPayment.TabIndex = 39;
            btnPayment.Text = "연장결제";
            btnPayment.UseVisualStyleBackColor = false;
            btnPayment.Click += btnPayment_Click;
            // 
            // btnSearch
            // 
            btnSearch.BackColor = Color.FromArgb(239, 236, 223);
            btnSearch.FlatAppearance.BorderColor = Color.FromArgb(51, 51, 51);
            btnSearch.FlatAppearance.BorderSize = 3;
            btnSearch.FlatStyle = FlatStyle.Flat;
            btnSearch.Font = new Font("맑은 고딕", 36F, FontStyle.Bold);
            btnSearch.ForeColor = Color.FromArgb(17, 17, 17);
            btnSearch.Location = new Point(309, 554);
            btnSearch.Name = "btnSearch";
            btnSearch.Size = new Size(245, 80);
            btnSearch.TabIndex = 38;
            btnSearch.Text = "차량조회";
            btnSearch.UseVisualStyleBackColor = false;
            btnSearch.Click += btnSearch_Click;
            // 
            // txtCarNum
            // 
            txtCarNum.BackColor = Color.Black;
            txtCarNum.Font = new Font("Consolas", 50F, FontStyle.Bold);
            txtCarNum.ForeColor = Color.White;
            txtCarNum.Location = new Point(230, 127);
            txtCarNum.Name = "txtCarNum";
            txtCarNum.Size = new Size(600, 86);
            txtCarNum.TabIndex = 36;
            txtCarNum.TextAlign = HorizontalAlignment.Center;
            // 
            // btnEu
            // 
            btnEu.BackColor = Color.FromArgb(60, 70, 84);
            btnEu.FlatAppearance.BorderColor = Color.FromArgb(51, 51, 51);
            btnEu.FlatAppearance.BorderSize = 3;
            btnEu.Font = new Font("Consolas", 52F, FontStyle.Bold);
            btnEu.ForeColor = Color.White;
            btnEu.Location = new Point(905, 430);
            btnEu.Margin = new Padding(0);
            btnEu.Name = "btnEu";
            btnEu.Size = new Size(110, 100);
            btnEu.TabIndex = 35;
            btnEu.Tag = "ㅡ";
            btnEu.Text = "ㅡ";
            btnEu.UseVisualStyleBackColor = false;
            btnEu.Click += KeyButton_Click;
            // 
            // btnDot
            // 
            btnDot.BackColor = Color.FromArgb(60, 70, 84);
            btnDot.FlatAppearance.BorderColor = Color.FromArgb(51, 51, 51);
            btnDot.FlatAppearance.BorderSize = 3;
            btnDot.Font = new Font("Consolas", 52F, FontStyle.Bold);
            btnDot.ForeColor = Color.White;
            btnDot.Location = new Point(905, 326);
            btnDot.Margin = new Padding(0);
            btnDot.Name = "btnDot";
            btnDot.Size = new Size(110, 100);
            btnDot.TabIndex = 34;
            btnDot.Tag = "ㅣ";
            btnDot.Text = "ㅣ";
            btnDot.UseVisualStyleBackColor = false;
            btnDot.Click += KeyButton_Click;
            // 
            // btnI
            // 
            btnI.BackColor = Color.FromArgb(60, 70, 84);
            btnI.FlatAppearance.BorderColor = Color.FromArgb(51, 51, 51);
            btnI.FlatAppearance.BorderSize = 3;
            btnI.Font = new Font("Consolas", 52F, FontStyle.Bold);
            btnI.ForeColor = Color.White;
            btnI.Location = new Point(905, 222);
            btnI.Margin = new Padding(0);
            btnI.Name = "btnI";
            btnI.Size = new Size(110, 100);
            btnI.TabIndex = 33;
            btnI.Tag = "ㆍ";
            btnI.Text = "ㆍ";
            btnI.UseVisualStyleBackColor = false;
            btnI.Click += KeyButton_Click;
            // 
            // btnDelete
            // 
            btnDelete.BackColor = Color.FromArgb(60, 70, 84);
            btnDelete.FlatAppearance.BorderColor = Color.FromArgb(51, 51, 51);
            btnDelete.FlatAppearance.BorderSize = 3;
            btnDelete.Font = new Font("맑은 고딕", 24.75F, FontStyle.Bold);
            btnDelete.ForeColor = Color.White;
            btnDelete.Location = new Point(791, 534);
            btnDelete.Margin = new Padding(0);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(110, 100);
            btnDelete.TabIndex = 31;
            btnDelete.Tag = "DEL";
            btnDelete.Text = "삭제 ( # )";
            btnDelete.UseVisualStyleBackColor = false;
            btnDelete.Click += KeyButton_Click;
            // 
            // btnMode
            // 
            btnMode.BackColor = Color.FromArgb(60, 70, 84);
            btnMode.FlatAppearance.BorderColor = Color.FromArgb(51, 51, 51);
            btnMode.FlatAppearance.BorderSize = 3;
            btnMode.Font = new Font("맑은 고딕", 24.75F, FontStyle.Bold, GraphicsUnit.Point, 129);
            btnMode.ForeColor = Color.White;
            btnMode.Location = new Point(563, 534);
            btnMode.Margin = new Padding(0);
            btnMode.Name = "btnMode";
            btnMode.Size = new Size(110, 100);
            btnMode.TabIndex = 30;
            btnMode.Tag = "MODE";
            btnMode.Text = "한글 ( * )";
            btnMode.UseVisualStyleBackColor = false;
            btnMode.Click += btnMode_Click;
            // 
            // btnNum0
            // 
            btnNum0.BackColor = Color.FromArgb(60, 70, 84);
            btnNum0.FlatAppearance.BorderColor = Color.FromArgb(51, 51, 51);
            btnNum0.FlatAppearance.BorderSize = 3;
            btnNum0.Font = new Font("Consolas", 52F, FontStyle.Bold);
            btnNum0.ForeColor = Color.White;
            btnNum0.Location = new Point(677, 534);
            btnNum0.Margin = new Padding(0);
            btnNum0.Name = "btnNum0";
            btnNum0.Size = new Size(110, 100);
            btnNum0.TabIndex = 29;
            btnNum0.Tag = "0";
            btnNum0.Text = "0";
            btnNum0.UseVisualStyleBackColor = false;
            btnNum0.Click += KeyButton_Click;
            // 
            // btnNum9
            // 
            btnNum9.BackColor = Color.FromArgb(60, 70, 84);
            btnNum9.FlatAppearance.BorderColor = Color.FromArgb(51, 51, 51);
            btnNum9.FlatAppearance.BorderSize = 3;
            btnNum9.Font = new Font("Consolas", 52F, FontStyle.Bold);
            btnNum9.ForeColor = Color.White;
            btnNum9.Location = new Point(791, 430);
            btnNum9.Margin = new Padding(0);
            btnNum9.Name = "btnNum9";
            btnNum9.Size = new Size(110, 100);
            btnNum9.TabIndex = 28;
            btnNum9.Tag = "9";
            btnNum9.Text = "9";
            btnNum9.UseVisualStyleBackColor = false;
            btnNum9.Click += KeyButton_Click;
            // 
            // btnNum8
            // 
            btnNum8.BackColor = Color.FromArgb(60, 70, 84);
            btnNum8.FlatAppearance.BorderColor = Color.FromArgb(51, 51, 51);
            btnNum8.FlatAppearance.BorderSize = 3;
            btnNum8.Font = new Font("Consolas", 52F, FontStyle.Bold);
            btnNum8.ForeColor = Color.White;
            btnNum8.Location = new Point(677, 430);
            btnNum8.Margin = new Padding(0);
            btnNum8.Name = "btnNum8";
            btnNum8.Size = new Size(110, 100);
            btnNum8.TabIndex = 27;
            btnNum8.Tag = "8";
            btnNum8.Text = "8";
            btnNum8.UseVisualStyleBackColor = false;
            btnNum8.Click += KeyButton_Click;
            // 
            // btnNum7
            // 
            btnNum7.BackColor = Color.FromArgb(60, 70, 84);
            btnNum7.FlatAppearance.BorderColor = Color.FromArgb(51, 51, 51);
            btnNum7.FlatAppearance.BorderSize = 3;
            btnNum7.Font = new Font("Consolas", 52F, FontStyle.Bold);
            btnNum7.ForeColor = Color.White;
            btnNum7.Location = new Point(563, 430);
            btnNum7.Margin = new Padding(0);
            btnNum7.Name = "btnNum7";
            btnNum7.Size = new Size(110, 100);
            btnNum7.TabIndex = 26;
            btnNum7.Tag = "7";
            btnNum7.Text = "7";
            btnNum7.UseVisualStyleBackColor = false;
            btnNum7.Click += KeyButton_Click;
            // 
            // btnNum6
            // 
            btnNum6.BackColor = Color.FromArgb(60, 70, 84);
            btnNum6.FlatAppearance.BorderColor = Color.FromArgb(51, 51, 51);
            btnNum6.FlatAppearance.BorderSize = 3;
            btnNum6.Font = new Font("Consolas", 52F, FontStyle.Bold);
            btnNum6.ForeColor = Color.White;
            btnNum6.Location = new Point(791, 326);
            btnNum6.Margin = new Padding(0);
            btnNum6.Name = "btnNum6";
            btnNum6.Size = new Size(110, 100);
            btnNum6.TabIndex = 25;
            btnNum6.Tag = "6";
            btnNum6.Text = "6";
            btnNum6.UseVisualStyleBackColor = false;
            btnNum6.Click += KeyButton_Click;
            // 
            // btnNum5
            // 
            btnNum5.BackColor = Color.FromArgb(60, 70, 84);
            btnNum5.FlatAppearance.BorderColor = Color.FromArgb(51, 51, 51);
            btnNum5.FlatAppearance.BorderSize = 3;
            btnNum5.Font = new Font("Consolas", 52F, FontStyle.Bold);
            btnNum5.ForeColor = Color.White;
            btnNum5.Location = new Point(677, 326);
            btnNum5.Margin = new Padding(0);
            btnNum5.Name = "btnNum5";
            btnNum5.Size = new Size(110, 100);
            btnNum5.TabIndex = 24;
            btnNum5.Tag = "5";
            btnNum5.Text = "5";
            btnNum5.UseVisualStyleBackColor = false;
            btnNum5.Click += KeyButton_Click;
            // 
            // btnNum4
            // 
            btnNum4.BackColor = Color.FromArgb(60, 70, 84);
            btnNum4.FlatAppearance.BorderColor = Color.FromArgb(51, 51, 51);
            btnNum4.FlatAppearance.BorderSize = 3;
            btnNum4.Font = new Font("Consolas", 52F, FontStyle.Bold);
            btnNum4.ForeColor = Color.White;
            btnNum4.Location = new Point(563, 326);
            btnNum4.Margin = new Padding(0);
            btnNum4.Name = "btnNum4";
            btnNum4.Size = new Size(110, 100);
            btnNum4.TabIndex = 23;
            btnNum4.Tag = "4";
            btnNum4.Text = "4";
            btnNum4.UseVisualStyleBackColor = false;
            btnNum4.Click += KeyButton_Click;
            // 
            // btnNum3
            // 
            btnNum3.BackColor = Color.FromArgb(60, 70, 84);
            btnNum3.FlatAppearance.BorderColor = Color.FromArgb(51, 51, 51);
            btnNum3.FlatAppearance.BorderSize = 3;
            btnNum3.Font = new Font("Consolas", 52F, FontStyle.Bold);
            btnNum3.ForeColor = Color.White;
            btnNum3.Location = new Point(791, 222);
            btnNum3.Margin = new Padding(0);
            btnNum3.Name = "btnNum3";
            btnNum3.Size = new Size(110, 100);
            btnNum3.TabIndex = 22;
            btnNum3.Tag = "3";
            btnNum3.Text = "3";
            btnNum3.UseVisualStyleBackColor = false;
            btnNum3.Click += KeyButton_Click;
            // 
            // btnNum2
            // 
            btnNum2.BackColor = Color.FromArgb(60, 70, 84);
            btnNum2.FlatAppearance.BorderColor = Color.FromArgb(51, 51, 51);
            btnNum2.FlatAppearance.BorderSize = 3;
            btnNum2.Font = new Font("Consolas", 52F, FontStyle.Bold);
            btnNum2.ForeColor = Color.White;
            btnNum2.Location = new Point(677, 222);
            btnNum2.Margin = new Padding(0);
            btnNum2.Name = "btnNum2";
            btnNum2.Size = new Size(110, 100);
            btnNum2.TabIndex = 21;
            btnNum2.Tag = "2";
            btnNum2.Text = "2";
            btnNum2.UseVisualStyleBackColor = false;
            btnNum2.Click += KeyButton_Click;
            // 
            // btnNum1
            // 
            btnNum1.BackColor = Color.FromArgb(60, 70, 84);
            btnNum1.FlatAppearance.BorderColor = Color.FromArgb(51, 51, 51);
            btnNum1.FlatAppearance.BorderSize = 3;
            btnNum1.Font = new Font("Consolas", 52F, FontStyle.Bold);
            btnNum1.ForeColor = Color.White;
            btnNum1.Location = new Point(563, 222);
            btnNum1.Margin = new Padding(0);
            btnNum1.Name = "btnNum1";
            btnNum1.Size = new Size(110, 100);
            btnNum1.TabIndex = 20;
            btnNum1.Tag = "1";
            btnNum1.Text = "1";
            btnNum1.UseVisualStyleBackColor = false;
            btnNum1.Click += KeyButton_Click;
            // 
            // PeriodRenew
            // 
            AutoScaleDimensions = new SizeF(15F, 37F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1060, 795);
            Controls.Add(pZoomContent);
            DoubleBuffered = true;
            Font = new Font("맑은 고딕", 20.25F);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(6, 7, 6, 7);
            Name = "PeriodRenew";
            Text = "PeriodRenew";
            Load += PeriodRenew_Load;
            Shown += PeriodRenew_Shown;
            pZoomContent.ResumeLayout(false);
            pZoomContent.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Button btnOk;
        private Label lblTitel;
        private Button btnHome;
        private Button btnPre;
        private Panel pZoomContent;
        private Button btnEu;
        private Button btnDot;
        private Button btnI;
        private Button btnDelete;
        private Button btnMode;
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
        private TextBox txtCarNum;
        private Button btnPayment;
        private Button btnSearch;
        private Panel panInfo;
    }
}