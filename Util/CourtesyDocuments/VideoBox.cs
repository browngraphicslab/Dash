using DashShared;
using Windows.Foundation;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using System.Diagnostics;
using Windows.System;
using Dash.Converters;
using Windows.UI.Xaml.Input;
using Windows.Devices.Input;

namespace Dash
{
	class VideoBox : CourtesyDocument
	{
		public static DocumentType DocumentType = new DocumentType("7C4D8D1A-4E2B-45F4-A148-17EAFB4356B2", "Video Box");
		private static readonly string PrototypeId = "513A5CEB-90FE-45A6-911E-1E46E933B553";
		private static Uri DefaultVideoUri => new Uri("ms-appx://Dash/Assets/DefaultVideo.mp4");

		public VideoBox(FieldControllerBase refToVideo, double x = 0, double y = 0, double w = 320, double h = 180)
		{
			var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToVideo);
			(fields[KeyStore.HorizontalAlignmentKey] as TextController).Data = HorizontalAlignment.Left.ToString();
			(fields[KeyStore.VerticalAlignmentKey] as TextController).Data = VerticalAlignment.Top.ToString();
            SetupDocument(DocumentType, PrototypeId, "VideoBox Prototype Layout", fields);
        }

        /// <summary>
        ///   Creates a MediaPlayerElement that will be binded to video reference.
        /// </summary>
        public static FrameworkElement MakeView(DocumentController docController, KeyController key)
        {
            //create the media player element 

            var video = new MediaPlayerElement
            {
				//set autoplay to false so the vid doesn't play automatically
				AutoPlay = false,
                AreTransportControlsEnabled = true,
                MinWidth = 200,
                MinHeight = 100
            };

            //enables fullscreen exit with escape shortcut
            video.KeyDown += (s, e) =>
            {
                if (e.Key == VirtualKey.Escape && video.IsFullWindow)
                {
                    video.IsFullWindow = false;
                    e.Handled = true;
                }
            };
            
            video.TransportControls.IsCompact = true;
            video.TransportControls.Visibility = Visibility.Collapsed;
            video.Loaded   += (s,e) => video.TransportControls.Visibility = video.GetDocumentView().IsSelected ?  Visibility.Visible : Visibility.Collapsed;
            video.Unloaded += (s,e) => video.MediaPlayer.Pause();
            video.PointerEntered += (s, e) =>
            {
                video.TransportControls.Show();
                video.TransportControls.Focus(FocusState.Programmatic);
            };
            video.PointerExited  += (s, e) => video.TransportControls.Hide();

            ManipulationControlHelper _manipulator = null;
            video.Tapped         += (s, e) => video.TransportControls.Show();
            video.PointerPressed += (s, e) =>
            {
                _manipulator = e.IsRightPressed() || !video.GetDocumentView().IsSelected ? new ManipulationControlHelper(video, true) : null;
                e.Handled = true;
            };
            video.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler((s, e) => _manipulator?.PointerMoved(s, e)), true);

            // setup bindings on the video
            SetupVideoBinding(video, docController, key);
			
			return video;
		}

		protected static void SetupVideoBinding(MediaPlayerElement video, DocumentController controller, KeyController key)
		{
			BindVideoSource(video, controller, key);
		}

		/// <summary>
		///   Binds the source of the MediaPlayerElement to the IMediaPlayBackSource of the video.
		/// </summary>
		protected static void BindVideoSource(MediaPlayerElement video, DocumentController docController, KeyController key)
		{
			var data = docController.GetDereferencedField(key, null) as VideoController;
			Debug.Assert(data != null);
			var binding = new FieldBinding<VideoController>
			{
				Document = docController,
				Key = key,
				Mode = BindingMode.OneWay,
				//converts uri to source data of the MediaPlayerElement
				Converter = UriToIMediaPlayBackSourceConverter.Instance
			};
			//bind to source property of MediaPlayerElement
			video.AddFieldBinding(MediaPlayerElement.SourceProperty, binding);
		}

		
	}
}



