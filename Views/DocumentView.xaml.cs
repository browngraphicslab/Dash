using DashShared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Shapes;
using DashShared;
using Newtonsoft.Json;
using Visibility = Windows.UI.Xaml.Visibility;
using Dash.Models.DragModels;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236
namespace Dash
{

    public sealed partial class DocumentView
    {
        public CollectionView ParentCollection => this.GetFirstAncestorOfType<CollectionView>(); // TODO document views should not be assumed to be in a collection this!

        public bool IsMainCollection { get; set; } //TODO document views should not be aware of if they are the main collection!

        /// <summary>
        /// Contains methods which allow the document to be moved around a free form canvas
        /// </summary>
        public ManipulationControls ManipulationControls { get; set; }

        public DocumentViewModel ViewModel { get; set; }

        public MenuFlyout MenuFlyout { get; set; }

        static readonly SolidColorBrush SingleSelectionBorderColor = new SolidColorBrush(Colors.LightGray);
        static readonly SolidColorBrush GroupSelectionBorderColor  = new SolidColorBrush(Colors.LightBlue);
        bool _ptrIn;
        bool _multiSelected;

        /// <summary>
        /// The width of the context preview
        /// </summary>
        const double _contextPreviewActualWidth = 255;

        /// <summary>
        /// The height of the context preview
        /// </summary>
        const double _contextPreviewActualHeight = 330;

        /// <summary>
        /// A reference to the actual context preview
        /// </summary>
        private UIElement _localContextPreview;
        private UIElement _selectedContextPreview;

        private long _bindRenderTransformToken = -1;
        public static readonly DependencyProperty BindRenderTransformProperty = DependencyProperty.Register(
            "BindRenderTransform", typeof(bool), typeof(DocumentView), new PropertyMetadata(default(bool)));

        public bool BindRenderTransform
        {
            get { return (bool)GetValue(BindRenderTransformProperty); }
            set { SetValue(BindRenderTransformProperty, value); }
        }

        // == CONSTRUCTORs ==
        public DocumentView(DocumentViewModel documentViewModel) : this()
        {
            DataContext = documentViewModel;
        }

        public DocumentView()
        {
            InitializeComponent();

            Util.InitializeDropShadow(xShadowHost, xDocumentBackground);


            DataContextChanged += DocumentView_DataContextChanged;
            // set bounds
            MinWidth = 100;
            MinHeight = 25;
            //OuterGrid.MinWidth = 100;
            //OuterGrid.MinHeight = 25;

            Loaded += This_Loaded;
            Unloaded += This_Unloaded;
            this.Drop += OnDrop;
            
            AddHandler(PointerEnteredEvent, new PointerEventHandler(DocumentView_PointerEntered), true);
            AddHandler(PointerExitedEvent, new PointerEventHandler(DocumentView_PointerExited), true);
            AddHandler(TappedEvent, new TappedEventHandler(OnTapped), true);


            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            Window.Current.CoreWindow.KeyUp += CoreWindow_KeyUp;

            MenuFlyout = xMenuFlyout;
        }

        private void BindRenderTransformChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (BindRenderTransform)
            {
                var doc = ViewModel?.DocumentController;
                if (doc == null)
                {
                    Debug.Fail("The view model should not be null at this point");
                }
                FieldMultiBinding<MatrixTransform> binding = new FieldMultiBinding<MatrixTransform>(
                    new DocumentFieldReference(doc.Id, KeyStore.PositionFieldKey),
                    new DocumentFieldReference(doc.Id, KeyStore.ScaleAmountFieldKey))
                {
                    Converter = new TransformGroupMultiConverter(),
                    Mode = BindingMode.OneWay
                };
                this.AddFieldBinding(RenderTransformProperty, binding);
            }
            else
            {
                this.AddFieldBinding(RenderTransformProperty, null);
            }
        }

        private void CoreWindow_KeyUp(CoreWindow sender, KeyEventArgs args)
        {
            var f1 = Window.Current.CoreWindow.GetKeyState(VirtualKey.F1);
            if (!f1.HasFlag(CoreVirtualKeyStates.Down))
            {
                ShowLocalContext(false);
            }
        }

