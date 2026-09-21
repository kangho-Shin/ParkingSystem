namespace APSMain
{
    partial class DiscountSelectForm
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
            cmbSelect = new ComboBox();
            btnSelect = new Button();
            btnCredit = new Button();
            label1 = new Label();
            SuspendLayout();
            // 
            // cmbSelect
            // 
            cmbSelect.FormattingEnabled = true;
            cmbSelect.Location = new Point(40, 91);
            cmbSelect.Margin = new Padding(4);
            cmbSelect.Name = "cmbSelect";
            cmbSelect.Size = new Size(228, 29);
            cmbSelect.TabIndex = 0;
            cmbSelect.SelectedIndexChanged += cmbSelect_SelectedIndexChanged;
            // 
            // btnSelect
            // 
            btnSelect.Location = new Point(293, 91);
            btnSelect.Margin = new Padding(4);
            btnSelect.Name = "btnSelect";
            btnSelect.Size = new Size(125, 32);
            btnSelect.TabIndex = 1;
            btnSelect.Text = "확인";
            btnSelect.UseVisualStyleBackColor = true;
            btnSelect.Click += btnSelect_Click;
            // 
            // btnCredit
            // 
            btnCredit.Location = new Point(293, 149);
            btnCredit.Margin = new Padding(4);
            btnCredit.Name = "btnCredit";
            btnCredit.Size = new Size(125, 32);
            btnCredit.TabIndex = 2;
            btnCredit.Text = "신용카드결제";
            btnCredit.UseVisualStyleBackColor = true;
            btnCredit.Click += btnCredit_Click;
            // 
            // label1
            // 
            label1.Location = new Point(40, 59);
            label1.Name = "label1";
            label1.Size = new Size(228, 28);
            label1.TabIndex = 3;
            label1.Text = "할인선택";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // DiscountSelectForm
            // 
            AutoScaleDimensions = new SizeF(9F, 21F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(454, 227);
            Controls.Add(label1);
            Controls.Add(btnCredit);
            Controls.Add(btnSelect);
            Controls.Add(cmbSelect);
            Font = new Font("맑은 고딕", 12F);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Margin = new Padding(4);
            Name = "DiscountSelectForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "할인키를 선택하세요";
            TopMost = true;
            Load += DiscountSelectForm_Load;
            ResumeLayout(false);
        }

        #endregion

        private ComboBox cmbSelect;
        private Button btnSelect;
        private Button btnCredit;
        private Label label1;
    }
}