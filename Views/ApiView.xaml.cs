using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class ApiView : UserControl
    {
        private ApiCreatorDisplay XApiCreator;

        public ApiView()
        {
            this.InitializeComponent();
            XApiCreator = new ApiCreatorDisplay(XApiSource);
            xGrid.Children.Add(XApiCreator);
            XApiCreator.MakeApi += XApiCreatorOnMakeApi;
            XApiSource.EditApi += XApiSourceOnEditApi;
        }

        private void XApiSourceOnEditApi()
        {
            XApiSource.Visibility = Visibility.Collapsed;
            XApiCreator.Visibility = Visibility.Visible;
        }

        private void XApiCreatorOnMakeApi()
        {
            XApiSource.Visibility = Visibility.Visible;
            XApiCreator.Visibility = Visibility.Collapsed;
        }
    }
}
