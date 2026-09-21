namespace APSMain
{
    partial class MenuForm15
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
            btnMain3 = new APSMain.BaseClass.CTButton();
            lblMainTitle = new Label();
            btnMain1 = new APSMain.BaseClass.CTButton();
            btnMain2 = new APSMain.BaseClass.CTButton();
            pZoomContent = new Panel();
            btnExplain = new APSMain.BaseClass.RectButton();
            btnZoom = new APSMain.BaseClass.RectButton();
            btnContrast = new APSMain.BaseClass.RectButton();
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
            btnMain3.Location = new Point(800, 239);
            btnMain3.Name = "btnMain3";
            btnMain3.PressedBack = Color.FromArgb(255, 255, 192);
            btnMain3.Size = new Size(253, 366);
            btnMain3.TabIndex = 2;
            btnMain3.Tag = "MemberExtend.mp3";
            btnMain3.Text = "정기권\\n연장";
            btnMain3.Theme = BaseClass.ButtonRole.ParkingFee;
            btnMain3.UseVisualStyleBackColor = false;
            btnMain3.Visible = false;
            btnMain3.Click += btnMain3_Click;
            // 
            // lblMainTitle
            // 
            lblMainTitle.BackColor = Color.Transparent;
            lblMainTitle.Font = new Font("맑은 고딕", 42F, FontStyle.Bold, GraphicsUnit.Point, 129);
            lblMainTitle.ForeColor = Color.White;
            lblMainTitle.Location = new Point(266, 24);
            lblMainTitle.Name = "lblMainTitle";
            lblMainTitle.Size = new Size(620, 69);
            lblMainTitle.TabIndex = 35;
            lblMainTitle.Text = "선택메뉴";
            lblMainTitle.TextAlign = ContentAlignment.MiddleCenter;
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
            btnMain1.Location = new Point(100, 239);
            btnMain1.Name = "btnMain1";
            btnMain1.PressedBack = Color.FromArgb(255, 255, 192);
            btnMain1.Size = new Size(253, 366);
            btnMain1.TabIndex = 0;
            btnMain1.Tag = "btnReceipt.mp3";
            btnMain1.Text = "영수증";
            btnMain1.Theme = BaseClass.ButtonRole.ParkingFee;
            btnMain1.UseVisualStyleBackColor = false;
            btnMain1.Visible = false;
            btnMain1.Click += btnMain1_Click;
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
            btnMain2.Location = new Point(450, 239);
            btnMain2.Name = "btnMain2";
            btnMain2.PressedBack = Color.FromArgb(255, 255, 192);
            btnMain2.Size = new Size(253, 366);
            btnMain2.TabIndex = 1;
            btnMain2.Tag = "Parkfee.mp3";
            btnMain2.Text = "주차요금";
            btnMain2.Theme = BaseClass.ButtonRole.ParkingFee;
            btnMain2.UseVisualStyleBackColor = false;
            btnMain2.Click += btnMain2_Click;
            // 
            // pZoomContent
            // 
            pZoomContent.BackColor = Color.Transparent;
            pZoomContent.BackgroundImage = Properties.Resources.background;
            pZoomContent.BackgroundImageLayout = ImageLayout.Stretch;
            pZoomContent.Controls.Add(btnExplain);
            pZoomContent.Controls.Add(btnZoom);
            pZoomContent.Controls.Add(btnContrast);
            pZoomContent.Controls.Add(btnMain3);
            pZoomContent.Controls.Add(lblMainTitle);
            pZoomContent.Controls.Add(btnMain1);
            pZoomContent.Controls.Add(btnMain2);
            pZoomContent.Dock = DockStyle.Fill;
            pZoomContent.Location = new Point(0, 0);
            pZoomContent.Margin = new Padding(0);
            pZoomContent.Name = "pZoomContent";
            pZoomContent.Size = new Size(1152, 864);
            pZoomContent.TabIndex = 2;
            pZoomContent.MouseUp += pZoomContent_MouseUp;
            // 
            // btnExplain
            // 
            btnExplain.BackColor = Color.Navy;
            btnExplain.BorderColor = Color.White;
            btnExplain.BorderThickness = 8;
            btnExplain.FlatAppearance.BorderSize = 0;
            btnExplain.FlatStyle = FlatStyle.Flat;
            btnExplain.Font = new Font("맑은 고딕", 38F, FontStyle.Bold);
            btnExplain.ForeColor = SystemColors.Control;
            btnExplain.IconImage = Properties.Resources.volume2;
            btnExplain.IconSize = 48;
            btnExplain.isHC = false;
            btnExplain.Location = new Point(800, 721);
            btnExplain.Name = "btnExplain";
            btnExplain.Size = new Size(253, 139);
            btnExplain.TabIndex = 38;
            btnExplain.Tag = "Volume2.mp3";
            btnExplain.Text = "볼륨 2단";
            btnExplain.UseVisualStyleBackColor = false;
            btnExplain.Click += btnExplain_Click;
            // 
            // btnZoom
            // 
            btnZoom.BackColor = Color.Navy;
            btnZoom.BorderColor = Color.White;
            btnZoom.BorderThickness = 8;
            btnZoom.FlatAppearance.BorderSize = 0;
            btnZoom.FlatStyle = FlatStyle.Flat;
            btnZoom.Font = new Font("맑은 고딕", 38F, FontStyle.Bold);
            btnZoom.ForeColor = SystemColors.Control;
            btnZoom.IconImage = Properties.Resources.icon31;
            btnZoom.IconSize = 48;
            btnZoom.isHC = false;
            btnZoom.Location = new Point(450, 721);
            btnZoom.Name = "btnZoom";
            btnZoom.Size = new Size(253, 139);
            btnZoom.TabIndex = 37;
            btnZoom.Tag = "zoomin.mp3";
            btnZoom.Text = "확대";
            btnZoom.UseVisualStyleBackColor = false;
            btnZoom.Click += btnZoom_Click;
            // 
            // btnContrast
            // 
            btnContrast.BackColor = Color.Navy;
            btnContrast.BorderColor = Color.White;
            btnContrast.BorderThickness = 8;
            btnContrast.FlatAppearance.BorderSize = 0;
            btnContrast.FlatStyle = FlatStyle.Flat;
            btnContrast.Font = new Font("맑은 고딕", 38F, FontStyle.Bold);
            btnContrast.ForeColor = SystemColors.Control;
            btnContrast.IconImage = Properties.Resources.icon21;
            btnContrast.IconSize = 48;
            btnContrast.isHC = false;
            btnContrast.Location = new Point(100, 721);
            btnContrast.Name = "btnContrast";
            btnContrast.Size = new Size(253, 139);
            btnContrast.TabIndex = 36;
            btnContrast.Tag = "highcontrast.mp3";
            btnContrast.Text = "고대비";
            btnContrast.UseVisualStyleBackColor = false;
            btnContrast.Click += btnContrast_Click;
            // 
            // MenuForm15
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1152, 864);
            Controls.Add(pZoomContent);
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            Name = "MenuForm15";
            Text = "MenuForm15";
            Load += MenuForm15_Load;
            Shown += MenuForm15_Shown;
            pZoomContent.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private BaseClass.CTButton btnMain3;
        private Label lblMainTitle;
        private BaseClass.CTButton btnMain1;
        private BaseClass.CTButton btnMain2;
        private Panel pZoomContent;
        private BaseClass.RectButton btnExplain;
        private BaseClass.RectButton btnZoom;
        private BaseClass.RectButton btnContrast;
    }
}