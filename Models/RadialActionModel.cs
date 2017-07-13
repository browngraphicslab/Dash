﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using RadialMenuControl.UserControl;

namespace Dash.Models
{
    class RadialActionModel: RadialItemModel
    {
        /// <summary>
        /// Different types of actions that can be set and will (all) be 
        /// invoked when the RadialMenuButton associated with this model 
        /// is pressed. 
        /// </summary>
        public Action<Canvas, Point> CanvasAction { get; set; }
        public Action<Color, RadialMenu> ColorAction { get; set; }
        public Action<object> GenericAction { get; set; }
        public Action<double> NumberAction { get; set; }
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
    }
}
