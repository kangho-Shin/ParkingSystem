namespace APSMain
{
    partial class RemoteCall
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
            btnPre = new Button();
            btnHome = new Button();
            btnPrint = new Button();
            pZoomContent.SuspendLayout();
            SuspendLayout();
            // 
            // pZoomContent
            // 
            pZoomContent.BackgroundImage = Properties.Resources.background;
            pZoomContent.BackgroundImageLayout = ImageLayout.Stretch;
            pZoomContent.Controls.Add(lbCloseTime);
            pZoomContent.Controls.Add(lblTitle);
            pZoomContent.Controls.Add(btnPre);
            pZoomContent.Controls.Add(btnHome);
            pZoomContent.Controls.Add(btnPrint);
            pZoomContent.Location = new Point(0, 1);
            pZoomContent.Margin = new Padding(0);
            pZoomContent.Name = "pZoomContent";
            pZoomContent.Size = new Size(1061, 793);
            pZoomContent.TabIndex = 1;
            // 
            // lbCloseTime
            // 
            lbCloseTime.BackColor = SystemColors.ActiveCaptionText;
            lbCloseTime.Font = new Font("렉시믹스", 32F, FontStyle.Bold);
            lbCloseTime.ForeColor = SystemColors.Control;
            lbCloseTime.Location = new Point(907, 56);
            lbCloseTime.Margin = new Padding(4, 0, 4, 0);
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
            lblTitle.Margin = new Padding(4, 0, 4, 0);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(899, 69);
            lblTitle.TabIndex = 21;
            lblTitle.Text = "원격할인 항목을 선택하세요";
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
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
            btnPre.Location = new Point(58, 654);
            btnPre.Margin = new Padding(4);
            btnPre.Name = "btnPre";
            btnPre.Padding = new Padding(8, 0, 0, 0);
            btnPre.Size = new Size(240, 100);
            btnPre.TabIndex = 1;
            btnPre.Tag = "btnPre.mp3";
            btnPre.Text = "  이전";
            btnPre.UseVisualStyleBackColor = false;
            btnPre.Visible = false;
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
            btnHome.Location = new Point(344, 654);
            btnHome.Margin = new Padding(4);
            btnHome.Name = "btnHome";
            btnHome.Padding = new Padding(8, 0, 0, 0);
            btnHome.Size = new Size(240, 100);
            btnHome.TabIndex = 0;
            btnHome.Tag = "btnHome.mp3";
            btnHome.Text = "  홈";
            btnHome.UseVisualStyleBackColor = false;
            btnHome.Visible = false;
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
            btnPrint.Margin = new Padding(4);
            btnPrint.Name = "btnPrint";
            btnPrint.Padding = new Padding(8, 0, 0, 0);
            btnPrint.Size = new Size(240, 100);
            btnPrint.TabIndex = 2;
            btnPrint.Tag = "btnPrint.mp3";
            btnPrint.Text = "  요청";
            btnPrint.UseVisualStyleBackColor = false;
            // 
            // RemoteCall
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1060, 795);
            Controls.Add(pZoomContent);
            Font = new Font("맑은 고딕", 9F);
            FormBorderStyle = FormBorderStyle.None;
            Name = "RemoteCall";
            Text = "RemoteCall";
            pZoomContent.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel pZoomContent;
        private Label lbCloseTime;
        private Label lblTitle;
        private Button btnPre;
        private Button btnHome;
        private Button btnPrint;
    }
}