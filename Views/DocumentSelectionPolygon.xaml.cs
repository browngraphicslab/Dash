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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class DocumentSelectionPolygon : UserControl
    {
        public Brush Fill
        {
            get => VisualHull.Fill;
            set => VisualHull.Fill = value;
        }
        
        public double StrokeThickness
        {
            get => VisualHull.StrokeThickness;
            set => VisualHull.StrokeThickness = value;
        }
        public Brush Stroke
        {
            get => VisualHull.Stroke;
            set => VisualHull.Stroke = value;
        }
        public PointCollection Points
        {
            get => VisualHull.Points;
            set => VisualHull.Points = value;
        }

        public DocumentSelectionPolygon()
        {
            this.InitializeComponent();
            VisualHull.CompositeMode = ElementCompositeMode.SourceOver;
        }
    }
}
