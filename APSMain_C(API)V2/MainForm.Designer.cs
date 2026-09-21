using APSMain.BaseClass;

namespace APSMain
{
    partial class MainForm
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            panelBottom = new Panel();
            btnExplain = new RectButton();
            cmsEar = new ContextMenuStrip(components);
            btnZoom = new RectButton();
            btnContrast = new RectButton();
            btnWheel = new RectButton();
            panelTop = new Panel();
            lblTitle = new Label();
            picLogo = new PictureBox();
            panelCenter = new Panel();
            panel1 = new Panel();
            btnLabel = new Button();
            lblScroll = new MarqueeLabel();
            lblStrTime = new Label();
            panelQuadNav = new Panel();
            btnRD = new Button();
            btnLD = new Button();
            btnRU = new Button();
            btnLU = new Button();
            panelFrame = new Panel();
            lblBlock = new Label();
            panelBottom.SuspendLayout();
            panelTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picLogo).BeginInit();
            panelCenter.SuspendLayout();
            panel1.SuspendLayout();
            panelQuadNav.SuspendLayout();
            panelFrame.SuspendLayout();
            SuspendLayout();
            // 
            // panelBottom
            // 
            panelBottom.BackColor = Color.FromArgb(64, 64, 64);
            panelBottom.Controls.Add(btnExplain);
            panelBottom.Controls.Add(btnZoom);
            panelBottom.Controls.Add(btnContrast);
            panelBottom.Controls.Add(btnWheel);
            panelBottom.Dock = DockStyle.Bottom;
            panelBottom.Font = new Font("맑은 고딕", 36F, FontStyle.Bold);
            panelBottom.Location = new Point(0, 713);
            panelBottom.Margin = new Padding(0);
            panelBottom.Name = "panelBottom";
            panelBottom.Padding = new Padding(0, 3, 0, 3);
            panelBottom.Size = new Size(1080, 167);
            panelBottom.TabIndex = 15;
            panelBottom.MouseDown += MainScreen_MouseDown;
            // 
            // btnExplain
            // 
            btnExplain.BackColor = Color.Navy;
            btnExplain.BorderColor = Color.White;
            btnExplain.BorderThickness = 8;
            btnExplain.ContextMenuStrip = cmsEar;
            btnExplain.FlatAppearance.BorderSize = 0;
            btnExplain.FlatStyle = FlatStyle.Flat;
            btnExplain.Font = new Font("맑은 고딕", 38F, FontStyle.Bold);
            btnExplain.ForeColor = SystemColors.Control;
            btnExplain.IconImage = Properties.Resources.volume2;
            btnExplain.IconSize = 48;
            btnExplain.isHC = false;
            btnExplain.Location = new Point(821, 8);
            btnExplain.Name = "btnExplain";
            btnExplain.Size = new Size(240, 150);
            btnExplain.TabIndex = 3;
            btnExplain.Tag = "Volume2.mp3";
            btnExplain.Text = "볼륨 2단";
            btnExplain.UseVisualStyleBackColor = false;
            btnExplain.Click += btnExplain_Click;
            btnExplain.MouseDown += btnExplain_Down;
            btnExplain.MouseUp += btnExplain_Up;
            // 
            // cmsEar
            // 
            cmsEar.Name = "cmsEar";
            cmsEar.Size = new Size(61, 4);
            cmsEar.Opening += cmsEar_Opening;
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
            btnZoom.Location = new Point(553, 9);
            btnZoom.Name = "btnZoom";
            btnZoom.Size = new Size(240, 150);
            btnZoom.TabIndex = 2;
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
            btnContrast.Location = new Point(285, 9);
            btnContrast.Name = "btnContrast";
            btnContrast.Size = new Size(240, 150);
            btnContrast.TabIndex = 1;
            btnContrast.Tag = "highcontrast.mp3";
            btnContrast.Text = "고대비";
            btnContrast.UseVisualStyleBackColor = false;
            btnContrast.Click += btnContrast_Click;
            // 
            // btnWheel
            // 
            btnWheel.BackColor = Color.Navy;
            btnWheel.BorderColor = Color.White;
            btnWheel.BorderThickness = 8;
            btnWheel.FlatAppearance.BorderColor = Color.White;
            btnWheel.FlatAppearance.BorderSize = 4;
            btnWheel.FlatStyle = FlatStyle.Flat;
            btnWheel.Font = new Font("맑은 고딕", 38F, FontStyle.Bold);
            btnWheel.ForeColor = SystemColors.Control;
            btnWheel.IconImage = Properties.Resources.icon11;
            btnWheel.IconSize = 48;
            btnWheel.isHC = false;
            btnWheel.Location = new Point(17, 9);
            btnWheel.Name = "btnWheel";
            btnWheel.Size = new Size(240, 150);
            btnWheel.TabIndex = 0;
            btnWheel.Tag = "lowscreen.mp3";
            btnWheel.Text = "낮은화면";
            btnWheel.UseVisualStyleBackColor = false;
            btnWheel.Click += btnWheel_Click;
            // 
            // panelTop
            // 
            panelTop.BackColor = Color.FromArgb(14, 47, 109);
            panelTop.Controls.Add(lblTitle);
            panelTop.Controls.Add(picLogo);
            panelTop.Dock = DockStyle.Top;
            panelTop.Font = new Font("맑은 고딕", 36F, FontStyle.Bold);
            panelTop.ForeColor = SystemColors.ButtonHighlight;
            panelTop.Location = new Point(0, 0);
            panelTop.Name = "panelTop";
            panelTop.Size = new Size(1080, 150);
            panelTop.TabIndex = 9;
            panelTop.MouseDoubleClick += panelTop_MouseDoubleClick;
            panelTop.MouseDown += MainScreen_MouseDown;
            panelTop.MouseUp += panelTop_MouseUp;
            // 
            // lblTitle
            // 
            lblTitle.BackColor = Color.FromArgb(14, 47, 109);
            lblTitle.Font = new Font("맑은 고딕", 72F, FontStyle.Bold, GraphicsUnit.Point, 129);
            lblTitle.Location = new Point(163, 12);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(751, 128);
            lblTitle.TabIndex = 8;
            lblTitle.Text = "대웅IPS";
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // picLogo
            // 
            picLogo.BackgroundImage = Properties.Resources.dwips;
            picLogo.BackgroundImageLayout = ImageLayout.Zoom;
            picLogo.Location = new Point(6, 12);
            picLogo.Name = "picLogo";
            picLogo.Size = new Size(154, 128);
            picLogo.TabIndex = 0;
            picLogo.TabStop = false;
            picLogo.MouseDown += picLogo_MouseDown;
            // 
            // panelCenter
            // 
            panelCenter.BackColor = SystemColors.ActiveCaptionText;
            panelCenter.BackgroundImageLayout = ImageLayout.Stretch;
            panelCenter.Controls.Add(panel1);
            panelCenter.Controls.Add(lblStrTime);
            panelCenter.Controls.Add(panelQuadNav);
            panelCenter.Controls.Add(panelFrame);
            panelCenter.Dock = DockStyle.Fill;
            panelCenter.Font = new Font("맑은 고딕", 36F, FontStyle.Bold);
            panelCenter.Location = new Point(0, 150);
            panelCenter.Name = "panelCenter";
            panelCenter.Size = new Size(1080, 563);
            panelCenter.TabIndex = 13;
            panelCenter.MouseDown += MainScreen_MouseDown;
            // 
            // panel1
            // 
            panel1.Controls.Add(btnLabel);
            panel1.Controls.Add(lblScroll);
            panel1.Dock = DockStyle.Bottom;
            panel1.Location = new Point(0, 497);
            panel1.Margin = new Padding(0);
            panel1.Name = "panel1";
            panel1.Size = new Size(1080, 66);
            panel1.TabIndex = 16;
            // 
            // btnLabel
            // 
            btnLabel.BackColor = Color.Transparent;
            btnLabel.Dock = DockStyle.Right;
            btnLabel.Font = new Font("맑은 고딕", 22F, FontStyle.Bold);
            btnLabel.ForeColor = Color.White;
            btnLabel.Image = Properties.Resources.labelpause;
            btnLabel.Location = new Point(1005, 0);
            btnLabel.Name = "btnLabel";
            btnLabel.Size = new Size(75, 66);
            btnLabel.TabIndex = 15;
            btnLabel.TabStop = false;
            btnLabel.UseVisualStyleBackColor = false;
            btnLabel.Click += btnLabel_Click;
            // 
            // lblScroll
            // 
            lblScroll.AutoStart = true;
            lblScroll.AutoWrapWhenOverflow = true;
            lblScroll.BackColor = Color.FromArgb(11, 58, 111);
            lblScroll.Dock = DockStyle.Left;
            lblScroll.DwellMilliseconds = 2500;
            lblScroll.Font = new Font("맑은 고딕", 38F, FontStyle.Bold);
            lblScroll.ForeColor = Color.FromArgb(255, 235, 59);
            lblScroll.Location = new Point(0, 0);
            lblScroll.Name = "lblScroll";
            lblScroll.Repeat = true;
            lblScroll.Size = new Size(1000, 66);
            lblScroll.TabIndex = 14;
            lblScroll.Text = "화면을 누르시면 도움말이 나옵니다.";
            lblScroll.UseCompatibleTextRendering = true;
            lblScroll.Click += lblScroll_Click;
            // 
            // lblStrTime
            // 
            lblStrTime.Font = new Font("렉시믹스", 40F, FontStyle.Bold);
            lblStrTime.ForeColor = SystemColors.Control;
            lblStrTime.Location = new Point(659, 3);
            lblStrTime.Name = "lblStrTime";
            lblStrTime.Size = new Size(402, 60);
            lblStrTime.TabIndex = 10;
            lblStrTime.Text = "08-09 12:12:00";
            lblStrTime.TextAlign = ContentAlignment.TopCenter;
            lblStrTime.UseCompatibleTextRendering = true;
            // 
            // panelQuadNav
            // 
            panelQuadNav.Controls.Add(btnRD);
            panelQuadNav.Controls.Add(btnLD);
            panelQuadNav.Controls.Add(btnRU);
            panelQuadNav.Controls.Add(btnLU);
            panelQuadNav.Location = new Point(0, 400);
            panelQuadNav.Margin = new Padding(0);
            panelQuadNav.Name = "panelQuadNav";
            panelQuadNav.Size = new Size(1080, 89);
            panelQuadNav.TabIndex = 12;
            // 
            // btnRD
            // 
            btnRD.BackColor = Color.FromArgb(230, 243, 255);
            btnRD.Font = new Font("맑은 고딕", 38F, FontStyle.Bold);
            btnRD.Image = Properties.Resources.anchor_br_74;
            btnRD.ImageAlign = ContentAlignment.MiddleLeft;
            btnRD.Location = new Point(831, 6);
            btnRD.Name = "btnRD";
            btnRD.Size = new Size(233, 84);
            btnRD.TabIndex = 7;
            btnRD.Tag = "screenrd.mp3";
            btnRD.Text = "    화면";
            btnRD.UseVisualStyleBackColor = false;
            btnRD.Click += btnRD_Click;
            // 
            // btnLD
            // 
            btnLD.BackColor = Color.FromArgb(230, 243, 255);
            btnLD.Font = new Font("맑은 고딕", 38F, FontStyle.Bold);
            btnLD.Image = Properties.Resources.anchor_bl_74;
            btnLD.ImageAlign = ContentAlignment.MiddleLeft;
            btnLD.Location = new Point(557, 6);
            btnLD.Name = "btnLD";
            btnLD.Size = new Size(233, 84);
            btnLD.TabIndex = 6;
            btnLD.Tag = "screenld.mp3";
            btnLD.Text = "    화면";
            btnLD.UseVisualStyleBackColor = false;
            btnLD.Click += btnLD_Click;
            // 
            // btnRU
            // 
            btnRU.BackColor = Color.FromArgb(230, 243, 255);
            btnRU.Font = new Font("맑은 고딕", 38F, FontStyle.Bold);
            btnRU.Image = Properties.Resources.anchor_tr_74;
            btnRU.ImageAlign = ContentAlignment.MiddleLeft;
            btnRU.Location = new Point(283, 6);
            btnRU.Name = "btnRU";
            btnRU.Size = new Size(233, 84);
            btnRU.TabIndex = 5;
            btnRU.Tag = "screenru.mp3";
            btnRU.Text = "    화면";
            btnRU.UseVisualStyleBackColor = false;
            btnRU.Click += btnRU_Click;
            // 
            // btnLU
            // 
            btnLU.BackColor = Color.FromArgb(230, 243, 255);
            btnLU.FlatAppearance.BorderColor = Color.White;
            btnLU.FlatAppearance.BorderSize = 3;
            btnLU.FlatAppearance.MouseDownBackColor = Color.White;
            btnLU.FlatAppearance.MouseOverBackColor = Color.White;
            btnLU.Font = new Font("맑은 고딕", 38F, FontStyle.Bold);
            btnLU.Image = Properties.Resources.anchor_tl_74;
            btnLU.ImageAlign = ContentAlignment.MiddleLeft;
            btnLU.Location = new Point(9, 6);
            btnLU.Name = "btnLU";
            btnLU.Size = new Size(233, 84);
            btnLU.TabIndex = 4;
            btnLU.Tag = "screenlu.mp3";
            btnLU.Text = "    화면";
            btnLU.UseVisualStyleBackColor = false;
            btnLU.Click += btnLU_Click;
            // 
            // panelFrame
            // 
            panelFrame.Anchor = AnchorStyles.None;
            panelFrame.Controls.Add(lblBlock);
            panelFrame.Location = new Point(12, 127);
            panelFrame.Name = "panelFrame";
            panelFrame.Size = new Size(1049, 245);
            panelFrame.TabIndex = 11;
            // 
            // lblBlock
            // 
            lblBlock.BackColor = Color.Black;
            lblBlock.Font = new Font("맑은 고딕", 36F, FontStyle.Regular, GraphicsUnit.Point, 129);
            lblBlock.ForeColor = SystemColors.Control;
            lblBlock.Location = new Point(74, 64);
            lblBlock.Name = "lblBlock";
            lblBlock.Size = new Size(914, 120);
            lblBlock.TabIndex = 41;
            lblBlock.Text = "";
            lblBlock.TextAlign = ContentAlignment.MiddleCenter;
            lblBlock.Visible = false;
            // 
            // MainForm
            // 
            AutoScaleMode = AutoScaleMode.None;
            AutoSize = true;
            ClientSize = new Size(1080, 880);
            Controls.Add(panelCenter);
            Controls.Add(panelTop);
            Controls.Add(panelBottom);
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)resources.GetObject("$this.Icon");
            KeyPreview = true;
            Name = "MainForm";
            StartPosition = FormStartPosition.Manual;
            Text = "MainForm";
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
            Shown += MainForm_Shown;
            MouseDoubleClick += MainForm_MouseDoubleClick;
            MouseUp += MainForm_MouseUp;
            panelBottom.ResumeLayout(false);
            panelTop.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)picLogo).EndInit();
            panelCenter.ResumeLayout(false);
            panel1.ResumeLayout(false);
            panelQuadNav.ResumeLayout(false);
            panelFrame.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel panelBottom;
        private Panel panelTop;
        private Panel panelCenter;
        private BaseClass.RectButton btnExplain;
        private BaseClass.RectButton btnContrast;
        private BaseClass.RectButton btnWheel;
        private BaseClass.MarqueeLabel lblScroll;
        private BaseClass.RectButton btnZoom;
        private Label lblTitle;
        private PictureBox picLogo;
        private Panel panelFrame;
        private Panel panelQuadNav;
        private Button btnRD;
        private Button btnLD;
        private Button btnRU;
        private Button btnLU;
        private Label lblStrTime;
        private ContextMenuStrip cmsEar;
        private Panel panel1;
        private Button btnLabel;
        private Label lblBlock;
    }
}
