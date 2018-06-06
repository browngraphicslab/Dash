using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using DashShared;
using Windows.UI.Xaml.Media;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.Storage;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Media.Imaging;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;

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
            (fields[KeyStore.HorizontalAlignmentKey] as TextController).Data = HorizontalAlignment.Left.ToString();
            (fields[KeyStore.VerticalAlignmentKey] as TextController).Data = VerticalAlignment.Top.ToString();
            SetupDocument(DocumentType, PrototypeId, "ImageBox Prototype Layout", fields);
        }
        public static FrameworkElement MakeView(DocumentController docController, Context context)
        {
            // create the image

           var editableImage = new EditableImage(docController.GetDereferencedField(KeyStore.DataKey, context) as ImageController);
           
            var image = editableImage.Image;

            // setup bindings on the image
            SetupBindings(image, docController, context);
            SetupImageBinding(image, docController, context);
          

            return editableImage;
            //cropping logic here
        }

        

        protected static void SetupImageBinding(Image image, DocumentController controller,
            Context context)
        {
            var data = controller.GetField(KeyStore.DataKey);
            if (data is ReferenceController)
            {
                var reference = data as ReferenceController;
                var dataDoc = reference.GetDocumentController(context);
                dataDoc.AddFieldUpdatedListener(reference.FieldKey,
                    delegate(FieldControllerBase sender, FieldUpdatedEventArgs args, Context c)
                    {
                        var doc = (DocumentController) sender;
                        var dargs =
                            (DocumentController.DocumentFieldUpdatedEventArgs) args;
                        if (args.Action == DocumentController.FieldUpdatedAction.Update || dargs.FromDelegate)
                            return;
                        BindImageSource(image, doc, c, reference.FieldKey);
                    });
            }
            BindImageSource(image, controller, context, KeyStore.DataKey);
        }

       public void cropImage(RectangleGeometry rectgeo)
        {
            //rectgeo.Rect.
        }

        protected static void BindImageSource(Image image, DocumentController docController, Context context,
            KeyController key)
        {
            var data = docController.GetDereferencedField(key, context) as ImageController;
            if (data == null)
                return;
            var binding = new FieldBinding<ImageController>
            {
                Document = docController,
                Key = KeyStore.DataKey,
                Mode = BindingMode.OneWay,
                Context = context,
                Converter = UriToBitmapImageConverter.Instance
            };
            image.AddFieldBinding(Image.SourceProperty, binding);
        }
    }
}