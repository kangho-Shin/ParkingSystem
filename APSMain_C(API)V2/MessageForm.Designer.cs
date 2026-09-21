namespace APSMain
{
    partial class MessageForm
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
            SuspendLayout();
            // 
            // MessageForm
            // 
            AutoScaleDimensions = new SizeF(29F, 65F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            BackgroundImage = Properties.Resources.board;
            BackgroundImageLayout = ImageLayout.Stretch;
            ClientSize = new Size(1024, 768);
            DoubleBuffered = true;
            Font = new Font("맑은 고딕", 36F, FontStyle.Bold);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(12, 13, 12, 13);
            Name = "MessageForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "MessageForm";
            TopMost = true;
            FormClosed += MessageForm_FormClosed;
            Load += MessageForm_Load;
            Shown += MessageForm_Shown;
            ResumeLayout(false);
        }

        #endregion
    }
}