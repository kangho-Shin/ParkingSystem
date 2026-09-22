using APSMain.BaseClass;
using APSMain.Comm;
using APSMain.Models;
using APSMain.Smatro;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace APSMain
{
    public partial class ParkInTimeFrm : Form
    {
        private IMainForm? _mainForm = null;
        private ParkFeeCalculator? parkcal;

        private CardTransInfo _cdinfo = new CardTransInfo();
        public SMPAYDATA _smPayData = new SMPAYDATA();

        private KiccCredit? _kiccCredit;
        public TicketReader? _ticketReader;

        public int _ParkingTime = 0;
        public int _totalFee = 0;

        public ParkInTimeFrm()
        {
            InitializeComponent();

            _mainForm = APSConfig.ScreenMode == 15 ? APSConfig.mainForm15 : APSConfig.mainForm;
        }

        private async void ParkInTime_Load(object sender, EventArgs e)
        {
            var defaultItem = new Tdiscounttable
            {
                Salecode = 0,
                Saletitle = "선택 안함",
            };

            // 할인 리스트 만들기
            var disDataList = new List<Tdiscounttable> { defaultItem };
            disDataList.AddRange(APSConfig.Discounts);

            // ComboBox 바인딩
            cmbDisList.DataSource = disDataList;
            cmbDisList.DisplayMember = "Saletitle";   // 보여줄 값
            cmbDisList.ValueMember = "Salecode";       // 넘겨줄 값
            cmbDisList.SelectedIndex = 0;         // 기본 선택은 "선택 안함"
            txtPrepay.Text = "0";
            parkcal = new ParkFeeCalculator();

            if (APSConfig.VANTYPE == (int)EnumVanType.SMATRO) {
                _mainForm!.CardCalculateEvent += CardCalculateEvent;
                await Task.Delay(100);
                _mainForm!.SmatroWaiting();

                _cdinfo = new CardTransInfo();
            }
            else if (APSConfig.VANTYPE == (int)EnumVanType.KICC) {
                await _kiccCredit!.ResetAndWaitRemoveAsync().ConfigureAwait(true);
            }
        }

        public void CardCalculateEvent(int msgType, SmartroPacket packet, SmartroApprovalResponse? res)
        {
            if (msgType == 0 && res != null) {
                lbLog.Items.Add($"{Encoding.GetEncoding("ks_c_5601").GetString(packet.Body)}");

                _cdinfo.AcceptType = ToInt(res.TradeType);
                _cdinfo.DealType = ToInt(res.MediaType);
                _cdinfo.ResCode = "00";
                _cdinfo.CardId = res.CardNo;

                _cdinfo.Money = res.Amount;
                _cdinfo.Tax = res.Tax;
                _cdinfo.Service = res.ServiceCharge;
                _cdinfo.Installment = ToInt(res.Installment);

                _cdinfo.AcceptNum = res.ApprovalNo;
                _cdinfo.DealDate = res.SaleDate;
                _cdinfo.DealTime = res.SaleTime;
                _cdinfo.DealNum = res.TradeNo;
                _cdinfo.PosId = res.MerchantNo;
                _cdinfo.SamId = res.TerminalNo;
                _cdinfo.CardName = res.IssuerInfo;
                _cdinfo.BranchNum = res.AcquirerInfo;

                lbLog.Items.Add("거래구분코드     : {_cdinfo.AcceptType}");
                lbLog.Items.Add($"거래매체        : {_cdinfo.DealType}");
                lbLog.Items.Add($"카드번호        : {_cdinfo.CardId}");
                lbLog.Items.Add($"승인금액        : {_cdinfo.Money}");
                lbLog.Items.Add($"세금/잔여횟수   : {_cdinfo.Tax}");
                lbLog.Items.Add($"봉사료/사용횟수 : {_cdinfo.Service}");
                lbLog.Items.Add($"할부개월        : {_cdinfo.Installment}");
                lbLog.Items.Add($"승인번호        : {_cdinfo.AcceptNum}");
                lbLog.Items.Add($"매출일자        : {_cdinfo.DealDate}");
                lbLog.Items.Add($"매출시간        : {_cdinfo.DealTime}");
                lbLog.Items.Add($"거래고유번호    : {_cdinfo.DealNum}");
                lbLog.Items.Add($"가맹점번호      : {_cdinfo.PosId}");
                lbLog.Items.Add($"단말기번호      : {_cdinfo.SamId}");
                lbLog.Items.Add($"발급사          : {_cdinfo.CardName}");
                lbLog.Items.Add($"매입사          : {_cdinfo.BranchNum}");
            }
            else if (msgType == 1) {
                string errMsg = "";

                if (res != null) {
                    errMsg = res.IssuerInfo + " " + res.AcquirerInfo;
                }

                lbLog.Items.Add($"승인 실패: {errMsg}");
                UiHelpers.ShowPayMsg(this, 4, true);
            }
            else if (msgType == 2) {
                UiHelpers.ShowPayMsg(this, 1, false);
            }
            else if (msgType == 5) {
                UiHelpers.ShowPayMsg(this, 5, true);
            }
        }

        private async void ParkInTimeFrm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_mainForm != null) {
                if (APSConfig.VANTYPE == (int)EnumVanType.SMATRO) {
                    _mainForm.CardCalculateEvent -= CardCalculateEvent;

                    _mainForm!.SmatroWaiting();
                }
                else if (APSConfig.VANTYPE == (int)EnumVanType.KICC) {
                    if (_kiccCredit != null) {
                        await _kiccCredit!.ResetAndWaitRemoveAsync().ConfigureAwait(true);
                        _kiccCredit.PaymentCompleted -= OnKiccPaymentCompleted;
                        _kiccCredit.Dispose();
                        _kiccCredit = null;
                    }
                }
                _mainForm.lelMessageChange(0);
            }
        }

        private void btnCalculate_Click(object sender, EventArgs e)
        {
            int carType = 1;

            try {
                DateTime inDate = dtpInDate.Value.Date + dtpInTime.Value.TimeOfDay;
                DateTime outTime = DateTime.Now;

                parkcal?.InitCalculator(inDate, outTime, carType, int.Parse(txtPrepay.Text));

                _totalFee = parkcal?.CalculateTotalFee(inDate, outTime) ?? 0;
                txtPay.Text = _totalFee.ToString();
                _ParkingTime = parkcal?.totalTimeMinute ?? 0;
                lbLog.Items.Add($"입차시간 : {inDate}");
                lbLog.Items.Add($"출차시간 : {outTime}");
                lbLog.Items.Add($"주차시간 : {parkcal?.totalTimeMinute} 분");
                lbLog.Items.Add($"할인시간 : {parkcal?.disTimeMinute} 분");
                lbLog.Items.Add($"주차요금 : {parkcal?.totalFee} 원");
                lbLog.Items.Add($"할인요금 : {parkcal?.totalDiscountFee} 원");
                lbLog.Items.Add($"정산요금 : {parkcal?.totalRemainFee} 원");
                if (parkcal?.ptList != null && parkcal.ptList.Count > 0) {
                    foreach (ParkingTime pt in parkcal.ptList) {
                        lbLog.Items.Add($"{pt.Date.ToString("yyyy-MM-dd")} {pt.dayMinutes} - {pt.nightMinutes} {pt.Fee}원");
                    }
                }
                lbLog.Items.Add($"============================================");
            }
            catch (Exception ex) {
                lbLog.Items.Add($"오류: {ex.Message}");
            }
        }

        private void btnDisAdd_Click(object sender, EventArgs e)
        {
            if (parkcal != null) {
                if (cmbDisList.SelectedIndex > 0 && cmbDisList.SelectedValue is int selectedKey) {
                    parkcal.AddDiskey(selectedKey, 1, 1);

                    lbLog.Items.Add($"할인적용: {cmbDisList.Text}");
                }
            }
        }

        private int ToInt(string value)
        {
            return int.TryParse(value, out int ret) ? ret : 0;
        }

        private void InitKicc(int amount, int vanTimeoutMs)
        {
            _kiccCredit?.Dispose();
            _kiccCredit = null;

            _kiccCredit = new KiccCredit(0);

            _kiccCredit.CancelHide = () =>
            {
            };

            _kiccCredit.PaymentCompleted += OnKiccPaymentCompleted;

            _kiccCredit!.BeginPayment(_totalFee, vanTimeoutMs);
        }

        private void OnKiccPaymentCompleted(KiccPaymentResult result)
        {
            if (result.IsSuccess) {
                if (result.ResCode == "0000") {
                    DateTime dt = DateTime.ParseExact(result.TradeDateTime, "yyyyMMddHHmmss", null);

                    _cdinfo.Sitenum = (short)APSConfig.Sitenum;
                    _cdinfo.Groupnum = (short)APSConfig.Groupnum;
                    _cdinfo.TicketData = "1234567890";
                    _cdinfo.OutDeviceNum = (short)APSConfig.APSNUM;
                    _cdinfo.ResCode = result.ResCode;
                    _cdinfo.TermId = result.TermId;
                    _cdinfo.PosId = result.PosId;
                    _cdinfo.CardId = result.CardId;
                    _cdinfo.CardName = result.CardName;
                    _cdinfo.AcceptNum = result.ApprovalNo;
                    _cdinfo.DealDate = dt.ToString("yyyy-MM-dd");
                    _cdinfo.DealTime = dt.ToString("HHmmss");
                    _cdinfo.ReceiptNum = "1001";
                    _cdinfo.BranchNum = result.BranchNo;
                    _cdinfo.AcceptType = 0;
                    _cdinfo.Money = result.Price;
                    _cdinfo.ParkTime = _ParkingTime;
                    _cdinfo.DendFlag = 0;
                    _cdinfo.TendFlag = 0;
                    _cdinfo.EndDate = "1970-01-01";
                    _cdinfo.DealNum = "테스트";

                    txtPay.Text = _cdinfo.Money.ToString();
                    txtAcceptnum.Text = _cdinfo.AcceptNum;

                    lbLog.Items.Add($"승인성공: {result.Amount}-{result.Price}, 승인번호={result.ApprovalNo}");
                    UiHelpers.ShowPayMsg(this, 0, true);
                }
                else {
                    _kiccCredit!.BeginPayment(_totalFee, APSConfig.KICCCANCELTIME);
                    lbLog.Items.Add($"승인실패0: {result.ResCode}-{result.Message}");
                }
            }
            else {
                lbLog.Items.Add($"승인실패1: {result.ResCode}-{result.Message}");
            }
        }

        private void btnCardApproval_Click(object sender, EventArgs e)
        {
            if (APSConfig.VANTYPE == (int)EnumVanType.SMATRO) {
                _smPayData.Init();
                _smPayData.carNum = "테스트차량";
                _smPayData.prodMsg = "일반차량";
                _smPayData.nMoney = _totalFee;
                if (_mainForm != null && _smPayData.nMoney > 0) {
                    _mainForm.MakePacketData(Constants.CMD_TX_ADD_INFO, _smPayData);
                }
            }
            else if (APSConfig.VANTYPE == (int)EnumVanType.KICC) {
                InitKicc(_totalFee, 30);
            }
        }

        private void btnCardCancellation_Click(object sender, EventArgs e)
        {
            if (APSConfig.VANTYPE == (int)EnumVanType.SMATRO) {
                _smPayData.Init();
                _smPayData.approVal = txtAcceptnum.Text;
                _smPayData.nMoney = ToInt(txtPay.Text);
                _smPayData.dealDate = "";
                _smPayData.dealNum = "";

                if (_mainForm != null && _smPayData.nMoney > 0) {
                    _mainForm.MakePacketData(Constants.CMD_TX_PAYCANCEL, _smPayData);
                }
            }
            else if (APSConfig.VANTYPE == (int)EnumVanType.KICC) {
            }
        }
    }
}
