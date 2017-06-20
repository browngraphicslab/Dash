using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Dash.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CollectionScreen : Page
    {
        public CollectionScreen()
        {
            this.InitializeComponent();
        }

        private void xCollectionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            xSelectCollectionMessage.Visibility = Visibility.Collapsed;
            xCollectionProperties.Visibility = Visibility.Visible;
            xCollectionName.Visibility = Visibility.Visible;
        }
    }
}
