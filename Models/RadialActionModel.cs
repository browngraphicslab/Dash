using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using RadialMenuControl.UserControl;

namespace Dash.Models
{
    public class RadialActionModel: RadialItemModel
    {
        /// <summary>
        /// Different types of actions that can be set and will (all) be 
        /// invoked when the RadialMenuButton associated with this model 
        /// is pressed. 
        /// </summary>
        public Action<ICollectionView, DragEventArgs> CollectionDropAction { get; set; }
        public Action<object, DragEventArgs> GenericDropAction { get; set; }
        public Action<Color, RadialMenu> ColorAction { get; set; }
        public Action<object> GenericAction { get; set; }
        public Action<double> NumberAction { get; set; }
        public bool IsRadio { get; set; } = true; 
        public bool IsToggle { get; set; }

        public RadialActionModel(string description, ImageSource iconSource)
        {
            Description = description;
            IconSource = iconSource;
            IsAction = true;
            BackGroundColor = Colors.Transparent;
        }

        public RadialActionModel(string description, string icon)
        {
            Description = description;
            Icon = icon;
            IsAction = true;
            BackGroundColor = Colors.Transparent;
        }

        public RadialActionModel(string description, Symbol icon)
        {
            Description = description;
            IconSymbol = icon;
            IsAction = true;
            BackGroundColor = Colors.Transparent;
        }
    }
}
