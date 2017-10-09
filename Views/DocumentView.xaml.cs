using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
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

        private Boolean useFixedMenu = false; // if true, doc menu appears fixed on righthand side of screen, otherwise appears next to doc

        private OverlayMenu _docMenu;
        public DocumentViewModel ViewModel { get; set; }
        // the document view that is being dragged
        public static DocumentView DragDocumentView;

        public bool ProportionalScaling { get; set; }

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
            this.Drop += OnDrop;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems)) e.Handled = true;
            FileDropHelper.HandleDropOnDocument(this, e);
            ParentCollection?.ViewModel.ChangeIndicationColor(ParentCollection.CurrentView, Colors.Transparent);
        }

        public DocumentView(DocumentViewModel documentViewModel) : this()
        {
            DataContext = documentViewModel;
        }

        private void This_Unloaded(object sender, RoutedEventArgs e)
        {
            //Debug.WriteLine($"Unloaded: Num DocViews = {--dvCount}");
            DraggerButton.Holding -= DraggerButtonHolding;
            DraggerButton.ManipulationDelta -= Dragger_OnManipulationDelta;
            DraggerButton.ManipulationCompleted -= Dragger_ManipulationCompleted;
            //Loaded -= This_Loaded;
            //Unloaded -= This_Unloaded;
        }


        private void This_Loaded(object sender, RoutedEventArgs e)
        {
            //Debug.WriteLine($"Loaded: Num DocViews = {++dvCount}");
            DraggerButton.Holding -= DraggerButtonHolding;
            DraggerButton.Holding += DraggerButtonHolding;
            DraggerButton.ManipulationDelta -= Dragger_OnManipulationDelta;
            DraggerButton.ManipulationDelta += Dragger_OnManipulationDelta;
            DraggerButton.ManipulationCompleted -= Dragger_ManipulationCompleted;
            DraggerButton.ManipulationCompleted += Dragger_ManipulationCompleted;

            ParentCollection = this.GetFirstAncestorOfType<CollectionView>();
            if (ViewModel != null)
            {
                //if (Parent == null)
                //    ViewModel.Width = ActualWidth;
                //else ViewModel.Width = double.NaN;
                //if (Parent == null)
                //    ViewModel.Height = ActualHeight;
                //else ViewModel.Height = double.NaN;
            }
        }


        #region Xaml Styling Methods (used by operator view)
        private bool isOperator = false;
        /// <summary>
        /// Applies custom override styles to the operator view
        /// </summary>
        public void StyleOperator(double borderRadiusAmount)
        {
            isOperator = true;
            xShadowTarget.RadiusX = borderRadiusAmount;
            xShadowTarget.RadiusY = borderRadiusAmount;

            var brush = (Application.Current.Resources["OperatorBackground"] as SolidColorBrush);
            Color c = brush.Color;
            c.A = 204;
            xGradientOverlay.CornerRadius = new CornerRadius(borderRadiusAmount);
        }

