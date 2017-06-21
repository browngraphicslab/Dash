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


        public TappedEventHandler OnAddDocumentsTapped, OnAddCollectionTapped, OnAddAPICreatorTapped, OnAddImageTapped, OnAddShapeTapped, OnOperatorAdd;
                
        public OverlayCanvas()
        {
            this.InitializeComponent();

            Debug.Assert(Instance == null);
            Instance = this;
        }

        private void AddDocumentsTapped(object sender, TappedRoutedEventArgs e)
        {
            OnAddDocumentsTapped?.Invoke(sender, e);
        }

        private void AddCollectionTapped(object sender, TappedRoutedEventArgs e)
        {
            OnAddCollectionTapped?.Invoke(sender, e);
        }

        private void AddShapeTapped(object sender, TappedRoutedEventArgs e)
        {
            OnAddShapeTapped?.Invoke(sender, e);
        }

        private void image1_Tapped(object sender, TappedRoutedEventArgs e) {
            OnAddImageTapped?.Invoke(sender, e);
        }

        private void image_Tapped(object sender, TappedRoutedEventArgs e) {
            OnAddAPICreatorTapped?.Invoke(sender, e);
        }

        private void UIElement_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            OnOperatorAdd?.Invoke(sender, e);
        }
    }
}
