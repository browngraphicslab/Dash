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
using Windows.UI.Text;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using Dash.Models;
using Dash.StaticClasses;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views
{
    public sealed partial class RadialMenuView : UserControl
    {
        private RadialMenu _mainMenu;
        private Canvas _parentCanvas;
        private List<RadialItemModel> _colors;
        private StackPanel _sliderPanel;
        private Slider _slider;
        private TextBlock _sliderHeader;
        private StackPanel _stackPanel;
        private Floating _floatingMenu;

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
                _mainMenu.CenterButtonLeft = 95;
                _mainMenu.CenterButtonTop = 95;
                if (!value)
                {
                    _mainMenu.Collapse();
                }
                else
                {
                    _mainMenu.Expand();
                }
            }
        }

        /// <summary>
        /// Default radial menu with certain menu items
        /// </summary>
        /// <param name="canvas"></param>
        public RadialMenuView(Canvas canvas)
        {
            this.InitializeComponent();
            _parentCanvas = canvas;
            _colors = new List<RadialItemModel>();
            this.SetUpBaseMenu();
            //_parentCanvas.OnDoubleTapped += Overlay_DoubleTapped;
            this.SampleRadialMenu(canvas);
        }

        /// <summary>
        /// Blank radial menu with no default menu items
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="Diameter"></param>
        public RadialMenuView(Canvas canvas, double Diameter)
        {
            this.InitializeComponent();
            _parentCanvas = canvas;
            this.SetUpBaseMenu();
            //_parentCanvas.OnDoubleTapped += Overlay_DoubleTapped;
        }


        /// <summary>
        /// Set up a radial menu with only a center button
        /// </summary>
        /// <param name="c"></param>
        private void SetUpBaseMenu()
        {
            var border = new Border();
            _floatingMenu = new Floating
            {
                IsBoundByParent = true,
                IsBoundByScreen = true,
                Content = border
            };
            _parentCanvas.Children.Add(_floatingMenu);
            var grid = new Grid();
            border.Child = grid;
            _stackPanel = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
            };
            
            grid.Children.Add(_stackPanel);

            _mainMenu = new RadialMenu();
            SetDefaultMenuStyle();
            _sliderPanel = new StackPanel()
            {
                Orientation = Orientation.Vertical,
                Visibility = Visibility.Collapsed,
                Background = (SolidColorBrush)App.Instance.Resources["TranslucentWhite"],
                BorderBrush = (SolidColorBrush) App.Instance.Resources["WindowsBlue"],
                BorderThickness = new Thickness(2,2,2,2),
                Margin = new Thickness(0,0,5,0),
                Padding = new Thickness(3,3,3,3)
            };
            _stackPanel.Children.Add(_sliderPanel);
            _stackPanel.Children.Add(_mainMenu);

        }

        public void JumpToPosition(double x, double y)
        {
            IsVisible = true;
            _floatingMenu.SetControlPosition(x - 15 - Diameter/2, y - 15 - Diameter/2);
        }

        /// <summary>
        /// Closes the slider panel on the left of the radial menu
        /// </summary>
        public void CloseSlider()
        {
            _sliderPanel.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Opens the slider panel with a passed in header and an action to be called when the slider's value is set.
        /// Also adds a button below the slider with a currently hard-coded action: it sets the ink color to a shade of grey when pressed.
        /// </summary>
        /// <param name="header"></param>
        /// <param name="valueSetAction"></param>
        public void OpenSlider(string header, Action<double, RadialMenu> valueSetAction)
        {
            _sliderPanel.Children.Clear();
            _sliderHeader = new TextBlock()
            {
                Text = header, HorizontalAlignment = HorizontalAlignment.Center,
                FontStyle = FontStyle.Normal,
            };
            _slider = new Slider()
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                Minimum = 0,
                Height = 200,
                Maximum = 100,
            };
            _slider.ValueChanged += delegate (object sender, RangeBaseValueChangedEventArgs args)
            {
                valueSetAction.Invoke(_slider.Value, _mainMenu);
            };
            _slider.Value = 50;
            Ellipse grey = new Ellipse()
            {
                Width = 20,
                Height = 20,
                Fill = new SolidColorBrush(Colors.DarkGray)
            };
            Button blackButton = new Button()
            {
                Content = grey,
                FontSize = 12,
                Padding = new Thickness(3,3,3,3),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0,3,6,0),
                Background = new SolidColorBrush(Colors.Transparent)
            };
            blackButton.Tapped += delegate(object sender, TappedRoutedEventArgs args) { Actions.ChangeInkColor(Colors.Gray, _mainMenu); };
            _sliderPanel.Children.Add(_sliderHeader);
            _sliderPanel.Children.Add(_slider);
            _sliderPanel.Children.Add(blackButton);
            _sliderPanel.Visibility = Visibility.Visible;
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
            _mainMenu.CenterButtonBackgroundFill = (SolidColorBrush) App.Instance.Resources["WindowsBlue"];
            _mainMenu.CenterButtonForeground = new SolidColorBrush(Colors.Black);
            _mainMenu.IndicationArcColor = ((SolidColorBrush)App.Instance.Resources["WindowsBlue"]).Color;
            _mainMenu.UseIndicationArcs = true;
            _mainMenu.IndicationArcStrokeThickness = 3;
            _mainMenu.IndicationArcDistanceFromEdge = 20;
            _mainMenu.InnerNormalColor = ((SolidColorBrush)App.Instance.Resources["TranslucentWhite"]).Color;
            _mainMenu.InnerHoverColor = ((SolidColorBrush)App.Instance.Resources["SelectedGrey"]).Color;
            _mainMenu.InnerTappedColor = ((SolidColorBrush)App.Instance.Resources["SelectedGrey"]).Color;
            _mainMenu.InnerReleasedColor = ((SolidColorBrush) App.Instance.Resources["SelectedGrey"]).Color;
            _mainMenu.OuterHoverColor = ((SolidColorBrush)App.Instance.Resources["SelectedGrey"]).Color;
            _mainMenu.OuterNormalColor = ((SolidColorBrush)App.Instance.Resources["WindowsBlue"]).Color;
            _mainMenu.OuterDisabledColor = ((SolidColorBrush) App.Instance.Resources["WindowsBlue"]).Color;
            _mainMenu.OuterTappedColor = ((SolidColorBrush)App.Instance.Resources["SelectedGrey"]).Color;
            _mainMenu.OuterThickness = 15;
            _mainMenu.CenterButtonSize = 60;
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
                FontFamily = new FontFamily("Century Gothic"),
                IconForegroundBrush = (SolidColorBrush)App.Instance.Resources["AccentGreen"],
                IconFontFamily = new FontFamily("Segoe UI Symbol"),
                IconSize = 5,
            };
            if (item.IconSource != null)
            {
                button.IconImage = item.IconSource;
            }
            else if (item.Icon != null)
            {
                button.Icon = item.Icon;
            }
            //Construct the color wheel buttons
            if (item.BackGroundColor != Colors.Transparent)
            {
                button.InnerNormalColor = item.BackGroundColor;
                button.OuterThickness = 0;
                button.StrokeColor = item.BackGroundColor;
                button.StrokeThickness = 1;
                button.InnerReleasedColor = Colors.AliceBlue;
            }
            else
            {
                button.InnerNormalColor = ((SolidColorBrush)App.Instance.Resources["TranslucentWhite"]).Color;
                button.OuterDisabledColor = ((SolidColorBrush)App.Instance.Resources["WindowsBlue"]).Color;
            }
            //Construct the associated meter, if there is one
            if ((item as RadialSubmenuModel)?.IsMeter == true)
            {
                MeterSubMenu meter = ((RadialSubmenuModel)item).MeterSubMenu;
                button.CustomMenu = meter;
                meter.ValueSelected += delegate(object sender, PointerRoutedEventArgs args)
                {
                    ((RadialSubmenuModel) item).MeterValueSelectionAction?.Invoke(meter.SelectedValue);
                };
                button.InnerArcReleased += delegate(object sender, PointerRoutedEventArgs args) { _mainMenu.ChangeMenu(null, meter); };
            }
            //Add the button model's actions as invoked actions when the button is pressed then released
            if (item.IsAction && item is RadialActionModel)
            {
                var actionButton = item as RadialActionModel;
                button.InnerArcReleased += delegate(object sender, PointerRoutedEventArgs e)
                {
                    actionButton.CanvasAction?.Invoke(_parentCanvas, e.GetCurrentPoint(_parentCanvas).Position);
                    if (button.InnerNormalColor != null)
                        actionButton.ColorAction?.Invoke(button.InnerNormalColor.Value, _mainMenu);
                    actionButton.GenericAction?.Invoke(null);
                    
                };
                
            } 
            menu.AddButton(button);
            return button;
        }

        /// <summary>
        /// Recursive method to create submenu(s) within submenu(s) by taking in a RaidialSubMenu object and a button to add the submenu to
        /// </summary>
        /// <param name="subMenu"></param>
        /// <param name="subMenuButton"></param>
        private void AddSubMenu(RadialSubmenuModel subMenu, RadialMenuButton subMenuButton)
        {
            // create new radialmenu to be added to a button
            var menu = new RadialMenu();
            menu.IsDraggable = subMenu.IsDraggable;
            var items = subMenu.RadialItemList;
            if (items != null)
            {
                foreach (var item in items)
                {
                    var button = this.AddButton(item, menu);
                    // if the item is another submenu, call this method on the submenu and the newly added button
                    if (!item.IsAction)
                    {
                        this.AddSubMenu(item as RadialSubmenuModel, button);
                    }
                }
                subMenuButton.InnerArcReleased += delegate (object sender, PointerRoutedEventArgs args)
                {
                    _mainMenu.ChangeMenu(null, menu);
                    subMenu.MenuModificationAction?.Invoke(this);
                };
                subMenuButton.OuterArcPressed += delegate (object sender, PointerRoutedEventArgs args)
                {
                    //_mainMenu.ChangeMenu(null, menu);
                    subMenu.MenuModificationAction?.Invoke(this);
                };
                menu.CenterButtonTapped += delegate (object sender, TappedRoutedEventArgs args)
                {
                    subMenu.CenterButtonTappedAction?.Invoke(null);
                    subMenu.CenterButtonMenuModAction?.Invoke(this);
                };

                subMenuButton.Submenu = menu;

            }
            
        }

        /// <summary>
        /// Constructs a sample radial menu with buttons and submenus used to add elements to the main page and change the ink options.
        /// </summary>
        /// <param name="canvas"></param>
        private void SampleRadialMenu(Canvas canvas)
        {
            

            #region Ink Controls

            Action<object> choosePen = Actions.ChoosePen;
            Action<object> choosePencil = Actions.ChoosePencil;
            Action<double> setOpacity = Actions.SetOpacity;
            Action<double> setSize = Actions.SetSize;
            Action<RadialMenuView> displayBrightnessSlider = Actions.DisplayBrightnessSlider;
            Action<RadialMenuView> closeSliderPanel = Actions.CloseSliderPanel;
            Action<object> setPenInput = Actions.SetPenInput;
            Action<object> setTouchInput = Actions.SetTouchInput;
            Action<object> setMouseInput = Actions.SetMouseInput;
            Action<object> setNoInput = Actions.SetNoInput;
            this.InitializeColors();

            var strokeMeter = new RadialSubmenuModel("Stroke Size", "〰", null)
            {
                IsMeter = true,
                MeterSubMenu = MakeMeterSubMenu(24, 2),
                MeterValueSelectionAction = setSize
            };

            var opacityMeter = new RadialSubmenuModel("Opacity", "💧", null)
            {
                IsMeter = true,
                MeterSubMenu = MakeMeterSubMenu(1, 0.2),
                MeterValueSelectionAction = setOpacity
            };

            var strokeTypeMenu = new RadialSubmenuModel("Pen Type", "✍️",
                new List<RadialItemModel>
                {
                    new RadialActionModel("Pen", "✒️")
                    {
                        GenericAction = choosePen
                    },
                    new RadialActionModel("Pencil", "✏️")
                    {
                        GenericAction = choosePencil
                    }
                });

            var inkPalette = new RadialSubmenuModel("Color Palette", "🎨", _colors)
            {
                IsDraggable = false,
                MenuModificationAction = displayBrightnessSlider,
                CenterButtonMenuModAction = closeSliderPanel
            };

            var inputTypeMenu = new RadialSubmenuModel("Input Type", "⬇️", new List<RadialItemModel>()
            {
                new RadialActionModel("Pen", "🖊️")
                {
                    GenericAction = setPenInput
                },
                new RadialActionModel("Touch", "☝️")
                {
                    GenericAction = setTouchInput
                },
                new RadialActionModel("Mouse", "🖱️")
                {
                    GenericAction = setMouseInput
                },
                new RadialActionModel("None", "❎")
                {
                    GenericAction = setNoInput
                }

            });


            var inkOptions = new RadialSubmenuModel("Ink Options", "🖌️", new List<RadialItemModel>
            {
                strokeTypeMenu,
                strokeMeter,
                opacityMeter,
                inkPalette,
                inputTypeMenu
            });


            #endregion

            Action<Canvas, Point> addSearch = Actions.AddSearch;
            var searchButton = new RadialActionModel("Search", "🔍")
            {
                CanvasAction = addSearch
            };



            Action<object> onOperatorAdd = Actions.OnOperatorAdd;
            Action<object> addCollection = Actions.AddCollection;
            Action<object> addApiCreator = Actions.AddApiCreator;
            Action<object> addDocuments = Actions.AddDocuments;
            
            var operatorButton = new RadialActionModel("Operator", "↔️") { GenericAction = onOperatorAdd };
            var collectionButton = new RadialActionModel("Collection", "📁") { GenericAction = addCollection };
            var apiButton = new RadialActionModel("Api", "⚙️") { GenericAction = addApiCreator };
            var documentButton = new RadialActionModel("Document", "📄") { GenericAction = addDocuments };

            var addOptionsMenu = new RadialSubmenuModel("Add", "+", new List<RadialItemModel>
            {
                operatorButton,
                apiButton,
                documentButton,
                collectionButton,
            });
            

            AddItems(new List<RadialItemModel>
            {
                searchButton,
                inkOptions,
                addOptionsMenu
                
            });

            
        }

        private void InitializeColors()
        {
            AddColorRange(Colors.Red, Colors.Violet);
            AddColorRange(Colors.Violet, Colors.Blue);
            AddColorRange(Colors.Blue, Colors.Aqua);
            AddColorRange(Colors.Aqua, Colors.Green);
            AddColorRange(Colors.Green, Colors.Yellow);
            AddColorRange(Colors.Yellow,Colors.Red);
        }

        private void AddColorRange(Color color1, Color color2, int size=13)
        {
            int r1 = color1.R;
            int rEnd = color2.R;
            int b1 = color1.B;
            int bEnd = color2.B;
            int g1 = color1.G;
            int gEnd = color2.G;
            for (byte i = 0; i < size; i++)
            {
                var rAverage = r1 + (int)((rEnd - r1) * i / size);
                var gAverage = g1 + (int)((gEnd - g1) * i / size);
                var bAverage = b1 + (int)((bEnd - b1) * i / size);
                var button = new RadialActionModel("","");
                button.BackGroundColor = Color.FromArgb(255, (byte) rAverage, (byte) gAverage, (byte) bAverage);
                button.ColorAction = Actions.ChangeInkColor;
                _colors.Add(button);
            }
        }

        /// <summary>
        /// Constructs a meter submenu with a range from 0 to the length parameter and intervals of length "interval" between ticks 
        /// </summary>
        /// <param name="length"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        private MeterSubMenu MakeMeterSubMenu(double length, double interval)
        {
            var meter = new MeterSubMenu()
            {
                MeterRadius = 55,
                CenterButtonBorder = new SolidColorBrush(Colors.Black),
                MeterStartValue = 0,
                MeterEndValue = length,
                MeterPointerLength = 70,
                Intervals = new List<MeterRangeInterval>()
                {
                    new MeterRangeInterval
                    {
                       StartValue = 0,
                       EndValue = length,
                       TickInterval = interval,
                       StartDegree = 0,
                       EndDegree = 300,
                    }
                },
                StartAngle = 0,
                RoundSelectValue = false,
                SelectedValueColor = new SolidColorBrush(Colors.Black),
                OuterEdgeBrush = ((SolidColorBrush)App.Instance.Resources["WindowsBlue"]),
                BackgroundFillBrush = (SolidColorBrush)App.Instance.Resources["TranslucentWhite"],
                SelectedValueTextColor = new SolidColorBrush(Colors.Black),
                MeterLineColor = new SolidColorBrush(Colors.Black),
                HoverValueColor = new SolidColorBrush(Colors.Black)
            };
            return meter;
        }
    }
}
