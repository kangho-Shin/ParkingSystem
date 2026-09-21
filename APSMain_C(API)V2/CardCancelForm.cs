using APSMain.BaseClass;
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


namespace APSMain
{
    public partial class CardCancelForm : Form
    {
        public SMPAYDATA _smPayData = new SMPAYDATA();
        private IMainForm? _mainForm = null;
        private CardTransInfo? _cdinfo = null;
        public ClsLog? XLogClass;

        public CardCancelForm()
        {
            InitializeComponent();
        }

        private void CardCancelForm_Load(object sender, EventArgs e)
        {
            _mainForm = APSConfig.ScreenMode == 15 ? APSConfig.mainForm15 : APSConfig.mainForm;
            XLogClass = ClsLog.Instance;

            if (APSConfig.VANTYPE == (int)EnumVanType.SMATRO) {
                if (_mainForm != null) {
                    _mainForm.CardCancleEvent += CardCancleEvent;
                }
            }
            else if (APSConfig.VANTYPE == (int)EnumVanType.KICC) {
            }
        }

        private int ToInt(string value)
        {
            return int.TryParse(value, out int ret) ? ret : 0;
        }

        public void CardCancleEvent(int msgType, SmartroPacket packet, SmartroApprovalResponse? res)
        {
            if (msgType == 0 && res != null) {
                _cdinfo = new CardTransInfo();

                XLogClass?.SaveLogString("CAL", $"BODY : {Encoding.GetEncoding("ks_c_5601").GetString(packet.Body)}");

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

                lbLog.Items.Add($"거래구분코드  : {_cdinfo.AcceptType}");
                lbLog.Items.Add($"거래매체      : {_cdinfo.DealType}");
                lbLog.Items.Add($"카드번호      : {_cdinfo.CardId}");
                lbLog.Items.Add($"승인금액      : {_cdinfo.Money}");
                lbLog.Items.Add($"세금/잔여횟수 : {_cdinfo.Tax}");
                lbLog.Items.Add($"봉사료/사용횟수 : {_cdinfo.Service}");
                lbLog.Items.Add($"할부개월      : {_cdinfo.Installment}");
                lbLog.Items.Add($"승인번호      : {_cdinfo.AcceptNum}");
                lbLog.Items.Add($"매출일자      : {_cdinfo.DealDate}");
                lbLog.Items.Add($"매출시간      : {_cdinfo.DealTime}");
                lbLog.Items.Add($"거래고유번호  : {_cdinfo.DealNum}");
                lbLog.Items.Add($"가맹점번호    : {_cdinfo.PosId}");
                lbLog.Items.Add($"단말기번호    : {_cdinfo.SamId}");
                lbLog.Items.Add($"발급사        : {_cdinfo.CardName}");
                lbLog.Items.Add($"매입사        : {_cdinfo.BranchNum}");
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (APSConfig.VANTYPE == (int)EnumVanType.SMATRO) {
                _smPayData.nMoney = int.Parse(txtMoney.Text);
                _smPayData.cancleCode = 0;
                _smPayData.nTax = 0;
                _smPayData.nService = 0;
                _smPayData.halbu = 0;
                _smPayData.approVal = txtAcceptNum.Text;

                _smPayData.dealDate = txtDateTime.Text.Substring(0, 8);
                _smPayData.dealNum = txtDateTime.Text.Substring(8, 6);
                if (_mainForm != null && _smPayData.nMoney > 0) {
                    _mainForm.MakePacketData(Constants.CMD_TX_PAYCANCEL, _smPayData);
                }
            }
            else if (APSConfig.VANTYPE == (int)EnumVanType.KICC) {
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            if (APSConfig.VANTYPE == (int)EnumVanType.SMATRO) {
                if (_mainForm != null) {
                    _mainForm.CardCancleEvent -= CardCancleEvent;
                }
            }
            else {
            }
            this.Close();
        }
    }
}
