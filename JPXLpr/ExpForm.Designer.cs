namespace JPXLpr
{
    partial class ExpForm
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
            label1 = new Label();
            cbCamera = new ComboBox();
            cbMonth = new ComboBox();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            txtDayMax = new TextBox();
            txtDayMin = new TextBox();
            txtNightMax = new TextBox();
            txtNightMin = new TextBox();
            lvExpose = new ListView();
            label6 = new Label();
            label7 = new Label();
            label8 = new Label();
            label9 = new Label();
            label10 = new Label();
            label11 = new Label();
            label12 = new Label();
            label13 = new Label();
            label14 = new Label();
            label15 = new Label();
            label16 = new Label();
            label17 = new Label();
            label18 = new Label();
            label19 = new Label();
            txtDayStart = new MaskedTextBox();
            txtDayEnd = new MaskedTextBox();
            chkAll = new CheckBox();
            btnSave = new Button();
            btnClose = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.BorderStyle = BorderStyle.FixedSingle;
            label1.Location = new Point(12, 9);
            label1.Name = "label1";
            label1.Size = new Size(100, 23);
            label1.TabIndex = 0;
            label1.Text = "카메라선택";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // cbCamera
            // 
            cbCamera.FormattingEnabled = true;
            cbCamera.Items.AddRange(new object[] { "카메라 1", "카메라 2", "카메라 3", "카메라 4" });
            cbCamera.Location = new Point(124, 9);
            cbCamera.Name = "cbCamera";
            cbCamera.Size = new Size(163, 23);
            cbCamera.TabIndex = 1;
            cbCamera.SelectedIndexChanged += cbCamera_SelectedIndexChanged;
            // 
            // cbMonth
            // 
            cbMonth.FormattingEnabled = true;
            cbMonth.Items.AddRange(new object[] { "1월", "2월", "3월", "4월", "5월", "6월", "7월", "8월", "9월", "10월", "11월", "12월" });
            cbMonth.Location = new Point(124, 38);
            cbMonth.Name = "cbMonth";
            cbMonth.Size = new Size(163, 23);
            cbMonth.TabIndex = 3;
            // 
            // label2
            // 
            label2.BorderStyle = BorderStyle.FixedSingle;
            label2.Location = new Point(12, 38);
            label2.Name = "label2";
            label2.Size = new Size(100, 23);
            label2.TabIndex = 2;
            label2.Text = "월선택";
            label2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label3
            // 
            label3.BorderStyle = BorderStyle.FixedSingle;
            label3.Location = new Point(12, 67);
            label3.Name = "label3";
            label3.Size = new Size(100, 23);
            label3.TabIndex = 4;
            label3.Text = "주간시간";
            label3.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label4
            // 
            label4.BorderStyle = BorderStyle.FixedSingle;
            label4.Location = new Point(309, 67);
            label4.Name = "label4";
            label4.Size = new Size(100, 23);
            label4.TabIndex = 8;
            label4.Text = "야간노출";
            label4.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label5
            // 
            label5.BorderStyle = BorderStyle.FixedSingle;
            label5.Location = new Point(309, 38);
            label5.Name = "label5";
            label5.Size = new Size(100, 23);
            label5.TabIndex = 7;
            label5.Text = "주간노출";
            label5.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // txtDayMax
            // 
            txtDayMax.Location = new Point(509, 38);
            txtDayMax.Name = "txtDayMax";
            txtDayMax.Size = new Size(69, 23);
            txtDayMax.TabIndex = 10;
            txtDayMax.TextAlign = HorizontalAlignment.Center;
            // 
            // txtDayMin
            // 
            txtDayMin.Location = new Point(415, 38);
            txtDayMin.Name = "txtDayMin";
            txtDayMin.Size = new Size(69, 23);
            txtDayMin.TabIndex = 9;
            txtDayMin.TextAlign = HorizontalAlignment.Center;
            // 
            // txtNightMax
            // 
            txtNightMax.Location = new Point(509, 67);
            txtNightMax.Name = "txtNightMax";
            txtNightMax.Size = new Size(69, 23);
            txtNightMax.TabIndex = 12;
            txtNightMax.TextAlign = HorizontalAlignment.Center;
            // 
            // txtNightMin
            // 
            txtNightMin.Location = new Point(415, 67);
            txtNightMin.Name = "txtNightMin";
            txtNightMin.Size = new Size(69, 23);
            txtNightMin.TabIndex = 11;
            txtNightMin.TextAlign = HorizontalAlignment.Center;
            // 
            // lvExpose
            // 
            lvExpose.Location = new Point(14, 97);
            lvExpose.Name = "lvExpose";
            lvExpose.Size = new Size(564, 341);
            lvExpose.TabIndex = 13;
            lvExpose.UseCompatibleStateImageBehavior = false;
            lvExpose.DoubleClick += lvExpose_DoubleClick;
            // 
            // label6
            // 
            label6.Location = new Point(597, 105);
            label6.Name = "label6";
            label6.Size = new Size(191, 23);
            label6.TabIndex = 14;
            label6.Text = "  월      일출시간      일몰시간";
            label6.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label7
            // 
            label7.Location = new Point(597, 128);
            label7.Name = "label7";
            label7.Size = new Size(191, 23);
            label7.TabIndex = 15;
            label7.Text = " 1월         7:24          17:25";
            label7.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label8
            // 
            label8.Location = new Point(597, 151);
            label8.Name = "label8";
            label8.Size = new Size(191, 23);
            label8.TabIndex = 16;
            label8.Text = " 2월         7:32          17:56";
            label8.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label9
            // 
            label9.Location = new Point(597, 197);
            label9.Name = "label9";
            label9.Size = new Size(191, 23);
            label9.TabIndex = 18;
            label9.Text = " 4월         6:17          18:52";
            label9.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label10
            // 
            label10.Location = new Point(597, 174);
            label10.Name = "label10";
            label10.Size = new Size(191, 23);
            label10.TabIndex = 17;
            label10.Text = " 3월         7:01          18:25";
            label10.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label11
            // 
            label11.Location = new Point(597, 243);
            label11.Name = "label11";
            label11.Size = new Size(191, 23);
            label11.TabIndex = 20;
            label11.Text = " 6월         5:14          19:42";
            label11.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label12
            // 
            label12.Location = new Point(597, 220);
            label12.Name = "label12";
            label12.Size = new Size(191, 23);
            label12.TabIndex = 19;
            label12.Text = " 5월         5:37          19:18";
            label12.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label13
            // 
            label13.Location = new Point(597, 289);
            label13.Name = "label13";
            label13.Size = new Size(191, 23);
            label13.TabIndex = 22;
            label13.Text = " 8월         5:37          19:36";
            label13.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label14
            // 
            label14.Location = new Point(597, 266);
            label14.Name = "label14";
            label14.Size = new Size(191, 23);
            label14.TabIndex = 21;
            label14.Text = " 7월         5:16          19:52";
            label14.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label15
            // 
            label15.Location = new Point(597, 335);
            label15.Name = "label15";
            label15.Size = new Size(191, 23);
            label15.TabIndex = 24;
            label15.Text = "10월         6:25          18:14";
            label15.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label16
            // 
            label16.Location = new Point(597, 312);
            label16.Name = "label16";
            label16.Size = new Size(191, 23);
            label16.TabIndex = 23;
            label16.Text = " 9월         6:02          18:59";
            label16.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label17
            // 
            label17.Location = new Point(597, 381);
            label17.Name = "label17";
            label17.Size = new Size(191, 23);
            label17.TabIndex = 26;
            label17.Text = "12월         7:23          17:15";
            label17.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label18
            // 
            label18.Location = new Point(597, 358);
            label18.Name = "label18";
            label18.Size = new Size(191, 23);
            label18.TabIndex = 25;
            label18.Text = "11월         6:53          17:34";
            label18.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label19
            // 
            label19.AutoSize = true;
            label19.Location = new Point(197, 71);
            label19.Name = "label19";
            label19.Size = new Size(15, 15);
            label19.TabIndex = 27;
            label19.Text = "~";
            // 
            // txtDayStart
            // 
            txtDayStart.InsertKeyMode = InsertKeyMode.Overwrite;
            txtDayStart.Location = new Point(124, 68);
            txtDayStart.Mask = "00:00";
            txtDayStart.Name = "txtDayStart";
            txtDayStart.ResetOnSpace = false;
            txtDayStart.Size = new Size(69, 23);
            txtDayStart.TabIndex = 28;
            txtDayStart.TextAlign = HorizontalAlignment.Center;
            txtDayStart.TextMaskFormat = MaskFormat.ExcludePromptAndLiterals;
            // 
            // txtDayEnd
            // 
            txtDayEnd.InsertKeyMode = InsertKeyMode.Overwrite;
            txtDayEnd.Location = new Point(218, 68);
            txtDayEnd.Mask = "00:00";
            txtDayEnd.Name = "txtDayEnd";
            txtDayEnd.ResetOnSpace = false;
            txtDayEnd.Size = new Size(69, 23);
            txtDayEnd.TabIndex = 29;
            txtDayEnd.TextAlign = HorizontalAlignment.Center;
            txtDayEnd.TextMaskFormat = MaskFormat.IncludePromptAndLiterals;
            // 
            // chkAll
            // 
            chkAll.AutoSize = true;
            chkAll.Location = new Point(313, 12);
            chkAll.Name = "chkAll";
            chkAll.Size = new Size(74, 19);
            chkAll.TabIndex = 30;
            chkAll.Text = "전체적용";
            chkAll.UseVisualStyleBackColor = true;
            // 
            // btnSave
            // 
            btnSave.Location = new Point(438, 8);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(140, 23);
            btnSave.TabIndex = 31;
            btnSave.Text = "수정하기";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // btnClose
            // 
            btnClose.Location = new Point(621, 37);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(140, 23);
            btnClose.TabIndex = 32;
            btnClose.Text = "나가기";
            btnClose.UseVisualStyleBackColor = true;
            // 
            // ExpForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(btnClose);
            Controls.Add(btnSave);
            Controls.Add(chkAll);
            Controls.Add(txtDayEnd);
            Controls.Add(txtDayStart);
            Controls.Add(label19);
            Controls.Add(label17);
            Controls.Add(label18);
            Controls.Add(label15);
            Controls.Add(label16);
            Controls.Add(label13);
            Controls.Add(label14);
            Controls.Add(label11);
            Controls.Add(label12);
            Controls.Add(label9);
            Controls.Add(label10);
            Controls.Add(label8);
            Controls.Add(label7);
            Controls.Add(label6);
            Controls.Add(lvExpose);
            Controls.Add(txtNightMax);
            Controls.Add(txtNightMin);
            Controls.Add(txtDayMax);
            Controls.Add(txtDayMin);
            Controls.Add(label4);
            Controls.Add(label5);
            Controls.Add(label3);
            Controls.Add(cbMonth);
            Controls.Add(label2);
            Controls.Add(cbCamera);
            Controls.Add(label1);
            Name = "ExpForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "월별 주간/야간 노출값 설정하기";
            FormClosing += ExpForm_FormClosing;
            Load += ExpForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private ComboBox cbCamera;
        private ComboBox cbMonth;
        private Label label2;
        private Label label3;
        private Label label4;
        private Label label5;
        private TextBox txtDayMax;
        private TextBox txtDayMin;
        private TextBox txtNightMax;
        private TextBox txtNightMin;
        private ListView lvExpose;
        private Label label6;
        private Label label7;
        private Label label8;
        private Label label9;
        private Label label10;
        private Label label11;
        private Label label12;
        private Label label13;
        private Label label14;
        private Label label15;
        private Label label16;
        private Label label17;
        private Label label18;
        private Label label19;
        private MaskedTextBox txtDayStart;
        private MaskedTextBox txtDayEnd;
        private CheckBox chkAll;
        private Button btnSave;
        private Button btnClose;
    }
}