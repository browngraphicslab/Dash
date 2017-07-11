using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Dash.Converters;
using DashShared;

namespace Dash {
    public static class CourtesyDocuments {
        /// <summary>
        /// This class provides base functionality for creating and displaying new documents.
        /// </summary>
        public class CourtesyDocument {

            public List<DocumentModel> ContextList;
            public virtual DocumentController Document { get; set; }

            public static void SetLayoutForDocument(DocumentController document, DocumentController layoutDoc) {
                var layoutController = new DocumentFieldModelController(layoutDoc);
                document.SetField(DashConstants.KeyStore.LayoutKey, layoutController, false);
            }

            /// <summary>
            /// Gives a layout document delegate both a Data field and a Layout field which override their prototypes' fields.
            /// The Data field is specifies the layout field instance documents that are needed to render the delegate
            /// The Layout field specifies that delegate will render itself instead of creating dynamic render instances for each of its fields
            /// </summary>
            /// <param name="prototypeLayout"></param>
            /// <param name="layoutDocs"></param>
            /// <returns></returns>
            public static DocumentController CreateDelegateLayout(DocumentController prototypeLayout,
                IEnumerable<DocumentController> layoutDocs) {
                var deleg = prototypeLayout.MakeDelegate();

                var fmc = new DocumentCollectionFieldModelController(layoutDocs);

                deleg.SetField(DashConstants.KeyStore.DataKey, fmc, true);

                var selfFmc = new DocumentFieldModelController(deleg);
                deleg.SetField(DashConstants.KeyStore.LayoutKey, selfFmc, true);
                return deleg;
            }

            public static Dictionary<Key, FieldModelController> DefaultLayoutFields(double x, double y, double w, double h,
                FieldModelController data) {
                // create a layout for the image
                var fields = new Dictionary<Key, FieldModelController> {
                    [DashConstants.KeyStore.WidthFieldKey] = new NumberFieldModelController(w),
                    [DashConstants.KeyStore.HeightFieldKey] = new NumberFieldModelController(h),
                    [DashConstants.KeyStore.PositionFieldKey] = new PointFieldModelController(x, y)
                };
                if (data != null)
                    fields.Add(DashConstants.KeyStore.DataKey, data);
                return fields;
            }

            public virtual FrameworkElement makeView(DocumentController docController,
                List<DocumentController> docContextList) {
                return new Grid();
            }

            /// <summary>
            /// Adds bindings needed to create links between renderable fields on collections.
            /// </summary>
            /// <param name="refFieldModelController">A reference back to the data source for the <paramref name="renderElement"/></param>
            /// <param name="renderElement">The element which is actually rendered on the screen, this will receive bindings for interactions</param>
            protected static void BindOperationInteractions(ReferenceFieldModelController refFieldModelController,
                FrameworkElement renderElement) {
                renderElement.ManipulationMode = ManipulationModes.All;
                renderElement.ManipulationStarted += delegate (object sender, ManipulationStartedRoutedEventArgs args) {
                    var view = renderElement.GetFirstAncestorOfType<CollectionView>();
                    if (view == null) return; // we can't always assume we're on a collection
                    if (view.CanLink) {
                        args.Complete();
                        view.CanLink = false; // essential s.t. drag events don't get overriden
                    }
                };
                renderElement.IsHoldingEnabled = true; // turn on holding

                // must hold on element first to fetch link node
                renderElement.Holding += delegate (object sender, HoldingRoutedEventArgs args) {
                    var view = renderElement.GetFirstAncestorOfType<CollectionView>();
                    if (view == null) return; // we can't always assume we're on a collection
                    Debug.WriteLine("we are aholding!");
                    view.CanLink = true;
                    if (view.CurrentView is CollectionFreeformView)
                        (view.CurrentView as CollectionFreeformView).StartDrag(new OperatorView.IOReference(refFieldModelController, true, view.PointerArgs, renderElement,
                        renderElement.GetFirstAncestorOfType<DocumentView>()));

                };
                renderElement.PointerPressed += delegate (object sender, PointerRoutedEventArgs args) {
                    var view = renderElement.GetFirstAncestorOfType<CollectionView>();
                    if (view == null) return; // we can't always assume we're on a collection
                    view.PointerArgs = args;
                };
                renderElement.PointerReleased += delegate (object sender, PointerRoutedEventArgs args) {
                    var view = renderElement.GetFirstAncestorOfType<CollectionView>();
                    view.CanLink = false;
                    if (view == null) return; // we can't always assume we're on a collection

                    (view.CurrentView as CollectionFreeformView)?.EndDrag(
                        new OperatorView.IOReference(refFieldModelController, false, args, renderElement, 
                        renderElement.GetFirstAncestorOfType<DocumentView>()));

                };
            }

            /// <summary>
            /// Adds a binding from the passed in <see cref="renderElement"/> to the passed in <see cref="NumberFieldModelController"/>
            /// <exception cref="ArgumentNullException">Throws an exception if the passed in <see cref="NumberFieldModelController"/> is null</exception>
            /// </summary>
            protected static void BindHeight(FrameworkElement renderElement,
                NumberFieldModelController heightController) {
                if (heightController == null) throw new ArgumentNullException(nameof(heightController));
                var heightBinding = new Binding {
                    Source = heightController,
                    Path = new PropertyPath(nameof(heightController.Data)),
                    Mode = BindingMode.TwoWay
                };
                renderElement.SetBinding(FrameworkElement.HeightProperty, heightBinding);
            }

            /// <summary>
            /// Adds a binding from the passed in <see cref="renderElement"/> to the passed in <see cref="NumberFieldModelController"/>
            /// <exception cref="ArgumentNullException">Throws an exception if the passed in <see cref="NumberFieldModelController"/> is null</exception>
            /// </summary>
            protected static void BindWidth(FrameworkElement renderElement, NumberFieldModelController widthController) {
                if (widthController == null) throw new ArgumentNullException(nameof(widthController));
                var widthBinding = new Binding {
                    Source = widthController,
                    Path = new PropertyPath(nameof(widthController.Data)),
                    Mode = BindingMode.TwoWay
                };
                renderElement.SetBinding(FrameworkElement.WidthProperty, widthBinding);
            }

