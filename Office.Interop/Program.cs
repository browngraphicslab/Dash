﻿//*********************************************************  
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

namespace OfficeInterop
{
    class Program
    {
        private static AppServiceConnection _connection = null;
        private static AutoResetEvent _appServiceExit;

        private static readonly MessageHandler Handler = new MessageHandler();

        public static void Main(string[] args)
        {
            // connect to app service and wait until the connection gets closed
            _appServiceExit = new AutoResetEvent(false);
            InitializeAppServiceConnection();
            _appServiceExit.WaitOne();
        }

        private static async void InitializeAppServiceConnection()
        {
            _connection = new AppServiceConnection
            {
                AppServiceName = "OfficeInteropService",
                PackageFamilyName = Windows.ApplicationModel.Package.Current.Id.FamilyName
            };
            _connection.RequestReceived += Connection_RequestReceived;
            _connection.ServiceClosed += Connection_ServiceClosed;
            Handler.SendRequest += async delegate(ValueSet set) { await _connection.SendMessageAsync(set); };

            AppServiceConnectionStatus status = await _connection.OpenAsync();
            if (status != AppServiceConnectionStatus.Success)
            {
                // TODO: error handling
            }
        }

        private static void Connection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            // signal the event so the process can shut down
            _appServiceExit.Set();
        }

        private static async void Connection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            await args.Request.SendResponseAsync(Handler.ProcessMessage(args.Request.Message));
        }
    }
}
