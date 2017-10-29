using System;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Dash;
using DashShared;
using System.Collections.Generic;

namespace Dash
{

    /// <summary>
    /// A generic document type containing a single image. The Data field on an ImageBox is a reference which eventually ends in an
    /// ImageFieldModelController or an ImageFieldModelController
    /// </summary>
    public class ImageBox : CourtesyDocument
    {

        public static DocumentType DocumentType = new DocumentType("3A6F92CC-D8DC-448B-9D3E-A1E04C2C77B3", "Image Box");
        public static readonly KeyController OpacityKey = new KeyController("78DB67E4-4D9F-47FA-980D-B8EEE87C4351", "Opacity Key");
        public static readonly KeyController ClipKey = new KeyController("8411212B-D56B-4B08-A0B3-094876D2BED2", "Clip Location Key");
        private const double DefaultOpacity = 1;
        private readonly RectangleGeometry _defaultClip = new RectangleGeometry { Rect = new Rect(0, 0, 100, 100) };
        private static Uri DefaultImageUri => new Uri("ms-appx://Dash/Assets/DefaultImage.png");
        private static string PrototypeId = "ABDDCBAF-20D7-400E-BE2E-3761313520CC";

        public ImageBox(FieldControllerBase refToImage, double x = 0, double y = 0, double w = 200, double h = 200)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToImage);
            (fields[KeyStore.HorizontalAlignmentKey] as TextFieldModelController).Data = HorizontalAlignment.Left.ToString();
            (fields[KeyStore.VerticalAlignmentKey] as TextFieldModelController).Data = VerticalAlignment.Top.ToString();
            Document = GetLayoutPrototype().MakeDelegate();
            Document.SetFields(fields, true);
            SetOpacityField(Document, DefaultOpacity, true, null);
            SetClipField(Document, _defaultClip, true, null);
        }

        private static void SetClipField(DocumentController docController, RectangleGeometry defaultClip, bool forceMask, Context context)
        {
            var currentClipField = new RectFieldModelController(defaultClip.Rect);
            docController.SetField(ClipKey, currentClipField, forceMask);
        }

        private static void SetupBindings(Image image, DocumentController docController,
            Context context)
        {
            CourtesyDocument.SetupBindings(image, docController, context);
            AddBinding(image, docController, OpacityKey, context, BindOpacity);
            AddBinding(image, docController, ClipKey, context, BindClip);
            SetupImageBinding(image, docController, context);
        }

        public override FrameworkElement makeView(DocumentController docController, Context context,
            bool isInterfaceBuilderLayout = false)
        {
            return MakeView(docController, context);
        }

        private static EditableImage _editableImage;

        public static FrameworkElement MakeView(DocumentController docController, Context context, Dictionary<KeyController, FrameworkElement> keysToFrameworkElementsIn = null,
            bool isInterfaceBuilderLayout = false)
        {
            // create the image
            _editableImage = new EditableImage(docController, context);
            var image = _editableImage.Image;


            SetupBindings(image, docController, context);


            // set up interactions with operations
            var imageFMController = docController.GetDereferencedField(KeyStore.DataKey, context) as ImageFieldModelController;
            var reference = docController.GetField(KeyStore.DataKey) as ReferenceFieldModelController;
            BindOperationInteractions(image, GetImageReference(docController).GetFieldReference().Resolve(context), reference.FieldKey, imageFMController);

          
            if(keysToFrameworkElementsIn != null) keysToFrameworkElementsIn[reference.FieldKey] = image;

            if (isInterfaceBuilderLayout)
            {
                _editableImage.IsHitTestVisible = false;
                var selectableContainer = new SelectableContainer(_editableImage, docController);
                //SetupBindings(selectableContainer, docController, context);
                return selectableContainer;
            }
            return _editableImage;
        }

        protected static void SetupImageBinding(Image image, DocumentController controller,
            Context context)
        {
            var data = controller.GetField(KeyStore.DataKey);
            if (data is ReferenceFieldModelController)
            {
                var reference = data as ReferenceFieldModelController;
                var dataDoc = reference.GetDocumentController(context);
                dataDoc.AddFieldUpdatedListener(reference.FieldKey,
                    delegate (DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
                    {
                        if (args.Action == DocumentController.FieldUpdatedAction.Update || args.FromDelegate)
                        {
                            return;
                        }
                        BindImageSource(image, sender, args.Context, reference.FieldKey);
                    });
            }
            BindImageSource(image, controller, context, KeyStore.DataKey);
        }

        protected static void BindImageSource(Image image, DocumentController docController, Context context, KeyController key)
        {
            var data = docController.GetDereferencedField(key, context) as ImageFieldModelController;
            if (data == null)
            {
                return;
            }
            var binding = new FieldBinding<FieldControllerBase>()
            {
                Document = docController,
                Key = KeyStore.DataKey,
                Mode = BindingMode.TwoWay,
                Context = context
            };
            image.AddFieldBinding(Image.SourceProperty, binding);
        }
        //if (data.Data.UriSource == null)
        //    image.Source = data.Data;
        //else
        //{
        //var sourceBinding = new Binding
        //        {
        //            Source = data,
        //            Path = new PropertyPath(nameof(data.Data)),
        //            Mode = BindingMode.OneWay
        //        };
        //        image.SetBinding(Image.SourceProperty, sourceBinding);
           // }
        //}

        private static void BindClip(Image image, DocumentController docController, Context context)
        {
            var widthController =
                docController.GetDereferencedField(KeyStore.WidthFieldKey, context) as NumberFieldModelController;
            var heightController =
                docController.GetDereferencedField(KeyStore.HeightFieldKey, context) as NumberFieldModelController;
            var clipController =
                docController.GetDereferencedField(ClipKey, context) as RectFieldModelController;
            if (clipController == null) return;
            Debug.Assert(widthController != null);
            Debug.Assert(heightController != null);

            UpdateClip(image, clipController.Data);
            AddClipBindingEvents(clipController, widthController, heightController, image);

            // fixes vertical and horizontal stretch problems 
            image.SizeChanged += (s, e) =>
            {
                UpdateClip(image, clipController.Data);
            };
        }

        private static void AddClipBindingEvents(RectFieldModelController clipController,
            NumberFieldModelController widthController, NumberFieldModelController heightController, Image image)
        {
            clipController.FieldModelUpdated += (ss, args, cc) =>
            {
                UpdateClip(image, clipController.Data);
            };
            widthController.FieldModelUpdated += (ss, args, cc) =>
            {
                UpdateClip(image, clipController.Data);
            };
            heightController.FieldModelUpdated += (ss, args, cc) =>
            {
                UpdateClip(image, clipController.Data);
            };
        }

        public static void UpdateClip(Image image, Rect data)
        {
            Debug.Assert(image != null);
            double width = image.ActualWidth;
            double height = image.ActualHeight;
            if (width <= 0) width = image.Width;
            if (height <= 0) height = image.Height;
            image.Clip = new RectangleGeometry
            {
                Rect = new Rect(data.X * width / 100,
                                data.Y * height / 100,
                                data.Width * width / 100,
                                data.Height * height / 100)
            };
        }

        private static void BindOpacity(Image image, DocumentController docController, Context context)
        {
            var opacityController = docController.GetDereferencedField(OpacityKey, context) as NumberFieldModelController;
            if (opacityController == null)
            {
                return;
            }
            var opacityBinding = new Binding
            {
                Source = opacityController,
                Path = new PropertyPath(nameof(opacityController.Data)),
                Mode = BindingMode.OneWay
            };
            image.SetBinding(UIElement.OpacityProperty, opacityBinding);
        }

        protected override DocumentController GetLayoutPrototype()
        {
            var prototype = ContentController<DocumentModel>.GetController<DocumentController>(PrototypeId);
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

        private static void SetOpacityField(DocumentController docController, double opacity, bool forceMask, Context context)
        {
            var currentOpacityField = new NumberFieldModelController(opacity);
            docController.SetField(OpacityKey, currentOpacityField, forceMask);
            // set the field here so that forceMask is respected
        }

        private static ReferenceFieldModelController GetImageReference(DocumentController docController)
        {
            return docController.GetField(KeyStore.DataKey) as ReferenceFieldModelController;
        }

        #endregion

    }

}