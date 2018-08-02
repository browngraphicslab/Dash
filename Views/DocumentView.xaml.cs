using DashShared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
using Dash.Converters;
using Visibility = Windows.UI.Xaml.Visibility;
using Dash.Models.DragModels;
using Dash.Views;
using iText.StyledXmlParser.Jsoup.Nodes;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Media.Animation;


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

        private Point _newpoint;

        public MenuFlyout MenuFlyout { get; set; }

        static readonly SolidColorBrush SingleSelectionBorderColor = new SolidColorBrush(Colors.LightGray);
        static readonly SolidColorBrush GroupSelectionBorderColor = new SolidColorBrush(Colors.LightBlue);

        static DocumentView _focusedDocument;

        // the document that has input focus (logically similar to keyboard focus but different since Images, etc can't be keyboard focused).
        public static DocumentView FocusedDocument
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
        private bool _showResize;
        private bool _isQuickEntryOpen;

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
            get { return (bool)GetValue(BindVisibilityProperty); }
            set { SetValue(BindVisibilityProperty, value); }
        }

        public static readonly DependencyProperty StandardViewLevelProperty = DependencyProperty.Register(
            "StandardViewLevel", typeof(CollectionViewModel.StandardViewLevel), typeof(DocumentView),
            new PropertyMetadata(CollectionViewModel.StandardViewLevel.None, StandardViewLevelChanged));


        public CollectionViewModel.StandardViewLevel StandardViewLevel
        {
            get => (CollectionViewModel.StandardViewLevel)GetValue(StandardViewLevelProperty);
            set => SetValue(StandardViewLevelProperty, value);
        }

        public bool ShowResize
        {
            get => _showResize;
            set
            {
                _showResize = value;
                if (!value)
                {
                    RemoveResizeHandlers();
                }
            }
        }

        private Flyout _flyout;
        private double _width;
        private double _height;

        private ImageSource _docPreview = null;
        public event EventHandler ResizeManipulationStarted;
        public event EventHandler ResizeManipulationCompleted;

        private ImageSource DocPreview
        {
            get { return _docPreview; }
            set
            {
                _docPreview = value;
                xToolTipPreview.Source = value;
                // To document previews from being resized
                //_docPreview.GetFirstAncestorOfType<DocumentView>().RemoveResizeHandlers();
            }
        }

        // == CONSTRUCTORs ==

        public DocumentView()
        {
            InitializeComponent();

            _flyout = new Flyout { Placement = FlyoutPlacementMode.Right };

            Util.InitializeDropShadow(xShadowHost, xDocumentBackground);

            // set bounds
            MinWidth = 25;
            MinHeight = 25;
            _newpoint = new Point(0, 0);


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
                        Tag = "Visibility binding in DocumentView"
                    };
                this.AddFieldBinding(VisibilityProperty, binding);
            }

            void updateBindings()
            {
                updateRenderTransformBinding(null, null);
                updateVisibilityBinding(null, null);

                _templateEditor = ViewModel?.DataDocument.GetField<DocumentController>(KeyStore.TemplateEditorKey);

                this.BindBackgroundColor();

            }

            void sizeChangedHandler(object sender, SizeChangedEventArgs e)
            {
                //var cview = this.GetFirstAncestorOfType<CollectionView>();
                //var container = cview?.CurrentView as CollectionFreeformView;
                //if (container != null && container.ViewModel.FitToParent)
                //    container.ViewModel.FitContents(cview);
                ViewModel?.LayoutDocument.SetActualSize(new Point(ActualWidth, ActualHeight));

                PositionContextPreview();
            }
            Loaded += (sender, e) =>
            {
                FadeIn.Begin();
                updateBindings();
                DataContextChanged += (s, a) => updateBindings();

                SizeChanged += sizeChangedHandler;
                ViewModel?.LayoutDocument.SetActualSize(new Point(ActualWidth, ActualHeight));

                var maxZ = int.MinValue;
                var parentCanvas = this.GetFirstAncestorOfType<ContentPresenter>()?.GetFirstAncestorOfType<Canvas>() ?? new
                    Canvas();
                foreach (var item in parentCanvas.Children)
                {
                    maxZ = Math.Max(Canvas.GetZIndex(item), maxZ);
                }

                Canvas.SetZIndex(this.GetFirstAncestorOfType<ContentPresenter>(), maxZ + 1);

                SetZLayer();

                //var type = ViewModel?.DocumentController.GetDereferencedField(KeyStore.DataKey, null)?.TypeInfo;
                //if (ViewModel?.LayoutDocument != null) BindBackgroundColor();



                //            switch (type)
                //            {
                //                case DashShared.TypeInfo.Image:
                //                    xTitleIcon.Text = Application.Current.Resources["ImageDocumentIcon"] as string;
                //                    break;
                //                case DashShared.TypeInfo.Audio:
                //                    xTitleIcon.Text = Application.Current.Resources["AudioDocumentIcon"] as string;
                //                    break;
                //                case DashShared.TypeInfo.Video:
                //                    xTitleIcon.Text = Application.Current.Resources["VideoDocumentIcon"] as string;
                //                    break;
                //                case DashShared.TypeInfo.RichText:
                //                case DashShared.TypeInfo.Text:
                //                    xTitleIcon.Text = Application.Current.Resources["TextIcon"] as string;
                //                    break;
                //                case DashShared.TypeInfo.Document:
                //                    xTitleIcon.Text = Application.Current.Resources["DocumentPlainIcon"] as string;
                //                    break;
                //                case DashShared.TypeInfo.Template:
                //                    xTitleIcon.Text = Application.Current.Resources["CollectionIcon"] as string;
                //                    break;
                //                default:
                //                    xTitleIcon.Text = Application.Current.Resources["DefaultIcon"] as string;
                //                    break;

                //}

                //            if (type.Equals(DashShared.TypeInfo.Template))
                //            {
                //                xTitleIcon.Text = Application.Current.Resources["CollectionIcon"] as string;
                //            }

                //if (_newpoint.X.Equals(0) && _newpoint.Y.Equals(0)) 
                //{
                //	xOperatorEllipseBorder.Margin = new Thickness(10, 0, 0, 0);
                //	xAnnotateEllipseBorder.Margin = new Thickness(10, AnnotateEllipseUnhighlight.Width + 5, 0, 0);
                //	xTemplateEditorEllipseBorder.Margin =
                //		new Thickness(10, 2 * (AnnotateEllipseUnhighlight.Width + 5), 0, 0);
                //}
                //else
                //{
                //	UpdateEllipses(_newpoint);
                //}

                UpdateResizers();


                //var converter = new StringToBrushConverter();
                //var currColor = converter.ConvertDataToXaml(ViewModel?.LayoutDocument?.GetField<TextController>(KeyStore.BackgroundColorKey, true).Data);
                //if (currColor != null) SetBackgroundColor((currColor as SolidColorBrush).Color);
            };
            Unloaded += (sender, args) => { SizeChanged -= sizeChangedHandler; SelectionManager.Deselect(this);  };

            PointerPressed += (sender, e) =>
            {
                DocumentSelected?.Invoke(this, new DocumentViewSelectedEventArgs());
                bool right =
                    (e.GetCurrentPoint(this).Properties.IsRightButtonPressed ||
                     MenuToolbar.Instance.GetMouseMode() == MenuToolbar.MouseMode.PanFast) && !ViewModel.Undecorated;
                var parentFreeform = this.GetFirstAncestorOfType<CollectionFreeformBase>();
                var parentParentFreeform = parentFreeform?.GetFirstAncestorOfType<CollectionFreeformBase>();
                ManipulationMode =
                    right && parentFreeform != null && (this.IsShiftPressed() || parentParentFreeform == null)
                        ? ManipulationModes.All
                        : ManipulationModes.None;
                MainPage.Instance.Focus(FocusState.Programmatic);
                e.Handled = ManipulationMode != ManipulationModes.None;
                if (false)  // bcz: set to 'true' for drag/Drop interactions
                    SetupDragDropDragging(e);
                e.Handled = true;
            };

            PointerEntered += DocumentView_PointerEntered;
            PointerExited += DocumentView_PointerExited;
            RightTapped += (sender, e) => e.Handled = TappedHandler(e.Handled);
            Tapped += (sender, e) => e.Handled = TappedHandler(e.Handled);
            // AddHandler(TappedEvent, new TappedEventHandler(DocumentView_OnTapped), true);  // RichText and other controls handle Tapped events

            void ResizeTLaspect(object sender, ManipulationDeltaRoutedEventArgs e) { Resize(sender as FrameworkElement, e, true, true, true); }
            void ResizeRTaspect(object sender, ManipulationDeltaRoutedEventArgs e) { Resize(sender as FrameworkElement, e, true, false, true); }
            void ResizeBLaspect(object sender, ManipulationDeltaRoutedEventArgs e) { Resize(sender as FrameworkElement, e, false, true, true); }
            void ResizeBRaspect(object sender, ManipulationDeltaRoutedEventArgs e) { Resize(sender as FrameworkElement, e, false, false, true); }
            void ResizeRTunconstrained(object sender, ManipulationDeltaRoutedEventArgs e) { Resize(sender as FrameworkElement, e, true, false, false); }
            void ResizeBLunconstrained(object sender, ManipulationDeltaRoutedEventArgs e) { Resize(sender as FrameworkElement, e, false, true, false); }
            void ResizeBRunconstrained(object sender, ManipulationDeltaRoutedEventArgs e) { Resize(sender as FrameworkElement, e, false, false, false); }
            // setup ResizeHandles
            void ResizeHandles_OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
            {
                if (this.IsRightBtnPressed())
                    return;
                (sender as FrameworkElement).ManipulationCompleted -= ResizeHandles_OnManipulationCompleted;
                (sender as FrameworkElement).ManipulationCompleted += ResizeHandles_OnManipulationCompleted;

                xTopLeftResizeControl.ManipulationDelta -= ResizeTLaspect;
                xTopRightResizeControl.ManipulationDelta -= ResizeRTaspect;
                xBottomLeftResizeControl.ManipulationDelta -= ResizeBLaspect;
                xBottomRightResizeControl.ManipulationDelta -= ResizeBRaspect;
                xTopResizeControl.ManipulationDelta -= ResizeRTunconstrained;
                xLeftResizeControl.ManipulationDelta -= ResizeBLunconstrained;
                xRightResizeControl.ManipulationDelta -= ResizeBRunconstrained;
                xBottomResizeControl.ManipulationDelta -= ResizeBRunconstrained;

                xTopLeftResizeControl.ManipulationDelta += ResizeTLaspect;
                xTopRightResizeControl.ManipulationDelta += ResizeRTaspect;
                xBottomLeftResizeControl.ManipulationDelta += ResizeBLaspect;
                xBottomRightResizeControl.ManipulationDelta += ResizeBRaspect;
                xTopResizeControl.ManipulationDelta += ResizeRTunconstrained;
                xLeftResizeControl.ManipulationDelta += ResizeBLunconstrained;
                xRightResizeControl.ManipulationDelta += ResizeBRunconstrained;
                xBottomResizeControl.ManipulationDelta += ResizeBRunconstrained;
                ResizeManipulationStarted?.Invoke(sender, null);
                UndoManager.StartBatch();

                MainPage.Instance.Focus(FocusState.Programmatic);
                if (!this.IsRightBtnPressed()) // ignore right button drags
                {
                    //this.GetDescendantsOfType<PdfView>().ToList().ForEach((p) => p.Freeze());
                    PointerExited -=
                        DocumentView_PointerExited; // ignore any pointer exit events which will change the visibility of the dragger
                    e.Handled = true;
                }
            }

            void ResizeHandles_restorePointerTracking()
            {
                if (StandardViewLevel.Equals(CollectionViewModel.StandardViewLevel.None) ||
                    StandardViewLevel.Equals(CollectionViewModel.StandardViewLevel.Detail))
                    ViewModel.DecorationState = xBottomRightResizeControl.IsPointerOver();
                PointerExited -= DocumentView_PointerExited;
                PointerExited += DocumentView_PointerExited;

            };

            void ResizeHandles_OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
            {
                xTopLeftResizeControl.ManipulationDelta -= ResizeTLaspect;
                xTopRightResizeControl.ManipulationDelta -= ResizeRTaspect;
                xBottomLeftResizeControl.ManipulationDelta -= ResizeBLaspect;
                xBottomRightResizeControl.ManipulationDelta -= ResizeBRaspect;
                xTopResizeControl.ManipulationDelta -= ResizeRTunconstrained;
                xLeftResizeControl.ManipulationDelta -= ResizeBLunconstrained;
                xRightResizeControl.ManipulationDelta -= ResizeBRunconstrained;
                xBottomResizeControl.ManipulationDelta -= ResizeBRunconstrained;
                (sender as FrameworkElement).ManipulationCompleted -= ResizeHandles_OnManipulationCompleted;
                ResizeHandles_restorePointerTracking();
                this.GetDescendantsOfType<CustomPdfView>().ToList().ForEach((p) => p.UnFreeze());
                e.Handled = true;

                UndoManager.EndBatch();

                ResizeManipulationCompleted?.Invoke(sender, null);
            }

            foreach (var handle in new Rectangle[]
            {
                xTopLeftResizeControl, xTopResizeControl, xTopRightResizeControl,
                xLeftResizeControl, xRightResizeControl,
                xBottomLeftResizeControl, xBottomRightResizeControl, xBottomResizeControl
            })
            {
                handle.Tag = handle.ManipulationMode;
                handle.ManipulationStarted += ResizeHandles_OnManipulationStarted;
                handle.PointerReleased += (s, e) => ResizeHandles_restorePointerTracking();
                handle.PointerPressed += (s, e) =>
                {
                    ManipulationMode = ManipulationModes.None;
                    e.Handled = !e.GetCurrentPoint(this).Properties.IsRightButtonPressed;
                    if (e.Handled)
                    {
                        CapturePointer(e.Pointer);
                        handle.ManipulationMode = (Windows.UI.Xaml.Input.ManipulationModes)handle.Tag;
                    }
                    else if (false) // bcz: set to true for drag/drop interactions
                        handle.ManipulationMode = ManipulationModes.None;
                    else
                        handle.ManipulationMode = ManipulationModes.All;
                };
            }

			// setup OperatorEllipse 
			//OperatorEllipseHighlight.PointerExited += (sender, e) => OperatorEllipseHighlight.Visibility = Visibility.Collapsed;
			//OperatorEllipseUnhighlight.PointerEntered += (sender, e) => OperatorEllipseHighlight.Visibility = Visibility.Visible;
			//xOperatorEllipseBorder.PointerPressed += (sender, e) =>
			//{
			//	this.ManipulationMode = ManipulationModes.None;
			//	e.Handled = !e.GetCurrentPoint(this).Properties.IsRightButtonPressed;
			//};
			//xOperatorEllipseBorder.PointerReleased += (sender, e) => ManipulationMode = ManipulationModes.All;
			//xOperatorEllipseBorder.DragStarting += (sender, args) =>
			//{
			//	//var selected = (ParentCollection.CurrentView as CollectionFreeformBase)?.SelectedDocs.Select((dv) => dv.ViewModel.DocumentController);
			//	//if (selected?.Count() > 0)
			//	//{
			//	//    args.Data.Properties[nameof(List<DragDocumentModel>)] =
			//	//            new List<DragDocumentModel>(selected.Select((s) => new DragDocumentModel(s, true)));
			//	//}
			//	//else
			//	args.Data.Properties[nameof(DragDocumentModel)] =
			//		new DragDocumentModel(ViewModel.DocumentController, false);
			//	args.AllowedOperations =
			//		DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;
			//	args.Data.RequestedOperation =
			//		DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
			//	ViewModel.DecorationState = false;
			//};

			// setup LinkEllipse
			//AnnotateEllipseHighlight.PointerExited += (sender, e) => AnnotateEllipseHighlight.Visibility = Visibility.Collapsed;
			//AnnotateEllipseUnhighlight.PointerEntered += (sender, e) => AnnotateEllipseHighlight.Visibility = Visibility.Visible;
			//xAnnotateEllipseBorder.PointerPressed += (sender, e) =>
			//{
			//	this.ManipulationMode = ManipulationModes.None;
			//	e.Handled = !e.GetCurrentPoint(this).Properties.IsRightButtonPressed;
			//};
			//xAnnotateEllipseBorder.PointerReleased += (sender, e) => ManipulationMode = ManipulationModes.All;
			//xAnnotateEllipseBorder.DragStarting += (sender, args) =>
			//{
			//	args.Data.Properties[nameof(DragDocumentModel)] =
			//		new DragDocumentModel(ViewModel.DocumentController, false, this);
			//	args.AllowedOperations =
			//		DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;
			//	args.Data.RequestedOperation =
			//		DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
			//	ViewModel.DecorationState = false;
			//};

			// setup EditorEllipse
			//TemplateEditorEllipseBorderHighlight.PointerExited += (sender, e) => TemplateEditorEllipseBorderHighlight.Visibility = Visibility.Collapsed;
			//TemplateEditorEllipseBorderUnhighlight.PointerEntered += (sender, e) => TemplateEditorEllipseBorderHighlight.Visibility = Visibility.Visible;
			//xTemplateEditorEllipseBorder.PointerPressed += (sender, e) =>
			//{
			//	this.ManipulationMode = ManipulationModes.None;
			//	ToggleTemplateEditor();
			//	e.Handled = !e.GetCurrentPoint(this).Properties.IsRightButtonPressed;
			//};
			//xTemplateEditorEllipseBorder.PointerReleased += (sender, e) => ManipulationMode = ManipulationModes.All;
			//xTemplateEditorEllipseBorder.DragStarting += (sender, args) => { };

			// setup Title Icon
			//xTitleIcon.PointerPressed += (sender, e) =>
			//{
			//	CapturePointer(e.Pointer);
			//	ManipulationMode = e.GetCurrentPoint(this).Properties.IsRightButtonPressed
			//		? ManipulationModes.None
			//		: ManipulationModes.All;
			//	e.Handled = ManipulationMode == ManipulationModes.All;
			//};
			//xTitleIcon.Tapped += (s, args) =>
			//{
			//	ShowContext();
			//	args.Handled = true;
			//};
			// setup Context Title
			xContextTitle.Tapped += (sender, e) => ShowContext();
			xContextTitle.SizeChanged += (sender, e) => Canvas.SetLeft(xContextTitle, -xContextTitle.ActualWidth - 1);

            // add manipulation code
            ManipulationControls = new ManipulationControls(this);
            ManipulationControls.OnManipulatorTranslatedOrScaled += (delta) =>
                SelectionManager.GetSelectedSiblings(this).ForEach((d) => d.TransformDelta(delta));
            ManipulationControls.OnManipulatorStarted += () =>
            {
                ToFront();
                var wasSelected = this.xTargetBorder.BorderThickness.Left > 0;

                // get all BackgroundBox types selected initially, and add the documents they contain to selected documents list 
                var adornmentGroups = this.IsAltPressed()
                    ? new List<DocumentView>()
                    : SelectionManager.GetSelectedSiblings(this).Where((dv) => dv.ViewModel.IsAdornmentGroup).ToList();
                if (!wasSelected && ParentCollection?.CurrentView is CollectionFreeformBase cview)
                {
                    adornmentGroups.ForEach((dv) =>
                    {
                        SelectionManager.SelectDocuments(cview.DocsInMarquee(new Rect(dv.ViewModel.Position,
                            new Size(dv.ActualWidth, dv.ActualHeight))));
                    });

                    SetSelectionBorder(false);
                }

                // initialize the cached values of position and scale for each manipulated document  
                SelectionManager.GetSelectedSiblings(this).ForEach((d) =>
                {
                    d.ViewModel.InteractiveManipulationPosition = d.ViewModel.Position;
                    d.ViewModel.InteractiveManipulationScale = d.ViewModel.Scale;
                });

                if (SelectionManager.SelectedDocs.Contains(this))
                {

                    
                }
            };
            ManipulationControls.OnManipulatorCompleted += () =>
            {
                using (UndoManager.GetBatchHandle())
                {
                    SelectionManager.GetSelectedSiblings(this).ForEach((d) =>
                    {
                        d.ViewModel.DecorationState = d.IsPointerOver() &&
                                                      (d.ViewModel.ViewLevel.Equals(CollectionViewModel
                                                           .StandardViewLevel.Detail) ||
                                                       d.ViewModel.ViewLevel.Equals(CollectionViewModel
                                                           .StandardViewLevel.None));
                        d.ViewModel.Position =
                            d.ViewModel
                                .InteractiveManipulationPosition; // write the cached values of position and scale back to the viewModel
                        d.ViewModel.Scale = d.ViewModel.InteractiveManipulationScale;
                    });
                    var wasSelected = this.xTargetBorder.BorderThickness.Left > 0;
                    if (ViewModel.IsAdornmentGroup && !wasSelected)
                    {
                        if (ParentCollection.CurrentView is CollectionFreeformView ||
                            ParentCollection.CurrentView is CollectionStandardView)
                        {
                            SelectionManager.DeselectAll();
                            
                        }
                    }
                }

              
               
            };

            KeyDown += (sender, args) =>
            {
                if (args.Key == VirtualKey.Down && !_isQuickEntryOpen || args.Key == VirtualKey.Up && _isQuickEntryOpen)
                {
                    if (!_isQuickEntryOpen)
                    {
                        _clearByClose = true;
                        ClearQuickEntryBoxes();
                        xKeyBox.Focus(FocusState.Keyboard);
                    }

                    ToggleQuickEntry();
                    args.Handled = true;
                }
                else if (args.Key == VirtualKey.Down && _isQuickEntryOpen)
                {
                    if (xKeyBox.FocusState != FocusState.Unfocused)
                    {
                        _articialChange = true;
                        int pos = xKeyBox.SelectionStart;
                        if (xKeyBox.Text.ToLower().StartsWith("v")) xKeyBox.Text = "d" + xKeyBox.Text.Substring(1);
                        else if (xKeyBox.Text.ToLower().StartsWith("d")) xKeyBox.Text = "v" + xKeyBox.Text.Substring(1);
                        xKeyBox.SelectionStart = pos;
                    }
                    args.Handled = true;
                }
            };

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

            MenuFlyout = xMenuFlyout;

            MenuFlyout.Opened += (s, e) =>
            {
                if (this.IsShiftPressed())
                    MenuFlyout.Hide();
            };

            ToFront();
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

        public uint PointerId;
        public async void SetupDragDropDragging(PointerRoutedEventArgs e)
        {
            if (e != null)
            {
                if (!e.IsRightPressed() || ViewModel.DataDocument.GetField<TextController>(KeyStore.CollectionViewTypeKey) != null)
                    return;
                if ((e.OriginalSource as FrameworkElement).Tag == e)
                    return;
                ManipulationMode = ManipulationModes.None;
                (e.OriginalSource as FrameworkElement).Tag = e;
                (e.OriginalSource as FrameworkElement).CanDrag = true;
            }
            Matrix mat = ((MatrixTransform)TransformToVisual(Window.Current.Content)).Matrix;
            mat.OffsetX = 0;
            mat.OffsetY = 0;

            Debug.WriteLine(new Point(ActualWidth, ActualHeight));

            var trans = new MatrixTransform { Matrix = mat };
            Point p = trans.TransformPoint(new Point(ActualWidth/* - xTitleBorder.Margin.Left*/, ActualHeight));

            var cdo = new CoreDragOperation();
            var rtb = new RenderTargetBitmap();

            await rtb.RenderAsync(this, (int)p.X, (int)p.Y);

            IBuffer buf = await rtb.GetPixelsAsync();
            SoftwareBitmap sb = SoftwareBitmap.CreateCopyFromBuffer(buf, BitmapPixelFormat.Bgra8, rtb.PixelWidth, rtb.PixelHeight);

            Point pos = e?.GetCurrentPoint(this).Position ?? new Point();

            //pos.X -= xTitleBorder.Margin.Left;
            pos = trans.TransformPoint(pos);
            pos.X = Math.Max(0, pos.X);
            pos.Y = Math.Max(0, pos.Y);
            pos.X = Math.Min(pos.X, ActualWidth);
            pos.Y = Math.Min(pos.Y, ActualHeight);

            cdo.AllowedOperations = DataPackageOperation.Copy | DataPackageOperation.Link;
            cdo.SetDragUIContentFromSoftwareBitmap(sb, pos);
            cdo.SetPointerId(e?.Pointer.PointerId ?? PointerId);

            await cdo.StartAsync();
        }

        public void RemoveResizeHandlers()
        {
            foreach (var handle in new Rectangle[]
            {
                xTopLeftResizeControl, xTopResizeControl, xTopRightResizeControl,
                xLeftResizeControl, xRightResizeControl,
                xBottomLeftResizeControl, xBottomRightResizeControl, xBottomResizeControl
            })
            {
                handle.Visibility = Visibility.Collapsed;
            }

            xLeftColumn.Width = new GridLength(0);
            xRightColumn.Width = new GridLength(0);
            xTopRow.Height = new GridLength(0);
            xBottomRow.Height = new GridLength(0);
            ViewModel.DecorationState = false;

			//xOperatorEllipseBorder.Visibility = Visibility.Collapsed;
			//xAnnotateEllipseBorder.Visibility = Visibility.Collapsed;
			//xTemplateEditorEllipseBorder.Visibility = Visibility.Collapsed;
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


        #region StandardCollectionView

        private async void GetDocPreview()
        {
            xIconBorder.BorderThickness = new Thickness(1);
            xIconBorder.Background = new SolidColorBrush(Colors.WhiteSmoke)
            {
                Opacity = 0.5
            };
            var type = ViewModel.DocumentController.DocumentType;
            xSmallIconImage.Visibility = Visibility.Visible;
            xSmallIconImage.Source = GetTypeIcon();
            if (DocPreview == null)
                DocPreview = await GetPreview();
            xIconImage.Source = DocPreview ?? new BitmapImage(new Uri("ms-appx:///Assets/Icons/Unavailable.png"));
            OpenIcon();
        }

        public async Task<RenderTargetBitmap> GetPreview()
        {
            RenderTargetBitmap bitmap = new RenderTargetBitmap();
            xContentPresenter.Visibility = Visibility.Visible;
            await bitmap.RenderAsync(xContentPresenter.Content as FrameworkElement, 1000, 1000);
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
            xDocumentBackground.Fill = new SolidColorBrush(Colors.Transparent);
            xIcon.Visibility = Visibility.Visible;
            xContentPresenter.Visibility = Visibility.Collapsed;
        }

        private void OpenFreeform()
        {
            if (ViewModel.DocumentController.DocumentType.Equals(CollectionBox.DocumentType))
                xDocumentBackground.Fill = ((SolidColorBrush)Application.Current.Resources["DocumentBackground"]);
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
            }
            else if (type.Equals(OperatorBox.DocumentType))
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
            if (ViewModel.DocumentController.DocumentType.Equals(BackgroundShape.DocumentType)) return;
            switch (StandardViewLevel)
            {
                case CollectionViewModel.StandardViewLevel.Detail:
                    DocPreview = await GetPreview();
                    CloseDocPreview();
                    OpenFreeform();
                    break;
                case CollectionViewModel.StandardViewLevel.Region:
                    xIconLabel.FontSize = 11;
                    GetDocPreview();
                    break;
                case CollectionViewModel.StandardViewLevel.Overview:
                    xIconLabel.FontSize = 25;
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
        public bool PreventManipulation { get; set; }

        /// <summary> 
        /// Updates the cached position and scale of the document without modifying the underlying viewModel.  
        /// At the end of the interaction, the caches are copied to the viewModel.
        /// </summary>
        /// <param name="delta"></param>
        public void TransformDelta(TransformGroupData delta)
        {
            
            if (PreventManipulation) return;
            var currentTranslate = ViewModel.InteractiveManipulationPosition;
            var currentScaleAmount = ViewModel.InteractiveManipulationScale;

            var deltaTranslate = delta.Translate;
            var deltaScaleAmount = delta.ScaleAmount;

            var scaleAmount = new Point(currentScaleAmount.X * deltaScaleAmount.X,
                currentScaleAmount.Y * deltaScaleAmount.Y);
            var translate = new Point(currentTranslate.X + deltaTranslate.X, currentTranslate.Y + deltaTranslate.Y);











            if (Bounds != null && (!Bounds.Rect.Contains(translate) ||
                                   !Bounds.Rect.Contains(new Point(translate.X + ActualWidth,
                                       translate.Y + ActualHeight))))
            {
                return;
            }

            ViewModel.InteractiveManipulationPosition = translate;
            ViewModel.InteractiveManipulationScale = scaleAmount;
            RenderTransform =
                TransformGroupMultiConverter.ConvertDataToXamlHelper(new List<object> { translate, scaleAmount });



        }

        public void TransformDelta(Point moveTo)
        {
            var scaleAmount = new Point(ViewModel.InteractiveManipulationScale.X,
                ViewModel.InteractiveManipulationScale.Y);

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
                        .GetDereferencedField<DocumentController>(KeyStore.SelectedSchemaRow, null)?.GetFirstContext()
                        .Title;
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

                void OnSelectedSchemaRowUpdated(DocumentController sender,
                    DocumentController.DocumentFieldUpdatedEventArgs args, Context context1)
                {
                    ShowSelectedContext(true);
                }

                ViewModel.DataDocument.RemoveFieldUpdatedListener(KeyStore.SelectedSchemaRow,
                    OnSelectedSchemaRowUpdated);
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
			//xTitleIcon.Text = Application.Current.Resources["OperatorIcon"] as string;
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
			//xTitleIcon.Text = Application.Current.Resources["CollectionIcon"] as string;
			//alter opacity to be visible (overrides default transparent)
			var currColor = (xDocumentBackground.Fill as SolidColorBrush)?.Color;
			if (currColor?.A < 100) xDocumentBackground.Fill = new SolidColorBrush(Color.FromArgb(255, currColor.Value.R, currColor.Value.G, currColor.Value.B));

            if (this != MainPage.Instance.MainDocView) return;
            view.xOuterGrid.BorderThickness = new Thickness(0);
            foreach (var handle in new Rectangle[]
            {
                xTopLeftResizeControl, xTopResizeControl, xTopRightResizeControl,
                xLeftResizeControl, xRightResizeControl,
                xBottomLeftResizeControl, xBottomRightResizeControl, xBottomRightResizeControl
            })
            {
                handle.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Applies custom override styles to the KeyValuePane view. 
        /// width - the width of a single link node (generally App.xaml defines this, "InputHandleWidth")
        /// </summary>
        public void StyleKeyValuePane()
        {
        }

        #endregion

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
            if (this.IsRightBtnPressed())
            {
                return;
            }
            e.Handled = true;
            if (PreventManipulation)
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
            else
            {
                extraOffsetX = xLeftColumn.Width.Value + xRightColumn.Width.Value;
            }


            double extraOffsetY = 0;

            if (!Double.IsNaN(Height))
            {
                extraOffsetY = ActualHeight - Height;
            }
            else
            {
                extraOffsetY = xTopRow.Height.Value + xBottomRow.Height.Value;
            }


            var delta = Util.DeltaTransformFromVisual(e.Delta.Translation, sender as FrameworkElement);
            //problem is that cumulativeDelta.Y is 0
            var cumulativeDelta = Util.DeltaTransformFromVisual(e.Cumulative.Translation, sender as FrameworkElement);

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
            if (Bounds != null)
            {
                var width = ActualWidth;
                var height = ActualHeight;
                var pos = new Point(ViewModel.XPos + width * (1 - moveXScale),
                    ViewModel.YPos + height * (1 - moveYScale));
                if (!Bounds.Rect.Contains((new Point(pos.X + delta.X, pos.Y + delta.Y))))
                    return;
                var clamped = Clamp(new Point(pos.X + delta.X, pos.Y + delta.Y), Bounds.Rect);
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
            if (Bounds != null)
            {
                if (!Bounds.Rect.Contains(newPos) ||
                    !Bounds.Rect.Contains(new Point(newPos.X + newSize.Width, newPos.Y + DesiredSize.Height)))
                {
                    ViewModel.Position = oldPos;
                    ViewModel.Width = oldSize.Width;
                    ViewModel.Height = oldSize.Height;
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

            if (delta.Y != 0 || this.IsShiftPressed() || isImage)
                ViewModel.Height = newSize.Height;
        }

        private Point Clamp(Point point, Rect rect)
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
            if (ParentCollection != null)
            {
                UndoManager.StartBatch(); // bcz: EndBatch happens in FadeOut completed
                FadeOut.Begin();
                FadeOutBegin?.Invoke();

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

            DocumentDeleted?.Invoke(this, new DocumentViewDeletedEventArgs());
            UndoManager.EndBatch();
        }

        #endregion

        #region Activation

        public void SetSelectionBorder(bool selected)
        {
            xTargetBorder.BorderThickness = selected ? new Thickness(3) : new Thickness(0);
            xTargetBorder.Margin = selected ? new Thickness(-3) : new Thickness(0);
            xTargetBorder.BorderBrush =  new SolidColorBrush(Colors.Transparent);

            xTopLeftResizeControl.Fill =
                selected ? new SolidColorBrush(Color.FromArgb(120, 160, 197, 232)) : new SolidColorBrush(Colors.Transparent);
            xTopResizeControl.Fill =
                selected ? new SolidColorBrush(Color.FromArgb(120, 160, 197, 232)) : new SolidColorBrush(Colors.Transparent);
            xTopRightResizeControl.Fill =
                selected ? new SolidColorBrush(Color.FromArgb(120, 160, 197, 232)) : new SolidColorBrush(Colors.Transparent);

            xBottomLeftResizeControl.Fill =
                selected ? new SolidColorBrush(Color.FromArgb(120, 160, 197, 232)) : new SolidColorBrush(Colors.Transparent);
            xBottomResizeControl.Fill =
                selected ? new SolidColorBrush(Color.FromArgb(120, 160, 197, 232)) : new SolidColorBrush(Colors.Transparent);
            xBottomRightResizeControl.Fill =
                selected ? new SolidColorBrush(Color.FromArgb(120, 160, 197, 232)) : new SolidColorBrush(Colors.Transparent);

            xRightResizeControl.Fill =
                selected ? new SolidColorBrush(Color.FromArgb(120, 160, 197, 232)) : new SolidColorBrush(Colors.Transparent);
            xLeftResizeControl.Fill =
                selected ? new SolidColorBrush(Color.FromArgb(120, 160, 197, 232)) : new SolidColorBrush(Colors.Transparent);

		}

        public void hideResizers()
        {
            xTopLeftResizeControl.Visibility = Visibility.Collapsed;
            xTopRightResizeControl.Visibility = Visibility.Collapsed;
            xTopResizeControl.Visibility = Visibility.Collapsed;

            xBottomLeftResizeControl.Visibility = Visibility.Collapsed;
            xBottomRightResizeControl.Visibility = Visibility.Collapsed;
            xBottomResizeControl.Visibility = Visibility.Collapsed;

            xRightResizeControl.Visibility = Visibility.Collapsed;
            xLeftResizeControl.Visibility = Visibility.Collapsed;
            xTargetBorder.Margin = new Thickness(0);
        }

        #endregion

        /// <summary>
        /// Handles left and right tapped events on DocumentViews
        /// </summary>
        /// <param name="wasHandled">Whether the tapped event was previously handled</param>//this is always false currently so it probably isn't needed
        /// <returns>Whether the calling tapped event should be handled</returns>
        public bool TappedHandler(bool wasHandled)
        {
            DocumentSelected?.Invoke(this, new DocumentViewSelectedEventArgs());
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
            if (!ViewModel.IsAdornmentGroup)
            {
                ToFront();
            }

            //         if (!this.IsRightBtnPressed() && (ParentCollection == null || ParentCollection.CurrentView is CollectionFreeformBase) && (e == null || !e.Handled))
            if ((ParentCollection == null || ParentCollection?.CurrentView is CollectionFreeformBase) && !wasHandled)
            {
                var cfview = ParentCollection?.CurrentView as CollectionFreeformBase;
                if (this.IsShiftPressed())
                {
                    SelectionManager.ToggleSelection(this);
                    
                }
                else
                {
                    SelectionManager.DeselectAll();
                    SelectionManager.Select(this);
                   
                }

                if (SelectionManager.SelectedDocs.Count() > 1)
                {
                    // move focus to container if multiple documents are selected, otherwise allow keyboard focus to remain where it was
                    cfview?.Focus(FocusState.Programmatic);
                }

                return true;
            }

            return false;
        }
		public void DocumentView_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			//if (StandardViewLevel.Equals(CollectionViewModel.StandardViewLevel.None) ||
			//    StandardViewLevel.Equals(CollectionViewModel.StandardViewLevel.Detail))
			//{
			//	if (e == null ||
			//	    (!e.GetCurrentPoint(this).Properties.IsRightButtonPressed &&
			//	     !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) && ViewModel != null)
			//		ViewModel.DecorationState = false;
			//}

			//MainPage.Instance.HighlightTreeView(ViewModel.DocumentController, false);
			//if (MainPage.Instance.MainDocView != this)
			//{
			//	var viewlevel = MainPage.Instance.MainDocView.ViewModel.ViewLevel;
			//	if (viewlevel.Equals(CollectionViewModel.StandardViewLevel.Overview) ||
			//	    viewlevel.Equals(CollectionViewModel.StandardViewLevel.Region))
			//		Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.SizeAll, 0);
			//	else if (viewlevel.Equals(CollectionViewModel.StandardViewLevel.Detail))
			//		Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.IBeam, 0);
			//}
		}

        public void DocumentView_PointerEntered(object sender, PointerRoutedEventArgs e) => DocumentView_PointerEntered();

        public void DocumentView_PointerEntered()
		{
			//if (ViewModel != null)
			//{
			//	if ((StandardViewLevel.Equals(CollectionViewModel.StandardViewLevel.None) ||
			//	     StandardViewLevel.Equals(CollectionViewModel.StandardViewLevel.Detail)) && ViewModel != null)
			//	{
			//	    var isSelected = this.xTargetBorder.BorderThickness.Left > 0;
   //                 ViewModel.DecorationState = ViewModel?.Undecorated == false && isSelected;
			//	}

			//	MainPage.Instance.HighlightTreeView(ViewModel.DocumentController, true);
			//}

			//Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 1);
			//if (MainPage.Instance.MainDocView == this && MainPage.Instance.MainDocView.ViewModel != null)
			//{
			//	var level = MainPage.Instance.MainDocView.ViewModel.ViewLevel;
			//	if (level.Equals(CollectionViewModel.StandardViewLevel.Overview) ||
			//	    level.Equals(CollectionViewModel.StandardViewLevel.Region))
			//		Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.SizeAll, 0);
			//	else if (level.Equals(CollectionViewModel.StandardViewLevel.Detail))
			//		Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.IBeam, 0);
			//}
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

            //if it's a group, add all content docs
            if (ViewModel.IsAdornmentGroup)
            {
                var adornmentGroups = SelectionManager.GetSelectedSiblings(this).Where((dv) => dv.ViewModel.IsAdornmentGroup).ToList();
                if (ParentCollection?.CurrentView is CollectionFreeformBase cview)
                {
                    adornmentGroups.ForEach((dv) =>
                    {
                        foreach (var docV in cview.DocsInMarquee(new Rect(dv.ViewModel.Position,
                            new Size(dv.ActualWidth, dv.ActualHeight))))
                        {
                            if (!selectedDocs.Contains(docV))
                            {
                                selectedDocs.Add(docV);
                            }
                        }
                    });

                    SetSelectionBorder(false);
                }
            }

            var collection = this.GetFirstAncestorOfType<CollectionView>();
            CollectionView nestedCollection = GetCollectionToMoveTo(overlappedViews);

            if (nestedCollection == null)
            {
                return false;
            }

            foreach (DocumentView selDoc in selectedDocs)
            {
                if (selDoc != nestedCollection.GetFirstAncestorOfType<DocumentView>())
                {
                    Point pos = selDoc.TransformToVisual(MainPage.Instance.MainDocView).TransformPoint(new Point());
                    Point where = nestedCollection.CurrentView is CollectionFreeformBase @base
                        ? Util.GetCollectionFreeFormPoint(@base, pos)
                        : new Point();
                    collection.ViewModel.RemoveDocument(selDoc.ViewModel.DocumentController);
                    nestedCollection.ViewModel.AddDocument(selDoc.ViewModel.DocumentController.GetSameCopy(where));
                }
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
            var dtext = this.ViewModel.DataDocument.GetDereferencedField<TextController>(KeyStore.DocumentTextKey, null)
                            ?.Data ?? "";
            collection.LoadNewDataBox(dtext, where, true);
        }

        #endregion

        #region Context menu click handlers

        private void MenuFlyoutItemCopy_Click(object sender, RoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
                foreach (var doc in SelectionManager.GetSelectedSiblings(this))
                    doc.CopyDocument();
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
                    collectionView.ViewModel.FitToParent = !collectionView.ViewModel.FitToParent;
                    if (collectionView.ViewModel.FitToParent)
                        collectionView.ViewModel.FitContents(collectionView);
                }
            }
        }
        public void MenuFlyoutItemPreview_Click(object sender, RoutedEventArgs e) { ParentCollection.ViewModel.AddDocument(ViewModel.DataDocument.GetPreviewDocument(new Point(ViewModel.LayoutDocument.GetPositionField().Data.X + ActualWidth, ViewModel.LayoutDocument.GetPositionField().Data.Y))); }
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
                    dockedView.ChangeView(new DocumentView(){DataContext = new DocumentViewModel(ViewModel.DocumentController)});
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
            //xFooter.Visibility = xHeader.Visibility = Visibility.Collapsed;
            var dragModel = (DragDocumentModel)e.DataView.Properties[nameof(DragDocumentModel)];
            if (dragModel?.LinkSourceView != null)
            {
                var dragDoc = dragModel.DraggedDocument;
                if (KeyStore.RegionCreator[dragDoc.DocumentType] != null)
                    dragDoc = KeyStore.RegionCreator[dragDoc.DocumentType](dragModel.LinkSourceView);


                ActionTextBox inputBox = MainPage.Instance.xLinkInputBox;
                Storyboard fadeIn = MainPage.Instance.xLinkInputIn;
                Storyboard fadeOut = MainPage.Instance.xLinkInputOut;

                Point where = e.GetPosition(MainPage.Instance.xCanvas);

                if (dragModel.LinkType != null)
                {
                    var dropDoc = ViewModel.DocumentController;
                    if (KeyStore.RegionCreator[dropDoc.DocumentType] != null)
                        dropDoc = KeyStore.RegionCreator[dropDoc.DocumentType](this);
                    dragDoc.Link(dropDoc, LinkContexts.None, dragModel.LinkType);
                    dropDoc?.SetField(KeyStore.AnnotationVisibilityKey, new BoolController(true), true);
                }
                else
                {

                    inputBox.RenderTransform = new TranslateTransform { X = where.X, Y = where.Y };

                    inputBox.AddKeyHandler(VirtualKey.Enter, args =>
                    {
                        string entry = inputBox.Text.Trim();
                        if (string.IsNullOrEmpty(entry)) return;

                        inputBox.ClearHandlers(VirtualKey.Enter);

                        void FadeOutOnCompleted(object sender2, object o1)
                        {
                            fadeOut.Completed -= FadeOutOnCompleted;

                            inputBox.Text = "";
                            inputBox.Visibility = Visibility.Collapsed;
                            var dropDoc = ViewModel.DocumentController;
                            if (KeyStore.RegionCreator[dropDoc.DocumentType] != null)
                                dropDoc = KeyStore.RegionCreator[dropDoc.DocumentType](this);
                            dragDoc.Link(dropDoc, LinkContexts.None, entry);
                            dropDoc.SetField(KeyStore.AnnotationVisibilityKey, new BoolController(true), true);
                        }

                        fadeOut.Completed += FadeOutOnCompleted;
                        fadeOut.Begin();

                        args.Handled = true;
                    });

                    inputBox.Visibility = Visibility.Visible;
                    fadeIn.Begin();
                    inputBox.Focus(FocusState.Programmatic);
                }

                e.AcceptedOperation = e.DataView.RequestedOperation == DataPackageOperation.None
                    ? DataPackageOperation.Link
                    : e.DataView.RequestedOperation;

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

        void This_DragOver(object sender, DragEventArgs e)
        {
            var dragModel = (DragDocumentModel)e.DataView.Properties[nameof(DragDocumentModel)];
            //xFooter.Visibility = xHeader.Visibility = Visibility.Visible;
            ViewModel.DecorationState = ViewModel?.Undecorated == false;

            e.AcceptedOperation = e.DataView.RequestedOperation == DataPackageOperation.None
                ? DataPackageOperation.Copy
                : e.DataView.RequestedOperation;

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
			//xAnnotateEllipseBorder.Visibility = Visibility.Collapsed;
			//xOperatorEllipseBorder.Visibility = Visibility.Collapsed;
			//xTemplateEditorEllipseBorder.Visibility = Visibility.Collapsed;
		}

        public void showControls()
        {
            ViewModel.DecorationState = true;
        }

        private void MenuFlyoutItemPin_Click(object sender, RoutedEventArgs e)
        {
            if (Equals(MainPage.Instance.MainDocView)) return;
            
            MainPage.Instance.PinToPresentation(ViewModel.LayoutDocument);
            if (ViewModel.LayoutDocument == null)
            {
                Debug.WriteLine("uh oh");
            }
        }

        private void XAnnotateEllipseBorder_OnTapped_(object sender, TappedRoutedEventArgs e)
        {
            var ann = new AnnotationManager(this);
            ann.FollowRegion(ViewModel.DocumentController, this.GetAncestorsOfType<ILinkHandler>(), e.GetPosition(this));
        }

        private void X_Direction_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.SizeWestEast, 0);
        }

        private void NESW_Direction_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.SizeNortheastSouthwest, 0);
        }

        private void NWSE_Direction_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.SizeNorthwestSoutheast, 0);
        }

        private void Y_Direction_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.SizeNorthSouth, 0);
        }

        private void AllResizers_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
        }


        public void UpdateResizers()
        {
            var newpoint = Util.DeltaTransformFromVisual(new Point(1, 1), this);
            _newpoint = newpoint;

            if (double.IsInfinity(newpoint.X) || double.IsInfinity(newpoint.Y))
                _newpoint = newpoint = new Point();

            xBottomRow.Height = new GridLength(ViewModel?.Undecorated == false || ResizersVisible ? newpoint.Y * 15 : 0);
            xTopRow.Height = new GridLength(ViewModel?.Undecorated == false || ResizersVisible ? newpoint.Y * 15 : 0);
            xLeftColumn.Width = new GridLength(ViewModel?.Undecorated == false || ResizersVisible ? newpoint.Y * 15 : 0);
            xRightColumn.Width = new GridLength(ViewModel?.Undecorated == false || ResizersVisible ? newpoint.Y * 15 : 0);
            //xBottomRow.Height = new GridLength(ViewModel?.Undecorated == true || ViewModel?.DecorationState == false ? 0 : newpoint.Y * 15);
            //xTopRow.Height = new GridLength(ViewModel?.Undecorated == true || ViewModel?.DecorationState == false ? 0 : newpoint.Y * 15);
            //xLeftColumn.Width = new GridLength(ViewModel?.Undecorated == true || ViewModel?.DecorationState == false ? 0 : newpoint.X * 15);
            //xRightColumn.Width = new GridLength(ViewModel?.Undecorated == true || ViewModel?.DecorationState == false ? 0 : newpoint.X * 15);

            UpdateEllipses(newpoint);
        }

        public bool ResizersVisible = false;

		private void UpdateEllipses(Point newpoint)
		{
			//AdjustEllipseSize(TemplateEditorEllipseBorderUnhighlight, 25 * (newpoint.Y));
			//AdjustEllipseSize(AnnotateEllipseUnhighlight, 25 * (newpoint.Y));
			//AdjustEllipseSize(OperatorEllipseUnhighlight, 25 * (newpoint.Y));

			//AdjustEllipseFontSize(12 * newpoint.Y);


			//xTitleIcon.FontSize = 16 * (newpoint.Y);

			//if (TemplateEditorEllipseBorderUnhighlight.Width < 25)
			//{
			//	AdjustEllipseSize(TemplateEditorEllipseBorderUnhighlight, 25);
			//	AdjustEllipseSize(AnnotateEllipseUnhighlight, 25);
			//	AdjustEllipseSize(OperatorEllipseUnhighlight, 25);
			//}

			//if (TemplateEditorEllipseBorderUnhighlight.Width > 150)
			//{
			//	AdjustEllipseSize(TemplateEditorEllipseBorderUnhighlight, 150);
			//	AdjustEllipseSize(AnnotateEllipseUnhighlight, 150);
			//	AdjustEllipseSize(OperatorEllipseUnhighlight, 150);
			//}

			//if (xTitleIcon.FontSize < 16)
			//{
			//	xTitleIcon.FontSize = 16;
			//}

			//if (xTitleIcon.FontSize > 100)
			//{
			//	xTitleIcon.FontSize = 100;
			//}

			//if (xTemplateEllipseText.FontSize < 12)
			//{
			//	AdjustEllipseFontSize(12);
			//}

			//if (xTemplateEllipseText.FontSize > 60)
			//{
			//	AdjustEllipseFontSize(60);
			//}

			//xTitleBorder.Margin = new Thickness(-1.2 * xTitleIcon.FontSize - 10, 0, 0, 0);
			//xOperatorEllipseBorder.Margin = new Thickness(10, 0, 0, 0);
			//xAnnotateEllipseBorder.Margin =
			//	new Thickness(10, AnnotateEllipseUnhighlight.Width + (5 * newpoint.Y), 0, 0);
			//xTemplateEditorEllipseBorder.Margin =
			//	new Thickness(10, 2 * (AnnotateEllipseUnhighlight.Width + (5 * newpoint.Y)), 0, 0);

        }

        private void AdjustEllipseSize(Ellipse ellipse, double length)
        {
            ellipse.Width = length;
            ellipse.Height = length;
        }

		private void AdjustEllipseFontSize(double size)
		{
			//xTemplateEllipseText.FontSize = size;
			//xAnnotateEllipseText.FontSize = size;
			//xOperatorEllipseText.FontSize = size;
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

        //sets background color of doc
        public void SetBackgroundColor(Color color)
        {
            ViewModel?.LayoutDocument?.SetField(KeyStore.BackgroundColorKey, new TextController(color.ToString()), true);
        }

        //gets current background color of doc
        public Color? GetBackgroundColor()
        {
            var colorString = ViewModel.LayoutDocument.GetField<TextController>(KeyStore.BackgroundColorKey, true)?.Data;
            if (colorString == null) return Colors.White;
            return (new StringToBrushConverter().ConvertDataToXaml(colorString) as SolidColorBrush)?.Color;
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

        private void XTitleIcon_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            ToggleQuickEntry();
            e.Handled = true;
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
            ProcessInput();
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
            this.Visibility = Visibility.Collapsed;
        }
    }
}