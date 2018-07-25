using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class RegionAnnotation : UserControl, ISelectable
    {

        public bool Selected { get; private set; }

        public DocumentController RegionDocument { get; }

        public RegionAnnotation(DocumentController regionDocument)
        {
            this.InitializeComponent();

            RegionDocument = regionDocument;

            Width = regionDocument.GetWidth();
            Height = regionDocument.GetHeight();
            var pos = regionDocument.GetPosition();
            Canvas.SetLeft(this, pos?.X ?? 0);
            Canvas.SetTop(this, pos?.Y ?? 0);
        }

        public void Select()
        {
            XRegionRect.StrokeDashArray = new DoubleCollection();
            XRegionRect.StrokeThickness = 3;
            XRegionRect.Stroke = new SolidColorBrush(Colors.Coral);
            XRegionRect.Fill = new SolidColorBrush(Color.FromArgb(40, 255, 255, 0));
            Selected = true;
        }

        public void Deselect()
        {
            XRegionRect.StrokeDashArray = new DoubleCollection {2};
            XRegionRect.StrokeThickness = 2;
            XRegionRect.Stroke = new SolidColorBrush(Colors.Black);
            XRegionRect.Fill = new SolidColorBrush(Colors.Transparent);
            Selected = false;
        }
    }
}