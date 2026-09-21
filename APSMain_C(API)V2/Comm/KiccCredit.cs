using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace APSMain.Comm
{

    public class KiccPaymentResult
    {
        public bool   IsSuccess { get; set; }
        public bool   NeedRetry { get; set; }
        public int    Amount { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ApprovalNo { get; set; } = string.Empty;
        public string ResCode { get; set; } = string.Empty;
        public string TradeNo { get; set; } = string.Empty;
        public string BranchNo { get; set; } = string.Empty;
        public string TradeDateTime { get; set; } = string.Empty;
        public string TermId { get; set; } = string.Empty;
        public string PosId { get; set; } = string.Empty;
        public string CardId { get; set; } = string.Empty;
        public string CardName { get; set; } = string.Empty;
        public string IssuerName { get; set; } = string.Empty;
        public string ValidDate { get; set; } = string.Empty;
        public int Price { get; set; } = 0;
        public string RawResponse { get; set; } = string.Empty;
        public bool IsTimeout { get; set; } = false;
    }

    public class KiccResponse
    {
        public string? SUC { get; set; }
        public string? MSG { get; set; }
        public string? RQ01 { get; set; }       // 전문구분 'D1':승인 'D4':당일취소 'B1':현금승인  'B2':현금취소
        public string? RQ02 { get; set; }
        public string? RQ03 { get; set; }       // 카드입력구분 승인('A':Swipe   '@':KeyIn)   취소('Q':Swipe   'P':KeyIn)
        public string? RQ04 { get; set; }       // 카드번호
        public string? RQ05 { get; set; }
        public string? RQ06 { get; set; }
        public string? RQ07 { get; set; }
        public string? RQ08 { get; set; }
        public string? RQ09 { get; set; }
        public string? RQ10 { get; set; }       // 원승인번호(취소승인시)
        public string? RQ11 { get; set; }       // 원승인일자(취소승인시)
        public string? RQ12 { get; set; }
        public string? RQ13 { get; set; }
        public string? RQ14 { get; set; }
        public string? RQ15 { get; set; }
        public string? RQ16 { get; set; }

        public string? RS01 { get; set; }
        public string? RS02 { get; set; }
        public string? RS03 { get; set; }
        public string? RS04 { get; set; }       // 0000 : 성공
        public string? RS05 { get; set; }
        public string? RS06 { get; set; }
        public string? RS07 { get; set; }       // 승인일시 YYMMDDhhmmssN
        public string? RS08 { get; set; }
        public string? RS09 { get; set; }
        public string? RS10 { get; set; }
        public string? RS11 { get; set; }       // 'C' 체크카드    'N' 일반카드
        public string? RS12 { get; set; }
        public string? RS13 { get; set; }
        public string? RS14 { get; set; }
        public string? RS15 { get; set; }
        public string? RS16 { get; set; }
        public string? RS17 { get; set; }
        public string? RS18 { get; set; }       // 'Y' 전자서명     'N' 일반
        public string? RS19 { get; set; }
        public string? RS20 { get; set; }
    }

    public sealed class KiccCredit : IDisposable
    {
        public Action? CancelHide;
        public Action<KiccPaymentResult>? PaymentCompleted;

        public byte m_VanFlag;

        private const byte VanIdle = 0x00;
        private const byte VanWaitInsert = 0x22;
        private const byte VanProcessing = 0x88;
        private const byte VanWaitRemove = 0x99;

        private readonly int _vanPort;
        private readonly int _baseIntervalMs;
        private readonly int _waitIntervalMs;
        private readonly int _csTimeoutMs;

        private readonly HttpClient _httpClient;
        private readonly System.Windows.Forms.Timer _cardTimer;

        private volatile bool _autoEnabled;
        private int _busy;
        private bool _waitOnce;

        private int _payAmount;
        private int _payTimeoutMs;
        private string _lastResponse = string.Empty;

        private bool _disposed;
        private CancellationTokenSource? _disposeCts;
        public KiccResponse? _kiccresp;

        public string LastSUC { get; private set; } = string.Empty;
        public string LastMSG { get; private set; } = string.Empty;

        public bool IsTimeout { get; set; }

        public KiccCredit(int vanPort, int baseIntervalMs = 500, int waitIntervalMs = 1500, int csTimeoutMs = 1500)
        {
            _vanPort = vanPort;
            _baseIntervalMs = baseIntervalMs;
            _waitIntervalMs = waitIntervalMs;
            _csTimeoutMs = csTimeoutMs;

            _httpClient = new HttpClient();
            _disposeCts = new CancellationTokenSource();

            _cardTimer = new System.Windows.Forms.Timer();
            _cardTimer.Interval = _baseIntervalMs;
            _cardTimer.Tick += CardTimer_Tick;
        }

        public void BeginPayment(int amount, int vanTimeoutMs)
        {
            if (_disposed)
                return;

            _payAmount = amount;
            _payTimeoutMs = vanTimeoutMs;
            m_VanFlag = VanWaitInsert;
            StartAuto();
            Console.WriteLine($"KICC READY amount={_payAmount}, timeout={_payTimeoutMs}");
        }

        public void ResetPayment(int amount, int vanTimeoutMs)
        {
            if (_disposed)
                return;

            _payAmount = amount;
            _payTimeoutMs = vanTimeoutMs;
            Console.WriteLine($"KICC READY amount={_payAmount}, timeout={_payTimeoutMs}");
        }

        public void StopPayment()
        {
            StopAuto();
            _payAmount = 0;
            _payTimeoutMs = 0;
            m_VanFlag = VanIdle;
        }

        public void StartAuto()
        {
            if (_disposed)
                return;

            _autoEnabled = true;
            _waitOnce = false;
            _cardTimer.Interval = _baseIntervalMs;
            _cardTimer.Start();
        }

        public void StopAuto()
        {
            _autoEnabled = false;
            _cardTimer.Stop();
        }

        private async void CardTimer_Tick(object? sender, EventArgs e)
        {
            if (_disposed || !_autoEnabled)
                return;

            if (Interlocked.Exchange(ref _busy, 1) == 1)
                return;

            try {
                if (_disposed || !_autoEnabled)
                    return;

                if (_waitOnce) {
                    _waitOnce = false;
                    _cardTimer.Interval = _baseIntervalMs;
                }

                string csResp = await SendReaderCommandAsync("CS", _csTimeoutMs).ConfigureAwait(true);

                if (_disposed || !_autoEnabled)
                    return;

                KiccCommandParsing(csResp);

                bool isCardInserted = IsCardInsertedByCS();
                bool isCardRemoved = IsCardRemovedByCS();

                if (m_VanFlag == VanWaitInsert) {
                    if (isCardInserted) {
                        m_VanFlag = VanProcessing;
                        CancelHide?.Invoke();
                        Console.WriteLine("CARD INSERT");
                        KiccPaymentResult result = await SendPaymentRequestAsync(_payAmount, _payTimeoutMs).ConfigureAwait(true);

                        if (_disposed)
                            return;

                        PaymentCompleted?.Invoke(result);

                        if (_disposed)
                            return;

                        if (result.IsSuccess) {
                            StopPayment();

                            await EjectCardAndWaitRemoveAsync().ConfigureAwait(true);
                        }
                        else {
                            await DelaySafeAsync(500).ConfigureAwait(true);

                            if (_disposed)
                                return;

                            await EjectCardAndWaitRemoveAsync().ConfigureAwait(true);
                        }
                    }
                    else {
                        SetWaitOnce();
                    }
                }
                else if (m_VanFlag == VanWaitRemove) {
                    if (isCardRemoved) {
                        m_VanFlag = VanWaitInsert;
                        Console.WriteLine("CARD REMOVED - WAIT INSERT");
                        SetWaitOnce();
                    }
                }
            }
            catch (OperationCanceledException) {
            }
            catch (ObjectDisposedException) {
            }
            catch (Exception ex) {
                if (_disposed)
                    return;

                Console.WriteLine($"KICC ERROR: {ex.Message}");

                if (m_VanFlag == VanProcessing) {
                    var result = new KiccPaymentResult
                    {
                        IsSuccess = false,
                        NeedRetry = true,
                        Amount = _payAmount,
                        Message = ex.Message,
                        RawResponse = _lastResponse
                    };

                    PaymentCompleted?.Invoke(result);

                    if (_disposed)
                        return;

                    try {
                        await EjectCardAndWaitRemoveAsync().ConfigureAwait(true);
                    }
                    catch {
                    }
                }
            }
            finally {
                Interlocked.Exchange(ref _busy, 0);
            }
        }

        private void SetWaitOnce()
        {
            if (_disposed)
                return;

            _waitOnce = true;
            _cardTimer.Interval = _waitIntervalMs;
        }

        private bool IsCardInsertedByCS()
        {
            if (LastSUC == "00")
                return true;

            if (!string.IsNullOrEmpty(LastMSG) &&
                LastMSG.IndexOf("Pluged in", StringComparison.OrdinalIgnoreCase) >= 0 &&
                LastMSG.IndexOf("not", StringComparison.OrdinalIgnoreCase) < 0)
                return true;

            return false;
        }

        private bool IsCardRemovedByCS()
        {
            if (LastSUC == "01" &&
                LastMSG.IndexOf("not Pluged in", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return false;
        }

        private async Task EjectCardAndWaitRemoveAsync()
        {
            if (_disposed)
                return;

            Console.WriteLine($"EJ - {_csTimeoutMs}");

            try {
                string ejResp = await SendReaderCommandAsync("EJ", _csTimeoutMs).ConfigureAwait(true);

                if (_disposed)
                    return;

                KiccCommandParsing(ejResp);
            }
            catch (OperationCanceledException) {
                return;
            }
            catch (ObjectDisposedException) {
                return;
            }
            catch (Exception ex) {
                if (_disposed)
                    return;

                Console.WriteLine($"EJECT ERROR: {ex.Message}");
            }

            if (_disposed)
                return;

            m_VanFlag = VanWaitRemove;
            Console.WriteLine("WAIT CARD REMOVE");
            SetWaitOnce();
        }

        public async Task ResetAndWaitRemoveAsync()
        {
            if (_disposed)
                return;

            StopPayment();
            Console.WriteLine($"CC - {_csTimeoutMs}");

            try {
                string ejResp = await SendReaderCommandAsync("CC", _csTimeoutMs).ConfigureAwait(true);

                if (_disposed)
                    return;

                KiccCommandParsing(ejResp);
            }
            catch (OperationCanceledException) {
                return;
            }
            catch (ObjectDisposedException) {
                return;
            }
            catch (Exception ex) {
                if (_disposed)
                    return;

                Console.WriteLine($"RESET ERROR: {ex.Message}");
            }

            if (_disposed)
                return;

            m_VanFlag = VanWaitRemove;
            Console.WriteLine("WAIT CARD REMOVE");
            SetWaitOnce();
        }


        private async Task<KiccPaymentResult> SendPaymentRequestAsync(int amount, int timeoutMs)
        {
            IsTimeout = false;

            string req = $"D1^^{amount}^00^^^^1234567890^WEB1234567890^^^{timeoutMs}";
            string resp = await SendReaderCommandAsync(req, timeoutMs).ConfigureAwait(true);

            if (_disposed)
                throw new OperationCanceledException();

            _kiccresp = KiccCommandParsing(resp);

            var result = new KiccPaymentResult
            {
                Amount = amount,
                RawResponse = resp
            };

            if ( LastSUC == "00" && _kiccresp != null ) {
                result.IsSuccess = true;
                result.Message       = _kiccresp?.RS16! + _kiccresp?.RS17!;
                result.TermId        = _kiccresp?.RQ02!;
                result.ResCode       = _kiccresp?.RS04!;
                result.BranchNo      = _kiccresp?.RS05!;
                result.CardId        = _kiccresp?.RQ04!;
                result.ApprovalNo    = _kiccresp?.RS09!;
                result.TradeNo       = _kiccresp?.RS03!;
                result.ValidDate     = _kiccresp?.RQ05!;
                result.Price         = Convert.ToInt32(_kiccresp?.RQ07! ?? "0");
                result.TradeDateTime = _kiccresp?.RS07!;
                result.CardName      = _kiccresp?.RS12!;
                result.IssuerName    = _kiccresp?.RS14!;

                var sb = new StringBuilder();

                sb.AppendLine($"IsSuccess       : {result.IsSuccess}");
                sb.AppendLine($"Message         : {result.Message}");
                sb.AppendLine($"TermId          : {result.TermId}");
                sb.AppendLine($"ResCode         : {result.ResCode}");
                sb.AppendLine($"BranchNo        : {result.BranchNo}");
                sb.AppendLine($"CardId          : {result.CardId}");
                sb.AppendLine($"ApprovalNo      : {result.ApprovalNo}");
                sb.AppendLine($"TradeNo         : {result.TradeNo}");
                sb.AppendLine($"ValidDate       : {result.ValidDate}");
                sb.AppendLine($"Price           : {result.Amount}-{result.Price}");
                sb.AppendLine($"TradeDateTime   : {result.TradeDateTime}");
                sb.AppendLine($"CardName        : {result.CardName}");
                sb.AppendLine($"IssuerName      : {result.IssuerName}");

                Console.WriteLine(sb.ToString());
            }
            else {
                result.IsSuccess = false;
                result.NeedRetry = true;
                result.Message = _kiccresp?.RS17! ?? "결제 실패";
            }

            return result;
        }

        private Task<string> SendReaderCommandAsync(string cmd, int timeoutMs)
        {
            if (_disposed)
                return Task.FromCanceled<string>(new CancellationToken(true));

            string url = _vanPort <= 0 ? $"http://127.0.0.1/?callback=jsonp12345678983543344&REQ={cmd}"
                                       : $"http://127.0.0.1:{_vanPort}/?callback=jsonp12345678983543344&REQ={cmd}";

            return GetStringAsync(url, timeoutMs);
        }
       
        private async Task<string> GetStringAsync(string url, int timeoutSec)
        {
            if (_disposed || _disposeCts == null)
                throw new OperationCanceledException();

            try {
                using var timeoutCts = new CancellationTokenSource(timeoutSec * 1000);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, _disposeCts.Token);

                var response = await _httpClient.GetAsync(url, linkedCts.Token).ConfigureAwait(false);
                var bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

                // EUC-KR (KS5601)
                string resp = Encoding.GetEncoding(949).GetString(bytes);

                if (_disposed)
                    throw new OperationCanceledException();

                _lastResponse = resp ?? string.Empty;
                Console.WriteLine(_lastResponse);
                return _lastResponse;
            }
            catch (OperationCanceledException) {
                _lastResponse = string.Empty;

                if (_disposed || _disposeCts != null && _disposeCts.IsCancellationRequested)
                    throw;

                IsTimeout = true;
                throw new TimeoutException("KICC TIMEOUT");
            }
        }

        private async Task DelaySafeAsync(int delayMs)
        {
            if (_disposed || _disposeCts == null)
                return;

            await Task.Delay(delayMs, _disposeCts.Token).ConfigureAwait(true);
        }

        public KiccResponse? KiccCommandParsing(string recBuff)
        {
            LastSUC = string.Empty;
            LastMSG = string.Empty;

            if (string.IsNullOrWhiteSpace(recBuff))
                return null;

            string json = ExtractJsonp(recBuff);
            if (string.IsNullOrWhiteSpace(json))
                return null;

            json = json.Replace('\'', '"');

            try {
                var resp = JsonConvert.DeserializeObject<KiccResponse>(json);

                if (resp == null)
                    return null;

                LastSUC = resp.SUC ?? "";
                LastMSG = resp.MSG ?? "";
                return resp;
            }
            catch (Exception ex) {
                Console.WriteLine($"KICC PARSE ERROR: {ex.Message}");
                return null;
            }
        }


        public void DummyKiccParse()
        {
            string xdata = "jsonp12345678983543344({'SUC':'00','RQ01':'D1','RQ02':'8095612','RQ03':'A','RQ04':'4232170000000000=00002010000000000001','RQ05':'****','RQ06':'00','RQ07':'6700','RQ08':'','RQ09':'','RQ10':'','RQ11':'','RQ12':'','RQ13':'609','RQ14':'1234567890','RQ15':'WEB1234567890','RQ16':'40','RS01':'P','RS02':'A','RS03':'0025','RS04':'0000','RS05':'027','RS06':'0000','RS07':'2602130345485','RS08':'130341427683','RS09':'00062356','RS10':'N*','RS11':'027','RS12':'현대비자개인','RS13':'520860660','RS14':'현대카드사','RS15':'d','RS16':'','RS17':'               KICC로제출','RS18':'Y','RS19':'4900603240','RS20':''})";

            _kiccresp = KiccCommandParsing(xdata);

            if (_kiccresp != null) {
                var result = new KiccPaymentResult();

                result.IsSuccess = true;
                result.Message = _kiccresp.RS16! + _kiccresp.RS17!;
                result.TermId = _kiccresp.RQ02!;
                result.ResCode = _kiccresp.RS04!;
                result.BranchNo = _kiccresp.RS05!;
                result.CardId = _kiccresp.RQ04!.Split('=')[0];
                result.ApprovalNo = _kiccresp.RS09!;
                result.TradeNo = _kiccresp.RS03!;
                result.ValidDate = _kiccresp.RQ05!;
                result.Price = _payAmount;
                result.TradeDateTime = $"20{_kiccresp.RS07!.Substring(0, 12)}";
                result.CardName = _kiccresp.RS12!;
                result.IssuerName = _kiccresp.RS14!;
                result.Amount = result.Price;

                var sb = new StringBuilder();

                sb.AppendLine($"IsSuccess       : {result.IsSuccess}");
                sb.AppendLine($"Message         : {result.Message}");
                sb.AppendLine($"TermId          : {result.TermId}");
                sb.AppendLine($"ResCode         : {result.ResCode}");
                sb.AppendLine($"BranchNo        : {result.BranchNo}");
                sb.AppendLine($"CardId          : {result.CardId}");
                sb.AppendLine($"ApprovalNo      : {result.ApprovalNo}");
                sb.AppendLine($"TradeNo         : {result.TradeNo}");
                sb.AppendLine($"ValidDate       : {result.ValidDate}");
                sb.AppendLine($"Price           : {result.Amount}-{result.Price}");
                sb.AppendLine($"TradeDateTime   : {result.TradeDateTime}");
                sb.AppendLine($"CardName        : {result.CardName}");
                sb.AppendLine($"IssuerName      : {result.IssuerName}");

                Console.WriteLine(sb.ToString());

                PaymentCompleted?.Invoke(result);
            }
        }

        private static string ExtractJsonp(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return string.Empty;

            int start = s.IndexOf('(');
            int end = s.LastIndexOf(')');

            if (start < 0 || end <= start)
                return string.Empty;

            return s.Substring(start + 1, end - start - 1).Trim();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            PaymentCompleted = null;
            CancelHide = null;

            StopPayment();

            _cardTimer.Tick -= CardTimer_Tick;

            try {
                _disposeCts?.Cancel();
            }
            catch {
            }

            _disposeCts?.Dispose();
            _disposeCts = null;

            _cardTimer.Dispose();
            _httpClient.Dispose();
        }
    }
}

/*
private KiccCredit? _kiccCredit;

private void InitKicc()
{
    _kiccCredit = new KiccCredit(0);

    _kiccCredit.KiccLog = msg =>
    {
        Console.WriteLine(msg);
    };

    _kiccCredit.CancelHide = () =>
    {
        // 화면 숨김 취소 등
    };

    _kiccCredit.PaymentCompleted = result =>
    {
        if (result.IsSuccess) {
            Console.WriteLine($"승인성공: {result.Amount}, 승인번호={result.ApprovalNo}");
        }
        else {
            Console.WriteLine($"승인실패: {result.Message}");
        }
    };
}

private void StartKiccPay()
{
    if (_kiccCredit == null)
        return;

    _kiccCredit.BeginPayment(m_calPark.m_restMoney, m_VANTimeOut);
}
 * */