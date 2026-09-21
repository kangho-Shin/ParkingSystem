using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace APSMain
{
    public partial class DiscountSelectForm : Form
    {
        private int _disKey = 0;

        public int DisKey => _disKey;

        public DiscountSelectForm()
        {
            InitializeComponent();
        }

        private void DiscountSelectForm_Load(object sender, EventArgs e)
        {
            cmbSelect.DisplayMember = "Value";
            cmbSelect.ValueMember = "Key";

            foreach (var item in APSConfig.Discounts) {
                cmbSelect.Items.Add(new KeyValuePair<int, string>(item.Salecode, item.Saletitle!));
            }
        }

        private void btnSelect_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;

            Close();
        }

        private void cmbSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbSelect.SelectedIndex != -1) {
                _disKey = (int)((KeyValuePair<int, string>)cmbSelect.SelectedItem!).Key;

                _disKey = _disKey % 100;
            }
        }

        private void btnCredit_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;

            _disKey = 101;
            Close();
        }
    }
}
