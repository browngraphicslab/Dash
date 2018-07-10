using DashShared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
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
using Visibility = Windows.UI.Xaml.Visibility;
using Dash.Models.DragModels;
using Dash.Views;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236
namespace Dash
{
    public sealed partial class DocumentView
    {
        public delegate void DocumentViewSelectedHandler(DocumentView sender, DocumentViewSelectedEventArgs args);
        public delegate void DocumentDeletedHandler(DocumentView sender, DocumentViewDeletedEventArgs args);
        public event DocumentViewSelectedHandler DocumentSelected;
        public event DocumentDeletedHandler DocumentDeleted;
        public CollectionView ParentCollection => this.GetFirstAncestorOfType<CollectionView>();

        /// <summary>
        /// Contains methods which allow the document to be moved around a free form canvass
        /// </summary>
        public ManipulationControls ManipulationControls { get; set; }

        public DocumentViewModel ViewModel
        {
            get => DataContext as DocumentViewModel;
            set => DataContext = value;
        }

        public MenuFlyout MenuFlyout { get; set; }

        static readonly SolidColorBrush SingleSelectionBorderColor = new SolidColorBrush(Colors.LightGray);
        static readonly SolidColorBrush GroupSelectionBorderColor = new SolidColorBrush(Colors.LightBlue);

        static DocumentView _focusedDocument;
        // the document that has input focus (logically similar to keyboard focus but different since Images, etc can't be keyboard focused).
        static public DocumentView FocusedDocument
        {
            get => _focusedDocument;
            set => _focusedDocument = value;
        }

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

        private DocumentController _templateEditor;
       

        public static readonly DependencyProperty BindRenderTransformProperty = DependencyProperty.Register(
            "BindRenderTransform", typeof(bool), typeof(DocumentView), new PropertyMetadata(default(bool)));

        public bool BindRenderTransform
        {
            get { return (bool)GetValue(BindRenderTransformProperty); }
            set { SetValue(BindRenderTransformProperty, value); }
        }

        public static readonly DependencyProperty StandardViewLevelProperty = DependencyProperty.Register(
            "StandardViewLevel", typeof(CollectionViewModel.StandardViewLevel), typeof(DocumentView), new PropertyMetadata(CollectionViewModel.StandardViewLevel.None, StandardViewLevelChanged));


        public CollectionViewModel.StandardViewLevel StandardViewLevel
        {
            get { return (CollectionViewModel.StandardViewLevel) GetValue(StandardViewLevelProperty); }
            set
            {
                SetValue(StandardViewLevelProperty, value);
            }
        }

        private ImageSource DocPreview = null;
        private Flyout _flyout;
        private double _width;
        private double _height;

        // == CONSTRUCTORs ==