        private void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs e)
        {
            var ctrlState = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Control)
                .HasFlag(CoreVirtualKeyStates.Down);
            var altState = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Menu)
                .HasFlag(CoreVirtualKeyStates.Down);
            var tabState = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Tab)
                .HasFlag(CoreVirtualKeyStates.Down);
            var shiftState = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Shift)
                .HasFlag(CoreVirtualKeyStates.Down);
            var f1State = Window.Current.CoreWindow.GetKeyState(VirtualKey.F1);
            var f2State = Window.Current.CoreWindow.GetKeyState(VirtualKey.F2);
            if (f1State.HasFlag(CoreVirtualKeyStates.Down) && _ptrIn)
            {
                ShowLocalContext(true);
            }
            if (f2State.HasFlag(CoreVirtualKeyStates.Down) && _ptrIn)
            {
                ShowSelectedContext();
            }
            
            var focused = (FocusManager.GetFocusedElement() as FrameworkElement)?.DataContext as DocumentViewModel;

            if (ViewModel != null && ViewModel.Equals(focused) && (shiftState && !e.VirtualKey.Equals(VirtualKey.Shift)) &&
                                      e.VirtualKey.Equals(VirtualKey.Enter))
            {
                // don't shift enter on key value documents
                if (ViewModel.LayoutDocument.DocumentType.Equals(KeyValueDocumentBox.DocumentType) ||
                    ViewModel.DocumentController.DocumentType.Equals(DashConstants.TypeStore.MainDocumentType))
                    return;

                HandleShiftEnter();
            }
        }

        public void ToggleMultiSelected(bool isMultiSelected)
        {
            if (isMultiSelected == _multiSelected) return;
            var freeformView = ParentCollection?.CurrentView as CollectionFreeformView;
            if (freeformView == null) return;
            if (!isMultiSelected)
            {
                this.CanDrag = false;
                xTargetContentGrid.BorderThickness = new Thickness(0);
            }
            else
            {
                this.CanDrag = true;
                xTargetContentGrid.BorderBrush = new SolidColorBrush(Colors.DodgerBlue);
                xTargetContentGrid.BorderThickness = new Thickness(2);
            }
            _multiSelected = isMultiSelected;
        }

        public void ShowLocalContext(bool showContext)
        {
            if (ViewModel == null)
                return;
            ViewModel.ShowLocalContext = showContext;

            if (!showContext && _localContextPreview != null)
            {
                xContextCanvas.Children.Remove(_localContextPreview);
                _localContextPreview = null;
                GC.Collect();
                ViewModel.SetHasTitle(DraggerButton.Visibility == Visibility.Visible);
                if (_selectedContextPreview == null)
                {
                    xContextTitle.Visibility = Visibility.Collapsed;
                }
                else
                {
                    xContextTitle.Content = ViewModel.DocumentController
                        .GetDereferencedField<DocumentController>(KeyStore.SelectedSchemaRow, null)?.GetFirstContext().Title;
                }
            }

            if (showContext)
            {
                if (ViewModel.DocumentController.DocumentType.Equals(DashConstants.TypeStore.MainDocumentType)) return;

                var context = ViewModel.DocumentController.GetFirstContext();
                if (context == null) return;
                ViewModel.SetHasTitle(true);

                if (_localContextPreview == null)
                {
                    _localContextPreview = new ContextPreview(context)
                    {
                        Width = _contextPreviewActualWidth,
                        Height = _contextPreviewActualHeight,
                    };
                    _localContextPreview.Tapped += (s, e) => ShowContext();
                    xContextCanvas.Children.Add(_localContextPreview);
                    xContextTitle.Content = context.Title;
                    xContextTitle.Visibility = Visibility.Visible;
                    PositionContextPreview();
                }
            }
        }

        public void ShowSelectedContext(bool selectedChanged = false)
        {
            if (ViewModel == null)
                return;

            if (_selectedContextPreview != null && selectedChanged == false)
            {
                xContextCanvas.Children.Remove(_selectedContextPreview);
                _selectedContextPreview = null;
                GC.Collect();
                ViewModel.SetHasTitle(DraggerButton.Visibility == Visibility.Visible);
                if (_localContextPreview == null)
                {
                    xContextTitle.Visibility = Visibility.Collapsed;
                }
                else
                {
                    xContextTitle.Content = ViewModel.DocumentController.GetFirstContext().Title;
                }
            }
            else
            {
                if (ViewModel.DocumentController.DocumentType.Equals(DashConstants.TypeStore.MainDocumentType)) return;

                var context = ViewModel.DocumentController
                    .GetDereferencedField<DocumentController>(KeyStore.SelectedSchemaRow, null)?.GetFirstContext();
                if (context == null) return;
                ViewModel.SetHasTitle(true);

                if (_selectedContextPreview == null)
                {
                    _selectedContextPreview = new ContextPreview(context)
                    {
                        Width = _contextPreviewActualWidth,
                        Height = _contextPreviewActualHeight,
                    };
                    xContextCanvas.Children.Add(_selectedContextPreview);
                }
                else
                {
                    (_selectedContextPreview as ContextPreview).Context = context;
                }
                xContextTitle.Content = context.Title;
                xContextTitle.Visibility = Visibility.Visible;
                PositionContextPreview();
                ViewModel.DocumentController.RemoveFieldUpdatedListener(KeyStore.SelectedSchemaRow, OnSelectedSchemaRowUpdated);
                ViewModel.DocumentController.AddFieldUpdatedListener(KeyStore.SelectedSchemaRow, OnSelectedSchemaRowUpdated);
            }
        }

        private void OnSelectedSchemaRowUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context1)
        {
            ShowSelectedContext(true);
        }

        private void PositionContextPreview()
        {
            var previewMarginLeft = 1;
            var previewMarginTop = 25;
            Canvas.SetTop(xContextTitle, previewMarginTop);
            if (_localContextPreview != null)
            {
                Canvas.SetLeft(_localContextPreview, -_contextPreviewActualWidth - previewMarginLeft);
                Canvas.SetTop(_localContextPreview, 35 + previewMarginTop);
            }
            if (_selectedContextPreview != null)
            {
                Canvas.SetLeft(_selectedContextPreview, -_contextPreviewActualWidth - previewMarginLeft);
                Canvas.SetTop(_selectedContextPreview, 35 + previewMarginTop);
            }
        }

        // since this is public it can be called with any parameters, be safe, check everything
        public void DocumentView_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            ToggleSelectionBorderAndChrome(false);

            _ptrIn = false;
            var f1State = Window.Current.CoreWindow.GetKeyState(VirtualKey.F1);
            var f2State = Window.Current.CoreWindow.GetKeyState(VirtualKey.F2);
            if (f1State.HasFlag(CoreVirtualKeyStates.None)) ShowLocalContext(false);
        }

        // since this is public it can be called with any parameters, be safe, check everything
        public void DocumentView_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            ToggleSelectionBorderAndChrome(true);

            _ptrIn = true;
            var f1State = Window.Current.CoreWindow.GetKeyState(VirtualKey.F1);
            var f2State = Window.Current.CoreWindow.GetKeyState(VirtualKey.F2);
            if (f1State.HasFlag(CoreVirtualKeyStates.Down)) ShowLocalContext(true);
            if (f2State.HasFlag(CoreVirtualKeyStates.Down)) ShowSelectedContext(); // TODO show selected row
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems)) e.Handled = true;
            ParentCollection?.ViewModel.ChangeIndicationColor(ParentCollection.CurrentView, Colors.Transparent);
        }

        public void ToFront()
        {
            if (ParentCollection == null || ViewModel?.DocumentController?.DocumentType?.Equals(BackgroundBox.DocumentType) == true)
                return;
            ParentCollection.MaxZ += 1;
            Canvas.SetZIndex(this.GetFirstAncestorOfType<ContentPresenter>(), ParentCollection.MaxZ);
        }

        private void This_Unloaded(object sender, RoutedEventArgs e)
        {
            //Debug.WriteLine($"Unloaded: Num DocViews = {--dvCount}");
            DraggerButton.PointerPressed -= DraggerButton_PointerPressed;
            DraggerButton.ManipulationDelta -= Dragger_OnManipulationDelta;
        }

        private void This_Loaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null && !ViewModel.Undecorated)
            {
                xTitleIcon.Tapped += (s, args) => {
                    ShowContext();
                    args.Handled = true;
                };
                // add manipulation code
                ManipulationControls = new ManipulationControls(this, new List<FrameworkElement>(new FrameworkElement[] { xTitleIcon }));
                ManipulationControls.OnManipulatorTranslatedOrScaled += ManipulatorOnManipulatorTranslatedOrScaled;
            }

            //Debug.WriteLine($"Loaded: Num DocViews = {++dvCount}");
            DraggerButton.PointerPressed += DraggerButton_PointerPressed;
            DraggerButton.ManipulationDelta += Dragger_OnManipulationDelta;

            // Adds a function to tabmenu, which brings said DocumentView to focus 
            // this gets the hierarchical view of the document, clicking on this will shimmy over to this
            IsMainCollection = (this == MainPage.Instance.MainDocView);

            // add corresponding instance of this to hierarchical view
            if (!IsMainCollection && ViewModel != null)
            {

                if (double.IsNaN(ViewModel.Width) &&
                    (ParentCollection?.CurrentView is CollectionFreeformView))
                {
                    ViewModel.Width = 50;
                    ViewModel.Height = 50;
                }
            }

            ToFront();
            if (ViewModel?.DocumentController?.DocumentType?.Equals(DashConstants.TypeStore.MainDocumentType) == true)
            {
                ManipulationControls.ElementOnManipulationCompleted(null, null); // TODO this causes groups to show up, and needs to be moved
                return;
            }
        }

        private void DraggerButton_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            ManipulationMode = e.GetCurrentPoint(this).Properties.IsRightButtonPressed ? ManipulationModes.None : ManipulationModes.All;
            if (ManipulationMode == ManipulationModes.All)
                e.Handled = true;
        }

        #region Xaml Styling Methods (used by operator/collection view)

        /// <summary>
        /// Applies custom override styles to the operator view. 
        /// width - the width of a single link node (generally App.xaml defines this, "InputHandleWidth")
        /// </summary>
        public void StyleOperator(double width, string title)
        {
            //xShadowTarget.Margin = new Thickness(width, 0, width, 0);
            //xGradientOverlay.Margin = new Thickness(width, 0, width, 0);
            //xShadowTarget.Margin = new Thickness(width, 0, width, 0);
            xTitleIcon.Text = Application.Current.Resources["OperatorIcon"] as string;
            if (ParentCollection != null)
            {
                var dataDoc = ViewModel.DocumentController.GetDataDocument(null);
                dataDoc.SetTitleField(title);
            }
            xOperatorEllipseBorder.Visibility = Visibility.Collapsed; ;
        }

        /// <summary>
        /// Applies custom override styles to the operator view. 
        /// width - the width of a single link node (generally App.xaml defines this, "InputHandleWidth")
        /// </summary>
        public void StyleCollection(CollectionView view)
        {
            xTitleIcon.Text = Application.Current.Resources["CollectionIcon"] as string;
            xDocumentBackground.Fill = ((SolidColorBrush)Application.Current.Resources["DocumentBackground"]);
            // TODO remove this arbtirary styling here
            if (this == MainPage.Instance.MainDocView)
            {
                IsMainCollection = true;
                view.xOuterGrid.BorderThickness = new Thickness(0);
            }
        }

        public void StyleKeyValuePane()
        {
            xOperatorEllipseBorder.Visibility = Visibility.Collapsed; ;

        }

        #endregion

        /// <summary>
        /// Update viewmodel when manipulator moves document
        /// </summary>
        /// <param name="delta"></param>
        private void ManipulatorOnManipulatorTranslatedOrScaled(TransformGroupData delta)
        {
            ViewModel?.TransformDelta(delta);
        }

        /// <summary>
        /// Resizes the CollectionView according to the increments in width and height. 
        /// The CollectionListView vertically resizes corresponding to the change in the size of its cells, so if ProportionalScaling is true and the ListView is being displayed, 
        /// the Grid must change size to accomodate the height of the ListView.
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        Size Resize(double dx = 0, double dy = 0)
        {
            var dvm = DataContext as DocumentViewModel;
            if (dvm != null)
            {
                Debug.Assert(dvm != null, "dvm != null");
                Debug.Assert(dvm.Width != double.NaN);
                Debug.Assert(dvm.Height != double.NaN);
                dvm.Width = Math.Max(dvm.Width + dx, MinWidth);
                dvm.Height = Math.Max(dvm.Height + dy, MinHeight);
                // should we allow documents with NaN's for width & height to be resized?
                return new Size(dvm.Width, dvm.Height);
            }
            return new Size();
        }

        void ProportionalResize(ManipulationDeltaRoutedEventArgs e)
        {
            var pos = Util.PointTransformFromVisual(e.Position, e.Container);
            var origin = Util.PointTransformFromVisual(new Point(0, 0), this);
            var projectedDelta = new Point(ActualWidth, ActualHeight).PointProjectArg(
                new Point(e.Delta.Translation.X, e.Delta.Translation.Y));
            var curScale = ViewModel.Scale;
            var scale = Math.Max(Math.Min((1 + projectedDelta.X / ActualWidth) * curScale.X, 5), 0.2);
            ViewModel.Scale = new Point(scale, scale);
        }

        /// <summary>
        /// Resizes the control based on the user's dragging the DraggerButton.  The contents will adjust to fit the bounding box
        /// of the control *unless* the Shift button is held in which case the control will be resized but the contents will remain.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Dragger_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var proportionalScaling = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);

            if (proportionalScaling)
            {
                ProportionalResize(e);
            }
            else
            {
                var p = Util.DeltaTransformFromVisual(e.Delta.Translation, sender as FrameworkElement);
                Resize(p.X, p.Y);
            }
            e.Handled = true;

            if (!Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down))
            {
                //uncomment to make children in collection stretch
                fitFreeFormChildrenToTheirLayouts();
            }
        }

        void fitFreeFormChildrenToTheirLayouts()
        {
            var freeFormChild = VisualTreeHelperExtensions.GetFirstDescendantOfType<CollectionFreeformView>(this);
            var parentOfFreeFormChild = freeFormChild != null ? VisualTreeHelperExtensions.GetFirstAncestorOfType<DocumentView>(freeFormChild) : null;
            if (this == parentOfFreeFormChild)
            {   // if this document directly contains a free form child, then initialize its contents to fit its layout.
                freeFormChild?.ManipulationControls?.FitToParent();
            }
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
            if (_bindRenderTransformToken != -1)
            {
                UnregisterPropertyChangedCallback(BindRenderTransformProperty, _bindRenderTransformToken);
            }
            if (ViewModel != null)
            {
                _bindRenderTransformToken = RegisterPropertyChangedCallback(BindRenderTransformProperty, BindRenderTransformChanged);
                BindRenderTransformChanged(this, BindRenderTransformProperty);
                // binds the display title of the document to the back end representation
                ViewModel.SetHasTitle(DraggerButton.Visibility == Visibility.Visible);
            }
        }

        #region Menu

        public void DeleteDocument()
        {
            DeleteDocument(false);
        }
        public void DeleteDocument(bool addTextBox)
        {
            if (ParentCollection != null)
            {
                FadeOut.Begin();

                if (addTextBox)
                {
                    (ParentCollection.CurrentView as CollectionFreeformView)?.RenderPreviewTextbox(ViewModel.Position);
                }
            }
        }
        public void ShowContext()
        {
            ViewModel.DocumentController.GetDataDocument().RestoreNeighboringContext();
        }
        public void GetJson()
        {
            Util.ExportAsJson(ViewModel.DocumentController.EnumFields());
        }

        private void FadeOut_Completed(object sender, object e)
        {
            ParentCollection?.ViewModel.RemoveDocument(ViewModel.DocumentController);
        }
        
        #endregion

        #region Activation
        
        public void RightTap()
        {
            var pointerPosition2 = Windows.UI.Core.CoreWindow.GetForCurrentThread().PointerPosition;
            var x = pointerPosition2.X - Window.Current.Bounds.X;
            var y = pointerPosition2.Y - Window.Current.Bounds.Y;
            var pos = new Point(x, y);

            xMenuFlyout.ShowAt(this, MainPage.Instance.TransformToVisual(this).TransformPoint(pos));
        }

        public async void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if ((Window.Current.CoreWindow.GetKeyState(VirtualKey.RightButton) & CoreVirtualKeyStates.Down) !=
                CoreVirtualKeyStates.Down &&
                ViewModel.DocumentController.DocumentType.Equals(BackgroundBox.DocumentType))
            {
                return;
            }
            // handle the event right away before any possible async delays
            if (e != null) e.Handled = true;


            await Task.Delay(100); // allows for double-tap

            // Selects it and brings it to the foreground of the canvas, in front of all other documents.
            if (ParentCollection != null && this.GetFirstAncestorOfType<ContentPresenter>() != null)
            {
                var zindex = Canvas.GetZIndex(this.GetFirstAncestorOfType<ContentPresenter>());
                if (zindex > -100 && zindex < ParentCollection.MaxZ)
                {
                    ParentCollection.MaxZ += 1;
                    Canvas.SetZIndex(this.GetFirstAncestorOfType<ContentPresenter>(), ParentCollection.MaxZ);
                }
            }
        }

        /// <summary>
        /// Sets whther the selection border is on, all other chrome can be turned off or on independently
        /// so that we don't see huge amounts of chrome when we hover over groups
        /// </summary>
        /// <param name="isBorderOn"></param>
        /// <param name="isOtherChromeVisible"></param>
        private void ToggleSelectionBorderAndChrome(bool isBorderOn, bool isOtherChromeVisible = true)
        {
            OperatorEllipseUnhighlight.Visibility = DraggerButton.Visibility = isBorderOn && isOtherChromeVisible && ViewModel?.Undecorated == false ? Visibility.Visible : Visibility.Collapsed;
            xTitleIcon.Foreground = isBorderOn && isOtherChromeVisible && ViewModel?.Undecorated == false
                ? (SolidColorBrush)Application.Current.Resources["TitleText"]
                : new SolidColorBrush(Colors.Transparent);
            if (OperatorEllipseUnhighlight.Visibility == Visibility.Collapsed)
                OperatorEllipseHighlight.Visibility = Visibility.Collapsed;
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
            this.OnTapped(sender, new TappedRoutedEventArgs());
            var doc = ViewModel.DocumentController;
            var text = doc.GetField(KeyStore.SystemUriKey) as TextController;
            if (text == null) return;
            var query = await Launcher.QueryAppUriSupportAsync(new Uri(text.Data));

        }

        private void XTitle_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter || e.Key == VirtualKey.Tab)
            {
                this.Focus(FocusState.Programmatic);
                e.Handled = true;
            }
        }

        private void DocumentView_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            ManipulationMode = e.GetCurrentPoint(this).Properties.IsRightButtonPressed ? ManipulationModes.All : ManipulationModes.None;
            ToFront();
        }

        public bool MoveToContainingCollection(List<DocumentView> overlappedViews, List<DocumentView> grouped)
        {
            var collection = this.GetFirstAncestorOfType<CollectionView>();
            if (collection == null || ViewModel == null)
                return false;

            foreach (var nestedDocument in overlappedViews)
            {
                CollectionView nestedCollection = null;
                if (nestedDocument.ViewModel.LayoutDocument.DocumentType.Equals(DashConstants.TypeStore.CollectionBoxType))
                    nestedCollection = nestedDocument.GetFirstDescendantOfType<CollectionView>();
                if (nestedCollection != null)
                {
                    if (nestedCollection.GetAncestors().ToList().Contains(this))
                        continue;
                    if (!nestedCollection.Equals(collection))
                    {
                        if (grouped != null)
                        {
                            foreach (var g in grouped)
                            {
                                var pos = g.TransformToVisual(MainPage.Instance).TransformPoint(new Point());
                                var where = nestedCollection.CurrentView is CollectionFreeformView ?
                                    Util.GetCollectionFreeFormPoint((nestedCollection.CurrentView as CollectionFreeformView), pos) :
                                    new Point();
                                nestedCollection.ViewModel.AddDocument(g.ViewModel.DocumentController.GetSameCopy(where), null);
                                (collection.CurrentView as CollectionFreeformView).SuspendGroups = true;
                                collection.ViewModel.RemoveDocument(g.ViewModel.DocumentController);
                                (collection.CurrentView as CollectionFreeformView).SuspendGroups = false;

                            }
                            return true;
                        }
                    }
                    else break;
                }
            }
            return false;
        }

        #region Context menu click handlers

        private void MenuFlyoutItemCopy_Click(object sender, RoutedEventArgs e) { ParentCollection?.ViewModel.AddDocument(ViewModel.DocumentController.GetCopy(null), null);  }
        private void MenuFlyoutItemAlias_Click(object sender, RoutedEventArgs e) { ParentCollection?.ViewModel.AddDocument(ViewModel.DocumentController.GetViewCopy(null), null); }
        private void MenuFlyoutItemDelete_Click(object sender, RoutedEventArgs e) { DeleteDocument(); }
        private void MenuFlyoutItemFields_Click(object sender, RoutedEventArgs e) { ParentCollection.ViewModel.AddDocument(ViewModel.DocumentController.GetKeyValueAlias(), null); }
        public void MenuFlyoutItemPreview_Click(object sender, RoutedEventArgs e) { ParentCollection.ViewModel.AddDocument(ViewModel.DocumentController.GetPreviewDocument(), null); }
        private void MenuFlyoutItemContext_Click(object sender, RoutedEventArgs e) { ShowContext(); }
        private void MenuFlyoutItemScreenCap_Click(object sender, RoutedEventArgs e) { Util.ExportAsImage(LayoutRoot); }
        private void MenuFlyoutItemOpen_OnClick(object sender, RoutedEventArgs e) { MainPage.Instance.SetCurrentWorkspace(ViewModel.DocumentController); }
        private void MenuFlyoutItemCopyHistory_Click(object sender, RoutedEventArgs e)
        {
            var data = new DataPackage() { };
            data.SetText(string.Join("\n",
                (ViewModel.DocumentController.GetAllContexts() ?? new List<DocumentContext>()).Select(
                    c => c.Title + "  :  " + c.Url)));
            Clipboard.SetContent(data);
        }

        #endregion

        private void DocumentView_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ViewModel?.UpdateActualSize(this.ActualWidth, this.ActualHeight);
            PositionContextPreview();
        }

        private void xContextLinkTapped(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
        {
            ShowContext();
        }

        public void HandleShiftEnter()
        {
            var collection = this.GetFirstAncestorOfType<CollectionFreeformView>();
            var docCanvas = this.GetFirstAncestorOfType<Canvas>();
            if (collection == null) return;
            var where = this.TransformToVisual(docCanvas).TransformPoint(new Point(0, ActualHeight + 1));

            // special case for search operators
            if (ViewModel.DataDocument.DocumentType.Equals(DashConstants.TypeStore.OperatorType))
            {
                if (ViewModel.DataDocument.GetField(KeyStore.OperatorKey) is SearchOperatorController)
                {
                    var operatorDoc = OperationCreationHelper.Operators["Search"].OperationDocumentConstructor();

                    operatorDoc.SetField(SearchOperatorController.InputCollection,
                        new DocumentReferenceController(ViewModel.DataDocument.Id,
                            SearchOperatorController.ResultsKey), true);

                    // TODO connect output to input

                    Actions.DisplayDocument(collection.ViewModel, operatorDoc, where);
                    return;
                }
            }

            collection.LoadNewActiveTextBox("", where, true);
        }

        private void OperatorEllipse_OnDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            args.Data.Properties[nameof(DragDocumentModel)] = new DragDocumentModel(ViewModel.DocumentController, false);
            args.AllowedOperations = DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;
            args.Data.RequestedOperation = DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
        }

        private void OperatorEllipseUnhighlight_OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            OperatorEllipseHighlight.Visibility = Visibility.Visible;
        }

        private void OperatorEllipseHighlight_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            OperatorEllipseHighlight.Visibility = Visibility.Collapsed;
        }

        private void xContextHyperLinkSizechanged(object sender, SizeChangedEventArgs e)
        {
            var frameworkElement = sender as FrameworkElement;
            if (frameworkElement != null)
                Canvas.SetLeft(xContextTitle, -frameworkElement.ActualWidth - 1);
        }

        private void xOperatorEllipseBorder_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            this.ManipulationMode = ManipulationModes.None;
            e.Handled = true;
        }

        private void xOperatorEllipseBorder_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            this.ManipulationMode = ManipulationModes.All;
        }

        private void xTitleIcon_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            ManipulationMode = e.GetCurrentPoint(this).Properties.IsRightButtonPressed ? ManipulationModes.None : ManipulationModes.All;
            if (ManipulationMode == ManipulationModes.All)
                e.Handled = true;
        }
    }
}