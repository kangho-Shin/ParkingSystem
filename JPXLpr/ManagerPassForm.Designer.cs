namespace JPXLpr
{
    partial class ManagerPassForm
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
            txtPass = new TextBox();
            btnOk = new Button();
            btnCancel = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.Location = new Point(12, 20);
            label1.Name = "label1";
            label1.Size = new Size(458, 23);
            label1.TabIndex = 0;
            label1.Text = "관리자 패스를 넣어 주십시요?";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // txtPass
            // 
            txtPass.Location = new Point(158, 68);
            txtPass.Name = "txtPass";
            txtPass.Size = new Size(162, 23);
            txtPass.TabIndex = 1;
            txtPass.TextAlign = HorizontalAlignment.Center;
            txtPass.KeyDown += txtPass_KeyDown;
            // 
            // btnOk
            // 
            btnOk.Location = new Point(111, 110);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(95, 25);
            btnOk.TabIndex = 2;
            btnOk.Text = "확인";
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += btnOk_Click;
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(270, 110);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(95, 25);
            btnCancel.TabIndex = 3;
            btnCancel.Text = "취소";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // ManagerPassForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(482, 160);
            Controls.Add(btnCancel);
            Controls.Add(btnOk);
            Controls.Add(txtPass);
            Controls.Add(label1);
            Name = "ManagerPassForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "관리자패스";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox txtPass;
        private Button btnOk;
        private Button btnCancel;
    }
}