using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Dash.FontIcons;
using Dash.Converters;
using DashShared;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236
namespace Dash
{
    public sealed partial class DocumentView
    {
        private readonly Flyout   _flyout = new Flyout { Placement = FlyoutPlacementMode.Right };
        private DocumentViewModel _oldViewModel = null;
        private Point             _pointerPoint = new Point(0, 0);
        private static readonly SolidColorBrush SingleSelectionBorderColor = new SolidColorBrush(Colors.LightGray);
        private static readonly SolidColorBrush GroupSelectionBorderColor = new SolidColorBrush(Colors.LightBlue);

        public CollectionView ParentCollection => this.GetFirstAncestorOfType<CollectionView>();

        public DocumentViewModel ViewModel
        {
            get { try { return DataContext as DocumentViewModel; } catch (Exception) { return null; } }
            set => DataContext = value;
        }

        public MenuFlyout MenuFlyout => xMenuFlyout;
        public bool PreventManipulation { get; set; }
        // the document that has input focus (logically similar to keyboard focus but different since Images, etc can't be keyboard focused).
        public static DocumentView FocusedDocument { get; set; }
        public static readonly DependencyProperty BindRenderTransformProperty = DependencyProperty.Register(
            "BindRenderTransform", typeof(bool), typeof(DocumentView), new PropertyMetadata(default(bool), BindRenderTransformChanged));

