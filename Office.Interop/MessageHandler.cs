using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Windows.Foundation.Collections;
using Word = Microsoft.Office.Interop.Word;

namespace OfficeInterop
{
    class MessageHandler
    {
        private Word.Application _word;
        private readonly Word.Document _doc;
        private static IntPtr windowHandle = IntPtr.Zero;

        private ChromeApp _chrome;

        public MessageHandler()
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
            {
                _word?.Quit(Word.WdSaveOptions.wdDoNotSaveChanges);
                _word = null;
            };

            Program.ShutdownWordApps();
            _word = new Word.Application();
            _doc = _word.Documents.Add();

            _chrome = new ChromeApp();
            _chrome.MessageReceived += message =>
            {
                Debug.WriteLine("received message:");
                Debug.WriteLine(message);
                Debug.WriteLine("----------------");

                // See if Chrome is open
                var newWindowHandle = WindowAPI.GetWindowByName("Chrome");
                if (newWindowHandle == IntPtr.Zero)
                {
                    return;
                }

                if (newWindowHandle != windowHandle)
                {
                    windowHandle = newWindowHandle;
                    WindowAPI.AddWindowEventListener(windowHandle, onMoveSizeChanged);
                }

                if (message.StartsWith("activate"))
                {
                    var sizex = WindowAPI.GetControlSize(windowHandle);

                    // place chrome in top-left corner
                    WindowAPI.ModifyWindow(windowHandle, 0, 0, (int)sizex.Width, (int)sizex.Height);

                    // ... notify Dash here that plugin was activated, pass 'size'.

                }
                else if (message.StartsWith("deactivate"))
                {
                }
                else if (message.StartsWith("expand"))
                {
                    WindowAPI.UndoSticky(windowHandle);

                    // ... notify Dash here
                }
                else if (message.StartsWith("collapse"))
                {
                    WindowAPI.MakeSticky(windowHandle);
                }
                else
                {
                }

                var colon = message.IndexOf(':');
                var bracket = message.IndexOf('[');
                if (colon >= 0 && colon < bracket)
                {
                    message = message.Substring(colon + 1);
                }
                OnSendRequest(new ValueSet()
                {
                    ["REQUEST"] = "Chrome",
                    ["DEBUG"] = "Received Chrome message",
                    ["DATA"] = message
                });


                // ... notify Dash here
                var size = WindowAPI.GetControlSize(windowHandle);
                OnSendRequest(new ValueSet()
                {
                    ["REQUEST"] = "SizeChrome",
                    ["DEBUG"] = "Chrome window changed",
                    ["DATA"] = "" + size.Width + "," + size.Height
                });
            };
            _chrome.Start();
        }

        private static string extractClipboardSource()
        {

            try
            {
                var sb   = new StringBuilder();
                var data = Clipboard.GetDataObject();
                var d    = data.GetData("OwnerLink", true);
                if (d != null)
                {
                    switch (d.GetType().ToString())
                    {
                    case "System.IO.MemoryStream":
                        var ms = (MemoryStream)data.GetData("OwnerLink", true);
                        var output = ms.ToArray().Select(a => (char)a);
                        return new string(output.ToArray());
                    }
                }
            }
            catch (Exception)
            {

            }
            return null;
        }

        public void Close()
        {
            _word.Quit(Word.WdSaveOptions.wdDoNotSaveChanges);
            _word = null;
        }

        private void onMoveSizeChanged(IntPtr hook, uint type, IntPtr hwnd, int idObject, int child, uint thread, uint time)
        {
            var size = WindowAPI.GetControlSize(windowHandle);
            Debug.WriteLine("Size/Position of Chrome has changed.");
            Debug.WriteLine(size);
            OnSendRequest(new ValueSet()
            {
                ["REQUEST"] = "SizeChrome",
                ["DEBUG"] = "Chrome window changed",
                ["DATA"] = "" + size.Width + "," + size.Height
            });
        }

        //Event that is triggered when we want to send a message through the interop to Dash
        public event Action<ValueSet> SendRequest;

        /// <summary>
        /// Processes a message that was received through the interop from Dash and dispatches is to the correct place
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [STAThread]
        public ValueSet ProcessMessage(ValueSet request)
        {
            string value = request["REQUEST"] as string;
            var response = new ValueSet();
            string debug = "";
            string result = "";
            switch (value)
            {
            case "Get OwnerLink":
                Program.F.Invoke(new MethodInvoker(() =>
                {
                    var csource = extractClipboardSource();
                    if (!string.IsNullOrEmpty(csource))
                    {
                        response.Add("OwnerLink", csource);
                        result = "SUCCESS";
                    }
                    else
                    {
                        result = "FAILURE";
                    }
                }));
                break;
            case "HTML to RTF":
                try
                {
                    _doc.Content.Select();//Select all and delete in case we are reusing a document
                    _doc.Content.Delete();

                    _doc.Content.Paste();//paste html
                    _doc.Content.Select();//select all
                    _doc.Content.Copy();//copy rtf
                    result = "SUCCESS";
                }
                catch (Exception exc)
                {
                    result = exc.Message;
                }
                break;
            case "Chrome":
                _chrome.Send(Encoding.UTF8.GetBytes((string) request["DATA"]));
                break;
            case "OpenUri":
                var strs = ((string) request["DATA"]).Split('!');
                if (strs.Count() == 2)
                {
                    Process.Start(strs[0]);
                    System.Threading.Thread.Sleep(1000);
                    if (int.TryParse(strs[1], out int page))
                    {
                        new Microsoft.Office.Interop.PowerPoint.Application().ActiveWindow.View.GotoSlide(page);
                    }
                }
                else System.Diagnostics.Process.Start((string)request["DATA"]);
                break;
            default:
                result = "unknown request";
                break;
            }

            response.Add("RESPONSE", result);
            response.Add("DEBUG", debug);

            return response;
        }

        protected virtual void OnSendRequest(ValueSet obj)
        {
            SendRequest?.Invoke(obj);
        }
    }
}
