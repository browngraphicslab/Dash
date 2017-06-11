using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Windows.UI.Xaml.Shapes;
using Microsoft.Extensions.DependencyInjection;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash 
{
    public sealed partial class OverlayCanvas : UserControl
    {
        public static OverlayCanvas Instance = null;

        public TappedEventHandler OnEllipseTapped;
        public TappedEventHandler OnEllipseTapped2;
        
        public OverlayCanvas()
        {
            this.InitializeComponent();

            Debug.Assert(Instance == null);
            Instance = this;
        }

        private void Ellipse_Tapped(object sender, TappedRoutedEventArgs e)
        {
            OnEllipseTapped.Invoke(sender, e);
        }

        private async void Ellipse_Tapped_1(object sender, TappedRoutedEventArgs e)
        {
            OnEllipseTapped2.Invoke(sender, e);
            Debug.WriteLine("Ellipse tapped");


            var ac = (Application.Current as App)?.Container.GetRequiredService<AccountController>();
            ac?.Register("baddabee@gmail.com", "Somefck1np@ss!");
            var authC = (Application.Current as App)?.Container.GetRequiredService<AuthenticationController>();
            var requestToken = authC?.RequestToken("baddabee@gmail.com", "Somefck1np@ss!");
            if (requestToken != null)
            {
                var token = await requestToken;
                var sc = (Application.Current as App)?.Container.GetRequiredService<ServerController>();
                sc?.SetAuthorizationToken(token);
            }
            var userInfo = ac?.GetUserInfo();
            if (userInfo != null)
            {
                var ui = await userInfo;
            }
        }

    }
}
