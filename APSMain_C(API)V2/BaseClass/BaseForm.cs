using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.BaseClass
{
    public class BaseForm : Form
    {
        public BaseForm()
        {
            this.KeyPreview = true;
            this.KeyDown += BaseForm_KeyDown;
            this.KeyPress += BaseForm_KeyPress;
        }

        protected virtual void BaseForm_KeyDown(object? s, KeyEventArgs e) { /* 위 KeyDown 로직 */ }

        private void InitializeComponent()
        {

        }

        protected virtual void BaseForm_KeyPress(object? s, KeyPressEventArgs e) { /* 위 KeyPress 로직 */ }
    }
}
