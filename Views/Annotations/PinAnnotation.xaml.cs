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
        public PinAnnotation(NewAnnotationOverlay parent, Point? point=null, DocumentController target = null) : base(parent)
        {
            this.InitializeComponent();

            if (point != null)
            {
                if (target == null)
                {
                    Initialize(point.Value);
                }
                else
                {
                    InitializeWithTarget(point.Value, target);
                }
            }

            AnnotationType = AnnotationType.Pin;

            var xToolTip = new ToolTip();
            ToolTipService.SetToolTip(this, xToolTip);

            PointerExited += (s, e) => xToolTip.IsOpen = false;
            PointerEntered += (s, e) =>
            {
                if (!xToolTip.IsOpen)
                    xToolTip.IsOpen = true;
                //update tag content based on current tags of region
                var tags = new List<string>();

                foreach (var link in DocumentController.GetDataDocument().GetLinks(null))
                {
                    var currTag = link.GetDataDocument().GetLinkTag();
                    if (currTag != null)
                    {
                        tags.Add(currTag.Data);
                    }
                }

                var content = tags.Count == 0 ? "" : tags[0];
                if (tags.Count > 0)
                    tags.Remove(tags[0]);
                foreach (var str in tags)
                {
                    content = content + ", " + str;
                }
                xToolTip.Content = content;
            };

            PointerPressed += (s, e) => e.Handled = true;

            //handlers for moving pin
            ManipulationMode = ManipulationModes.All;
            ManipulationStarted += (s, e) =>
            {
                ManipulationMode = ManipulationModes.All;
                e.Handled = true;
            };
            ManipulationDelta += (s, e) =>
            {
                DocumentController.SetPosition(new Point(Canvas.GetLeft(this) + e.Delta.Translation.X, Canvas.GetTop(this) + e.Delta.Translation.Y));
                var p = Util.DeltaTransformFromVisual(e.Delta.Translation, s as UIElement);
                Canvas.SetLeft(this, Canvas.GetLeft(this) + p.X);
                Canvas.SetTop(this, Canvas.GetTop(this) + p.Y);
                e.Handled = true;
            };

            Tapped += (sender, args) =>
            {
                if (this.IsCtrlPressed() && this.IsAltPressed())
                {
                    ParentOverlay.XAnnotationCanvas.Children.Remove(this);
                    ParentOverlay.RegionDocsList.Remove(DocumentController);
                }
                SelectRegionFromParent(ViewModel, args.GetPosition(this));
                args.Handled = true;
            };
            //formatting bindings
            xShape.SetBinding(Shape.FillProperty, new Binding
            {
                Path = new PropertyPath(nameof(ViewModel.SelectionColor)),
                Mode = BindingMode.OneWay
            });
        }

        public async void Initialize(Point point)
        {
            DocumentController annotationController;

            if (!ParentOverlay.XAnnotationCanvas.Children.OfType<PinAnnotation>().Where((pin) => pin.GetBoundingRect(ParentOverlay).Contains(point)).Any())
            {
                // the user can gain more control over what kind of pushpin annotation they want to make by holding control, which triggers a popup
                if (this.IsCtrlPressed())
                {
                    switch (await MainPage.Instance.GetPushpinType())
                    {
                        case PushpinType.Text:  annotationController = CreateTextPin(point);
                            break;
                        case PushpinType.Video: annotationController = await CreateVideoPin(point);
                            break;
                        case PushpinType.Image: annotationController = await CreateImagePin(point);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else
                {
                    annotationController = CreateTextPin(point);  // by default the pushpin will create a text note
                }

                // if the user presses back or cancel, return null
                if (annotationController != null)
                {
                    InitializeWithTarget(point, annotationController);
                }
            }
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
            var richText = new RichTextNote("<annotation>", new Point(point.X + 10, point.Y + 10), new Size(150, 75));
            richText.Document.SetField(KeyStore.BackgroundColorKey, new TextController(Colors.White.ToString()), true);

            return richText.Document;
        }

        SelectionViewModel ViewModel => DataContext as SelectionViewModel;

        public override void Render(SelectionViewModel vm)
        {
            DataContext = vm;
            var pos = DocumentController.GetPosition() ?? new Point();
            Canvas.SetLeft(this, pos.X - xShape.Width / 2);
            Canvas.SetTop(this,  pos.Y - xShape.Height / 2);
            
            FormatRegionOptionsFlyout(DocumentController, this);

            ParentOverlay.Regions.Add(vm);
            ParentOverlay.XAnnotationCanvas.Children.Add(this);
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