            /// <summary>
            /// Adds a binding from the passed in <see cref="renderElement"/> to the passed in <see cref="PointFieldModelController"/>
            /// <exception cref="ArgumentNullException">Throws an exception if the passed in <see cref="PointFieldModelController"/> is null</exception>
            /// </summary>
            public static void BindTranslation(FrameworkElement renderElement,
                PointFieldModelController translateController) {
                if (translateController == null) throw new ArgumentNullException(nameof(translateController));
                var translateBinding = new Binding {
                    Source = translateController,
                    Path = new PropertyPath(nameof(translateController.Data)),
                    Mode = BindingMode.TwoWay,
                    Converter = new PointToTranslateTransformConverter()
                };
                renderElement.SetBinding(UIElement.RenderTransformProperty, translateBinding);
            }

            /// <summary>
            /// Returns the <see cref="NumberFieldModelController"/> from the passed in <see cref="DocumentController"/>
            /// used to control that <see cref="DocumentController"/>'s height.
            /// </summary>
            protected static NumberFieldModelController GetHeightFieldController(DocumentController docController, IEnumerable<DocumentController> contextList) {
                // make text height resize
                var heightController = docController.GetDereferencedField(DashConstants.KeyStore.HeightFieldKey, contextList) as NumberFieldModelController;
                Debug.Assert(heightController != null);
                return heightController;
            }

            /// <summary>
            /// Returns the <see cref="NumberFieldModelController"/> from the passed in <see cref="DocumentController"/>
            /// used to control that <see cref="DocumentController"/>'s width.
            /// </summary>
            protected static NumberFieldModelController GetWidthFieldController(DocumentController docController, IEnumerable<DocumentController> contextList) {

                // make text width resize
                var widthController =
                    docController.GetDereferencedField(DashConstants.KeyStore.WidthFieldKey, contextList) as NumberFieldModelController;
                Debug.Assert(widthController != null);
                return widthController;
            }



            /// <summary>
            /// Returns the <see cref="NumberFieldModelController"/> from the passed in <see cref="DocumentController"/>
            /// used to control that <see cref="DocumentController"/>'s translation.
            /// </summary>
            protected static PointFieldModelController GetTranslateFieldController(DocumentController docController, IEnumerable<DocumentController> contextList) {
                var translateController =
                    docController.GetDereferencedField(DashConstants.KeyStore.PositionFieldKey, contextList) as PointFieldModelController;
                Debug.Assert(translateController != null);
                return translateController;
            }
        }

        /// <summary>
        /// Given a document, this provides an API for getting all of the layout documents that define it's view.
        /// </summary>
        public class LayoutCourtesyDocument : CourtesyDocument {
            public DocumentController LayoutDocumentController = null;

            public LayoutCourtesyDocument(DocumentController docController, IEnumerable<DocumentController> contextList) {
                Document = docController; // get the layout field on the document being displayed
                var layoutField = docController.GetDereferencedField(DashConstants.KeyStore.LayoutKey, contextList) as DocumentFieldModelController;
                if (layoutField == null) {
                    var fields = DefaultLayoutFields(0, 0, double.NaN, double.NaN,
                        new DocumentCollectionFieldModelController(new DocumentController[] { }));
                    LayoutDocumentController =
                        new DocumentController(fields, CourtesyDocuments.CollectionBox.DocumentType);

                    SetLayoutForDocument(Document, LayoutDocumentController);
                } else
                    LayoutDocumentController = layoutField?.Data;
            }

            public IEnumerable<DocumentController> GetLayoutDocuments(List<DocumentController> docContextList)
            {

                var layoutDataField =
                        LayoutDocumentController?.GetDereferencedField(DashConstants.KeyStore.DataKey, docContextList);
                if (layoutDataField is DocumentCollectionFieldModelController)
                    foreach (var d in (layoutDataField as DocumentCollectionFieldModelController).GetDocuments())
                        yield return d;
                else if (layoutDataField.FieldModel is DocumentModelFieldModel)
                    yield return ContentController.GetController<DocumentController>(
                        (layoutDataField.FieldModel as DocumentModelFieldModel).Data.Id);
                else yield return LayoutDocumentController;
            }

            public override FrameworkElement makeView(DocumentController docController,
                List<DocumentController> docContextList) {
                return LayoutCourtesyDocument.MakeView(docController, docContextList);
            }

            public static FrameworkElement MakeView(DocumentController docController,
                List<DocumentController> docContextList) {
                var docViewModel = new DocumentViewModel(docController, docContextList) {
                    IsDetailedUserInterfaceVisible = false,
                    IsMoveable = false
                };
                return new DocumentView(docViewModel);
            }
        }

        /// <summary>
        /// Given a reference to an operator field model, constructs a document type that displays that operator.
        /// </summary>
        public class OperatorBox : CourtesyDocument {
            public static DocumentType DocumentType =
                new DocumentType("53FC9C82-F32C-4704-AF6B-E55AC805C84F", "Operator Box");

            public OperatorBox(ReferenceFieldModelController refToOp) {
                var fields = DefaultLayoutFields(0, 0, double.NaN, double.NaN, refToOp);
                Document = new DocumentController(fields, DocumentType);
            }

            public override FrameworkElement makeView(DocumentController docController,
                List<DocumentController> docContextList) {
                return OperatorBox.MakeView(docController, docContextList);
            }

            public static FrameworkElement MakeView(DocumentController docController,
                List<DocumentController> docContextList) {
                var data = docController.GetField(DashConstants.KeyStore.DataKey, docContextList) ?? null;
                var opfmc = (data as ReferenceFieldModelController);
                OperatorView opView = new OperatorView { DataContext = opfmc };
                return opView;
            }
        }

        /// <summary>
        /// A generic document type containing a single text element.
        /// </summary>
        public class TextingBox : CourtesyDocument {
            public static Key PrefixKey = new Key("AC1B4A0C-CFBF-43B3-B7F1-D7FC9E5BEEBE", "Text Prefix");
            public static Key FontWeightKey = new Key("03FC5C4B-6A5A-40BA-A262-578159E2D5F7", "FontWeight");
            public static Key FontSizeKey = new Key("75902765-7F0E-4AA6-A98B-3C8790DBF7CE", "FontSize");
            public static Key TextAlignmentKey = new Key("3BD4572A-C6C9-4710-8E74-831204D2C17D", "Font Alignment");

            public static DocumentType DocumentType =
                new DocumentType("181D19B4-7DEC-42C0-B1AB-365B28D8EA42", "Texting Box");

