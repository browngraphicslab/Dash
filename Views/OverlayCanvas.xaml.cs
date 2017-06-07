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

        private void Ellipse_Tapped_1(object sender, TappedRoutedEventArgs e)
        {
            OnEllipseTapped2.Invoke(sender, e);
        }
    }
}
