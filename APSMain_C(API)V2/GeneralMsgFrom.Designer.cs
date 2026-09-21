namespace APSMain
{
    partial class GeneralMsgFrom
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
            rtb = new RichTextBox();
            SuspendLayout();
            // 
            // rtb
            // 
            rtb.BackColor = Color.Black;
            rtb.Dock = DockStyle.Fill;
            rtb.ForeColor = SystemColors.Info;
            rtb.Location = new Point(0, 0);
            rtb.Name = "rtb";
            rtb.ReadOnly = true;
            rtb.ScrollBars = RichTextBoxScrollBars.None;
            rtb.Size = new Size(1060, 780);
            rtb.TabIndex = 0;
            rtb.Text = "";
            // 
            // GeneralMsgFrom
            // 
            AutoScaleDimensions = new SizeF(27F, 61F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.ActiveCaptionText;
            BackgroundImageLayout = ImageLayout.Stretch;
            ClientSize = new Size(1060, 780);
            Controls.Add(rtb);
            Font = new Font("맑은 고딕", 34F, FontStyle.Bold);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(12);
            Name = "GeneralMsgFrom";
            StartPosition = FormStartPosition.CenterParent;
            Text = "MsgForm";
            TopMost = true;
            FormClosed += PayMsgForm_FormClosed;
            Load += PayMsgForm_Load;
            Shown += PayMsgForm_Shown;
            ResumeLayout(false);
        }

        #endregion

        private RichTextBox rtb;
    }
}