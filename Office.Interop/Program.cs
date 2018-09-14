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
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Microsoft.Office.Interop.Word;
using Excel = Microsoft.Office.Interop.Excel;
using Word = Microsoft.Office.Interop.Word;
using Windows.Foundation;

namespace OfficeInterop
{
    class Program
    {
        private static AppServiceConnection _connection = null;
        private static AutoResetEvent _appServiceExit;

        private static readonly MessageHandler Handler = new MessageHandler();

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int Width, int Height, bool Repaint);
        
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, ShowWindowEnum flags);

        private enum ShowWindowEnum
        {
            Hide = 0,
            ShowNormal = 1, ShowMinimized = 2, ShowMaximized = 3,
            Maximize = 3, ShowNormalNoActivate = 4, Show = 5,
            Minimize = 6, ShowMinNoActivate = 7, ShowNoActivate = 8,
            Restore = 9, ShowDefault = 10, ForceMinimized = 11
        };

        [DllImport("user32.dll")]
        private static extern int SetForegroundWindow(IntPtr hwnd);
        
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        private const UInt32 TOPMOST_FLAGS = 0x0002 | 0x0001;

        public static void ShutdownWordApps()
        {
            foreach (var process in Process.GetProcessesByName("WINWORD"))
            {
                if (process.MainWindowTitle == "")
                    process.Kill();
            }
        }

        public static void Main(string[] args)
        {
            // connect to app service and wait until the connection gets closed
            _appServiceExit = new AutoResetEvent(false);
            InitializeAppServiceConnection();

            //CODE TO CHANGE STUFF WITH NOTEPAD'S WINDOW
            //Process[] processes = Process.GetProcessesByName("notepad");
            //foreach (Process p in processes)
            //{
            //    IntPtr handle = p.MainWindowHandle;

            //    //move window to give position / size
            //    MoveWindow(handle, 0, 0, 200, 200, true);


            //    //make window not minused and on front
            //    ShowWindow(handle, ShowWindowEnum.Restore);
            //    SetForegroundWindow(handle);

            //    //pin window to top
            //    SetWindowPos(handle, new IntPtr(-1), 0, 0, 0, 0, TOPMOST_FLAGS);
            //}

            _appServiceExit.WaitOne();
            Handler.Close();
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
