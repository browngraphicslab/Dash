using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Dash.Models
{
    class RadialActionModel: RadialItemModel
    {
        public Action<OverlayCanvas, Point> Action;
        public RadialActionModel(string description, ImageSource iconSource, Action<OverlayCanvas, Point> action)
        {
            Description = description;
            IconSource = iconSource;
            Action = action;
            IsAction = true;
        }

        public RadialActionModel(string description, string icon, Action<OverlayCanvas, Point> action)
        {
            Description = description;
            Icon = icon;
            Action = action;
            IsAction = true;
        }
    }
}
