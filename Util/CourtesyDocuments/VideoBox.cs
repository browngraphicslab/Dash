﻿using DashShared;
using Windows.Foundation;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using System.Diagnostics;
using Dash.Converters;

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
			Document = GetLayoutPrototype().MakeDelegate();
			Document.SetFields(fields, true);
			//set field to the doc controller of the reference
			Document.SetField(KeyStore.DocumentContextKey,
				(refToVideo as ReferenceController).GetDocumentController(null), true);
		}

		public override FrameworkElement makeView(DocumentController docController, Context context)
		{
			return MakeView(docController, context);
		}

		/// <summary>
		///   Creates a MediaPlayerElement that will be binded to video reference.
		/// </summary>
		public static FrameworkElement MakeView(DocumentController docController, Context context)
		{
			//create the media player element 
			MediaPlayerElement video = new MediaPlayerElement
			{
				//set autoplay to false so the vid doesn't play automatically
				AutoPlay = false,
				AreTransportControlsEnabled = true
			};

			// setup bindings on the video
			SetupBindings(video, docController, context);
			SetupVideoBinding(video, docController, context);

			return video;
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
				Converter = UriToIMediaPlayBackSourceConverter.Instance
			};
			//bind to source property of MediaPlayerElement
			video.AddFieldBinding(MediaPlayerElement.SourceProperty, binding);
		}

		protected override DocumentController GetLayoutPrototype()
		{
			return ContentController<FieldModel>.GetController<DocumentController>(PrototypeId) ??
				   InstantiatePrototypeLayout();
		}

		protected override DocumentController InstantiatePrototypeLayout()
		{
			//create new video controller w/ default uri
			var vidController = new VideoController(DefaultVideoUri);
			var fields = DefaultLayoutFields(new Point(), new Size(double.NaN, double.NaN), vidController);
			var prototypeDocument = new DocumentController(fields, DocumentType, PrototypeId);
			return prototypeDocument;
		}
	}
}