        private static void BindRenderTransformChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DocumentView)d).UpdateRenderTransformBinding();
        }

        public bool BindRenderTransform
        {
            get => (bool)GetValue(BindRenderTransformProperty);
            set => SetValue(BindRenderTransformProperty, value);
        }

        public static readonly DependencyProperty BindVisibilityProperty = DependencyProperty.Register(
            "BindVisibility", typeof(bool), typeof(DocumentView), new PropertyMetadata(default(bool), BindVisibilityChanged));

        private static void BindVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DocumentView)d).UpdateVisibilityBinding();
        }

        public bool BindVisibility
        {
            get => (bool)GetValue(BindVisibilityProperty);
            set => SetValue(BindVisibilityProperty, value);
        }

        private void UpdateRenderTransformBinding()
        {
            var doc = ViewModel?.LayoutDocument;

            var binding = !BindRenderTransform || doc == null
                ? null
                : new FieldMultiBinding<MatrixTransform>(new DocumentFieldReference(doc, KeyStore.PositionFieldKey),
                    new DocumentFieldReference(doc, KeyStore.ScaleAmountFieldKey))
                {
                    Converter = new TransformGroupMultiConverter(),
                    Context = new Context(doc),
                    Mode = BindingMode.OneWay,
                    CanBeNull = true,
                    Tag = "RenderTransform multi binding in DocumentView"
                };
            this.AddFieldBinding(RenderTransformProperty, binding);
            if (ViewModel?.IsDimensionless == true)
            {
                Width = double.NaN;
                Height = double.NaN;
            }
            else
            {
                if (doc != null)
                {
                    CourtesyDocument.BindWidth(this, doc, null);
                    CourtesyDocument.BindHeight(this, doc, null);
                }
            }
        }
        private void UpdateVisibilityBinding()
        {
            var doc = ViewModel?.LayoutDocument;

            var binding = !BindVisibility || doc == null
                ? null
                : new FieldBinding<BoolController>
                {
                    Converter = new InverseBoolToVisibilityConverter(),
                    Document = doc,
                    Key = KeyStore.HiddenKey,
                    Mode = BindingMode.OneWay,
                    Tag = "Visibility binding in DocumentView",
                    FallbackValue = false
                };
            this.AddFieldBinding(VisibilityProperty, binding);

            var binding3 = doc == null ? null : new FieldBinding<BoolController>
            {
                Document = doc,
                Key = KeyStore.AreContentsHitTestVisibleKey,
                Mode = BindingMode.OneWay,
                Tag = "AreContentsHitTestVisible binding in DocumentView",
                FallbackValue = true
            };
            LayoutRoot.AddFieldBinding(IsHitTestVisibleProperty, binding3);
        }
        public void UpdateAlignmentBindings()
        {
            var doc = ViewModel?.LayoutDocument;
            //var isFreeform = !this.IsInVisualTree() || this.GetFirstAncestorOfType<CollectionView>()?.CurrentView is CollectionFreeformView;
            //if (isFreeform)
            //{
            //    HorizontalAlignment = HorizontalAlignment.Left;
            //    VerticalAlignment = VerticalAlignment.Top;
            //    this.AddFieldBinding(FrameworkElement.HorizontalAlignmentProperty, null);
            //    this.AddFieldBinding(FrameworkElement.VerticalAlignmentProperty, null);
            //    ViewModel.LayoutDocument.SetWidth( ViewModel.LayoutDocument.GetDereferencedField<NumberController>(KeyStore.CollectionOpenWidthKey, null)?.Data ??
            //        (!double.IsNaN(ViewModel.LayoutDocument.GetWidth()) ? ViewModel.LayoutDocument.GetWidth() :
            //           ViewModel.LayoutDocument.GetActualSize().Value.X));
            //    ViewModel.LayoutDocument.SetHeight(ViewModel.LayoutDocument.GetDereferencedField<NumberController>(KeyStore.CollectionOpenHeightKey, null)?.Data ??
            //        (!double.IsNaN(ViewModel.LayoutDocument.GetHeight()) ? ViewModel.LayoutDocument.GetHeight() :
            //           ViewModel.LayoutDocument.GetActualSize().Value.Y));
            //}
            //else
            //{
            if (ViewModel?.IsDimensionless == true)
            {
                HorizontalAlignment = HorizontalAlignment.Stretch;
                VerticalAlignment = VerticalAlignment.Stretch;
                this.AddFieldBinding(HorizontalAlignmentProperty, null);
                this.AddFieldBinding(VerticalAlignmentProperty, null);
            }
            else
            {
                CourtesyDocument.BindHorizontalAlignment(this, doc, HorizontalAlignment.Left);
                CourtesyDocument.BindVerticalAlignment(this, doc, VerticalAlignment.Top);
            }
            //}
        }

        // == CONSTRUCTORs ==
        public DocumentView()
        {
            InitializeComponent();
            DataContextChanged += DocumentView_DataContextChanged;

            //Util.InitializeDropShadow(xShadowHost, xDocumentBackground);
            // set bounds
            MinWidth = 25;
            MinHeight = 10;

            void sizeChangedHandler(object sender, SizeChangedEventArgs e)
            {
                ViewModel?.LayoutDocument.SetActualSize(new Point(ActualWidth, ActualHeight));
            }

            //int id = DOCID++;
            //int count = 0;
            Loaded += (sender, e) =>
            {
                //Debug.WriteLine($"Document View {id} loaded {++count}");
                //FadeIn.Begin();

                SizeChanged += sizeChangedHandler;
                PointerWheelChanged += wheelChangedHandler;

                UpdateAlignmentBindings();
                ViewModel?.LayoutDocument.SetActualSize(new Point(ActualWidth, ActualHeight));

                var parentCanvas = this.GetFirstAncestorOfType<ContentPresenter>()?.GetFirstAncestorOfType<Canvas>() ?? new Canvas();
                var maxZ = parentCanvas.Children.Aggregate(int.MinValue, (agg, val) => Math.Max(Canvas.GetZIndex(val), agg));
                Canvas.SetZIndex(this.GetFirstAncestorOfType<ContentPresenter>(), maxZ + 1);
                SetZLayer();
            };

            Unloaded += (sender, args) =>
            {
                //Debug.WriteLine($"Document View {id} unloaded {--count}");
                SizeChanged -= sizeChangedHandler;
                SelectionManager.Deselect(this);
                _oldViewModel?.UnLoad();
            };

            PointerPressed += (sender, e) =>
            {
                return;
                bool right = e.IsRightPressed();
                var parentFreeform = this.GetFirstAncestorOfType<CollectionFreeformBase>();
                var parentParentFreeform = parentFreeform?.GetFirstAncestorOfType<CollectionFreeformBase>();
                ManipulationMode = right ? ManipulationModes.All : ManipulationModes.None;
                MainPage.Instance.Focus(FocusState.Programmatic);

                if (e.Pointer.PointerDeviceType == PointerDeviceType.Touch)
                {
                    if (!SelectionManager.IsSelected(this))
                        SelectionManager.Select(this, false);
                    SelectionManager.TryInitiateDragDrop(this, e, null);
                }

                //e.Handled = true;

                if (parentParentFreeform != null && !this.IsShiftPressed())
                {
                    e.Handled = false;
                }
            };

            MenuFlyout.Opened += (s, e) =>
            {
                if (this.IsShiftPressed())
                    MenuFlyout.Hide();
            };

            ManipulationMode = ManipulationModes.All;
            ManipulationStarted += (s, e) =>
            {
                if (this.IsRightBtnPressed() && ViewModel.AreContentsHitTestVisible)
                {
                    if (SelectionManager.TryInitiateDragDrop(this, null, e))
                        e.Handled = true;
                }
            };
            DragStarting += (s, e) => SelectionManager.DragStarting(this, s, e);
            DropCompleted += (s, e) => SelectionManager.DropCompleted(this, s, e);
            RightTapped += async (s, e) => e.Handled = await TappedHandler(e.Handled, true);
            Tapped += async (s, e) => e.Handled = await TappedHandler(e.Handled, false);
            DoubleTapped += async (sender, args) => await ExhibitBehaviors(KeyStore.DoubleTappedOpsKey);
            RightTapped += async (sender, args) => await ExhibitBehaviors(KeyStore.RightTappedOpsKey);

            ToFront();
            xContentClip.Rect = new Rect(0, 0, LayoutRoot.Width, LayoutRoot.Height);
        }

        private void UpdateBindings()
        {
            UpdateRenderTransformBinding();
            UpdateVisibilityBinding();
            UpdateAlignmentBindings();

            BindBackgroundColor();
            ViewModel?.Load();
        }


        private void DocumentView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs a)
        {
            if (ViewModel != _oldViewModel)
            {
                _oldViewModel?.UnLoad();
                _oldViewModel = ViewModel;
                if (ViewModel != null)
                {
                    UpdateBindings();
                }
            }
        }

        private void ToggleAnnotationVisibility_OnClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is MenuFlyoutItem item)) return;

            var linkDocs = MainPage.Instance.XDocumentDecorations.TagMap.Values;

            bool allVisible = linkDocs.All(l =>
                l.All(doc => doc.GetField<BoolController>(KeyStore.IsAnnotationScrollVisibleKey)?.Data ?? false));

            foreach (var docs in linkDocs)
            {
                foreach (DocumentController l in docs)
                {
                    l.SetField<BoolController>(KeyStore.IsAnnotationScrollVisibleKey, !allVisible, true);
                    l.SetHidden(allVisible);
                }
            }
        }

        //public void ToggleTemplateEditor()
        //{
        //    if (ViewModel.DataDocument.GetField<DocumentController>(KeyStore.TemplateEditorKey) == null)
        //    {
        //        var where = new Point((RenderTransform as MatrixTransform).Matrix.OffsetX + ActualWidth + 60,
        //            (RenderTransform as MatrixTransform).Matrix.OffsetY);
        //        if (_templateEditor != null)
        //        {
        //            Actions.DisplayDocument(ParentCollection.ViewModel, _templateEditor, where);

        //            _templateEditor.SetHidden(!_templateEditor.GetHidden());
        //            ViewModel.DataDocument.SetField(KeyStore.TemplateEditorKey, _templateEditor, true);
        //            return;
        //        }

        //        _templateEditor = new TemplateEditorBox(ViewModel.DocumentController, where, new Size(1000, 540))
        //            .Document;

        //        ViewModel.DataDocument.SetField(KeyStore.TemplateEditorKey, _templateEditor, true);
        //        //creates a doc controller for the image(s)
        //        Actions.DisplayDocument(ParentCollection.ViewModel, _templateEditor, where);
        //    }
        //    else
        //    {
        //        _templateEditor = ViewModel.DataDocument.GetField<DocumentController>(KeyStore.TemplateEditorKey);
        //        ViewModel.DataDocument.SetField(KeyStore.TemplateEditorKey, _templateEditor, true);
        //        _templateEditor.SetHidden(!_templateEditor.GetHidden());
        //    }
        //}

        /// <summary>
        /// Sets the 2D stacking layer ("Z" value) of the document.
        /// If the document is marked as being an adornment, we want to place it below all other documents
        /// </summary>
        void SetZLayer()
        {
            if (ViewModel?.IsAdornmentGroup == true)
            {
                var cp = this.GetFirstAncestorOfType<ContentPresenter>();
                int curZ = 0;
                var parCanvas = cp.GetFirstDescendantOfType<Canvas>();
                if (parCanvas != null)
                {
                    foreach (var c in parCanvas.Children)
                        if (Canvas.GetZIndex(c) < curZ)
                            curZ = Canvas.GetZIndex(c);
                    Canvas.SetZIndex(cp, curZ - 1);
                }
            }
        }

        /// <summary>
        /// Resizes the control based on the user's dragging the ResizeHandles.  The contents will adjust to fit the bounding box
        /// of the control *unless* the Shift button is held in which case the control will be resized but the contents will remain.
        /// Pass true into maintainAspectRatio to preserve the aspect ratio of documents when resizing. Automatically set to true
        /// if the sender is a corner resizer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Resize(FrameworkElement sender, ManipulationDeltaRoutedEventArgs e, bool shiftTop, bool shiftLeft,
            bool maintainAspectRatio)
        {
            e.Handled = true;
            if (PreventManipulation || MainPage.Instance.IsRightBtnPressed())
            {
                return;
            }

            var isImage = ViewModel.DocumentController.DocumentType.Equals(ImageBox.DocumentType) ||
                          (ViewModel.DocumentController.DocumentType.Equals(CollectionBox.DocumentType) && ViewModel.DocumentController.GetFitToParent()) ||
                          ViewModel.DocumentController.DocumentType.Equals(VideoBox.DocumentType);

            double extraOffsetX = 0;
            if (!double.IsNaN(Width))
            {
                extraOffsetX = ActualWidth - Width;
            }


            double extraOffsetY = 0;

            if (!double.IsNaN(Height))
            {
                extraOffsetY = ActualHeight - Height;
            }


            var delta = Util.DeltaTransformFromVisual(e.Delta.Translation, this);
            //problem is that cumulativeDelta.Y is 0
            var cumulativeDelta = Util.DeltaTransformFromVisual(e.Cumulative.Translation, this);

            //if (((this.IsCtrlPressed() || this.IsShiftPressed()) ^ maintainAspectRatio) && delta.Y != 0.0)
            //{
            //    delta.X = 0.0;
            //}
            var oldSize = new Size(ActualWidth - extraOffsetX, ActualHeight - extraOffsetY);

            var oldPos = ViewModel.Position;

            // sets directions/weights depending on which handle was dragged as mathematical manipulations
            var cursorXDirection = shiftLeft ? -1 : 1;
            var cursorYDirection = shiftTop ? -1 : 1;
            var moveXScale = shiftLeft ? 1 : 0;
            var moveYScale = shiftTop ? 1 : 0;

            cumulativeDelta.X *= cursorXDirection;
            cumulativeDelta.Y *= cursorYDirection;

            var w = ActualWidth - extraOffsetX;
            var h = ActualHeight - extraOffsetY;

            Rect dragBounds = Rect.Empty;
            // clamp the drag position to the available Bounds
            if (ViewModel.DragWithinParentBounds)
            {
                var parentViewPresenter =
                    this.GetFirstAncestorOfType<ItemsPresenter >(); // presenter of this document which defines the drag area bounds
                var parentViewTransformationCanvas = parentViewPresenter .GetFirstDescendantOfType<Canvas >(); // bcz: assuming the content being presented has a Canvas ItemsPanelTemplate which may contain a RenderTransformation of the parent (which affects the drag area)
                var rect = parentViewTransformationCanvas.RenderTransform.Inverse.TransformBounds(new Rect(0, 0,
                    parentViewPresenter.ActualWidth, parentViewPresenter.ActualHeight));
                dragBounds = rect;
                if (dragBounds != Rect.Empty)
                {
                    var width = ActualWidth;
                    var height = ActualHeight;
                    var pos = new Point(ViewModel.XPos + width * (1 - moveXScale),
                        ViewModel.YPos + height * (1 - moveYScale));
                    if (!dragBounds.Contains((new Point(pos.X + delta.X, pos.Y + delta.Y))))
                        return;
                    var clamped = Util.Clamp(new Point(pos.X + delta.X, pos.Y + delta.Y), dragBounds);
                    delta = new Point(clamped.X - pos.X, clamped.Y - pos.Y);
                }
            }

            double diffX;
            double diffY;

            var aspect = w / h;
            var moveAspect = cumulativeDelta.X / cumulativeDelta.Y;

            bool useX = cumulativeDelta.X > 0 && cumulativeDelta.Y <= 0;
            if (cumulativeDelta.X <= 0 && cumulativeDelta.Y <= 0)
            {

                useX |= maintainAspectRatio ? moveAspect <= aspect : delta.X != 0;
            }
            else if (cumulativeDelta.X > 0 && cumulativeDelta.Y > 0)
            {
                useX |= maintainAspectRatio ? moveAspect > aspect : delta.X != 0;
            }

            var proportional = (!isImage && maintainAspectRatio)
                ? this.IsShiftPressed()
                : (this.IsShiftPressed() ^ maintainAspectRatio);
            if (useX)
            {
                aspect = 1 / aspect;
                diffX = cursorXDirection * delta.X;
                diffY = proportional
                    ? aspect * diffX
                    : cursorYDirection * delta.Y; // proportional resizing if Shift or Ctrl is presssed
            }
            else
            {
                diffY = cursorYDirection * delta.Y;
                diffX = proportional
                    ? aspect * diffY
                    : cursorXDirection * delta.X;
            }

            var newSize = new Size(Math.Max(w + diffX, MinWidth), Math.Max(h + diffY, MinHeight));
            // set the position of the doc based on how much it resized (if Top and/or Left is being dragged)
            var newPos = new Point(
                ViewModel.XPos - moveXScale * (newSize.Width - oldSize.Width) * ViewModel.Scale.X,
                ViewModel.YPos - moveYScale * (newSize.Height - oldSize.Height) * ViewModel.Scale.Y);

            if (ViewModel.DocumentController.DocumentType.Equals(AudioBox.DocumentType))
            {
                MinWidth = 200;
                newSize.Height = oldSize.Height;
                newPos.Y = ViewModel.YPos;
            }


            // re-clamp the position to keep it in bounds
            if (dragBounds != Rect.Empty)
            {
                if (!dragBounds.Contains(newPos) ||
                    !dragBounds.Contains(new Point(newPos.X + newSize.Width,
                        newPos.Y + DesiredSize.Height)))
                {
                    ViewModel.Position = oldPos;
                    ViewModel.Width = oldSize.Width;
                    ViewModel.Height = oldSize.Height;
                    return;
                }

                var clamp = Util.Clamp(newPos, dragBounds);
                newSize.Width += newPos.X - clamp.X;
                newSize.Height += newPos.Y - clamp.Y;
                newPos = clamp;
                var br = Util.Clamp(new Point(newPos.X + newSize.Width, newPos.Y + newSize.Height), dragBounds);
                newSize = new Size(br.X - newPos.X, br.Y - newPos.Y);
            }

            ViewModel.Position = newPos;
            if (newSize.Width != ViewModel.ActualSize.X)
            {
                ViewModel.Width = newSize.Width;
            }

            if (delta.Y != 0 || this.IsShiftPressed() || isImage)
            {
                if (newSize.Height != ViewModel.ActualSize.Y)
                {
                    ViewModel.Height = newSize.Height;
                }
            }
        }

        // Controls functionality for the Right-click context menu

        #region Menu

        /// <summary>
        /// Brings the element to the front of its containing parent canvas.
        /// </summary>
        public void ToFront()
        {
            if (ParentCollection != null && ViewModel?.IsAdornmentGroup != true)
            {
                ParentCollection.MaxZ += 1;
                Canvas.SetZIndex(this.GetFirstAncestorOfType<ContentPresenter>(), ParentCollection.MaxZ);
            }
        }

        // this action is used to remove template editor in sync with document
        public Action FadeOutBegin;

        /// <summary>
        /// Deletes the document from the view.
        /// </summary>
        /// <param name="addTextBox"></param>
        public void DeleteDocument()
        {
            if (this.GetFirstAncestorOfType<AnnotationOverlayEmbeddings>() != null)
            {
                // bcz: if the document is on an annotation layer, then deleting it would orphan its annotation pin,
                //      but it would still be in the list of pinned annotations.  That means the document would reappear
                //      the next time the container document gets loaded.  We need a cleaner way to handle deleting 
                //      documents which would allow us to delete this document and any references to it, including possibly removing the pin
                ViewModel.DocumentController.SetHidden(true);
            }
            else if (ParentCollection != null)
            {
                SelectionManager.Deselect(this);
                UndoManager.StartBatch(); // bcz: EndBatch happens in FadeOut completed
                FadeOut.Begin();
                FadeOutBegin?.Invoke();
            }
        }

        /// <summary>
        /// Copies the Document.
        /// </summary>
        public void CopyDocument()
        {
            using (UndoManager.GetBatchHandle())
            {
                // will this screw things up?
                Canvas.SetZIndex(this.GetFirstAncestorOfType<ContentPresenter>(), 0);
                var doc = ViewModel.DocumentController.GetCopy(null);
                ParentCollection?.ViewModel.AddDocument(doc);
            }
        }
        /// <summary>
        /// Copies the Document.
        /// </summary>
        public void MakeInstance()
        {
            using (UndoManager.GetBatchHandle())
            {
                // will this screw things up?
                Canvas.SetZIndex(this.GetFirstAncestorOfType<ContentPresenter>(), 0);
                var doc = ViewModel.DocumentController.GetDataInstance(null);
                ParentCollection?.ViewModel.AddDocument(doc);
            }
        }

        /// <summary>
        /// Copes the DocumentView for the document
        /// </summary>
        private void CopyViewDocument()
        {
            // will this screw things up?
            Canvas.SetZIndex(this.GetFirstAncestorOfType<ContentPresenter>(), 0);

            ParentCollection?.ViewModel.AddDocument(ViewModel.DocumentController.GetViewCopy(null));
            //xDelegateStatusCanvas.Visibility = ViewModel.DocumentController.HasDelegatesOrPrototype ? Visibility.Visible : Visibility.Collapsed;  // TODO theoretically the binding should take care of this..
        }

        /// <summary>
        /// Pulls up the linked KeyValuePane of the document.
        /// </summary>
        private void KeyValueViewDocument()
        {
            ParentCollection?.ViewModel.AddDocument(ViewModel.DocumentController.GetKeyValueAlias());
        }

        /// <summary>
        /// Opens in Chrome the context from which the document was made.
        /// </summary>
        public void ShowContext()
        {
            ViewModel.DocumentController.GetDataDocument().RestoreNeighboringContext();
        }
        /// <summary>
        /// Opens in Chrome the context from which the document was made.
        /// </summary>
        public void ShowXaml()
        {
            var where = ViewModel.Position;
            ParentCollection?.ViewModel.AddDocument(
                new DataBox(new DocumentReferenceController(ViewModel.LayoutDocument, KeyStore.XamlKey), where.X, where.Y, 300, 400).Document
            );
        }

        public void GetJson()
        {
            Util.ExportAsJson(ViewModel.DocumentController.EnumFields());
        }

        public event Action<DocumentView> DocumentDeleted;

        private void FadeOut_Completed(object sender, object e)
        {
            ParentCollection?.ViewModel.RemoveDocument(ViewModel.DocumentController);

            DocumentDeleted?.Invoke(this);
            UndoManager.EndBatch();
        }

        #endregion

        #region Activation

        public event Action<DocumentView> DocumentSelected;
        public event Action<DocumentView> DocumentDeselected;

        public void OnSelected()
        {
            DocumentSelected?.Invoke(this);
        }

        public void OnDeselected()
        {
            DocumentDeselected?.Invoke(this);
        }

        #endregion

        /// <summary>
        /// Handles left and right tapped events on DocumentViews
        /// </summary>
        /// <param name="wasHandled">Whether the tapped event was previously handled
        /// this is always false currently so it probably isn't needed</param>
        /// <param name="wasRightTapped"></param>
        /// <returns>Whether the calling tapped event should be handled</returns>
        public async Task<bool> TappedHandler(bool wasHandled, bool wasRightTapped)
        {
            if (!wasHandled && await ExhibitBehaviors(wasRightTapped ? KeyStore.RightTappedOpsKey : KeyStore.LeftTappedOpsKey)) return true;
            
            if (!wasHandled) FocusedDocument = this;

            if (!(FocusManager.GetFocusedElement() is FrameworkElement focused) || !focused.GetAncestorsOfType<DocumentView>().Contains(this))
            {
                Focus(FocusState.Programmatic);
            }
            //if (!Equals(MainPage.Instance.MainDocView)) Focus(FocusState.Programmatic);

            MainPage.Instance.xPresentationView.TryHighlightMatches(this);

            //TODO Have more standard way of selecting groups/getting selection of groups to the toolbar
            ToFront();

            //         if (!this.IsRightBtnPressed() && (ParentCollection == null || ParentCollection.CurrentView is CollectionFreeformBase) && (e == null || !e.Handled))
            if (!wasHandled) // (ParentCollection == null || ParentCollection?.CurrentView is CollectionFreeformBase) && !wasHandled)
            {
                var cfview = ParentCollection?.CurrentView as CollectionFreeformBase;
                if (!MainPage.Instance.IsRightBtnPressed())
                {
                    SelectionManager.Select(this, this.IsShiftPressed());
                }

                if (SelectionManager.GetSelectedDocs().Count > 1)
                {
                    // move focus to container if multiple documents are selected, otherwise allow keyboard focus to remain where it was
                    cfview?.Focus(FocusState.Programmatic);
                }

                return true;
            }

            return false;
        }

        private async Task<bool> ExhibitBehaviors(KeyController behaviorKey)
        {
            var scripts = ViewModel.DocumentController.GetBehaviors(behaviorKey);
            if (scripts != null && scripts.Any())
            {
                using (UndoManager.GetBatchHandle())
                {
                    var args = new List<FieldControllerBase>() {ViewModel.DocumentController};
                    var tasks = new List<Task<(FieldControllerBase, ScriptErrorModel)>>(scripts.Count);
                    foreach (var operatorController in scripts)
                    {
                        var task = ExecutionEnvironment.Run(operatorController, args, new DictionaryScope());
                        if (!task.IsCompleted)
                        {
                            tasks.Add(task);
                        }
                    }

                    if (tasks.Any())
                    {
                        await Task.WhenAll(tasks);
                    }
                }

                return true;
            }

            return false;
        }

        #region UtilityFuncions

        public void HandleShiftEnter()
        {
            var collection = this.GetFirstAncestorOfType<CollectionFreeformBase>();
            var docCanvas = this.GetFirstAncestorOfType<Canvas>();
            if (collection == null)
            {
                return;
            }

            var where = TransformToVisual(docCanvas).TransformPoint(new Point(0, ActualHeight + 1));

            // special case for search operators
            if (ViewModel.DataDocument.DocumentType.Equals(DashConstants.TypeStore.OperatorType))
            {
                if (ViewModel.DataDocument.GetField(KeyStore.OperatorKey) is SearchOperatorController)
                {
                    var operatorDoc = OperationCreationHelper.Operators["Search"].OperationDocumentConstructor();

                    operatorDoc.SetField(SearchOperatorController.InputCollection,
                        new DocumentReferenceController(ViewModel.DataDocument,
                            SearchOperatorController.ResultsKey), true);

                    // TODO connect output to input

                    Actions.DisplayDocument(collection.ViewModel, operatorDoc, where);
                    return;
                }
            }

            using (UndoManager.GetBatchHandle())
            {
                collection.LoadNewActiveTextBox("", where);
            }
        }

        #endregion

        #region Context menu click handlers

        private void MenuFlyoutItemCopy_Click(object sender, RoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                foreach (var doc in SelectionManager.GetSelectedSiblings(this))
                {
                    doc.CopyDocument();
                }
            }
        }

        private void MenuFlyoutItemAlias_Click(object sender, RoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                foreach (var doc in SelectionManager.GetSelectedSiblings(this))
                {
                    doc.CopyViewDocument();
                }
            }
        }

        private void MenuFlyoutItemDelete_Click(object sender, RoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                foreach (var doc in SelectionManager.GetSelectedSiblings(this))
                {
                    doc.DeleteDocument();
                }
            }
        }

        private void MenuFlyoutItemFields_Click(object sender, RoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                foreach (var doc in SelectionManager.GetSelectedSiblings(this))
                {
                    doc.KeyValueViewDocument();
                }
            }
        }


        private void MenuFlyoutItemToggleAsButton_Click(object sender, RoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                foreach (var docView in SelectionManager.GetSelectedSiblings(this))
                {
                    docView.ViewModel.DocumentController.ToggleButton();
                    SetZLayer();
                }
            }
        }
        private void MenuFlyoutItemToggleAsAdornment_Click(object sender, RoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                foreach (var docView in SelectionManager.GetSelectedSiblings(this))
                {
                    docView.ViewModel.IsAdornmentGroup = !docView.ViewModel.IsAdornmentGroup;
                    SetZLayer();
                }
            }
        }

        public void MenuFlyoutItemFitToParent_Click(object sender, RoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                var collectionView = this.GetFirstDescendantOfType<CollectionView>();
                if (collectionView != null)
                {
                    collectionView.ViewModel.ContainerDocument.SetFitToParent(!collectionView.ViewModel
                        .ContainerDocument.GetFitToParent());
                    if (collectionView.ViewModel.ContainerDocument.GetFitToParent())
                        collectionView.FitContents();
                }
            }
        }


        private void MenuFlyoutItemCopyPath_Click(object sender, RoutedEventArgs e)
        {
            var path = DocumentTree.GetPathsToDocuments(ViewModel.DocumentController).FirstOrDefault();
            if (path == null)
            {
                return;
            }
            DataPackage dp = new DataPackage();
            dp.SetText(DocumentTree.GetEscapedPath(path));
            Clipboard.SetContent(dp);
        }

        private void MenuFlyoutItemGetScript_Click(object o, RoutedEventArgs routedEventArgs)
        {
            var path = DocumentTree.GetPathsToDocuments(ViewModel.DocumentController).FirstOrDefault();
            if (path == null)
            {
                return;
            }

            var pathString = DocumentTree.GetEscapedPath(path);
            var pathScript = $"d(\"{pathString.Replace(@"\", @"\\").Replace("\"", "\\\"")}\")";
            DataPackage dp = new DataPackage();
            dp.SetText(pathScript);
            Clipboard.SetContent(dp);
        }

        private void MenuFlyoutItemContext_Click(object sender, RoutedEventArgs e)
        {
            ShowContext();
        }

        private async void MenuFlyoutItemScreenCap_Click(object sender, RoutedEventArgs e)
        {
            await Util.ExportAsImage(LayoutRoot);
        }

        private void MenuFlyoutItemOpen_OnClick(object sender, RoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                SplitFrame.OpenInActiveFrame(ViewModel.DocumentController);
            }
        }

        private void MenuFlyoutItemCopyHistory_Click(object sender, RoutedEventArgs e)
        {
            var data = new DataPackage() { };
            data.SetText(string.Join("\n",
                (ViewModel.DocumentController.GetAllContexts() ?? new List<DocumentContext>()).Select(
                    c => c.Title + "  :  " + c.Url)));
            Clipboard.SetContent(data);
        }

        private async void MenuFlyoutLaunch_Click(object sender, RoutedEventArgs e)
        {
            var text = ViewModel.DocumentController.GetField(KeyStore.SystemUriKey) as TextController;
            if (text != null)
                await Launcher.QueryAppUriSupportAsync(new Uri(text.Data));

            //var storageFile = item as StorageFile;
            //var fields = new Dictionary<KeyController, FieldControllerBase>
            //{
            //    [KeyStore.SystemUriKey] = new TextController(storageFile.Path + storageFile.Name)
            //};
            //var doc = new DocumentController(fields, DashConstants.TypeStore.FileLinkDocument);
        }

        #endregion

        public async void This_Drop(object sender, DragEventArgs e)
        {
            if (ViewModel.IsAdornmentGroup || !ViewModel.AreContentsHitTestVisible)
                return;

            var dragModel = e.DataView.GetDragModel();
            if (dragModel != null)
            {
                if (!(dragModel is DragDocumentModel dm) || dm.DraggedDocumentViews == null || !dm.DraggingLinkButton) return;
                e.Handled = true;

                if (MainPage.Instance.IsAltPressed())
                {
                    var curLayout = ViewModel.LayoutDocument;
                    var draggedLayout = dm.DraggedDocuments.First().GetDataInstance(ViewModel.Position);
                    draggedLayout.SetField(KeyStore.DocumentContextKey, ViewModel.DataDocument, true);
                    if (double.IsNaN(curLayout.GetWidth()) || double.IsNaN(curLayout.GetHeight()))
                    {
                        curLayout.SetWidth(dm.DraggedDocuments.First().GetActualSize().Value.X);
                        curLayout.SetHeight(dm.DraggedDocuments.First().GetActualSize().Value.Y);
                    }
                    curLayout.SetField(KeyStore.DataKey, draggedLayout.GetField(KeyStore.DataKey), true);
                    curLayout.SetField(KeyStore.PrototypeKey, draggedLayout.GetField(KeyStore.PrototypeKey), true);
                    curLayout.SetField(KeyStore.LayoutPrototypeKey, draggedLayout, true);

                    curLayout.SetField(KeyStore.CollectionFitToParentKey, draggedLayout.GetDereferencedField(KeyStore.CollectionFitToParentKey, null), true);
                    curLayout.DocumentType = draggedLayout.DocumentType;
                    UpdateBindings();
                    return;
                }

                var dragDocs = dm.DraggedDocuments;
                for (var index = 0; index < dragDocs.Count; index++)
                {
                    var dragDoc = dragDocs[index];
                    if (KeyStore.RegionCreator.TryGetValue(dragDoc.DocumentType, out var creatorFunc) && creatorFunc != null)
                    {
                        dragDoc = await creatorFunc(dm.DraggedDocumentViews[index]);
                    }
                    //add link description to doc and if it isn't empty, have flag to show as popup when links followed
                    var dropDoc = ViewModel.DocumentController;
                    if (KeyStore.RegionCreator[dropDoc.DocumentType] != null)
                    {
                        dropDoc = await KeyStore.RegionCreator[dropDoc.DocumentType](this, this.IsShiftPressed() || this.IsCtrlPressed() ? e.GetPosition(this) : (Point?) null);
                    }

                    var linkDoc = dragDoc.Link(dropDoc, LinkBehavior.Annotate, dm.DraggedLinkType);
                    MainPage.Instance.AddFloatingDoc(linkDoc);
                    //TODO: ADD SUPPORT FOR MAINTAINING COLOR FOR LINK BUBBLES
                    dropDoc?.SetField(KeyStore.IsAnnotationScrollVisibleKey, new BoolController(true), true);
                }
                e.AcceptedOperation = e.DataView.RequestedOperation == DataPackageOperation.None
                    ? DataPackageOperation.Link
                    : e.DataView.RequestedOperation;
            }
        }

        public bool IsTopLevel()
        {
            return this.GetFirstAncestorOfType<SplitFrame>()?.DataContext == DataContext;
        }
        private void MenuFlyoutItemCaption_Click(object sender, RoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                if (ViewModel.LayoutDocument != null)
                {
                    ViewModel.LayoutDocument.SetField<TextController>(KeyStore.XamlKey,
                        @"<Grid  xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                                 xmlns:dash=""using:Dash""
                                 xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006"" >
                            <Grid.RowDefinitions>
                                <RowDefinition Height=""Auto"" ></RowDefinition>
                                <RowDefinition Height=""*"" ></RowDefinition>
                            </Grid.RowDefinitions>
                                <Border Grid.Row=""0"" Background =""CadetBlue"" >
                                    <dash:EditableImage x:Name=""xImageFieldData"" Foreground =""White"" HorizontalAlignment =""Stretch"" Grid.Row=""1"" VerticalAlignment =""Top"" />
                                </Border>
                                <Border Grid.Row=""1"" Background =""CadetBlue"" MinHeight =""30"" >
                                    <dash:RichEditView x:Name= ""xRichTextFieldCaption"" TextWrapping= ""Wrap"" Foreground= ""White"" HorizontalAlignment= ""Stretch"" Grid.Row= ""1"" VerticalAlignment= ""Top"" />
                                </Border>
                        </Grid>",
                        true);
                }
            }
        }
        private void MenuFlyoutItemPin_Click(object sender, RoutedEventArgs e)
        {
            if (IsTopLevel())
            {
                return;
            }

            using (UndoManager.GetBatchHandle())
            {
                MainPage.Instance.PinToPresentation(ViewModel.DocumentController);
                if (ViewModel.LayoutDocument == null)
                {
                    Debug.WriteLine("uh oh");
                }
            }
        }

        private void XAnnotateEllipseBorder_OnTapped_(object sender, TappedRoutedEventArgs e)
        {
            new AnnotationManager(this).FollowRegion(this, ViewModel.DocumentController, this.GetAncestorsOfType<ILinkHandler>(), e.GetPosition(this));
        }

        private void AdjustEllipseSize(Ellipse ellipse, double length)
        {
            ellipse.Width = length;
            ellipse.Height = length;
        }

        //private void MenuFlyoutItemApplyTemplate_Click(object sender, RoutedEventArgs e)
        //{
        //    var applier = new TemplateApplier(ViewModel.LayoutDocument);
        //    _flyout.Content = applier;
        //    if (_flyout.IsInVisualTree())
        //    {
        //        _flyout.Hide();
        //    }
        //    else
        //    {
        //        _flyout.ShowAt(this);
        //    }
        //}

        //binds the background color of the document to the ViewModel's LayoutDocument's BackgroundColorKey
        void BindBackgroundColor()
        {
            if (ViewModel?.LayoutDocument != null)
            {
                var backgroundBinding = new FieldBinding<TextController>()
                {
                    Key = KeyStore.BackgroundColorKey,
                    Document = ViewModel.LayoutDocument,
                    Converter = new StringToBrushConverter(),
                    Mode = BindingMode.TwoWay,
                    Context = new Context(),
                    FallbackValue = new SolidColorBrush(Colors.Transparent)
                };
                xDocumentBackground.AddFieldBinding(Shape.FillProperty, backgroundBinding);
            }
        }

        private async void xMenuFlyout_Opening(object sender, object e)
        {
            xMenuFlyout.Items.Clear();

            xMenuFlyout.Items.Add(new MenuFlyoutItem()
            {
                Text = "Open",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.FolderOpen }
            });
            (xMenuFlyout.Items.Last() as MenuFlyoutItem).Click += MenuFlyoutItemOpen_OnClick;
            xMenuFlyout.Items.Add(new MenuFlyoutItem()
            {
                Text = MainPage.Instance.MainSplitter.GetFrameWithDoc(ViewModel.DocumentController, true) == null ? "Open In Collapsed Frame" : "Close Frame",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.Folder }
            });
            (xMenuFlyout.Items.Last() as MenuFlyoutItem).Click += MenuFlyoutItemOpenCollapsed_OnClick;
            xMenuFlyout.Items.Add(new MenuFlyoutItem()
            {
                Text = "Delete",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.Trash }
            });
            (xMenuFlyout.Items.Last() as MenuFlyoutItem).Click += MenuFlyoutItemDelete_Click;
            xMenuFlyout.Items.Add(new MenuFlyoutItem()
            {
                Text = "Hide",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.Close }
            });
            (xMenuFlyout.Items.Last() as MenuFlyoutItem).Click += MenuFlyoutItemHide_Click;

            xMenuFlyout.Items.Add(new MenuFlyoutSeparator());

            xMenuFlyout.Items.Add(new MenuFlyoutItem()
            {
                Text = "Duplicate",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.Copy }
            });
            (xMenuFlyout.Items.Last() as MenuFlyoutItem).Click += MenuFlyoutItemCopy_Click;
            xMenuFlyout.Items.Add(new MenuFlyoutItem()
            {
                Text = "Alias",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.Link }
            });
            (xMenuFlyout.Items.Last() as MenuFlyoutItem).Click += MenuFlyoutItemAlias_Click;
            xMenuFlyout.Items.Add(new MenuFlyoutItem()
            {
                Text = "Fields",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.Database }
            });
            (xMenuFlyout.Items.Last() as MenuFlyoutItem).Click += MenuFlyoutItemFields_Click;
            xMenuFlyout.Items.Add(new MenuFlyoutItem()
            {
                Text = "Cut",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.Cut }
            });
            (xMenuFlyout.Items.Last() as MenuFlyoutItem).Click += MenuFlyoutItemCut_Click;
            xMenuFlyout.Items.Add(new MenuFlyoutItem()
            {
                Text = "Copy",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.Copy }
            });
            (xMenuFlyout.Items.Last() as MenuFlyoutItem).Click += MenuFlyoutItemClipboardCopy_Click;

            xMenuFlyout.Items.Add(new MenuFlyoutItem()
            {
                Text = "Copy Path",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.CodeFork }
            });
            (xMenuFlyout.Items.Last() as MenuFlyoutItem).Click += MenuFlyoutItemCopyPath_Click;
            xMenuFlyout.Items.Add(new MenuFlyoutItem()
            {
                Text = "Get Script Representation",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.Code }
            });
            (xMenuFlyout.Items.Last() as MenuFlyoutItem).Click += MenuFlyoutItemGetScript_Click;

            xMenuFlyout.Items.Add(new MenuFlyoutSeparator());

            xMenuFlyout.Items.Add(new MenuFlyoutItem()
            {
                Text = "Add Caption",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.FileText }
            });
            (xMenuFlyout.Items.Last() as MenuFlyoutItem).Click += MenuFlyoutItemCaption_Click;
            xMenuFlyout.Items.Add(new MenuFlyoutItem()
            {
                Text = "Add to Presentation",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.MapPin }
            });
            (xMenuFlyout.Items.Last() as MenuFlyoutItem).Click += MenuFlyoutItemPin_Click;
            xMenuFlyout.Items.Add(new MenuFlyoutItem()
            {
                Text = ViewModel.LayoutDocument.GetIsAdornment() ? "Remove Adornment Behavior" : "Add Adornment Behavior",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.Lock }
            });
            (xMenuFlyout.Items.Last() as MenuFlyoutItem).Click += MenuFlyoutItemToggleAsAdornment_Click;
            xMenuFlyout.Items.Add(new MenuFlyoutItem()
            {
                Text = AnyBehaviors() ? "Manage Behaviors" : "Add Behaviors",
                Icon = new FontIcons.FontAwesome { Icon = AnyBehaviors() ? FontAwesomeIcon.AddressBook : FontAwesomeIcon.Plus }
            });
            (xMenuFlyout.Items.Last() as MenuFlyoutItem).Click += async (o, args) =>
            {
                await UIFunctions.ManageBehaviors(ViewModel.DocumentController);
            };
            //Add the Layout Template Popup
           xMenuFlyout.Items.Add(new MenuFlyoutItem()
            {
                Text = "Document Layouts",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.Sitemap }
            });
            (xMenuFlyout.Items.Last() as MenuFlyoutItem).Click += MenuFlyoutItemLayoutTemplates_Click;
            if (ViewModel.DocumentController.DocumentType.Equals(RichTextBox.DocumentType))
            {
                xMenuFlyout.Items.Add(new MenuFlyoutItem()
                {
                    Text = "Add to Action Menu",
                    Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.PlusCircle }
                });
                (xMenuFlyout.Items.Last() as MenuFlyoutItem).Click += MenuFlyoutItemAddToActionMenu_Click;
            }
            if (ViewModel.DocumentController.DocumentType.Equals(RichTextBox.DocumentType))
            {
                xMenuFlyout.Items.Add(new MenuFlyoutItem()
                {
                    Text = "Make Default Textbox",
                    Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.PlusCircle }
                });
                (xMenuFlyout.Items.Last() as MenuFlyoutItem).Click += MenuFlyoutItemMakeDefaultTextBox_Click;
            }

            if (ViewModel.DocumentController.DocumentType.Equals(RichTextBox.DocumentType))
            {
                xMenuFlyout.Items.Add(new MenuFlyoutItem()
                {
                    Text = "Edit Xaml",
                    Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.Xing }
                });
                (xMenuFlyout.Items.Last() as MenuFlyoutItem).Click += MenuFlyoutEditXaml_Click;
            }

            if (ViewModel.Content is CollectionView collectionView)
            {
                collectionView.SetupContextMenu(xMenuFlyout);
            }
            else if (ViewModel.Content.GetFirstDescendantOfType<CollectionView>() is CollectionView cView)
            {
                cView.SetupContextMenu(xMenuFlyout);
            }
            if ((ViewModel.Content is ContentPresenter cpresent) &&
                (cpresent.Content is CollectionView collectionView2))
            {
                collectionView2.SetupContextMenu(xMenuFlyout);
            }
        }

        private bool AnyBehaviors() => ViewModel.LayoutDocument.GetDataDocument().GetField<ListController<DocumentController>>(KeyStore.DocumentBehaviorsKey)?.Any() ?? false;

        private void Cut(bool delete)
        {
            var selected = SelectionManager.GetSelectedDocs();
            if (selected.Any())
            {
                var dataPackage = new DataPackage();
                dataPackage.SetClipboardData(new CopyPasteModel(selected.Select(view => view.ViewModel.DocumentController).ToList(), !delete));
                if (delete)
                {
                    selected.ForEach(dv => dv.DeleteDocument());
                }
                Clipboard.SetContent(dataPackage);
            }
            else
            {
                var dataPackage = new DataPackage();
                dataPackage.SetClipboardData(new CopyPasteModel(new List<DocumentController> { ViewModel.DocumentController }, !delete));
                if (delete)
                {
                    DeleteDocument();
                }

                Clipboard.SetContent(dataPackage);
            }

        }

        private void MenuFlyoutItemClipboardCopy_Click(object sender, RoutedEventArgs e) => Cut(false);

        private void MenuFlyoutItemCut_Click(object sender, RoutedEventArgs e) => Cut(true);

        private void MenuFlyoutItemOpenCollapsed_OnClick(object sender, RoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                var frame = MainPage.Instance.MainSplitter.GetFrameWithDoc(ViewModel.DocumentController, true);
                if (frame != null)
                {
                    frame.Delete();
                }
                else
                {
                    SplitFrame.OpenInInactiveFrame(ViewModel.DocumentController);
                }
            }
        }

        private async void MenuFlyoutItemLayoutTemplates_Click(object sender, RoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                var docs = SelectionManager.GetSelectedSiblings(this).Select(doc => doc.ViewModel.DocumentController);
                var template = await MainPage.Instance.GetLayoutTemplate(docs);

                if (template == null)
                    return;

                foreach (var doc in docs)
                {
                    doc.SetField<TextController>(KeyStore.XamlKey, template, true);
                }
            }
        }

        private void MenuFlyoutEditXaml_Click(object sender, RoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                foreach (var doc in SelectionManager.GetSelectedSiblings(this))
                {
                    doc.ShowXaml();
                }
            }
        }
        private async void MenuFlyoutItemMakeDefaultTextBox_Click(object sender, RoutedEventArgs e)
        {
            this.GetFirstAncestorOfType<CollectionView>()?.ViewModel.ContainerDocument.GetDataDocument().SetField<TextController>(
                KeyStore.DefaultTextboxXamlKey,
                ViewModel.LayoutDocument.GetDereferencedField<TextController>(KeyStore.XamlKey, null)?.Data, true);
        }
        private async void MenuFlyoutItemAddToActionMenu_Click(object sender, RoutedEventArgs e)
        {
            (string name, string desc) = await MainPage.Instance.PromptNewTemplate();
            if (!(name == string.Empty && desc == string.Empty))
            {
                var copy = ViewModel.DocumentController.GetCopy();
                copy.SetTitle(name);
                copy.SetField<TextController>(KeyStore.CaptionKey, desc, true);
                var templates = MainPage.Instance.MainDocument.GetDataDocument()
                    .GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.TemplateListKey);
                templates.Add(copy);
                MainPage.Instance.MainDocument.GetDataDocument().SetField(KeyStore.TemplateListKey, templates, true);
            }
        }

        private void MenuFlyoutItemHide_Click(object sender, RoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                ViewModel.LayoutDocument.SetHidden(true);
            }
        }

        ~DocumentView()
        {
            //Debug.Write("dispose DocumentView");
        }

        public bool AreContentsHitTestVisible
        {
            get => ViewModel.AreContentsHitTestVisible;
            set => ViewModel.AreContentsHitTestVisible = !ViewModel.DocumentController.GetAreContentsHitTestVisible();
            //xBackgroundPin.Text = "" + (char)(!ViewModel.DocumentController.GetAreContentsHitTestVisible() ? 0xE840 : 0xE77A);

        }

        /// <summary>
        /// Pans content of a document view
        /// </summary>
        private void PanContent(double deltaX, double deltaY)
        {
            if (!(xContentTransform.Matrix.OffsetX + deltaX > 0 && xContentTransform.Matrix.OffsetY + deltaY > 0))
            {
                bool moveXAllowed = xContentTransform.Matrix.OffsetX + deltaX <= 0 &&
                    xContentTransform.Matrix.M11 * ViewModel.ActualSize.X + xContentTransform.Matrix.OffsetX + deltaX + 0.2 >=
                    ViewModel.ActualSize.X;
                bool moveYAllowed =
                    xContentTransform.Matrix.OffsetY + deltaY <= 0 && xContentTransform.Matrix.M22 * ViewModel.ActualSize.Y + xContentTransform.Matrix.OffsetY + deltaY + 0.2 >=
                    ViewModel.ActualSize.Y;

                var tgroup = new TransformGroup();
                tgroup.Children.Add(xContentTransform);
                tgroup.Children.Add(new TranslateTransform() { X = moveXAllowed ? deltaX : 0, Y = moveYAllowed ? deltaY : 0 });
                xContentTransform.Matrix = tgroup.Value;
            }
        }

        //checks if we should be panning content
        private void LayoutRoot_OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            //if ctrl is pressed and either or both of left/right btns, we should pan content
            if (this.IsCtrlPressed() && this.IsLeftBtnPressed())
            {
                var curPt = e.GetCurrentPoint(LayoutRoot).Position;
                PanContent(-_pointerPoint.X + curPt.X, -_pointerPoint.Y + curPt.Y);
            }

            _pointerPoint = e.GetCurrentPoint(LayoutRoot).Position;
            e.Handled = true;
        }

        private void LayoutRoot_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            xContentClip.Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height);
        }

        /// <summary>
        /// Zooms content of docView, with a central focus on the cursor location
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void wheelChangedHandler(object sender, PointerRoutedEventArgs e)
        {
            //if control is pressed, zoom content of document
            if (this.IsCtrlPressed())
            {
                //set render scale transform of content to zoom based on wheel changed value
                double wheelValue = e.GetCurrentPoint(null).Properties.MouseWheelDelta;
                double deltaScale = 1 + wheelValue / 500;

                //ensures zoom level can't be less than 1
                if (xContentTransform.Matrix.M11 * deltaScale <= 1) deltaScale = 1 / xContentTransform.Matrix.M11;

                var scale = new ScaleTransform();
                scale.ScaleX = deltaScale;
                scale.ScaleY = deltaScale;

                //set center X to mouse position
                scale.CenterX = e.GetCurrentPoint(LayoutRoot).Position.X;
                scale.CenterY = e.GetCurrentPoint(LayoutRoot).Position.Y;

                var tgroup = new TransformGroup();
                tgroup.Children.Add(xContentTransform);
                tgroup.Children.Add(scale);
                xContentTransform.Matrix = tgroup.Value;

                //use transform bounds to check if content has gotten out of bounds and if so, pan to compensate
                var tb = xContentTransform.TransformBounds(new Rect(0, 0, ViewModel.ActualSize.X, ViewModel.ActualSize.Y));
                if (tb.X > 0) PanContent(0 - tb.X, 0);
                if (tb.Y > 0) PanContent(0, 0 - tb.Y);
                if (tb.X + tb.Width < ViewModel.ActualSize.X) PanContent(ViewModel.ActualSize.X - (tb.X + tb.Width), 0);
                if (tb.Y + tb.Height < ViewModel.ActualSize.Y) PanContent(0, ViewModel.ActualSize.Y - (tb.Y + tb.Height));

                e.Handled = true;
            }
        }

        //this won't work

        private void XContent_OnHolding(object sender, HoldingRoutedEventArgs e)
        {
            xMenuFlyout_Opening(sender, e);
        }
    }
}
