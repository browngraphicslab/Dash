﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class WebBoxView : UserControl
    {
        private WebView xWebView;
        public WebBoxView()
        {
            this.InitializeComponent();
            //TODO Try out SeparateThread and SeparateProcess
            xWebView = new WebView(WebViewExecutionMode.SameThread);
            xGrid.Children.Add(xWebView);
        }

        public WebView GetView()
        {
            return xWebView;
        }
    }
}
