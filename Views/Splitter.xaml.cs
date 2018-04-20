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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views
{
    public sealed partial class Splitter : UserControl
    {
        public FrameworkElement Left { get; set; }
        public FrameworkElement Right { get; set; }

        public Splitter()
        {
            this.InitializeComponent();
        }

        private void Splitter_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (!Double.IsNaN(Left.Width))
                Left.Width += e.Delta.Translation.X;
            if (!Double.IsNaN(Right.Width))
                Right.Width -= e.Delta.Translation.X;
        }
    }
}
