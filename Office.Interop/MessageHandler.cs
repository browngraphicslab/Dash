using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Excel = Microsoft.Office.Interop.Excel;
using Word = Microsoft.Office.Interop.Word;

namespace OfficeInterop
{
    class MessageHandler
    {
        private Word.Application _word;
        private readonly Word.Document _doc;

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

            _chrome = new ChromeApp();
            _chrome.MessageReceived += s =>
            {
                OnSendRequest(new ValueSet()
                {
                    ["REQUEST"] = "Chrome",
                    ["DEBUG"] = "Received Chrome message",
                    ["DATA"] = s
                });
            };
            _chrome.Start();
        }

        public void Close()
        {
            _word.Quit(Word.WdSaveOptions.wdDoNotSaveChanges);
            _word = null;
        }

        //Event that is triggered when we want to send a message through the interop to Dash
        public event Action<ValueSet> SendRequest; 

        /// <summary>
        /// Processes a message that was received through the interop from Dash and dispatches is to the correct place
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public ValueSet ProcessMessage(ValueSet request)
        {
            string value = request["REQUEST"] as string;
            var response = new ValueSet();
            string debug = "";
            string result = "";
            switch (value)
            {
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
                    _chrome.Send(Encoding.UTF8.GetBytes(request["DATA"] as string));
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
