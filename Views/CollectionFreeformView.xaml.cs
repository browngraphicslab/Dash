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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views
{
    public sealed partial class CollectionFreeformView : UserControl
    {
        public CollectionFreeformView()
        {
            this.InitializeComponent();
        }

        private void DocumentView_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void DocumentView_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void StartDrag(OperatorView.IOReference ioreference)
        {
            throw new NotImplementedException();
        }

        private void EndDrag(OperatorView.IOReference ioreference)
        {
            throw new NotImplementedException();
        }

        private void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e)
        {
            DocumentView dv = sender as DocumentView;
            ManipulationControls ctrl = new ManipulationControls(dv);
        }
    }
}
