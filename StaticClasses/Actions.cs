using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Dash.StaticClasses
{
    public static class Actions
    {

        public static void AddSearch(OverlayCanvas c, Point p)
        {
            var searchBox = new SearchBox();
            searchBox.Height = 30;
            searchBox.Width = 250;
            c.Children.Add(searchBox);
            Canvas.SetLeft(searchBox, p.X);
            Canvas.SetTop(searchBox, p.Y);
            searchBox.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            searchBox.RenderTransform = new CompositeTransform();
            searchBox.ManipulationDelta += delegate (object sender, ManipulationDeltaRoutedEventArgs e)
            {
                var transform = searchBox.RenderTransform as CompositeTransform;
                transform.TranslateX += e.Delta.Translation.X;
                transform.TranslateY += e.Delta.Translation.Y;
            };
            searchBox.Holding += delegate { ((Canvas)searchBox.Parent).Children.Remove(searchBox); };
        }

        public static void AddPalette(OverlayCanvas c, Point p)
        {

        }
    }
}
