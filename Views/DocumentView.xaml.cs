using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Dash.Views;
using Windows.UI.Xaml.Shapes;
using DashShared;
using Visibility = Windows.UI.Xaml.Visibility;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236
namespace Dash
{

    public sealed partial class DocumentView : SelectionElement
    {
        public CollectionView ParentCollection; // TODO document views should not be assumed to be in a collection this!
        public bool IsMainCollection { get; set; } //TODO document views should not be aware of if they are the main collection!
        /// <summary>
        /// Contains methods which allow the document to be moved around a free form canvas
        /// </summary>
        private ManipulationControls manipulator;

        private OverlayMenu _docMenu;
        public DocumentViewModel ViewModel { get; set; }

        public bool ProportionalScaling { get; set; }
        public ManipulationControls Manipulator { get { return manipulator; } }


        public static int dvCount = 0;
        public DocumentView()
        {
            InitializeComponent();
            Util.InitializeDropShadow(xShadowHost, xShadowTarget);

            DataContextChanged += DocumentView_DataContextChanged;

            // add manipulation code
            manipulator = new ManipulationControls(this, true, true);
            manipulator.OnManipulatorTranslatedOrScaled += ManipulatorOnManipulatorTranslatedOrScaled;

            // set bounds
            MinWidth = 100;
            MinHeight = 25;
            
            Loaded += This_Loaded;
            Unloaded += This_Unloaded;
        }

        public DocumentView(DocumentViewModel documentViewModel) : this()
        {
            DataContext = documentViewModel;
        }

