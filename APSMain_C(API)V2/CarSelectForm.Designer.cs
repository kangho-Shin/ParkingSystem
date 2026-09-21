namespace APSMain
{
    partial class CarSelectForm
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
            label16 = new Label();
            pZoomContent = new Panel();
            dgvCarList = new DataGridView();
            btnPre = new Button();
            btnHome = new Button();
            btnOk = new Button();
            pZoomContent.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvCarList).BeginInit();
            SuspendLayout();
            // 
            // label16
            // 
            label16.BackColor = Color.Transparent;
            label16.Font = new Font("맑은 고딕", 36F, FontStyle.Bold, GraphicsUnit.Point, 129);
            label16.ForeColor = SystemColors.Control;
            label16.Location = new Point(80, 30);
            label16.Name = "label16";
            label16.Size = new Size(900, 69);
            label16.TabIndex = 5;
            label16.Text = "정산할 차량을 선택하세요";
            label16.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // pZoomContent
            // 
            pZoomContent.BackColor = SystemColors.ActiveBorder;
            pZoomContent.BackgroundImage = Properties.Resources.background;
            pZoomContent.BackgroundImageLayout = ImageLayout.Stretch;
            pZoomContent.Controls.Add(dgvCarList);
            pZoomContent.Controls.Add(btnPre);
            pZoomContent.Controls.Add(btnHome);
            pZoomContent.Controls.Add(btnOk);
            pZoomContent.Controls.Add(label16);
            pZoomContent.Font = new Font("맑은 고딕", 36F, FontStyle.Bold);
            pZoomContent.Location = new Point(0, 0);
            pZoomContent.Margin = new Padding(0);
            pZoomContent.Name = "pZoomContent";
            pZoomContent.Size = new Size(1060, 795);
            pZoomContent.TabIndex = 0;
            // 
            // dgvCarList
            // 
            dgvCarList.BackgroundColor = Color.White;
            dgvCarList.BorderStyle = BorderStyle.None;
            dgvCarList.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvCarList.EnableHeadersVisualStyles = false;
            dgvCarList.GridColor = Color.White;
            dgvCarList.Location = new Point(53, 153);
            dgvCarList.Name = "dgvCarList";
            dgvCarList.ReadOnly = true;
            dgvCarList.ShowCellToolTips = false;
            dgvCarList.Size = new Size(954, 462);
            dgvCarList.TabIndex = 6;
            dgvCarList.TabStop = false;
            dgvCarList.RowPostPaint += dgvCarList_RowPostPaint;
            dgvCarList.SelectionChanged += dgvCarList_SelectionChanged;
            dgvCarList.Paint += dgvCarList_Paint;
            dgvCarList.SystemColorsChanged += dgvCarList_SystemColorsChanged;
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
            btnOk.Text = "  확 인";
            btnOk.UseVisualStyleBackColor = false;
            btnOk.Click += btnOk_Click;
            btnOk.MouseDown += btn_MouseDown;
            btnOk.MouseUp += btn_MouseUp;
            // 
            // CarSelectForm
            // 
            AutoScaleDimensions = new SizeF(19F, 45F);
            AutoScaleMode = AutoScaleMode.Font;
            BackgroundImage = Properties.Resources.background;
            ClientSize = new Size(1060, 795);
            Controls.Add(pZoomContent);
            DoubleBuffered = true;
            Font = new Font("맑은 고딕", 24F, FontStyle.Bold, GraphicsUnit.Point, 129);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(8, 9, 8, 9);
            Name = "CarSelectForm";
            StartPosition = FormStartPosition.Manual;
            Text = "CarSelectForm";
            TopMost = true;
            FormClosing += CarSelectForm_FormClosing;
            Load += CarSelectForm_Load;
            Shown += CarSelectForm_Shown;
            pZoomContent.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvCarList).EndInit();
            ResumeLayout(false);
        }

        #endregion
        private Label label16;
        private Panel pZoomContent;
        private Button btnOk;
        private Button btnPre;
        private Button btnHome;
        private DataGridView dgvCarList;
    }
}