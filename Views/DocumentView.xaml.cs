﻿using DashShared;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Visibility = Windows.UI.Xaml.Visibility;
using Dash.Models.DragModels;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236
namespace Dash
{
    public sealed partial class DocumentView
    {
        public CollectionView ParentCollection => this.GetFirstAncestorOfType<CollectionView>();
       
        /// <summary>
        /// Contains methods which allow the document to be moved around a free form canvas
        /// </summary>
        public ManipulationControls ManipulationControls { get; set; }

        public DocumentViewModel ViewModel { get { return DataContext == null ? null : DataContext as DocumentViewModel; } }
        
        public MenuFlyout MenuFlyout { get; set; }

        static readonly SolidColorBrush SingleSelectionBorderColor = new SolidColorBrush(Colors.LightGray);
        static readonly SolidColorBrush GroupSelectionBorderColor  = new SolidColorBrush(Colors.LightBlue);

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
        
        public static readonly DependencyProperty BindRenderTransformProperty = DependencyProperty.Register(
            "BindRenderTransform", typeof(bool), typeof(DocumentView), new PropertyMetadata(default(bool)));

        public bool BindRenderTransform
        {
            get { return (bool)GetValue(BindRenderTransformProperty); }
            set { SetValue(BindRenderTransformProperty, value); }
        }
        // == CONSTRUCTORs ==

