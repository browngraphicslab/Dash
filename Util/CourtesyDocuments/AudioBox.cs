using DashShared;
using Windows.Foundation;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using System.Diagnostics;
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
            (fields[KeyStore.HorizontalAlignmentKey] as TextController).Data = HorizontalAlignment.Left.ToString();
            (fields[KeyStore.VerticalAlignmentKey] as TextController).Data = VerticalAlignment.Top.ToString();
            SetupDocument(DocumentType, PrototypeId, "AudioBox Prototype Layout", fields);
        }

        /// <summary>
        ///   Creates a MediaPlayerElement that will be binded to audio reference.
        /// </summary>
        public static FrameworkElement MakeView(DocumentController docController, Context context)
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

            _audioplayer = audio;

            // setup bindings on the audio
            SetupBindings(audio, docController, context);
            SetupAudioBinding(audio, docController, context);
            audio.TransportControls.IsFullWindowEnabled = false;
            audio.TransportControls.IsFullWindowButtonVisible = false;

            return audio;
        }

        protected static void SetupAudioBinding(MediaPlayerElement audio, DocumentController controller,
            Context context)
        {
            var data = controller.GetField(KeyStore.DataKey);
            if (data is ReferenceController reference)
            {
                //add fieldUpdatedListener to the doc controller of the reference
                var dataDoc = reference.GetDocumentController(context);
                dataDoc.AddFieldUpdatedListener(reference.FieldKey,
                    delegate (DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args, Context c)
                    {
                        if (args.Action == DocumentController.FieldUpdatedAction.Update || args.FromDelegate)
                            return;
                        //bind the MediaPlayerElement source to the new audio
                        BindAudioSource(audio, sender, c, reference.FieldKey);
                    });
            }
            BindAudioSource(audio, controller, context, KeyStore.DataKey);
        }

        /// <summary>
        ///   Binds the source of the MediaPlayerElement to the IMediaPlayBackSource of the audio.
        /// </summary>
        protected static void BindAudioSource(MediaPlayerElement audio, DocumentController docController, Context context,
            KeyController key)
        {
            var data = docController.GetDereferencedField(key, context) as AudioController;
            Debug.Assert(data != null);
            var binding = new FieldBinding<AudioController>
            {
                Document = docController,
                Key = KeyStore.DataKey,
                Mode = BindingMode.OneWay,
                Context = context,
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