            public DocumentController MakeDelegate(ReferenceFieldModelController refModel) {
                var delg = Document.MakeDelegate();
                delg.SetField(DashConstants.KeyStore.DataKey, refModel, true);
                return delg;
            }

            public TextingBox(FieldModelController refToText, double x = 0, double y = 0, double w = 200, double h = 20) {
                var fields = DefaultLayoutFields(x, y, w, h, refToText);
                fields[FontWeightKey] = new NumberFieldModelController(FontWeights.Normal.Weight);
                fields[FontSizeKey] = new NumberFieldModelController(12);
                fields[TextAlignmentKey] = new NumberFieldModelController(0);
                Document = new DocumentController(fields, DocumentType);
                SetLayoutForDocument(Document, Document);
            }

            public override FrameworkElement makeView(DocumentController docController,
                List<DocumentController> docContextList) {
                return TextingBox.MakeView(docController, docContextList);
            }

            protected static NumberFieldModelController GetTextAlignmentFieldController(
                DocumentController docController, IEnumerable<DocumentController> docContextList) {
                var textController =
                    docController.GetDereferencedField(TextAlignmentKey, docContextList) as NumberFieldModelController;
                Debug.Assert(textController != null);
                return textController;
            }

            protected static void BindTextAlignment(FrameworkElement renderElement,
                NumberFieldModelController textAlignmentController) {
                if (textAlignmentController == null) throw new ArgumentNullException(nameof(textAlignmentController));
                var alignmentBinding = new Binding {
                    Source = textAlignmentController,
                    Path = new PropertyPath(nameof(textAlignmentController.Data)),
                    Mode = BindingMode.TwoWay,
                    Converter = new IntToTextAlignmentConverter()
                };
                if (renderElement is TextBlock) {
                    renderElement.SetBinding(TextBlock.TextAlignmentProperty, alignmentBinding);
                } else if (renderElement is TextBox) {
                    renderElement.SetBinding(TextBox.TextAlignmentProperty, alignmentBinding);
                } else {
                    Debug.Assert(false, $"we don't support alignment for elements of type {renderElement.GetType()}");
                }
            }

            #region Font Weight Binding

            protected static NumberFieldModelController GetFontWeightFieldController(DocumentController docController, IEnumerable<DocumentController> contextList) {
                var fontController =
                    docController.GetDereferencedField(FontWeightKey, contextList) as NumberFieldModelController;
                Debug.Assert(fontController != null);
                return fontController;
            }

            protected static void BindFontWeight(FrameworkElement renderElement,
                NumberFieldModelController fontWeightController) {
                if (fontWeightController == null) throw new ArgumentNullException(nameof(fontWeightController));
                var fontWeightBinding = new Binding {
                    Source = fontWeightController,
                    Path = new PropertyPath(nameof(fontWeightController.Data)),
                    Mode = BindingMode.TwoWay,
                    Converter = new DoubleToFontWeightConverter()
                };
                if (renderElement is TextBlock) {
                    renderElement.SetBinding(Control.FontWeightProperty, fontWeightBinding);
                } else if (renderElement is TextBox) {
                    renderElement.SetBinding(Control.FontWeightProperty, fontWeightBinding);
                } else {
                    Debug.Assert(false, $"we don't support fontweight for elements of type {renderElement.GetType()}");
                }
            }

            #endregion

            #region Font Size Binding

            protected static NumberFieldModelController GetFontSizeFieldController(DocumentController docController, IEnumerable<DocumentController> docContextList) {
                var fontController =
                    docController.GetDereferencedField(FontSizeKey, docContextList) as NumberFieldModelController;
                Debug.Assert(fontController != null);
                return fontController;
            }

            protected static void BindFontSize(FrameworkElement renderElement,
                NumberFieldModelController sizeController) {
                if (sizeController == null) throw new ArgumentNullException(nameof(sizeController));
                var fontSizeBinding = new Binding {
                    Source = sizeController,
                    Path = new PropertyPath(nameof(sizeController.Data)),
                    Mode = BindingMode.TwoWay,
                };
                if (renderElement is TextBlock) {
                    renderElement.SetBinding(Control.FontSizeProperty, fontSizeBinding);
                } else if (renderElement is TextBox) {
                    renderElement.SetBinding(Control.FontSizeProperty, fontSizeBinding);
                } else {
                    Debug.Assert(false, $"we don't support fontsize for elements of type {renderElement.GetType()}");
                }
            }

            #endregion

            public static FrameworkElement MakeView(DocumentController docController,
                List<DocumentController> docContextList) {
                // the text field model controller provides us with the DATA
                // the Document on this courtesty document provides us with the parameters to display the DATA.
                // X, Y, Width, and Height etc....

                // create the textblock
                FrameworkElement tb = null;

                // use the reference to the text to get the text field model controller
                var retToText = docController.GetField(DashConstants.KeyStore.DataKey) as ReferenceFieldModelController;
                Debug.Assert(retToText != null);
                var fieldModelController = ContentController.DereferenceToRootFieldModel(retToText, docContextList);
                if (fieldModelController is TextFieldModelController) {
                    var textBox = new TextBox();
                    tb = textBox;
                    tb.HorizontalAlignment = HorizontalAlignment.Stretch;
                    tb.VerticalAlignment = VerticalAlignment.Stretch;
                    var textFieldModelController = fieldModelController as TextFieldModelController;
                    Debug.Assert(textFieldModelController != null);
                    // make text update when changed
                    var sourceBinding = new Binding {
                        Source = textFieldModelController,
                        Path = new PropertyPath(nameof(textFieldModelController.Data)),
                        Mode = BindingMode.TwoWay
                    };
                    tb.SetBinding(TextBox.TextProperty, sourceBinding);
                    textBox.TextWrapping = Windows.UI.Xaml.TextWrapping.Wrap;

                } else if (fieldModelController is NumberFieldModelController) {
                    tb = new TextBlock();
                    var numFieldModelController = fieldModelController as NumberFieldModelController;
                    Debug.Assert(numFieldModelController != null);
                    // make text update when changed
                    var sourceBinding = new Binding {
                        Source = numFieldModelController,
                        Path = new PropertyPath(nameof(numFieldModelController.Data))
                    };
                    tb.SetBinding(TextBlock.TextProperty, sourceBinding);
                }

                // bind the text height
                var heightController = GetHeightFieldController(docController, docContextList);
                BindHeight(tb, heightController);

                // bind the text width
                var widthController = GetWidthFieldController(docController, docContextList);
                BindWidth(tb, widthController);

                var fontWeightController = GetFontWeightFieldController(docController, docContextList);
                BindFontWeight(tb, fontWeightController);

                var fontSizeController = GetFontSizeFieldController(docController, docContextList);
                BindFontSize(tb, fontSizeController);

                var textAlignmentController = GetTextAlignmentFieldController(docController, docContextList);
                BindTextAlignment(tb, textAlignmentController);

                // add bindings to work with operators
                BindOperationInteractions(retToText, tb);

                return tb;
            }
        }

