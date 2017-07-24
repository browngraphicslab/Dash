using System;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Dash;
using DashShared;


namespace Dash
{

    /// <summary>
    /// A generic document type containing a single image. The Data field on an ImageBox is a reference which eventually ends in an
    /// ImageFieldModelController or an ImageFieldModelController
    /// </summary>
    public class ImageBox : CourtesyDocument
    {

        public static DocumentType DocumentType = new DocumentType("3A6F92CC-D8DC-448B-9D3E-A1E04C2C77B3", "Image Box");
        public static Key OpacityKey = new Key("78DB67E4-4D9F-47FA-980D-B8EEE87C4351", "Opacity Key");
        public static double DefaultOpacity = 1;
        public static Uri DefaultImageUri => new Uri("ms-appx://Dash/Assets/DefaultImage.png");
        private static string PrototypeId = "ABDDCBAF-20D7-400E-BE2E-3761313520CC";

        public ImageBox(FieldModelController refToImage, double x = 0, double y = 0, double w = 200, double h = 200)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToImage);
            Document = GetLayoutPrototype().MakeDelegate();
            Document.SetFields(fields, true);
            SetOpacityField(Document, DefaultOpacity, true, null);
        }

        public override FrameworkElement makeView(DocumentController docController, Context context,
            bool isInterfaceBuilderLayout = false)
        {
            return MakeView(docController, context);
        }


        public static FrameworkElement MakeView(DocumentController docController, Context context,
            bool isInterfaceBuilderLayout = false)
        {
            // use the reference to the image to get the image field model controller
            var imFieldModelController = GetImageField(docController, context);
            Debug.Assert(imFieldModelController != null);

            // create the image
            var image = new Image
            {
                Stretch = Stretch.Fill // set image to fill container but ignore aspect ratio :/

            };
            image.CacheMode = new BitmapCache();

            // make image source update when changed
            BindSource(image, imFieldModelController);

            // make image height resize
            var heightController = GetHeightField(docController, context);
            BindHeight(image, heightController);

            // make image width resize
            var widthController = GetWidthField(docController, context);
            BindWidth(image, widthController);

            // set up interactions with operations
            BindOperationInteractions(image, GetImageReference(docController).FieldReference.Resolve(context));

            // make image opacity change
            var opacityController = GetOpacityField(docController, context);
            BindOpacity(image, opacityController);

            if (isInterfaceBuilderLayout)
            {
                return new SelectableContainer(image, docController);
            }
            return image;
        }

        protected override DocumentController GetLayoutPrototype()
        {
            var prototype = ContentController.GetController<DocumentController>(PrototypeId);
            if (prototype == null)
            {
                prototype = InstantiatePrototypeLayout();
            }
            return prototype;
        }

        protected override DocumentController InstantiatePrototypeLayout()
        {
            var imFieldModelController = new ImageFieldModelController(DefaultImageUri);
            var fields = DefaultLayoutFields(new Point(), new Size(double.NaN, double.NaN), imFieldModelController);
            var prototypeDocument = new DocumentController(fields, DocumentType, PrototypeId);
            SetOpacityField(prototypeDocument, DefaultOpacity, true, null);
            return prototypeDocument;
        }

        #region FieldGettersAndSetters

        private static NumberFieldModelController GetOpacityField(DocumentController docController, Context context)
        {
            context = Context.SafeInitAndAddDocument(context, docController);
            return docController.GetField(OpacityKey)?
                .DereferenceToRoot<NumberFieldModelController>(context);
        }


        private static void SetOpacityField(DocumentController docController, double opacity, bool forceMask,
            Context context)
        {
            var currentOpacityField = new NumberFieldModelController(opacity);
            docController.SetField(OpacityKey, currentOpacityField,
                forceMask); // set the field here so that forceMask is respected

        }

        private static ImageFieldModelController GetImageField(DocumentController docController, Context context)
        {
            context = Context.SafeInitAndAddDocument(context, docController);
            return docController.GetField(DashConstants.KeyStore.DataKey)
                .DereferenceToRoot<ImageFieldModelController>(context);
        }

        private static ReferenceFieldModelController GetImageReference(DocumentController docController)
        {
            return docController.GetField(DashConstants.KeyStore.DataKey) as ReferenceFieldModelController;
        }

        #endregion

        #region Bindings

        private static void BindSource(FrameworkElement renderElement, ImageFieldModelController imageField)
        {
            var sourceBinding = new Binding
            {
                Source = imageField,
                Path = new PropertyPath(nameof(imageField.Data)),
                Mode = BindingMode.OneWay,
            };
            renderElement.SetBinding(Image.SourceProperty, sourceBinding);
        }

        private static void BindOpacity(FrameworkElement renderElement, NumberFieldModelController opacityController)
        {
            var opacityBinding = new Binding
            {
                Source = opacityController,
                Path = new PropertyPath(nameof(opacityController.Data)),
                Mode = BindingMode.OneWay
            };
            renderElement.SetBinding(UIElement.OpacityProperty, opacityBinding);
        }

        #endregion

    }

}