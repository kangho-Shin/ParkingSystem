namespace APSMain
{
    partial class PayMsgForm
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
            lblMsg1 = new Label();
            lblMsg2 = new Label();
            lblMsg3 = new Label();
            SuspendLayout();
            // 
            // lblMsg1
            // 
            lblMsg1.BackColor = Color.White;
            lblMsg1.Location = new Point(12, 193);
            lblMsg1.Name = "lblMsg1";
            lblMsg1.Size = new Size(759, 71);
            lblMsg1.TabIndex = 0;
            lblMsg1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblMsg2
            // 
            lblMsg2.BackColor = Color.White;
            lblMsg2.Location = new Point(12, 273);
            lblMsg2.Name = "lblMsg2";
            lblMsg2.Size = new Size(759, 71);
            lblMsg2.TabIndex = 1;
            lblMsg2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblMsg3
            // 
            lblMsg3.BackColor = Color.White;
            lblMsg3.Location = new Point(12, 353);
            lblMsg3.Name = "lblMsg3";
            lblMsg3.Size = new Size(759, 71);
            lblMsg3.TabIndex = 2;
            lblMsg3.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // PayMsgForm
            // 
            AutoScaleDimensions = new SizeF(29F, 65F);
            AutoScaleMode = AutoScaleMode.Font;
            BackgroundImage = Properties.Resources.payCancel;
            BackgroundImageLayout = ImageLayout.Stretch;
            ClientSize = new Size(800, 450);
            Controls.Add(lblMsg3);
            Controls.Add(lblMsg2);
            Controls.Add(lblMsg1);
            Font = new Font("맑은 고딕", 36F, FontStyle.Bold);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(12, 13, 12, 13);
            Name = "PayMsgForm";
            StartPosition = FormStartPosition.Manual;
            Text = "PayMsgFrom";
            TopMost = true;
            FormClosed += PayMsgFrom_FormClosed;
            Load += PayMsgFrom_Load;
            Shown += PayMsgFrom_Shown;
            ResumeLayout(false);
        }

        #endregion

        private Label lblMsg1;
        private Label lblMsg2;
        private Label lblMsg3;
    }
}