        public DocumentView()
        {
            InitializeComponent();

            Util.InitializeDropShadow(xShadowHost, xDocumentBackground);
            
            // set bounds
            MinWidth = 5;
            MinHeight = 25;

            RegisterPropertyChangedCallback(BindRenderTransformProperty, updateBindings);

            void updateBindings(object sender, DependencyProperty dp)
            {
                var doc = ViewModel?.LayoutDocument;

                var binding =  !BindRenderTransform || doc == null ? null :
                        new FieldMultiBinding<MatrixTransform>(new DocumentFieldReference(doc.Id, KeyStore.PositionFieldKey),
                                                               new DocumentFieldReference(doc.Id, KeyStore.ScaleAmountFieldKey)) {
                        Converter = new TransformGroupMultiConverter(),
                        Context = new Context(doc),
                        Mode = BindingMode.OneWay
                    };
                this.AddFieldBinding(RenderTransformProperty, binding);
            }
            
            Loaded += (sender, e) => {
                updateBindings(null, null);
                DataContextChanged += (s, a) => updateBindings(null, null);
                Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
                Window.Current.CoreWindow.KeyUp   += CoreWindow_KeyUp;
            };
            Unloaded += (sender, e) =>
            {
                Window.Current.CoreWindow.KeyDown -= CoreWindow_KeyDown;
                Window.Current.CoreWindow.KeyUp   -= CoreWindow_KeyUp;
            };

            PointerPressed += (sender, e) =>
            {
                var right = e.GetCurrentPoint(this).Properties.IsRightButtonPressed;
                var parentFreeform = this.GetFirstAncestorOfType<CollectionFreeformView>();
                var parentParentFreeform = parentFreeform?.GetFirstAncestorOfType<CollectionFreeformView>();
                ManipulationMode = right && parentFreeform != null && (this.IsShiftPressed() || parentParentFreeform == null) ? ManipulationModes.All : ManipulationModes.None;
            };
            PointerEntered += DocumentView_PointerEntered;
            PointerExited  += DocumentView_PointerExited;
            RightTapped    += (s,e) => DocumentView_OnTapped(null,null);
            AddHandler(TappedEvent, new TappedEventHandler(DocumentView_OnTapped), true);  // RichText and other controls handle Tapped events

            SizeChanged += (sender, e) => {
                ViewModel?.UpdateActualSize(this.ActualWidth, this.ActualHeight);
                PositionContextPreview();
            };

            // setup ResizeHandles
            void ResizeHandles_OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
            {
                if (!this.IsRightBtnPressed()) // ignore right button drags
                {
                    e.Handled = true;
                    PointerExited -= DocumentView_PointerExited;// ignore any pointer exit events which will change the visibility of the dragger
                }
            }
            void ResizeHandles_restorePointerTracking()
            {
                ViewModel.DecorationState = ResizeHandleBottomRight.IsPointerOver();
                PointerExited -= DocumentView_PointerExited;
                PointerExited += DocumentView_PointerExited;
                
            };
            ResizeHandleTopLeft.ManipulationDelta     += (s, e) => Resize(s as FrameworkElement, e, true,  true); 
            ResizeHandleTopRight.ManipulationDelta    += (s, e) => Resize(s as FrameworkElement, e, true,  false);
            ResizeHandleBottomLeft.ManipulationDelta  += (s, e) => Resize(s as FrameworkElement, e, false, true);
            ResizeHandleBottomRight.ManipulationDelta += (s, e) => Resize(s as FrameworkElement, e, false, false);
            
            foreach (var handle in new Ellipse[] { ResizeHandleBottomLeft, ResizeHandleBottomRight, ResizeHandleTopLeft, ResizeHandleTopRight })
            {
                handle.ManipulationStarted   += ResizeHandles_OnManipulationStarted;
                handle.ManipulationCompleted += (s, e) => { ResizeHandles_restorePointerTracking(); e.Handled = true; }; // call Snap() if resizing should snap
                handle.PointerReleased       += (s, e) => ResizeHandles_restorePointerTracking();
                handle.PointerPressed        += (s, e) =>
                {
                    CapturePointer(e.Pointer);
                    ManipulationMode = ManipulationModes.None;
                    e.Handled = !e.GetCurrentPoint(this).Properties.IsRightButtonPressed;
                };
            }

            // setup OperatorEllipse 
            OperatorEllipseHighlight.PointerExited    += (sender, e) => OperatorEllipseHighlight.Visibility = Visibility.Collapsed;
            OperatorEllipseUnhighlight.PointerEntered += (sender, e) => OperatorEllipseHighlight.Visibility = Visibility.Visible;
            xOperatorEllipseBorder.PointerPressed += (sender, e) => {
                this.ManipulationMode = ManipulationModes.None;
                e.Handled = !e.GetCurrentPoint(this).Properties.IsRightButtonPressed;
            };
            xOperatorEllipseBorder.PointerReleased += (sender, e) => ManipulationMode = ManipulationModes.All;
            xOperatorEllipseBorder.DragStarting += (sender, args) =>
            {
                var selected = (ParentCollection.CurrentView as CollectionFreeformView)?.SelectedDocs.Select((dv) => dv.ViewModel.DocumentController);
                if (selected?.Count() > 0)
                {
                    args.Data.Properties[nameof(List<DragDocumentModel>)] =
                            new List<DragDocumentModel>(selected.Select((s) => new DragDocumentModel(s, true)));
                }
                else
                    args.Data.Properties[nameof(DragDocumentModel)] = new DragDocumentModel(ViewModel.DocumentController, false);
                args.AllowedOperations = DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;
                args.Data.RequestedOperation = DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
            };

            // setup Title Icon
            xTitleIcon.PointerPressed += (sender, e) =>  {
                CapturePointer(e.Pointer);
                ManipulationMode = e.GetCurrentPoint(this).Properties.IsRightButtonPressed ? ManipulationModes.None : ManipulationModes.All;
                e.Handled = ManipulationMode == ManipulationModes.All;
            };
            xTitleIcon.Tapped += (s, args) =>
            {
                ShowContext();
                args.Handled = true;
            };
            // setup Context Title
            xContextTitle.Tapped += (sender, e) => ShowContext();
            xContextTitle.SizeChanged += (sender, e) =>  Canvas.SetLeft(xContextTitle, -xContextTitle.ActualWidth - 1);
            
            // add manipulation code
            ManipulationControls = new ManipulationControls(this);
            ManipulationControls.OnManipulatorTranslatedOrScaled += (delta) => SelectedDocuments().ForEach((d) => d.TransformDelta(delta));
            ManipulationControls.OnManipulatorStarted += () => {
                if (!this.IsShiftPressed() && ViewModel.DocumentController.DocumentType.Equals(BackgroundBox.DocumentType))
                {
                    if (ParentCollection.CurrentView is CollectionFreeformView cview)
                    {
                        cview.SelectDocs(cview.DocsInMarquee(new Rect(ViewModel.Position, new Size(ActualWidth, ActualHeight))));
                    }
                }
                SelectedDocuments().ForEach((d) =>
                {
                    d.ViewModel.InteractiveManipulationPosition = d.ViewModel.Position;  // initialize the cached values of position and scale
                        d.ViewModel.InteractiveManipulationScale = d.ViewModel.Scale;
                });
            };
            ManipulationControls.OnManipulatorCompleted += () =>
            {
                SelectedDocuments().ForEach((d) =>
                {
                    d.ViewModel.Position = d.ViewModel.InteractiveManipulationPosition; // write the cached values of position and scale back to the viewModel
                    d.ViewModel.Scale = d.ViewModel.InteractiveManipulationScale;
                });
                if (ViewModel.DocumentController.DocumentType.Equals(BackgroundBox.DocumentType))
                {
                    if (ParentCollection.CurrentView is CollectionFreeformView cview)
                    {
                        cview.DeselectAll();
                    }
                }
            };

            MenuFlyout = xMenuFlyout;

            xMenuFlyout.Opened += (s, e) =>
            {
                if (this.IsShiftPressed())
                    xMenuFlyout.Hide();
            };
        }

