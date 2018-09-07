using DashShared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Andy.Code4App.Extension.CommonObjectEx;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;
using Dash.Converters;
using Visibility = Windows.UI.Xaml.Visibility;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.Graphics.Display;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using Windows.UI.Input;
using Windows.UI.Xaml.Media.Animation;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236
namespace Dash
{
    public sealed partial class DocumentView
    {
        DocumentController _templateEditor;
        bool               _isQuickEntryOpen;
        readonly Flyout    _flyout = new Flyout { Placement = FlyoutPlacementMode.Right };
        DocumentViewModel  _oldViewModel = null;

        static readonly SolidColorBrush SingleSelectionBorderColor = new SolidColorBrush(Colors.LightGray);
        static readonly SolidColorBrush GroupSelectionBorderColor = new SolidColorBrush(Colors.LightBlue);
        public CollectionView ParentCollection => this.GetFirstAncestorOfType<CollectionView>();
        public DocumentViewModel ViewModel
        {
            get => DataContext as DocumentViewModel;
            set => DataContext = value;
        }
        public MenuFlyout MenuFlyout => xMenuFlyout;
        public bool PreventManipulation { get; set; }
        // the document that has input focus (logically similar to keyboard focus but different since Images, etc can't be keyboard focused).
        public static DocumentView FocusedDocument { get; set; }
        public static readonly DependencyProperty BindRenderTransformProperty = DependencyProperty.Register(
            "BindRenderTransform", typeof(bool), typeof(DocumentView), new PropertyMetadata(default(bool)));

        public bool BindRenderTransform
        {
            get => (bool)GetValue(BindRenderTransformProperty);
            set => SetValue(BindRenderTransformProperty, value);
        }

        public static readonly DependencyProperty BindVisibilityProperty = DependencyProperty.Register(
            "BindVisibility", typeof(bool), typeof(DocumentView), new PropertyMetadata(default(bool)));

        public bool BindVisibility
        {
            get => (bool)GetValue(BindVisibilityProperty);
            set => SetValue(BindVisibilityProperty, value);
        }

        public event Action<DocumentView> DocumentDeleted;

        public DocumentView()
        {
            InitializeComponent();

            Util.InitializeDropShadow(xShadowHost, xDocumentBackground);
            // set bounds
            MinWidth = 25;
            MinHeight = 25;

            RegisterPropertyChangedCallback(BindRenderTransformProperty, updateRenderTransformBinding);
            RegisterPropertyChangedCallback(BindVisibilityProperty, updateVisibilityBinding);

            void updateRenderTransformBinding(object sender, DependencyProperty dp)
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
                        Tag = "RenderTransform multi binding in DocumentView"
                    };
                this.AddFieldBinding(RenderTransformProperty, binding);
            }

