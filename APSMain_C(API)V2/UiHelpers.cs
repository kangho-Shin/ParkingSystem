using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace APSMain
{
    public static class UiHelpers
    {
        #region PayMsg
        public static PayMsgForm? _payMsgForm;
        public static MessageForm? _MessageForm;

        // 생성 레이스 방지
        private static int _creating = 0;                 // 0: idle, 1: creating
        private static volatile int _pendingIndex = 0;   // 생성 중 들어온 최신 요청
        private static volatile bool _pendingEndFlag = false;
        private static volatile int _delayTime = 5000;
        private static Form? _parentForm = null;


        static void CenterOver(Control anchor, Form child)
        {
            if (anchor == null || child == null)
                return;

            if (anchor.IsDisposed || anchor.Disposing || !anchor.IsHandleCreated)
                return;

            var r = anchor.RectangleToScreen(anchor.ClientRectangle); // 클라이언트→스크린
            int x = r.Left + (r.Width - child.Width) / 2;
            int y = r.Top + (r.Height - child.Height) / 2;

            child.StartPosition = FormStartPosition.Manual;
            child.Location = new Point(x, y);
        }

        public static void ShowPayMsg(Form parent, int nIndex = 0, bool endflag = false)
        {
            // 이미 살아있으면 재사용
            var inst = _payMsgForm;
            if (inst != null && !inst.IsDisposed) {
                if (!inst.Visible) {
                    CenterOver(parent, inst);
                    inst.Show(parent);     // 매번 owner 지정
                    inst.BringToFront();   // 재사용 시에도 동일
                }
                ShowPayMessage(nIndex, endflag);
                return;
            }

            // 아직 없거나(=null) Disposed 상태. 생성 요청을 최신 값으로 기록
            _parentForm = parent;
            _pendingIndex = nIndex;
            _pendingEndFlag = endflag;
            //_chkButton = chkButton;
            // 생성 중이면(다른 스레드가 이미 예약) 추가 생성 금지
            if (Interlocked.CompareExchange(ref _creating, 1, 0) != 0)
                return;

            void ShowOnUi()
            {
                try {
                    if (_payMsgForm == null || _payMsgForm.IsDisposed) {
                        _payMsgForm = new PayMsgForm();
                        _payMsgForm.AutoClose += DistroyPayForm;
                        CenterOver(parent, _payMsgForm);
                        _payMsgForm.Show(parent);   // owner 지정
                        _payMsgForm.BeginInvoke((Action)(() => CenterOver(parent, _payMsgForm)));
                        _payMsgForm.BringToFront();   // 재사용 시에도 동일
                    }
                    _payMsgForm.BringToFront();

                    // 생성 완료 시점에 최신 요청을 적용
                    ShowPayMessage(_pendingIndex, _pendingEndFlag);
                }
                finally {
                    Interlocked.Exchange(ref _creating, 0);
                }
            }

            if (parent.InvokeRequired)
                parent.BeginInvoke((Action)ShowOnUi);
            else
                ShowOnUi();
        }

        public static void ShowPayMessage(int nIndex, bool endflag)
        {
            var f = _payMsgForm;
            if (f == null || f.IsDisposed)
                return;

            _pendingIndex = nIndex;
            _pendingEndFlag = endflag;
            if (f.InvokeRequired) {
                f.BeginInvoke(new Action(() =>
                {
                    f.ShowMessage(nIndex);
                    if (endflag) {
                        if (nIndex == 0)
                            f.StartPayAutoClose(3000);
                        else
                            f.StartPayAutoClose(_delayTime);
                    }
                }));
            }
            else {
                f.ShowMessage(nIndex);
                if (endflag)
                    f.StartPayAutoClose(_delayTime);
            }
        }

        public static void DistroyPayForm()
        {
            var f = _payMsgForm;
            if (f != null)
                f.AutoClose -= DistroyPayForm;

            _payMsgForm = null;

            //if(_parentForm!= null && _chkButton != null ) {
            //    _parentForm.BeginInvoke((Action)(() =>
            //    {
            //        if (_chkButton.CanFocus)
            //            _chkButton.Focus();
            //    }));
            //}
            Interlocked.Exchange(ref _creating, 0);
        }
        /*
                public static void ShowMessage(Form parent,int nIndex = 0, bool endflag = false)
                {
                    // 이미 살아있으면 재사용
                    var inst = _MessageForm;
                    if (inst != null && !inst.IsDisposed) {
                        if (!inst.Visible) {
                            CenterOver(parent, inst);
                            inst.Show(parent);     // 매번 owner 지정
                            inst.BringToFront();   // 재사용 시에도 동일
                        }
                        ShowMessageForm(nIndex, endflag);
                        return;
                    }

                    // 아직 없거나(=null) Disposed 상태. 생성 요청을 최신 값으로 기록
                    _pendingIndex = nIndex;
                    _pendingEndFlag = endflag;

                    // 생성 중이면(다른 스레드가 이미 예약) 추가 생성 금지
                    if (Interlocked.CompareExchange(ref _creating, 1, 0) != 0)
                        return;

                    void ShowOnUi()
                    {
                        try {
                            if (_MessageForm == null || _MessageForm.IsDisposed) {
                                _MessageForm = new MessageForm();
                                _MessageForm.AutoClose += DistroyMessageForm;
                                CenterOver(parent, _MessageForm);
                                _MessageForm.Show(parent);   // owner 지정
                                _MessageForm.BeginInvoke((Action)(() => CenterOver(parent, _MessageForm)));
                                _MessageForm.BringToFront();   // 재사용 시에도 동일
                            }
                            _MessageForm.BringToFront();

                            // 생성 완료 시점에 최신 요청을 적용
                            ShowMessageForm(_pendingIndex, _pendingEndFlag);
                        }
                        finally {
                            Interlocked.Exchange(ref _creating, 0);
                        }
                    }

                    if (parent.InvokeRequired)
                        parent.BeginInvoke((Action)ShowOnUi);
                    else
                        ShowOnUi();
                }
        */

        public static void ShowMessage(Form parent, int nIndex = 0, bool endflag = false)
        {
            if (parent == null || parent.IsDisposed || parent.Disposing || !parent.IsHandleCreated)
                return;

            _pendingIndex = nIndex;
            _pendingEndFlag = endflag;

            void ShowOnUi()
            {
                try {
                    if (parent.IsDisposed || parent.Disposing || !parent.IsHandleCreated)
                        return;

                    var inst = _MessageForm;

                    if (inst != null && !inst.IsDisposed) {
                        if (!inst.Visible) {
                            CenterOver(parent, inst);

                            if (parent.IsDisposed || parent.Disposing)
                                return;

                            inst.Show(parent);
                        }

                        inst.BringToFront();
                        ShowMessageForm(_pendingIndex, _pendingEndFlag);
                        return;
                    }

                    if (Interlocked.CompareExchange(ref _creating, 1, 0) != 0)
                        return;

                    try {
                        if (parent.IsDisposed || parent.Disposing || !parent.IsHandleCreated)
                            return;

                        _MessageForm = new MessageForm();
                        _MessageForm.AutoClose += DistroyMessageForm;

                        CenterOver(parent, _MessageForm);

                        if (parent.IsDisposed || parent.Disposing) {
                            _MessageForm.Dispose();
                            _MessageForm = null;
                            return;
                        }

                        _MessageForm.Show(parent);
                        _MessageForm.BringToFront();

                        ShowMessageForm(_pendingIndex, _pendingEndFlag);
                    }
                    finally {
                        Interlocked.Exchange(ref _creating, 0);
                    }
                }
                catch (ObjectDisposedException) {
                    Interlocked.Exchange(ref _creating, 0);
                }
                catch (InvalidOperationException) {
                    Interlocked.Exchange(ref _creating, 0);
                }
            }

            try {
                if (parent.InvokeRequired)
                    parent.BeginInvoke((Action)ShowOnUi);
                else
                    ShowOnUi();
            }
            catch (ObjectDisposedException) {
            }
            catch (InvalidOperationException) {
            }
        }

        public static void ShowMessageForm(int nIndex, bool endflag)
        {
            var f = _MessageForm;
            if (f == null || f.IsDisposed)
                return;

            _pendingIndex = nIndex;
            _pendingEndFlag = endflag;
            if (f.InvokeRequired) {
                f.BeginInvoke(new Action(() =>
                {
                    f.ShowMessage(nIndex);
                    if (endflag)
                        f.StartAutoClose(_delayTime);
                }));
            }
            else {
                f.ShowMessage(nIndex);
                if (endflag)
                    f.StartAutoClose(_delayTime);
            }
        }

        public static void DistroyMessageForm()
        {
            var f = _payMsgForm;
            if (f != null)
                f.AutoClose -= DistroyMessageForm;

            _payMsgForm = null;
            Interlocked.Exchange(ref _creating, 0);
        }
        #endregion

        #region HtmlMsg
        public static GeneralMsgFrom? _gMsgForm;
        private static int _creatingGen = 0; // 생성 레이스 방지
        public static bool HtmlMsg = false;
        private static volatile bool _pendingAutoClose = false;
        private static volatile int _pendingCloseMs = 5000;

        /// <summary>
        /// 일반 메시지폼 표시(존재하면 재사용). 2중 생성 방지.
        /// </summary>
        public static void ShowHtmlMsg(Form owner, int msgIndex, bool autoClose = false, int closeMs = 5000)
        {
            var inst = _gMsgForm;
            if (inst != null && !inst.IsDisposed) {
                if (!inst.Visible)
                    inst.Show(owner);
                ShowHtml(msgIndex);
                return;
            }
            _pendingAutoClose = autoClose;
            _pendingCloseMs = closeMs;

            if (Interlocked.CompareExchange(ref _creatingGen, 1, 0) != 0)
                return; // 이미 생성 중 → 해당 생성이 끝나면 pending 적용됨

            void ShowOnUi()
            {
                try {
                    if (_gMsgForm == null || _gMsgForm.IsDisposed) {
                        _gMsgForm = new GeneralMsgFrom();
                        _gMsgForm.AutoClose += DistroyMsgForm;
                        _gMsgForm.Show(owner);
                        HtmlMsg = true;
                    }
                    _gMsgForm.BringToFront();
                }
                finally {
                    Interlocked.Exchange(ref _creatingGen, 0);
                }
            }

            if (owner.InvokeRequired)
                owner.BeginInvoke((Action)ShowOnUi);
            else
                ShowOnUi();
        }

        public static void CenterOverMenu()
        {
            if (_gMsgForm != null)
                _gMsgForm.CenterOverMenu();
        }

        public static void ShowHtml(int nIndex)
        {

        }

        public static void CloseHtmlMsg()
        {
            if (_gMsgForm != null) {
                HtmlMsg = false;
                _gMsgForm.Close();
            }
        }

        public static void DistroyMsgForm()
        {
            var f = _gMsgForm;
            if (f != null)
                f.AutoClose -= DistroyMsgForm;
            _gMsgForm = null;
            Interlocked.Exchange(ref _creatingGen, 0);
        }
        #endregion
    }
}
