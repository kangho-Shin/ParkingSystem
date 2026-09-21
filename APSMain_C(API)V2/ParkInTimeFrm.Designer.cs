namespace APSMain
{
    partial class ParkInTimeFrm
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
            lbLog = new ListBox();
            label1 = new Label();
            btnCalculate = new Button();
            dtpInDate = new DateTimePicker();
            label2 = new Label();
            cmbDisList = new ComboBox();
            btnDisAdd = new Button();
            label3 = new Label();
            txtPrepay = new TextBox();
            dtpInTime = new DateTimePicker();
            btnCardApproval = new Button();
            btnCardCancellation = new Button();
            txtPay = new TextBox();
            label4 = new Label();
            txtAcceptnum = new TextBox();
            label5 = new Label();
            SuspendLayout();
            // 
            // lbLog
            // 
            lbLog.FormattingEnabled = true;
            lbLog.HorizontalScrollbar = true;
            lbLog.ItemHeight = 21;
            lbLog.Location = new Point(15, 214);
            lbLog.Margin = new Padding(4);
            lbLog.Name = "lbLog";
            lbLog.Size = new Size(578, 403);
            lbLog.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(24, 21);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(74, 21);
            label1.TabIndex = 2;
            label1.Text = "입차시간";
            // 
            // btnCalculate
            // 
            btnCalculate.ForeColor = Color.Black;
            btnCalculate.Location = new Point(448, 59);
            btnCalculate.Name = "btnCalculate";
            btnCalculate.Size = new Size(145, 32);
            btnCalculate.TabIndex = 3;
            btnCalculate.Text = "계산하기";
            btnCalculate.UseVisualStyleBackColor = true;
            btnCalculate.Click += btnCalculate_Click;
            // 
            // dtpInDate
            // 
            dtpInDate.Format = DateTimePickerFormat.Short;
            dtpInDate.Location = new Point(148, 21);
            dtpInDate.Margin = new Padding(4);
            dtpInDate.Name = "dtpInDate";
            dtpInDate.Size = new Size(130, 29);
            dtpInDate.TabIndex = 4;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(24, 59);
            label2.Margin = new Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new Size(74, 21);
            label2.TabIndex = 5;
            label2.Text = "할인선택";
            // 
            // cmbDisList
            // 
            cmbDisList.FormattingEnabled = true;
            cmbDisList.Location = new Point(148, 59);
            cmbDisList.Margin = new Padding(4);
            cmbDisList.Name = "cmbDisList";
            cmbDisList.Size = new Size(277, 29);
            cmbDisList.TabIndex = 6;
            // 
            // btnDisAdd
            // 
            btnDisAdd.ForeColor = Color.Black;
            btnDisAdd.Location = new Point(448, 97);
            btnDisAdd.Name = "btnDisAdd";
            btnDisAdd.Size = new Size(145, 32);
            btnDisAdd.TabIndex = 7;
            btnDisAdd.Text = "할인하기";
            btnDisAdd.UseVisualStyleBackColor = true;
            btnDisAdd.Click += btnDisAdd_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(24, 97);
            label3.Margin = new Padding(4, 0, 4, 0);
            label3.Name = "label3";
            label3.Size = new Size(106, 21);
            label3.TabIndex = 8;
            label3.Text = "사전정산금액";
            // 
            // txtPrepay
            // 
            txtPrepay.Location = new Point(148, 97);
            txtPrepay.Name = "txtPrepay";
            txtPrepay.Size = new Size(277, 29);
            txtPrepay.TabIndex = 9;
            // 
            // dtpInTime
            // 
            dtpInTime.Format = DateTimePickerFormat.Time;
            dtpInTime.Location = new Point(295, 21);
            dtpInTime.Name = "dtpInTime";
            dtpInTime.ShowUpDown = true;
            dtpInTime.Size = new Size(130, 29);
            dtpInTime.TabIndex = 10;
            // 
            // btnCardApproval
            // 
            btnCardApproval.ForeColor = Color.Black;
            btnCardApproval.Location = new Point(448, 135);
            btnCardApproval.Name = "btnCardApproval";
            btnCardApproval.Size = new Size(145, 32);
            btnCardApproval.TabIndex = 11;
            btnCardApproval.Text = "신용승인";
            btnCardApproval.UseVisualStyleBackColor = true;
            btnCardApproval.Click += btnCardApproval_Click;
            // 
            // btnCardCancellation
            // 
            btnCardCancellation.ForeColor = Color.Black;
            btnCardCancellation.Location = new Point(448, 173);
            btnCardCancellation.Name = "btnCardCancellation";
            btnCardCancellation.Size = new Size(145, 32);
            btnCardCancellation.TabIndex = 12;
            btnCardCancellation.Text = "신용취소";
            btnCardCancellation.UseVisualStyleBackColor = true;
            btnCardCancellation.Click += btnCardCancellation_Click;
            // 
            // txtPay
            // 
            txtPay.Location = new Point(148, 135);
            txtPay.Name = "txtPay";
            txtPay.Size = new Size(277, 29);
            txtPay.TabIndex = 14;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(24, 135);
            label4.Margin = new Padding(4, 0, 4, 0);
            label4.Name = "label4";
            label4.Size = new Size(74, 21);
            label4.TabIndex = 13;
            label4.Text = "승인금액";
            // 
            // txtAcceptnum
            // 
            txtAcceptnum.Location = new Point(148, 173);
            txtAcceptnum.Name = "txtAcceptnum";
            txtAcceptnum.Size = new Size(277, 29);
            txtAcceptnum.TabIndex = 16;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(24, 173);
            label5.Margin = new Padding(4, 0, 4, 0);
            label5.Name = "label5";
            label5.Size = new Size(74, 21);
            label5.TabIndex = 15;
            label5.Text = "승인번호";
            // 
            // ParkInTimeFrm
            // 
            AutoScaleDimensions = new SizeF(9F, 21F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(0, 51, 102);
            ClientSize = new Size(607, 630);
            Controls.Add(txtAcceptnum);
            Controls.Add(label5);
            Controls.Add(txtPay);
            Controls.Add(label4);
            Controls.Add(btnCardCancellation);
            Controls.Add(btnCardApproval);
            Controls.Add(dtpInTime);
            Controls.Add(txtPrepay);
            Controls.Add(label3);
            Controls.Add(btnDisAdd);
            Controls.Add(cmbDisList);
            Controls.Add(label2);
            Controls.Add(dtpInDate);
            Controls.Add(btnCalculate);
            Controls.Add(label1);
            Controls.Add(lbLog);
            Font = new Font("맑은 고딕", 12F, FontStyle.Regular, GraphicsUnit.Point, 129);
            ForeColor = Color.White;
            Margin = new Padding(4);
            Name = "ParkInTimeFrm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "ParkInTime";
            TopMost = true;
            FormClosing += ParkInTimeFrm_FormClosing;
            Load += ParkInTime_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private ListBox lbLog;
        private Label label1;
        private Button btnCalculate;
        private DateTimePicker dtpInDate;
        private Label label2;
        private ComboBox cmbDisList;
        private Button btnDisAdd;
        private Label label3;
        private TextBox txtPrepay;
        private DateTimePicker dtpInTime;
        private Button btnCardApproval;
        private Button btnCardCancellation;
        private TextBox txtPay;
        private Label label4;
        private TextBox txtAcceptnum;
        private Label label5;
    }
}