            void updateVisibilityBinding(object sender, DependencyProperty dp)
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
            }

            void updateBindings()
            {
                updateRenderTransformBinding(null, null);
                updateVisibilityBinding(null, null);

                _templateEditor = ViewModel?.DataDocument.GetField<DocumentController>(KeyStore.TemplateEditorKey);

                this.BindBackgroundColor();
                ViewModel?.Load();
            }

            void sizeChangedHandler(object sender, SizeChangedEventArgs e)
            {
                ViewModel?.LayoutDocument.SetActualSize(new Point(ActualWidth, ActualHeight));
            }
            Loaded += (sender, e) =>
            {
                FadeIn.Begin();
                updateBindings();
                DataContextChanged += (s, a) =>
                {
                    if (a.NewValue != _oldViewModel)
                    {
                        _oldViewModel?.UnLoad();
                        updateBindings();
                        _oldViewModel = ViewModel;
                    }
                };

                SizeChanged += sizeChangedHandler;
                ViewModel?.LayoutDocument.SetActualSize(new Point(ActualWidth, ActualHeight));
                
                var parentCanvas = this.GetFirstAncestorOfType<ContentPresenter>()?.GetFirstAncestorOfType<Canvas>() ?? new Canvas();
                var maxZ = parentCanvas.Children.Aggregate(int.MinValue, (agg, val) => Math.Max(Canvas.GetZIndex(val), agg));
                Canvas.SetZIndex(this.GetFirstAncestorOfType<ContentPresenter>(), maxZ + 1);
                SetZLayer();
            };

            Unloaded += (sender, args) =>
            {
                SizeChanged -= sizeChangedHandler;
                SelectionManager.Deselect(this);
                ViewModel?.UnLoad();
                DataContext = null;
            };

            PointerPressed += (sender, e) =>
            {
                bool right = e.IsRightPressed() || MenuToolbar.Instance.GetMouseMode() == MenuToolbar.MouseMode.PanFast;
                var parentFreeform = this.GetFirstAncestorOfType<CollectionFreeformBase>();
                var parentParentFreeform = parentFreeform?.GetFirstAncestorOfType<CollectionFreeformBase>();
                ManipulationMode = right ? ManipulationModes.All : ManipulationModes.None;
                MainPage.Instance.Focus(FocusState.Programmatic);
                e.Handled = true;
                if (parentParentFreeform != null && !this.IsShiftPressed())
                {
                    e.Handled = false;
                }
            };


            ManipulationMode = ManipulationModes.All;
            ManipulationStarted   += (s,e) => SelectionManager.InitiateDragDrop(this, null, e);
            DragStarting          += (s,e) => SelectionManager.DragStarting(this, s, e);
            DropCompleted         += (s,e) => SelectionManager.DropCompleted(this, s, e);
            RightTapped           += (s,e) => e.Handled = TappedHandler(e.Handled);
            Tapped                += (s,e) => e.Handled = TappedHandler(e.Handled);

            xKeyBox.AddKeyHandler(VirtualKey.Enter, KeyBoxOnEnter);
            xValueBox.AddKeyHandler(VirtualKey.Enter, ValueBoxOnEnter);

            _lastValueInput = "";

            xQuickEntryIn.Completed += (sender, o) =>
            {
                xKeyBox.Text = "d.";
                xKeyBox.SelectionStart = 2;
            };

            xKeyEditSuccess.Completed += SetFocusToKeyBox;
            xValueErrorFailure.Completed += SetFocusToKeyBox;

            xKeyBox.TextChanged += XKeyBoxOnTextChanged;
            xKeyBox.BeforeTextChanging += XKeyBoxOnBeforeTextChanging;
            xValueBox.TextChanged += XValueBoxOnTextChanged;

            xValueBox.GotFocus += XValueBoxOnGotFocus;

            LostFocus += (sender, args) =>
            {
                if (_isQuickEntryOpen && xKeyBox.FocusState == FocusState.Unfocused && xValueBox.FocusState == FocusState.Unfocused) ToggleQuickEntry();

                MainPage.Instance.xPresentationView.ClearHighlightedMatch();
            };
            
            MenuFlyout.Opened += (s, e) =>
            {
                if (this.IsShiftPressed())
                    MenuFlyout.Hide();
            };

            ToFront();
        }
        private void ToggleAnnotationVisibility_OnClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is MenuFlyoutItem item)) return;

            Dictionary<string, List<DocumentController>>.ValueCollection linkDocs = MainPage.Instance.XDocumentDecorations.TagMap.Values;

            bool allVisible = linkDocs.All(l => l.All(doc => doc.GetField<BoolController>(KeyStore.IsAnnotationScrollVisibleKey)?.Data ?? false));

            foreach (var docs in linkDocs)
            {
                foreach (DocumentController l in docs)
                {
                    l.SetField<BoolController>(KeyStore.IsAnnotationScrollVisibleKey, !allVisible, true);
                    l.SetField<BoolController>(KeyStore.HiddenKey, allVisible, true);
                }
            }
        }

        private void XMenuFlyout_OnOpening(object sender, object e)
        {
            var linkDocs = MainPage.Instance.XDocumentDecorations.TagMap.Values;
            bool allVisible = linkDocs.All(l => l.All(doc => doc.GetField<BoolController>(KeyStore.IsAnnotationScrollVisibleKey)?.Data ?? false));
            xAnnotationVisibility.Text = allVisible ? "Hide Annotations on Scroll" : "Show Annotations on Scroll";
        }

        private void XKeyBoxOnBeforeTextChanging(TextBox textBox, TextBoxBeforeTextChangingEventArgs e)
        {
            if (!_clearByClose && e.NewText.Length <= xKeyBox.Text.Length)
            {
                if (xKeyBox.Text.Length <= 2 && !(e.NewText.StartsWith("d.") || e.NewText.StartsWith("v.")))
                {
                    e.Cancel = true;
                }
                else
                {
                    if (string.IsNullOrEmpty(e.NewText))
                    {
                        xKeyBox.Text = xKeyBox.Text.Substring(0, 2);
                        xKeyBox.SelectionStart = 2;
                        xKeyBox.Focus(FocusState.Keyboard);
                    }
                }
            }
            else
            {
                if (!(e.NewText.StartsWith("d.") || e.NewText.StartsWith("v."))) e.Cancel = true;
            }
            _clearByClose = false;
        }

        private void ClearQuickEntryBoxes()
        {
            _lastValueInput = "";
            xKeyBox.Text = "";
            xValueBox.Text = "";
        }

        public void ToggleTemplateEditor()
        {
            if (ViewModel.DataDocument.GetField<DocumentController>(KeyStore.TemplateEditorKey) == null)
            {
                var where = new Point((RenderTransform as MatrixTransform).Matrix.OffsetX + ActualWidth + 60,
                    (RenderTransform as MatrixTransform).Matrix.OffsetY);
                if (_templateEditor != null)
                {
                    Actions.DisplayDocument(ParentCollection.ViewModel, _templateEditor, where);

                    _templateEditor.SetHidden(!_templateEditor.GetHidden());
                    ViewModel.DataDocument.SetField(KeyStore.TemplateEditorKey, _templateEditor, true);
                    return;
                }

                _templateEditor = new TemplateEditorBox(ViewModel.DocumentController, where, new Size(1000, 540))
                    .Document;

                ViewModel.DataDocument.SetField(KeyStore.TemplateEditorKey, _templateEditor, true);
                //creates a doc controller for the image(s)
                Actions.DisplayDocument(ParentCollection.ViewModel, _templateEditor, where);
            }
            else
            {
                _templateEditor = ViewModel.DataDocument.GetField<DocumentController>(KeyStore.TemplateEditorKey);
                ViewModel.DataDocument.SetField(KeyStore.TemplateEditorKey, _templateEditor, true);
                _templateEditor.SetHidden(!_templateEditor.GetHidden());
            }
        }

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
        public void Resize(FrameworkElement sender, ManipulationDeltaRoutedEventArgs e, bool shiftTop, bool shiftLeft, bool maintainAspectRatio)
        {
            e.Handled = true;
            if (PreventManipulation || MainPage.Instance.IsRightBtnPressed())
            {
                return;
            }

            var isImage = ViewModel.DocumentController.DocumentType.Equals(ImageBox.DocumentType) ||
                ViewModel.DocumentController.DocumentType.Equals(VideoBox.DocumentType);

            double extraOffsetX = 0;
            if (!Double.IsNaN(Width))
            {
                extraOffsetX = ActualWidth - Width;
            }


            double extraOffsetY = 0;

            if (!Double.IsNaN(Height))
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

            // clamp the drag position to the available Bounds
            if (ViewModel.DragBounds != null)
            {
                var width = ActualWidth;
                var height = ActualHeight;
                var pos = new Point(ViewModel.XPos + width * (1 - moveXScale),
                    ViewModel.YPos + height * (1 - moveYScale));
                if (!ViewModel.DragBounds.Rect.Contains((new Point(pos.X + delta.X, pos.Y + delta.Y))))
                    return;
                var clamped = Util.Clamp(new Point(pos.X + delta.X, pos.Y + delta.Y), ViewModel.DragBounds.Rect);
                delta = new Point(clamped.X - pos.X, clamped.Y - pos.Y);
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
            if (ViewModel.DragBounds != null)
            {
                if (!ViewModel.DragBounds.Rect.Contains(newPos) ||
                    !ViewModel.DragBounds.Rect.Contains(new Point(newPos.X + newSize.Width, newPos.Y + DesiredSize.Height)))
                {
                    ViewModel.Position = oldPos;
                    ViewModel.Width = oldSize.Width;
                    ViewModel.Height = oldSize.Height;
                    return;
                }

                var clamp = Util.Clamp(newPos, ViewModel.DragBounds.Rect);
                newSize.Width += newPos.X - clamp.X;
                newSize.Height += newPos.Y - clamp.Y;
                newPos = clamp;
                var br = Util.Clamp(new Point(newPos.X + newSize.Width, newPos.Y + newSize.Height), ViewModel.DragBounds.Rect);
                newSize = new Size(br.X - newPos.X, br.Y - newPos.Y);
            }

            ViewModel.Position = newPos;
            ViewModel.Width = newSize.Width;

            if (delta.Y != 0 || this.IsShiftPressed() || isImage)
                ViewModel.Height = newSize.Height;
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

        /// <summary>
        /// Ensures the menu flyout is shown on right tap.
        /// </summary>
        public void ForceLeftTapped()
        {
            TappedHandler(false);
        }

        // this action is used to remove template editor in sync with document
        public Action FadeOutBegin;
        private bool _animationBusy;
        private string _lastValueInput;
        private bool _articialChange;
        private bool _clearByClose;
        private string _mostRecentPrefix;

        /// <summary>
        /// Deletes the document from the view.
        /// </summary>
        /// <param name="addTextBox"></param>
        public void DeleteDocument(bool addTextBox = false)
        {
            if (this.GetFirstAncestorOfType<AnnotationOverlay>() != null)
            {
                // bcz: if the document is on an annotation layer, then deleting it would orphan its annotation pin,
                //      but it would still be in the list of pinned annotations.  That means the document would reappear
                //      the next time the container document gets loaded.  We need a cleaner way to handle deleting 
                //      documents which would allow us to delete this document and any references to it, including possibly removing the pin
                this.ViewModel.DocumentController.SetHidden(true);
            }
            else if (ParentCollection != null)
            {
                UndoManager.StartBatch(); // bcz: EndBatch happens in FadeOut completed
                FadeOut.Begin();
                FadeOutBegin?.Invoke();

                if (addTextBox)
                {
                    (ParentCollection.CurrentView as CollectionFreeformBase)?.RenderPreviewTextbox(ViewModel.Position);
                }

                MainPage.Instance.ActivationManager.DeactivateDoc(this);
                SelectionManager.Deselect(this);
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

        public void GetJson()
        {
            Util.ExportAsJson(ViewModel.DocumentController.EnumFields());
        }

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
            SetSelectionBorder(true);
            this.GetAncestorsOfType<CollectionView>().ToList().ForEach(p => p.selectedCollection = true);
            DocumentSelected?.Invoke(this);
        }

        public void OnDeselected()
        {
            SetSelectionBorder(false);
            this.GetAncestorsOfType<CollectionView>().ToList().ForEach(p => p.selectedCollection = false);
            DocumentDeselected?.Invoke(this);
        }

        private void SetSelectionBorder(bool selected)
        {
            xTargetBorder.BorderThickness = selected ? new Thickness(3) : new Thickness(0);
            xTargetBorder.Margin = selected ? new Thickness(-3) : new Thickness(0);
            xTargetBorder.BorderBrush = new SolidColorBrush(Colors.Transparent);

            ColorSelectionBorder(selected ? Color.FromArgb(120, 160, 197, 232) : Colors.Transparent);

        }

        private void ColorSelectionBorder(Color color)
        {
            var brush = new SolidColorBrush(color);
        }

        #endregion

        /// <summary>
        /// Handles left and right tapped events on DocumentViews
        /// </summary>
        /// <param name="wasHandled">Whether the tapped event was previously handled</param>//this is always false currently so it probably isn't needed
        /// <returns>Whether the calling tapped event should be handled</returns>
        public bool TappedHandler(bool wasHandled)
        {
            if (!wasHandled)
            {
                FocusedDocument = this;
            }

            if (!(FocusManager.GetFocusedElement() as FrameworkElement).GetAncestorsOfType<DocumentView>()
                .Contains(this))
            {
                Focus(FocusState.Programmatic);
            }
            //if (!Equals(MainPage.Instance.MainDocView)) Focus(FocusState.Programmatic);

            MainPage.Instance.xPresentationView.TryHighlightMatches(this);

            //TODO Have more standard way of selecting groups/getting selection of groups to the toolbar
            if (ViewModel?.IsAdornmentGroup == false)
            {
                ToFront();
            }

            //         if (!this.IsRightBtnPressed() && (ParentCollection == null || ParentCollection.CurrentView is CollectionFreeformBase) && (e == null || !e.Handled))
            if ((ParentCollection == null || ParentCollection?.CurrentView is CollectionFreeformBase) && !wasHandled)
            {
                var cfview = ParentCollection?.CurrentView as CollectionFreeformBase;
                SelectionManager.Select(this, this.IsShiftPressed());

                if (SelectionManager.GetSelectedDocs().Count > 1)
                {
                    // move focus to container if multiple documents are selected, otherwise allow keyboard focus to remain where it was
                    cfview?.Focus(FocusState.Programmatic);
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
            if (collection == null) return;
            var where = this.TransformToVisual(docCanvas).TransformPoint(new Point(0, ActualHeight + 1));

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

            collection.LoadNewActiveTextBox("", where, true);
        }

        #endregion

        #region Context menu click handlers

        private void MenuFlyoutItemCopy_Click(object sender, RoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                foreach (var doc in SelectionManager.GetSelectedSiblings(this))
                    doc.CopyDocument();
            }
        }
        private void MenuFlyoutItemAlias_Click(object sender, RoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
                foreach (var doc in SelectionManager.GetSelectedSiblings(this))
                    doc.CopyViewDocument();
        }
        private void MenuFlyoutItemDelete_Click(object sender, RoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
                foreach (var doc in SelectionManager.GetSelectedSiblings(this))
                    doc.DeleteDocument();
        }
        private void MenuFlyoutItemFields_Click(object sender, RoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
                foreach (var doc in SelectionManager.GetSelectedSiblings(this))
                    doc.KeyValueViewDocument();
        }
        private void MenuFlyoutItemToggleAsAdornment_Click(object sender, RoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
                foreach (var docView in SelectionManager.GetSelectedSiblings(this))
                {
                    docView.ViewModel.IsAdornmentGroup = !docView.ViewModel.IsAdornmentGroup;
                    SetZLayer();
                }
        }
        public void MenuFlyoutItemFitToParent_Click(object sender, RoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                var collectionView = this.GetFirstDescendantOfType<CollectionView>();
                if (collectionView != null)
                {
                    collectionView.ViewModel.ContainerDocument.SetFitToParent(!collectionView.ViewModel.ContainerDocument.GetFitToParent());
                    if (collectionView.ViewModel.ContainerDocument.GetFitToParent())
                        collectionView.ViewModel.FitContents(collectionView);
                }
            }
        }
        private void MenuFlyoutItemContext_Click(object sender, RoutedEventArgs e) { ShowContext(); }
        private void MenuFlyoutItemScreenCap_Click(object sender, RoutedEventArgs e) { Util.ExportAsImage(LayoutRoot); }
        private void MenuFlyoutItemOpen_OnClick(object sender, RoutedEventArgs e)
        {

            var docs = new List<ListController<DocumentController>>
            {
                MainPage.Instance.DockManager.DocController.GetDereferencedField<ListController<DocumentController>>(KeyStore.DockedDocumentsLeftKey, null)
            };

            using (UndoManager.GetBatchHandle())
            {
                var dockedView = this.GetFirstAncestorOfType<DockedView>();
                ViewModel.DocumentController.SetField<NumberController>(KeyStore.TextWrappingKey, (int)DashShared.TextWrapping.Wrap, true);
                if (dockedView != null)
                {
                    var toDock = ViewModel.DocumentController.GetViewCopy();
                    toDock.SetWidth(double.NaN);
                    toDock.SetHeight(double.NaN);
                    dockedView.ChangeView(new DocumentView() { DataContext = new DocumentViewModel(toDock) });
                }
                else
                {
                    MainPage.Instance.SetCurrentWorkspace(ViewModel.DocumentController);
                }
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
        private void MenuFlyoutLaunch_Click(object sender, RoutedEventArgs e)
        {
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
        
        public void This_Drop(object sender, DragEventArgs e)
        {
            if (this.ViewModel.IsAdornmentGroup)
                return;
            var dropDoc = ViewModel.DocumentController;
            if (KeyStore.RegionCreator[dropDoc.DocumentType] != null)
                dropDoc = KeyStore.RegionCreator[dropDoc.DocumentType](this);

            var dragModels = e.DataView.GetDragModels();
            foreach (var dragModel in dragModels)
            {
                if (!(dragModel is DragDocumentModel dm) || dm.DraggedDocumentViews == null) continue;

                var dragDocs = dm.DraggedDocuments;
                for (var index = 0; index < dragDocs.Count; index++)
                {
                    var dragDoc = dragDocs[index];
                    if (KeyStore.RegionCreator.TryGetValue(dragDoc.DocumentType, out var creatorFunc) && creatorFunc != null)
                        dragDoc = creatorFunc(dm.DraggedDocumentViews[index]);
                    //add link description to doc and if it isn't empty, have flag to show as popup when links followed
                    var linkDoc = dragDoc.Link(dropDoc, LinkBehavior.Annotate, dm.DraggedLinkType);
                    MainPage.Instance.AddFloatingDoc(linkDoc);
                    //dragDoc.Link(dropDoc, LinkContexts.None, dragModel.LinkType);
                    //TODO: ADD SUPPORT FOR MAINTAINING COLOR FOR LINK BUBBLES
                    dropDoc?.SetField(KeyStore.IsAnnotationScrollVisibleKey, new BoolController(true), true);
                }
            }
            e.AcceptedOperation = e.DataView.RequestedOperation == DataPackageOperation.None
                ? DataPackageOperation.Link
                : e.DataView.RequestedOperation;
            e.Handled = true;
        }

        void drop(bool footer, DocumentController newFieldDoc)
        {
            //xFooter.Visibility = xHeader.Visibility = Visibility.Collapsed;

            // newFieldDoc.SetField<NumberController>(KeyStore.HeightFieldKey, 30, true);
            newFieldDoc.SetWidth(double.NaN);
            newFieldDoc.SetPosition(new Point(100, 100));
            var activeLayout = ViewModel.LayoutDocument;
            if (activeLayout?.DocumentType.Equals(StackLayout.DocumentType) == true) // activeLayout is a stack
            {
                if (activeLayout.GetField(KeyStore.DataKey, true) == null)
                {
                    var fields = activeLayout
                        .GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null).TypedData
                        .ToArray().ToList();
                    if (!footer)
                        fields.Insert(0, newFieldDoc);
                    else fields.Add(newFieldDoc);
                    activeLayout.SetField(KeyStore.DataKey, new ListController<DocumentController>(fields), true);
                }
                else
                {
                    var listCtrl =
                        activeLayout.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);
                    if (!footer)
                        listCtrl.Insert(0, newFieldDoc);
                    else listCtrl.Add(newFieldDoc);
                }
            }
            else
            {
                var curLayout = activeLayout;
                if (ViewModel.DocumentController?.GetActiveLayout() != null
                ) // wrap existing activeLayout into a new StackPanel activeLayout
                {
                    curLayout.SetHorizontalAlignment(HorizontalAlignment.Stretch);
                    curLayout.SetVerticalAlignment(VerticalAlignment.Stretch);
                    curLayout.SetWidth(double.NaN);
                    curLayout.SetHeight(double.NaN);
                }
                else // need to create a stackPanel activeLayout and add the document to it
                {
                    curLayout =
                        activeLayout
                                .MakeCopy() as
                            DocumentController; // ViewModel's DocumentController is this activeLayout so we can't nest that or we get an infinite recursion
                    curLayout.Tag = "StackPanel DocView Layout";
                    curLayout.SetWidth(double.NaN);
                    curLayout.SetHeight(double.NaN);
                    curLayout.SetField(KeyStore.DocumentContextKey, ViewModel.DataDocument, true);
                }

                activeLayout = new StackLayout(new DocumentController[]
                    {footer ? curLayout : newFieldDoc, footer ? newFieldDoc : curLayout}).Document;
                activeLayout.Tag = "StackLayout";
                // we need to move the Height and Width fields from the current layout to the new active layout.
                // this is because we want any bindings that were made to the current layout to still fire when changes
                // are made to the new layout.
                activeLayout.SetField(KeyStore.PositionFieldKey, curLayout.GetField(KeyStore.PositionFieldKey), true);
                activeLayout.SetField(KeyStore.WidthFieldKey, curLayout.GetField(KeyStore.WidthFieldKey), true);
                activeLayout.SetField(KeyStore.HeightFieldKey, curLayout.GetField(KeyStore.HeightFieldKey), true);
                //activeLayout.SetPosition(ViewModel.Position);
                //activeLayout.SetWidth(ViewModel.ActualSize.X);
                //activeLayout.SetHeight(ViewModel.ActualSize.Y + 32);
                activeLayout.SetField(KeyStore.DocumentContextKey, ViewModel.DataDocument, true);
                ViewModel.DocumentController.SetField(KeyStore.ActiveLayoutKey, activeLayout, true);
            }
        }

        private void This_DragOver(object sender, DragEventArgs e)
        {
            ViewModel.DecorationState = ViewModel?.Undecorated == false;
        }

        public void This_DragLeave(object sender, DragEventArgs e)
        {
            //xFooter.Visibility = xHeader.Visibility = Visibility.Collapsed;
            ViewModel.DecorationState = false;
        }

        private void MenuFlyoutItemPin_Click(object sender, RoutedEventArgs e)
        {
            if (!Equals(MainPage.Instance.MainDocView))
                using (UndoManager.GetBatchHandle())
                {
                    MainPage.Instance.PinToPresentation(ViewModel.LayoutDocument);
                    if (ViewModel.LayoutDocument == null)
                    {
                        Debug.WriteLine("uh-oh");
                    }
                }
        }

        private void XAnnotateEllipseBorder_OnTapped_(object sender, TappedRoutedEventArgs e)
        {
            var ann = new AnnotationManager(this);
            ann.FollowRegion(ViewModel.DocumentController, this.GetAncestorsOfType<ILinkHandler>(), e.GetPosition(this));
        }
        
        private void AdjustEllipseSize(Ellipse ellipse, double length)
        {
            ellipse.Width = length;
            ellipse.Height = length;
        }

        private void MenuFlyoutItemApplyTemplate_Click(object sender, RoutedEventArgs e)
        {
            var applier = new TemplateApplier(ViewModel.LayoutDocument);
            _flyout.Content = applier;
            if (_flyout.IsInVisualTree())
            {
                _flyout.Hide();
            }
            else
            {
                _flyout.ShowAt(this);
            }
        }

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
                    FallbackValue = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Transparent)
                };
                xDocumentBackground.AddFieldBinding(Shape.FillProperty, backgroundBinding);
            }
        }

        private void ToggleQuickEntry()
        {
            if (_animationBusy || Equals(MainPage.Instance.MainDocView) || Equals(MainPage.Instance.xMapDocumentView)) return;

            _isQuickEntryOpen = !_isQuickEntryOpen;
            Storyboard animation = _isQuickEntryOpen ? xQuickEntryIn : xQuickEntryOut;

            if (animation == xQuickEntryIn) xKeyValueBorder.Width = double.NaN;

            _animationBusy = true;
            animation.Begin();
            animation.Completed += AnimationCompleted;

            void AnimationCompleted(object sender, object e)
            {
                animation.Completed -= AnimationCompleted;
                if (animation == xQuickEntryOut)
                {
                    xKeyValueBorder.Width = 0;
                    Focus(FocusState.Programmatic);
                }
                else
                {
                    xKeyBox.Focus(FocusState.Programmatic);
                }
                _animationBusy = false;
            }
        }

        private void KeyBoxOnEnter(KeyRoutedEventArgs obj)
        {
            obj.Handled = true;
            ProcessInput();
        }

        private void ValueBoxOnEnter(KeyRoutedEventArgs obj)
        {
            obj.Handled = true;
            using (UndoManager.GetBatchHandle())
            {
                ProcessInput();
            }

        }

        private void XValueBoxOnTextChanged(object sender1, TextChangedEventArgs e)
        {
            if (_articialChange)
            {
                _articialChange = false;
                return;
            }
            _lastValueInput = xValueBox.Text.Trim();
        }

        private void XKeyBoxOnTextChanged(object sender1, TextChangedEventArgs textChangedEventArgs)
        {
            var split = xKeyBox.Text.Split(".", StringSplitOptions.RemoveEmptyEntries);
            if (split == null || split.Length != 2) return;

            string docSpec = split[0];

            if (!(docSpec.Equals("d") || docSpec.Equals("v"))) return;

            DocumentController target = docSpec.Equals("d") ? ViewModel.DataDocument : ViewModel.LayoutDocument;
            string keyInput = split[1].Replace("_", " ");

            var val = target.GetDereferencedField(new KeyController(keyInput), null);
            if (val == null)
            {
                xValueBox.SelectionLength = 0;
                xValueBox.Text = "";
                return;
            }

            _articialChange = true;
            xValueBox.Text = val.GetValue(null).ToString();

            if (double.TryParse(xValueBox.Text.Trim(), out double res))
            {
                xValueBox.Text = "=" + xValueBox.Text;
                xValueBox.SelectionStart = 1;
                xValueBox.SelectionLength = xValueBox.Text.Length - 1;
            }
            else
            {
                xValueBox.SelectAll();
            }
        }

        private void XValueBoxOnGotFocus(object sender1, RoutedEventArgs routedEventArgs)
        {
            if (xValueBox.Text.StartsWith("="))
            {
                xValueBox.SelectionStart = 1;
                xValueBox.SelectionLength = xValueBox.Text.Length - 1;
            }
            else
            {
                xValueBox.SelectAll();
            }
        }

        private void ProcessInput()
        {
            string rawKeyText = xKeyBox.Text;
            string rawValueText = xValueBox.Text;

            var emptyKeyFailure = false;
            var emptyValueFailure = false;

            if (string.IsNullOrEmpty(rawKeyText))
            {
                xKeyEditFailure.Begin();
                emptyKeyFailure = true;
            }
            if (string.IsNullOrEmpty(rawValueText))
            {
                xValueEditFailure.Begin();
                emptyValueFailure = true;
            }

            if (emptyKeyFailure || emptyValueFailure) return;

            var components = rawKeyText.Split(".", StringSplitOptions.RemoveEmptyEntries);
            string docSpec = components[0].ToLower();

            if (components.Length != 2 || !(docSpec.Equals("v") || docSpec.Equals("d")))
            {
                xKeyEditFailure.Begin();
                return;
            }

            FieldControllerBase computedValue = DSL.InterpretUserInput(rawValueText, true);
            DocumentController target = docSpec.Equals("d") ? ViewModel.DataDocument : ViewModel.LayoutDocument;
            if (computedValue is DocumentController doc && doc.DocumentType.Equals(DashConstants.TypeStore.ErrorType))
            {
                computedValue = new TextController(xValueBox.Text.Trim());
                xValueErrorFailure.Begin();
            }

            string key = components[1].Replace("_", " ");

            target.SetField(new KeyController(key), computedValue, true);

            _mostRecentPrefix = xKeyBox.Text.Substring(0, 2);
            xKeyEditSuccess.Begin();
            xValueEditSuccess.Begin();

            ClearQuickEntryBoxes();
        }

        private void SetFocusToKeyBox(object sender1, object o2)
        {
            xKeyBox.Text = _mostRecentPrefix;
            xKeyBox.SelectionStart = 2;
            xKeyBox.Focus(FocusState.Keyboard);
        }

        private void MenuFlyoutItemHide_Click(object sender, RoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                ViewModel.LayoutDocument.SetHidden(true);
            }
        }
        
        public void SetLinkBorderColor()
        {
            MainPage.Instance.HighlightDoc(ViewModel.DocumentController, null, 1, true);
        }

        public void SetActivationMode(bool onoff)
        {
            this.xActivationMode.Visibility = onoff ? Visibility.Visible : Visibility.Collapsed;
        }

        public void RemoveLinkBorderColor()
        {
            MainPage.Instance.HighlightDoc(ViewModel.DocumentController, null, 2, true);
            xToYellow.Begin();
        }
        ~DocumentView()
        {
            Debug.Write("dispose DocumentView");
        }

        private void xActivationMode_Tapped(object sender, TappedRoutedEventArgs e)
        {
            SetActivationMode(this.xActivationMode.Visibility == Visibility.Collapsed);
        }
    }
}
