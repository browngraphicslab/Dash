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
        public static DocumentType DocumentType = new DocumentType("7C4D8D1A-4E2B-45F4-A148-17EAFB4356B2", "Audio Box");
        private static readonly string PrototypeId = "513A5CEB-90FE-45A6-911E-1E46E933B553";
        private static Uri DefaultAudiooUri => new Uri("ms-appx://Dash/Assets/DefaultAudio.mp3");

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
                AreTransportControlsEnabled = true
            };

            // setup bindings on the audio
            SetupBindings(audio, docController, context);
            SetupAudioBinding(audio, docController, context);

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
                    delegate (FieldControllerBase sender, FieldUpdatedEventArgs args, Context c)
                    {
                        var doc = (DocumentController)sender;
                        var dargs =
                            (DocumentController.DocumentFieldUpdatedEventArgs)args;
                        if (args.Action == DocumentController.FieldUpdatedAction.Update || dargs.FromDelegate)
                            return;
                        //bind the MediaPlayerElement source to the new audio
                        BindAudioSource(audio, doc, c, reference.FieldKey);
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
    }
}



