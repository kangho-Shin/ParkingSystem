namespace APSMain
{
    partial class MainForm15
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm15));
            panelFrame = new Panel();
            lblBlock = new Label();
            SuspendLayout();
            // 
            // panelFrame
            // 
            panelFrame.BackColor = SystemColors.ControlDarkDark;
            panelFrame.BackgroundImageLayout = ImageLayout.Stretch;
            panelFrame.Dock = DockStyle.Fill;
            panelFrame.Location = new Point(0, 0);
            panelFrame.Margin = new Padding(0);
            panelFrame.Name = "panelFrame";
            panelFrame.Size = new Size(1152, 864);
            panelFrame.TabIndex = 0;
            // 
            // lblBlock
            // 
            lblBlock.BackColor = Color.Black;
            lblBlock.Font = new Font("맑은 고딕", 36F, FontStyle.Regular, GraphicsUnit.Point, 129);
            lblBlock.ForeColor = SystemColors.Control;
            lblBlock.Location = new Point(107, 400);
            lblBlock.Name = "lblBlock";
            lblBlock.Size = new Size(914, 120);
            lblBlock.TabIndex = 40;
            lblBlock.Text = "";
            lblBlock.TextAlign = ContentAlignment.MiddleCenter;
            lblBlock.Visible = false;
            lblBlock.MouseDown += lblBlock_MouseDown;
            lblBlock.MouseUp += lblBlock_MouseUp;
            // 
            // MainForm15
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.ActiveCaption;
            BackgroundImageLayout = ImageLayout.Stretch;
            ClientSize = new Size(1152, 864);
            Controls.Add(lblBlock);
            Controls.Add(panelFrame);
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "MainForm15";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "MainForm15";
            FormClosing += MainForm15_FormClosing;
            Load += MainForm15_Load;
            ResumeLayout(false);
        }

        #endregion

        private Panel panelFrame;
        private Label lblBlock;
    }
}
