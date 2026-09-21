namespace APSMain
{
    partial class ReceiptForm
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
            pZoomContent = new Panel();
            lbCloseTime = new Label();
            lblTitle = new Label();
            label2 = new Label();
            label1 = new Label();
            picPrint = new PictureBox();
            btnPre = new Button();
            btnHome = new Button();
            btnPrint = new Button();
            pZoomContent.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picPrint).BeginInit();
            SuspendLayout();
            // 
            // pZoomContent
            // 
            pZoomContent.BackgroundImage = Properties.Resources.background;
            pZoomContent.BackgroundImageLayout = ImageLayout.Stretch;
            pZoomContent.Controls.Add(lbCloseTime);
            pZoomContent.Controls.Add(lblTitle);
            pZoomContent.Controls.Add(label2);
            pZoomContent.Controls.Add(label1);
            pZoomContent.Controls.Add(picPrint);
            pZoomContent.Controls.Add(btnPre);
            pZoomContent.Controls.Add(btnHome);
            pZoomContent.Controls.Add(btnPrint);
            pZoomContent.Location = new Point(0, 0);
            pZoomContent.Margin = new Padding(0);
            pZoomContent.Name = "pZoomContent";
            pZoomContent.Size = new Size(1061, 793);
            pZoomContent.TabIndex = 0;
            // 
            // lbCloseTime
            // 
            lbCloseTime.BackColor = SystemColors.ActiveCaptionText;
            lbCloseTime.Font = new Font("렉시믹스", 32F, FontStyle.Bold);
            lbCloseTime.ForeColor = SystemColors.Control;
            lbCloseTime.Location = new Point(907, 56);
            lbCloseTime.Margin = new Padding(1, 0, 1, 0);
            lbCloseTime.Name = "lbCloseTime";
            lbCloseTime.Size = new Size(141, 61);
            lbCloseTime.TabIndex = 22;
            lbCloseTime.Text = "2:00";
            lbCloseTime.TextAlign = ContentAlignment.TopCenter;
            // 
            // lblTitle
            // 
            lblTitle.BackColor = Color.Transparent;
            lblTitle.Font = new Font("맑은 고딕", 36F, FontStyle.Bold, GraphicsUnit.Point, 129);
            lblTitle.ForeColor = SystemColors.Control;
            lblTitle.Location = new Point(79, 30);
            lblTitle.Margin = new Padding(1, 0, 1, 0);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(899, 69);
            lblTitle.TabIndex = 21;
            lblTitle.Text = "영수증 발행";
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.BackColor = Color.Transparent;
            label2.Font = new Font("맑은 고딕", 36F, FontStyle.Bold);
            label2.ForeColor = Color.Transparent;
            label2.Location = new Point(485, 429);
            label2.Margin = new Padding(1, 0, 1, 0);
            label2.Name = "label2";
            label2.Size = new Size(542, 65);
            label2.TabIndex = 20;
            label2.Text = "출력 버튼을 눌러주세요";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.BackColor = Color.Transparent;
            label1.Font = new Font("맑은 고딕", 36F, FontStyle.Bold);
            label1.ForeColor = Color.Transparent;
            label1.Location = new Point(505, 308);
            label1.Margin = new Padding(1, 0, 1, 0);
            label1.Name = "label1";
            label1.Size = new Size(494, 65);
            label1.TabIndex = 19;
            label1.Text = "영수증이 필요 하시면";
            // 
            // picPrint
            // 
            picPrint.BackColor = Color.Transparent;
            picPrint.BackgroundImage = Properties.Resources.Printer;
            picPrint.BackgroundImageLayout = ImageLayout.Stretch;
            picPrint.Location = new Point(99, 277);
            picPrint.Margin = new Padding(0);
            picPrint.Name = "picPrint";
            picPrint.Size = new Size(360, 273);
            picPrint.TabIndex = 18;
            picPrint.TabStop = false;
            // 
            // btnPre
            // 
            btnPre.BackColor = Color.FromArgb(239, 236, 223);
            btnPre.Enabled = false;
            btnPre.FlatAppearance.BorderColor = Color.FromArgb(51, 51, 51);
            btnPre.FlatAppearance.BorderSize = 3;
            btnPre.FlatStyle = FlatStyle.Flat;
            btnPre.Font = new Font("맑은 고딕", 44F, FontStyle.Bold);
            btnPre.ForeColor = Color.FromArgb(17, 17, 17);
            btnPre.Image = Properties.Resources.prev;
            btnPre.ImageAlign = ContentAlignment.MiddleLeft;
            btnPre.Location = new Point(344, 654);
            btnPre.Margin = new Padding(1, 1, 1, 1);
            btnPre.Name = "btnPre";
            btnPre.Padding = new Padding(2, 0, 0, 0);
            btnPre.Size = new Size(240, 100);
            btnPre.TabIndex = 1;
            btnPre.Tag = "btnPre.mp3";
            btnPre.Text = "  이전";
            btnPre.UseVisualStyleBackColor = false;
            btnPre.Visible = false;
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
            btnHome.Location = new Point(58, 654);
            btnHome.Margin = new Padding(1, 1, 1, 1);
            btnHome.Name = "btnHome";
            btnHome.Padding = new Padding(2, 0, 0, 0);
            btnHome.Size = new Size(240, 100);
            btnHome.TabIndex = 0;
            btnHome.Tag = "btnHome.mp3";
            btnHome.Text = "  홈";
            btnHome.UseVisualStyleBackColor = false;
            btnHome.Click += btnHome_Click;
            btnHome.MouseDown += btn_MouseDown;
            btnHome.MouseUp += btn_MouseUp;
            // 
            // btnPrint
            // 
            btnPrint.BackColor = Color.FromArgb(239, 236, 223);
            btnPrint.FlatAppearance.BorderColor = Color.FromArgb(51, 51, 51);
            btnPrint.FlatAppearance.BorderSize = 3;
            btnPrint.FlatStyle = FlatStyle.Flat;
            btnPrint.Font = new Font("맑은 고딕", 44F, FontStyle.Bold);
            btnPrint.ForeColor = Color.FromArgb(17, 17, 17);
            btnPrint.Image = Properties.Resources.Receipt;
            btnPrint.ImageAlign = ContentAlignment.MiddleLeft;
            btnPrint.Location = new Point(758, 654);
            btnPrint.Margin = new Padding(1, 1, 1, 1);
            btnPrint.Name = "btnPrint";
            btnPrint.Padding = new Padding(2, 0, 0, 0);
            btnPrint.Size = new Size(240, 100);
            btnPrint.TabIndex = 2;
            btnPrint.Tag = "btnPrint.mp3";
            btnPrint.Text = "  출력";
            btnPrint.UseVisualStyleBackColor = false;
            btnPrint.Click += btnOk_Click;
            btnPrint.MouseDown += btn_MouseDown;
            btnPrint.MouseUp += btn_MouseUp;
            // 
            // ReceiptForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackgroundImage = Properties.Resources.background;
            BackgroundImageLayout = ImageLayout.Stretch;
            ClientSize = new Size(1061, 793);
            Controls.Add(pZoomContent);
            DoubleBuffered = true;
            Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            FormBorderStyle = FormBorderStyle.None;
            Name = "ReceiptForm";
            Text = "ReceiptForm";
            FormClosing += ReceiptForm_FormClosing;
            Load += ReceiptForm_Load;
            Shown += ReceiptForm_Shown;
            pZoomContent.ResumeLayout(false);
            pZoomContent.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picPrint).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel pZoomContent;
        private Button btnPre;
        private Button btnHome;
        private Button btnPrint;
        private Label label2;
        private Label label1;
        private PictureBox picPrint;
        private Label lblTitle;
        private Label lbCloseTime;
    }
}