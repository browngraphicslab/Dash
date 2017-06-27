using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;

namespace Dash.Models
{
    class RadialSubmenuModel:RadialItemModel
    {
        public List<RadialItemModel> RadialItemList { get; }

        // Constructor for a submenu with an image
        public RadialSubmenuModel(string description, ImageSource iconSource, List<RadialItemModel> buttons)
        {
            Description = description;
            IconSource = iconSource;
            RadialItemList = buttons;
            IsAction = false;
        }

        // Constructor for a submenu with a string icon
        public RadialSubmenuModel(string description, string icon, List<RadialItemModel> buttons)
        {
            Description = description;
            Icon = icon;
            RadialItemList = buttons;
            IsAction = false;
        }
    }
}
