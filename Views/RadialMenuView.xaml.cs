using RadialMenuControl.Components;
using RadialMenuControl.UserControl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
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
        private List<RadialItemModel> _colors;
        private StackPanel _sliderPanel;
        private Slider _slider;
        private TextBlock _sliderHeader;
        private StackPanel _stackPanel;
        private Floating _floatingMenu;
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

        /// <summary>
        /// Get or set the Diameter of the radial menu
        /// </summary>
    public double Diameter
        {
            get { return RadialMenu.Diameter; }
            set
            {
                if (value > 0.15 * ApplicationView.GetForCurrentView().VisibleBounds.Width ||
                    value > 0.15 * ApplicationView.GetForCurrentView().VisibleBounds.Height)
                {
                    RadialMenu.Diameter = value;
                }
            }
        }

        /// <summary>
        /// Get or set the start angle of buttons arranged in the radial menu
        /// </summary>
        public double StartAngle
        {
            get { return RadialMenu.StartAngle; }
            set { RadialMenu.StartAngle = value; }
        }

        /// <summary>
        /// Get or set the visibility of the radial menu
        /// </summary>
        public bool IsVisible
        {
            get
            {
                return RadialMenu.Visibility == Visibility.Visible;
            }
            set
            {
                RadialMenu.CenterButtonLeft = 95;
                RadialMenu.CenterButtonTop = 95;
                if (!value)
                {
                    RadialMenu.Collapse();
                }
                else
                {
                    RadialMenu.Expand();
                }
            }
        }

        private MeterSubMenu _strokeMeter;
        private MeterSubMenu _opacityMeter;
        private InkSettingsPane _settingsPane;

        /// <summary>
        /// Default radial menu with certain menu items
        /// </summary>
        /// <param name="canvas"></param>
        public RadialMenuView(Canvas canvas)
        {
            this.InitializeComponent();
            _parentCanvas = canvas;
            this.SetUpBaseMenu();
            Loaded += (sender, args) => InkSubMenu.MenuOpenedOrClosed += InkSubMenu_OnMenuOpenedOrClosed;
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
            SettingsPane.Visibility = Visibility.Collapsed;
            ColorPicker.Visibility = Visibility.Collapsed;
            Grid.Height = 250;
            Column0.Width = new GridLength(250);
            Column1.Width = new GridLength(0);
            Grid.Width = 250;
        }

        /// <summary>
        /// Opens the slider panel with a passed in header and an action to be called when the slider's value is set.
        /// Also adds a button below the slider with a currently hard-coded action: it sets the ink color to a shade of grey when pressed.
        /// </summary>
        /// <param name="header"></param>
        /// <param name="valueSetAction"></param>
        public void OpenInkMenu()
        {
            SettingsPane.Visibility = Visibility.Visible;
            ColorPicker.Visibility = Visibility.Visible;
            Grid.Height = 327;
            Column0.Width = new GridLength(327);
            Column1.Width = GridLength.Auto;
            Grid.Width = double.NaN;
        }

        /// <summary>
        /// Specify the default look of the radial menu
        /// </summary>
        private void SetDefaultMenuStyle()
        {
            RadialMenu.Diameter = 250;
            RadialMenu.StartAngle = 0;
            RadialMenu.CenterButtonIcon = "🛠️";
            RadialMenu.CenterButtonSymbol = (Symbol) 0xE115;
            RadialMenu.CenterButtonBorder = new SolidColorBrush(Colors.Transparent);
            RadialMenu.CenterButtonBackgroundFill = (SolidColorBrush) App.Instance.Resources["WindowsBlue"];
            RadialMenu.CenterButtonForeground = new SolidColorBrush(Colors.Black);
            RadialMenu.IndicationArcColor = ((SolidColorBrush)App.Instance.Resources["WindowsBlue"]).Color;
            RadialMenu.UseIndicationArcs = true;
            RadialMenu.IndicationArcStrokeThickness = 3;
            RadialMenu.IndicationArcDistanceFromEdge = 20;
            RadialMenu.InnerNormalColor = ((SolidColorBrush)App.Instance.Resources["TranslucentWhite"]).Color;
            RadialMenu.InnerHoverColor = ((SolidColorBrush)App.Instance.Resources["SelectedGrey"]).Color;
            RadialMenu.InnerTappedColor = ((SolidColorBrush)App.Instance.Resources["SelectedGrey"]).Color;
            RadialMenu.InnerReleasedColor = ((SolidColorBrush) App.Instance.Resources["SelectedGrey"]).Color;
            RadialMenu.OuterHoverColor = ((SolidColorBrush)App.Instance.Resources["SelectedGrey"]).Color;
            RadialMenu.OuterNormalColor = ((SolidColorBrush)App.Instance.Resources["WindowsBlue"]).Color;
            RadialMenu.OuterDisabledColor = ((SolidColorBrush) App.Instance.Resources["WindowsBlue"]).Color;
            RadialMenu.OuterTappedColor = ((SolidColorBrush)App.Instance.Resources["SelectedGrey"]).Color;
            RadialMenu.OuterThickness = 15;
            RadialMenu.CenterButtonSize = 60;
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
            Action<object> toggleInkRecognition = Actions.ToggleInkRecognition;
            var penInk = new RadialActionModel("", (Symbol) 0xEE56) {GenericAction = choosePen};
            var pencilInk = new RadialActionModel("", (Symbol) 0xED63) {GenericAction = choosePencil};
            var eraserInkModel = new RadialActionModel("", (Symbol) 0xED60) {GenericAction = chooseEraser};
            //var toggleInkRecognitionModel = new RadialActionModel("", (Symbol) 0xE945) {GenericAction = toggleInkRecognition, IsToggle = true};
            var selectModel =
                new RadialActionModel("", (Symbol)0xEF20) { GenericAction = toggleSelect};
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
                GenericAction = setPenInput
            };
            var setTouchModel = new RadialActionModel("Touch", (Symbol) 0xED5F)
            {
                GenericAction = setTouchInput
            };
            var setMouseModel = new RadialActionModel("Mouse", (Symbol) 0xE962)
            {
                GenericAction = setMouseInput
            };
            var disableModel = new RadialActionModel("Disable", Symbol.Clear)
            {
                GenericAction = setNoInput
            };
            SetActionModel(setPenModel, SetPenInputButton);
            SetActionModel(setTouchModel, SetTouchInputButton);
            SetActionModel(setMouseModel, SetMouseInputButton);
            SetActionModel(disableModel, DisableButton);

            Action<ICollectionView, DragEventArgs> onOperatorAdd = Actions.OnOperatorAdd;
            Action<ICollectionView, DragEventArgs> addCollection = Actions.AddCollection;
            Action<ICollectionView, DragEventArgs> addDocument = Actions.AddDocument;
            var operatorModel = new RadialActionModel("Operator", (Symbol)0xE8EF) { CollectionDropAction = onOperatorAdd, IsDraggable = true};
            var collectionModel = new RadialActionModel("Collection", (Symbol)0xE8B7) { CollectionDropAction = addCollection, IsDraggable = true};
            var documentModel = new RadialActionModel("Document", (Symbol)0xE160) {CollectionDropAction = addDocument, IsDraggable = true};
            SetActionModel(operatorModel, Operator);
            SetActionModel(collectionModel, Collection);
            SetActionModel(documentModel, Document);
        }

        private void Ink_OnInnerArcPressed(object sender, PointerRoutedEventArgs e)
        {
            RadialMenu.ChangeMenu(null, InkSubMenu);
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

        private void InputSubMenuButton_OnInnerArcReleased(object sender, PointerRoutedEventArgs e)
        {
            RadialMenu.ChangeMenu(null, InputSubMenu);
        }

        private void InkSubMenu_OnMenuOpenedOrClosed(bool isopen)
        {
            if(isopen) OpenInkMenu();
            else CloseInkMenu();
        }

        private void InputSubMenu_OnCenterButtonTapped(object sender, TappedRoutedEventArgs e)
        {
            OpenInkMenu();
        }

        private void InkSubMenu_OnCenterButtonTapped(object sender, TappedRoutedEventArgs e)
        {
            CloseInkMenu();
        }
    }
}
