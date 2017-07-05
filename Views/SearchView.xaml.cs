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
    public sealed partial class SearchView : UserControl
    {
        public bool IsDraggable
        {
            get { return _isDraggable; }
            set
            {
                _isDraggable = value;
                this.ChangeDraggability();
            } }

        private bool _isDraggable;
        public SearchView()
        {
            this.InitializeComponent();
            this.SetManipulation();
        }

        private void SetManipulation()
        {
            xMainGrid.ManipulationMode = ManipulationModes.All;
            xMainGrid.RenderTransform = new CompositeTransform();
            xMainGrid.ManipulationDelta += delegate (object sender, ManipulationDeltaRoutedEventArgs e)
            {
                var transform = xMainGrid.RenderTransform as CompositeTransform;
                if (transform != null)
                {
                    transform.TranslateX += e.Delta.Translation.X;
                    transform.TranslateY += e.Delta.Translation.Y;
                }
            };
        }

        
        public void SetPosition(Point point)
        {
            Canvas.SetLeft(this, point.X);
            Canvas.SetTop(this,point.Y);
        }

        private void ChangeDraggability()
        {
            xMainGrid.ManipulationMode = _isDraggable ? ManipulationModes.All : ManipulationModes.None;
        }

        //private void XMainGrid_OnHolding(object sender, HoldingRoutedEventArgs e)
        //{
        //    ((Canvas)VisualTreeHelper.GetParent(this)).Children.Remove(this);
        //}
    }
}