#endregion
        SolidColorBrush bgbrush = (Application.Current.Resources["WindowsBlue"] as SolidColorBrush);
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

        DateTime copyDown = DateTime.MinValue;
        MenuButton copyButton;
        private void SetUpMenu()
        {
            var bgcolor = bgbrush.Color;
            bgcolor.A = 0;
            var red = new Color();
            red.A = 204;
            red.R = 190;
            red.B = 25;
            red.G = 25;

            copyButton = new MenuButton(Symbol.Copy,         "Copy", bgcolor, CopyDocument);
            var moveButton = new MenuButton(Symbol.MoveToFolder, "Move", bgcolor, null);
            copyButton = new MenuButton(Symbol.Copy, "Copy", bgcolor, CopyDocument);
            var copyDataButton = new MenuButton(Symbol.SetTile, "Copy Data", bgcolor, CopyDataDocument);
            var copyViewButton = new MenuButton(Symbol.SetTile, "Copy View", bgcolor, CopyViewDocument);
            var documentButtons = new List<MenuButton>
            {
                new MenuButton(Symbol.Pictures, "Layout",bgcolor,OpenLayout),
                moveButton,
                copyButton,
               // delegateButton,
                copyDataButton,
                copyViewButton,
                new MenuButton(Symbol.Delete, "Delete",bgcolor,DeleteDocument)
                //new MenuButton(Symbol.Camera, "ScrCap",bgcolor, ScreenCap),
                //new MenuButton(Symbol.Placeholder, "Commands",bgcolor, CommandLine)
            };

            var moveButtonView = moveButton.View;
            moveButtonView.CanDrag = true;
            moveButtonView.DragStarting += (s, e) =>
            {
                e.Data.RequestedOperation = DataPackageOperation.Move;
                ViewModel.DocumentView_DragStarting(this, e);
            };
            moveButtonView.DropCompleted += MoveButtonView_DropCompleted;
            var copyButtonView = copyButton.View;
            copyButtonView.CanDrag = true;
            copyButton.AddHandler(PointerPressedEvent, new PointerEventHandler(CopyButton_PointerPressed), true);
            copyButtonView.DragStarting += (s, e) =>
            {
                _moveTimer.Stop();
                e.Data.RequestedOperation = copyButton.ButtonIcon.Symbol == Symbol.MoveToFolder ? DataPackageOperation.Move : DataPackageOperation.Copy;
                ViewModel.DocumentView_DragStarting(this, e);
            };
            copyButtonView.DropCompleted += CopyButtonView_DropCompleted1;
            var copyDataButtonView = copyDataButton.View;
            copyDataButtonView.CanDrag = true;
            copyDataButtonView.DragStarting += (s, e) =>
            {
                e.Data.RequestedOperation = DataPackageOperation.Link;
                ViewModel.DocumentView_DragStarting(this, e);
            };
            var copyViewButtonView = copyViewButton.View;
            copyViewButtonView.CanDrag = true;
            copyViewButtonView.DragStarting += (s, e) =>
            {
                e.Data.RequestedOperation = DataPackageOperation.Link;
                e.Data.Properties.Add("View", true);
                ViewModel.DocumentView_DragStarting(this, e);
            };


            _docMenu = new OverlayMenu(null, documentButtons);

            Binding visibilityBinding = new Binding
            {
                Source = ViewModel,
                Path = new PropertyPath(nameof(ViewModel.DocMenuVisibility)),
                Mode = BindingMode.OneWay
            };
            xMenuCanvas.SetBinding(VisibilityProperty, visibilityBinding);

            if (!useFixedMenu)
                xMenuCanvas.Children.Add(_docMenu);
            _moveTimer.Interval = new TimeSpan(0, 0, 0, 0, 600);
            _moveTimer.Tick += Timer_Tick;
        }

        private void CopyButtonView_DropCompleted1(UIElement sender, DropCompletedEventArgs args)
        {
            if (args.DropResult == DataPackageOperation.Move)
            {
                var coll = CollectionView.GetParentCollectionView(this);
                coll.ViewModel.RemoveDocument(ViewModel.DocumentController);
            }
        }

        private void MoveButtonView_DropCompleted(UIElement sender, DropCompletedEventArgs args)
        {
        }

        private void CopyButtonView_DropCompleted(UIElement sender, DropCompletedEventArgs args)
        {
            copyButton.ButtonIcon.Symbol = Symbol.Copy;
            copyButton.ButtonText.Text = "Copy";
            _moveTimer.Stop();
        }

        DispatcherTimer _moveTimer = new DispatcherTimer();

        private void CopyButton_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _moveTimer.Start();
        }

        private void Timer_Tick(object sender, object e)
        {
            copyButton.ButtonIcon.Symbol = Symbol.MoveToFolder;
            copyButton.ButtonText.Text = "Move";
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
            // should we allow documents with NaN's for width & height to be resized?
            //if (double.IsNaN(dvm.Width))
            //    dvm.Width = ActualWidth + dx;
            //if (double.IsNaN(dvm.Height))
            //    dvm.Height = ActualHeight + dy;
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

        /// <summary>
        /// Updates the minimized-view icon from the ViewModel's corresponding IconType array.
        /// </summary>
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
                xClipRect.Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height);
            }
            // update collapse info
            // collapse to icon view on resize
            int pad = 1;
            if (Height < MinHeight + xTextView.Height + 5)
            {
                xFieldContainer.Visibility = Visibility.Collapsed;
                xIcon.Visibility = Visibility.Collapsed;
                xTextView.Visibility = Visibility.Visible;
            }
            else
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


        #region Menu

        public void DeleteDocument()
        {
            (ParentCollection.CurrentView as CollectionFreeformView)?.AddToStoryboard(FadeOut, this);
            FadeOut.Begin();

            if (useFixedMenu)
                MainPage.Instance.HideDocumentMenu();

        }

        private void CopyDocument()
        {
            _moveTimer.Stop();
            ParentCollection.ViewModel.AddDocument(ViewModel.DocumentController.GetCopy(null), null);
        }
        private void CopyViewDocument()
        {
            _moveTimer.Stop();
            ParentCollection.ViewModel.AddDocument(ViewModel.DocumentController.GetViewCopy(null), null);
            xDelegateStatusCanvas.Visibility = ViewModel.DocumentController.HasDelegatesOrPrototype ? Visibility.Visible : Visibility.Collapsed;  // TODO theoretically the binding should take care of this..
        }

        private void CopyDataDocument()
        {
            ParentCollection.ViewModel.AddDocument(ViewModel.DocumentController.GetDataCopy(), null);
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
            ViewModel.CloseMenu();
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
        
        public Rect ClipRect { get { return xClipRect.Rect;  } }

        public async void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (!IsSelected)
            {
                await Task.Delay(100);

                //Selects it and brings it to the foreground of the canvas, in front of all other documents.
                if (ParentCollection == null) return;
                ParentCollection.MaxZ += 1;
                Canvas.SetZIndex(this.GetFirstAncestorOfType<ContentPresenter>(), ParentCollection.MaxZ);

                if (e != null) e.Handled = true;
                OnSelected();
            }
        }

        protected override void OnActivated(bool isSelected)
        {
            ViewModel.SetSelected(this, isSelected);
            // if we are being deselected
            if (!isSelected)
            {
                colorStoryboardOut.Begin();
                if (useFixedMenu)
                    MainPage.Instance.HideDocumentMenu();
            }
            else
            {
                // update the main toolbar in the overlay canvas
                if (_docMenu == null)
                {
                    SetUpMenu();
                }
                if (_docMenu != null && MainPage.Instance != null)
                {
                    colorStoryboard.Begin();
                    if (useFixedMenu)
                    {
                        MainPage.Instance.SetOptionsMenu(_docMenu);
                        if (MainPage.Instance.MainDocView != this)
                            MainPage.Instance.ShowDocumentMenu();
                    }
                }
            }
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
                _docMenu.AddAndPlayOpenAnimation();
            }
            else
            {
                ViewModel?.CloseMenu();
            }
        }

        #endregion

        private void DocumentView_OnDragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = (DataPackageOperation.Copy | DataPackageOperation.Move) & (e.DataView.RequestedOperation == DataPackageOperation.None ? DataPackageOperation.Copy : e.DataView.RequestedOperation);
            }
        }

        private async void DocumentView_OnRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var doc = ViewModel.DocumentController;
            var text = doc.GetField(KeyStore.SystemUriKey) as TextFieldModelController;
            if (text == null) return;
            var query = await Launcher.QueryAppUriSupportAsync(new Uri(text.Data));
            Debug.WriteLine(query);
        }
    }
}