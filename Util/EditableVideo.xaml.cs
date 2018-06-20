using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
	public sealed partial class EditableVideo : UserControl, IAnnotationEnabled
	{
		private MediaPlayerElement _video;

		public EditableVideo(DocumentController docController, Context context)
		{
			this.InitializeComponent();
			_video = new MediaPlayerElement
			{
				//set autoplay to false so the vid doesn't play automatically
				AutoPlay = false,
				AreTransportControlsEnabled = true,
				MinWidth = 250,
				MinHeight = 100
			};

			// setup bindings on the video
			//SetupBindings(video, docController, context);
			SetupVideoBinding(_video, docController, context);
		}

		public MediaPlayerElement GetMediaPlayerElement()
		{
			return _video;
		}

		public void RegionSelected(object region, Point pt, DocumentController chosenDoc = null)
		{
			Debug.WriteLine("REGION SELECTED VIDEO");
		}

		protected static void SetupVideoBinding(MediaPlayerElement video, DocumentController controller,
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
						//bind the MediaPlayerElement source to the new video
						BindVideoSource(video, doc, c, reference.FieldKey);
					});
			}
			BindVideoSource(video, controller, context, KeyStore.DataKey);
		}

		/// <summary>
		///   Binds the source of the MediaPlayerElement to the IMediaPlayBackSource of the video.
		/// </summary>
		protected static void BindVideoSource(MediaPlayerElement video, DocumentController docController, Context context,
			KeyController key)
		{
			var data = docController.GetDereferencedField(key, context) as VideoController;
			Debug.Assert(data != null);
			var binding = new FieldBinding<VideoController>
			{
				Document = docController,
				Key = KeyStore.DataKey,
				Mode = BindingMode.OneWay,
				Context = context,
				//converts uri to source data of the MediaPlayerElement
				Converter = Dash.Converters.UriToIMediaPlayBackSourceConverter.Instance
			};
			//bind to source property of MediaPlayerElement
			video.AddFieldBinding(MediaPlayerElement.SourceProperty, binding);
		}
	}
}
