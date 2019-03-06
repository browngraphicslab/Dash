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
        private static readonly SolidColorBrush    SingleSelectionBorderColor = new SolidColorBrush(Colors.LightGray);
        private static readonly SolidColorBrush    GroupSelectionBorderColor = new SolidColorBrush(Colors.LightBlue);
        private static readonly DependencyProperty BindRenderTransformProperty = DependencyProperty.Register(
            "BindRenderTransform", typeof(bool), typeof(DocumentView), new PropertyMetadata(default(bool), BindRenderTransformChanged));
        private bool              _anyBehaviors => ViewModel.LayoutDocument.GetDataDocument().GetField<ListController<DocumentController>>(KeyStore.DocumentBehaviorsKey)?.Any() ?? false;
        private DocumentViewModel _oldViewModel = null;
        private bool              _doubleTapped = false;
        private Point             _down         = new Point();
        private Point             _pointerPoint = new Point(0, 0);

        public CollectionView      ParentCollection => this.GetFirstAncestorOfType<CollectionView>();
        public CollectionViewModel ParentViewModel => ParentCollection?.ViewModel;
        public DocumentViewModel   ViewModel 
        {
            get { try { return DataContext as DocumentViewModel; } catch (Exception) { return null; } }
            set => DataContext = value;
        }
        public bool                AreContentsActive => SelectionManager.SelectedDocViews.Any(sel => sel == this || sel.GetAncestors().Contains(this)) || SplitManager.IsRoot(ViewModel);
        public Action              FadeOutBegin;

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

                if (ViewModel != null)
                {
                    var opacityBinding = new FieldBinding<NumberController>
                    {
                        Document = ViewModel.DocumentController,
                        Key = KeyStore.OpacityKey,
                        Mode = BindingMode.OneWay,
                        FallbackValue = 1
                    };
                    this.AddFieldBinding(OpacityProperty, opacityBinding);
                }
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
            xMenuFlyout.Closed += (s, e) =>
            {
                if (this.IsShiftPressed())
                    xMenuFlyout.Hide();
            };

            DragStarting  += (s, e) => SelectionManager.DragStarting(this, s, e);
            DropCompleted += (s, e) => SelectionManager.DropCompleted(this, s, e);
            RightTapped   += (s, e) => { e.Handled = true; TappedHandler(true); };
            Tapped        += (s, e) => { e.Handled = true; TappedHandler(false); };
            DoubleTapped  += async (s, e) => { e.Handled = await ExhibitBehaviors(KeyStore.DoubleTappedOpsKey); };
            PointerPressed += (s, e) => this_PointerPressed(s, e);

            ToFront();
            xContentClip.Rect = new Rect(0, 0, Width, Height);
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
                ViewModel.DocumentController.Resize(
                                 Util.DeltaTransformFromVisual(e.Delta.Translation,      this), 
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
            if (this.GetFirstAncestorOfType<AnnotationOverlayEmbeddings>() is AnnotationOverlayEmbeddings embedding)
            {
                embedding.EmbeddedDocsList.Remove(ViewModel.DocumentController);
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
                ParentViewModel?.AddDocument(ViewModel.DocumentController.GetCopy(null));
            }
        }
        /// <summary>
        /// Copies the Document.
        /// </summary>
        public void MakeInstance()
        {
            using (UndoManager.GetBatchHandle())
            {
                ParentViewModel?.AddDocument(ViewModel.DocumentController.GetDataInstance(null));
            }
        }
        /// <summary>
        /// Copes the DocumentView for the document
        /// </summary>
        public void CopyViewDocument()
        {
            using (UndoManager.GetBatchHandle())
            {
                ParentViewModel?.AddDocument(ViewModel.DocumentController.GetViewCopy(null));
            }
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
            MainPage.Instance.XDocumentDecorations.RebuildMenu();
            MainPage.Instance.XDocumentDecorations.SetPositionAndSize();
            MainPage.Instance.XDocumentDecorations.OpenNewLinkMenu(dm.DraggedLinkType, lastLinkDoc);
        }
        /// <summary>
        /// Opens in Chrome the context from which the document was made.
        /// </summary>
        public void ShowContext()                { ViewModel.DocumentController.GetDataDocument().RestoreNeighboringContext(); }
        public void ShowContextMenu(Point where) { xMenuFlyout.ShowAt(null, where); }
        /// <summary>
        /// Opens in Chrome the context from which the document was made.
        /// </summary>
        public void ShowXaml()                   { ParentViewModel?.AddDocument(new DataBox(ViewModel.LayoutDocument, KeyStore.XamlKey, ViewModel.LayoutDocument.GetPosition(), 300,400).Document); }
        public void GetJson()                    { Util.ExportAsJson(ViewModel.DocumentController.EnumFields()); }
        
        //binds the background color of the document to the ViewModel's LayoutDocument's BackgroundColorKey
        private void UpdateBackgroundColorBinding()
        {
            var doc = ViewModel?.LayoutDocument;
            var backgroundBinding = doc == null ? null :new FieldBinding<TextController>()
            {
                Key       = KeyStore.BackgroundColorKey,
                Document  = doc,
                Converter = new StringToBrushConverter(),
                Mode      = BindingMode.TwoWay,
                FallbackValue = new SolidColorBrush(Colors.Transparent)
            };
            xDocContentPresenter.AddFieldBinding(ContentPresenter.BackgroundProperty, backgroundBinding);
        }
        private void UpdateRenderTransformBinding()
        {
            var doc = ViewModel?.LayoutDocument;

            var binding = !BindRenderTransform || doc == null ? null
                : new FieldMultiBinding<MatrixTransform>(new DocumentFieldReference(doc, KeyStore.PositionFieldKey))
                {
                    Converter = new TransformGroupMultiConverter(),
                    Context   = new Context(doc),
                    Mode      = BindingMode.OneWay,
                    CanBeNull = true,
                    Tag       = "RenderTransform multi binding in DocumentView"
                };
            this.AddFieldBinding(RenderTransformProperty, binding);
            if (ViewModel?.IsDimensionless == true)
            {
                Width  = double.NaN;
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

            var binding = doc == null ? null : new FieldBinding<BoolController>
                {
                    Converter = new InverseBoolToVisibilityConverter(),
                    Document  = doc,
                    Key       = KeyStore.HiddenKey,
                    Mode      = BindingMode.TwoWay,
                    Tag       = "Visibility binding in DocumentView",
                    FallbackValue = false
                };
            this.AddFieldBinding(VisibilityProperty, binding);

            var binding3 = doc == null ? null : new FieldBinding<BoolController>
            {
                Document = doc,
                Key      = KeyStore.AreContentsHitTestVisibleKey,
                Mode     = BindingMode.TwoWay,
                Tag      = "AreContentsHitTestVisible binding in DocumentView",
                FallbackValue = true
            };
            this.AddFieldBinding(IsHitTestVisibleProperty, binding3);
        }
        private void UpdateAlignmentBindings()
        {
            if (ViewModel?.IsDimensionless == true)
            {
                HorizontalAlignment = HorizontalAlignment.Stretch;
                VerticalAlignment   = VerticalAlignment.Stretch;
                this.AddFieldBinding(HorizontalAlignmentProperty, null);
                this.AddFieldBinding(VerticalAlignmentProperty, null);
            }
            else
            {
                CourtesyDocument.BindHorizontalAlignment(this, ViewModel?.LayoutDocument, HorizontalAlignment.Left);
                CourtesyDocument.BindVerticalAlignment  (this, ViewModel?.LayoutDocument, VerticalAlignment.Top);
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
            ToolTipService.SetToolTip(this, label);
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
                var parCanvas = cp.GetFirstAncestorOfTypeFast<Canvas>();
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
            e.Handled = true;
            _down     = e.GetCurrentPoint(MainPage.Instance.xCanvas).Position;
            CapturePointer(e.Pointer);
            PointerMoved    += this_PointerMoved;
            PointerReleased += this_PointerReleased;
        }
        private void this_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
            ReleasePointerCapture(e.Pointer);
            PointerMoved    -= this_PointerMoved;
            PointerReleased -= this_PointerReleased;
        }
        private void this_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (ViewModel?.DragAllowed == true)
            {
                e.Handled = true;
                var cur   = e.GetCurrentPoint(MainPage.Instance.xCanvas).Position;
                var delta = new Point(cur.X - _down.X, cur.Y - _down.Y);
                if (ViewModel.LayoutDocument.GetAreContentsHitTestVisible() && Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y) > 10)
                {
                    ReleasePointerCapture(e.Pointer);
                    PointerMoved    -= this_PointerMoved;
                    PointerReleased -= this_PointerReleased;
                    SelectionManager.InitiateDragDrop(this, e);
                }
            }
        }
        private void This_Drop(object sender, DragEventArgs e)
        {
            if (!ViewModel.DocumentController.GetIsAdornment() && ViewModel.LayoutDocument.GetAreContentsHitTestVisible() &&
                e.DataView.GetDragModel() is DragDocumentModel dm && dm.DraggedDocumentViews != null && dm.DraggingLinkButton)
            {
                e.Handled = true;

                //if (MainPage.Instance.IsAltPressed())
                //{
                //    ApplyPseudoTemplate(dm);
                //}
                //else
                {
                    MakeDocumentLink(e.GetPosition(this), dm);
                    e.AcceptedOperation = e.DataView.RequestedOperation == DataPackageOperation.None ? DataPackageOperation.Link : e.DataView.RequestedOperation;
                }
            }
        }

        private void FadeOut_Completed(object sender, object e)
        {
            ParentViewModel?.RemoveDocument(ViewModel.DocumentController);
            
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

                if (!rightTapped)
                {
                    ToFront();
                    await System.Threading.Tasks.Task.Delay(100);
                    if (!_doubleTapped)
                    {
                        if (!SelectionManager.SelectedDocViews.Contains(this) || this.IsShiftPressed())
                        {
                            SelectionManager.Select(this, this.IsShiftPressed());
                        }
                        else if (!SelectionManager.SelectedDocViews.Any(dv => dv.GetFirstAncestorOfType<SplitFrame>() == null)) // clear the floating documents unless the newly selected document is a floating document
                        {
                            MainPage.Instance.ClearFloatingDoc(null);
                        }
                    }
                }
                else if (!SelectionManager.SelectedDocViews.Contains(this) || this.IsShiftPressed())
                {
                    SelectionManager.Select(this, this.IsShiftPressed());
                }

                if (SelectionManager.SelectedDocViewModels.Count() > 1)
                {
                    // move focus to container if multiple documents are selected (otherwise focus remains where it was)
                    (ParentCollection?.CurrentView as CollectionFreeformView)?.Focus(FocusState.Programmatic);
                }
            }
        }

        private async Task<bool> ExhibitBehaviors(KeyController behaviorKey)
        {
            var scripts = ViewModel?.DocumentController.GetBehaviors(behaviorKey);
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
                if (!ViewModel.DocumentController.DocumentType.Equals(RichTextBox.DocumentType))
                {
                    MenuFlyoutItemOpen_OnClick(null, null);
                    _doubleTapped =  true;
                }
                return true;
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
        private void MenuFlyoutItemLinks_Click(object sender, RoutedEventArgs e)
        {
            var linkColl = ViewModel.DataDocument.GetLinkCollection();
            if (linkColl != null)
            {
                using (UndoManager.GetBatchHandle())
                {
                    MainPage.Instance.AddFloatingDoc(linkColl, new Point(500, 300), MainPage.Instance.xCanvas.PointerPos());
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
            await Util.ExportAsImage(this);
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
            if (!SplitManager.IsRoot(ViewModel))
            {
                using (UndoManager.GetBatchHandle())
                {
                    MainPage.Instance.xPresentationView.PinToPresentation(ViewModel.DocumentController);                    if (ViewModel.LayoutDocument == null)
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
        private void xMenuFlyout_Opening(object sender, object e)
        {
            if (!ViewModel.IsSelected)
            {
                SelectionManager.Select(this, this.IsShiftPressed());
            }
            xMenuFlyout.Items.Clear();

            xMenuFlyout.Items.Add(new MenuFlyoutItem()
            {
                Text = "Fields",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.Database }
            });
            (xMenuFlyout.Items.Last() as MenuFlyoutItem).Click += MenuFlyoutItemFields_Click;
            xMenuFlyout.Items.Add(new MenuFlyoutItem()
            {
                Text = "Links",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.Link }
            });
            (xMenuFlyout.Items.Last() as MenuFlyoutItem).Click += MenuFlyoutItemLinks_Click;
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

            var scriptSource = ViewModel.DocumentController.GetField<TextController>(KeyStore.ScriptSourceKey);
            if (scriptSource != null)
            {
                var lineIndex = scriptSource.Data.IndexOfAny(new[] {'\r', '\n'});
                xMenuFlyout.Items.Add(new MenuFlyoutItem
                {
                    Text = lineIndex == -1 ? "Refresh: " + scriptSource.Data : "Refresh script",
                    Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.Code }
                });
                (xMenuFlyout.Items.Last() as MenuFlyoutItem).Click += async (o, args) =>
                {
                    var (field, error) = await ExecutionEnvironment.Run(scriptSource.Data);
                    if (error != null)
                    {
                        Debug.WriteLine($"Error executing script\n{scriptSource.Data}\nError: {error.GetHelpfulString()}");
                    }

                    if (field != null)
                    {
                        ViewModel.DataDocument.SetField(KeyStore.DataKey, field, true);
                    }
                };
            }

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
            else if ((ViewModel.Content is ContentPresenter cpresent) &&
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
                var template = await MainPage.Instance.PromptLayoutTemplate(docs);

                if (template == null)
                    return;

                bool flashcard = false;
                if (template.Contains("FlashcardTemplate"))
                    flashcard = true;

                foreach (var doc in docs)
                {
                    doc.SetXaml(template);
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
                ViewModel.LayoutDocument.GetXaml(), true);
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
                var actualSize = ViewModel.LayoutDocument.GetActualSize();
                bool moveXAllowed = xContentTransform.Matrix.OffsetX + deltaX <= 0 &&
                    xContentTransform.Matrix.M11 * actualSize.X + xContentTransform.Matrix.OffsetX + deltaX + 0.2 >= actualSize.X;
                bool moveYAllowed =
                    xContentTransform.Matrix.OffsetY + deltaY <= 0 && xContentTransform.Matrix.M22 * actualSize.Y + xContentTransform.Matrix.OffsetY + deltaY + 0.2 >= actualSize.Y;

                var tgroup = new TransformGroup();
                tgroup.Children.Add(xContentTransform);
                tgroup.Children.Add(new TranslateTransform() { X = moveXAllowed ? deltaX : 0, Y = moveYAllowed ? deltaY : 0 });
                xContentTransform.Matrix = tgroup.Value;
            }
        }

        //checks if we should be panning content
        private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            //if ctrl is pressed and either or both of left/right btns, we should pan content
            if (this.IsCtrlPressed() && this.IsLeftBtnPressed())
            {
                var curPt = e.GetCurrentPoint(this).Position;
                PanContent(-_pointerPoint.X + curPt.X, -_pointerPoint.Y + curPt.Y);
                e.Handled = true;
            }

            _pointerPoint = e.GetCurrentPoint(this).Position;
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
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
                scale.CenterX = e.GetCurrentPoint(this).Position.X;
                scale.CenterY = e.GetCurrentPoint(this).Position.Y;

                var tgroup = new TransformGroup();
                tgroup.Children.Add(xContentTransform);
                tgroup.Children.Add(scale);
                xContentTransform.Matrix = tgroup.Value;

                var actualSize = ViewModel.LayoutDocument.GetActualSize();
                //use transform bounds to check if content has gotten out of bounds and if so, pan to compensate
                var tb = xContentTransform.TransformBounds(new Rect(0, 0, actualSize.X, actualSize.Y));
                if (tb.X > 0) PanContent(0 - tb.X, 0);
                if (tb.Y > 0) PanContent(0, 0 - tb.Y);
                if (tb.X + tb.Width < actualSize.X) PanContent(actualSize.X - (tb.X + tb.Width), 0);
                if (tb.Y + tb.Height < actualSize.Y) PanContent(0, actualSize.Y - (tb.Y + tb.Height));

                e.Handled = true;
            }
        }
        
        private void XContent_OnHolding(object sender, HoldingRoutedEventArgs e)
        {
            xMenuFlyout_Opening(sender, e);
        }

        private void ShowTooltip(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Grid g && ToolTipService.GetToolTip(g) is ToolTip tip)
            {
                tip.IsOpen = true;
            }
        }
        private void HideTooltip(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Grid g && ToolTipService.GetToolTip(g) is ToolTip tip && tip.IsOpen)
            {
                tip.IsOpen = false;
            }
        }
    }
}
