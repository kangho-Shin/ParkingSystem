using JPXLpr.BaseClass;
using JPXLpr.HelpClass;
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
    public partial class ExpForm : Form
    {
        public JPXLpr _mainForm;
        public ExpSchedule[] _expConfig;
        private int _camIndex = 0;
        private bool _isDirty = false;
        private string _expFileName = "ExpSchedule.json";

        public ExpForm(JPXLpr main, ExpSchedule[] ExpConfig)
        {
            InitializeComponent();

            _mainForm = main;
            _expConfig = ExpConfig;
        }

        private void ExpForm_Load(object sender, EventArgs e)
        {
            InitExpListView();
            ShowExpListView(_expConfig[_camIndex]);
        }

        private void InitExpListView()
        {
            lvExpose.Clear();
            lvExpose.View = View.Details;
            lvExpose.FullRowSelect = true;
            lvExpose.GridLines = true;
            lvExpose.MultiSelect = false;

            lvExpose.Columns.Add("월", 60);
            lvExpose.Columns.Add("주간시작", 90);
            lvExpose.Columns.Add("주간종료", 90);
            lvExpose.Columns.Add("주간Min", 80);
            lvExpose.Columns.Add("주간Max", 80);
            lvExpose.Columns.Add("야간Min", 80);
            lvExpose.Columns.Add("야간Max", 80);
        }

        private void ShowExpListView(ExpSchedule schedule)
        {
            lvExpose.Items.Clear();

            for (int mon = 1; mon <= 12; mon++) {
                ExpInfo info = schedule.ExpVal[mon];

                ListViewItem item = new ListViewItem(mon.ToString() + "월");

                item.SubItems.Add(info.SHour.ToString("00") + ":" + info.SMin.ToString("00"));
                item.SubItems.Add(info.EHour.ToString("00") + ":" + info.EMin.ToString("00"));
                item.SubItems.Add(info.ExpDayMin.ToString());
                item.SubItems.Add(info.ExpDayMax.ToString());
                item.SubItems.Add(info.ExpNightMin.ToString());
                item.SubItems.Add(info.ExpNightMax.ToString());

                item.Tag = mon;

                lvExpose.Items.Add(item);
            }
        }

        private void UpdateExpListViewRow(int mon, ExpInfo info)
        {
            if (mon < 1 || mon > 12) return;

            ListViewItem item = lvExpose.Items[mon - 1];

            item.SubItems[1].Text = info.SHour.ToString("00") + ":" + info.SMin.ToString("00");
            item.SubItems[2].Text = info.EHour.ToString("00") + ":" + info.EMin.ToString("00");
            item.SubItems[3].Text = info.ExpDayMin.ToString();
            item.SubItems[4].Text = info.ExpDayMax.ToString();
            item.SubItems[5].Text = info.ExpNightMin.ToString();
            item.SubItems[6].Text = info.ExpNightMax.ToString();
        }

        private void cbCamera_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbCamera.SelectedIndex >= 0 && cbCamera.SelectedIndex < _expConfig.Length) {
                _camIndex = cbCamera.SelectedIndex;
                ShowExpListView(_expConfig[_camIndex]);
            }
        }

        private void lvExpose_DoubleClick(object sender, EventArgs e)
        {
            if (lvExpose.SelectedItems.Count == 0)
                return;

            ListViewItem item = lvExpose.SelectedItems[0];

            cbMonth.Text = item.SubItems[0].Text;

            txtDayStart.Text = item.SubItems[1].Text;
            txtDayEnd.Text = item.SubItems[2].Text;

            txtDayMin.Text = item.SubItems[3].Text;
            txtDayMax.Text = item.SubItems[4].Text;

            txtNightMin.Text = item.SubItems[5].Text;
            txtNightMax.Text = item.SubItems[6].Text;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (chkAll.Checked) {
                ApplyAllExposure(
                    int.Parse(txtDayMin.Text),
                    int.Parse(txtDayMax.Text),
                    int.Parse(txtNightMin.Text),
                    int.Parse(txtNightMax.Text)
                );
                return;
            }
            int cam = cbCamera.SelectedIndex;
            int mon = cbMonth.SelectedIndex + 1;

            if (cam < 0 || mon < 1 || mon > 12)
                return;

            ExpInfo info = _expConfig[cam].ExpVal[mon];

            if (!SetTime(txtDayStart.Text, out int sHour, out int sMin))
                return;

            if (!SetTime(txtDayEnd.Text, out int eHour, out int eMin))
                return;

            info.SHour = sHour;
            info.SMin = sMin;
            info.STick = sHour * 60 + sMin;

            info.EHour = eHour;
            info.EMin = eMin;
            info.ETick = eHour * 60 + eMin;

            info.ExpDayMin = int.Parse(txtDayMin.Text);
            info.ExpDayMax = int.Parse(txtDayMax.Text);
            info.ExpNightMin = int.Parse(txtNightMin.Text);
            info.ExpNightMax = int.Parse(txtNightMax.Text);

            UpdateExpListViewRow(mon, info);

            _isDirty = true;
        }

        private bool SetTime(string text, out int hour, out int min)
        {
            hour = 0;
            min = 0;

            string[] arr = text.Split(':');

            if (arr.Length != 2)
                return false;

            if (!int.TryParse(arr[0], out hour))
                return false;

            if (!int.TryParse(arr[1], out min))
                return false;

            if (hour < 0 || hour > 23)
                return false;

            if (min < 0 || min > 59)
                return false;

            return true;
        }

        private void ExpForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isDirty) {
                ExpScheduleFile.Save(_expFileName, _expConfig);
                _isDirty = false;
            }
        }

        private void ApplyAllExposure(int dayMin, int dayMax, int nightMin, int nightMax)
        {
            for (int cam = 0; cam < _expConfig.Length; cam++) {
                for (int mon = 1; mon <= 12; mon++) {
                    _expConfig[cam].ExpVal[mon].ExpDayMin = dayMin;
                    _expConfig[cam].ExpVal[mon].ExpDayMax = dayMax;
                    _expConfig[cam].ExpVal[mon].ExpNightMin = nightMin;
                    _expConfig[cam].ExpVal[mon].ExpNightMax = nightMax;
                }
            }

            int curCam = cbCamera.SelectedIndex;

            if (curCam >= 0)
                ShowExpListView(_expConfig[curCam]);

            _isDirty = true;
        }
    }
}
