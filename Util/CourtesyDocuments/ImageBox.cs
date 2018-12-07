using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using DashShared;
using Dash.Converters;

namespace Dash
{
    /// <summary>
    ///     A generic document type containing a single image. The Data field on an ImageBox is a reference which eventually
    ///     ends in an
    ///     ImageController or an ImageController
    /// </summary>
    public class ImageBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("3A6F92CC-D8DC-448B-9D3E-A1E04C2C77B3", "Image Box");
        private static readonly string PrototypeId = "ABDDCBAF-20D7-400E-BE2E-3761313520CC";
        private static Uri DefaultImageUri => new Uri("ms-appx://Dash/Assets/DefaultImage.png");

        public ImageBox(FieldControllerBase refToImage, double x = 0, double y = 0, double w = 200, double h = 200)
        {
			var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToImage);
            SetupDocument(DocumentType, PrototypeId, "ImageBox Prototype Layout", fields);
        }

        public static FrameworkElement MakeView(DocumentController docController, KeyController key, Context context)
        {
            // create the image
            var editableImage = new EditableImage();
            // setup bindings on the image
            SetupBinding(editableImage, docController, key, context);

            return editableImage;
        }

	    public static void SetupBinding(EditableImage editableImage, DocumentController controller, KeyController key, Context context)
        {
            editableImage.DataFieldKey = key;
            BindImageSource(editableImage, controller, key, context);
        }

        protected static void BindImageSource(EditableImage editableImage, DocumentController docController, KeyController key, Context context)
        {
            var binding = new FieldBinding<ImageController>
            {
                Document = docController,
                Key = key,
                Mode = BindingMode.OneWay,
                Context = context,
                Converter = UriToBitmapImageConverter.Instance
            };
            editableImage.Image.AddFieldBinding(Image.SourceProperty, binding);
            var binding2 = new FieldBinding<TextController>
            {
                Document = docController,
                Key = KeyStore.ImageStretchKey,
                Mode = BindingMode.OneWay,
                Context = context,
                Converter = new StringToEnumConverter<Stretch>(),
                FallbackValue = Stretch.Uniform
            };
            editableImage.Viewbox.AddFieldBinding(Viewbox.StretchProperty, binding2);
        }

	    public static Task<DocumentController> MakeRegionDocument(DocumentView image, Point? point)
	    {
		    return image.GetFirstDescendantOfType<EditableImage>().GetRegionDocument(point);
	    }
	}
}
