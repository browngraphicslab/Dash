using DashShared;
using Windows.Foundation;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using System.Diagnostics;
using Windows.System;
using Dash.Converters;

namespace Dash
{
    class AudioBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("41D7DF1F-4E3B-4770-9041-36835FF171FC", "Audio Box");
        private static readonly string PrototypeId = "77E99E16-560C-4BB4-9FCD-E7A6F8CD5517";
        private static Uri DefaultAudiooUri => new Uri("ms-appx://Dash/Assets/DefaultAudio.mp3");
        private static MediaPlayerElement _audioplayer;

        public AudioBox(FieldControllerBase refToAudio, double x = 0, double y = 0, double w = 320, double h = 180)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToAudio);
            SetupDocument(DocumentType, PrototypeId, "AudioBox Prototype Layout", fields);
        }

        /// <summary>
        ///   Creates a MediaPlayerElement that will be binded to audio reference.
        /// </summary>
        public static FrameworkElement MakeView(DocumentController docController, KeyController key)
        {
            //create the media player element 
            MediaPlayerElement audio = new MediaPlayerElement
            {
                //set autoplay to false so the vid doesn't play automatically
                AutoPlay = false,
                AreTransportControlsEnabled = true,
                MinWidth = 200,
                MinHeight = 50
            };

	        //enables fullscreen exit with escape shortcut
	        audio.KeyDown += (s, e) =>
	        {
		        if (e.Key == VirtualKey.Escape && audio.IsFullWindow)
		        {
			        audio.IsFullWindow = false;
		        }
	        };
			

			// setup bindings on the audio
            BindAudioSource(audio, docController, key);
            audio.TransportControls.IsFullWindowEnabled = false;
            audio.TransportControls.IsFullWindowButtonVisible = false;

			//disables audio's fullscreen mode
	        audio.TransportControls.IsFullWindowEnabled = false;
	        audio.TransportControls.IsFullWindowButtonVisible = false;

            return audio;
        }

        /// <summary>
        ///   Binds the source of the MediaPlayerElement to the IMediaPlayBackSource of the audio.
        /// </summary>
        protected static void BindAudioSource(MediaPlayerElement audio, DocumentController docController, KeyController key)
        {
            var binding = new FieldBinding<AudioController>
            {
                Document = docController,
                Key = key,
                Mode = BindingMode.OneWay,
                //converts uri to source data of the MediaPlayerElement
                Converter = UriToIMediaPlayBackSourceConverter.Instance
            };
            //bind to source property of MediaPlayerElement
            audio.AddFieldBinding(MediaPlayerElement.SourceProperty, binding);
        }

        public void setMargin(Double x)
        {
            Thickness margin = new Thickness(0, x, 0, x);
            _audioplayer.Margin = margin;
        }
    }
}



