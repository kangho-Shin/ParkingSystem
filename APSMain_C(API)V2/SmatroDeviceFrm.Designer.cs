namespace APSMain
{
    partial class SmatroDeviceFrm
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
            txtCrdId = new TextBox();
            txtCrdIp = new TextBox();
            label2 = new Label();
            txtCrdPort = new TextBox();
            txtPrePort = new TextBox();
            txtPreIp = new TextBox();
            label3 = new Label();
            txtPreId = new TextBox();
            label4 = new Label();
            txtKeyPort = new TextBox();
            txtKeyIp = new TextBox();
            label5 = new Label();
            txtAirPort = new TextBox();
            txtAirIp = new TextBox();
            label6 = new Label();
            label7 = new Label();
            cbDeviceType = new ComboBox();
            groupBox1 = new GroupBox();
            cbSamSlot4 = new ComboBox();
            label11 = new Label();
            cbSamSlot2 = new ComboBox();
            label10 = new Label();
            cbSamSlot3 = new ComboBox();
            label9 = new Label();
            cbSamSlot1 = new ComboBox();
            label8 = new Label();
            label12 = new Label();
            cbDhcp = new ComboBox();
            groupBox2 = new GroupBox();
            txtEntGateway = new TextBox();
            label15 = new Label();
            txtEntSaubnet = new TextBox();
            label14 = new Label();
            txtEntIp = new TextBox();
            label13 = new Label();
            btnReadInfo = new Button();
            btnSet = new Button();
            btnSave = new Button();
            btnCancle = new Button();
            btnReset = new Button();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            SuspendLayout();
            // 
            // label1
            // 
            label1.BorderStyle = BorderStyle.FixedSingle;
            label1.Location = new Point(23, 21);
            label1.Name = "label1";
            label1.Size = new Size(138, 29);
            label1.TabIndex = 0;
            label1.Text = "신용ID[PG ID]";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // txtCrdId
            // 
            txtCrdId.Location = new Point(180, 21);
            txtCrdId.Name = "txtCrdId";
            txtCrdId.Size = new Size(280, 29);
            txtCrdId.TabIndex = 1;
            // 
            // txtCrdIp
            // 
            txtCrdIp.Location = new Point(180, 63);
            txtCrdIp.Name = "txtCrdIp";
            txtCrdIp.Size = new Size(280, 29);
            txtCrdIp.TabIndex = 3;
            txtCrdIp.Text = "211.192.50.244";
            // 
            // label2
            // 
            label2.BorderStyle = BorderStyle.FixedSingle;
            label2.Location = new Point(23, 63);
            label2.Name = "label2";
            label2.Size = new Size(138, 29);
            label2.TabIndex = 2;
            label2.Text = "신용IP / PORT";
            label2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // txtCrdPort
            // 
            txtCrdPort.Location = new Point(475, 63);
            txtCrdPort.Name = "txtCrdPort";
            txtCrdPort.Size = new Size(118, 29);
            txtCrdPort.TabIndex = 4;
            txtCrdPort.Text = "5604";
            // 
            // txtPrePort
            // 
            txtPrePort.Location = new Point(475, 147);
            txtPrePort.Name = "txtPrePort";
            txtPrePort.Size = new Size(118, 29);
            txtPrePort.TabIndex = 9;
            // 
            // txtPreIp
            // 
            txtPreIp.Location = new Point(180, 147);
            txtPreIp.Name = "txtPreIp";
            txtPreIp.Size = new Size(280, 29);
            txtPreIp.TabIndex = 8;
            // 
            // label3
            // 
            label3.BorderStyle = BorderStyle.FixedSingle;
            label3.Location = new Point(23, 147);
            label3.Name = "label3";
            label3.Size = new Size(138, 29);
            label3.TabIndex = 7;
            label3.Text = "선불IP / PORT";
            label3.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // txtPreId
            // 
            txtPreId.Location = new Point(180, 105);
            txtPreId.Name = "txtPreId";
            txtPreId.Size = new Size(280, 29);
            txtPreId.TabIndex = 6;
            // 
            // label4
            // 
            label4.BorderStyle = BorderStyle.FixedSingle;
            label4.Location = new Point(23, 105);
            label4.Name = "label4";
            label4.Size = new Size(138, 29);
            label4.TabIndex = 5;
            label4.Text = "선불 ID";
            label4.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // txtKeyPort
            // 
            txtKeyPort.Location = new Point(475, 189);
            txtKeyPort.Name = "txtKeyPort";
            txtKeyPort.Size = new Size(118, 29);
            txtKeyPort.TabIndex = 12;
            txtKeyPort.Text = "5600";
            // 
            // txtKeyIp
            // 
            txtKeyIp.Location = new Point(180, 189);
            txtKeyIp.Name = "txtKeyIp";
            txtKeyIp.Size = new Size(280, 29);
            txtKeyIp.TabIndex = 11;
            txtKeyIp.Text = "211.192.50.244";
            // 
            // label5
            // 
            label5.BorderStyle = BorderStyle.FixedSingle;
            label5.Location = new Point(23, 189);
            label5.Name = "label5";
            label5.Size = new Size(138, 29);
            label5.TabIndex = 10;
            label5.Text = "KEY IP / PORT";
            label5.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // txtAirPort
            // 
            txtAirPort.Location = new Point(475, 231);
            txtAirPort.Name = "txtAirPort";
            txtAirPort.Size = new Size(118, 29);
            txtAirPort.TabIndex = 15;
            // 
            // txtAirIp
            // 
            txtAirIp.Location = new Point(180, 231);
            txtAirIp.Name = "txtAirIp";
            txtAirIp.Size = new Size(280, 29);
            txtAirIp.TabIndex = 14;
            // 
            // label6
            // 
            label6.BorderStyle = BorderStyle.FixedSingle;
            label6.Location = new Point(23, 231);
            label6.Name = "label6";
            label6.Size = new Size(138, 29);
            label6.TabIndex = 13;
            label6.Text = "AIR IP / PORT";
            label6.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label7
            // 
            label7.BorderStyle = BorderStyle.FixedSingle;
            label7.Location = new Point(23, 292);
            label7.Name = "label7";
            label7.Size = new Size(138, 29);
            label7.TabIndex = 16;
            label7.Text = "연동장치타입";
            label7.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // cbDeviceType
            // 
            cbDeviceType.FormattingEnabled = true;
            cbDeviceType.Items.AddRange(new object[] { "COM", "이더넷" });
            cbDeviceType.Location = new Point(23, 332);
            cbDeviceType.Name = "cbDeviceType";
            cbDeviceType.Size = new Size(138, 29);
            cbDeviceType.TabIndex = 17;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(cbSamSlot4);
            groupBox1.Controls.Add(label11);
            groupBox1.Controls.Add(cbSamSlot2);
            groupBox1.Controls.Add(label10);
            groupBox1.Controls.Add(cbSamSlot3);
            groupBox1.Controls.Add(label9);
            groupBox1.Controls.Add(cbSamSlot1);
            groupBox1.Controls.Add(label8);
            groupBox1.Location = new Point(182, 281);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(447, 140);
            groupBox1.TabIndex = 18;
            groupBox1.TabStop = false;
            groupBox1.Text = "SAM 설정";
            // 
            // cbSamSlot4
            // 
            cbSamSlot4.FormattingEnabled = true;
            cbSamSlot4.Items.AddRange(new object[] { "후불", "티머니", "이비", "한페이", "유페이", "마이비", "없음" });
            cbSamSlot4.Location = new Point(284, 78);
            cbSamSlot4.Name = "cbSamSlot4";
            cbSamSlot4.Size = new Size(136, 29);
            cbSamSlot4.TabIndex = 25;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new Point(230, 81);
            label11.Name = "label11";
            label11.Size = new Size(48, 21);
            label11.TabIndex = 24;
            label11.Text = "Slot4";
            // 
            // cbSamSlot2
            // 
            cbSamSlot2.FormattingEnabled = true;
            cbSamSlot2.Items.AddRange(new object[] { "후불", "티머니", "이비", "한페이", "유페이", "마이비", "없음" });
            cbSamSlot2.Location = new Point(284, 31);
            cbSamSlot2.Name = "cbSamSlot2";
            cbSamSlot2.Size = new Size(136, 29);
            cbSamSlot2.TabIndex = 23;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(230, 34);
            label10.Name = "label10";
            label10.Size = new Size(48, 21);
            label10.TabIndex = 22;
            label10.Text = "Slot2";
            // 
            // cbSamSlot3
            // 
            cbSamSlot3.FormattingEnabled = true;
            cbSamSlot3.Items.AddRange(new object[] { "후불", "티머니", "이비", "한페이", "유페이", "마이비", "없음" });
            cbSamSlot3.Location = new Point(78, 78);
            cbSamSlot3.Name = "cbSamSlot3";
            cbSamSlot3.Size = new Size(136, 29);
            cbSamSlot3.TabIndex = 21;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(24, 81);
            label9.Name = "label9";
            label9.Size = new Size(48, 21);
            label9.TabIndex = 20;
            label9.Text = "Slot3";
            // 
            // cbSamSlot1
            // 
            cbSamSlot1.FormattingEnabled = true;
            cbSamSlot1.Items.AddRange(new object[] { "후불", "티머니", "이비", "한페이", "유페이", "마이비", "없음" });
            cbSamSlot1.Location = new Point(78, 31);
            cbSamSlot1.Name = "cbSamSlot1";
            cbSamSlot1.Size = new Size(136, 29);
            cbSamSlot1.TabIndex = 19;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(24, 34);
            label8.Name = "label8";
            label8.Size = new Size(48, 21);
            label8.TabIndex = 0;
            label8.Text = "Slot1";
            // 
            // label12
            // 
            label12.BorderStyle = BorderStyle.FixedSingle;
            label12.Location = new Point(23, 435);
            label12.Name = "label12";
            label12.Size = new Size(138, 29);
            label12.TabIndex = 19;
            label12.Text = "단말기 IP 타입";
            label12.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // cbDhcp
            // 
            cbDhcp.FormattingEnabled = true;
            cbDhcp.Items.AddRange(new object[] { "DHCP", "STATIC" });
            cbDhcp.Location = new Point(182, 435);
            cbDhcp.Name = "cbDhcp";
            cbDhcp.Size = new Size(138, 29);
            cbDhcp.TabIndex = 20;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(txtEntGateway);
            groupBox2.Controls.Add(label15);
            groupBox2.Controls.Add(txtEntSaubnet);
            groupBox2.Controls.Add(label14);
            groupBox2.Controls.Add(txtEntIp);
            groupBox2.Controls.Add(label13);
            groupBox2.Location = new Point(184, 478);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(445, 140);
            groupBox2.TabIndex = 21;
            groupBox2.TabStop = false;
            groupBox2.Text = "단말기 네트워크 설정";
            // 
            // txtEntGateway
            // 
            txtEntGateway.Location = new Point(180, 105);
            txtEntGateway.Name = "txtEntGateway";
            txtEntGateway.Size = new Size(238, 29);
            txtEntGateway.TabIndex = 5;
            txtEntGateway.Text = "192.168.0.1";
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.Location = new Point(33, 108);
            label15.Name = "label15";
            label15.Size = new Size(132, 21);
            label15.TabIndex = 4;
            label15.Text = "단말기GATEWAY";
            // 
            // txtEntSaubnet
            // 
            txtEntSaubnet.Location = new Point(180, 71);
            txtEntSaubnet.Name = "txtEntSaubnet";
            txtEntSaubnet.Size = new Size(238, 29);
            txtEntSaubnet.TabIndex = 3;
            txtEntSaubnet.Text = "255.255.255.0";
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Location = new Point(33, 74);
            label14.Name = "label14";
            label14.Size = new Size(122, 21);
            label14.TabIndex = 2;
            label14.Text = "단말기 SUBNET";
            // 
            // txtEntIp
            // 
            txtEntIp.Location = new Point(180, 36);
            txtEntIp.Name = "txtEntIp";
            txtEntIp.Size = new Size(238, 29);
            txtEntIp.TabIndex = 1;
            txtEntIp.Text = "192.168.0.161";
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Location = new Point(33, 39);
            label13.Name = "label13";
            label13.Size = new Size(77, 21);
            label13.TabIndex = 0;
            label13.Text = "단말기 IP";
            // 
            // btnReadInfo
            // 
            btnReadInfo.Location = new Point(651, 21);
            btnReadInfo.Name = "btnReadInfo";
            btnReadInfo.Size = new Size(139, 36);
            btnReadInfo.TabIndex = 22;
            btnReadInfo.Text = "읽   기";
            btnReadInfo.UseVisualStyleBackColor = true;
            btnReadInfo.Click += btnReadInfo_Click;
            // 
            // btnSet
            // 
            btnSet.Location = new Point(651, 67);
            btnSet.Name = "btnSet";
            btnSet.Size = new Size(139, 36);
            btnSet.TabIndex = 23;
            btnSet.Text = "설  정";
            btnSet.UseVisualStyleBackColor = true;
            btnSet.Click += btnSet_Click;
            // 
            // btnSave
            // 
            btnSave.Location = new Point(651, 113);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(139, 36);
            btnSave.TabIndex = 24;
            btnSave.Text = "저  장";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // btnCancle
            // 
            btnCancle.Location = new Point(651, 578);
            btnCancle.Name = "btnCancle";
            btnCancle.Size = new Size(139, 36);
            btnCancle.TabIndex = 25;
            btnCancle.Text = "나가기";
            btnCancle.UseVisualStyleBackColor = true;
            btnCancle.Click += btnCancle_Click;
            // 
            // btnReset
            // 
            btnReset.Location = new Point(651, 159);
            btnReset.Name = "btnReset";
            btnReset.Size = new Size(139, 36);
            btnReset.TabIndex = 26;
            btnReset.Text = "리 셋";
            btnReset.UseVisualStyleBackColor = true;
            btnReset.Click += btnReset_Click;
            // 
            // SmatroDeviceFrm
            // 
            AutoScaleDimensions = new SizeF(9F, 21F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(802, 630);
            Controls.Add(btnReset);
            Controls.Add(btnCancle);
            Controls.Add(btnSave);
            Controls.Add(btnSet);
            Controls.Add(btnReadInfo);
            Controls.Add(groupBox2);
            Controls.Add(cbDhcp);
            Controls.Add(label12);
            Controls.Add(groupBox1);
            Controls.Add(cbDeviceType);
            Controls.Add(label7);
            Controls.Add(txtAirPort);
            Controls.Add(txtAirIp);
            Controls.Add(label6);
            Controls.Add(txtKeyPort);
            Controls.Add(txtKeyIp);
            Controls.Add(label5);
            Controls.Add(txtPrePort);
            Controls.Add(txtPreIp);
            Controls.Add(label3);
            Controls.Add(txtPreId);
            Controls.Add(label4);
            Controls.Add(txtCrdPort);
            Controls.Add(txtCrdIp);
            Controls.Add(label2);
            Controls.Add(txtCrdId);
            Controls.Add(label1);
            Font = new Font("맑은 고딕", 12F, FontStyle.Regular, GraphicsUnit.Point, 129);
            Margin = new Padding(4);
            Name = "SmatroDeviceFrm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "스마트로 TL3800 설정하기";
            TopMost = true;
            FormClosed += SmatroDeviceFrm_FormClosed;
            Load += SmatroDeviceFrm_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox txtCrdId;
        private TextBox txtCrdIp;
        private Label label2;
        private TextBox txtCrdPort;
        private TextBox txtPrePort;
        private TextBox txtPreIp;
        private Label label3;
        private TextBox txtPreId;
        private Label label4;
        private TextBox txtKeyPort;
        private TextBox txtKeyIp;
        private Label label5;
        private TextBox txtAirPort;
        private TextBox txtAirIp;
        private Label label6;
        private Label label7;
        private ComboBox cbDeviceType;
        private GroupBox groupBox1;
        private ComboBox cbSamSlot4;
        private Label label11;
        private ComboBox cbSamSlot2;
        private Label label10;
        private ComboBox cbSamSlot3;
        private Label label9;
        private ComboBox cbSamSlot1;
        private Label label8;
        private Label label12;
        private ComboBox cbDhcp;
        private GroupBox groupBox2;
        private TextBox txtEntGateway;
        private Label label15;
        private TextBox txtEntSaubnet;
        private Label label14;
        private TextBox txtEntIp;
        private Label label13;
        private Button btnReadInfo;
        private Button btnSet;
        private Button btnSave;
        private Button btnCancle;
        private Button btnReset;
    }
}