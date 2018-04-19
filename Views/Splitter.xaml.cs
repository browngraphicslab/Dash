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
        private FrameworkElement _left;
        private FrameworkElement _right;

        public Splitter()
        {
            this.InitializeComponent();
        }

        public void SetLeft(FrameworkElement left)
        {
            _left = left;
        }

        public void SetRight(FrameworkElement right)
        {
            _right = right;
        }

        private void Splitter_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            _left.Width += e.Delta.Translation.X;
            _right.Width -= e.Delta.Translation.X;
        }
    }
}
