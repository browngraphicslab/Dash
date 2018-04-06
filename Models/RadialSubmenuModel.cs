using System;
using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using RadialMenuControl.UserControl;

namespace Dash
{
    class RadialSubmenuModel:RadialItemModel
    {
        public List<RadialItemModel> RadialItemList { get; }
        public bool IsMeter { get; set; }
        public MeterSubMenu MeterSubMenu { get; set; }

        /// <summary>
        /// Different types of actions that can be assigned and invoked.
        /// </summary>
        // Invoked when the meter value is selected
        public Action<double> MeterValueSelectionAction { get; set; }
        // Invoked when the submenu's center button is tapped
        public Action<object> CenterButtonTappedAction { get; set; }
        public Action<RadialMenuView> CenterButtonMenuModAction { get; set; }
        public Action<RadialMenuView> MenuModificationAction { get; set; }

        // Constructor for a submenu with an image
        public RadialSubmenuModel(string description, ImageSource iconSource, List<RadialItemModel> buttons)
        {
            Description = description;
            IconSource = iconSource;
            RadialItemList = buttons;
            BackGroundColor = Colors.Transparent;
            IsMeter = false;
            IsAction = false;
            IsDraggable = true;
            IsSubMenu = true;
        }

        // Constructor for a submenu with a string icon
        public RadialSubmenuModel(string description, string icon, List<RadialItemModel> buttons)
        {
            Description = description;
            Icon = icon;
            RadialItemList = buttons;
            BackGroundColor = Colors.Transparent;
            IsMeter = false;
            IsAction = false;
            IsDraggable = true;
            IsSubMenu = true;
        }

        public RadialSubmenuModel(string description, Symbol symbol, List<RadialItemModel> buttons)
        {
            Description = description;
            IconSymbol = symbol;
            RadialItemList = buttons;
            BackGroundColor = Colors.Transparent;
            IsMeter = false;
            IsAction = false;
            IsDraggable = true;
            IsSubMenu = true;
        }
    }
}
