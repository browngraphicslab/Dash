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
        
        public PinAnnotation(NewAnnotationOverlay parent, DocumentController regionDocumentController) : 
            base(parent, regionDocumentController)
        {
            this.InitializeComponent();

            AnnotationType = AnnotationType.Pin;

            InitializeAnnotationObject(xShape, null, PlacementMode.Top);

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
                var curPos = RegionDocumentController.GetPosition() ?? new Point();
                var p = Util.DeltaTransformFromVisual(e.Delta.Translation, s as UIElement);
                RegionDocumentController.SetPosition(new Point(curPos.X +p.X, curPos.Y + p.Y));
                e.Handled = true;
            };
        }

        /// <summary>
        /// Creates a target annotation for a pushpin.  If Ctrl is pressed, then the user can choose the type of annotation, 
        /// otherwise the default is text.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static async Task<DocumentController> CreateTarget(NewAnnotationOverlay parent, Point point)
        {
            // the user can gain more control over what kind of pushpin annotation they want to make by holding control, which triggers a popup
            switch (parent.IsCtrlPressed() ? await MainPage.Instance.GetPushpinType() : PushpinType.Text)
            {
                case PushpinType.Text: return CreateTextPin(point);
                case PushpinType.Video: return await CreateVideoPin(point);
                case PushpinType.Image: return await CreateImagePin(point);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        static async Task<DocumentController> CreateVideoPin(Point point)
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

        static async Task<DocumentController> CreateImagePin(Point point)
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
        static DocumentController CreateTextPin(Point point)
        {
            var richText = new RichTextNote("<annotation>", new Point(point.X + 10, point.Y + 10), new Size(150, 75));
            richText.Document.SetField(KeyStore.BackgroundColorKey, new TextController(Colors.White.ToString()), true);

            return richText.Document;
        }
        
        public override void Render(SelectionViewModel vm)
        {
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
