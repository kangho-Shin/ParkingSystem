namespace APSMain
{
    partial class FeeStepForm
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
            cmbSiteNum = new ComboBox();
            label1 = new Label();
            label2 = new Label();
            cmbGroupNum = new ComboBox();
            label3 = new Label();
            cmbWeekType = new ComboBox();
            cmbDayNight = new ComboBox();
            label4 = new Label();
            cmbCarType = new ComboBox();
            label5 = new Label();
            label6 = new Label();
            cmbFeeStep = new ComboBox();
            label7 = new Label();
            label8 = new Label();
            txtParkFee = new TextBox();
            txtParkTime = new TextBox();
            btnAdd = new Button();
            btnDelete = new Button();
            btnExit = new Button();
            lvFeeStep = new ListView();
            columnHeader1 = new ColumnHeader();
            columnHeader2 = new ColumnHeader();
            columnHeader3 = new ColumnHeader();
            columnHeader4 = new ColumnHeader();
            columnHeader5 = new ColumnHeader();
            columnHeader6 = new ColumnHeader();
            columnHeader7 = new ColumnHeader();
            columnHeader8 = new ColumnHeader();
            columnHeader9 = new ColumnHeader();
            columnHeader10 = new ColumnHeader();
            txtMaxCount = new TextBox();
            label9 = new Label();
            btnAllView = new Button();
            btnSearch = new Button();
            SuspendLayout();
            // 
            // cmbSiteNum
            // 
            cmbSiteNum.FormattingEnabled = true;
            cmbSiteNum.Location = new Point(127, 13);
            cmbSiteNum.Name = "cmbSiteNum";
            cmbSiteNum.Size = new Size(134, 23);
            cmbSiteNum.TabIndex = 0;
            // 
            // label1
            // 
            label1.BorderStyle = BorderStyle.FixedSingle;
            label1.Location = new Point(12, 9);
            label1.Name = "label1";
            label1.Size = new Size(109, 29);
            label1.TabIndex = 1;
            label1.Text = "사이트번호";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            label2.BorderStyle = BorderStyle.FixedSingle;
            label2.Location = new Point(281, 9);
            label2.Name = "label2";
            label2.Size = new Size(109, 29);
            label2.TabIndex = 3;
            label2.Text = "그룹번호";
            label2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // cmbGroupNum
            // 
            cmbGroupNum.FormattingEnabled = true;
            cmbGroupNum.Location = new Point(396, 13);
            cmbGroupNum.Name = "cmbGroupNum";
            cmbGroupNum.Size = new Size(134, 23);
            cmbGroupNum.TabIndex = 2;
            // 
            // label3
            // 
            label3.BorderStyle = BorderStyle.FixedSingle;
            label3.Location = new Point(12, 42);
            label3.Name = "label3";
            label3.Size = new Size(109, 29);
            label3.TabIndex = 4;
            label3.Text = "주중/주말";
            label3.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // cmbWeekType
            // 
            cmbWeekType.FormattingEnabled = true;
            cmbWeekType.Location = new Point(127, 47);
            cmbWeekType.Name = "cmbWeekType";
            cmbWeekType.Size = new Size(134, 23);
            cmbWeekType.TabIndex = 5;
            // 
            // cmbDayNight
            // 
            cmbDayNight.FormattingEnabled = true;
            cmbDayNight.Location = new Point(396, 47);
            cmbDayNight.Name = "cmbDayNight";
            cmbDayNight.Size = new Size(134, 23);
            cmbDayNight.TabIndex = 7;
            // 
            // label4
            // 
            label4.BorderStyle = BorderStyle.FixedSingle;
            label4.Location = new Point(281, 42);
            label4.Name = "label4";
            label4.Size = new Size(109, 29);
            label4.TabIndex = 6;
            label4.Text = "주간/야간";
            label4.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // cmbCarType
            // 
            cmbCarType.FormattingEnabled = true;
            cmbCarType.Location = new Point(396, 113);
            cmbCarType.Name = "cmbCarType";
            cmbCarType.Size = new Size(134, 23);
            cmbCarType.TabIndex = 9;
            // 
            // label5
            // 
            label5.BorderStyle = BorderStyle.FixedSingle;
            label5.Location = new Point(281, 108);
            label5.Name = "label5";
            label5.Size = new Size(109, 29);
            label5.TabIndex = 8;
            label5.Text = "차종";
            label5.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label6
            // 
            label6.BorderStyle = BorderStyle.FixedSingle;
            label6.Location = new Point(12, 75);
            label6.Name = "label6";
            label6.Size = new Size(109, 29);
            label6.TabIndex = 10;
            label6.Text = "요금단계";
            label6.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // cmbFeeStep
            // 
            cmbFeeStep.FormattingEnabled = true;
            cmbFeeStep.Location = new Point(127, 81);
            cmbFeeStep.Name = "cmbFeeStep";
            cmbFeeStep.Size = new Size(134, 23);
            cmbFeeStep.TabIndex = 11;
            // 
            // label7
            // 
            label7.BorderStyle = BorderStyle.FixedSingle;
            label7.Location = new Point(281, 75);
            label7.Name = "label7";
            label7.Size = new Size(109, 29);
            label7.TabIndex = 12;
            label7.Text = "주차시간";
            label7.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label8
            // 
            label8.BorderStyle = BorderStyle.FixedSingle;
            label8.Location = new Point(12, 141);
            label8.Name = "label8";
            label8.Size = new Size(109, 29);
            label8.TabIndex = 14;
            label8.Text = "주차요금";
            label8.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // txtParkFee
            // 
            txtParkFee.Location = new Point(127, 146);
            txtParkFee.Name = "txtParkFee";
            txtParkFee.Size = new Size(134, 23);
            txtParkFee.TabIndex = 15;
            // 
            // txtParkTime
            // 
            txtParkTime.Location = new Point(396, 81);
            txtParkTime.Name = "txtParkTime";
            txtParkTime.Size = new Size(134, 23);
            txtParkTime.TabIndex = 16;
            // 
            // btnAdd
            // 
            btnAdd.Location = new Point(547, 12);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new Size(134, 29);
            btnAdd.TabIndex = 17;
            btnAdd.Text = "등 록";
            btnAdd.UseVisualStyleBackColor = true;
            btnAdd.Click += btnAdd_Click;
            // 
            // btnDelete
            // 
            btnDelete.Location = new Point(547, 42);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(134, 29);
            btnDelete.TabIndex = 18;
            btnDelete.Text = "삭 제";
            btnDelete.UseVisualStyleBackColor = true;
            btnDelete.Click += btnDelete_Click;
            // 
            // btnExit
            // 
            btnExit.Location = new Point(703, 107);
            btnExit.Name = "btnExit";
            btnExit.Size = new Size(134, 29);
            btnExit.TabIndex = 19;
            btnExit.Text = "나가기";
            btnExit.UseVisualStyleBackColor = true;
            btnExit.Click += btnExit_Click;
            // 
            // lvFeeStep
            // 
            lvFeeStep.CheckBoxes = true;
            lvFeeStep.Columns.AddRange(new ColumnHeader[] { columnHeader1, columnHeader2, columnHeader3, columnHeader4, columnHeader5, columnHeader6, columnHeader7, columnHeader8, columnHeader9, columnHeader10 });
            lvFeeStep.FullRowSelect = true;
            lvFeeStep.GridLines = true;
            lvFeeStep.Location = new Point(2, 175);
            lvFeeStep.Name = "lvFeeStep";
            lvFeeStep.Size = new Size(846, 356);
            lvFeeStep.TabIndex = 20;
            lvFeeStep.UseCompatibleStateImageBehavior = false;
            lvFeeStep.View = View.Details;
            lvFeeStep.SelectedIndexChanged += lvFeeStep_SelectedIndexChanged;
            // 
            // columnHeader1
            // 
            columnHeader1.Text = "사이트";
            // 
            // columnHeader2
            // 
            columnHeader2.Text = "그룹";
            // 
            // columnHeader3
            // 
            columnHeader3.Text = "주중";
            columnHeader3.Width = 90;
            // 
            // columnHeader4
            // 
            columnHeader4.Text = "주/야";
            columnHeader4.Width = 90;
            // 
            // columnHeader5
            // 
            columnHeader5.Text = "차종";
            columnHeader5.Width = 80;
            // 
            // columnHeader6
            // 
            columnHeader6.Text = "요금단계";
            // 
            // columnHeader7
            // 
            columnHeader7.Text = "주차시간";
            // 
            // columnHeader8
            // 
            columnHeader8.Text = "주차요금";
            columnHeader8.Width = 90;
            // 
            // columnHeader9
            // 
            columnHeader9.Text = "카운트";
            // 
            // columnHeader10
            // 
            columnHeader10.Text = "등록일자";
            columnHeader10.Width = 150;
            // 
            // txtMaxCount
            // 
            txtMaxCount.Location = new Point(127, 113);
            txtMaxCount.Name = "txtMaxCount";
            txtMaxCount.Size = new Size(134, 23);
            txtMaxCount.TabIndex = 22;
            // 
            // label9
            // 
            label9.BorderStyle = BorderStyle.FixedSingle;
            label9.Location = new Point(12, 108);
            label9.Name = "label9";
            label9.Size = new Size(109, 29);
            label9.TabIndex = 21;
            label9.Text = "적용건수";
            label9.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // btnAllView
            // 
            btnAllView.Location = new Point(547, 75);
            btnAllView.Name = "btnAllView";
            btnAllView.Size = new Size(134, 29);
            btnAllView.TabIndex = 23;
            btnAllView.Text = "전체검색";
            btnAllView.UseVisualStyleBackColor = true;
            btnAllView.Click += btnAllView_Click;
            // 
            // btnSearch
            // 
            btnSearch.Location = new Point(547, 108);
            btnSearch.Name = "btnSearch";
            btnSearch.Size = new Size(134, 29);
            btnSearch.TabIndex = 24;
            btnSearch.Text = "조건검색";
            btnSearch.UseVisualStyleBackColor = true;
            btnSearch.Click += btnSearch_Click;
            // 
            // FeeStepForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(849, 533);
            Controls.Add(btnSearch);
            Controls.Add(btnAllView);
            Controls.Add(txtMaxCount);
            Controls.Add(label9);
            Controls.Add(lvFeeStep);
            Controls.Add(btnExit);
            Controls.Add(btnDelete);
            Controls.Add(btnAdd);
            Controls.Add(txtParkTime);
            Controls.Add(txtParkFee);
            Controls.Add(label8);
            Controls.Add(label7);
            Controls.Add(cmbFeeStep);
            Controls.Add(label6);
            Controls.Add(cmbCarType);
            Controls.Add(label5);
            Controls.Add(cmbDayNight);
            Controls.Add(label4);
            Controls.Add(cmbWeekType);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(cmbGroupNum);
            Controls.Add(label1);
            Controls.Add(cmbSiteNum);
            Name = "FeeStepForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "FeeStepForm";
            TopMost = true;
            FormClosing += FeeStepForm_FormClosing;
            Load += FeeStepForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ComboBox cmbSiteNum;
        private Label label1;
        private Label label2;
        private ComboBox cmbGroupNum;
        private Label label3;
        private ComboBox cmbWeekType;
        private ComboBox cmbDayNight;
        private Label label4;
        private ComboBox cmbCarType;
        private Label label5;
        private Label label6;
        private ComboBox cmbFeeStep;
        private Label label7;
        private Label label8;
        private TextBox txtParkFee;
        private TextBox txtParkTime;
        private Button btnAdd;
        private Button btnDelete;
        private Button btnExit;
        private ListView lvFeeStep;
        private ColumnHeader columnHeader1;
        private ColumnHeader columnHeader2;
        private ColumnHeader columnHeader3;
        private ColumnHeader columnHeader4;
        private ColumnHeader columnHeader5;
        private ColumnHeader columnHeader6;
        private ColumnHeader columnHeader7;
        private ColumnHeader columnHeader8;
        private ColumnHeader columnHeader9;
        private ColumnHeader columnHeader10;
        private TextBox txtMaxCount;
        private Label label9;
        private Button btnAllView;
        private Button btnSearch;
    }
}