        /// <summary> 
        /// Updates the cached position and scale of the document without modifying the underlying viewModel.  
        /// At the end of the interaction, the caches are copied to the viewModel.
        /// </summary>
        /// <param name="delta"></param>
        public void TransformDelta(TransformGroupData delta)
        {
            var currentTranslate = ViewModel.InteractiveManipulationPosition;  
            var currentScaleAmount = ViewModel.InteractiveManipulationScale;

            var deltaTranslate = delta.Translate;
            var deltaScaleAmount = delta.ScaleAmount;
            var scaleAmount = new Point(currentScaleAmount.X * deltaScaleAmount.X, currentScaleAmount.Y * deltaScaleAmount.Y);
            var translate = new Point(currentTranslate.X + deltaTranslate.X, currentTranslate.Y + deltaTranslate.Y);

            ViewModel.InteractiveManipulationPosition = translate;
            ViewModel.InteractiveManipulationScale = scaleAmount; 
            RenderTransform = TransformGroupMultiConverter.ConvertDataToXamlHelper(new List<object> { translate, scaleAmount }); 
        }
        
        /// <summary>
        /// Handles keypress events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CoreWindow_KeyUp(CoreWindow sender, KeyEventArgs args)
        {
            if (!this.IsF1Pressed())
                ShowLocalContext(false);
        }
        private void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs e)
        {
            if (this.IsF1Pressed() && this.IsPointerOver())
            {
                ShowLocalContext(true);
            }
            if (this.IsF2Pressed() && this.IsPointerOver())
            {
                ShowSelectedContext();
            }
            
            var focused = (FocusManager.GetFocusedElement() as FrameworkElement)?.DataContext as DocumentViewModel;

            if (ViewModel != null && ViewModel.Equals(focused) && 
                this.IsShiftPressed() && !e.VirtualKey.Equals(VirtualKey.Shift) && e.VirtualKey.Equals(VirtualKey.Enter)) // shift + Enter
            {
                // don't shift enter on KeyValue documents (since they already display the key/value adding)
                if (!ViewModel.LayoutDocument.DocumentType.Equals(KeyValueDocumentBox.DocumentType) &&
                    !ViewModel.DocumentController.DocumentType.Equals(DashConstants.TypeStore.MainDocumentType))
                    HandleShiftEnter();
            }
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
                if (context != null && _localContextPreview == null)
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

                var context = ViewModel.DataDocument
                    .GetDereferencedField<DocumentController>(KeyStore.SelectedSchemaRow, null)?.GetFirstContext();
                if (context == null) return;

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
                void OnSelectedSchemaRowUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context1)
                {
                    ShowSelectedContext(true);
                }
                ViewModel.DataDocument.RemoveFieldUpdatedListener(KeyStore.SelectedSchemaRow, OnSelectedSchemaRowUpdated);
                ViewModel.DataDocument.AddFieldUpdatedListener(KeyStore.SelectedSchemaRow, OnSelectedSchemaRowUpdated);
            }
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

        #region Xaml Styling Methods (used by operator/collection view)

        /// <summary>
        /// Applies custom override styles to the operator view. 
        /// width - the width of a single link node (generally App.xaml defines this, "InputHandleWidth")
        /// </summary>
        public void StyleOperator(double width, string title)
        {
            xTitleIcon.Text = Application.Current.Resources["OperatorIcon"] as string;
            if (ParentCollection != null)
            {
                var dataDoc = ViewModel.DocumentController.GetDataDocument();
                dataDoc.SetTitleField(title);
            }
        }

        /// <summary>
        /// Applies custom override styles to the collection view. 
        /// width - the width of a single link node (generally App.xaml defines this, "InputHandleWidth")
        /// </summary>
        public void StyleCollection(CollectionView view)
        {
            xTitleIcon.Text = Application.Current.Resources["CollectionIcon"] as string;
            xDocumentBackground.Fill = ((SolidColorBrush)Application.Current.Resources["DocumentBackground"]);
            if (this != MainPage.Instance.MainDocView) return;
            view.xOuterGrid.BorderThickness = new Thickness(0);
            ResizeHandleTopLeft.Visibility = Visibility.Collapsed;
            ResizeHandleBottomLeft.Visibility = Visibility.Collapsed;
            ResizeHandleBottomRight.Visibility = Visibility.Collapsed;
            ResizeHandleTopRight.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Applies custom override styles to the KeyValuePane view. 
        /// width - the width of a single link node (generally App.xaml defines this, "InputHandleWidth")
        /// </summary>
        public void StyleKeyValuePane() { }

        #endregion

        /// <summary>
        /// Resizes the control based on the user's dragging the ResizeHandles.  The contents will adjust to fit the bounding box
        /// of the control *unless* the Shift button is held in which case the control will be resized but the contents will remain.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Resize(FrameworkElement sender, ManipulationDeltaRoutedEventArgs e, bool shiftTop, bool shiftLeft)
        {

            /// <summary>
            /// Resizes the document while keeping its original width/height ratio.
            /// </summary>
            /// <param name="e"></param>
            void ProportionalResize(ManipulationDeltaRoutedEventArgs args)
            {

                /*
                var curScale = ViewModel.Scale;
                var pos = Util.PointTransformFromVisual(e.Position, e.Container);
                var origin = Util.PointTransformFromVisual(new Point(0, 0), this);
                var projectedDelta = new Point(ActualWidth, ActualHeight).PointProjectArg(
                    new Point(e.Delta.Translation.X / curScale.X, e.Delta.Translation.Y / curScale.Y));
                var scale = Math.Max(Math.Min((1 + projectedDelta.X / ActualWidth) * curScale.X, 5), 0.2);
                ViewModel.Scale = new Point(scale, scale);
                */
            }

            if (this.IsRightBtnPressed())
                return; // let the manipulation fall through to an ancestor when Rightbutton dragging

            var p = Util.DeltaTransformFromVisual(e.Delta.Translation, sender as FrameworkElement);

            // set old and new sizes for change in height/width comparisons
            Size oldSize = new Size(ViewModel.ActualWidth, ViewModel.ActualHeight);
            oldSize.Height = double.IsNaN(oldSize.Height) ? ViewModel.ActualHeight / ViewModel.ActualWidth * oldSize.Width : oldSize.Height;
            Size newSize = new Size();

            // sets directions/weights depending on which handle was dragged as mathematical manipulations
            int cursorXDirection = shiftLeft ? -1 : 1;
            int cursorYDirection = shiftTop ? -1 : 1;
            int moveXScale = shiftLeft ? 1 : 0;
            int moveYScale = shiftTop ? 1 : 0;

            if (this.IsCtrlPressed())
            {
                // proportional resizing
                var diffX = cursorXDirection * p.X;
                newSize = Resize(diffX, ViewModel.ActualHeight / ViewModel.ActualWidth * diffX);
            }
            else
            {
                // significance of the direction weightings: if the left handles are dragged to the left, should resize larger instead of smaller as p.X would say. So flip the negative sign by multiplying by -1.
                newSize = Resize(cursorXDirection * p.X, cursorYDirection * p.Y);
                
                // can't have undefined heights for calculating delta-h for adjusting XPos and YPos
                newSize.Height = double.IsNaN(newSize.Height)
                    ? ViewModel.ActualHeight / ViewModel.ActualWidth * newSize.Width
                    : newSize.Height;
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
                if (ViewModel != null && !(MainPage.Instance.Content as Grid).Children.Contains(this))
                {
                    // if Height is NaN but width isn't, then we want to keep Height as NaN and just change width.  This happens for some images to coerce proportional scaling.
                    var w = !double.IsNaN(ViewModel.Height) ? ViewModel.Width : ViewModel.ActualWidth;
                    var h = double.IsNaN(ViewModel.Height) && ViewModel.Content is CollectionView ? ViewModel.ActualHeight : ViewModel.Height;
                    ViewModel.Width = Math.Max(w + dx, MinWidth);
                    ViewModel.Height = Math.Max(h + dy, MinHeight);
                    return new Size(ViewModel.Width, ViewModel.Height);
                }
                return new Size();
            }

            // if one of the scales is 0, it means that dimension doesn't get repositioned (differs depending on handle)
            ViewModel.Position = new Point(
                 (ViewModel.XPos - moveXScale * (newSize.Width - oldSize.Width) * ViewModel.Scale.X),
                 (ViewModel.YPos - moveYScale * (newSize.Height - oldSize.Height) * ViewModel.Scale.Y));

            e.Handled = true;

            if (!this.IsShiftPressed())
            {
                //uncomment to make children in collection stretch
                fitFreeFormChildrenToTheirLayouts();
            }
        }

        /// <summary>
        /// If the documentView contains a FreeformCollection, resizes the (TODO: is this right) first
        /// DocumentVIew in that collection to be the size of the FreeformCollection.
        /// </summary>
        void fitFreeFormChildrenToTheirLayouts()
        {
            var freeFormChild = VisualTreeHelperExtensions.GetFirstDescendantOfType<CollectionFreeformView>(this);
            var parentOfFreeFormChild = freeFormChild != null ? VisualTreeHelperExtensions.GetFirstAncestorOfType<DocumentView>(freeFormChild) : null;
            if (this == parentOfFreeFormChild)
            {   // if this document directly contains a free form child, then initialize its contents to fit its layout.
                freeFormChild?.ViewManipulationControls?.FitToParent();
            }
        }

        // Controls functionality for the Right-click context menu
        #region Menu

        /// <summary>
        /// Brings the element to the front of its containing parent canvas.
        /// </summary>
        public void ToFront()
        {
            if (ParentCollection == null || ViewModel?.DocumentController?.DocumentType?.Equals(BackgroundBox.DocumentType) == true)
                return;
            ParentCollection.MaxZ += 1;
            Canvas.SetZIndex(this.GetFirstAncestorOfType<ContentPresenter>(), ParentCollection.MaxZ);
        }
        public void ForceRightTapContextMenu()
        {
            xMenuFlyout.ShowAt(this, MainPage.Instance.TransformToVisual(this).TransformPoint(this.RootPointerPos()));
        }
        public void DeleteDocument(bool addTextBox=false)
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
        private void CopyDocument()
        {
            // will this screw things up?
            Canvas.SetZIndex(this.GetFirstAncestorOfType<ContentPresenter>(), 0);

            ParentCollection?.ViewModel.AddDocument(ViewModel.DocumentController.GetCopy(null), null);
        }
        private void CopyViewDocument()
        {
            // will this screw things up?
            Canvas.SetZIndex(this.GetFirstAncestorOfType<ContentPresenter>(), 0);

            ParentCollection?.ViewModel.AddDocument(ViewModel.DocumentController.GetViewCopy(null), null);
            //xDelegateStatusCanvas.Visibility = ViewModel.DocumentController.HasDelegatesOrPrototype ? Visibility.Visible : Visibility.Collapsed;  // TODO theoretically the binding should take care of this..
        }
        private void KeyValueViewDocument()
        {
            ParentCollection?.ViewModel.AddDocument(ViewModel.DocumentController.GetKeyValueAlias(), null);
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

        public void SetSelectionBorder(bool selected)
        {
            xTargetContentGrid.BorderThickness = selected ? new Thickness(3) : new Thickness(0);
            xTargetContentGrid.BorderBrush = selected ? GroupSelectionBorderColor : new SolidColorBrush(Colors.Transparent);
        }
        /// <summary>
        /// Returns the currently selected documents, or just this document if nothing is selected
        /// </summary>
        List<DocumentView> SelectedDocuments()
        {
            var marqueeDocs = (ParentCollection?.CurrentView as CollectionFreeformView)?.SelectedDocs;
            if (marqueeDocs != null && marqueeDocs.Contains(this))
                return marqueeDocs.ToList();
            return new List<DocumentView>(new DocumentView[] { this } );
        }

        #endregion
        public void DocumentView_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (!ViewModel.DocumentController.DocumentType.Equals(BackgroundBox.DocumentType))
            {
                ToFront();
            }
        }
        public void DocumentView_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (e == null|| ( !e.GetCurrentPoint(this).Properties.IsRightButtonPressed && ! e.GetCurrentPoint(this).Properties.IsLeftButtonPressed))
                ViewModel.DecorationState = false;
            MainPage.Instance.HighlightTreeView(ViewModel.DocumentController, false);
        }
        public void DocumentView_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            ViewModel.DecorationState = ViewModel?.Undecorated == false;
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 1);
            MainPage.Instance.HighlightTreeView(ViewModel.DocumentController, true);
        }

        #region UtilityFuncions
        public bool MoveToContainingCollection(List<DocumentView> overlappedViews)
        {
            var selectedDocs = SelectedDocuments();

            var collection = this.GetFirstAncestorOfType<CollectionView>();
            if (collection == null || ViewModel == null || selectedDocs == null)
                return false;

            foreach (var nestedDocument in overlappedViews)
            {
                var nestedCollection = nestedDocument.GetFirstDescendantOfType<CollectionView>();
                if (nestedCollection != null && !nestedCollection.GetAncestors().ToList().Contains(this))
                {
                    if (!nestedCollection.Equals(collection))
                    {
                        foreach (var selDoc in selectedDocs)
                        {
                            var pos = selDoc.TransformToVisual(MainPage.Instance).TransformPoint(new Point());
                            var where = nestedCollection.CurrentView is CollectionFreeformView ?
                                Util.GetCollectionFreeFormPoint((nestedCollection.CurrentView as CollectionFreeformView), pos) :
                                new Point();
                            nestedCollection.ViewModel.AddDocument(selDoc.ViewModel.DocumentController.GetSameCopy(where), null);
                            collection.ViewModel.RemoveDocument(selDoc.ViewModel.DocumentController);

                        }
                        return true;
                    }
                    else break;
                }
            }
            return false;
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

        #endregion
        #region Context menu click handlers

        private void MenuFlyoutItemCopy_Click(object sender, RoutedEventArgs e)
        {
            foreach (var doc in SelectedDocuments())
                doc.CopyDocument();
        }
        private void MenuFlyoutItemAlias_Click(object sender, RoutedEventArgs e)
        {
            foreach (var doc in SelectedDocuments())
                doc.CopyViewDocument();
        }
        private void MenuFlyoutItemDelete_Click(object sender, RoutedEventArgs e)
        {
            foreach (var doc in SelectedDocuments())
                doc.DeleteDocument();
        }
        private void MenuFlyoutItemFields_Click(object sender, RoutedEventArgs e)
        {
            foreach (var doc in SelectedDocuments())
                doc.KeyValueViewDocument();
        }
        public void MenuFlyoutItemPreview_Click(object sender, RoutedEventArgs e) { ParentCollection.ViewModel.AddDocument(ViewModel.DataDocument.GetPreviewDocument(new Point(ViewModel.LayoutDocument.GetPositionField().Data.X + ActualWidth, ViewModel.LayoutDocument.GetPositionField().Data.Y)), null) ; }
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
        private void MenuFlyoutLaunch_Click(object sender, RoutedEventArgs e) {
            var text = ViewModel.DocumentController.GetField(KeyStore.SystemUriKey) as TextController;
            if (text != null)
               Launcher.QueryAppUriSupportAsync(new Uri(text.Data));

            //var storageFile = item as StorageFile;
            //var fields = new Dictionary<KeyController, FieldControllerBase>
            //{
            //    [KeyStore.SystemUriKey] = new TextController(storageFile.Path + storageFile.Name)
            //};
            //var doc = new DocumentController(fields, DashConstants.TypeStore.FileLinkDocument);
        }

        #endregion

        void This_Drop(object sender, DragEventArgs e)
        {
            var footer = sender == this.xFooter;
            xFooter.Visibility = xHeader.Visibility = Visibility.Collapsed;
            var dragModel = (DragDocumentModel)e.DataView.Properties[nameof(DragDocumentModel)];

            if (dragModel?.DraggedKey != null)
            {
                e.AcceptedOperation = e.DataView.RequestedOperation == DataPackageOperation.None ? DataPackageOperation.Copy : e.DataView.RequestedOperation;

                var newField = dragModel.GetDropDocument(new Point());
                newField.SetField<NumberController>(KeyStore.HeightFieldKey, 30, true);
                newField.SetField<NumberController>(KeyStore.WidthFieldKey, double.NaN, true);
                newField.SetField<NumberController>(KeyStore.PositionFieldKey, new Point(100,100), true);
                var activeLayout = ViewModel.LayoutDocument;
                if (activeLayout?.DocumentType.Equals(StackLayout.DocumentType) == true) // activeLayout is a stack
                {
                    if (activeLayout.GetField(KeyStore.DataKey, true) == null)
                    {
                        var fields = activeLayout.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null).TypedData.ToArray().ToList();
                        if (!footer)
                            fields.Insert(0, newField);
                        else fields.Add(newField);
                        activeLayout.SetField(KeyStore.DataKey, new ListController<DocumentController>(fields), true);
                    }
                    else
                    {
                        var listCtrl = activeLayout.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);
                        if (!footer)
                            listCtrl.Add(newField, 0);
                        else listCtrl.Add(newField);
                    }
                }
                else
                {
                    var curLayout = activeLayout;
                    if (ViewModel.DocumentController?.GetActiveLayout() != null) // wrap existing activeLayout into a new StackPanel activeLayout
                    {
                        curLayout.SetHorizontalAlignment(HorizontalAlignment.Stretch);
                        curLayout.SetVerticalAlignment(VerticalAlignment.Stretch);
                        curLayout.SetField<NumberController>(KeyStore.WidthFieldKey, double.NaN, true);
                        curLayout.SetField<NumberController>(KeyStore.HeightFieldKey, double.NaN, true);
                    }
                    else  // need to create a stackPanel activeLayout and add the document to it
                    {
                        curLayout = activeLayout.MakeCopy() as DocumentController; // ViewModel's DocumentController is this activeLayout so we can't nest that or we get an infinite recursion
                        curLayout.SetField<NumberController>(KeyStore.WidthFieldKey, double.NaN, true);
                        curLayout.SetField<NumberController>(KeyStore.HeightFieldKey, double.NaN, true);
                        curLayout.SetField(KeyStore.DocumentContextKey, ViewModel.DataDocument, true);
                    }
                    activeLayout = new StackLayout(new DocumentController[] { footer ? curLayout: newField, footer ? newField : curLayout }).Document;
                    activeLayout.SetField<PointController>(KeyStore.PositionFieldKey, ViewModel.Position, true);
                    activeLayout.SetField<NumberController>(KeyStore.WidthFieldKey, ViewModel.ActualWidth, true);
                    activeLayout.SetField<NumberController>(KeyStore.HeightFieldKey, ViewModel.ActualHeight, true);
                    activeLayout.SetField(KeyStore.DocumentContextKey, ViewModel.DataDocument, true);
                    ViewModel.DocumentController.SetField(KeyStore.ActiveLayoutKey, activeLayout, true);
                }
                
                e.Handled = true;
            }
        }

        void This_DragOver(object sender, DragEventArgs e)
        {
            xFooter.Visibility = xHeader.Visibility = Visibility.Visible;
            ViewModel.DecorationState = ViewModel?.Undecorated == false;
            var dragModel = (DragDocumentModel)e.DataView.Properties[nameof(DragDocumentModel)];

            if (dragModel?.DraggedKey != null)
            {
                e.AcceptedOperation = e.DataView.RequestedOperation == DataPackageOperation.None ? DataPackageOperation.Copy : e.DataView.RequestedOperation;

                e.DragUIOverride.IsContentVisible = true;

                e.Handled = true;
            }
        }

        public void This_DragLeave(object sender, DragEventArgs e)
        {
            xFooter.Visibility = xHeader.Visibility = Visibility.Collapsed;
            ViewModel.DecorationState = false;
        }
    }
}