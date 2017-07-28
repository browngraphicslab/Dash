using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace Dash
{
    public class RadialItemModel
    {
        public string Description { get; set; }
        public ImageSource IconSource { get; set; }
        public string Icon { get; set; }
        public Color BackGroundColor { get; set; }
        public bool IsAction { get; set; }
        public bool IsDraggable { get; set; }
        public bool IsSubMenu { get; set; }
    }
}
