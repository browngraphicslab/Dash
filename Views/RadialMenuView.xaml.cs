using RadialMenuControl.Components;
using RadialMenuControl.UserControl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Dash.Models;
using Dash.StaticClasses;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views
{
    public sealed partial class RadialMenuView : UserControl
    {
        private RadialMenu _mainMenu;
        private OverlayCanvas _parentCanvas;

        /// <summary>
        /// Get or set the Diameter of the radial menu
        /// </summary>
        public double Diameter
        {
            get { return _mainMenu.Diameter; }
            set
            {
                if (value > 0.15 * ApplicationView.GetForCurrentView().VisibleBounds.Width ||
                    value > 0.15 * ApplicationView.GetForCurrentView().VisibleBounds.Height)
                {
                    _mainMenu.Diameter = value;
                }
            }
        }

        /// <summary>
        /// Get or set the start angle of buttons arranged in the radial menu
        /// </summary>
        public double StartAngle
        {
            get { return _mainMenu.StartAngle; }
            set { _mainMenu.StartAngle = value; }
        }

        /// <summary>
        /// Get or set the visibility of the radial menu
        /// </summary>
        public bool IsVisible
        {
            get
            {
                return _mainMenu.Visibility == Visibility.Visible;
            }
            set
            {
                _mainMenu.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Default radial menu with certain menu items
        /// </summary>
        /// <param name="canvas"></param>
        public RadialMenuView(OverlayCanvas canvas)
        {
            this.InitializeComponent();
            _parentCanvas = canvas;
            this.SetUpBaseMenu();
            _parentCanvas.OnDoubleTapped += Overlay_DoubleTapped;
            this.SampleRadialMenu(canvas);
        }

        /// <summary>
        /// Blank radial menu with no default menu items
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="Diameter"></param>
        public RadialMenuView(OverlayCanvas canvas, double Diameter)
        {
            this.InitializeComponent();
            _parentCanvas = canvas;
            this.SetUpBaseMenu();
            _parentCanvas.OnDoubleTapped += Overlay_DoubleTapped;
        }

        private void Overlay_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
//            // can't set position
//            Canvas.SetLeft(_mainMenu, 500);
//            Canvas.SetTop(_mainMenu, 250);
            _mainMenu.Visibility = _mainMenu.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }


        /// <summary>
        /// Set up a radial menu with only a center button
        /// </summary>
        /// <param name="c"></param>
        private void SetUpBaseMenu()
        {
            var floatingMenu = new Floating();
            _parentCanvas.Children.Add(floatingMenu);
            floatingMenu.IsBoundByParent = true;
            floatingMenu.IsBoundByScreen = true;
            var border = new Border();
            floatingMenu.Content = border;
            var grid = new Grid();
            border.Child = grid;
            _mainMenu = new RadialMenu();
            grid.Children.Add(_mainMenu);
            _mainMenu.TogglePie();
            _mainMenu.Visibility = Visibility.Collapsed;

            SetDefaultMenuStyle();
        }

        /// <summary>
        /// Specify the default look of the radial menu
        /// </summary>
        private void SetDefaultMenuStyle()
        {
            _mainMenu.Diameter = 250;
            _mainMenu.StartAngle = 0;
            _mainMenu.CenterButtonIcon = "🛠️";
            _mainMenu.CenterButtonBorder = new SolidColorBrush(Colors.Transparent);
            _mainMenu.CenterButtonBackgroundFill = new SolidColorBrush(Colors.DarkSlateBlue);
            _mainMenu.CenterButtonForeground = new SolidColorBrush(Colors.Black);
            _mainMenu.InnerNormalColor = Colors.LightSteelBlue;
            _mainMenu.InnerHoverColor = Colors.AliceBlue;
            _mainMenu.InnerTappedColor = Colors.LightSteelBlue;
            _mainMenu.InnerReleasedColor = Colors.AliceBlue;
            _mainMenu.OuterHoverColor = Colors.SteelBlue;
            _mainMenu.OuterNormalColor = Colors.Black;
            _mainMenu.OuterDisabledColor = Colors.DarkSlateBlue;
            _mainMenu.OuterTappedColor = Colors.LightSteelBlue;
            _mainMenu.OuterThickness = 15;
            _mainMenu.UseIndicationArcs = true;
        }

        /// <summary>
        /// Takes in a list of RadialItems and add them to the main menu as appropriate buttons
        /// </summary>
        /// <param name="items"></param>
        public void AddItems(List<RadialItemModel> items)
        {
            foreach (var item in items)
            {
                var button = this.AddButton(item, _mainMenu);
                if (!item.IsAction)
                {
                    this.AddSubMenu(item as RadialSubmenuModel, button);
                }
            }
        }

        /// <summary>
        /// Adds a button to the main menu, button could be an action or a submenu
        /// </summary>
        /// <param name="item"></param>
        private RadialMenuButton AddButton(RadialItemModel item, RadialMenu menu)
        {
            var button = new RadialMenuButton()
            {
                Label = item.Description,
                IconImage = item.IconSource,
                FontFamily = new FontFamily("Century Gothic"),
                IconFontFamily = new FontFamily("Segoe UI Symbol"),
                IconSize = 5,
                IconForegroundBrush = new SolidColorBrush(Colors.WhiteSmoke),
            };
            if (item.IconSource != null)
            {
                button.IconImage = item.IconSource;
            }
            else if (item.Icon != null)
            {
                button.Icon = item.Icon;
            }
            button.InnerArcPressed += delegate (object sender, PointerRoutedEventArgs e)
            {
                if (item.IsAction)
                {
                    var actionButton = item as RadialActionModel;
                    if (actionButton?.Action != null)
                    {
                        actionButton?.Action(_parentCanvas, e.GetCurrentPoint(_parentCanvas).Position);
                        // collapse menu when option is chosen
                        _mainMenu.Visibility = Visibility.Collapsed;
                    }
                }
            };
            //            button.IndicationArcDistanceFromEdge = 30;
            //            button.IndicationArcColor = Colors.SteelBlue;
            //            button.IndicationArcStrokeThickness = 5;
            menu.AddButton(button);
            return button;
        }

        /// <summary>
        /// Recursive method to create submenu(s) within submenu(s) by taking in a RaidialSubMenu object and a button to add the submenu to
        /// </summary>
        /// <param name="subMenu"></param>
        private void AddSubMenu(RadialSubmenuModel subMenu, RadialMenuButton subMenuButton)
        {
            // create new radialmenu to be added to a button
            var menu = new RadialMenu();

            var items = subMenu.RadialItemList;
            foreach (var item in items)
            {
                var button = this.AddButton(item, menu);

                // if the item is another submenu, call this method on the submenu and the newly added button
                if (!item.IsAction)
                {
                    this.AddSubMenu(item as RadialSubmenuModel, button);
                }
            }
            subMenuButton.Submenu = menu;
        }

        private void SampleRadialMenu(OverlayCanvas canvas)
        {
            Action<OverlayCanvas, Point> addSearch = Actions.AddSearch;
            Action<OverlayCanvas, Point> addPalette = Actions.AddPalette;
            this.AddItems(new List<RadialItemModel>()
            {new RadialActionModel("Search", "🔍", addSearch),new RadialActionModel("Colors","🎨", addPalette)});
        }
    }
}