        public DocumentView()
        {
            InitializeComponent();

            _flyout = new Flyout {Placement = FlyoutPlacementMode.Right};

            Util.InitializeDropShadow(xShadowHost, xDocumentBackground);

            // set bounds
            MinWidth = 35;
            MinHeight = 35;

            RegisterPropertyChangedCallback(BindRenderTransformProperty, updateBindings);

            void updateBindings(object sender, DependencyProperty dp)
            {
                var doc = ViewModel?.LayoutDocument;

                var binding = !BindRenderTransform || doc == null ? null :
                        new FieldMultiBinding<MatrixTransform>(new DocumentFieldReference(doc, KeyStore.PositionFieldKey),
                                                               new DocumentFieldReference(doc, KeyStore.ScaleAmountFieldKey))
                        {
                            Converter = new TransformGroupMultiConverter(),
                            Context = new Context(doc),
                            Mode = BindingMode.OneWay,
                            Tag = "RenderTransform multi binding in DocumentView"
                        };
                this.AddFieldBinding(RenderTransformProperty, binding);

                _templateEditor = ViewModel?.DataDocument.GetField<DocumentController>(KeyStore.TemplateEditorKey);
              
            }

            void sizeChangedHandler(object sender, SizeChangedEventArgs e)
            {
                ViewModel?.LayoutDocument.SetActualSize(new Point(ActualWidth, ActualHeight));
                PositionContextPreview();
            }
            Loaded += (sender, e) =>
            {
                updateBindings(null, null);
                DataContextChanged += (s, a) => updateBindings(null, null);

                SizeChanged += sizeChangedHandler;
                ViewModel?.LayoutDocument.SetActualSize(new Point(ActualWidth, ActualHeight));
                //if (ViewModel?.DataDocument.GetField(KeyStore.TemplateEditorKey) != null)
                //{
                //    ViewModel?.DataDocument.RemoveField(KeyStore.TemplateEditorKey);
                //}
                SetZLayer();

                var type = ViewModel?.DocumentController.GetDereferencedField(KeyStore.DataKey, null)?.TypeInfo;

                switch (type)
                {
                    case TypeInfo.Image:
                        xTitleIcon.Text = Application.Current.Resources["ImageDocumentIcon"] as string;
                        break;
                    case TypeInfo.Audio:
                        xTitleIcon.Text = Application.Current.Resources["AudioDocumentIcon"] as string;
                        break;
                    case TypeInfo.Video:
                        xTitleIcon.Text = Application.Current.Resources["VideoDocumentIcon"] as string;
                        break;
                    case TypeInfo.RichText:
                    case TypeInfo.Text:
                        xTitleIcon.Text = Application.Current.Resources["TextIcon"] as string;
                        break;
                    case TypeInfo.Document:
                        xTitleIcon.Text = Application.Current.Resources["DocumentPlainIcon"] as string;
                        break;
                    default:
                        xTitleIcon.Text = Application.Current.Resources["DefaultIcon"] as string;
                        break;

                }
            };
            Unloaded += (sender, e) => SizeChanged -= sizeChangedHandler;

            PointerPressed += (sender, e) =>
            {
                DocumentSelected?.Invoke(this, new DocumentViewSelectedEventArgs());
                var right = (e.GetCurrentPoint(this).Properties.IsRightButtonPressed || MenuToolbar.Instance.GetMouseMode() == MenuToolbar.MouseMode.PanFast);
                var parentFreeform = this.GetFirstAncestorOfType<CollectionFreeformBase>();
                var parentParentFreeform = parentFreeform?.GetFirstAncestorOfType<CollectionFreeformBase>();
                ManipulationMode = right && parentFreeform != null && (this.IsShiftPressed() || parentParentFreeform == null) ? ManipulationModes.All : ManipulationModes.None;
                MainPage.Instance.Focus(FocusState.Programmatic);
                e.Handled = ManipulationMode != ManipulationModes.None;
            };

            PointerEntered += DocumentView_PointerEntered;
            PointerExited += DocumentView_PointerExited;
            RightTapped += (s, e) => DocumentView_OnTapped(null, null);
            AddHandler(TappedEvent, new TappedEventHandler(DocumentView_OnTapped), true);  // RichText and other controls handle Tapped events

            // setup ResizeHandles
            void ResizeHandles_OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
            {
                UndoManager.StartBatch();

                MainPage.Instance.Focus(FocusState.Programmatic);
                if (!this.IsRightBtnPressed()) // ignore right button drags
                {
                    MainPage.Instance.GetDescendantsOfType<PdfView>().ToList().ForEach((p) => p.Freeze());
                    PointerExited -= DocumentView_PointerExited;// ignore any pointer exit events which will change the visibility of the dragger
                    e.Handled = true;
                }
            }
            void ResizeHandles_restorePointerTracking()
            {
                if (StandardViewLevel.Equals(CollectionViewModel.StandardViewLevel.None) || StandardViewLevel.Equals(CollectionViewModel.StandardViewLevel.Detail))
                    ViewModel.DecorationState = ResizeHandleBottomRight.IsPointerOver();
                PointerExited -= DocumentView_PointerExited;
                PointerExited += DocumentView_PointerExited;

            };
            void ResizeHandles_OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
            {
                ResizeHandles_restorePointerTracking();
                MainPage.Instance.GetDescendantsOfType<PdfView>().ToList().ForEach((p) => p.UnFreeze());
                e.Handled = true;

                UndoManager.EndBatch();
            }
            ResizeHandleTopLeft.ManipulationDelta += (s, e) => Resize(s as FrameworkElement, e, true, true);
            ResizeHandleTopRight.ManipulationDelta += (s, e) => Resize(s as FrameworkElement, e, true, false);
            ResizeHandleBottomLeft.ManipulationDelta += (s, e) => Resize(s as FrameworkElement, e, false, true);
            ResizeHandleBottomRight.ManipulationDelta += (s, e) => Resize(s as FrameworkElement, e, false, false);

            foreach (var handle in new Ellipse[] { ResizeHandleBottomLeft, ResizeHandleBottomRight, ResizeHandleTopLeft, ResizeHandleTopRight })
            {
                handle.ManipulationStarted += ResizeHandles_OnManipulationStarted;
                handle.ManipulationCompleted += ResizeHandles_OnManipulationCompleted;
                handle.PointerReleased += (s, e) => ResizeHandles_restorePointerTracking();
                handle.PointerPressed += (s, e) =>
         {
             CapturePointer(e.Pointer);
             ManipulationMode = ManipulationModes.None;
             e.Handled = !e.GetCurrentPoint(this).Properties.IsRightButtonPressed;
         };
            }

            // setup OperatorEllipse 
            OperatorEllipseHighlight.PointerExited += (sender, e) => OperatorEllipseHighlight.Visibility = Visibility.Collapsed;
            OperatorEllipseUnhighlight.PointerEntered += (sender, e) => OperatorEllipseHighlight.Visibility = Visibility.Visible;
            xOperatorEllipseBorder.PointerPressed += (sender, e) =>
            {
                this.ManipulationMode = ManipulationModes.None;
                e.Handled = !e.GetCurrentPoint(this).Properties.IsRightButtonPressed;
            };
            xOperatorEllipseBorder.PointerReleased += (sender, e) => ManipulationMode = ManipulationModes.All;
            xOperatorEllipseBorder.DragStarting += (sender, args) =>
            {
                //var selected = (ParentCollection.CurrentView as CollectionFreeformBase)?.SelectedDocs.Select((dv) => dv.ViewModel.DocumentController);
                //if (selected?.Count() > 0)
                //{
                //    args.Data.Properties[nameof(List<DragDocumentModel>)] =
                //            new List<DragDocumentModel>(selected.Select((s) => new DragDocumentModel(s, true)));
                //}
                //else
                args.Data.Properties[nameof(DragDocumentModel)] = new DragDocumentModel(ViewModel.DocumentController, false);
                args.AllowedOperations = DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;
                args.Data.RequestedOperation = DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
                ViewModel.DecorationState = false;
            };

            // setup LinkEllipse
            AnnotateEllipseHighlight.PointerExited += (sender, e) => AnnotateEllipseHighlight.Visibility = Visibility.Collapsed;
            AnnotateEllipseUnhighlight.PointerEntered += (sender, e) => AnnotateEllipseHighlight.Visibility = Visibility.Visible;
            xAnnotateEllipseBorder.PointerPressed += (sender, e) =>
            {
                this.ManipulationMode = ManipulationModes.None;
                e.Handled = !e.GetCurrentPoint(this).Properties.IsRightButtonPressed;
            };
            xAnnotateEllipseBorder.PointerReleased += (sender, e) => ManipulationMode = ManipulationModes.All;
            xAnnotateEllipseBorder.DragStarting += (sender, args) =>
            {
                args.Data.Properties[nameof(DragDocumentModel)] = new DragDocumentModel(ViewModel.DocumentController, false, this);
                args.AllowedOperations = DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;
                args.Data.RequestedOperation = DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
                ViewModel.DecorationState = false;
            };

            // setup EditorEllipse
            TemplateEditorEllipseBorderHighlight.PointerExited += (sender, e) => TemplateEditorEllipseBorderHighlight.Visibility = Visibility.Collapsed;
            TemplateEditorEllipseBorderUnhighlight.PointerEntered += (sender, e) => TemplateEditorEllipseBorderHighlight.Visibility = Visibility.Visible;
            xTemplateEditorEllipseBorder.PointerPressed += (sender, e) =>
            {
                this.ManipulationMode = ManipulationModes.None;
                ToggleTemplateEditor();
                e.Handled = !e.GetCurrentPoint(this).Properties.IsRightButtonPressed;
            };
            xTemplateEditorEllipseBorder.PointerReleased += (sender, e) => ManipulationMode = ManipulationModes.All;
            xTemplateEditorEllipseBorder.DragStarting += (sender, args) =>
            {
            };

            // setup Title Icon
            xTitleIcon.PointerPressed += (sender, e) =>
            {
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
            xContextTitle.SizeChanged += (sender, e) => Canvas.SetLeft(xContextTitle, -xContextTitle.ActualWidth - 1);

            // add manipulation code
            ManipulationControls = new ManipulationControls(this);
            ManipulationControls.OnManipulatorTranslatedOrScaled += (delta) => SelectionManager.GetSelectedSiblings(this).ForEach((d) => d.TransformDelta(delta));
            ManipulationControls.OnManipulatorStarted += () =>
            {

                var wasSelected = this.xTargetBorder.BorderThickness.Left > 0;

                // get all BackgroundBox types selected initially, and add the documents they contain to selected documents list 
                var adornmentGroups = SelectionManager.GetSelectedSiblings(this).Where((dv) => dv.ViewModel.IsAdornmentGroup).ToList();
                if (!wasSelected && ParentCollection?.CurrentView is CollectionFreeformBase cview)
                {
                    adornmentGroups.ForEach((dv) =>
                    {
                        SelectionManager.SelectDocuments(cview.DocsInMarquee(new Rect(dv.ViewModel.Position, new Size(dv.ActualWidth, dv.ActualHeight))));
                    });
                    SetSelectionBorder(false);
                }
                // initialize the cached values of position and scale for each manipulated document  
                SelectionManager.GetSelectedSiblings(this).ForEach((d) =>
                {
                    d.ViewModel.InteractiveManipulationPosition = d.ViewModel.Position;
                    d.ViewModel.InteractiveManipulationScale = d.ViewModel.Scale;
                });
            };
            ManipulationControls.OnManipulatorCompleted += () =>
            {
                using (UndoManager.GetBatchHandle())
                {
                    SelectionManager.GetSelectedSiblings(this).ForEach((d) =>
                    {
                        d.ViewModel.DecorationState = d.IsPointerOver();
                        d.ViewModel.Position =
                            d.ViewModel
                                .InteractiveManipulationPosition; // write the cached values of position and scale back to the viewModel
                        d.ViewModel.Scale = d.ViewModel.InteractiveManipulationScale;
                    });
                    var wasSelected = this.xTargetBorder.BorderThickness.Left > 0;
                    if (ViewModel.IsAdornmentGroup && !wasSelected)
                    {
                        if (ParentCollection.CurrentView is CollectionFreeformView)
                        {
                            SelectionManager.DeselectAll();
                        }
                    }
                }
            };

            MenuFlyout = xMenuFlyout;

            MenuFlyout.Opened += (s, e) =>
            {
                if (this.IsShiftPressed())
                    MenuFlyout.Hide();
            };

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

                _templateEditor = new TemplateEditorBox(ViewModel.LayoutDocument, where, new Size(1000, 540)).Document;
	           
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

        #region StandardCollectionView

        private async void GetDocPreview()
        {
            //if (ViewModel.DocumentController.DocumentType.Equals(PdfBox.DocumentType))
            //{
            //    var pdfBox = xContentPresenter.GetFirstDescendantOfType<SfPdfViewerControl>();
            //    var img = pdfBox.GetPage(0);
            //    DocPreview = img.Source;
            //}
            xIconBorder.BorderThickness = new Thickness(1);
            xIconBorder.Background = new SolidColorBrush(Colors.WhiteSmoke)
            {
                Opacity = 0.5
            };
            var type = ViewModel.DocumentController.DocumentType;
            WebViewBrush webBrush = null;
            WebBoxView web = null;
            xSmallIconImage.Visibility = Visibility.Visible;
            xSmallIconImage.Source = GetTypeIcon();
            if (DocPreview == null)
                DocPreview = await this.GetPreview();
            xIconImage.Source = DocPreview ?? new BitmapImage(new Uri("ms-appx:///Assets/Icons/Unavailable.png"));
            OpenIcon();

        }

        public async Task<RenderTargetBitmap> GetPreview()
        {
            RenderTargetBitmap bitmap = new RenderTargetBitmap();
            xContentPresenter.Visibility = Visibility.Visible;
            await bitmap.RenderAsync(xContentPresenter.Content as FrameworkElement);
            xContentPresenter.Visibility = Visibility.Collapsed;
            return bitmap;
        }

        private void CloseDocPreview()
        {
            xIconImage.Visibility = Visibility.Visible;
            xSmallIconImage.Visibility = Visibility.Collapsed;
            xIconBorder.BorderThickness = new Thickness(0);
            xIconBorder.Background = new SolidColorBrush(Colors.Transparent);
        }

        private static void StandardViewLevelChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var view = obj as DocumentView;
            view?.UpdateView();
        }

        private void OpenIcon()
        {
            xIcon.Visibility = Visibility.Visible;
            xContentPresenter.Visibility = Visibility.Collapsed;
        }

        private void OpenFreeform()
        {
            xContentPresenter.Visibility = Visibility.Visible;
            xIcon.Visibility = Visibility.Collapsed;
        }

        BitmapImage GetTypeIcon()
        {
            var type = ViewModel.DocumentController.DocumentType;
            // TODO: make icons for different types
            if (type.Equals(CollectionBox.DocumentType))
            {
                return new BitmapImage(new Uri("ms-appx:///Assets/Icons/col-icon.png"));
            }
            else if (type.Equals(PdfBox.DocumentType))
            {
                return new BitmapImage(new Uri("ms-appx:///Assets/Icons/pdf-icon.png"));
            }
            else if (type.Equals(RichTextBox.DocumentType))
            {
                return new BitmapImage(new Uri("ms-appx:///Assets/Icons/rtf-icon.png"));
            }
            else if (type.Equals(VideoBox.DocumentType))
            {
                return new BitmapImage(new Uri("ms-appx:///Assets/Icons/vid-icon.png"));
            }
            else if (type.Equals(ImageBox.DocumentType))
            {
                return new BitmapImage(new Uri("ms-appx:///Assets/Icons/img-icon.png"));
            }
            else if (type.Equals(WebBox.DocumentType))
            {
                return new BitmapImage(new Uri("ms-appx:///Assets/Icons/html-icon.png"));
            }
            else if (type.Equals(ApiOperatorBox.DocumentType))
            {
                return new BitmapImage(new Uri("ms-appx:///Assets/Icons/api-icon.png"));
            }
            else if (type.Equals(DataBox.DocumentType))
            {
                return new BitmapImage(new Uri("ms-appx:///Assets/Icons/data-icon.png"));
            } else if (type.Equals(OperatorBox.DocumentType))
            {
                return new BitmapImage(new Uri("ms-appx:///Assets/Icons/opr-icon.png"));
            }
            else
            {
                return new BitmapImage(new Uri("ms-appx:///Assets/Icons/doc-icon.png"));
            }
        }

        private async void UpdateView()
        {
            switch (StandardViewLevel)
            {
                case CollectionViewModel.StandardViewLevel.Detail:
                    if (DocPreview == null)
                        DocPreview = await GetPreview();
                    CloseDocPreview();
                    OpenFreeform();
                    break;
                case CollectionViewModel.StandardViewLevel.Region:
                    GetDocPreview();
                    break;
                case CollectionViewModel.StandardViewLevel.Overview:
                    CloseDocPreview();
                    OpenIcon();
                    xIconImage.Source = GetTypeIcon();
                    break;
            }
        }
        #endregion


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

        public RectangleGeometry Bounds { get; set; }

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

            if (Bounds != null && (!Bounds.Rect.Contains(translate) || !Bounds.Rect.Contains(new Point(translate.X + ActualWidth, translate.Y + ActualHeight))))
            {
                return;
            }
            
            ViewModel.InteractiveManipulationPosition = translate;
            ViewModel.InteractiveManipulationScale = scaleAmount;
            RenderTransform = TransformGroupMultiConverter.ConvertDataToXamlHelper(new List<object> { translate, scaleAmount });
        }