        private void This_Unloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"Unloaded: Num DocViews = {--dvCount}");
            DraggerButton.Holding -= DraggerButtonHolding;
            DraggerButton.ManipulationDelta -= Dragger_OnManipulationDelta;
            DraggerButton.ManipulationCompleted -= Dragger_ManipulationCompleted;
            DoubleTapped -= ExpandContract_DoubleTapped;
            //Loaded -= This_Loaded;
            //Unloaded -= This_Unloaded;
        }


        private void This_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"Loaded: Num DocViews = {++dvCount}");
            DraggerButton.Holding += DraggerButtonHolding;
            DraggerButton.ManipulationDelta += Dragger_OnManipulationDelta;
            DraggerButton.ManipulationCompleted += Dragger_ManipulationCompleted;
            DoubleTapped += ExpandContract_DoubleTapped;

            ParentCollection = this.GetFirstAncestorOfType<CollectionView>();
            if (ViewModel != null)
            {
                ViewModel.Width = ActualWidth;
                ViewModel.Height = ActualHeight;
            }
            DraggerButton.Holding += DraggerButtonHolding;
            DraggerButton.ManipulationDelta += Dragger_OnManipulationDelta;
            DraggerButton.ManipulationCompleted += Dragger_ManipulationCompleted;
            DoubleTapped += ExpandContract_DoubleTapped;
        }


        /// <summary>
        /// When a field is dragged onto documentview, adds that field to the document 
        /// </summary>
        //private void OuterGrid_PointerReleased(object sender, PointerRoutedEventArgs args)
        //{

            //var view = OuterGrid.GetFirstAncestorOfType<CollectionFreeformView>();
            //if (view == null) return; // we can't always assume we're on a collection		

            //view.CanLink = false;
            //args.Handled = true;

            //view.CancelDrag(args.Pointer); 

            //view?.EndDragOnDocumentView(ref ViewModel.DocumentController,
            //    new IOReference(null, null, new DocumentFieldReference(ViewModel.DocumentController.DocumentModel.Id, KeyStore.DataKey), false, args, OuterGrid,
            //        OuterGrid.GetFirstAncestorOfType<DocumentView>()));

        //}

        private void SetUpMenu()
        {
            Color bgcolor = (Application.Current.Resources["WindowsBlue"] as SolidColorBrush).Color;

            var documentButtons = new List<MenuButton>
            {
                new MenuButton(Symbol.Pictures, "Layout",bgcolor,OpenLayout),
                new MenuButton(Symbol.Copy, "Copy",bgcolor,CopyDocument),
                new MenuButton(Symbol.SetTile, "Delegate",bgcolor, MakeDelegate),
                new MenuButton(Symbol.Delete, "Delete",bgcolor,DeleteDocument),
                new MenuButton(Symbol.Camera, "ScrCap",bgcolor, ScreenCap),
                new MenuButton(Symbol.Placeholder, "Commands",bgcolor, CommandLine)
            };
            _docMenu = new OverlayMenu(null, documentButtons);
            Binding visibilityBinding = new Binding
            {
                Source = ViewModel,
                Path = new PropertyPath(nameof(ViewModel.DocMenuVisibility)),
                Mode = BindingMode.OneWay
            };
            _docMenu.SetBinding(VisibilityProperty, visibilityBinding);

            xMenuCanvas.Children.Add(_docMenu);
        }


        /// <summary>
        /// Update viewmodel when manipulator moves document
        /// </summary>
        /// <param name="delta"></param>
        private void ManipulatorOnManipulatorTranslatedOrScaled(TransformGroupData delta)
        {
            var currentTranslate = ViewModel.GroupTransform.Translate;
            var currentScaleAmount = ViewModel.GroupTransform.ScaleAmount;

            var deltaTranslate = delta.Translate;
            var deltaScaleAmount = delta.ScaleAmount;

            var translate = new Point(currentTranslate.X + deltaTranslate.X, currentTranslate.Y + deltaTranslate.Y);
            //delta does contain information about scale center as is, but it looks much better if you just zoom from middle tbh
            var scaleCenter = new Point(0, 0);
            var scaleAmount = new Point(currentScaleAmount.X * deltaScaleAmount.X, currentScaleAmount.Y * deltaScaleAmount.Y);

            ViewModel.GroupTransform = new TransformGroupData(translate, scaleCenter, scaleAmount);
        }

        /// <summary>
        /// Resizes the CollectionView according to the increments in width and height. 
        /// The CollectionListView vertically resizes corresponding to the change in the size of its cells, so if ProportionalScaling is true and the ListView is being displayed, 
        /// the Grid must change size to accomodate the height of the ListView.
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        public Size Resize(double dx = 0, double dy = 0)
        {
            var dvm = DataContext as DocumentViewModel;
            Debug.Assert(dvm != null, "dvm != null");
            dvm.Width = Math.Max(dvm.Width + dx, MinWidth);
            dvm.Height = Math.Max(dvm.Height + dy, MinHeight);
            //Debug.WriteLine(ActualWidth + ", " + ActualHeight);
            ViewModel.GroupTransform = new TransformGroupData(ViewModel.GroupTransform.Translate, new Point(0, 0), ViewModel.GroupTransform.ScaleAmount);
            return new Size(dvm.Width, dvm.Height);
        }

        /// <summary>
        /// Called when the user holds the dragger button, or finishes holding it; 
        /// if the button is held down, initiates the proportional resizing mode.
        /// </summary>
        /// <param name="sender">DraggerButton in the DocumentView class</param>
        /// <param name="e"></param>
        public void DraggerButtonHolding(object sender, HoldingRoutedEventArgs e)
        {
            if (e.HoldingState == HoldingState.Started)
            {
                ProportionalScaling = true;
            }
            else if (e.HoldingState == HoldingState.Completed)
            {
                ProportionalScaling = false;
            }
        }

        /// <summary>
        /// Resizes the control based on the user's dragging the DraggerButton.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Dragger_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            Point p = Util.DeltaTransformFromVisual(e.Delta.Translation, sender as FrameworkElement);
            Resize(p.X, p.Y);
            e.Handled = true;
        }

        /// <summary>
        /// If the user was resizing proportionally, ends the proportional resizing and 
        /// changes the DraggerButton back to its normal appearance.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Dragger_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (ProportionalScaling)
            {
                ProportionalScaling = false;
            }
        }

        private void updateIcon()
        {
            if (ViewModel == null) return;
            // when you want a new icon, you have to add a check for it here!
            if (ViewModel.IconType == IconTypeEnum.Document)
            {
                xIconImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/doc-icon.png"));
            }
            else if (ViewModel.IconType == IconTypeEnum.Collection)
            {
                xIconImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/col-icon.png"));
            }
            else if (ViewModel.IconType == IconTypeEnum.Api)
            {
                xIconImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/api-icon.png"));
            }
        }

        void initDocumentOnDataContext()
        {
            // document type specific styles >> use VERY sparringly
            var docType = ViewModel.DocumentController.DocumentModel.DocumentType;
            if (docType.Type != null)
            {

            }
            else
            {

                ViewModel.DocumentController.DocumentModel.DocumentType.Type = docType.Id.Substring(0, 5);
            }

            // if there is a readable document type, use that as label
            var sourceBinding = new Binding
            {
                Source = ViewModel.DocumentController.DocumentModel.DocumentType,
                Path = new PropertyPath(nameof(ViewModel.DocumentController.DocumentModel.DocumentType.Type)),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            xIconLabel.SetBinding(TextBox.TextProperty, sourceBinding);

        }

        /// <summary>
        /// The first time the local DocumentViewModel _vm can be set to the new datacontext
        /// this resets the fields otherwise does nothing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void DocumentView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            ViewModel = DataContext as DocumentViewModel;

            //initDocumentOnDataContext();
        }

        private void OuterGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ViewModel != null)
            {
                ClipRect.Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height);
            }
            // update collapse info
            // collapse to icon view on resize
            int pad = 1;
            if (Height < MinHeight + xTextView.Height + 5)
            {
                xFieldContainer.Visibility = Visibility.Collapsed;
                xIcon.Visibility = Visibility.Collapsed;
                xTextView.Visibility = Visibility.Visible;
            } else
                if (Width < MinWidth + pad && Height < MinWidth + xIconLabel.ActualHeight) // MinHeight + xIconLabel.ActualHeight)
            {
                updateIcon();
                xFieldContainer.Visibility = Visibility.Collapsed;
                xIcon.Visibility = Visibility.Visible;
                xTextView.Visibility = Visibility.Collapsed;
                xDragImage.Opacity = 0;
                if (_docMenu != null) ViewModel.CloseMenu();
                UpdateBinding(true);
            }
            else if (xIcon.Visibility == Visibility.Visible ||
                xTextView.Visibility == Visibility.Visible)
            {
                xFieldContainer.Visibility = Visibility.Visible;
                xIcon.Visibility = Visibility.Collapsed;
                xTextView.Visibility = Visibility.Collapsed;
                xDragImage.Opacity = 1;
                UpdateBinding(false);
                IsLowestSelected = false; // to bring up the menu upon click 
            }
        }

        /// <summary>
        /// Updates the bindings on the lines when documentview is minimized/vice versa 
        /// </summary>
        /// <param name="becomeSmall"></param>
        private void UpdateBinding(bool becomeSmall)
        {
            var view = OuterGrid.GetFirstAncestorOfType<CollectionView>();
            if (view == null) return; // we can't always assume we're on a collection		

            (view.CurrentView as CollectionFreeformView)?.UpdateBinding(becomeSmall, this);
        }

        private void ExpandContract_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            // if in icon view expand to default size
            if (xIcon.Visibility == Visibility.Visible)
            {
                Resize(250, 250);
                IsLowestSelected = false; 
            }
            e.Handled = true; // prevent propagating
        }

        #region Menu

        public void DeleteDocument()
        {
            FadeOut.Begin();
        }

        private void CopyDocument()
        {
            ParentCollection.ViewModel.AddDocument(ViewModel.Copy(), null);
        }

        private void MakeDelegate()
        {
            ParentCollection.ViewModel.AddDocument(ViewModel.GetDelegate(), null);
        }

        public void ScreenCap()
        {
            Util.ExportAsImage(OuterGrid);
        }

        public void CommandLine()
        {
            FlyoutBase.ShowAttachedFlyout(xFieldContainer);
        }

        public void GetJson()
        {
            Util.ExportAsJson(ViewModel.DocumentController.EnumFields());
        }

        private void FadeOut_Completed(object sender, object e)
        {
            (ParentCollection.CurrentView as CollectionFreeformView)?.DeleteConnections(this);
            ParentCollection.ViewModel.RemoveDocument(ViewModel.DocumentController);
        }

        private void OpenLayout()
        {
            MainPage.Instance.DisplayElement(new InterfaceBuilder(ViewModel.DocumentController), new Point(0, 0), this);
        }

        private void CommandLine_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            Debug.Assert(tb != null, "tb != null");
            if (!tb.Text.EndsWith("\r"))
                return;
            var docController = (DataContext as DocumentViewModel).DocumentController;
            foreach (var tag in (sender as TextBox).Text.Split('#'))
                if (tag.Contains("="))
                {
                    var eqPos = tag.IndexOfAny(new[] { '=' });
                    var word = tag.Substring(0, eqPos).TrimEnd(' ').TrimStart(' ');
                    var valu = tag.Substring(eqPos + 1, Math.Max(0, tag.Length - eqPos - 1)).TrimEnd(' ', '\r');
                    var key = new KeyController(word, word);
                    foreach (var keyFields in docController.EnumFields())
                        if (keyFields.Key.Name == word)
                        {
                            key = keyFields.Key;
                            break;
                        }

                    DBTest.ResetCycleDetection();
                    docController.ParseDocField(key, valu);
                }
        }
        #endregion

        #region Activation

        private void UserControl_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (ParentCollection == null) return;
            ParentCollection.MaxZ += 1;
            Canvas.SetZIndex(this.GetFirstAncestorOfType<ContentPresenter>(), ParentCollection.MaxZ);
        }

        private void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            if (ViewModel.IsInInterfaceBuilder)
                return;

            OnSelected();
        }

        protected override void OnActivated(bool isSelected)
        {
            ViewModel.SetSelected(this, isSelected);
        }

        protected override void OnLowestActivated(bool isLowestSelected)
        {
            ViewModel.SetLowestSelected(this, isLowestSelected);

            if (xIcon.Visibility == Visibility.Collapsed && !IsMainCollection && isLowestSelected)
            {
                if (_docMenu == null)
                {
                    SetUpMenu();
                }
                ViewModel?.OpenMenu();
            }
            else
            {
                ViewModel?.CloseMenu();
            }
        }

        #endregion

    }
}