        /// <summary>
        /// A generic document type containing a single image.
        /// </summary>
        public class ImageBox : CourtesyDocument {
            public static DocumentType DocumentType =
                new DocumentType("3A6F92CC-D8DC-448B-9D3E-A1E04C2C77B3", "Image Box");

            public static Key OpacityKey = new Key("78DB67E4-4D9F-47FA-980D-B8EEE87C4351", "Opacity Key");
            public static double OpacityDefault = 1;

            public ImageBox(FieldModelController refToImage, double x = 0, double y = 0, double w = 200, double h = 200) {
                var fields = DefaultLayoutFields(x, y, w, h, refToImage);
                fields[OpacityKey] = new NumberFieldModelController(OpacityDefault);
                Document = new DocumentController(fields, DocumentType);

                SetLayoutForDocument(Document, Document);
            }

            public DocumentController MakeDelegate(ReferenceFieldModelController refModel) {
                var delg = Document.MakeDelegate();
                delg.SetField(DashConstants.KeyStore.DataKey, refModel, true);
                return delg;
            }

            public static FrameworkElement MakeView(DocumentController docController,
                List<DocumentController> docContextList) {
                // use the reference to the image to get the image field model controller
                var refToImage =
                    docController.GetField(DashConstants.KeyStore.DataKey) as ReferenceFieldModelController;
                Debug.Assert(refToImage != null);
                var fieldModelController =
                    ContentController.DereferenceToRootFieldModel<FieldModelController>(refToImage, docContextList);
                var imFieldModelController = fieldModelController as ImageFieldModelController;
                var textFieldModelController = fieldModelController as TextFieldModelController;
                Debug.Assert(imFieldModelController != null || textFieldModelController != null);

                // the image field model controller provides us with the DATA
                // the Document on this courtesty document provides us with the parameters to display the DATA.
                // X, Y, Width, and Height etc....

                // create the image
                var image = new Image {
                    Stretch = Stretch.Fill // set image to fill container but ignore aspect ratio :/
                };

                // make image source update when changed
                var sourceBinding = imFieldModelController != null
                    ? new Binding {
                        Source = imFieldModelController,
                        Path = new PropertyPath(nameof(imFieldModelController.Data))
                    }
                    : new Binding {
                        Source = fieldModelController,
                        Path = new PropertyPath(nameof(textFieldModelController.Data))
                    };
                image.SetBinding(Image.SourceProperty, sourceBinding);

                // make image height resize
                var heightController = GetHeightFieldController(docController, docContextList);
                BindHeight(image, heightController);

                // make image width resize
                var widthController = GetWidthFieldController(docController, docContextList);
                BindWidth(image, widthController);

                // set up interactions with operations
                BindOperationInteractions(refToImage, image);

                // make image opacity change
                var opacityController =
                    docController.GetDereferencedField(OpacityKey, docContextList) as NumberFieldModelController;
                Debug.Assert(opacityController != null);
                var opacityBinding = new Binding {
                    Source = opacityController,
                    Path = new PropertyPath(nameof(opacityController.Data))
                };
                image.SetBinding(UIElement.OpacityProperty, opacityBinding);
                return image;
            }

            public override FrameworkElement makeView(DocumentController docController,
                List<DocumentController> docContextList) {
                return ImageBox.MakeView(docController, docContextList);
            }
        }

        /// <summary>
        /// A generic data wrappe document display type used to display images or text fields.
        /// </summary>
        public class DataBox : CourtesyDocument {
            CourtesyDocument _doc;

            public DataBox(ReferenceFieldModelController refToField, bool isImage) {
                if (isImage)
                    _doc = new ImageBox(refToField);
                else
                    _doc = new TextingBox(refToField);
            }

            public override DocumentController Document {
                get { return _doc.Document; }
                set { _doc.Document = value; }
            }

            public override FrameworkElement makeView(DocumentController docController,
                List<DocumentController> docContextList) {
                return _doc.makeView(docController, docContextList);
            }
        }

        public class CollectionBox : CourtesyDocument {
            public static DocumentType DocumentType =
                new DocumentType("7C59D0E9-11E8-4F12-B355-20035B3AC359", "Generic Collection");

            void Initialize(FieldModelController fieldModel) {
                var fields = DefaultLayoutFields(0, 0, double.NaN, double.NaN, fieldModel);
                Document = new DocumentController(fields, DocumentType);
                Document.SetField(DashConstants.KeyStore.IconTypeFieldKey, new NumberFieldModelController((double)IconTypeEnum.Collection), true);
                SetLayoutForDocument(Document, Document);
            }

            public CollectionBox(ReferenceFieldModelController refToCollection) {
                Initialize(refToCollection);
            }

            public CollectionBox(DocumentCollectionFieldModelController docCollection) {
                Initialize(docCollection);
            }

            public override FrameworkElement makeView(DocumentController docController,
                List<DocumentController> docContextList) {
                return CollectionBox.MakeView(docController, docContextList);
            }

            static public FrameworkElement MakeView(DocumentController docController,
                List<DocumentController> docContextList) {
                var data = docController.GetDereferencedField(DashConstants.KeyStore.DataKey, docContextList) ?? null;

                if (data != null)
                {
                    var opacity = (docController.GetDereferencedField(new Key("opacity", "opacity"), docContextList) as NumberFieldModelController)?.Data;
                  
                    double opacityValue = opacity.HasValue ? (double)opacity :1;

                    var collectionFieldModelController = ContentController
                        .DereferenceToRootFieldModel<DocumentCollectionFieldModelController>(data, docContextList);
                    Debug.Assert(collectionFieldModelController != null);

                    var collectionViewModel = new CollectionViewModel(collectionFieldModelController, docContextList);

                    var view = new CollectionView(collectionViewModel);
                    view.Opacity = opacityValue;
                    return view;
                }
                return new Grid();
            }
        }


