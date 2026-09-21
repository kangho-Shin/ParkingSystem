using APSMain.Api.Request;
using APSMain.Api.Response;
using APSMain.DbModels;
using APSMain.Tcpip;
using Dapper;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
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
    public partial class FeeStepForm : Form
    {
        private int _selXindex = 0;

        public FeeStepForm()
        {
            InitializeComponent();

        }

        private void FeeStepForm_FormClosing(object sender, FormClosingEventArgs e)
        {
        }

        private void FeeStepForm_Load(object sender, EventArgs e)
        {
            InitCombo();
            LoadFeeStep();
        }

        private void InitCombo()
        {
            cmbWeekType.Items.Clear();
            cmbWeekType.Items.Add(new ComboItem("주중", 0));
            cmbWeekType.Items.Add(new ComboItem("주말", 1));
            cmbWeekType.SelectedIndex = 0;

            cmbDayNight.Items.Clear();
            cmbDayNight.Items.Add(new ComboItem("주간", 0));
            cmbDayNight.Items.Add(new ComboItem("야간", 1));
            cmbDayNight.SelectedIndex = 0;

            cmbCarType.Items.Clear();
            cmbCarType.Items.Add(new ComboItem("소형", 0));
            cmbCarType.Items.Add(new ComboItem("중형", 1));
            cmbCarType.Items.Add(new ComboItem("대형", 2));
            cmbCarType.SelectedIndex = 0;

            cmbSiteNum.Items.Clear();
            for (int i = 1; i <= 20; i++)
                cmbSiteNum.Items.Add(new ComboItem(i.ToString(), i));
            cmbSiteNum.SelectedIndex = 0;

            cmbGroupNum.Items.Clear();
            for (int i = 1; i <= 20; i++)
                cmbGroupNum.Items.Add(new ComboItem(i.ToString(), i));
            cmbGroupNum.SelectedIndex = 0;

            cmbFeeStep.Items.Clear();
            for (int i = 1; i <= 10; i++)
                cmbFeeStep.Items.Add(new ComboItem(i.ToString(), i));
            cmbFeeStep.SelectedIndex = 0;
        }

        private async void LoadFeeStep()
        {
            EnvItemRequest request = new EnvItemRequest
            {
                Sitenum = (short)APSConfig.Sitenum,
                Groupnum = (short)APSConfig.Groupnum,
                CmdType = "parkfee"
            };
            var content = await RestHelper.Instance.PostAsync<EnvItemRequest>("/api/env/item", request);

            if (content == null) { return; }

            var response = JsonConvert.DeserializeObject<EnvItemResponse>(content);

            APSConfig.FeeRules.Clear();
            APSConfig.FeeRules.AddRange(response?.Tparkfee ?? new List<Tparkfee>());

            List<Tparkfee> feeList = APSConfig.FeeRules;

            lvFeeStep.BeginUpdate();
            lvFeeStep.Items.Clear();

            foreach (Tparkfee fee in feeList) {
                ListViewItem item = new ListViewItem(fee.Sitenum.ToString());
                item.SubItems.Add(fee.Groupnum.ToString());
                item.SubItems.Add(GetWeekTypeName((int)fee.Weektype!));
                item.SubItems.Add(GetDayShiftName((int)fee.Dayshift!));
                item.SubItems.Add(GetCarTypeName((int)fee.Cartype!));
                item.SubItems.Add(fee.Feestep.ToString());
                item.SubItems.Add(fee.Parktime.ToString());
                item.SubItems.Add(fee.Parkfee.ToString());
                item.SubItems.Add(fee.Maxcount.ToString());
                item.SubItems.Add(fee.Regdate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "");
                item.Tag = fee;

                lvFeeStep.Items.Add(item);
            }

            lvFeeStep.EndUpdate();
        }

        private void ClearInput()
        {
            _selXindex = 0;
            txtParkTime.Text = "";
            txtParkFee.Text = "";
        }

        private int GetComboValue(ComboBox cmb)
        {
            if (cmb.SelectedItem is ComboItem item)
                return item.Value;

            return 0;
        }

        private void SetComboValue(ComboBox cmb, int value)
        {
            for (int i = 0; i < cmb.Items.Count; i++) {
                if (cmb.Items[i] is ComboItem item && item.Value == (value)) {
                    cmb.SelectedIndex = i;
                    return;
                }
            }
        }

        private bool GetIntValue(string text, out int value)
        {
            return int.TryParse(text.Trim(), out value);
        }

        private string GetWeekTypeName(int value)
        {
            return value == 1 ? "주중" : "주말";
        }

        private string GetDayShiftName(int value)
        {
            return value == 1 ? "주간" : "야간";
        }

        private string GetCarTypeName(int value)
        {
            return value switch
            {
                1 => "소형",
                2 => "중형",
                3 => "대형",
                _ => value.ToString()
            };
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (!GetIntValue(txtParkTime.Text, out int parkTime)) {
                MessageBox.Show("주차시간 확인");
                txtParkTime.Focus();
                return;
            }

            if (!GetIntValue(txtParkFee.Text, out int parkFee)) {
                MessageBox.Show("주차요금 확인");
                txtParkFee.Focus();
                return;
            }

            if (!GetIntValue(txtMaxCount.Text, out int maxCount)) {
                MessageBox.Show("적용건수 확인");
                txtParkFee.Focus();
                return;
            }

            int siteNum = GetComboValue(cmbSiteNum);
            int groupNum = GetComboValue(cmbGroupNum);
            int weekType = GetComboValue(cmbWeekType) + 1;
            int dayShift = GetComboValue(cmbDayNight) + 1;
            int carType = GetComboValue(cmbCarType) + 1;
            int feeStep = GetComboValue(cmbFeeStep);

            //string chkQuery = @" select xindex from tparkfee where sitenum=@sitenum " +
            //                   " and groupnum= @groupnum " +
            //                   " and weektype= @weektype " +
            //                   " and cartype= @cartype " +
            //                   " and dayshift= @dayshift " +
            //                   " and feestep=@feestep limit 1";

            //int oldXindex = _db.QueryFirstOrDefault<int>(chkQuery, new
            //{
            //    sitenum = siteNum,
            //    groupnum = groupNum,
            //    weektype = weekType,
            //    cartype = carType,
            //    dayshift = dayShift,
            //    feestep = feeStep
            //});

            //if (oldXindex > 0) {
            //    string updQuery = @"update tparkfee set maxcount=@maxcount,parktime=@parktime,parkfee=@parkfee,moddate=now() where xindex=@xindex";
            //    _db.Execute(updQuery, new
            //    {
            //        parktime = parkTime,
            //        parkfee = parkFee,
            //        maxcount = maxCount,
            //        xindex = oldXindex
            //    });
            //}
            //else {
            //    string insQuery = @"insert into tparkfee(sitenum, groupnum, weektype, cartype, dayshift, " +
            //                       " feestep, parktime, parkfee, maxcount, regdate, mid) " +
            //                       " values(@sitenum, @groupnum, @weektype, @cartype, @dayshift, " +
            //                       " @feestep, @parktime, @parkfee, @maxcount, now(), '')";

            //    _db.Execute(insQuery, new
            //    {
            //        sitenum = siteNum,
            //        groupnum = groupNum,
            //        weektype = weekType,
            //        cartype = carType,
            //        dayshift = dayShift,
            //        feestep = feeStep,
            //        parktime = parkTime,
            //        parkfee = parkFee,
            //        maxcount = maxCount
            //    });
            //}

            //LoadFeeStep();
            //ClearInput();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            //if (lvFeeStep.SelectedItems.Count <= 0)
            //    return;

            //if (lvFeeStep.SelectedItems[0].Tag is not Tparkfee fee)
            //    return;

            //DialogResult ret = MessageBox.Show("삭제하시겠습니까?", "확인", MessageBoxButtons.YesNo);
            //if (ret != DialogResult.Yes)
            //    return;


            //LoadFeeStep();
            //ClearInput();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            //string sql = @"SELECT * FROM tparkfee where sitenum=@SiteNum and groupnum=@Groupnum";

            //APSConfig.FeeRules.Clear();
            //APSConfig.FeeRules = (await _db.QueryAsync<Tparkfee>(sql, new { SiteNum = APSConfig.Sitenum, GroupNum = APSConfig.Groupnum })).ToList();

            //foreach (var r in APSConfig.FeeRules.OrderBy(x => x.Feestep)) {
            //    Console.WriteLine($"Feestep:{r.Feestep}, Parktime:{r.Parktime}, Parkfee:{r.Parkfee}, Maxcount:{r.Maxcount}, Weektype:{r.Weektype}, Cartype:{r.Cartype}");
            //}

            Close();
        }

        private void lvFeeStep_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvFeeStep.SelectedItems.Count <= 0)
                return;

            if (lvFeeStep.SelectedItems[0].Tag is not Tparkfee fee)
                return;

            _selXindex = fee.Xindex;

            SetComboValue(cmbSiteNum, (int)fee.Sitenum!);
            SetComboValue(cmbGroupNum, (int)fee.Groupnum!);
            SetComboValue(cmbWeekType, (int)fee.Weektype!-1);
            SetComboValue(cmbDayNight, (int)fee.Dayshift!-1);
            SetComboValue(cmbCarType, (int)fee.Cartype!-1);
            SetComboValue(cmbFeeStep, (int)(fee.Feestep!));

            txtParkTime.Text = fee.Parktime.ToString();
            txtParkFee.Text = fee.Parkfee.ToString();
            txtMaxCount.Text = fee.Maxcount.ToString();
        }

        private void btnAllView_Click(object sender, EventArgs e)
        {
            LoadFeeStep();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            //int siteNum = GetComboValue(cmbSiteNum);
            //int groupNum = GetComboValue(cmbGroupNum);
            //int weekType = GetComboValue(cmbWeekType) + 1;
            //int dayShift = GetComboValue(cmbDayNight) + 1;
            //int carType = GetComboValue(cmbCarType) + 1;

            //string strQuery = @"select xindex, sitenum, groupnum, weektype, dayshift, cartype,feestep, parktime, parkfee,maxcount, regdate " +
            //                    " from Tparkfee where " +
            //                    " sitenum=@sitenum and groupnum=@groupnum and weektype=@weektype and dayshift=@dayshift and cartype=@cartype " +
            //                    " order by feestep";

            //List<Tparkfee> feeList = _db.Query<Tparkfee>(strQuery, new
            //{
            //    sitenum = siteNum,
            //    groupnum = groupNum,
            //    weektype = weekType,
            //    dayshift = dayShift,
            //    cartype = carType
            //}).ToList();

            //lvFeeStep.BeginUpdate();
            //lvFeeStep.Items.Clear();

            //foreach (Tparkfee fee in feeList) {
            //    ListViewItem item = new ListViewItem(fee.Sitenum.ToString());
            //    item.SubItems.Add(fee.Groupnum.ToString());
            //    item.SubItems.Add(GetWeekTypeName((int)fee.Weektype!));
            //    item.SubItems.Add(GetDayShiftName((int)fee.Dayshift!));
            //    item.SubItems.Add(GetCarTypeName((int)fee.Cartype!));
            //    item.SubItems.Add(fee.Feestep.ToString());
            //    item.SubItems.Add(fee.Parktime.ToString());
            //    item.SubItems.Add(fee.Parkfee.ToString());
            //    item.SubItems.Add(fee.Maxcount.ToString());
            //    item.SubItems.Add(fee.Regdate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "");
            //    item.Tag = fee;

            //    lvFeeStep.Items.Add(item);
            //}

            //lvFeeStep.EndUpdate();
        }
    }

    public class ComboItem
    {
        public string Text { get; set; }
        public int Value { get; set; }

        public ComboItem(string text, int value)
        {
            Text = text;
            Value = value;
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
