using RadialMenuControl.Components;
using RadialMenuControl.UserControl;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
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
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using Dash.Models;
using Dash.StaticClasses;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class RadialMenuView : UserControl
    {
        public static RadialMenu MainMenu;
        private Canvas _parentCanvas;
        public static string RadialMenuDropKey = "A84862E6-34C3-44AC-A162-EE7DE702DAA0";
        public Symbol? PenInkSymbol = (Symbol) 0xEE56;
        public Symbol? PencilInkSymbol = (Symbol) 0xED63;
        public Symbol? EraserSymbol = (Symbol) 0xED60;
        public Symbol? SelectSymbol = (Symbol) 0xEF20;
        public Symbol? SetPen = (Symbol)0xEDC6;
        public Symbol? SetTouch = (Symbol) 0xED5F;
        public Symbol? SetMouse = (Symbol) 0xE962;
        public Symbol? Disable = Symbol.Clear;
        public Symbol? Input = (Symbol) 0xEDC6;
        public Symbol? InkMenuSymbol = (Symbol) 0xE76D;
        public Symbol? OperatorSymbol = (Symbol) 0xE8EF;
        public Symbol? CollectionSymbol = (Symbol) 0xE8B7;
        public Symbol? DocumentSymbol = (Symbol) 0xE160;
        public Symbol? FilePickerSymbol = Symbol.Add;
        public Symbol? SearchSymbol = Symbol.Find;
        public Symbol? NoteSymbol = Symbol.Page;

        /// <summary>
        /// Get or set the Diameter of the radial menu
        /// </summary>
        public double Diameter
        {
            get { return xRadialMenu.Diameter; }
            set
            {
                if (value > 0.15 * ApplicationView.GetForCurrentView().VisibleBounds.Width ||
                    value > 0.15 * ApplicationView.GetForCurrentView().VisibleBounds.Height)
                {
                    xRadialMenu.Diameter = value;
                }
            }
        }

        /// <summary>
        /// Get or set the start angle of buttons arranged in the radial menu
        /// </summary>
        public double StartAngle
        {
            get { return xRadialMenu.StartAngle; }
            set { xRadialMenu.StartAngle = value; }
        }

        /// <summary>
        /// Get or set the visibility of the radial menu
        /// </summary>
        public bool IsVisible
        {
            get
            {
                return xRadialMenu.Visibility == Visibility.Visible;
            }
            set
            {
                xRadialMenu.CenterButtonLeft = 95;
                xRadialMenu.CenterButtonTop = 95;
                if (!value)
                {
                    xRadialMenu.Collapse();
                }
                else
                {
                    xRadialMenu.Expand();
                }
            }
        }

        private List<RadialMenuButton> _inputButtons;

        private List<RadialMenuButton> _strokeButtons;
        private bool _inkOpened;

        /// <summary>
        /// Default radial menu with certain menu items
        /// </summary>
        /// <param name="canvas"></param>
        public RadialMenuView(Canvas canvas)
        {
            this.InitializeComponent();
            _parentCanvas = canvas;
            this.SetUpBaseMenu();
            Loaded += (sender, args) =>
            {
                InkSubMenu.MenuOpenedOrClosed += InkSubMenu_OnMenuOpenedOrClosed;
                CloseInkMenu();
            };
            _strokeButtons = new List<RadialMenuButton>
            {
                SetPenInkButton, SetPencilInkButton, SetEraserButton, SelectButton
            };
            _inputButtons = new List<RadialMenuButton>
            {
                SetPenInputButton, SetMouseInputButton, SetTouchInputButton, DisableButton
            };
        }


        /// <summary>
        /// Set up a radial menu with only a center button
        /// </summary>
        /// <param name="c"></param>
        private void SetUpBaseMenu()
        {
            _parentCanvas.Children.Add(this);
            SetDefaultMenuStyle();
            SetButtonActions();
        }

        public void JumpToPosition(double x, double y)
        {
            IsVisible = true;
            Floating.SetControlPosition(x + Diameter/2, y + Diameter/2);
        }

        /// <summary>
        /// Closes the slider panel on the left of the radial menu
        /// </summary>
        public void CloseInkMenu()
        {
            if (SettingsPane != null) SettingsPane.Visibility = Visibility.Collapsed;
            xRadialMenu.Margin = new Thickness(0, 0, 0, 0);
            Floating.ManipulateControlPosition(16, 23.5, 215, 215);
        }

        /// <summary>
        /// Opens the slider panel with a passed in header and an action to be called when the slider's value is set.
        /// Also adds a button below the slider with a currently hard-coded action: it sets the ink color to a shade of grey when pressed.
        /// </summary>
        /// <param name="header"></param>
        /// <param name="valueSetAction"></param>
        public void OpenInkMenu()
        {
            Point pos1 = Util.PointTransformFromVisual(new Point(107.5, 107.5), xRadialMenu, _parentCanvas);
            if (SettingsPane == null) FindName("SettingsPane");
            SettingsPane.Visibility = Visibility.Visible;
            xRadialMenu.Margin = new Thickness(32, 15, 0, 0);

            //Clicks the pen and ink buttons the first time they are loaded. hacky? yes.
            if (!_inkOpened)
            {
                DispatcherTimer timer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(300)};
                timer.Tick += delegate
                {
                    xRadialMenu.ClickInnerRadialMenuButton(SetPenInputButton);
                    xRadialMenu.ClickInnerRadialMenuButton(SetPenInkButton);
                    _inkOpened = true;
                    timer.Stop();
                };
                timer.Start();
            }
            Floating.ManipulateControlPosition(-32, -47, 294, 309);
        }

        /// <summary>
        /// Specify the default look of the radial menu
        /// </summary>
        private void SetDefaultMenuStyle()
        {
            xRadialMenu.Diameter = 215;
            xRadialMenu.StartAngle = 0;
            xRadialMenu.CenterButtonIcon = "🛠️";
            xRadialMenu.CenterButtonSymbol = (Symbol) 0xE115;
            xRadialMenu.CenterButtonBorder = new SolidColorBrush(Colors.Transparent);
            xRadialMenu.CenterButtonBackgroundFill = (SolidColorBrush) App.Instance.Resources["WindowsBlue"];
            xRadialMenu.CenterButtonForeground = new SolidColorBrush(Colors.Black);
            xRadialMenu.IndicationArcColor = ((SolidColorBrush)App.Instance.Resources["WindowsBlue"]).Color;
            xRadialMenu.UseIndicationArcs = true;
            xRadialMenu.IndicationArcStrokeThickness = 3;
            xRadialMenu.IndicationArcDistanceFromEdge = 20;
            xRadialMenu.InnerNormalColor = ((SolidColorBrush)App.Instance.Resources["TranslucentWhite"]).Color;
            xRadialMenu.InnerHoverColor = ((SolidColorBrush)App.Instance.Resources["SelectedGrey"]).Color;
            xRadialMenu.InnerTappedColor = ((SolidColorBrush)App.Instance.Resources["SelectedGrey"]).Color;
            xRadialMenu.InnerReleasedColor = ((SolidColorBrush) App.Instance.Resources["SelectedGrey"]).Color;
            xRadialMenu.OuterHoverColor = ((SolidColorBrush)App.Instance.Resources["SelectedGrey"]).Color;
            xRadialMenu.OuterNormalColor = ((SolidColorBrush)App.Instance.Resources["WindowsBlue"]).Color;
            xRadialMenu.OuterDisabledColor = ((SolidColorBrush) App.Instance.Resources["WindowsBlue"]).Color;
            xRadialMenu.OuterTappedColor = ((SolidColorBrush)App.Instance.Resources["SelectedGrey"]).Color;
            xRadialMenu.OuterThickness = 10;
            xRadialMenu.CenterButtonSize = 45;
        }
        

        /// <summary>
        /// Constructs a sample radial menu with buttons and submenus used to add elements to the main page and change the ink options.
        /// </summary>
        /// <param name="canvas"></param>
        private void SetButtonActions()
        {
            Action<object> choosePen = Actions.ChoosePen;
            Action<object> choosePencil = Actions.ChoosePencil;
            Action<object> chooseEraser = Actions.ChooseEraser;
            Action<object> toggleSelect = Actions.ToggleSelectionMode;
            var penInk = new RadialActionModel("", (Symbol) 0xEE56) {GenericAction = choosePen,
            };
            var pencilInk = new RadialActionModel("", (Symbol) 0xED63) {GenericAction = choosePencil,
            };
            var eraserInkModel = new RadialActionModel("", (Symbol) 0xED60) {GenericAction = chooseEraser,
            };
            var selectModel =
                new RadialActionModel("", (Symbol)0xEF20) { GenericAction = toggleSelect,
                };
            SetActionModel(penInk, SetPenInkButton);
            SetActionModel(pencilInk, SetPencilInkButton);
            SetActionModel(eraserInkModel, SetEraserButton);
            SetActionModel(selectModel, SelectButton);

            Action<object> setPenInput = Actions.SetPenInput;
            Action<object> setTouchInput = Actions.SetTouchInput;
            Action<object> setMouseInput = Actions.SetMouseInput;
            Action<object> setNoInput = Actions.SetNoInput;
            var setPenModel = new RadialActionModel("Pen", (Symbol) 0xEDC6)
            {
                GenericAction = setPenInput,
            };
            var setTouchModel = new RadialActionModel("Touch", (Symbol) 0xED5F)
            {
                GenericAction = setTouchInput,
            };
            var setMouseModel = new RadialActionModel("Mouse", (Symbol) 0xE962)
            {
                GenericAction = setMouseInput,
            };
            var disableModel = new RadialActionModel("Disable", Symbol.Clear)
            {
                GenericAction = setNoInput,
            };
            SetActionModel(setPenModel, SetPenInputButton);
            SetActionModel(setTouchModel, SetTouchInputButton);
            SetActionModel(setMouseModel, SetMouseInputButton);
            SetActionModel(disableModel, DisableButton);

            Action<ICollectionView, DragEventArgs> onImportDropped = Actions.OpenFilePickerForImport;
            Action<ICollectionView, DragEventArgs> onOperatorAdd = Actions.OnOperatorAdd;
            Action<ICollectionView, DragEventArgs> addCollection = Actions.AddCollection;
            Action<ICollectionView, DragEventArgs> addDocument = Actions.AddDocument;
            Action<ICollectionView, DragEventArgs> onSearchAdd = Actions.AddSearch;
            Action<ICollectionView, DragEventArgs> addNotes = Actions.AddNotes;
            var importModel =
                new RadialActionModel("", "") {CollectionDropAction = onImportDropped, IsDraggable = true};
            var operatorModel = new RadialActionModel("Operator", (Symbol)0xE8EF) { CollectionDropAction = onOperatorAdd, IsDraggable = true};
            var collectionModel = new RadialActionModel("Collection", (Symbol)0xE8B7) { CollectionDropAction = addCollection, IsDraggable = true};
            var documentModel = new RadialActionModel("Document", (Symbol)0xE160) {CollectionDropAction = addDocument, IsDraggable = true};
            var searchModel = new RadialActionModel("Search", Symbol.Find) { CollectionDropAction = onSearchAdd, IsDraggable = true };
            var notesModel = new RadialActionModel("Notes", Symbol.Page) { CollectionDropAction = addNotes, IsDraggable = true };
            SetActionModel(importModel, ImportButton);
            SetActionModel(operatorModel, Operator);
            SetActionModel(collectionModel, Collection);
            SetActionModel(documentModel, Document);
            SetActionModel(searchModel, SearchButton);
            SetActionModel(notesModel, NotesButton);
        }

        private void Ink_OnInnerArcPressed(object sender, PointerRoutedEventArgs e)
        {
            xRadialMenu.ChangeMenu(null, InkSubMenu);
        }

        private void SetActionModel(RadialItemModel item, RadialMenuButton button)
        {
            if (item.IsAction && item is RadialActionModel)
            {
                var action = button.ActionModel = (RadialActionModel) item;
                if (!action.IsDraggable) { button.Type = RadialMenuButton.ButtonType.Radio; }
                if (action.IsToggle)
                {
                    button.Type = RadialMenuButton.ButtonType.Toggle;
                }
            }
        }

        private void InkSubMenu_OnMenuOpenedOrClosed(bool isopen)
        {
            if(isopen) OpenInkMenu();
        }

        private void InkSubMenu_OnCenterButtonTapped(object sender, TappedRoutedEventArgs e)
        {
            CloseInkMenu();
        }
        

        private void RadialMenu_OnCenterButtonTapped(object sender, TappedRoutedEventArgs e)
        {
            if (xRadialMenu.Pie.Visibility == Visibility.Collapsed)
            {
                xRadialMenu.HorizontalAlignment = HorizontalAlignment.Left;
            }
            else
            {
                xRadialMenu.VerticalAlignment = VerticalAlignment.Center;
                xRadialMenu.HorizontalAlignment = HorizontalAlignment.Center;
            }
        }

        private void Input_OnInnerArcPressed(object sender, PointerRoutedEventArgs e)
        {
            xRadialMenu.ChangeMenu(sender, InputSubMenu);
        }
    }
}