        /// <summary>
        /// Constructs a nested stackpanel that displays the fields of all documents in the list
        /// docs.
        /// </summary>
        public class StackingPanel : CourtesyDocument {
            public static DocumentType StackPanelDocumentType =
                new DocumentType("61369301-820F-4779-8F8C-701BCB7B0CB7", "Stack Panel");

            static public DocumentType DocumentType {
                get { return StackPanelDocumentType; }
            }

            public StackingPanel(IEnumerable<DocumentController> docs) {
                var fields = DefaultLayoutFields(0, 0, double.NaN, double.NaN,
                    new DocumentCollectionFieldModelController(docs));
                Document = new DocumentController(fields, StackPanelDocumentType);
            }

            public override FrameworkElement makeView(DocumentController docController,
                List<DocumentController> docContextList) {
                return StackingPanel.MakeView(docController, docContextList);
            }

            public static FrameworkElement MakeView(DocumentController docController,
                List<DocumentController> docContextList) {
                var stack = new StackPanel();
                stack.Orientation = Orientation.Horizontal;

                var stackFieldData =
                    docController.GetDereferencedField(DashConstants.KeyStore.DataKey, docContextList) as DocumentCollectionFieldModelController;

                if (stackFieldData != null)
                    foreach (var stackDoc in stackFieldData.GetDocuments()) {
                        stack.Children.Add(stackDoc.makeViewUI(docContextList));
                    }
                return stack;
            }
        }

        public class PostitNote : CourtesyDocument {
            public static DocumentType PostitNoteType =
                new DocumentType("A5FEFB00-EA2C-4B64-9230-BBA41BACCAFC", "Post It");

            public static Key NotesFieldKey = new Key("A5486740-8AD2-4A35-A179-6FF1DA4D504F", "Notes");
            static DocumentController _prototypeLayout = CreatePrototypeLayout();
            static TextingBox _prototypeTextLayout;

            static DocumentController CreatePrototypeLayout() {
                _prototypeTextLayout = new TextingBox(new TextFieldModelController("Text"), 0, 0, double.NaN,
                    double.NaN);

                return _prototypeTextLayout.Document;
            }

            public PostitNote() {
                var fields = DefaultLayoutFields(0, 0, double.NaN, double.NaN, null);
                fields.Add(NotesFieldKey, new TextFieldModelController("<your note>"));

                Document = new DocumentController(fields, PostitNoteType);

                var tBox = _prototypeTextLayout.MakeDelegate(
                    new ReferenceFieldModelController(Document.GetId(), NotesFieldKey));
                SetLayoutForDocument(tBox, tBox);

                SetLayoutForDocument(Document, tBox);
            }

        }

        public class TwoImages : CourtesyDocument {
            public static DocumentType TwoImagesType = new DocumentType("FC8EF5EB-1A0B-433C-85B6-6929B974A4B7", "Two Images");
            public static Key Image1FieldKey = new Key("827F581B-6ECB-49E6-8EB3-B8949DE0FE21", "ImageField1");
            public static Key Image2FieldKey = new Key("BCB1109C-0C55-47B7-B1E3-34CA9C66627E", "ImageField2");
            public static Key TextFieldKey = new Key("73A8E9AB-A798-4FA0-941E-4C4A5A2BF9CE", "TextField");
            static DocumentController _prototypeTwoImages = CreatePrototype2Images();
            static DocumentController _prototypeLayout   = CreatePrototypeLayout();

            static DocumentController CreatePrototype2Images()
            {
                // bcz: default values for data fields can be added, but should not be needed
                Dictionary<Key, FieldModelController> fields = new Dictionary<Key, FieldModelController>();
                fields.Add(TextFieldKey, new TextFieldModelController("Prototype Text"));
                fields.Add(Image1FieldKey, new ImageFieldModelController(new Uri("ms-appx://Dash/Assets/cat.jpg")));
                fields.Add(Image2FieldKey, new ImageFieldModelController(new Uri("ms-appx://Dash/Assets/cat2.jpeg")));

                //return new DocumentController(fields, TwoImagesType);
                return new DocumentController(new Dictionary<Key,FieldModelController>(), TwoImagesType);
            }
            /// <summary>
            /// Creates a default Layout for a Two Images document.  This requires that a prototype of a Two Images document exist so that
            /// this layout can reference the fields of the prototype.  When a delegate is made of a Two Images document,  this layout's 
            /// field references will automatically point to the delegate's (not the prototype's) values for those fields because of the
            /// context list used in MakeView().
            /// </summary>
            /// <returns></returns>
            static DocumentController CreatePrototypeLayout()
            {
                // set the default layout parameters on prototypes of field layout documents
                // these prototypes will be overridden by delegates when an instance is created
                var prototypeImage1Layout = new ImageBox(new ReferenceFieldModelController(_prototypeTwoImages.GetId(), Image1FieldKey),   0, 50, 200, 200);
                var prototypeImage2Layout = new ImageBox(new ReferenceFieldModelController(_prototypeTwoImages.GetId(), Image2FieldKey),   0, 250, 200, 200);
                var prototypeTextLayout   = new TextingBox(new ReferenceFieldModelController(_prototypeTwoImages.GetId(), TextFieldKey),   0, 0, 200, 50);
                var prototypeLayout       = new CollectionBox(new DocumentCollectionFieldModelController(new[] { prototypeTextLayout.Document,
                                                                                                                 prototypeImage1Layout.Document,
                                                                                                                 prototypeImage2Layout.Document }));
                prototypeLayout.Document.SetField(DashConstants.KeyStore.HeightFieldKey, new NumberFieldModelController(500), true);
                prototypeLayout.Document.SetField(DashConstants.KeyStore.WidthFieldKey, new NumberFieldModelController(200), true);
                return prototypeLayout.Document;
            }

            public TwoImages(bool displayFieldsAsDocuments)
            {
                Document = _prototypeTwoImages.MakeDelegate();
                
                Document.SetField(Image1FieldKey, new ImageFieldModelController(new Uri("ms-appx://Dash/Assets/cat.jpg")), true);
                Document.SetField(Image2FieldKey, new ImageFieldModelController(new Uri("ms-appx://Dash/Assets/cat2.jpeg")), true);
                Document.SetField(TextFieldKey,   new TextFieldModelController("Hello World!"), true);
                Document.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(new Windows.Foundation.Point()), true);