        public void TransformDelta(Point moveTo)
        {
            var scaleAmount = new Point(ViewModel.InteractiveManipulationScale.X, ViewModel.InteractiveManipulationScale.Y);

            ViewModel.InteractiveManipulationPosition = moveTo;
            RenderTransform =
                TransformGroupMultiConverter.ConvertDataToXamlHelper(new List<object> { moveTo, scaleAmount });
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

            if (this.IsShiftPressed() && !e.VirtualKey.Equals(VirtualKey.Shift))
            {
                var focusedEle = (FocusManager.GetFocusedElement() as FrameworkElement);
                var docView = focusedEle?.GetFirstAncestorOfType<DocumentView>();
                var focused = docView == this;

                if (ViewModel != null && focused && e.VirtualKey.Equals(VirtualKey.Enter)) // shift + Enter
                {
                    // don't shift enter on KeyValue documents (since they already display the key/value adding)
                    if (!ViewModel.LayoutDocument.DocumentType.Equals(KeyValueDocumentBox.DocumentType) &&
                        !ViewModel.DocumentController.DocumentType.Equals(DashConstants.TypeStore.MainDocumentType))
                        HandleShiftEnter();
                }
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
                void OnSelectedSchemaRowUpdated(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args, Context context1)
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
                ViewModel.DocumentController.GetDataDocument().SetTitle(title);
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
        /// <param name="shiftTop"></param>
        /// <param name="shiftLeft"></param>
        public void Resize(FrameworkElement sender, ManipulationDeltaRoutedEventArgs e, bool shiftTop, bool shiftLeft)
        {
            if (this.IsRightBtnPressed())
                return; // let the manipulation fall through to an ancestor when Rightbutton dragging


            e.Handled = true;
            var delta = Util.DeltaTransformFromVisual(e.Delta.Translation, sender as FrameworkElement);
            var oldSize = new Size(ViewModel.ActualSize.X, ViewModel.ActualSize.Y);
            var origSize = new Size(ViewModel.Width, ViewModel.Height);
            var origPos  = ViewModel.Position;

            // sets directions/weights depending on which handle was dragged as mathematical manipulations
            var cursorXDirection = shiftLeft ? -1 : 1;
            var cursorYDirection = shiftTop ? -1 : 1;
            var moveXScale = shiftLeft ? 1 : 0;
            var moveYScale = shiftTop ? 1 : 0;

            // clamp the drag position to the available Bounds
            if (Bounds != null)
            {
                var width   = double.IsNaN(ViewModel.Width) ? ViewModel.ActualSize.X : ViewModel.Width;
                var height  = double.IsNaN(ViewModel.Height) ? ViewModel.ActualSize.Y : ViewModel.Height;
                var pos     = new Point(ViewModel.XPos + width * (1 - moveXScale), ViewModel.YPos + height * (1 - moveYScale));
                if (!Bounds.Rect.Contains((new Point(pos.X + delta.X, pos.Y + delta.Y))))
                    return;
                var clamped = Clamp(new Point(pos.X + delta.X, pos.Y + delta.Y), Bounds.Rect);
                delta = new Point(clamped.X - pos.X, clamped.Y - pos.Y);
            }

            // if Height is NaN but width isn't, then we want to keep Height as NaN and just change width.  This happens for some images to coerce proportional scaling.
            var w = double.IsNaN(ViewModel.Width) ? ViewModel.ActualSize.X : ViewModel.Width;
            var h = ViewModel.Height;

            // significance of the direction weightings: if the left handles are dragged to the left, should resize larger instead of smaller as p.X would say. 
            // So flip the negative sign by multiplying by -1.
            var aspect = ViewModel.ActualSize.Y / ViewModel.ActualSize.X;
            var diffX = cursorXDirection * delta.X;
            var diffY = (this.IsCtrlPressed() || this.IsShiftPressed()) ? diffX : cursorYDirection * delta.Y; // proportional resizing if Shift or Ctrl is presssed
            var newSize = new Size(Math.Max(w + diffX, MinWidth), Math.Max(h + aspect * diffY, MinHeight));

            // test for changes to height based on changes to width (eg. images to maintain aspect, text boxes that wrap
            ViewModel.Width = newSize.Width;
            ViewModel.Height = newSize.Height;
            this.UpdateLayout(); // bcz: text boxes seem to need the ActualWidth/Height set to measure properly
            this.Measure(new Size(newSize.Width, 5000));

            // set the position of the doc based on how much it resized (if Top and/or Left is being dragged)
            var newPos = new Point(
                ViewModel.XPos - moveXScale * (newSize.Width      - oldSize.Width)  * ViewModel.Scale.X,
                ViewModel.YPos - moveYScale * (DesiredSize.Height - oldSize.Height) * ViewModel.Scale.Y);
            // re-clamp the position to keep it in bounds
            if (Bounds != null)
            {
                if (!Bounds.Rect.Contains(newPos) ||
                    !Bounds.Rect.Contains(new Point(newPos.X + newSize.Width, newPos.Y + DesiredSize.Height)))
                {
                    ViewModel.Position = origPos;
                    ViewModel.Width = origSize.Width;
                    ViewModel.Height = origSize.Height;
                    return;
                }
                var clamp = Clamp(newPos, Bounds.Rect);
                newSize.Width += newPos.X - clamp.X;
                newSize.Height += newPos.Y - clamp.Y;
                newPos = clamp;
                var br = Clamp(new Point(newPos.X + newSize.Width, newPos.Y + newSize.Height), Bounds.Rect);
                newSize = new Size(br.X - newPos.X, br.Y - newPos.Y);
            }

            ViewModel.Position = newPos;
            ViewModel.Width = newSize.Width;
            ViewModel.Height = newSize.Height;

            Point Clamp(Point point, Rect rect)
            {
                if (point.X < rect.Left)
                {
                    point.X = rect.Left;
                }
                else if (point.X > rect.Right)
                {
                    point.X = rect.Right;
                }

                if (point.Y < rect.Top)
                {
                    point.Y = rect.Top;
                }
                else if (point.Y > rect.Bottom)
                {
                    point.Y = rect.Bottom;
                }

                return point;
            }
        }

        // Controls functionality for the Right-click context menu
        #region Menu

        /// <summary>
        /// Brings the element to the front of its containing parent canvas.
        /// </summary>
        public void ToFront()
        {
            if (ParentCollection == null || ViewModel?.IsAdornmentGroup == true)
                return;
            ParentCollection.MaxZ += 1;
            Canvas.SetZIndex(this.GetFirstAncestorOfType<ContentPresenter>(), ParentCollection.MaxZ);
        }

        /// <summary>
        /// Ensures the menu flyout is shown on right tap.
        /// </summary>
        public void ForceRightTapContextMenu()
        {
            MenuFlyout.ShowAt(this, MainPage.Instance.TransformToVisual(this).TransformPoint(this.RootPointerPos()));
        }

        /// <summary>
        /// Ensures the menu flyout is shown on right tap.
        /// </summary>
        public void ForceLeftTapped()
        {
            this.DocumentView_OnTapped(null, null);
        }

        /// <summary>
        /// Deletes the document from the view.
        /// </summary>
        /// <param name="addTextBox"></param>
        public void DeleteDocument(bool addTextBox = false)
        {
            if (ParentCollection != null)
            {
                FadeOut.Begin();
                _templateEditor?.SetHidden(true);

                if (addTextBox)
                {
                    (ParentCollection.CurrentView as CollectionFreeformBase)?.RenderPreviewTextbox(ViewModel.Position);
                }
                SelectionManager.Deselect(this);
            }
        }

        /// <summary>
        /// Copies the Document.
        /// </summary>
        public void CopyDocument()
        {
            UndoManager.StartBatch();
            // will this screw things up?
            Canvas.SetZIndex(this.GetFirstAncestorOfType<ContentPresenter>(), 0);
            var doc = ViewModel.DocumentController.GetCopy(null);
            ParentCollection?.ViewModel.AddDocument(doc);
            UndoManager.EndBatch();
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
           
          
            DocumentDeleted?.Invoke(this, new DocumentViewDeletedEventArgs());
        }

        #endregion

        #region Activation

        public void SetSelectionBorder(bool selected)
        {
            xTargetBorder.BorderThickness = selected ? new Thickness(3) : new Thickness(0);
            xTargetBorder.Margin = selected ? new Thickness(-3) : new Thickness(0);
            xTargetBorder.BorderBrush = selected ? GroupSelectionBorderColor : new SolidColorBrush(Colors.Transparent);
        }

        #endregion
        public void DocumentView_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            FocusedDocument = this;
            DocumentSelected?.Invoke(this, new DocumentViewSelectedEventArgs());
            //TODO Have more standard way of selecting groups/getting selection of groups to the toolbar
            if (!ViewModel.IsAdornmentGroup)
            {
                ToFront();
            }
            if (ParentCollection?.CurrentView is CollectionFreeformBase cfview && (e == null || !e.Handled))
            {
                if (this.IsShiftPressed())
                {
                    SelectionManager.ToggleSelection(this);
                }
                else
                {
                    SelectionManager.DeselectAll();
                    SelectionManager.Select(this);
                }
                if (SelectionManager.SelectedDocs.Count() > 1 && this.IsShiftPressed())
                {
                    cfview.Focus(FocusState.Programmatic); // move focus to container if multiple documents are selected, otherwise allow keyboard focus to remain where it was
                }

                //TODO this should always be handled but OnTapped is sometimes called from righttapped with null event
                if (e != null)
                {
                    e.Handled = true;
                }
            }
        }

        public void DocumentView_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (StandardViewLevel.Equals(CollectionViewModel.StandardViewLevel.None) || StandardViewLevel.Equals(CollectionViewModel.StandardViewLevel.Detail))
            {
                if (e == null || (!e.GetCurrentPoint(this).Properties.IsRightButtonPressed && !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed))
                    ViewModel.DecorationState = false;
            }
            MainPage.Instance.HighlightTreeView(ViewModel.DocumentController, false);
        }
        public void DocumentView_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            DocumentView_PointerEntered();
        }

