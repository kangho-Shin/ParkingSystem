namespace APSMain
{
    partial class LDMShow
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
            label1 = new Label();
            cbLDMList = new ComboBox();
            label2 = new Label();
            ldmText = new TextBox();
            btnSendData = new Button();
            rdLine1 = new RadioButton();
            rdLine2 = new RadioButton();
            groupBox1 = new GroupBox();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            groupBox2 = new GroupBox();
            rdMemory1 = new RadioButton();
            rdMemory2 = new RadioButton();
            groupBox3 = new GroupBox();
            rdShift1 = new RadioButton();
            rdShift2 = new RadioButton();
            btnGateOpen = new Button();
            btnGateClose = new Button();
            btnDetReset = new Button();
            btnGateReset = new Button();
            btnCancle = new Button();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            groupBox3.SuspendLayout();
            SuspendLayout();
            // 
            // label1
            // 
            label1.BorderStyle = BorderStyle.FixedSingle;
            label1.Location = new Point(13, 19);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(143, 32);
            label1.TabIndex = 0;
            label1.Text = "전광판 IP";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // cbLDMList
            // 
            cbLDMList.FormattingEnabled = true;
            cbLDMList.Location = new Point(163, 22);
            cbLDMList.Margin = new Padding(4);
            cbLDMList.Name = "cbLDMList";
            cbLDMList.Size = new Size(217, 29);
            cbLDMList.TabIndex = 1;
            // 
            // label2
            // 
            label2.BorderStyle = BorderStyle.FixedSingle;
            label2.Location = new Point(13, 59);
            label2.Margin = new Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new Size(143, 32);
            label2.TabIndex = 2;
            label2.Text = "문구";
            label2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // ldmText
            // 
            ldmText.Location = new Point(163, 59);
            ldmText.Name = "ldmText";
            ldmText.Size = new Size(551, 29);
            ldmText.TabIndex = 3;
            // 
            // btnSendData
            // 
            btnSendData.Location = new Point(578, 18);
            btnSendData.Name = "btnSendData";
            btnSendData.Size = new Size(136, 35);
            btnSendData.TabIndex = 4;
            btnSendData.Text = "문구전송";
            btnSendData.UseVisualStyleBackColor = true;
            btnSendData.Click += btnSendData_Click;
            // 
            // rdLine1
            // 
            rdLine1.AutoSize = true;
            rdLine1.Checked = true;
            rdLine1.Location = new Point(49, 22);
            rdLine1.Name = "rdLine1";
            rdLine1.Size = new Size(53, 25);
            rdLine1.TabIndex = 5;
            rdLine1.TabStop = true;
            rdLine1.Text = "1단";
            rdLine1.UseVisualStyleBackColor = true;
            // 
            // rdLine2
            // 
            rdLine2.AutoSize = true;
            rdLine2.Location = new Point(148, 22);
            rdLine2.Name = "rdLine2";
            rdLine2.Size = new Size(53, 25);
            rdLine2.TabIndex = 6;
            rdLine2.Text = "2단";
            rdLine2.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(rdLine1);
            groupBox1.Controls.Add(rdLine2);
            groupBox1.Location = new Point(179, 100);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(270, 50);
            groupBox1.TabIndex = 7;
            groupBox1.TabStop = false;
            // 
            // label3
            // 
            label3.BorderStyle = BorderStyle.FixedSingle;
            label3.Location = new Point(13, 115);
            label3.Margin = new Padding(4, 0, 4, 0);
            label3.Name = "label3";
            label3.Size = new Size(143, 32);
            label3.TabIndex = 8;
            label3.Text = "라인";
            label3.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label4
            // 
            label4.BorderStyle = BorderStyle.FixedSingle;
            label4.Location = new Point(13, 175);
            label4.Margin = new Padding(4, 0, 4, 0);
            label4.Name = "label4";
            label4.Size = new Size(143, 32);
            label4.TabIndex = 9;
            label4.Text = "메모리";
            label4.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label5
            // 
            label5.BorderStyle = BorderStyle.FixedSingle;
            label5.Location = new Point(13, 246);
            label5.Margin = new Padding(4, 0, 4, 0);
            label5.Name = "label5";
            label5.Size = new Size(143, 32);
            label5.TabIndex = 10;
            label5.Text = "효과";
            label5.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(rdMemory1);
            groupBox2.Controls.Add(rdMemory2);
            groupBox2.Location = new Point(179, 157);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(270, 50);
            groupBox2.TabIndex = 8;
            groupBox2.TabStop = false;
            // 
            // rdMemory1
            // 
            rdMemory1.AutoSize = true;
            rdMemory1.Checked = true;
            rdMemory1.Location = new Point(49, 22);
            rdMemory1.Name = "rdMemory1";
            rdMemory1.Size = new Size(93, 25);
            rdMemory1.TabIndex = 5;
            rdMemory1.TabStop = true;
            rdMemory1.Text = "EEPROM";
            rdMemory1.UseVisualStyleBackColor = true;
            // 
            // rdMemory2
            // 
            rdMemory2.AutoSize = true;
            rdMemory2.Location = new Point(148, 22);
            rdMemory2.Name = "rdMemory2";
            rdMemory2.Size = new Size(64, 25);
            rdMemory2.TabIndex = 6;
            rdMemory2.Text = "RAM";
            rdMemory2.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(rdShift1);
            groupBox3.Controls.Add(rdShift2);
            groupBox3.Location = new Point(179, 228);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(270, 50);
            groupBox3.TabIndex = 8;
            groupBox3.TabStop = false;
            // 
            // rdShift1
            // 
            rdShift1.AutoSize = true;
            rdShift1.Checked = true;
            rdShift1.Location = new Point(49, 22);
            rdShift1.Name = "rdShift1";
            rdShift1.Size = new Size(60, 25);
            rdShift1.TabIndex = 5;
            rdShift1.TabStop = true;
            rdShift1.Text = "고정";
            rdShift1.UseVisualStyleBackColor = true;
            // 
            // rdShift2
            // 
            rdShift2.AutoSize = true;
            rdShift2.Location = new Point(148, 22);
            rdShift2.Name = "rdShift2";
            rdShift2.Size = new Size(92, 25);
            rdShift2.TabIndex = 6;
            rdShift2.Text = "왼쪽흐름";
            rdShift2.UseVisualStyleBackColor = true;
            // 
            // btnGateOpen
            // 
            btnGateOpen.Location = new Point(488, 112);
            btnGateOpen.Name = "btnGateOpen";
            btnGateOpen.Size = new Size(136, 35);
            btnGateOpen.TabIndex = 11;
            btnGateOpen.Text = "차단기열기";
            btnGateOpen.UseVisualStyleBackColor = true;
            btnGateOpen.Click += btnGateOpen_Click;
            // 
            // btnGateClose
            // 
            btnGateClose.Location = new Point(630, 112);
            btnGateClose.Name = "btnGateClose";
            btnGateClose.Size = new Size(136, 35);
            btnGateClose.TabIndex = 12;
            btnGateClose.Text = "차단기닫기";
            btnGateClose.UseVisualStyleBackColor = true;
            btnGateClose.Click += btnGateClose_Click;
            // 
            // btnDetReset
            // 
            btnDetReset.Location = new Point(488, 157);
            btnDetReset.Name = "btnDetReset";
            btnDetReset.Size = new Size(136, 35);
            btnDetReset.TabIndex = 13;
            btnDetReset.Text = "검지기리셋";
            btnDetReset.UseVisualStyleBackColor = true;
            btnDetReset.Click += btnDetReset_Click;
            // 
            // btnGateReset
            // 
            btnGateReset.Location = new Point(630, 157);
            btnGateReset.Name = "btnGateReset";
            btnGateReset.Size = new Size(136, 35);
            btnGateReset.TabIndex = 14;
            btnGateReset.Text = "차단기리셋";
            btnGateReset.UseVisualStyleBackColor = true;
            btnGateReset.Click += btnGateReset_Click;
            // 
            // btnCancle
            // 
            btnCancle.Location = new Point(630, 240);
            btnCancle.Name = "btnCancle";
            btnCancle.Size = new Size(136, 35);
            btnCancle.TabIndex = 15;
            btnCancle.Text = "나가기";
            btnCancle.UseVisualStyleBackColor = true;
            btnCancle.Click += btnCancle_Click;
            // 
            // LDMShow
            // 
            AutoScaleDimensions = new SizeF(10F, 21F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(778, 304);
            Controls.Add(btnCancle);
            Controls.Add(btnGateReset);
            Controls.Add(btnDetReset);
            Controls.Add(btnGateClose);
            Controls.Add(btnGateOpen);
            Controls.Add(groupBox3);
            Controls.Add(groupBox2);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(groupBox1);
            Controls.Add(btnSendData);
            Controls.Add(ldmText);
            Controls.Add(label2);
            Controls.Add(cbLDMList);
            Controls.Add(label1);
            Font = new Font("맑은 고딕", 12F, FontStyle.Bold, GraphicsUnit.Point, 129);
            Margin = new Padding(4);
            Name = "LDMShow";
            StartPosition = FormStartPosition.CenterParent;
            Text = "LDMShow";
            TopMost = true;
            Load += LDMShow_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private ComboBox cbLDMList;
        private Label label2;
        private TextBox ldmText;
        private Button btnSendData;
        private RadioButton rdLine1;
        private RadioButton rdLine2;
        private GroupBox groupBox1;
        private Label label3;
        private Label label4;
        private Label label5;
        private GroupBox groupBox2;
        private GroupBox groupBox3;
        private RadioButton rdShift1;
        private RadioButton rdShift2;
        private RadioButton rdMemory1;
        private RadioButton rdMemory2;
        private Button btnGateOpen;
        private Button btnGateClose;
        private Button btnDetReset;
        private Button btnGateReset;
        private Button btnCancle;
    }
}