                var docLayout = _prototypeLayout.MakeDelegate();
                docLayout.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(new Windows.Foundation.Point(0,0)), true);
                docLayout.SetField(new Key("opacity", "opacity"), new NumberFieldModelController(0.8), true);
                SetLayoutForDocument(Document, docLayout);
                Document.SetField(DashConstants.KeyStore.IconTypeFieldKey, new NumberFieldModelController((double)IconTypeEnum.Collection), true);


                if (displayFieldsAsDocuments) {
                    //var documentFieldModel = new DocumentCollectionFieldModel(new DocumentModel[] { tBox.DocumentModel, imBox1.DocumentModel, imBox2.DocumentModel });
                    //var documentFieldModelController = new DocumentCollectionFieldModelController(documentFieldModel);
                    //ContentController.AddModel(documentFieldModel);
                    //ContentController.AddController(documentFieldModelController);
                    //Document.SetField(DashConstants.KeyStore.DataKey, documentFieldModelController, true);

                    //var genericCollection = new CollectionBox(documentFieldModel).Document;
                    //genericCollection.SetField(DashConstants.KeyStore.WidthFieldKey, new NumberFieldModelController(new NumberFieldModel(800)), true);

                    //SetLayoutForDocument(Document, genericCollection.DocumentModel);
                }
            }

        }
        public class NestedDocExample : CourtesyDocument {
            public static DocumentType NestedDocExampleType =
                new DocumentType("700FAEE4-5520-4E5E-9AED-3C8C5C1BE58B", "Nested Doc Example");

            public static Key TextFieldKey = new Key("73A8E9AB-A798-4FA0-941E-4C4A5A2BF9CE", "TextField");
            public static Key TextField2Key = new Key("B53F1453-4C52-4302-96A3-A6B40DA7D587", "TextField2");
            public static Key TwoImagesKey = new Key("4E5C2B62-905D-4952-891D-24AADE14CA80", "TowImagesField");

            public NestedDocExample(bool displayFieldsAsDocuments) {
                // create a document with two images
                var twoModel = new DocumentFieldModelController(new TwoImages(displayFieldsAsDocuments).Document);
                var tModel = new TextFieldModelController("Nesting");
                var tModel2 = new TextFieldModelController("More Nesting");
                var fields = new Dictionary<Key, FieldModelController> {
                    [TextFieldKey] = tModel,
                    [TwoImagesKey] = twoModel,
                    [TextField2Key] = tModel2
                };
                Document = new DocumentController(fields, NestedDocExampleType);

                var tBox = new TextingBox(new ReferenceFieldModelController(Document.GetId(), TextFieldKey))
                    .Document;
                var imBox1 = twoModel.Data;
                var tBox2 = new TextingBox(new ReferenceFieldModelController(Document.GetId(), TextField2Key))
                    .Document;

                var stackPan = new StackingPanel(new DocumentController[] { tBox, imBox1, tBox2 }).Document;

                SetLayoutForDocument(Document, stackPan);
            }
        }

        public class Numbers : CourtesyDocument {
            public static DocumentType NumbersType =
                new DocumentType("8FC422AB-015E-4B72-A28B-16271808C888", "Numbers");

            public static Key Number1FieldKey = new Key("0D3B939F-1E74-4577-8ACC-0685111E451C", "Number1");
            public static Key Number2FieldKey = new Key("56162B53-B02D-4880-912F-9D66B5F1F15B", "Number2");
            public static Key Number3FieldKey = new Key("61C34393-7DF7-4F26-9FDF-E0B138532F39", "Number3");

            public Numbers() {
                // create a document with two images
                var fields = DefaultLayoutFields(0, 0, double.NaN, double.NaN, null);
                fields.Add(Number1FieldKey, new NumberFieldModelController(789));
                fields.Add(Number2FieldKey, new NumberFieldModelController(23));
                fields.Add(Number3FieldKey, new NumberFieldModelController(8));

                Document = new DocumentController(fields, NumbersType);

                var imBox1 = new TextingBox(new ReferenceFieldModelController(Document.GetId(), Number1FieldKey), 0,
                    0, 50, 20).Document;
                var imBox2 = new TextingBox(new ReferenceFieldModelController(Document.GetId(), Number2FieldKey), 0,
                    0, 50, 20).Document;
                var tBox = new TextingBox(new ReferenceFieldModelController(Document.GetId(), Number3FieldKey), 0,
                    0, 50, 20).Document;

                var stackPan = new StackingPanel(new[] { tBox, imBox1, imBox2 }).Document;

                SetLayoutForDocument(Document, stackPan);
            }

        }

        // == API COURTESY DOCUMENTS ==


        /// <summary>
        /// Wrapper document to display the ApiSourceCreatorDisplay Usercontrol.
        /// </summary>
        public class ApiDocumentModel : CourtesyDocument {
            public static DocumentType DocumentType =
                new DocumentType("453ACC23-14EF-4990-A36D-53D5EBE2734D", "Api Source Creator");

            public static Key BaseUrlKey = new Key("C20E4B2B-A633-4C2C-ACBF-757FF6AC8E5A", "Base URL");
            public static Key HttpMethodKey = new Key("1CE4047D-1813-410B-804E-BA929D8CB4A4", "Http Method");
            public static Key HeadersKey = new Key("6E9D9F12-E978-4E61-85C7-707A0C13EFA7", "Headers");
            public static Key ParametersKey = new Key("654A4BDF-1AE0-432A-9C90-CCE9B4809870", "Parameter");

            public static Key AuthHttpMethodKey = new Key("D37CCAC0-ABBC-4861-BEB4-8C079049DCF8", "Auth Method");
            public static Key AuthBaseUrlKey = new Key("7F8709B6-2C9B-43D0-A86C-37F3A1517884", "Auth URL");
            public static Key AuthKey = new Key("1E5B5398-9349-4585-A420-EDBFD92502DE", "Auth Key");
            public static Key AuthSecretKey = new Key("A690EFD0-FF35-45FF-9795-372D0D12711E", "Auth Secret");
            public static Key AuthHeadersKey = new Key("E1773B06-F54C-4052-B888-AE85278A7F88", "Auth Header");
            public static Key AuthParametersKey = new Key("CD546F0B-A0BA-4C3B-B683-5B2A0C31F44E", "Auth Parameter");

            public static Key KeyTextKey = new Key("388F7E20-4424-4AC0-8BB7-E8CCF2279E60", "Key");
            public static Key ValueTextKey = new Key("F89CAD72-271F-48E6-B233-B6BA766E613F", "Value");
            public static Key RequiredKey = new Key("D4FCBA25-B540-4E17-A17A-FCDE775B97F9", "Required");
            public static Key DisplayKey = new Key("2B80D6A8-4224-4EC7-9BDF-DFD2CC20E463", "Display");


            public ApiDocumentModel() {
                var fields = new Dictionary<Key, FieldModelController> {
                    [BaseUrlKey] = new TextFieldModelController(""),
                    [HttpMethodKey] = new NumberFieldModelController(0),
                    [AuthBaseUrlKey] = new TextFieldModelController(""),
                    [AuthHttpMethodKey] = new NumberFieldModelController(0),
                    [AuthSecretKey] = new TextFieldModelController(""),
                    [AuthKey] = new TextFieldModelController(""),
                    [ParametersKey] = new DocumentCollectionFieldModelController(new List<DocumentController>()),
                    [HeadersKey] = new DocumentCollectionFieldModelController(new List<DocumentController>()),
                    [AuthParametersKey] =
                    new DocumentCollectionFieldModelController(new List<DocumentController>()),
                    [AuthHeadersKey] = new DocumentCollectionFieldModelController(new List<DocumentController>()),

                    // TODO: differentiating similar fields in different documents for operator view (Not sure what this means Anna)
                    [DocumentCollectionFieldModelController.CollectionKey] =
                    new DocumentCollectionFieldModelController(new List<DocumentController>())
                };
                Document = new DocumentController(fields, DocumentType);
                Document.SetField(DashConstants.KeyStore.IconTypeFieldKey, new NumberFieldModelController((double)IconTypeEnum.Api), true);
            }

            /// <summary>
            /// Generates a new document containing the parameter information and adds that document to
            /// the corresponding DocumentCollectionFieldModel representing that parameter's list (i.e. Header, AuthParameters).
            /// </summary>
            /// <returns>The newly generated document representing the newly added parameter.</returns>
            public static DocumentController addParameter(DocumentController docController, TextBox key,
                TextBox value, CheckBox display,
                CheckBox required, Key parameterCollectionKey, ApiSourceDisplay sourceDisplay) {
                Debug.Assert(docController.DocumentType == DocumentType);
                Debug.Assert(parameterCollectionKey == AuthParametersKey ||
                             parameterCollectionKey == AuthHeadersKey ||
                             parameterCollectionKey == ParametersKey || parameterCollectionKey == HeadersKey);

                // fetch parameter list to add to
                var col =
                    (DocumentCollectionFieldModelController)docController.GetField(parameterCollectionKey);

                double displayDouble = ((bool)display.IsChecked) ? 0 : 1;
                double requiredDouble = ((bool)required.IsChecked) ? 0 : 1;

                // generate new doc with information to add
                var fields = new Dictionary<Key, FieldModelController> {
                    [ValueTextKey] = new TextFieldModelController(key.Text),
                    [DisplayKey] = new NumberFieldModelController(displayDouble),
                    [KeyTextKey] = new TextFieldModelController(value.Text),
                    [RequiredKey] = new NumberFieldModelController(requiredDouble),
                };

                // add to collection & return new document result
                var ret = new DocumentController(fields, DocumentType);

                // apply textbox bindings
                bindToTextBox(key, ret.GetField(KeyTextKey));
                bindToTextBox(value, ret.GetField(ValueTextKey));

                // apply checkbox bindings
                bindToCheckBox(display, ret.GetField(DisplayKey));
                bindToCheckBox(required, ret.GetField(RequiredKey));

                // get the property's type
                ApiProperty.ApiPropertyType type = ApiProperty.ApiPropertyType.Parameter;
                if (parameterCollectionKey == HeadersKey)
                    type = ApiProperty.ApiPropertyType.Header;
                if (parameterCollectionKey == AuthHeadersKey)
                    type = ApiProperty.ApiPropertyType.AuthHeader;
                if (parameterCollectionKey == AuthParametersKey)
                    type = ApiProperty.ApiPropertyType.AuthParameter;

                // make new property in source view
                ApiProperty apiprop = new ApiProperty(key.Text, value.Text, type, ret, required.IsChecked.Value);
                sourceDisplay.AddToListView(apiprop);
                Debug.WriteLine("here: " + key.Text);

                // bind source's fields to those of the editor (key, value)
                TextFieldModelController textFieldModelController =
                    ret.GetField(KeyTextKey) as TextFieldModelController;
                var sourceBinding = new Binding {
                    Source = textFieldModelController,
                    Path = new PropertyPath(nameof(textFieldModelController.Data)),
                    Mode = BindingMode.OneWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                apiprop.XKey.SetBinding(TextBlock.TextProperty, sourceBinding);
                bindToTextBox(apiprop.XValue, ret.GetField(ValueTextKey));

                // bind source visibility to display checkbox which is bound to backend display field of param document
                var binding = new Binding {
                    Source = display,
                    Path = new PropertyPath(nameof(display.IsChecked)),
                    Mode = BindingMode.OneWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    Converter = new BoolToVisibilityConverter()
                };
                apiprop.SetBinding(ApiProperty.VisibilityProperty, binding);

                // bind ApiRequired property to the required checkbox
                var bindin = new Binding {
                    Source = display,
                    Path = new PropertyPath(nameof(required.IsChecked)),
                    Mode = BindingMode.OneWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                apiprop.XRequired.SetBinding(CheckBox.IsCheckedProperty, bindin);


                col.AddDocument(ret);
                return ret;
            }

            /// <summary>
            /// Removes a parameter from a given list of parameter documents.
            /// </summary>
            public static void removeParameter(DocumentController docController,
                DocumentController docModelToRemove,
                Key parameterCollectionKey, ApiSourceDisplay sourceDisplay) {
                Debug.Assert(docController.DocumentType == DocumentType);
                Debug.Assert(parameterCollectionKey == AuthParametersKey ||
                             parameterCollectionKey == AuthHeadersKey ||
                             parameterCollectionKey == ParametersKey || parameterCollectionKey == HeadersKey);

                DocumentCollectionFieldModelController col =
                    (DocumentCollectionFieldModelController)docController.GetField(parameterCollectionKey);
                col.RemoveDocument(docModelToRemove);

            }

            // inherited
            public override FrameworkElement makeView(DocumentController docController,
                List<DocumentController> docContextList) {
                return ApiDocumentModel.MakeView(docController, docContextList);
            }

            /// <summary>
            /// Binds a textbox to a fieldModelController.
            /// </summary>
            private static void bindToTextBox(TextBox tb, FieldModelController field) {

                // bind URL
                TextFieldModelController textFieldModelController = field as TextFieldModelController;
                var sourceBinding = new Binding {
                    Source = textFieldModelController,
                    Path = new PropertyPath(nameof(textFieldModelController.Data)),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                tb.SetBinding(TextBox.TextProperty, sourceBinding);

            }

            /// <summary>
            /// Binds a textbox to a fieldModelController.
            /// </summary>
            private static void bindToCheckBox(CheckBox cb, FieldModelController field) {

                // bind URL
                NumberFieldModelController textFieldModelController = field as NumberFieldModelController;
                var sourceBinding = new Binding {
                    Source = textFieldModelController,
                    Path = new PropertyPath(nameof(textFieldModelController.Data)),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    Converter = new DoubleToBoolConverter()
                };
                cb.SetBinding(CheckBox.IsCheckedProperty, sourceBinding);
                textFieldModelController.Data = 1;
            }

            private static void makeBinding(ApiCreatorDisplay apiDisplay, DocumentController docController) {

                // set up text bindings
                bindToTextBox(apiDisplay.UrlTB, docController.GetField(BaseUrlKey));
                bindToTextBox(apiDisplay.AuthDisplay.UrlTB, docController.GetField(AuthBaseUrlKey));
                bindToTextBox(apiDisplay.AuthDisplay.KeyTB, docController.GetField(AuthKey));
                // bindToTextBox(apiDisplay.AuthDisplay.SecretTB, docController.Fields[AuthSecretKey));

                // bind drop down list
                NumberFieldModelController fmcontroller =
                    docController.GetField(HttpMethodKey) as NumberFieldModelController;
                var sourceBinding = new Binding {
                    Source = fmcontroller,
                    Path = new PropertyPath(nameof(fmcontroller.Data)),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                apiDisplay.RequestMethodCB.SetBinding(ComboBox.SelectedIndexProperty, sourceBinding);

            }

            public static void setResults(DocumentController docController, List<DocumentController> documents) {
                (docController.GetField(DocumentCollectionFieldModelController.CollectionKey) as
                    DocumentCollectionFieldModelController).SetDocuments(documents);
            }

            public static FrameworkElement MakeView(DocumentController docController,
                List<DocumentController> docContextList) {

                ApiSourceDisplay sourceDisplay = new ApiSourceDisplay();
                ApiCreatorDisplay apiDisplay = new ApiCreatorDisplay(docController, sourceDisplay);
                makeBinding(apiDisplay, docController);

                // test bindings are working
                Debug.WriteLine((docController.GetDereferencedField(BaseUrlKey, docContextList) as TextFieldModelController).Data);
                apiDisplay.UrlTB.Text = "https://itunes.apple.com/search";
                Debug.WriteLine((docController.GetDereferencedField(BaseUrlKey, docContextList) as TextFieldModelController).Data);

                // generate collection view preview for results
                var resultView =
                    docController.GetDereferencedField(DocumentCollectionFieldModelController.CollectionKey, docContextList) as
                        DocumentCollectionFieldModelController;

                // make collection view display framework element
                var data = resultView;
                var collectionFieldModelController = ContentController
                    .DereferenceToRootFieldModel<DocumentCollectionFieldModelController>(data, docContextList);
                Debug.Assert(collectionFieldModelController != null);

                var collectionViewModel = new CollectionViewModel(collectionFieldModelController, docContextList);
                var collectionDisplay = new CollectionView(collectionViewModel);


                // this binding makes it s.t. either only the ApiSource or the ApiSourceCreator is visible at a single time
                // TODO: should clients be able to decide for themselves how this is displaying (separate superuser and regular user)
                // or should everyone just see the same view ?
                // bind URL
                var sourceBinding = new Binding {
                    Source = apiDisplay,
                    Path = new PropertyPath(nameof(apiDisplay.Visibility)),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    Converter = new InverseVisibilityConverter()
                };
                sourceDisplay.SetBinding(ApiSourceDisplay.VisibilityProperty, sourceBinding);

                // set up grid to hold UI elements: api size is fixed, results display resizes w/ document container
                Grid containerGrid = new Grid();
                containerGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
                containerGrid.VerticalAlignment = VerticalAlignment.Stretch;
                containerGrid.RowDefinitions.Add(new RowDefinition());
                containerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(450) });
                containerGrid.ColumnDefinitions.Add(new ColumnDefinition());
                Grid.SetColumn(collectionDisplay, 1);
                containerGrid.Children.Add(apiDisplay);
                containerGrid.Children.Add(sourceDisplay);
                containerGrid.Children.Add(collectionDisplay);

                collectionDisplay.MaxWidth = 550;
                collectionDisplay.HorizontalAlignment = HorizontalAlignment.Left;

                // return all results
                return containerGrid;
            }
        }

        /// <summary>
        /// Converts doubles to booleans and back. 0 = false, 1 = true (or any nonzero number). Used
        /// primarily to convert NumberFieldModels into boolean values.
        /// </summary>
        public class DoubleToBoolConverter : IValueConverter {
            public object Convert(object value, Type targetType, object parameter, string language) {
                return ((double)value != 0);
            }

            public object ConvertBack(object value, Type targetType, object parameter, string language) {
                if ((bool)value) return 1;
                return 0;
            }
        }


        /// <summary>
        /// Converts doubles to booleans and back. 0 = false, 1 = true (or any nonzero number). Used
        /// primarily to convert NumberFieldModels into boolean values.
        /// </summary>
        public class InverseVisibilityConverter : IValueConverter {
            public object Convert(object value, Type targetType, object parameter, string language) {
                if ((Windows.UI.Xaml.Visibility)value == Windows.UI.Xaml.Visibility.Collapsed)
                    return Windows.UI.Xaml.Visibility.Visible;
                else
                    return Windows.UI.Xaml.Visibility.Collapsed;
            }

            public object ConvertBack(object value, Type targetType, object parameter, string language) {
                if ((Windows.UI.Xaml.Visibility)value == Windows.UI.Xaml.Visibility.Collapsed)
                    return Windows.UI.Xaml.Visibility.Visible;
                else
                    return Windows.UI.Xaml.Visibility.Collapsed;
            }
        }
    }
}