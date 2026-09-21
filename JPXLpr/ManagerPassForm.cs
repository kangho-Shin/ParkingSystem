using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JPXLpr
{
    public partial class ManagerPassForm : Form
    {
        public string? Password = string.Empty;

        public ManagerPassForm()
        {
            InitializeComponent();
        }
/*
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter) {
                btnOk.PerformClick();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }
*/
        private void btnOk_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Password = txtPass.Text;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;

            this.Close();
        }

        private void txtPass_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) {
                btnOk.PerformClick();
                e.SuppressKeyPress = true;
            }
        }
    }
}
