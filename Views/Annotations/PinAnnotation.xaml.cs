using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Web;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using MyToolkit.Multimedia;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class PinAnnotation
    {
        public DocumentController DocumentController { get; set; }

        public PinAnnotation(NewAnnotationOverlay parent, Point point, DocumentController target = null) : base(parent)
        {
            this.InitializeComponent();

            if (target == null)
            {
                Initialize(point);
            }
            else
            {
                InitializeWithTarget(point, target);
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Call with just a target and no point when you intend to use the target's information to render a pin.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="target"></param>
        public PinAnnotation(NewAnnotationOverlay parent, DocumentController target) : base(parent)
        {
            this.InitializeComponent();
            DocumentController = target;
        }

        public async void Initialize(Point point)
        {
            foreach (var region in ParentOverlay.XAnnotationCanvas.Children)
            {
                if (region is Ellipse existingPin && existingPin.GetBoundingRect(this).Contains(point))
                {
                    return;
                }
            }

            DocumentController annotationController;

            // the user can gain more control over what kind of pushpin annotation they want to make by holding control, which triggers a popup
            if (this.IsCtrlPressed())
            {
                var pushpinType = await MainPage.Instance.GetPushpinType();
                switch (pushpinType)
                {
                    case PushpinType.Text:
                        annotationController = CreateTextPin(point);
                        break;
                    case PushpinType.Video:
                        annotationController = await CreateVideoPin(point);
                        break;
                    case PushpinType.Image:
                        annotationController = await CreateImagePin(point);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                // by default the pushpin will create a text note
                annotationController = CreateTextPin(point);
            }

            // if the user presses back or cancel, return null
            if (annotationController == null)
            {
                return;
            }

            InitializeWithTarget(point, annotationController);
        }

        private void InitializeWithTarget(Point point, DocumentController target)
        {
            var pdfView = ParentOverlay.GetFirstAncestorOfType<PdfView>();
            var width = pdfView?.PdfMaxWidth ??
                        ParentOverlay.GetFirstAncestorOfType<DocumentView>().ActualWidth;
            var height = pdfView?.PdfTotalHeight ??
                         ParentOverlay.GetFirstAncestorOfType<DocumentView>().ActualHeight;

            var dvm = new DocumentViewModel(target)
            {
                Undecorated = true,
                ResizersVisible = true,
                DragBounds = new RectangleGeometry { Rect = new Rect(0, 0, width, height) }
            };
            (ParentOverlay.DataContext as NewAnnotationOverlayViewModel).ViewModels.Add(dvm);

            // bcz: should this be called in LoadPinAnnotations as well?
            dvm.DocumentController.AddFieldUpdatedListener(KeyStore.GoToRegionLinkKey,
                delegate (DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args, Context context)
                {
                    if (args.NewValue != null)
                    {
                        var regionDef = (args.NewValue as DocumentController).GetDataDocument()
                            .GetField<DocumentController>(KeyStore.LinkDestinationKey).GetDataDocument().GetRegionDefinition();
                        var pos = regionDef.GetPosition().Value;
                        pdfView?.ScrollToPosition(pos.Y);
                        dvm.DocumentController.RemoveField(KeyStore.GoToRegionLinkKey);
                    }
                });
            ParentOverlay.MainDocument.GetDataDocument()
                .GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.PinAnnotationsKey)
                .Add(dvm.DocumentController);

            DocumentController = ParentOverlay.MakeAnnotationPinDoc(point, target);
        }

        private async Task<DocumentController> CreateVideoPin(Point point)
        {
            var video = await MainPage.Instance.GetVideoFile();
            if (video == null) return null;

            DocumentController videoNote = null;

            // we may get a URL or a storage file -- I had a hard time with getting a StorageFile from a URI, so unfortunately right now they're separated
            switch (video.Type)
            {
                case VideoType.StorageFile:
                    videoNote = await new VideoToDashUtil().ParseFileAsync(video.File);
                    break;
                case VideoType.Uri:
                    var query = HttpUtility.ParseQueryString(video.Uri.Query);
                    var videoId = string.Empty;

                    if (query.AllKeys.Contains("v"))
                    {
                        videoId = query["v"];
                    }
                    else
                    {
                        videoId = video.Uri.Segments.Last();
                    }

                    try
                    {
                        var url = await YouTube.GetVideoUriAsync(videoId, YouTubeQuality.Quality1080P);
                        var uri = url.Uri;
                        videoNote = VideoToDashUtil.CreateVideoBoxFromUri(uri);
                    }
                    catch (Exception)
                    {
                        // TODO: display error video not found
                    }

                    break;
            }

            if (videoNote == null) return null;

            videoNote.SetField(KeyStore.LinkTargetPlacement, new TextController(nameof(LinkTargetPlacement.Overlay)), true);
            videoNote.SetField(KeyStore.WidthFieldKey, new NumberController(250), true);
            videoNote.SetField(KeyStore.HeightFieldKey, new NumberController(200), true);
            videoNote.SetField(KeyStore.PositionFieldKey, new PointController(point.X + 10, point.Y + 10), true);

            return videoNote;
        }

        private async Task<DocumentController> CreateImagePin(Point point)
        {
            var file = await MainPage.Instance.GetImageFile();
            if (file == null) return null;

            var imageNote = await new ImageToDashUtil().ParseFileAsync(file);
            imageNote.SetField(KeyStore.LinkTargetPlacement, new TextController(nameof(LinkTargetPlacement.Overlay)), true);
            imageNote.SetField(KeyStore.WidthFieldKey, new NumberController(250), true);
            imageNote.SetField(KeyStore.HeightFieldKey, new NumberController(200), true);
            imageNote.SetField(KeyStore.PositionFieldKey, new PointController(point.X + 10, point.Y + 10), true);

            return imageNote;
        }

        /// <summary>
        /// Creates a pushpin annotation with a text note, and returns its DocumentController for CreatePin to finish the process.
        /// </summary>
        /// <param name="point"></param>
        private DocumentController CreateTextPin(Point point)
        {
            var richText = new RichTextNote("<annotation>", new Point(point.X + 10, point.Y + 10),
                new Size(150, 75));
            richText.Document.SetField(KeyStore.BackgroundColorKey, new TextController(Colors.White.ToString()), true);
            richText.Document.SetField(KeyStore.LinkTargetPlacement, new TextController(nameof(LinkTargetPlacement.Overlay)), true);

            return richText.Document;
        }


        public override void Render()
        {
            var point = DocumentController.GetPosition() ?? new Point(0, 0);
            point.X -= 10;
            point.Y -= 10;
            var pin = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = new SolidColorBrush(Colors.OrangeRed),
                IsDoubleTapEnabled = false
            };
            var vm = new SelectionViewModel(DocumentController, new SolidColorBrush(Color.FromArgb(128, 255, 0, 0)),
                new SolidColorBrush(Colors.OrangeRed));

            InitializeAnnotationObject(pin, point, PlacementMode.Bottom, vm);
        }

        protected override void InitializeAnnotationObject(Shape shape, Point pos, PlacementMode mode, SelectionViewModel vm)
        {
            shape.DataContext = vm;
            Canvas.SetLeft(shape, pos.X - shape.Width / 2);
            Canvas.SetTop(shape, pos.Y - shape.Height / 2);
            ParentOverlay.XAnnotationCanvas.Children.Add(shape);
            var tip = new ToolTip
            {
                Placement = mode
            };
            ToolTipService.SetToolTip(shape, tip);

            shape.PointerExited += (s, e) => tip.IsOpen = false;
            shape.PointerEntered += (s, e) =>
            {
                tip.IsOpen = true;
                //update tag content based on current tags of region
                var tags = new ObservableCollection<string>();

                foreach (var link in DocumentController.GetDataDocument().GetLinks(null))
                {
                    var currTags = link.GetDataDocument().GetLinkTags()?.TypedData ?? new List<TextController>();
                    foreach (var text in currTags)
                    {
                        tags.Add(text.Data);
                    }
                }

                var content = tags.Count == 0 ? "" : tags[0];
                if (tags.Count > 0)
                    tags.Remove(tags[0]);
                foreach (var str in tags)
                {
                    content = content + ", " + str;
                }
                tip.Content = content;
            };
            shape.Tapped += (sender, args) =>
            {
                if (this.IsCtrlPressed() && this.IsAltPressed())
                {
                    ParentOverlay.XAnnotationCanvas.Children.Remove(shape);
                    ParentOverlay.RegionDocsList.Remove(DocumentController);
                }
                SelectRegionFromParent(vm, args.GetPosition(this));
                args.Handled = true;
            };

            shape.PointerPressed += (s, e) => e.Handled = true;

            //handlers for moving pin
            shape.ManipulationMode = ManipulationModes.All;
            shape.ManipulationStarted += (s, e) =>
            {
                shape.ManipulationMode = ManipulationModes.All;
                e.Handled = true;
            };
            shape.ManipulationDelta += (s, e) =>
            {
                DocumentController.SetPosition(new Point(Canvas.GetLeft(shape) + e.Delta.Translation.X, Canvas.GetTop(shape) + e.Delta.Translation.Y));
                var p = Util.DeltaTransformFromVisual(e.Delta.Translation, s as UIElement);
                Canvas.SetLeft(shape, Canvas.GetLeft(shape) + p.X);
                Canvas.SetTop(shape, Canvas.GetTop(shape) + p.Y);
                e.Handled = true;
            };

            FormatRegionOptionsFlyout(DocumentController, shape);

            //formatting bindings
            shape.SetBinding(Shape.FillProperty, new Binding
            {
                Path = new PropertyPath(nameof(vm.SelectionColor)),
                Mode = BindingMode.OneWay
            });
            shape.SetBinding(VisibilityProperty, new Binding
            {
                Source = this,
                Path = new PropertyPath(nameof(NewAnnotationOverlay.AnnotationVisibility)),
                Converter = new BoolToVisibilityConverter(),
                Mode = BindingMode.OneWay
            });

            ParentOverlay.Regions.Add(vm);
        }

        #region Unimplemented Methods
        public override void StartAnnotation(Point p)
        {
        }

        public override void UpdateAnnotation(Point p)
        {
        }

        public override void EndAnnotation(Point p)
        {
        }

        public override double AddSubregionToRegion(DocumentController region)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