        public void DocumentView_PointerEntered()
        {
            if (StandardViewLevel.Equals(CollectionViewModel.StandardViewLevel.None) || StandardViewLevel.Equals(CollectionViewModel.StandardViewLevel.Detail))
                ViewModel.DecorationState = ViewModel?.Undecorated == false;
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 1);
            MainPage.Instance.HighlightTreeView(ViewModel.DocumentController, true);
        }

        /// <summary>
        /// Encompasses the different type of events triggers by changing document data.
        /// </summary>
        public class DocumentViewSelectedEventArgs
        {
            public DocumentViewSelectedEventArgs()
            {
            }
        }

        /// <summary>
        /// Encompasses the different type of events triggers by changing document data.
        /// </summary>
        public class DocumentViewDeletedEventArgs
        {
            public DocumentViewDeletedEventArgs()
            {
            }
        }

        #region UtilityFuncions

        public CollectionView GetCollectionToMoveTo(List<DocumentView> overlappedViews)
        {
            var selectedDocs = SelectionManager.GetSelectedSiblings(this);
            var collection = this.GetFirstAncestorOfType<CollectionView>();

            if (collection == null || ViewModel == null || selectedDocs == null)
                return null;

            foreach (var nestedDocument in overlappedViews)
            {
                var nestedCollection = nestedDocument.GetFirstDescendantOfType<CollectionView>();
                if (nestedCollection != null && !nestedCollection.GetAncestors().ToList().Contains(this))
                {
                    if (!nestedCollection.Equals(collection))
                    {
                        return nestedCollection;
                    }

                    return null;
                }
            }

            return null;
        }

        public bool MoveToContainingCollection(List<DocumentView> overlappedViews)
        {
            var selectedDocs = SelectionManager.GetSelectedSiblings(this);

            var collection = this.GetFirstAncestorOfType<CollectionView>();
            var nestedCollection = GetCollectionToMoveTo(overlappedViews);

            if (nestedCollection == null)
            {
                return false;
            }

            foreach (var selDoc in selectedDocs)
            {
                var pos = selDoc.TransformToVisual(MainPage.Instance).TransformPoint(new Point());
                var where = nestedCollection.CurrentView is CollectionFreeformBase ?
                    Util.GetCollectionFreeFormPoint((nestedCollection.CurrentView as CollectionFreeformBase), pos) :
                    new Point();
                collection.ViewModel.RemoveDocument(selDoc.ViewModel.DocumentController);
                nestedCollection.ViewModel.AddDocument(selDoc.ViewModel.DocumentController.GetSameCopy(where));
            }
            return true;
        }

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
        public void HandleCtrlEnter()
        {
            var collection = this.GetFirstAncestorOfType<CollectionFreeformBase>();
            var docCanvas = this.GetFirstAncestorOfType<Canvas>();
            if (collection == null) return;
            var where = this.TransformToVisual(docCanvas).TransformPoint(new Point(0, ActualHeight + 1));
            var dtext = this.ViewModel.DataDocument.GetDereferencedField<TextController>(KeyStore.DocumentTextKey, null)?.Data ?? "";
            collection.LoadNewDataBox(dtext, where, true);
        }

        #endregion
        #region Context menu click handlers

        private void MenuFlyoutItemCopy_Click(object sender, RoutedEventArgs e)
        {
            foreach (var doc in SelectionManager.GetSelectedSiblings(this))
                doc.CopyDocument();
        }
        private void MenuFlyoutItemAlias_Click(object sender, RoutedEventArgs e)
        {
            foreach (var doc in SelectionManager.GetSelectedSiblings(this))
                doc.CopyViewDocument();
        }
        private void MenuFlyoutItemDelete_Click(object sender, RoutedEventArgs e)
        {
            foreach (var doc in SelectionManager.GetSelectedSiblings(this))
                doc.DeleteDocument();
        }
        private void MenuFlyoutItemFields_Click(object sender, RoutedEventArgs e)
        {
            foreach (var doc in SelectionManager.GetSelectedSiblings(this))
                doc.KeyValueViewDocument();
        }
        private void MenuFlyoutItemToggleAsAdornment_Click(object sender, RoutedEventArgs e)
        {
            foreach (var docView in SelectionManager.GetSelectedSiblings(this))
            {
                docView.ViewModel.IsAdornmentGroup = !docView.ViewModel.IsAdornmentGroup;
                SetZLayer();
            }
        }
        public void MenuFlyoutItemFitToParent_Click(object sender, RoutedEventArgs e)
        {
            var collectionView = this.GetFirstDescendantOfType<CollectionView>();
            if (collectionView != null)
            {
                collectionView.ViewModel.FitToParent = !collectionView.ViewModel.FitToParent;
                if (collectionView.ViewModel.FitToParent)
                    collectionView.ViewModel.FitContents();
            }
        }
        public void MenuFlyoutItemPreview_Click(object sender, RoutedEventArgs e) { ParentCollection.ViewModel.AddDocument(ViewModel.DataDocument.GetPreviewDocument(new Point(ViewModel.LayoutDocument.GetPositionField().Data.X + ActualWidth, ViewModel.LayoutDocument.GetPositionField().Data.Y))); }
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


        void This_Drop(object sender, DragEventArgs e)
        {
            //xFooter.Visibility = xHeader.Visibility = Visibility.Collapsed;
            var dragModel = (DragDocumentModel)e.DataView.Properties[nameof(DragDocumentModel)];
            if (dragModel?.LinkSourceView != null)
            {
                var dragDoc = dragModel.DraggedDocument;
                if (KeyStore.RegionCreator[dragDoc.DocumentType] != null)
                    dragDoc = KeyStore.RegionCreator[dragDoc.DocumentType](dragModel.LinkSourceView);

                var dropDoc = ViewModel.DocumentController;
                if (KeyStore.RegionCreator[dropDoc.DocumentType] != null)
                    dropDoc = KeyStore.RegionCreator[dropDoc.DocumentType](this);

                dragDoc.Link(dropDoc);

                e.AcceptedOperation = e.DataView.RequestedOperation == DataPackageOperation.None ? DataPackageOperation.Link : e.DataView.RequestedOperation;

                e.Handled = true;
            }
        }
        //void FooterDrop(object sender, DragEventArgs e)
        //{
        //    var dragModel = (DragDocumentModel)e.DataView.Properties[nameof(DragDocumentModel)];

        //    if (dragModel?.LinkSourceView != null)
        //    {
        //        var note = new RichTextNote("<annotation>").Document;
        //        dragModel.DraggedDocument.Link(note);
        //        drop(true, note);
        //    }
        //    else
        //        drop(true, dragModel.GetDropDocument(new Point()));
        //    e.AcceptedOperation = e.DataView.RequestedOperation == DataPackageOperation.None ? DataPackageOperation.Copy : e.DataView.RequestedOperation;

        //    e.Handled = true;
        //}
        //void HeaderDrop(object sender, DragEventArgs e)
        //{
        //    var dragModel = (DragDocumentModel)e.DataView.Properties[nameof(DragDocumentModel)];

        //    if (dragModel?.LinkSourceView != null)
        //    {
        //        var note = new RichTextNote("<annotation>").Document;
        //        dragModel.DraggedDocument.Link(note);
        //        drop(false, note);
        //    }
        //    else
        //        drop(false, dragModel.GetDropDocument(new Point()));

        //    e.AcceptedOperation = e.DataView.RequestedOperation == DataPackageOperation.None ? DataPackageOperation.Copy : e.DataView.RequestedOperation;

        //    e.Handled = true;
        //}

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
                    var fields = activeLayout.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null).TypedData.ToArray().ToList();
                    if (!footer)
                        fields.Insert(0, newFieldDoc);
                    else fields.Add(newFieldDoc);
                    activeLayout.SetField(KeyStore.DataKey, new ListController<DocumentController>(fields), true);
                }
                else
                {
                    var listCtrl = activeLayout.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);
                    if (!footer)
                        listCtrl.Add(newFieldDoc, 0);
                    else listCtrl.Add(newFieldDoc);
                }
            }
            else
            {
                var curLayout = activeLayout;
                if (ViewModel.DocumentController?.GetActiveLayout() != null) // wrap existing activeLayout into a new StackPanel activeLayout
                {
                    curLayout.SetHorizontalAlignment(HorizontalAlignment.Stretch);
                    curLayout.SetVerticalAlignment(VerticalAlignment.Stretch);
                    curLayout.SetWidth(double.NaN);
                    curLayout.SetHeight(double.NaN);
                }
                else  // need to create a stackPanel activeLayout and add the document to it
                {
                    curLayout = activeLayout.MakeCopy() as DocumentController; // ViewModel's DocumentController is this activeLayout so we can't nest that or we get an infinite recursion
                    curLayout.Tag = "StackPanel DocView Layout";
                    curLayout.SetWidth(double.NaN);
                    curLayout.SetHeight(double.NaN);
                    curLayout.SetField(KeyStore.DocumentContextKey, ViewModel.DataDocument, true);
                }
                activeLayout = new StackLayout(new DocumentController[] { footer ? curLayout : newFieldDoc, footer ? newFieldDoc : curLayout }).Document;
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

        void This_DragOver(object sender, DragEventArgs e)
        {
            var dragModel = (DragDocumentModel)e.DataView.Properties[nameof(DragDocumentModel)];
            //xFooter.Visibility = xHeader.Visibility = Visibility.Visible;
            ViewModel.DecorationState = ViewModel?.Undecorated == false;

            e.AcceptedOperation = e.DataView.RequestedOperation == DataPackageOperation.None ? DataPackageOperation.Copy : e.DataView.RequestedOperation;

            e.DragUIOverride.IsContentVisible = true;

            e.Handled = true;
        }

        public void This_DragLeave(object sender, DragEventArgs e)
        {
            //xFooter.Visibility = xHeader.Visibility = Visibility.Collapsed;
            ViewModel.DecorationState = false;
        }
		
        public void hideControls()
        {
            ViewModel.DecorationState = false;
        }

        public void hideEllipses()
        {
            xAnnotateEllipseBorder.Visibility = Visibility.Collapsed;
            xOperatorEllipseBorder.Visibility = Visibility.Collapsed;
            xTemplateEditorEllipseBorder.Visibility = Visibility.Collapsed;
        }

        public void showControls()
        {
            ViewModel.DecorationState = true;
        }

        private void MenuFlyoutItemPin_Click(object sender, RoutedEventArgs e)
        {
            MainPage.Instance.PinToPresentation(ViewModel);
        }

	    private void XAnnotateEllipseBorder_OnTapped_(object sender, TappedRoutedEventArgs e)
	    {

		    if (ViewModel.Content is IAnnotationEnabled)
		    {
			    var element = (IAnnotationEnabled) ViewModel.Content;
			    element?.RegionSelected(element, new Point(0, 0), null);

			}
		    else
		    {
			    var ann = new AnnotationManager(ViewModel.Content);
			    ann.RegionPressed(ViewModel.DocumentController, e.GetPosition(MainPage.Instance));
		    }
		}

        private void MenuFlyoutItemApplyTemplate_Click(object sender, RoutedEventArgs e)
        {
            var applier = new TemplateApplier(ViewModel.LayoutDocument, ParentCollection.ViewModel.DocumentViewModels);
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
    }
}