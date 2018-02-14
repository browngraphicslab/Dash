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

    public sealed partial class DocumentView : SelectionElement
    {
        public CollectionView ParentCollection => this.GetFirstAncestorOfType<CollectionView>(); // TODO document views should not be assumed to be in a collection this!

        public bool IsMainCollection { get; set; } //TODO document views should not be aware of if they are the main collection!

        /// <summary>
        /// Contains methods which allow the document to be moved around a free form canvas
        /// </summary>
        public ManipulationControls ManipulationControls;

        public DocumentViewModel ViewModel { get; set; }

        // the document view that is being dragged
        public static DocumentView DragDocumentView;

        public bool ProportionalScaling { get; set; }

        public static int dvCount = 0;

        private Storyboard _storyboard;

        public MenuFlyout MenuFlyout;

        private static readonly SolidColorBrush SingleSelectionBorderColor = new SolidColorBrush(Colors.LightGray);
        private static readonly SolidColorBrush GroupSelectionBorderColor = new SolidColorBrush(Colors.LightBlue);
        private bool _ptrIn;
        private bool _multiSelected;

        /// <summary>
        /// The width of the context preview
        /// </summary>
        private const double _contextPreviewActualWidth = 255;

        /// <summary>
        /// The height of the context preview
        /// </summary>
        private const double _contextPreviewActualHeight = 330;

        /// <summary>
        /// A reference to the actual context preview
        /// </summary>
        private UIElement _localContextPreview;
        private UIElement _selectedContextPreview;


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

            AddHandler(ManipulationCompletedEvent, new ManipulationCompletedEventHandler(DocumentView_ManipulationCompleted), true);
            AddHandler(PointerEnteredEvent, new PointerEventHandler(DocumentView_PointerEntered), true);
            AddHandler(PointerExitedEvent, new PointerEventHandler(DocumentView_PointerExited), true);
            AddHandler(TappedEvent, new TappedEventHandler(OnTapped), true);

            AddBorderRegionHandlers();


            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            Window.Current.CoreWindow.KeyUp += CoreWindow_KeyUp;

            MenuFlyout = xMenuFlyout;
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
            if (f1State.HasFlag(CoreVirtualKeyStates.Down))
            {
                if (_ptrIn) ShowLocalContext(true);
            }
            if (f2State.HasFlag(CoreVirtualKeyStates.Down))
            {
                if (_ptrIn) ShowSelectedContext();
            }
            

            if (ViewModel != null && (shiftState && !e.VirtualKey.Equals(VirtualKey.Shift)) &&
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
                xFieldContainer.BorderThickness = new Thickness(0);
            } else
            {
                this.CanDrag = true;
                xFieldContainer.BorderBrush = new SolidColorBrush(Colors.DodgerBlue);
                xFieldContainer.BorderThickness = new Thickness(2);
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

        private void AddBorderRegionHandlers()
        {
            foreach (var region in new FrameworkElement[] { xTitleBorder })
            {
                region.AddHandler(PointerEnteredEvent, new PointerEventHandler(BorderRegion_PointerEntered), true);
                region.AddHandler(PointerExitedEvent, new PointerEventHandler(BorderRegion_PointerExited), true);
            }
        }

        private void BorderRegion_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            ToggleGroupSelectionBorderColor(true);
        }

        private void BorderRegion_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            ToggleGroupSelectionBorderColor(false);
        }

        // since this is public it can be called with any parameters, be safe, check everything
        public void DocumentView_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            ToggleSelectionBorderAndChrome(false);
            ToggleGroupSelectionBorderColor(false);


            _ptrIn = false;
            var f1State = Window.Current.CoreWindow.GetKeyState(VirtualKey.F1);
            var f2State = Window.Current.CoreWindow.GetKeyState(VirtualKey.F2);
            if (f1State.HasFlag(CoreVirtualKeyStates.None)) ShowLocalContext(false);
        }



        // since this is public it can be called with any parameters, be safe, check everything
        public void DocumentView_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            ToggleSelectionBorderAndChrome(true);
            ToggleGroupSelectionBorderColor(true);

            _ptrIn = true;
            var f1State = Window.Current.CoreWindow.GetKeyState(VirtualKey.F1);
            var f2State = Window.Current.CoreWindow.GetKeyState(VirtualKey.F2);
            if (f1State.HasFlag(CoreVirtualKeyStates.Down)) ShowLocalContext(true);
            if (f2State.HasFlag(CoreVirtualKeyStates.Down)) ShowSelectedContext(); // TODO show selected row
        }


        public void DocumentView_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (sender is DocumentView docView)
                CheckForDropOnLink(docView);

            ToggleGroupSelectionBorderColor(false);
        }

        private void CheckForDropOnLink(DocumentView docView)
        {
            if (docView != null)
            {
                var docType = docView.ViewModel?.DocumentController?.GetActiveLayout()?.DocumentType;
                if (docType != null && docView.ViewModel?.DocumentController?.IsConnected == false)
                {
                    if (docType.Equals(DashConstants.TypeStore.OperatorBoxType))
                    {
                        //Get the coordinates of the view
                        Point screenCoords = docView.TransformToVisual(Window.Current.Content)
                            .TransformPoint(new Point(0, 0));

                        //parent freeform view
                        var freeformView = docView.ParentCollection?.CurrentView as CollectionFreeformView;
                        if (freeformView?.RefToLine != null && !IsConnected(docView))
                        {
                            // iterate through all the links in this freeform view to check for overlap
                            foreach (var link in freeformView.RefToLine)
                            {
                                //Get the slope of the line through the endpoints of the link
                                var converter = freeformView.LineToConverter[link.Value];

                                // first end point of link
                                var curvePoint1 = converter.Element1
                                    .TransformToVisual(freeformView.xItemsControl.ItemsPanelRoot)
                                    .TransformPoint(new Point(converter.Element1.ActualWidth / 2,
                                        converter.Element1.ActualHeight / 2));

                                // second end point of link
                                var curvePoint2 = converter.Element2
                                    .TransformToVisual(freeformView.xItemsControl.ItemsPanelRoot)
                                    .TransformPoint(new Point(converter.Element2.ActualWidth / 2,
                                        converter.Element2.ActualHeight / 2));

                                // calculate slope
                                var slope = (curvePoint2.Y - curvePoint1.Y) / (curvePoint2.X - curvePoint1.X);

                                // Figure out the x coordinates where the line intersects the top and bottom bounding horizontal lines of the rectangle of the document view
                                var intersectionTopX = curvePoint1.X - (1 / slope) * (-screenCoords.Y + curvePoint1.Y);
                                var intersectionBottomX =
                                    curvePoint1.X - (1 / slope) * (-(screenCoords.Y + docView.ActualHeight) + curvePoint1.Y);

                                // If the top intersection point is to the left of the documentView, or the bottom intersection is to the right, when the slope is positive,
                                // the link is outside the document.
                                if ((slope < 0 && !(intersectionTopX < screenCoords.X ||
                                                    intersectionBottomX > screenCoords.X + docView.ActualWidth)
                                     || slope > 0 && !(intersectionTopX > screenCoords.X ||
                                                       intersectionBottomX < screenCoords.X + docView.ActualWidth)))
                                {
                                    // if the document is between the vertical bounds of the link endpoints
                                    if (screenCoords.Y > (Math.Min(curvePoint1.Y, curvePoint2.Y))
                                        && (screenCoords.Y + docView.ActualHeight < (Math.Max(curvePoint1.Y, curvePoint2.Y))))
                                    {
                                        // connect the dropped document to the documents linked by the path
                                        ChangeConnections(freeformView, docView, link);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void DisconnectFromLink()
        {
            (ParentCollection.CurrentView as CollectionFreeformView)?.DeleteConnections(this);
            ViewModel.DocumentController.IsConnected = false;
        }

        /// <summary>
        /// Returns true if a document view is already linked to another, false if not
        /// </summary>
        /// <param name="docView"></param>
        /// <returns></returns>
        private bool IsConnected(DocumentView docView)
        {
            var userLinks = docView.ViewModel.DocumentController.GetField(KeyStore.UserLinksKey) as ListController<TextController>;
            if (userLinks == null || userLinks.Data.Count <= 0)
            {
                return false;
            }
            return true;
        }



        /// <summary>
        /// Changes the connections to connect the dropped document with the documents connected by the link
        /// </summary>
        /// <param name="ffView"></param>
        /// <param name="docView"></param>
        /// <param name="link"></param>
        private void ChangeConnections(CollectionFreeformView ffView, DocumentView docView, KeyValuePair<FieldReference, Path> link)
        {
            // the old connection is [referencedDoc] -> [referencingDoc]
            // the new connections are [referencedDoc] -> [droppedDoc] -> [referencingDoc]

            // get all the ingredients
            var droppedDoc = docView.ViewModel.DocumentController;
            var droppedDocOpFMController = droppedDoc.GetField(KeyStore.OperatorKey) as OperatorController;
            var droppedDocInputKey = droppedDocOpFMController.Inputs.Keys.FirstOrDefault();
            var droppedDocOutputKey = droppedDocOpFMController.Outputs.Keys.FirstOrDefault();

            var userLink = link.Value as UserCreatedLink;

            var referencingDoc = userLink.referencingDocument;
            var referencingKey = userLink.referencingKey;

            var referencedKey = userLink.referencedKey;
            var referencedDoc = userLink.referencedDocument;



            // Check if nodes inputs/outputs are of the same type
            var droppedDocOutputType = droppedDocOpFMController.Outputs[droppedDocOutputKey];
            var droppedDocInputType = droppedDocOpFMController.Inputs[droppedDocInputKey];

            var referencedDocOpFMController = referencedDoc.GetField(KeyStore.OperatorKey) as OperatorController;
            var referencedDocOutputType = referencedDocOpFMController?.Outputs[referencedKey];

            var referencingDocOpFMController = referencingDoc.GetField(KeyStore.OperatorKey) as OperatorController;
            var referencingDocInputType = referencingDocOpFMController?.Inputs[referencingKey];

            if (droppedDocOutputType == referencingDocInputType?.Type || referencedDocOutputType == droppedDocInputType?.Type)
            {
                // delete the current connection between referenced doc and referencing doc
                ffView.DeleteLine(link.Key, userLink); // check

                //Add connection between dropped and right node
                MakeConnection(ffView, droppedDoc, droppedDocOutputKey, referencingDoc, referencingKey);

                //Add connection between dropped and right node
                MakeConnection(ffView, referencedDoc, referencedKey, droppedDoc, droppedDocInputKey);

                referencedDoc.IsConnected = true;
                referencingDoc.IsConnected = true;
                droppedDoc.IsConnected = true;
            }
        }

        /// <summary>
        /// Makes a link between 2 documents
        /// </summary>
        /// <param name="ffView"></param>
        /// <param name="referencedDoc"></param>
        /// <param name="referencedKey"></param>
        /// <param name="referencingDoc"></param>
        /// <param name="referencingKey"></param>
        /// <returns></returns>
        private static void MakeConnection(CollectionFreeformView ffView, DocumentController referencedDoc, KeyController referencedKey, DocumentController referencingDoc, KeyController referencingKey)
        {
            // set the field of the referencing field to be a field reference to the referenced document/field
            var fieldRef = new DocumentFieldReference(referencedDoc.GetId(), referencedKey);
            var thisRef = (referencedDoc.GetDereferencedField(KeyStore.ThisKey, null));

            if (referencedDoc.DocumentType.Equals(DashConstants.TypeStore.OperatorBoxType) &&
                fieldRef is DocumentFieldReference && thisRef != null)
                referencingDoc.SetField(referencedKey, thisRef, true);
            else
            {
                referencingDoc.SetField(referencingKey,
                    new DocumentReferenceController(fieldRef.GetDocumentId(), referencedKey), true);
            }

            // add line visually
            ffView.AddLineFromData(fieldRef, new DocumentFieldReference(referencingDoc.GetId(), referencingKey));
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


        public DocumentController Choose()
        {
            // bring document to center? 
            var mainView = MainPage.Instance.GetMainCollectionView().CurrentView as CollectionFreeformView;
            if (mainView != null)
            {
                var pInWorld = Util.PointTransformFromVisual(new Point(Width / 2, Height / 2), this, mainView);
                var worldMid = new Point(mainView.ClipRect.Width / 2, mainView.ClipRect.Height / 2);
                mainView.Move(new TranslateTransform { X = worldMid.X - pInWorld.X, Y = worldMid.Y - pInWorld.Y });
            }
            return null;
        }

        private void This_Unloaded(object sender, RoutedEventArgs e)
        {
            //Debug.WriteLine($"Unloaded: Num DocViews = {--dvCount}");
            DraggerButton.Holding -= DraggerButtonHolding;
            DraggerButton.ManipulationDelta -= Dragger_OnManipulationDelta;
            DraggerButton.ManipulationCompleted -= Dragger_ManipulationCompleted;
        }


        private void This_Loaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null && !ViewModel.Undecorated)
            {
                xTitleIcon.Tapped += XTitleIcon_Tapped;
                // add manipulation code
                ManipulationControls = new ManipulationControls(OuterGrid, true, true, new List<FrameworkElement>(new FrameworkElement[] { xTitleIcon }));
                ManipulationControls.OnManipulatorTranslatedOrScaled += ManipulatorOnManipulatorTranslatedOrScaled;
            }

            //Debug.WriteLine($"Loaded: Num DocViews = {++dvCount}");
            DraggerButton.Holding -= DraggerButtonHolding;
            DraggerButton.Holding += DraggerButtonHolding;
            DraggerButton.ManipulationDelta -= Dragger_OnManipulationDelta;
            DraggerButton.ManipulationDelta += Dragger_OnManipulationDelta;
            DraggerButton.ManipulationCompleted -= Dragger_ManipulationCompleted;
            DraggerButton.ManipulationCompleted += Dragger_ManipulationCompleted;

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
                ManipulationControls.ManipulationCompleted(null, false); // TODO this causes groups to show up, and needs to be moved
                return;
            }
        }

        private void XTitleIcon_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ShowContext();
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
            DraggerButton.Margin = new Thickness(0, 0, -(20 - width), -20);
            xTitleIcon.Text = Application.Current.Resources["OperatorIcon"] as string;
            if (ParentCollection != null)
            {
                var dataDoc = ViewModel.DocumentController.GetDataDocument(null);
                dataDoc.SetTitleField(title);
            }
            xOperatorEllipseBorder.Visibility = Visibility.Collapsed;;
        }

        /// <summary>
        /// Applies custom override styles to the operator view. 
        /// width - the width of a single link node (generally App.xaml defines this, "InputHandleWidth")
        /// </summary>
        public void StyleCollection(CollectionView view)
        {
            xTitleIcon.Text = Application.Current.Resources["CollectionIcon"] as string;
            xDocumentBackground.Fill = ((SolidColorBrush)Application.Current.Resources["DocumentBackground"]);
        }

        public void StyleKeyValuePane()
        {
            xOperatorEllipseBorder.Visibility = Visibility.Collapsed; ;

        }

        #endregion


        private void ShowContext()
        {
            ViewModel.DocumentController.GetDataDocument(null).RestoreNeighboringContext();
        }


        MenuButton copyButton;

        DispatcherTimer _moveTimer = new DispatcherTimer()
        {
            Interval = new TimeSpan(0, 0, 0, 0, 600),
        };

        private bool _draggerButtonBeingManipulated;


        /// <summary>
        /// Update viewmodel when manipulator moves document
        /// </summary>
        /// <param name="delta"></param>
        private void ManipulatorOnManipulatorTranslatedOrScaled(TransformGroupData delta)
        {
            ToggleGroupSelectionBorderColor(true);
            ViewModel?.TransformDelta(delta);
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

        public void ProportionalResize(ManipulationDeltaRoutedEventArgs e)
        {
            var pos = Util.PointTransformFromVisual(e.Position, e.Container);
            var origin = Util.PointTransformFromVisual(new Point(0, 0), this);
            var projectedDelta = new Point(ActualWidth, ActualHeight).PointProjectArg(
                new Point(e.Delta.Translation.X, e.Delta.Translation.Y));
            var gt = ViewModel.GroupTransform;
            var scale = Math.Max(Math.Min((1 + projectedDelta.X / ActualWidth) * gt.ScaleAmount.X, 5), 0.2);
            ViewModel.GroupTransform = new TransformGroupData(gt.Translate, new Point(scale, scale));
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
        /// Resizes the control based on the user's dragging the DraggerButton.  The contents will adjust to fit the bounding box
        /// of the control *unless* the Shift button is held in which case the control will be resized but the contents will remain.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Dragger_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            ProportionalScaling = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);

            if (ProportionalScaling)
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

            if (!_draggerButtonBeingManipulated) Debug.WriteLine("Dragger Manipulation Started");

            _draggerButtonBeingManipulated = true;
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
            _draggerButtonBeingManipulated = false;
        }

        /// <summary>
        /// Updates the minimized-view icon from the ViewModel's corresponding IconType array.
        /// </summary>
        private void updateIcon()
        {
            return;
            //if (ViewModel == null) return;
            //// when you want a new icon, you have to add a check for it here!
            //if (ViewModel.IconType == IconTypeEnum.Document)
            //{
            //    xIconImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/doc-icon.png"));
            //}
            //else if (ViewModel.IconType == IconTypeEnum.Collection)
            //{
            //    xIconImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/col-icon.png"));
            //}
            //else if (ViewModel.IconType == IconTypeEnum.Api)
            //{
            //    xIconImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/api-icon.png"));
            //}
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
            //var sourceBinding = new Binding
            //{
            //    Source = ViewModel.DocumentController.DocumentModel.DocumentType,
            //    Path = new PropertyPath(nameof(ViewModel.DocumentController.DocumentModel.DocumentType.Type)),
            //    Mode = BindingMode.TwoWay,
            //    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            //};
            //xIconLabel.SetBinding(TextBox.TextProperty, sourceBinding);

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
            if (ViewModel != null)
            {
                updateIcon();
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
                (ParentCollection.CurrentView as CollectionFreeformView)?.AddToStoryboard(FadeOut, this);
                FadeOut.Begin();

                if (addTextBox)
                {
                    (ParentCollection.CurrentView as CollectionFreeformView)?.
                        RenderPreviewTextbox(ViewModel.GroupTransform.Translate);
                }
            }
        }

        private void CopyDocument()
        {
            _moveTimer.Stop();


            // will this screw things up?
            Canvas.SetZIndex(this.GetFirstAncestorOfType<ContentPresenter>(), 0);

            ParentCollection?.ViewModel.AddDocument(ViewModel.DocumentController.GetCopy(null), null);
        }
        private void CopyViewDocument()
        {
            _moveTimer.Stop();

            // will this screw things up?
            Canvas.SetZIndex(this.GetFirstAncestorOfType<ContentPresenter>(), 0);

            ParentCollection?.ViewModel.AddDocument(ViewModel.DocumentController.GetViewCopy(null), null);
            //xDelegateStatusCanvas.Visibility = ViewModel.DocumentController.HasDelegatesOrPrototype ? Visibility.Visible : Visibility.Collapsed;  // TODO theoretically the binding should take care of this..
        }

        private void CopyDataDocument()
        {
            ParentCollection.ViewModel.AddDocument(ViewModel.DocumentController.GetDataCopy(), null);
        }

        private void ShowPreviewDocument()
        {
            ParentCollection.ViewModel.AddDocument(ViewModel.DocumentController.GetPreviewDocument(), null);
        }

        private void KeyValueViewDocument()
        {
            ParentCollection.ViewModel.AddDocument(ViewModel.DocumentController.GetKeyValueAlias(), null);
        }

        private void InstanceDataDocument()
        {
            ParentCollection.ViewModel.AddDocument(ViewModel.DocumentController.GetDataInstance(), null);
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
            (ParentCollection?.CurrentView as CollectionFreeformView)?.DeleteConnections(this);
            ParentCollection?.ViewModel.RemoveDocument(ViewModel.DocumentController);
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

                    //DBTest.ResetCycleDetection();
                    docController.ParseDocField(key, valu);
                }
        }
        #endregion

        #region Activation

        public Rect ClipRect => new Rect();

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

            //Selects it and brings it to the foreground of the canvas, in front of all other documents.
            if (ParentCollection != null && this.GetFirstAncestorOfType<ContentPresenter>() != null)
            {
                var zindex = Canvas.GetZIndex(this.GetFirstAncestorOfType<ContentPresenter>());
                if (zindex > -100)
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
            if (_draggerButtonBeingManipulated)
            {
                OperatorEllipse.Visibility = DraggerButton.Visibility = Visibility.Visible;
                xSelectionBorder.BorderThickness = new Thickness(3);
                xTitleIcon.Foreground = (SolidColorBrush) Application.Current.Resources["TitleText"];
            }
            else
            {
                OperatorEllipse.Visibility = DraggerButton.Visibility = isBorderOn && isOtherChromeVisible && ViewModel?.Undecorated == false ? Visibility.Visible : Visibility.Collapsed;
                xSelectionBorder.BorderThickness = isBorderOn ? new Thickness(3) : new Thickness(0);
                xTitleIcon.Foreground = isBorderOn && isOtherChromeVisible && ViewModel?.Undecorated == false
                    ? (SolidColorBrush)Application.Current.Resources["TitleText"]
                    : new SolidColorBrush(Colors.Transparent);
            }
        }

        private void ToggleGroupSelectionBorderColor(bool isGroupBorderVisible)
        {
            // get all the document views that are in the same collection as ourself
            var allDocumentViews = (ParentCollection?.CurrentView as CollectionFreeformView)?.DocumentViews;
            if (allDocumentViews == null) return;
            return;

            // get all the document views connected to ourself (forming a group)
            var documentGroup = AddConnected(new List<DocumentView>(), allDocumentViews);

            // if we are by ourself then hide the border
            if (documentGroup.Count < 2) isGroupBorderVisible = false;


            // iterate over all the documents in the collection
            foreach (var dv in allDocumentViews)
            {
                // turn on the borders for documents in the group
                if (documentGroup.Contains(dv))
                {
                    // don't turn on our own border (for aesthetic reasons)
                    if (dv != this)
                    {
                        dv.ToggleSelectionBorderAndChrome(isGroupBorderVisible, false);
                    }


                    dv.xSelectionBorder.BorderBrush = isGroupBorderVisible
                        ? GroupSelectionBorderColor
                        : SingleSelectionBorderColor;
                }
                // turn off the borders for documents not in the group
                else
                {
                    dv.ToggleSelectionBorderAndChrome(false);
                }


            }

            //StackGroup();
        }
        /*
        public void StackGroup()
        {
            if (DocumentGroup == null)
            {
                return;
            }
            var ordered = DocumentGroup.Where(d => d != null && (d.ViewModel.DocumentController.GetField(KeyStore.PositionFieldKey) as PointController) != null).Select(doc => doc.ViewModel).OrderBy(vm => (vm.DocumentController.GetField(KeyStore.PositionFieldKey) as PointController).Data.Y).ToArray();
            var length = ordered.Length;
            for (int i = 1; i < length; i++)
            {
                ordered[i].GroupTransform = new TransformGroupData(
                    new Point(ordered[i].GroupTransform.Translate.X, ordered[i - 1].GroupTransform.Translate.Y + ordered[i - 1].Height + 5)
                    , ordered[i].GroupTransform.ScaleCenter
                    , ordered[i].GroupTransform.ScaleAmount);
            }
        }
        */

        public List<DocumentView> AddConnected(List<DocumentView> grouped, List<DocumentView> documentViews)
        {
            grouped = grouped ?? new List<DocumentView>();
            var docRootBounds = ViewModel.GroupingBounds;
            foreach (var doc in documentViews)
            {
                var docBounds = doc.ViewModel.GroupingBounds;
                docBounds.Intersect(docRootBounds);
                if (docBounds == Rect.Empty || grouped.Contains(doc)) continue;
                grouped.Add(doc);
                doc.AddConnected(grouped, documentViews);
            }

            /*
            var ordered = grouped.Where(d => d != null && (d.ViewModel.DocumentController.GetField(KeyStore.PositionFieldKey) as PointController) != null).Select(doc => doc.ViewModel).OrderBy(vm => (vm.DocumentController.GetField(KeyStore.PositionFieldKey) as PointController).Data.Y).ToArray();
            var length = ordered.Length;
            for (int i = 1; i < length; i++)
            {
                ordered[i].GroupTransform = new TransformGroupData(
                    new Point(ordered[i].GroupTransform.Translate.X, ordered[i - 1].GroupTransform.Translate.Y + ordered[i - 1].Height + 5)
                    , ordered[i].GroupTransform.ScaleCenter
                    , ordered[i].GroupTransform.ScaleAmount);
            }*/
            
            return grouped;
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

        private void MenuFlyoutItemCopy_Click(object sender, RoutedEventArgs e)
        {
            CopyDocument();
        }

        private void MenuFlyoutItemAlias_Click(object sender, RoutedEventArgs e)
        {
            CopyViewDocument();
        }

        private void MenuFlyoutItemDelete_Click(object sender, RoutedEventArgs e)
        {
            DeleteDocument();
        }

        private void MenuFlyoutItemFields_Click(object sender, RoutedEventArgs e)
        {
            KeyValueViewDocument();
        }

        public void MenuFlyoutItemPreview_Click(object sender, RoutedEventArgs e)
        {
            ShowPreviewDocument();
        }


        private void MenuFlyoutItemContext_Click(object sender, RoutedEventArgs e)
        {
            ShowContext();
        }

        private void MenuFlyoutItemScreenCap_Click(object sender, RoutedEventArgs e)
        {
            ScreenCap();

        }


        #endregion

        #region hide helpers for key value pane
        /// <summary>
        /// Hides the dragger button for the KeyValuePane
        /// </summary>
        internal void hideDraggerButton()
        {
            DraggerButton.Visibility = Visibility.Collapsed;
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

        private void XMetadataPanel_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            xMetadataPanel.Margin = new Thickness(-xMetadataPanel.ActualWidth, 0, 0, 0);
        }

        private void CopyHistory_Click(object sender, RoutedEventArgs e)
        {
            var data = new DataPackage() { };
            data.SetText(string.Join("\n",
                (ViewModel.DocumentController.GetAllContexts() ?? new List<DocumentContext>()).Select(
                    c => c.Title + "  :  " + c.Url)));
            Clipboard.SetContent(data);
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

        private void OperatorEllipse_OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Ellipse ellipse)
            {
                ellipse.Fill = new SolidColorBrush(Colors.Gold);
                ellipse.Height += 3;
                ellipse.Width += 3;
            }
        }

        private void OperatorEllipse_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Ellipse ellipse)
            {
                ellipse.Fill = (SolidColorBrush) App.Instance.Resources["FieldHandleColor"];
                ellipse.Height -= 3;
                ellipse.Width -= 3;
            }
        }

        private void MenuFlyoutItemOpen_OnClick(object sender, RoutedEventArgs e)
        {
            MainPage.Instance.SetCurrentWorkspace((DataContext as DocumentViewModel).DocumentController);
        }

        private void xContextHyperLinkSizechanged(object sender, SizeChangedEventArgs e)
        {
            var frameworkElement = sender as FrameworkElement;
            if (frameworkElement != null)
                Canvas.SetLeft(xContextTitle, -frameworkElement.ActualWidth - 1);
        }
    }
}