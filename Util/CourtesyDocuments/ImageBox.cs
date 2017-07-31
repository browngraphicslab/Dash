﻿using System;
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
        public static readonly Key OpacityKey = new Key("78DB67E4-4D9F-47FA-980D-B8EEE87C4351", "Opacity Key");
        public static readonly Key ClipKey = new Key("8411212B-D56B-4B08-A0B3-094876D2BED2", "Clip Location Key");
        private const double DefaultOpacity = 1;
        private readonly RectangleGeometry _defaultClip = new RectangleGeometry {Rect = new Rect(0,0,100,100)};
        private static Uri DefaultImageUri => new Uri("ms-appx://Dash/Assets/DefaultImage.png");
        private static string PrototypeId = "ABDDCBAF-20D7-400E-BE2E-3761313520CC";

        public ImageBox(FieldModelController refToImage, double x = 0, double y = 0, double w = 200, double h = 200)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToImage);
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

        protected static void SetupBindings(Image image, DocumentController docController,
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


        public static FrameworkElement MakeView(DocumentController docController, Context context,
            bool isInterfaceBuilderLayout = false)
        {
            // create the image
            var image = new Image
            {
                Stretch = Stretch.Fill,// set image to fill container but ignore aspect ratio :/
                CacheMode = new BitmapCache(),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };

            SetupBindings(image, docController, context);


            // set up interactions with operations
            var imageFMController = docController.GetDereferencedField(DashConstants.KeyStore.DataKey, context) as ImageFieldModelController;
            var reference = docController.GetField(DashConstants.KeyStore.DataKey) as ReferenceFieldModelController;
            BindOperationInteractions(image, GetImageReference(docController).FieldReference.Resolve(context), reference.FieldKey, imageFMController);

            if (isInterfaceBuilderLayout)
            {
                var selectableContainer = new SelectableContainer(image, docController);
                //SetupBindings(selectableContainer, docController, context);
                return selectableContainer;
            }
            return image;
        }

        protected static void SetupImageBinding(Image image, DocumentController controller,
            Context context)
        {
            var data = controller.GetField(DashConstants.KeyStore.DataKey);
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
            BindImageSource(image, controller, context, DashConstants.KeyStore.DataKey);
        }

        protected static void BindImageSource(Image image, DocumentController docController, Context context, Key key)
        {
            var data = docController.GetDereferencedField(key, context) as ImageFieldModelController;
            if (data == null)
            {
                return;
            }
            var sourceBinding = new Binding
            {
                Source = data,
                Path = new PropertyPath(nameof(data.Data)),
                Mode = BindingMode.OneWay
            };
            image.SetBinding(Image.SourceProperty, sourceBinding);
        }

        private static void BindClip(Image image, DocumentController docController, Context context)
        {
            var widthController =
                docController.GetDereferencedField(DashConstants.KeyStore.WidthFieldKey, context) as NumberFieldModelController;
            var heightController =
                docController.GetDereferencedField(DashConstants.KeyStore.HeightFieldKey, context) as NumberFieldModelController;
            var clipController =
                docController.GetDereferencedField(ClipKey, context) as RectFieldModelController;
            if (clipController == null)  return;
            Debug.Assert(widthController != null);
            Debug.Assert(heightController != null);
            //Debug.WriteLine(clipController.Data.Width + ", " + clipController.Data.Height);
            var data = clipController.Data;
            UpdateClip(image, data);
            widthController.FieldModelUpdated += (ss, cc) =>
            {
                UpdateClip(image, data);
            };
            heightController.FieldModelUpdated += (ss, cc) =>
            {
                UpdateClip(image, data);
            };
        }

        private static void UpdateClip(Image image, Rect data)
        {
            Debug.Assert(image != null);
            image.Clip = new RectangleGeometry
            {
                Rect = new Rect(data.X * image.Width / 100, 
                                data.Y * image.Height / 100, 
                                data.Width * image.Width / 100, 
                                data.Height * image.Height / 100)
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

        private static void SetOpacityField(DocumentController docController, double opacity, bool forceMask, Context context)
        {
            var currentOpacityField = new NumberFieldModelController(opacity);
            docController.SetField(OpacityKey, currentOpacityField, forceMask); 
            // set the field here so that forceMask is respected
        }

        private static ReferenceFieldModelController GetImageReference(DocumentController docController)
        {
            return docController.GetField(DashConstants.KeyStore.DataKey) as ReferenceFieldModelController;
        }

        #endregion

    }

}