using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using DashShared;
using DashShared.Models;

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
            Document = GetLayoutPrototype().MakeDelegate();
            Document.SetFields(fields, true);
            Document.SetField(KeyStore.DocumentContextKey,
                (refToImage as ReferenceController).GetDocumentController(null), true);
        }

        public override FrameworkElement makeView(DocumentController docController, Context context,
            bool isInterfaceBuilderLayout = false)
        {
            return MakeView(docController, context);
        }
        public static FrameworkElement MakeView(DocumentController docController, Context context,
            Dictionary<KeyController, FrameworkElement> keysToFrameworkElementsIn = null,
            bool isInterfaceBuilderLayout = false)
        {
            // create the image
            var editableImage = new EditableImage();
            var image = editableImage.Image;

            // setup bindings on the image
            SetupBindings(image, docController, context);
            SetupImageBinding(image, docController, context);

            //add to key to framework element dictionary
            if (docController.GetField(KeyStore.DataKey) is ReferenceController reference)
                if (keysToFrameworkElementsIn != null) keysToFrameworkElementsIn[reference.FieldKey] = image;

            if (isInterfaceBuilderLayout)
            {
                editableImage.IsHitTestVisible = false;
                var selectableContainer = new SelectableContainer(editableImage, docController);
                return selectableContainer;
            }

            return editableImage;
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

        protected override DocumentController GetLayoutPrototype()
        {
            return ContentController<FieldModel>.GetController<DocumentController>(PrototypeId) ??
                   InstantiatePrototypeLayout();
        }

        protected override DocumentController InstantiatePrototypeLayout()
        {
            var imController = new ImageController(DefaultImageUri);
            var fields = DefaultLayoutFields(new Point(), new Size(double.NaN, double.NaN), imController);
            var prototypeDocument = new DocumentController(fields, DocumentType, PrototypeId);
            return prototypeDocument;
        }
    }
}