
using APSMain.BaseClass;

namespace APSMain
{
    partial class MenuForm
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
            btnMain3 = new CTButton();
            btnMain2 = new CTButton();
            btnMain1 = new CTButton();
            lblMainTitle = new Label();
            pZoomContent = new Panel();
            pZoomContent.SuspendLayout();
            SuspendLayout();
            // 
            // btnMain3
            // 
            btnMain3.ActiveBack = Color.FromArgb(55, 65, 81);
            btnMain3.BackColor = Color.FromArgb(255, 255, 192);
            btnMain3.BorderColor = Color.White;
            btnMain3.BorderThickness = 8;
            btnMain3.FlatStyle = FlatStyle.Flat;
            btnMain3.Font = new Font("맑은 고딕", 40F, FontStyle.Bold);
            btnMain3.ForeColor = Color.BlanchedAlmond;
            btnMain3.HighContrast = false;
            btnMain3.HoverBack = Color.FromArgb(14, 165, 233);
            btnMain3.IconImage = Properties.Resources.calendaricon;
            btnMain3.IconSize = 128;
            btnMain3.Location = new Point(706, 282);
            btnMain3.Name = "btnMain3";
            btnMain3.PressedBack = Color.FromArgb(255, 255, 192);
            btnMain3.Size = new Size(253, 366);
            btnMain3.TabIndex = 2;
            btnMain3.Tag = "MemberExtend.mp3";
            btnMain3.Text = "정기권\\n연장";
            btnMain3.Theme = ButtonRole.ParkingFee;
            btnMain3.UseVisualStyleBackColor = false;
            btnMain3.Visible = false;
            btnMain3.Click += btnMain3_Click;
            // 
            // btnMain2
            // 
            btnMain2.ActiveBack = Color.FromArgb(55, 65, 81);
            btnMain2.BackColor = Color.FromArgb(255, 255, 192);
            btnMain2.BorderColor = Color.White;
            btnMain2.BorderThickness = 8;
            btnMain2.FlatStyle = FlatStyle.Flat;
            btnMain2.Font = new Font("맑은 고딕", 40F, FontStyle.Bold);
            btnMain2.ForeColor = Color.BlanchedAlmond;
            btnMain2.HighContrast = false;
            btnMain2.HoverBack = Color.FromArgb(14, 165, 233);
            btnMain2.IconImage = Properties.Resources.caricon;
            btnMain2.IconSize = 128;
            btnMain2.Location = new Point(403, 282);
            btnMain2.Name = "btnMain2";
            btnMain2.PressedBack = Color.FromArgb(255, 255, 192);
            btnMain2.Size = new Size(253, 366);
            btnMain2.TabIndex = 1;
            btnMain2.Tag = "Parkfee.mp3";
            btnMain2.Text = "주차요금";
            btnMain2.Theme = ButtonRole.ParkingFee;
            btnMain2.UseVisualStyleBackColor = false;
            btnMain2.Click += btnMain2_Click;
            // 
            // btnMain1
            // 
            btnMain1.ActiveBack = Color.FromArgb(55, 65, 81);
            btnMain1.BackColor = Color.FromArgb(255, 255, 192);
            btnMain1.BorderColor = Color.White;
            btnMain1.BorderThickness = 8;
            btnMain1.FlatStyle = FlatStyle.Flat;
            btnMain1.Font = new Font("맑은 고딕", 40F, FontStyle.Bold);
            btnMain1.ForeColor = Color.BlanchedAlmond;
            btnMain1.HighContrast = false;
            btnMain1.HoverBack = Color.FromArgb(88, 217, 236);
            btnMain1.IconImage = Properties.Resources.receipticon;
            btnMain1.IconSize = 128;
            btnMain1.Location = new Point(100, 282);
            btnMain1.Name = "btnMain1";
            btnMain1.PressedBack = Color.FromArgb(255, 255, 192);
            btnMain1.Size = new Size(253, 366);
            btnMain1.TabIndex = 0;
            btnMain1.Tag = "btnReceipt.mp3";
            btnMain1.Text = "영수증";
            btnMain1.Theme = ButtonRole.ParkingFee;
            btnMain1.UseVisualStyleBackColor = false;
            btnMain1.Visible = false;
            btnMain1.Click += btnMain1_Click;
            // 
            // lblMainTitle
            // 
            lblMainTitle.BackColor = Color.Transparent;
            lblMainTitle.Font = new Font("맑은 고딕", 42F, FontStyle.Bold, GraphicsUnit.Point, 129);
            lblMainTitle.ForeColor = Color.White;
            lblMainTitle.Location = new Point(221, 24);
            lblMainTitle.Name = "lblMainTitle";
            lblMainTitle.Size = new Size(620, 69);
            lblMainTitle.TabIndex = 35;
            lblMainTitle.Text = "선택메뉴";
            lblMainTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // pZoomContent
            // 
            pZoomContent.BackColor = Color.Transparent;
            pZoomContent.BackgroundImage = Properties.Resources.background;
            pZoomContent.BackgroundImageLayout = ImageLayout.Stretch;
            pZoomContent.Controls.Add(btnMain3);
            pZoomContent.Controls.Add(lblMainTitle);
            pZoomContent.Controls.Add(btnMain1);
            pZoomContent.Controls.Add(btnMain2);
            pZoomContent.Location = new Point(0, 0);
            pZoomContent.Margin = new Padding(0);
            pZoomContent.Name = "pZoomContent";
            pZoomContent.Size = new Size(1060, 795);
            pZoomContent.TabIndex = 1;
            pZoomContent.MouseDown += pZoomContent_MouseDown;
            // 
            // MenuForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            BackgroundImageLayout = ImageLayout.Stretch;
            ClientSize = new Size(1060, 795);
            Controls.Add(pZoomContent);
            DoubleBuffered = true;
            ForeColor = SystemColors.Control;
            FormBorderStyle = FormBorderStyle.None;
            KeyPreview = true;
            Name = "MenuForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "APSMain";
            TransparencyKey = Color.White;
            FormClosed += MenuForm_FormClosed;
            Load += MenuForm_Load;
            Shown += MenuForm_Shown;
            pZoomContent.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private CTButton btnMain2;
        private CTButton btnMain1;
        private Label lblMainTitle;
        private CTButton btnMain3;
        private Panel pZoomContent;
    }
}
