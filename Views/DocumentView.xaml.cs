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
        public static readonly DependencyProperty BindRenderTransformProperty = DependencyProperty.Register(
            "BindRenderTransform", typeof(bool), typeof(DocumentView), new PropertyMetadata(default(bool), BindRenderTransformChanged));
        public event Action<DocumentView> DocumentDeleted;
        private bool              _anyBehaviors => ViewModel.LayoutDocument.GetDataDocument().GetField<ListController<DocumentController>>(KeyStore.DocumentBehaviorsKey)?.Any() ?? false;
        private readonly Flyout   _flyout       = new Flyout { Placement = FlyoutPlacementMode.Right };
        private DocumentViewModel _oldViewModel = null;
        private bool              _doubleTapped = false;
        private Point             _down         = new Point();
        private Point             _pointerPoint = new Point(0, 0);
        private static readonly SolidColorBrush SingleSelectionBorderColor = new SolidColorBrush(Colors.LightGray);
        private static readonly SolidColorBrush GroupSelectionBorderColor = new SolidColorBrush(Colors.LightBlue);

        public CollectionView    ParentCollection => this.GetFirstAncestorOfType<CollectionView>();
        public DocumentViewModel ViewModel 
        {
            get { try { return DataContext as DocumentViewModel; } catch (Exception) { return null; } }
            set => DataContext = value;
        }
        public bool              IsSelected => SelectionManager.SelectedDocViewModels.Contains(ViewModel);
        public bool              AreContentsActive => SelectionManager.SelectedDocViews.Any((sel) => sel == this || sel.GetAncestors().Contains(this)) || MainPage.Instance.IsTopLevel(ViewModel);
        public Action            FadeOutBegin;

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
            
            Loaded += (sender, e) =>
            {
                //Debug.WriteLine($"Document View {id} loaded {++count}");
                SizeChanged += sizeChangedHandler;
                PointerWheelChanged += wheelChangedHandler;

                UpdateAlignmentBindings();
                ViewModel?.LayoutDocument.SetActualSize(new Point(ActualWidth, ActualHeight));

                var parentCanvas = this.GetFirstAncestorOfType<ContentPresenter>()?.GetFirstAncestorOfType<Canvas>() ?? new Canvas();
                var maxZ = parentCanvas.Children.Aggregate(int.MinValue, (agg, val) => Math.Max(Canvas.GetZIndex(val), agg));
                Canvas.SetZIndex(this.GetFirstAncestorOfType<ContentPresenter>(), maxZ + 1);
                SetZLayer();
                SetUpToolTips();
            };

            Unloaded += (sender, args) =>
            {
                //Debug.WriteLine($"Document View {id} unloaded {--count}");
                SizeChanged -= sizeChangedHandler;
                SelectionManager.Deselect(this);
            };

            xMenuFlyout.Opened += (s, e) =>
            {
                if (this.IsShiftPressed())
                    xMenuFlyout.Hide();
            };
            
            DragStarting  += (s, e) => SelectionManager.DragStarting(this, s, e);
            DropCompleted += (s, e) => SelectionManager.DropCompleted(this, s, e);
            RightTapped   += (s, e) => { e.Handled = true; TappedHandler(true); };
            Tapped        += (s, e) => { e.Handled = true; TappedHandler(false); };
            DoubleTapped  += (s, e) => ExhibitBehaviors(KeyStore.DoubleTappedOpsKey);
            RightTapped   += (s, e) => ExhibitBehaviors(KeyStore.RightTappedOpsKey);
            PointerPressed += (s, e) => this_PointerPressed(s, e);

            ToFront();
            xContentClip.Rect = new Rect(0, 0, LayoutRoot.Width, LayoutRoot.Height);
        }
        ~DocumentView()
        {
            //Debug.Write("dispose DocumentView");
        }

        public bool BindRenderTransform
        {
            get => (bool)GetValue(BindRenderTransformProperty);
            set => SetValue(BindRenderTransformProperty, value);
        }
        /// <summary>
        /// Resizes the control based on the user's dragging the ResizeHandles.  The contents will adjust to fit the bounding box
        /// of the control *unless* the Shift button is held in which case the control will be resized but the contents will remain.
        /// Pass true into maintainAspectRatio to preserve the aspect ratio of documents when resizing. Automatically set to true
        /// if the sender is a corner resizer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Resize(ManipulationDeltaRoutedEventArgs e, bool shiftTop, bool shiftLeft, bool maintainAspectRatio)
        {
            if (!MainPage.Instance.IsRightBtnPressed())
            {
                ViewModel.Resize(Util.DeltaTransformFromVisual(e.Delta.Translation,      this), 
                                 Util.DeltaTransformFromVisual(e.Cumulative.Translation, this), 
                                 this.IsShiftPressed(), shiftTop, shiftLeft, maintainAspectRatio);
                e.Handled = true;
            }
        }
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
        public void Cut(bool delete)
        {
            var dataPackage      = new DataPackage();
            var cutDocViewModels = SelectionManager.SelectedDocViewModels.Any() ?
                                       SelectionManager.SelectedDocViewModels : new DocumentViewModel[] { ViewModel };
            dataPackage.SetClipboardData(new CopyPasteModel(cutDocViewModels.Select(dvm => dvm.DocumentController).ToList(), !delete));
            if (delete) {
                using (UndoManager.GetBatchHandle())
                {
                    cutDocViewModels.ToList().ForEach(dvm => dvm.RequestDelete());
                }
            }
            Clipboard.SetContent(dataPackage);
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
                ParentCollection?.ViewModel.AddDocument(ViewModel.DocumentController.GetDataInstance(null));
            }
        }
        /// <summary>
        /// Copes the DocumentView for the document
        /// </summary>
        public void CopyViewDocument()
        {
            // will this screw things up?
            Canvas.SetZIndex(this.GetFirstAncestorOfType<ContentPresenter>(), 0);

            ParentCollection?.ViewModel.AddDocument(ViewModel.DocumentController.GetViewCopy(null));
        }
        public async void MakeDocumentLink(Point where, DragDocumentModel dm)
        {
            DocumentController lastLinkDoc = null;
            for (var index = 0; index < dm.DraggedDocuments.Count; index++)
            {
                var dragDoc = dm.DraggedDocuments[index];
                if (KeyStore.RegionCreator.TryGetValue(dragDoc.DocumentType, out var creatorFunc) && creatorFunc != null)
                {
                    dragDoc = await creatorFunc(dm.DraggedDocumentViews[index]);
                }
                //add link description to doc and if it isn't empty, have flag to show as popup when links followed
                var dropDoc = ViewModel.DocumentController;
                if (KeyStore.RegionCreator[dropDoc.DocumentType] != null)
                {
                    dropDoc = await KeyStore.RegionCreator[dropDoc.DocumentType](this, this.IsShiftPressed() || this.IsCtrlPressed() ? where : (Point?)null);
                }

                lastLinkDoc = dragDoc.Link(dropDoc, LinkBehavior.Annotate, dm.DraggedLinkType);

                //TODO: ADD SUPPORT FOR MAINTAINING COLOR FOR LINK BUBBLES
                dropDoc?.SetField(KeyStore.IsAnnotationScrollVisibleKey, new BoolController(true), true);
            }
            MainPage.Instance.XDocumentDecorations.SetPositionAndSize(true);
            MainPage.Instance.XDocumentDecorations.OpenNewLinkMenu(dm.DraggedLinkType, lastLinkDoc);
        }
        /// <summary>
        /// Opens in Chrome the context from which the document was made.
        /// </summary>
        public void ShowContext()
        {
            ViewModel.DocumentController.GetDataDocument().RestoreNeighboringContext();
        }
        public void ShowContextMenu(Point where)
        {
            xMenuFlyout.ShowAt(null, where);
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
        
        //binds the background color of the document to the ViewModel's LayoutDocument's BackgroundColorKey
        private void UpdateBackgroundColorBinding()
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
        private void UpdateRenderTransformBinding()
        {
            var doc = ViewModel?.LayoutDocument;

            var binding = !BindRenderTransform || doc == null
                ? null
                : new FieldMultiBinding<MatrixTransform>(new DocumentFieldReference(doc, KeyStore.PositionFieldKey))
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
            else if (doc != null)
            {
                CourtesyDocument.BindWidth(this, doc, null);
                CourtesyDocument.BindHeight(this, doc, null);
            }
        }
        private void UpdateVisibilityBinding()
        {
            var doc = ViewModel?.LayoutDocument;

            var binding = doc == null
                ? null
                : new FieldBinding<BoolController>
                {
                    Converter = new InverseBoolToVisibilityConverter(),
                    Document = doc,
                    Key = KeyStore.HiddenKey,
                    Mode = BindingMode.TwoWay,
                    Tag = "Visibility binding in DocumentView",
                    FallbackValue = false
                };
            this.AddFieldBinding(VisibilityProperty, binding);

            var binding3 = doc == null ? null : new FieldBinding<BoolController>
            {
                Document = doc,
                Key = KeyStore.AreContentsHitTestVisibleKey,
                Mode = BindingMode.TwoWay,
                Tag = "AreContentsHitTestVisible binding in DocumentView",
                FallbackValue = true
            };
            LayoutRoot.AddFieldBinding(IsHitTestVisibleProperty, binding3);
        }
        private void UpdateAlignmentBindings()
        {
            var doc = ViewModel?.LayoutDocument;

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
        }
        private void UpdateBindings()
        {
            UpdateRenderTransformBinding();
            UpdateVisibilityBinding();
            UpdateAlignmentBindings();
            UpdateBackgroundColorBinding();
        }
        private void SetUpToolTips()
        {
            var text = ViewModel?.DocumentController?.GetField(KeyStore.ToolbarButtonNameKey);
            if (!(text is TextController t)) return;

            var label = new ToolTip()
            {
                Content = t.Data,
                Placement = PlacementMode.Bottom,
                VerticalOffset = 10
            };
            ToolTipService.SetToolTip(xMasterStack, label);
        }
        /// <summary>
        /// Brings the element to the front of its containing parent canvas.
        /// </summary>
        private void ToFront()
        {
            if (ParentCollection != null && ViewModel?.DocumentController.GetIsAdornment() != true)
            {
                ParentCollection.MaxZ += 1;
                Canvas.SetZIndex(this.GetFirstAncestorOfType<ContentPresenter>(), ParentCollection.MaxZ);
            }
        }

        private void DocumentView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs a)
        {
            if (ViewModel != _oldViewModel)
            {
                _oldViewModel = ViewModel;
                if (ViewModel != null)
                {
                    ViewModel.DeleteRequested += (s, e) => DeleteDocument();
                    UpdateBindings();
                }
            }
        }

        /// <summary>
        /// Sets the 2D stacking layer ("Z" value) of the document.
        /// If the document is marked as being an adornment, we want to place it below all other documents
        /// </summary>
        private void SetZLayer()
        {
            if (ViewModel?.DocumentController.GetIsAdornment() == true)
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
        
        private void this_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _down = e.GetCurrentPoint(MainPage.Instance.xCanvas).Position;
            CapturePointer(e.Pointer);
            PointerMoved    += this_PointerMoved;
            PointerReleased += this_PointerReleased;
            e.Handled = true;
        }
        private void this_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
            ReleasePointerCapture(e.Pointer);
            PointerMoved -= this_PointerMoved;
            PointerReleased -= this_PointerReleased;
        }
        private void this_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (ViewModel.DragAllowed)
            {
                e.Handled = true;
                var cur = e.GetCurrentPoint(MainPage.Instance.xCanvas).Position;
                var delta = new Point(cur.X - _down.X, cur.Y - _down.Y);
                if (ViewModel.AreContentsHitTestVisible && Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y) > 10)
                {
                    ReleasePointerCapture(e.Pointer);
                    PointerMoved -= this_PointerMoved;
                    PointerReleased -= this_PointerReleased;
                    SelectionManager.InitiateDragDrop(this, e);
                }
            }
        }
        private void This_Drop(object sender, DragEventArgs e)
        {
            if (!ViewModel.DocumentController.GetIsAdornment() && ViewModel.AreContentsHitTestVisible &&
                e.DataView.GetDragModel() is DragDocumentModel dm && dm.DraggedDocumentViews != null && dm.DraggingLinkButton)
            {
                e.Handled = true;

                if (MainPage.Instance.IsAltPressed())
                {
                    ApplyPseudoTemplate(dm);
                }
                else
                {
                    MakeDocumentLink(e.GetPosition(this), dm);
                    e.AcceptedOperation = e.DataView.RequestedOperation == DataPackageOperation.None ? DataPackageOperation.Link : e.DataView.RequestedOperation;
                }
            }
        }

        private void ApplyPseudoTemplate(DragDocumentModel dm)
        {
            var curLayout     = ViewModel.LayoutDocument;
            var draggedLayout = dm.DraggedDocuments.First().GetDataInstance(ViewModel.Position);
            draggedLayout.SetField(KeyStore.DocumentContextKey, ViewModel.DataDocument, true);
            if (double.IsNaN(curLayout.GetWidth()) || double.IsNaN(curLayout.GetHeight()))
            {
                curLayout.SetWidth (dm.DraggedDocuments.First().GetActualSize().Value.X);
                curLayout.SetHeight(dm.DraggedDocuments.First().GetActualSize().Value.Y);
            }
            curLayout.SetField(KeyStore.DataKey,            draggedLayout.GetField(KeyStore.DataKey), true);
            curLayout.SetField(KeyStore.PrototypeKey,       draggedLayout.GetField(KeyStore.PrototypeKey), true);
            curLayout.SetField(KeyStore.LayoutPrototypeKey, draggedLayout, true);

            curLayout.SetField(KeyStore.CollectionFitToParentKey, draggedLayout.GetDereferencedField(KeyStore.CollectionFitToParentKey, null), true);
            curLayout.DocumentType = draggedLayout.DocumentType;
            UpdateBindings();
        }

        private void FadeOut_Completed(object sender, object e)
        {
            ParentCollection?.ViewModel.RemoveDocument(ViewModel.DocumentController);

            DocumentDeleted?.Invoke(this);
            UndoManager.EndBatch();
        }

        private static void BindRenderTransformChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DocumentView)d).UpdateRenderTransformBinding();
        }

        /// <summary>
        /// Handles left and right tapped events on DocumentViews
        /// </summary>
        /// <param name="wasHandled">Whether the tapped event was previously handled
        /// this is always false currently so it probably isn't needed</param>
        /// <param name="wasRightTapped"></param>
        /// <returns>Whether the calling tapped event should be handled</returns>
        public async void TappedHandler(bool rightTapped)
        {
            _doubleTapped = false;
            if (!await ExhibitBehaviors(rightTapped ? KeyStore.RightTappedOpsKey : KeyStore.LeftTappedOpsKey))
            {
                if ((FocusManager.GetFocusedElement() as FrameworkElement)?.GetAncestorsOfType<DocumentView>().Contains(this) == false)
                {
                    Focus(FocusState.Programmatic);
                }

                MainPage.Instance.xPresentationView.TryHighlightMatches(this);
                
                ToFront();

                if (!MainPage.Instance.IsRightBtnPressed())
                {
                    await System.Threading.Tasks.Task.Delay(100);
                    if (!_doubleTapped)
                    {
                        if (!SelectionManager.SelectedDocViewModels.Contains(ViewModel) || this.IsShiftPressed())
                        {
                            SelectionManager.Select(this, this.IsShiftPressed());
                        }
                    }
                }

                if (SelectionManager.SelectedDocViewModels.Count() > 1)
                {
                    // move focus to container if multiple documents are selected (otherwise focus remains where it was)
                    (ParentCollection?.CurrentView as CollectionFreeformBase)?.Focus(FocusState.Programmatic);
                }
            }
        }

        private async Task<bool> ExhibitBehaviors(KeyController behaviorKey)
        {
            var scripts = ViewModel.DocumentController.GetBehaviors(behaviorKey);
            if (scripts != null && scripts.Any())
            {
                using (UndoManager.GetBatchHandle())
                {
                    var args = new List<FieldControllerBase> {ViewModel.DocumentController};
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

            if (behaviorKey.Equals(KeyStore.DoubleTappedOpsKey))
            {
                MenuFlyoutItemOpen_OnClick(null, null);
                _doubleTapped = true;
            }

            return false;
        }

        #region Context menu click handlers
        private void MenuFlyoutItemFields_Click(object sender, RoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                foreach (var doc in SelectionManager.GetSelectedSiblings(this))
                {
                    var kvp = doc.ViewModel.DocumentController.GetKeyValueAlias();
                    MainPage.Instance.AddFloatingDoc(kvp, new Point(500, 300), MainPage.Instance.xCanvas.PointerPos());
                }
            }
        }
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
        private void MenuFlyoutItemInstance_Click(object sender, RoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                foreach (var doc in SelectionManager.GetSelectedSiblings(this))
                {
                    doc.MakeInstance();
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
                    docView.ViewModel.DocumentController.SetIsAdornment(!docView.ViewModel.DocumentController.GetIsAdornment());
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
            var dp = new DataPackage();
            dp.SetText(GetScriptingRepresentation());
            Clipboard.SetContent(dp);
        }

        private string GetScriptingRepresentation()
        {
            var path = DocumentTree.GetPathsToDocuments(ViewModel.DocumentController).FirstOrDefault();
            if (path == null) return "Invalid path";

            var pathString = DocumentTree.GetEscapedPath(path);
            var pathScript = $"d(\"{pathString.Replace(@"\", @"\\").Replace("\"", "\\\"")}\")";
            return pathScript;
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
                SelectionManager.DeselectAll();
                SplitFrame.OpenInActiveFrame(ViewModel.DocumentController);
                var df = SplitFrame.ActiveFrame.GetFirstDescendantOfType<DocumentView>();
                SelectionManager.Select(df, false);
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
        private void MenuFlyoutItemPin_Click(object sender, RoutedEventArgs e)
        {
            if (!MainPage.Instance.IsTopLevel(ViewModel))
            {
                using (UndoManager.GetBatchHandle())
                {
                    MainPage.Instance.PinToPresentation(ViewModel.DocumentController);
                    if (ViewModel.LayoutDocument == null)
                    {
                        Debug.WriteLine("uh oh");
                    }
                }
            }
        }

        private void XAnnotateEllipseBorder_OnTapped_(object sender, TappedRoutedEventArgs e)
        {
           AnnotationManager.FollowRegion(this, ViewModel.DocumentController, this.GetAncestorsOfType<ILinkHandler>(), e.GetPosition(this));
        }

        private void AdjustEllipseSize(Ellipse ellipse, double length)
        {
            ellipse.Width = length;
            ellipse.Height = length;
        }

        private async void xMenuFlyout_Opening(object sender, object e)
        {
            xMenuFlyout.Items.Clear();

            xMenuFlyout.Items.Add(new MenuFlyoutItem()
            {
                Text = "Fields",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.Database }
            });
            (xMenuFlyout.Items.Last() as MenuFlyoutItem).Click += MenuFlyoutItemFields_Click;
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
            //xMenuFlyout.Items.Add(new MenuFlyoutItem()
            //{
            //    Text = "Delete",
            //    Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.Trash }
            //});
            //(xMenuFlyout.Items.Last() as MenuFlyoutItem).Click += MenuFlyoutItemDelete_Click;
            //xMenuFlyout.Items.Add(new MenuFlyoutItem()
            //{
            //    Text = "Hide",
            //    Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.Close }
            //});
            //(xMenuFlyout.Items.Last() as MenuFlyoutItem).Click += MenuFlyoutItemHide_Click;

            xMenuFlyout.Items.Add(new MenuFlyoutSeparator());

            xMenuFlyout.Items.Add(new MenuFlyoutItem()
            {
                Text = "Duplicate",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.Copy }
            });
            (xMenuFlyout.Items.Last() as MenuFlyoutItem).Click += MenuFlyoutItemCopy_Click;
            xMenuFlyout.Items.Add(new MenuFlyoutItem()
            {
                Text = "Alias (duplicate view)",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.Link }
            });
            (xMenuFlyout.Items.Last() as MenuFlyoutItem).Click += MenuFlyoutItemAlias_Click;
            xMenuFlyout.Items.Add(new MenuFlyoutItem()
            {
                Text = "Instance",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.Link }
            });
            (xMenuFlyout.Items.Last() as MenuFlyoutItem).Click += MenuFlyoutItemInstance_Click;
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
                Text = _anyBehaviors ? "Manage Behaviors" : "Add Behaviors",
                Icon = new FontIcons.FontAwesome { Icon = _anyBehaviors ? FontAwesomeIcon.AddressBook : FontAwesomeIcon.Plus }
            });
            (xMenuFlyout.Items.Last() as MenuFlyoutItem).Click += async (o, args) =>
            {
                await UIFunctions.ManageBehaviors(ViewModel.DocumentController);
            };
            //Add the Layout Template Popup
           xMenuFlyout.Items.Add(new MenuFlyoutItem()
            {
                Text = "Apply Template",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.Sitemap }
            });
            (xMenuFlyout.Items.Last() as MenuFlyoutItem).Click += MenuFlyoutItemLayoutTemplates_Click;
            if (true || ViewModel.DocumentController.DocumentType.Equals(RichTextBox.DocumentType))
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

                bool flashcard = false;
                if (template.Contains("FlashcardTemplate"))
                    flashcard = true;

                foreach (var doc in docs)
                {
                    doc.SetField<TextController>(KeyStore.XamlKey, template, true);
                    if (flashcard)
                    {
                        doc.SetWidth(600);
                        doc.SetHeight(600);
                    }
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
        private void MenuFlyoutItemMakeDefaultTextBox_Click(object sender, RoutedEventArgs e)
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

        #endregion

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
                e.Handled = true;
            }

            _pointerPoint = e.GetCurrentPoint(LayoutRoot).Position;
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
        
        private void XContent_OnHolding(object sender, HoldingRoutedEventArgs e)
        {
            xMenuFlyout_Opening(sender, e);
        }

        private void MasterStackShowTooltip(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Grid g && ToolTipService.GetToolTip(g) is ToolTip tip) tip.IsOpen = true;
        }

        private void MasterStackHideTooltip(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Grid g && ToolTipService.GetToolTip(g) is ToolTip tip) tip.IsOpen = false;
        }
    }
}
