namespace APSMain
{
    partial class CardCancelForm
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
            txtMoney = new TextBox();
            label4 = new Label();
            txtDateTime = new TextBox();
            label2 = new Label();
            txtAcceptNum = new TextBox();
            label1 = new Label();
            lbLog = new ListBox();
            btnCancel = new Button();
            btnClose = new Button();
            SuspendLayout();
            // 
            // txtMoney
            // 
            txtMoney.Location = new Point(151, 91);
            txtMoney.Margin = new Padding(4);
            txtMoney.Name = "txtMoney";
            txtMoney.Size = new Size(263, 27);
            txtMoney.TabIndex = 12;
            // 
            // label4
            // 
            label4.BorderStyle = BorderStyle.FixedSingle;
            label4.Location = new Point(13, 89);
            label4.Margin = new Padding(4, 0, 4, 0);
            label4.Name = "label4";
            label4.Size = new Size(130, 30);
            label4.TabIndex = 11;
            label4.Text = "승인금액";
            label4.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // txtDateTime
            // 
            txtDateTime.Location = new Point(151, 50);
            txtDateTime.Margin = new Padding(4);
            txtDateTime.Name = "txtDateTime";
            txtDateTime.Size = new Size(263, 27);
            txtDateTime.TabIndex = 10;
            // 
            // label2
            // 
            label2.BorderStyle = BorderStyle.FixedSingle;
            label2.Location = new Point(13, 48);
            label2.Margin = new Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new Size(130, 30);
            label2.TabIndex = 9;
            label2.Text = "승인일자";
            label2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // txtAcceptNum
            // 
            txtAcceptNum.Location = new Point(151, 11);
            txtAcceptNum.Margin = new Padding(4);
            txtAcceptNum.Name = "txtAcceptNum";
            txtAcceptNum.Size = new Size(263, 27);
            txtAcceptNum.TabIndex = 8;
            // 
            // label1
            // 
            label1.BorderStyle = BorderStyle.FixedSingle;
            label1.Location = new Point(13, 9);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(130, 30);
            label1.TabIndex = 7;
            label1.Text = "승인번호";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lbLog
            // 
            lbLog.FormattingEnabled = true;
            lbLog.ItemHeight = 20;
            lbLog.Location = new Point(14, 129);
            lbLog.Name = "lbLog";
            lbLog.Size = new Size(593, 364);
            lbLog.TabIndex = 13;
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(447, 9);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(160, 30);
            btnCancel.TabIndex = 14;
            btnCancel.Text = "승인취소";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // btnClose
            // 
            btnClose.Location = new Point(447, 88);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(160, 30);
            btnClose.TabIndex = 15;
            btnClose.Text = "나가기";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += btnClose_Click;
            // 
            // CardCancelForm
            // 
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(623, 500);
            Controls.Add(btnClose);
            Controls.Add(btnCancel);
            Controls.Add(lbLog);
            Controls.Add(txtMoney);
            Controls.Add(label4);
            Controls.Add(txtDateTime);
            Controls.Add(label2);
            Controls.Add(txtAcceptNum);
            Controls.Add(label1);
            Font = new Font("맑은 고딕", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            Margin = new Padding(4);
            Name = "CardCancelForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "CardCancelForm";
            TopMost = true;
            Load += CardCancelForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtMoney;
        private Label label4;
        private TextBox txtDateTime;
        private Label label2;
        private TextBox txtAcceptNum;
        private Label label1;
        private ListBox lbLog;
        private Button btnCancel;
        private Button btnClose;
    }
}