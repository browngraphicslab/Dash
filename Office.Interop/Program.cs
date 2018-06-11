//*********************************************************  
//  
// Copyright (c) Microsoft. All rights reserved.  
// This code is licensed under the MIT License (MIT).  
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF  
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY  
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR  
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.  
//  
//*********************************************************  
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Excel = Microsoft.Office.Interop.Excel;
using Word = Microsoft.Office.Interop.Word;

namespace ExcelInterop
{
    class Program
    {
        static AppServiceConnection connection = null;
        static AutoResetEvent appServiceExit;
        private static readonly Word.Application Word = new Word.Application();
        private static Word.Document _doc = Word.Documents.Add();

        static void Main(string[] args)
        {
            // connect to app service and wait until the connection gets closed
            appServiceExit = new AutoResetEvent(false);
            InitializeAppServiceConnection();
            appServiceExit.WaitOne();
        }

        static async void InitializeAppServiceConnection()
        {
            connection = new AppServiceConnection();
            connection.AppServiceName = "OfficeInteropService";
            connection.PackageFamilyName = Windows.ApplicationModel.Package.Current.Id.FamilyName;
            connection.RequestReceived += Connection_RequestReceived;
            connection.ServiceClosed += Connection_ServiceClosed;

            AppServiceConnectionStatus status = await connection.OpenAsync();
            if (status != AppServiceConnectionStatus.Success)
            {
                // TODO: error handling
            }
        }

        private static void Connection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            // signal the event so the process can shut down
            appServiceExit.Set();
        }

        private static async void Connection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            string value = args.Request.Message["REQUEST"] as string;
            var response = new ValueSet();
            string debug = "";
            string result = "";
            switch (value)
            {
                case "HTML to RTF":
                    try
                    {
                        _doc.Content.Select();//Select all and delete in case we are reusing a document
                        debug += $"select \n";
                        _doc.Content.Delete();
                        debug += $"delete \n";

                        _doc.Content.Paste();//paste html
                        debug += $"paste \n";
                        _doc.Content.Select();//select all
                        debug += $"select 2\n";
                        _doc.Content.Copy();//copy rtf
                        debug += $"copy\n";
                        result = "SUCCESS";
                    }
                    catch (Exception exc)
                    {
                        debug += exc.Message;
                        result = exc.Message;
                    }
                    break;
                default:
                    result = "unknown request";
                    break;
            }

            response.Add("RESPONSE", result);
            response.Add("DEBUG", debug);
            await args.Request.SendResponseAsync(response);
        }
    }
}
