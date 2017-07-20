using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Dash.Converters;
using DashShared;
using Windows.UI.Xaml.Controls.Primitives;
using Dash.Views;
using TextWrapping = Windows.UI.Xaml.TextWrapping;

namespace Dash
{
    public static class CourtesyDocuments
    {

        /// <summary>
        /// This class provides base functionality for creating and providing layouts to documents which contain data
        /// </summary>
        public abstract class CourtesyDocument
        {

            protected abstract DocumentController GetLayoutPrototype();

            public virtual DocumentController Document { get; set; }

            protected abstract DocumentController InstantiatePrototypeLayout();

            protected static FieldModelController GetDereferencedDataFieldModelController(DocumentController docController, Context context, FieldModelController defaultFieldModelController, out ReferenceFieldModelController refToData)
            {
                refToData = docController.GetField(DashConstants.KeyStore.DataKey) as ReferenceFieldModelController;
                Debug.Assert(refToData != null);
                var fieldModelController = refToData.DereferenceToRoot(context);

                // bcz: think this through better:
                //   -- the idea is that we're referencing a field that doesn't exist.  Instead of throwing an error, we can
                //      create the field with a default value.  The question is where in the 'context' should we set it?  I think
                //      we want to follow the reference to its end, adding fields along the way ... this just follows the reference one level.
                if (fieldModelController == null)
                {
                    var parent = refToData.GetDocumentController(context);
                    Debug.Assert(parent != null);
                    parent.SetField((refToData as ReferenceFieldModelController).ReferenceFieldModel.FieldKey, defaultFieldModelController, true);
                    fieldModelController = refToData.DereferenceToRoot(context);
                }
                return fieldModelController;
            }

            /// <summary>
            /// Sets the active layout on the <paramref name="dataDocument"/> to the passed in <paramref name="layoutDoc"/>
            /// </summary>
            protected static void SetLayoutForDocument(DocumentController dataDocument, DocumentController layoutDoc, bool forceMask, bool addToLayoutList)
            {
                dataDocument.SetActiveLayout(layoutDoc, forceMask: forceMask, addToLayoutList: addToLayoutList);
            }

            [Deprecated("Use alternate DefaultLayoutFields", DeprecationType.Deprecate, 1)]
            protected static Dictionary<Key, FieldModelController> DefaultLayoutFields(double x, double y, double w, double h,
                FieldModelController data)
            {
                return DefaultLayoutFields(new Point(x, y), new Size(w, h), data);
            }

            protected static Dictionary<Key, FieldModelController> DefaultLayoutFields(Point pos, Size size, FieldModelController data = null)
            {
                // assign the default fields
                var fields = new Dictionary<Key, FieldModelController>
                {
                    [DashConstants.KeyStore.WidthFieldKey] = new NumberFieldModelController(size.Width),
                    [DashConstants.KeyStore.HeightFieldKey] = new NumberFieldModelController(size.Height),
                    [DashConstants.KeyStore.PositionFieldKey] = new PointFieldModelController(pos),
                    [DashConstants.KeyStore.ScaleAmountFieldKey] = new PointFieldModelController(1, 1),
                    [DashConstants.KeyStore.ScaleCenterFieldKey] = new PointFieldModelController(0, 0)
                };

                if (data != null)
                    fields.Add(DashConstants.KeyStore.DataKey, data);
                return fields;
            }

            public virtual FrameworkElement makeView(DocumentController docController,
                Context context, bool isInterfaceBuilderLayout = false) {
                return new Grid();
            }

            #region Bindings

            /// <summary>
            /// Adds bindings needed to create links between renderable fields on collections.
            /// </summary>
            protected static void BindOperationInteractions(FrameworkElement renderElement, ReferenceFieldModelController fieldModelController)
            {
                renderElement.ManipulationMode = ManipulationModes.All;
                renderElement.ManipulationStarted += delegate (object sender, ManipulationStartedRoutedEventArgs args)
                {
                    var view = renderElement.GetFirstAncestorOfType<CollectionView>();
                    if (view == null) return; // we can't always assume we're on a collection
                    if (view.CanLink)
                    {
                        args.Complete();
                        view.CanLink = false; // essential s.t. drag events don't get overriden
                    }
                };
                renderElement.IsHoldingEnabled = true; // turn on holding

                // must hold on element first to fetch link node
                renderElement.Holding += delegate (object sender, HoldingRoutedEventArgs args)
                {
                    var view = renderElement.GetFirstAncestorOfType<CollectionView>();
                    if (view == null) return; // we can't always assume we're on a collection
                    view.CanLink = true;
                    if (view.CurrentView is CollectionFreeformView)
                        (view.CurrentView as CollectionFreeformView).StartDrag(new OperatorView.IOReference(fieldModelController, true, view.PointerArgs, renderElement,
                            renderElement.GetFirstAncestorOfType<DocumentView>()));

                };
                renderElement.PointerPressed += delegate (object sender, PointerRoutedEventArgs args)
                {
                    var view = renderElement.GetFirstAncestorOfType<CollectionView>();
                    if (view == null) return; // we can't always assume we're on a collection
                    view.PointerArgs = args;
                    if (args.GetCurrentPoint(view).Properties.IsLeftButtonPressed)
                    {

                    }
                    else if (args.GetCurrentPoint(view).Properties.IsRightButtonPressed)
                    {
                        view.CanLink = true;
                        if (view.CurrentView is CollectionFreeformView)
                            (view.CurrentView as CollectionFreeformView).StartDrag(new OperatorView.IOReference(fieldModelController, true, args, renderElement,
                                renderElement.GetFirstAncestorOfType<DocumentView>()));
                    }
                };
                renderElement.PointerReleased += delegate (object sender, PointerRoutedEventArgs args)
                {
                    var view = renderElement.GetFirstAncestorOfType<CollectionView>();
                    if (view == null) return; // we can't always assume we're on a collection
                    view.CanLink = false;

                    args.Handled = true;
                    (view.CurrentView as CollectionFreeformView)?.EndDrag(
                        new OperatorView.IOReference(fieldModelController, false, args, renderElement,
                            renderElement.GetFirstAncestorOfType<DocumentView>()));

                };
            }

            /// <summary>
            /// Adds a binding from the passed in <see cref="renderElement"/> to the passed in <see cref="NumberFieldModelController"/>
            /// <exception cref="ArgumentNullException">Throws an exception if the passed in <see cref="NumberFieldModelController"/> is null</exception>
            /// </summary>
            protected static void BindHeight(FrameworkElement renderElement,
                NumberFieldModelController heightController)
            {
                if (heightController == null) throw new ArgumentNullException(nameof(heightController));
                var heightBinding = new Binding
                {
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
            protected static void BindWidth(FrameworkElement renderElement, NumberFieldModelController widthController)
            {
                if (widthController == null) throw new ArgumentNullException(nameof(widthController));
                var widthBinding = new Binding
                {
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
                PointFieldModelController translateController)
            {
                if (translateController == null) throw new ArgumentNullException(nameof(translateController));
                var translateBinding = new Binding
                {
                    Source = translateController,
                    Path = new PropertyPath(nameof(translateController.Data)),
                    Mode = BindingMode.TwoWay,
                    Converter = new PointToTranslateTransformConverter()
                };
                renderElement.SetBinding(UIElement.RenderTransformProperty, translateBinding);
            }

            #endregion

            #region GettersAndSetters

            protected static NumberFieldModelController GetHeightField(DocumentController docController, Context context)
            {
                context = Context.SafeInitAndAddDocument(context, docController);
                return docController.GetField(DashConstants.KeyStore.HeightFieldKey)
                    .DereferenceToRoot<NumberFieldModelController>(context);
            }

            protected static NumberFieldModelController GetWidthField(DocumentController docController, Context context)
            {
                context = Context.SafeInitAndAddDocument(context, docController);
                return docController.GetField(DashConstants.KeyStore.WidthFieldKey)
                    .DereferenceToRoot<NumberFieldModelController>(context);
            }

            protected static PointFieldModelController GetPositionField(DocumentController docController, Context context)
            {
                context = Context.SafeInitAndAddDocument(context, docController);
                return docController.GetField(DashConstants.KeyStore.PositionFieldKey)
                    .DereferenceToRoot<PointFieldModelController>(context);
            }

            #endregion
        }

        /// <summary>
        /// Given a document, this provides an API for getting all of the layout documents that define it's view.
        /// </summary>
        public class LayoutCourtesyDocument : CourtesyDocument
        {

            // the active layout for the doc that was passed in
            public DocumentController ActiveLayoutDocController = null;

            public LayoutCourtesyDocument(DocumentController docController)
            {
                Document = docController;
                var activeLayout = Document.GetActiveLayout();
                ActiveLayoutDocController = activeLayout == null ? InstantiateActiveLayout(Document) : activeLayout.Data;
            }

            public IEnumerable<DocumentController> GetLayoutDocuments()
            {
                var layoutDataField =
                        ActiveLayoutDocController?.GetDereferencedField(DashConstants.KeyStore.DataKey, null);

                if (layoutDataField is DocumentCollectionFieldModelController) // layout data is a collection of documents each referencing some field
                    foreach (var d in (layoutDataField as DocumentCollectionFieldModelController).GetDocuments())
                        yield return d;
                else if (layoutDataField is DocumentFieldModelController) // layout data is a document referencing some field
                    yield return (layoutDataField as DocumentFieldModelController).Data;
                else yield return ActiveLayoutDocController; // TODO why would the layout be any other type of field model controller
            }

            protected override DocumentController GetLayoutPrototype()
            {
                throw new NotImplementedException();
            }

            protected override DocumentController InstantiatePrototypeLayout()
            {
                throw new NotImplementedException();
            }
            
            public override FrameworkElement makeView(DocumentController docController, Context context, bool isInterfaceBuilderLayout = true) {
                return MakeView(docController, context);
            }

            public static FrameworkElement MakeView(DocumentController docController, Context context = null)
            {
                context = Context.SafeInitAndAddDocument(context, docController);

                var docViewModel = new DocumentViewModel(docController)
                {
                    IsDetailedUserInterfaceVisible = false,
                    IsMoveable = false
                };
                var docView = new DocumentView(docViewModel);
                return docView;
            }


            private DocumentController InstantiateActiveLayout(DocumentController doc)
            {
                // instantiate default fields
                var fields = DefaultLayoutFields(new Point(), new Size(double.NaN, double.NaN));
                var newLayout = new DocumentController(fields, DashConstants.DocumentTypeStore.DefaultLayout);
                // since this is the first view of the document, set the prototype active layout to the new layout
                doc.SetPrototypeActiveLayout(newLayout);
                return newLayout;
            }
        }


        /// <summary>
        /// Given a reference to an operator field model, constructs a document type that displays that operator.
        /// </summary>
        public class OperatorBox : CourtesyDocument
        {
            public static DocumentType DocumentType =
                new DocumentType("53FC9C82-F32C-4704-AF6B-E55AC805C84F", "Operator Box");

            public OperatorBox(ReferenceFieldModelController refToOp)
            {
                var fields = DefaultLayoutFields(0, 0, double.NaN, double.NaN, refToOp);
                Document = new DocumentController(fields, DocumentType);
            }

            protected override DocumentController GetLayoutPrototype()
            {
                throw new NotImplementedException();
            }

            protected override DocumentController InstantiatePrototypeLayout()
            {
                throw new NotImplementedException();
            }

            public override FrameworkElement makeView(DocumentController docController,
                Context context, bool isInterfaceBuilderLayout = false) {
                return OperatorBox.MakeView(docController, context, isInterfaceBuilderLayout);
            }

            public static FrameworkElement MakeView(DocumentController docController,
                Context context, bool isInterfaceBuilderLayout = false) {
                var data = docController.GetField(DashConstants.KeyStore.DataKey) ?? null;
                var opfmc = (data as ReferenceFieldModelController);
                OperatorView opView = new OperatorView { DataContext = opfmc };
                if (isInterfaceBuilderLayout) return opView;
                return new SelectableContainer(opView, docController);
            }
        }

        /// <summary>
        /// A generic document type containing a single text element.
        /// </summary>
        public class DocumentBox : CourtesyDocument
        {
            public static DocumentType DocumentType =
                new DocumentType("7C92378E-C38E-4B28-90C4-F5EF495878E5", "Document Box");
            public DocumentBox(FieldModelController refToDoc, double x = 0, double y = 0, double w = 200, double h = 20)
            {
                var fields = DefaultLayoutFields(x, y, w, h, refToDoc);
                Document = new DocumentController(fields, DocumentType);
                //SetLayoutForDocument(Document, Document);
            }
            public static FrameworkElement MakeView(DocumentController docController, Context context, bool isInterfaceBuilderLayout = false)
            {
                // the document field model controller provides us with the DATA
                // the Document on this courtesty document provides us with the parameters to display the DATA.
                // X, Y, Width, and Height etc....

                ///* 
                ReferenceFieldModelController refToData;
                var fieldModelController = GetDereferencedDataFieldModelController(docController, context, new DocumentFieldModelController(new DocumentController(new Dictionary<Key, FieldModelController>(), DashConstants.DocumentTypeStore.TextBoxDocumentType)), out refToData);

                var documentfieldModelController = fieldModelController as DocumentFieldModelController;
                Debug.Assert(documentfieldModelController != null);

                var doc = fieldModelController.DereferenceToRoot<DocumentFieldModelController>(context);
                var docView = documentfieldModelController.Data.MakeViewUI(context, isInterfaceBuilderLayout);

                // bind the text height
                var docheightController = GetHeightField(docController, context);
                if (docheightController != null)
                    BindHeight(docView, docheightController);

                // bind the text width
                var docwidthController = GetWidthField(docController, context);
                if (docwidthController != null)
                    BindWidth(docView, docwidthController);

                if (isInterfaceBuilderLayout)
                {
                    return new SelectableContainer(docView, docController);
                }
                return docView;
                //*/ 

                return new TextBox();
            }

            protected override DocumentController GetLayoutPrototype()
            {
                throw new NotImplementedException();
            }

            protected override DocumentController InstantiatePrototypeLayout()
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// A generic document type containing a single text element.
        /// </summary>
        public class TextingBox : CourtesyDocument
        {
            public static Key FontWeightKey = new Key("03FC5C4B-6A5A-40BA-A262-578159E2D5F7", "FontWeight");
            public static Key FontSizeKey = new Key("75902765-7F0E-4AA6-A98B-3C8790DBF7CE", "FontSize");
            public static Key TextAlignmentKey = new Key("3BD4572A-C6C9-4710-8E74-831204D2C17D", "Font Alignment");
            public static DocumentType DocumentType =
                new DocumentType("181D19B4-7DEC-42C0-B1AB-365B28D8EA42", "Texting Box");

            public static string DefaultText = "Default Text";
            public static double DefaultFontWeight = 100;
            public static double DefaultTextAlignment = (int)TextAlignment.Left;
            public static double DefaultFontSize = 12;
            private static string PrototypeId = "F917C90C-14E8-45E0-A524-94C8958DDC4F";

            public TextingBox(FieldModelController refToText, double x = 0, double y = 0, double w = 200, double h = 20)
            {
                var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToText);
                Document = GetLayoutPrototype().MakeDelegate();
                Document.SetFields(fields, true);
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
                var textFieldModelController = new TextFieldModelController(DefaultText);
                var fields = DefaultLayoutFields(new Point(), new Size(double.NaN, double.NaN), textFieldModelController);
                var prototypeDocument = new DocumentController(fields, DocumentType, PrototypeId);

                SetFontWeightField(prototypeDocument, DefaultFontWeight, true, null);
                SetFontSizeField(prototypeDocument, DefaultFontSize, true, null);
                SetTextAlignmentField(prototypeDocument, DefaultTextAlignment, true, null);
                return prototypeDocument;
            }

            public override FrameworkElement makeView(DocumentController docController,
                Context context, bool isInterfaceBuilderLayout = false) {
                return MakeView(docController, context);
            }

            public static FrameworkElement MakeView(DocumentController docController, Context context, bool isInterfaceBuilderLayout = false) {
                // the text field model controller provides us with the DATA
                // the Document on this courtesty document provides us with the parameters to display the DATA.
                // X, Y, Width, and Height etc....

                // create the textblock
                FrameworkElement tb = null;

                // use the reference to the text to get the text field model controller
                var textField = GetTextField(docController, context);
                Debug.Assert(textField != null);
                if (textField is TextFieldModelController)
                {
                    var textBox = new TextBox();
                    textBox.GotFocus += (s, e) => textBox.ManipulationMode = ManipulationModes.None;
                    textBox.LostFocus += (s, e) => textBox.ManipulationMode = ManipulationModes.All;
                    tb = textBox;
                    tb.HorizontalAlignment = HorizontalAlignment.Stretch;
                    tb.VerticalAlignment = VerticalAlignment.Stretch;
                    textBox.TextWrapping = TextWrapping.Wrap;
                    var textFieldModelController = textField as TextFieldModelController;
                    BindTextBoxSource(tb, textFieldModelController);
                }
                else if (textField is NumberFieldModelController)
                {
                    tb = new TextBlock();
                    var numFieldModelController = textField as NumberFieldModelController;
                    BindTextBlockSource(tb, numFieldModelController);
                } else if (textField is RichTextFieldModelController)
                {
                    tb = new TextBlock();
                    var richTextFieldModelController = textField as RichTextFieldModelController;
                    BindTextBlockSource(tb, richTextFieldModelController);
                }

                // bind the text height
                var heightController = GetHeightField(docController, context);
                BindHeight(tb, heightController);

                // bind the text width
                var widthController = GetWidthField(docController, context);
                BindWidth(tb, widthController);

                var fontWeightController = GetFontWeightField(docController, context);
                BindFontWeight(tb, fontWeightController);

                var fontSizeController = GetFontSizeField(docController, context);
                BindFontSize(tb, fontSizeController);

                var textAlignmentController = GetTextAlignmentField(docController, context);
                BindTextAlignment(tb, textAlignmentController);

                // add bindings to work with operators
                var referenceToText = GetTextReference(docController);
                if (referenceToText != null) // only bind operation interactions if text is a reference
                {
                    BindOperationInteractions(tb, referenceToText);
                }

                var doc = referenceToText.GetDocumentController(context);
                doc.AddFieldUpdatedListener(referenceToText.FieldKey, delegate (DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
                {
                    Debug.Assert(args.Reference.FieldKey.Equals(referenceToText.FieldKey));
                    string id = args.Context.GetDeepestDelegateOf(args.Reference.DocId);
                    Debug.Assert(id.Equals(referenceToText.GetDocumentController(context).GetId()));
                    if (args.Action != DocumentController.FieldUpdatedAction.Update)
                    {
                        var field = GetTextField(docController, args.Context);
                        Debug.Assert(field != null);
                        if (field is TextFieldModelController)
                        {
                            var textFieldModelController = field as TextFieldModelController;
                            BindTextBoxSource(tb, textFieldModelController);
                        }
                        else if (field is NumberFieldModelController)
                        {
                            var numFieldModelController = field as NumberFieldModelController;
                            BindTextBlockSource(tb, numFieldModelController);
                        } else if (field is RichTextFieldModelController)
                        {
                            var richTextFieldModelController = field as RichTextFieldModelController;
                            BindTextBlockSource(tb, richTextFieldModelController);
                        }
                    }
                });

                if (isInterfaceBuilderLayout)
                {
                    var selectableContainer = new SelectableContainer(tb, docController);
                    return selectableContainer;
                }

                return tb;
            }


            #region GettersAndSetters

            private static FieldModelController GetTextField(DocumentController docController, Context context)
            {
                context = Context.SafeInitAndAddDocument(context, docController);
                return docController.GetField(DashConstants.KeyStore.DataKey)?
                    .DereferenceToRoot<FieldModelController>(context);
            }

            private static ReferenceFieldModelController GetTextReference(DocumentController docController)
            {
                return docController.GetField(DashConstants.KeyStore.DataKey) as ReferenceFieldModelController;
            }

            private static NumberFieldModelController GetTextAlignmentField(DocumentController docController, Context context)
            {
                context = Context.SafeInitAndAddDocument(context, docController);
                return docController.GetField(TextAlignmentKey)?
                    .DereferenceToRoot<NumberFieldModelController>(context);
            }

            private static NumberFieldModelController GetFontWeightField(DocumentController docController, Context context)
            {
                context = Context.SafeInitAndAddDocument(context, docController);
                return docController.GetField(FontWeightKey)?
                    .DereferenceToRoot<NumberFieldModelController>(context);
            }

            protected static NumberFieldModelController GetFontSizeField(DocumentController docController, Context context)
            {
                context = Context.SafeInitAndAddDocument(context, docController);
                return docController.GetField(FontSizeKey)?
                    .DereferenceToRoot<NumberFieldModelController>(context);
            }

            private void SetTextAlignmentField(DocumentController docController, double textAlignment, bool forceMask, Context context)
            {
                var currentTextAlignmentField = new NumberFieldModelController(textAlignment);
                docController.SetField(TextAlignmentKey, currentTextAlignmentField, forceMask); // set the field here so that forceMask is respected
            }

            private void SetFontSizeField(DocumentController docController, double fontSize, bool forceMask, Context context)
            {
                var currentFontSizeField = new NumberFieldModelController(fontSize);
                docController.SetField(FontSizeKey, currentFontSizeField, forceMask); // set the field here so that forceMask is respected
            }

            private void SetFontWeightField(DocumentController docController, double fontWeight, bool forceMask, Context context)
            {
                var currentFontWeightField = new NumberFieldModelController(fontWeight);
                docController.SetField(FontWeightKey, currentFontWeightField, forceMask); // set the field here so that forceMask is respected
            }

            #endregion

            #region Bindings

            protected static void BindTextAlignment(FrameworkElement renderElement,
                NumberFieldModelController textAlignmentController)
            {
                if (textAlignmentController == null) throw new ArgumentNullException(nameof(textAlignmentController));
                var alignmentBinding = new Binding
                {
                    Source = textAlignmentController,
                    Path = new PropertyPath(nameof(textAlignmentController.Data)),
                    Mode = BindingMode.TwoWay,
                    Converter = new IntToTextAlignmentConverter()
                };
                if (renderElement is TextBlock)
                {
                    renderElement.SetBinding(TextBlock.TextAlignmentProperty, alignmentBinding);
                }
                else if (renderElement is TextBox)
                {
                    renderElement.SetBinding(TextBox.TextAlignmentProperty, alignmentBinding);
                }
                else
                {
                    Debug.Assert(false, $"we don't support alignment for elements of type {renderElement.GetType()}");
                }
            }

            protected static void BindFontWeight(FrameworkElement renderElement,
                NumberFieldModelController fontWeightController)
            {
                if (fontWeightController == null) throw new ArgumentNullException(nameof(fontWeightController));
                var fontWeightBinding = new Binding
                {
                    Source = fontWeightController,
                    Path = new PropertyPath(nameof(fontWeightController.Data)),
                    Mode = BindingMode.TwoWay,
                    Converter = new DoubleToFontWeightConverter()
                };
                if (renderElement is TextBlock)
                {
                    renderElement.SetBinding(Control.FontWeightProperty, fontWeightBinding);
                }
                else if (renderElement is TextBox)
                {
                    renderElement.SetBinding(Control.FontWeightProperty, fontWeightBinding);
                }
                else
                {
                    Debug.Assert(false, $"we don't support fontweight for elements of type {renderElement.GetType()}");
                }
            }

            protected static void BindFontSize(FrameworkElement renderElement,
                NumberFieldModelController sizeController)
            {
                if (sizeController == null) throw new ArgumentNullException(nameof(sizeController));
                var fontSizeBinding = new Binding
                {
                    Source = sizeController,
                    Path = new PropertyPath(nameof(sizeController.Data)),
                    Mode = BindingMode.TwoWay,
                };
                if (renderElement is TextBlock)
                {
                    renderElement.SetBinding(Control.FontSizeProperty, fontSizeBinding);
                }
                else if (renderElement is TextBox)
                {
                    renderElement.SetBinding(Control.FontSizeProperty, fontSizeBinding);
                }
                else
                {
                    Debug.Assert(false, $"we don't support fontsize for elements of type {renderElement.GetType()}");
                }
            }

            private static void BindTextBlockSource(FrameworkElement renderElement, RichTextFieldModelController fieldModelController)
            {
                var sourceBinding = new Binding
                {
                    Source = fieldModelController,
                    Path = new PropertyPath(nameof(fieldModelController.RichTextData))
                };
                renderElement.SetBinding(TextBlock.TextProperty, sourceBinding);
            }

            private static void BindTextBlockSource(FrameworkElement renderElement, NumberFieldModelController fieldModelController)
            {
                //<<<<<<< HEAD
                var sourceBinding = new Binding
                {
                    Source = fieldModelController,
                    Path = new PropertyPath(nameof(fieldModelController.Data))
                };
                renderElement.SetBinding(TextBlock.TextProperty, sourceBinding);
                //=======
                //                // the text field model controller provides us with the DATA
                //                // the Document on this courtesty document provides us with the parameters to display the DATA.
                //                // X, Y, Width, and Height etc....

                //                // create the textblock
                //                FrameworkElement tb = null;
                //                FrameworkElement child = null;

                //                // use the reference to the text to get the text field model controller
                //                ReferenceFieldModelController refToData;
                //                var fieldModelController = GetDereferencedDataFieldModelController(docController, context, new TextFieldModelController("<default>"), out refToData);
                //                var doc = refToData.GetDocumentController(context);
                //                Debug.Assert(fieldModelController != null);
                //                if (fieldModelController is TextFieldModelController)
                //                {
                //                    var textFieldModelController = fieldModelController as TextFieldModelController;
                //                    var textBox = new TextBox();
                //                    textBox.ManipulationDelta += (s, e) => e.Handled = true;
                //                    tb = textBox;
                //                    textBox.AcceptsReturn = true;
                //                    tb.HorizontalAlignment = HorizontalAlignment.Stretch;
                //                    tb.VerticalAlignment = VerticalAlignment.Stretch;
                //                    // make text update when changed
                //                    var sourceBinding = new Binding
                //                    {
                //                        Source = textFieldModelController,
                //                        Path = new PropertyPath(nameof(textFieldModelController.Data)),
                //                        Mode = BindingMode.TwoWay
                //                    };
                //                    tb.SetBinding(TextBox.TextProperty, sourceBinding);
                //                    textBox.TextWrapping = Windows.UI.Xaml.TextWrapping.Wrap;
                //                    textBox.TextChanged += TextBox_TextChanged;
                //                    textBox.Tag = refToData.GetDocumentController(context);
                //                    child = textBox;
                //                }
                //                else if (fieldModelController is NumberFieldModelController)
                //                {
                //                    var numFieldModelController = fieldModelController as NumberFieldModelController;
                //                    var textBox = new TextBox();
                //                    textBox.ManipulationDelta += (s, e) => e.Handled = true;
                //                    tb = textBox;
                //                    textBox.AcceptsReturn = false;
                //                    tb.HorizontalAlignment = HorizontalAlignment.Stretch;
                //                    tb.VerticalAlignment = VerticalAlignment.Stretch;
                //                    // make text update when changed
                //                    var sourceBinding = new Binding
                //                    {
                //                        Source = numFieldModelController,
                //                        Converter = new StringToDoubleConverter(0),
                //                        Path = new PropertyPath(nameof(numFieldModelController.Data)),
                //                        Mode = BindingMode.TwoWay,
                //                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                //                    };
                //                    tb.SetBinding(TextBox.TextProperty, sourceBinding);
                //                    textBox.TextWrapping = Windows.UI.Xaml.TextWrapping.Wrap;
                //                    textBox.TextChanged += TextBox_NumberChanged;
                //                    textBox.Tag = numFieldModelController;
                //                    child = textBox;
                //                }
                //                else if (fieldModelController is DocumentFieldModelController)
                //                {
                //                    var documentfieldModelController = fieldModelController as DocumentFieldModelController;
                //                    return documentfieldModelController.Data.MakeViewUI(context);
                //                }

                //                doc.DocumentFieldUpdated += delegate (DocumentController.DocumentFieldUpdatedEventArgs args)
                //                {
                //                    string s = args.Context.GetDeepestDelegateOf(args.Reference.DocId);
                //                    if (args.Action != DocumentController.FieldUpdatedAction.Update && s.Equals(refToData.GetDocumentController(context).GetId()) && args.Reference.FieldKey.Equals(refToData.FieldKey))
                //                    {
                //                        var fmc = args.Reference.DereferenceToRoot(new Context(args.Context));
                //                        Binding sourceBinding = null;
                //                        if (fmc is TextFieldModelController)
                //                        {
                //                            TextFieldModelController tfmc = fmc as TextFieldModelController;
                //                            sourceBinding = new Binding
                //                            {
                //                                Source = fmc,
                //                                Path = new PropertyPath(nameof(tfmc.Data)),
                //                                Mode = BindingMode.TwoWay
                //                            };
                //                        }
                //                        else if (fmc is NumberFieldModelController)
                //                        {
                //                            NumberFieldModelController nfmc = fmc as NumberFieldModelController;
                //                            sourceBinding = new Binding
                //                            {
                //                                Source = nfmc,
                //                                Converter = new StringToDoubleConverter(0),
                //                                Path = new PropertyPath(nameof(nfmc.Data)),
                //                                Mode = BindingMode.TwoWay,
                //                                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                //                            };
                //                        }
                //                        child.SetBinding(TextBox.TextProperty, sourceBinding);
                //                    }
                //                };

                //                var border = new Border();
                //                border.Child = tb;

                //                var fontWeightController = GetFontWeightFieldController(docController, context);
                //                BindFontWeight(tb, fontWeightController);

                //                var fontSizeController = GetFontSizeFieldController(docController, context);
                //                BindFontSize(tb, fontSizeController);

                //                var textAlignmentController = GetTextAlignmentFieldController(docController, context);
                //                BindTextAlignment(tb, textAlignmentController);

                //                tb = border;
                //                // bind the text height
                //                var heightController = GetHeightFieldController(docController, context);
                //                BindHeight(tb, heightController);

                //                // bind the text width
                //                var widthController = GetWidthFieldController(docController, context);
                //                BindWidth(tb, widthController);

                //                // add bindings to work with operators
                //                BindOperationInteractions(refToData.Resolve(context), tb);

                //                border.BorderThickness = new Thickness(5);
                //                border.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(50, 50, 50, 50));

                //                return border;
                //>>>>>>> origin/master
            }

            private static void BindTextBoxSource(FrameworkElement renderElement, TextFieldModelController fieldModelController)
            {
                //<<<<<<< HEAD
                var sourceBinding = new Binding
                {
                    Source = fieldModelController,
                    Path = new PropertyPath(nameof(fieldModelController.Data)),
                    Mode = BindingMode.TwoWay
                };
                renderElement.SetBinding(TextBox.TextProperty, sourceBinding);
            }

            #endregion
        }

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
            }

            public override FrameworkElement makeView(DocumentController docController, Context context, bool isInterfaceBuilderLayout = false)
            {
                return MakeView(docController, context);
            }

            
            public static FrameworkElement MakeView(DocumentController docController, Context context, bool isInterfaceBuilderLayout = false) {
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
                BindOperationInteractions(image, GetImageReference(docController));

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


            private static void SetOpacityField(DocumentController docController, double opacity, bool forceMask, Context context)
            {
                var currentOpacityField = new NumberFieldModelController(opacity);
                docController.SetField(OpacityKey, currentOpacityField, forceMask); // set the field here so that forceMask is respected

            }

            private static ImageFieldModelController GetImageField(DocumentController docController, Context context)
            {
                context = Context.SafeInitAndAddDocument(context, docController);
                return docController.GetField(DashConstants.KeyStore.DataKey).DereferenceToRoot<ImageFieldModelController>(context);
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

        /// <summary>
        /// A generic data wrappe document display type used to display images or text fields.
        /// </summary>
        public class DataBox : CourtesyDocument
        {
            CourtesyDocument _doc;

            public DataBox(ReferenceFieldModelController refToField, bool isImage)
            {
                if (isImage)
                    _doc = new ImageBox(refToField);
                else
                    _doc = new TextingBox(refToField);
            }

            public override DocumentController Document
            {
                get { return _doc.Document; }
                set { _doc.Document = value; }
            }

            protected override DocumentController InstantiatePrototypeLayout()
            {
                throw new NotImplementedException();
            }

            protected override DocumentController GetLayoutPrototype()
            {
                throw new NotImplementedException();
            }

            public override FrameworkElement makeView(DocumentController docController,
                Context context, bool isInterfaceBuilderLayout = true) {
                return _doc.makeView(docController, context);
            }
        }

        /// <summary>
        /// The data field for a collection is a document collection field model controller
        /// </summary>
        public class CollectionBox : CourtesyDocument
        {

            public static DocumentType DocumentType = new DocumentType("7C59D0E9-11E8-4F12-B355-20035B3AC359", "Generic Collection");
            private static string PrototypeId = "E1F828EA-D44D-4C3C-BE22-9AAF369C3F19";


            public CollectionBox(FieldModelController refToCollection, double x = 0, double y = 0, double w = 400, double h = 400)
            {
                var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToCollection);
                Document = GetLayoutPrototype().MakeDelegate();
                Document.SetFields(fields, true);
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
                var docFieldModelController = new DocumentCollectionFieldModelController(new List<DocumentController>());
                var fields = DefaultLayoutFields(new Point(), new Size(double.NaN, double.NaN), docFieldModelController);
                fields[DashConstants.KeyStore.IconTypeFieldKey] = new NumberFieldModelController((int)IconTypeEnum.Collection); // TODO factor out into SetIconField() method in base class
                var prototypeDocument = new DocumentController(fields, DocumentType, PrototypeId);
                return prototypeDocument;
            }

            public override FrameworkElement makeView(DocumentController docController,
                Context context, bool isInterfaceBuilderLayout = false) {
                return CollectionBox.MakeView(docController, context, isInterfaceBuilderLayout);
            }

            public static FrameworkElement MakeView(DocumentController docController,
                Context context, bool isInterfaceBuilderLayout = false) {
                var data = docController.GetDereferencedField(DashConstants.KeyStore.DataKey, context) ?? null;

                if (data != null)
                {
                    var opacity = (docController.GetDereferencedField(new Key("opacity", "opacity"), context) as NumberFieldModelController)?.Data;

                    double opacityValue = opacity.HasValue ? (double)opacity : 1;

                    var collectionFieldModelController = data
                        .DereferenceToRoot<DocumentCollectionFieldModelController>(context);
                    Debug.Assert(collectionFieldModelController != null);

                    var collectionViewModel = new CollectionViewModel(collectionFieldModelController, context);

                    var view = new CollectionView(collectionViewModel);
                    view.Opacity = opacityValue;
                    if (isInterfaceBuilderLayout)
                    {
                        return new SelectableContainer(view, docController);
                    }
                    return view;
                }
                return new Grid();
            }
        }

        public class GridViewLayout : CourtesyDocument
        {
            private static string PrototypeId = "C2EB5E08-1C04-44BF-970A-DB213949EE48";
            public static DocumentType DocumentType = new DocumentType("B7A022D4-B667-469C-B47E-3A84C0AA78A0", "GridView Layout");

            public GridViewLayout(IList<DocumentController> layoutDocuments, Point position = new Point(), Size size = new Size())
            {
                Document = GetLayoutPrototype().MakeDelegate();
                var layoutDocumentCollection = new DocumentCollectionFieldModelController(layoutDocuments);
                var fields = DefaultLayoutFields(position, size, layoutDocumentCollection);
                Document.SetFields(fields, true); //TODO add fields to constructor parameters                
            }

            public GridViewLayout() : this(new List<DocumentController>()) { }

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
                var layoutDocCollection = new DocumentCollectionFieldModelController(new List<DocumentController>());
                var fields = DefaultLayoutFields(new Point(), new Size(double.NaN, double.NaN), layoutDocCollection);
                var prototypeDocument = new DocumentController(fields, DocumentType, PrototypeId);
                return prototypeDocument;
            }

            public override FrameworkElement makeView(DocumentController docController, Context context, bool isInterfaceBuilderLayout = false)
            {
                throw new NotImplementedException("We don't have access to the data document here");
            }

            public static FrameworkElement MakeView(DocumentController docController, Context context, DocumentController dataDocument, bool isInterfaceBuilderLayout = false)
            {

                var grid = new Grid();
                var gridView = new GridView
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };
                LayoutDocuments(docController, context, gridView, isInterfaceBuilderLayout);

                docController.DocumentFieldUpdated += delegate (DocumentController sender,
                    DocumentController.DocumentFieldUpdatedEventArgs args)
                {
                    if (args.Reference.FieldKey.Equals(DashConstants.KeyStore.DataKey))
                    {
                        LayoutDocuments(sender, args.Context, gridView, isInterfaceBuilderLayout);
                    }
                };
                grid.Children.Add(gridView);
                if (isInterfaceBuilderLayout)
                {
                    return new SelectableContainer(grid, docController, dataDocument);
                }
                return grid;
            }

            private static void LayoutDocuments(DocumentController docController, Context context, GridView grid, bool isInterfaceBuilder)
            {
                var layoutDocuments = GetLayoutDocumentCollection(docController, context).GetDocuments();
                ObservableCollection<FrameworkElement> itemsSource = new ObservableCollection<FrameworkElement>();
                foreach (var layoutDocument in layoutDocuments)
                {
                    var layoutView = layoutDocument.MakeViewUI(context, isInterfaceBuilder);
                    layoutView.HorizontalAlignment = HorizontalAlignment.Left;
                    layoutView.VerticalAlignment = VerticalAlignment.Top;

                    itemsSource.Add(layoutView);
                }
                grid.ItemsSource = itemsSource;
            }

            private static DocumentCollectionFieldModelController GetLayoutDocumentCollection(DocumentController docController, Context context)
            {
                context = Context.SafeInitAndAddDocument(context, docController);
                return docController.GetField(DashConstants.KeyStore.DataKey)?
                    .DereferenceToRoot<DocumentCollectionFieldModelController>(context);
            }
        }

        public class ListViewLayout : CourtesyDocument
        {
            private static string PrototypeId = "C512FC2E-CDD1-4E94-A98F-35A65E821C08";
            public static DocumentType DocumentType = new DocumentType("3E5C2739-A511-40FF-9B2E-A875901B296D", "ListView Layout");

            public ListViewLayout(IList<DocumentController> layoutDocuments, Point position = new Point(), Size size = new Size())
            {
                Document = GetLayoutPrototype().MakeDelegate();
                var layoutDocumentCollection = new DocumentCollectionFieldModelController(layoutDocuments);
                var fields = DefaultLayoutFields(position, size, layoutDocumentCollection);
                Document.SetFields(fields, true); //TODO add fields to constructor parameters                
            }

            public ListViewLayout() : this(new List<DocumentController>()) { }

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
                var layoutDocCollection = new DocumentCollectionFieldModelController(new List<DocumentController>());
                var fields = DefaultLayoutFields(new Point(), new Size(double.NaN, double.NaN), layoutDocCollection);
                var prototypeDocument = new DocumentController(fields, DocumentType, PrototypeId);
                return prototypeDocument;
            }

            public override FrameworkElement makeView(DocumentController docController, Context context, bool isInterfaceBuilderLayout = false)
            {
                return MakeView(docController, context);
            }

            public static FrameworkElement MakeView(DocumentController docController, Context context, bool isInterfaceBuilderLayout = false)
            {

                var grid = new Grid();
                var listView = new ListView
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                };
                LayoutDocuments(docController, context, listView, isInterfaceBuilderLayout);

                docController.DocumentFieldUpdated += delegate (DocumentController sender,
                    DocumentController.DocumentFieldUpdatedEventArgs args)
                {
                    if (args.Reference.FieldKey.Equals(DashConstants.KeyStore.DataKey))
                    {
                        LayoutDocuments(sender, args.Context, listView, isInterfaceBuilderLayout);
                    }
                };
                grid.Children.Add(listView);
                if (isInterfaceBuilderLayout)
                {
                    return new SelectableContainer(grid, docController);
                }
                return grid;
            }

            private static void LayoutDocuments(DocumentController docController, Context context, ListView list, bool isInterfaceBuilder)
            {
                var layoutDocuments = GetLayoutDocumentCollection(docController, context).GetDocuments();
                ObservableCollection<FrameworkElement> itemsSource = new ObservableCollection<FrameworkElement>();
                foreach (var layoutDocument in layoutDocuments)
                {
                    var layoutView = layoutDocument.MakeViewUI(context, isInterfaceBuilder);
                    layoutView.HorizontalAlignment = HorizontalAlignment.Left;
                    layoutView.VerticalAlignment = VerticalAlignment.Top;

                    itemsSource.Add(layoutView);
                }
                list.ItemsSource = itemsSource;
            }

            private static DocumentCollectionFieldModelController GetLayoutDocumentCollection(DocumentController docController, Context context)
            {
                context = Context.SafeInitAndAddDocument(context, docController);
                return docController.GetField(DashConstants.KeyStore.DataKey)?
                    .DereferenceToRoot<DocumentCollectionFieldModelController>(context);
            }
        }

        public class FreeFormDocument : CourtesyDocument
        {
            private static string PrototypeId = "A5614540-0A50-40F3-9D89-965B8948F2A2";

            public FreeFormDocument(IList<DocumentController> layoutDocuments, Point position = new Point(), Size size = new Size())
            {
                Document = GetLayoutPrototype().MakeDelegate();
                var layoutDocumentCollection = new DocumentCollectionFieldModelController(layoutDocuments);
                var fields = DefaultLayoutFields(position, size, layoutDocumentCollection);
                Document.SetFields(fields, true); //TODO add fields to constructor parameters     

                //Document.SetField(DashConstants.KeyStore.IconTypeFieldKey, new NumberFieldModelController((double)IconTypeEnum.Api), true);
            }

            public FreeFormDocument() : this(new List<DocumentController>()) { }

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
                var layoutDocCollection = new DocumentCollectionFieldModelController(new List<DocumentController>());
                var fields = DefaultLayoutFields(new Point(), new Size(double.NaN, double.NaN), layoutDocCollection);
                var prototypeDocument = new DocumentController(fields, DashConstants.DocumentTypeStore.FreeFormDocumentLayout, PrototypeId);
                return prototypeDocument;
            }

            public override FrameworkElement makeView(DocumentController docController, Context context, bool isInterfaceBuilderLayout = false)
            {
                throw new NotImplementedException("We don't have the dataDocument here and right now this is never called anyway");
            }

            public static FrameworkElement MakeView(DocumentController docController, Context context, DocumentController dataDocument, bool isInterfaceBuilderLayout = false)
            {

                var grid = new Grid();
                LayoutDocuments(docController, context, grid, isInterfaceBuilderLayout);

                docController.AddFieldUpdatedListener(DashConstants.KeyStore.DataKey, delegate (DocumentController sender,
                    DocumentController.DocumentFieldUpdatedEventArgs args)
                {
                    if (args.Reference.FieldKey.Equals(DashConstants.KeyStore.DataKey))
                    {
                        LayoutDocuments(sender, args.Context, grid, isInterfaceBuilderLayout);
                    }
                });
                if (isInterfaceBuilderLayout)
                {
                    //DropControls controls = new DropControls(grid, docController);
                    return new SelectableContainer(grid, docController, dataDocument);
                }
                return grid;
            }

            private static void LayoutDocuments(DocumentController docController, Context context, Grid grid, bool isInterfaceBuilder)
            {
                var layoutDocuments = GetLayoutDocumentCollection(docController, context).GetDocuments();
                grid.Children.Clear();
                foreach (var layoutDocument in layoutDocuments)
                {
                    var layoutView = layoutDocument.MakeViewUI(context, isInterfaceBuilder);
                    layoutView.HorizontalAlignment = HorizontalAlignment.Left;
                    layoutView.VerticalAlignment = VerticalAlignment.Top;

                    var positionField = layoutDocument.GetPositionField(context);
                    BindTranslation(layoutView, positionField);

                    grid.Children.Add(layoutView);
                }
            }

            private static DocumentCollectionFieldModelController GetLayoutDocumentCollection(DocumentController docController, Context context)
            {
                context = Context.SafeInitAndAddDocument(context, docController);
                return docController.GetField(DashConstants.KeyStore.DataKey)?
                    .DereferenceToRoot<DocumentCollectionFieldModelController>(context);
            }
        }

        public class RichTextBox : CourtesyDocument
        {
            public static DocumentType DocumentType = new DocumentType("ED3B2D3C-C3EA-4FDC-9C0C-71E10F549C5F", "Rich Text");

            public RichTextBox(FieldModelController refToRichText, double x = 0, double y = 0, double w = 200, double h = 20)
            {
                var fields = DefaultLayoutFields(x, y, w, h, refToRichText);
                Document = new DocumentController(fields, DocumentType);
                SetLayoutForDocument(Document, Document, forceMask: true, addToLayoutList: true);
            }

            public static FrameworkElement MakeView(DocumentController docController,
                Context context, bool isInterfaceBuilderLayout = false)
            {
                RichTextView rtv = null;
                var refToRichText =
                    docController.GetField(DashConstants.KeyStore.DataKey) as ReferenceFieldModelController;
                Debug.Assert(refToRichText != null);
                var fieldModelController = refToRichText.DereferenceToRoot(context);
                if (fieldModelController is RichTextFieldModelController)
                {
                    var richTextFieldModelController = fieldModelController as RichTextFieldModelController;
                    Debug.Assert(richTextFieldModelController != null);
                    var richText = new RichTextView(richTextFieldModelController);
                    rtv = richText;
                    rtv.HorizontalAlignment = HorizontalAlignment.Stretch;
                    rtv.VerticalAlignment = VerticalAlignment.Stretch;
                }

                // bind the rich text height
                var heightController = GetHeightField(docController, context);
                BindHeight(rtv, heightController);

                // bind the rich text width
                var widthController = GetWidthField(docController, context);
                BindWidth(rtv, widthController);
                
                if (isInterfaceBuilderLayout)
                {
                    return new SelectableContainer(rtv, docController);
                }
                return rtv;
            }

            protected override DocumentController GetLayoutPrototype()
            {
                throw new NotImplementedException();
            }

            protected override DocumentController InstantiatePrototypeLayout()
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Constructs a nested stackpanel that displays the fields of all documents in the list
        /// docs.
        /// </summary>
        public class StackingPanel : CourtesyDocument
        {
            public static DocumentType StackPanelDocumentType =
                new DocumentType("61369301-820F-4779-8F8C-701BCB7B0CB7", "Stack Panel");
            public static Key StyleKey = new Key("943A801F-A4F4-44AE-8390-31630055D62F", "Style");

            static public DocumentType DocumentType
            {
                get { return StackPanelDocumentType; }
            }

            public bool FreeForm;

            public StackingPanel(IEnumerable<DocumentController> docs, bool freeForm)
            {
                FreeForm = freeForm;
                var fields = DefaultLayoutFields(0, 0, double.NaN, double.NaN, new DocumentCollectionFieldModelController(docs));
                fields.Add(StyleKey, new TextFieldModelController(freeForm ? "Free Form" : "Stacked"));
                Document = new DocumentController(fields, StackPanelDocumentType);
            }

            protected override DocumentController GetLayoutPrototype()
            {
                throw new NotImplementedException();
            }

            protected override DocumentController InstantiatePrototypeLayout()
            {
                throw new NotImplementedException();
            }

            public override FrameworkElement makeView(DocumentController docController, Context context, bool isInterfaceBuilderLayout = false) {
                throw new NotImplementedException("We don't have access to the data document here");
            }

            /// <summary>
            /// Genereates the grid view to contain the stacked elements.
            /// </summary>
            /// <param name="docController"></param>
            /// <param name="context"></param>
            /// <param name="isInterfaceBuilderLayout"></param>
            /// <param name="dataDocument"></param>
            /// <returns></returns>
            public static FrameworkElement MakeView(DocumentController docController, Context context, DocumentController dataDocument, bool isInterfaceBuilderLayout = false)
            {
                if ((docController.GetDereferencedField(StyleKey, context) as TextFieldModelController).TextFieldModel.Data == "Free Form")
                    return MakeFreeFormView(docController, context, isInterfaceBuilderLayout);
                var stack = new  GridView();
                stack.Loaded += (s, e) =>
                {
                    var stackViewer = stack.GetFirstDescendantOfType<ScrollViewer>();
                    var stackScrollBar = stackViewer.GetFirstDescendantOfType<ScrollBar>();
                    stackScrollBar.ManipulationMode = ManipulationModes.All;
                    stackScrollBar.ManipulationDelta += (ss, ee) => ee.Handled = true;
                };
                var stackFieldData =
                    docController.GetDereferencedField(DashConstants.KeyStore.DataKey, context)
                    as DocumentCollectionFieldModelController;

                // create a dynamic gridview that wraps content in borders
                if (stackFieldData != null)
                {
                    CreateStack(context, stack, stackFieldData, isInterfaceBuilderLayout);
                    stackFieldData.OnDocumentsChanged += delegate (IEnumerable<DocumentController> documents)
                    {
                        CreateStack(context, stack, stackFieldData, isInterfaceBuilderLayout);
                    };
                }
                if (isInterfaceBuilderLayout)
                {
                    return new SelectableContainer(stack, docController, dataDocument);
                }
                return stack;
            }

            private static void CreateStack(Context context, GridView stack, DocumentCollectionFieldModelController stackFieldData, bool isInterfaceBuilderLayout)
            {
                double maxHeight = 0;
                stack.Items.Clear();
                foreach (var stackDoc in stackFieldData.GetDocuments())
                {
                    Border b = new Border();
                    FrameworkElement item = stackDoc.MakeViewUI(context, isInterfaceBuilderLayout);
                    b.Child = item;
                    maxHeight = Math.Max(maxHeight, double.IsNaN(item.Height) ? 0 : item.Height);
                    stack.Items.Add(b);
                }
                foreach (Border b in stack.Items)
                {
                    b.Height = maxHeight;
                }
            }

            public static FrameworkElement MakeFreeFormView(DocumentController docController, Context context, bool isInterfaceBuilderLayout)
            {
                var stack = new Grid();
                stack.HorizontalAlignment = HorizontalAlignment.Left;
                stack.VerticalAlignment = VerticalAlignment.Top;
                var stackFieldData =
                    docController.GetDereferencedField(DashConstants.KeyStore.DataKey, context)
                    as DocumentCollectionFieldModelController;

                // create a dynamic gridview that wraps content in borders
                if (stackFieldData != null)
                    foreach (var stackDoc in stackFieldData.GetDocuments())
                    {

                        FrameworkElement item = stackDoc.MakeViewUI(context, isInterfaceBuilderLayout);
                        var posController = GetPositionField(stackDoc, context);

                        item.HorizontalAlignment = HorizontalAlignment.Left;
                        item.VerticalAlignment = VerticalAlignment.Top;
                        BindTranslation(item, posController);
                        stack.Children.Add(item);
                    }
                if (isInterfaceBuilderLayout)
                {
                    return new SelectableContainer(stack, docController);
                }
                return stack;
            }
        }

        public class AnnotatedImage : CourtesyDocument
        {
            public static DocumentType ImageDocType = new DocumentType("41E1280D-1BA9-4C3F-AE72-4080677E199E", "Image Doc");
            public static Key Image1FieldKey = new Key("827F581B-6ECB-49E6-8EB3-B8949DE0FE21", "Annotate Image");
            public static Key TextFieldKey = new Key("73A8E9AB-A798-4FA0-941E-4C4A5A2BF9CE", "TextField");
            static DocumentController _prototypeDoc = CreatePrototypeDoc();
            static DocumentController _prototypeLayout = CreatePrototypeLayout();

            static DocumentController CreatePrototypeDoc()
            {
                return new DocumentController(new Dictionary<Key, FieldModelController>(), ImageDocType);
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
                var prototypeTextLayout = new TextingBox(new DocumentReferenceController(_prototypeDoc.GetId(), TextFieldKey), 0, 0, 200, 50);
                var prototypeImage1Layout = new ImageBox(new DocumentReferenceController(_prototypeDoc.GetId(), Image1FieldKey), 0, 50, 200, 200);

                var prototypeLayout = new StackingPanel(new DocumentController[] { prototypeImage1Layout.Document, prototypeTextLayout.Document }, true);

                prototypeTextLayout.Document.SetField(DashConstants.KeyStore.WidthFieldKey, new DocumentReferenceController(prototypeLayout.Document.GetId(), DashConstants.KeyStore.WidthFieldKey), true);
                prototypeImage1Layout.Document.SetField(DashConstants.KeyStore.WidthFieldKey, new DocumentReferenceController(prototypeLayout.Document.GetId(), DashConstants.KeyStore.WidthFieldKey), true);
                prototypeImage1Layout.Document.SetField(DashConstants.KeyStore.HeightFieldKey, new DocumentReferenceController(prototypeLayout.Document.GetId(), DashConstants.KeyStore.HeightFieldKey), true);

                return prototypeLayout.Document;
            }

            public AnnotatedImage(Uri imageUri, string text)
            {
                Document = _prototypeDoc.MakeDelegate();
                Document.SetField(Image1FieldKey, new ImageFieldModelController(imageUri), true);
                Document.SetField(TextFieldKey, new TextFieldModelController(text), true);
                var docLayout = _prototypeLayout.MakeDelegate();
                docLayout.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
                docLayout.SetField(DashConstants.KeyStore.HeightFieldKey, new NumberFieldModelController(250), true);
                docLayout.SetField(DashConstants.KeyStore.WidthFieldKey, new NumberFieldModelController(200), true);
                //SetLayoutForDocument(Document, docLayout);
            }

            protected override DocumentController GetLayoutPrototype()
            {
                throw new NotImplementedException();
            }

            protected override DocumentController InstantiatePrototypeLayout()
            {
                throw new NotImplementedException();
            }
        }
        public class TwoImages : CourtesyDocument
        {
            public static DocumentType TwoImagesType = new DocumentType("FC8EF5EB-1A0B-433C-85B6-6929B974A4B7", "Two Images");
            public static Key Image1FieldKey = new Key("827F581B-6ECB-49E6-8EB3-B8949DE0FE21", "ImageField1");
            public static Key Image2FieldKey = new Key("BCB1109C-0C55-47B7-B1E3-34CA9C66627E", "ImageField2");
            public static Key AnnotatedFieldKey = new Key("F370A8F6-22D9-4442-A528-A7FEEC29E306", "AnnotatedImage");
            public static Key TextFieldKey = new Key("73A8E9AB-A798-4FA0-941E-4C4A5A2BF9CE", "TextField");
            public static Key RichTextKey = new Key("1C46E96E-F3CB-4DEE-8799-AD71DB1FB4D1", "RichTextField");
            static DocumentController _prototypeTwoImages = CreatePrototype2Images();
            static DocumentController _prototypeLayout = CreatePrototypeLayout();

            static DocumentController CreatePrototype2Images()
            {
                // bcz: default values for data fields can be added, but should not be needed
                Dictionary<Key, FieldModelController> fields = new Dictionary<Key, FieldModelController>();
                fields.Add(TextFieldKey, new TextFieldModelController("Prototype Text"));
                fields.Add(Image1FieldKey, new ImageFieldModelController(new Uri("ms-appx://Dash/Assets/cat.jpg")));
                fields.Add(Image2FieldKey, new ImageFieldModelController(new Uri("ms-appx://Dash/Assets/cat2.jpeg")));
                fields.Add(AnnotatedFieldKey, new ImageFieldModelController(new Uri("ms-appx://Dash/Assets/cat2.jpeg")));
                return new DocumentController(fields, TwoImagesType);

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
                var prototypeImage1Layout = new ImageBox(new DocumentReferenceController(_prototypeTwoImages.GetId(), Image1FieldKey), 0, 50, 200, 200);
                var prototypeImage2Layout = new ImageBox(new DocumentReferenceController(_prototypeTwoImages.GetId(), Image2FieldKey), 0, 250, 200, 200);
                var prototypeAnnotatedLayout = new DocumentBox(new DocumentReferenceController(_prototypeTwoImages.GetId(), AnnotatedFieldKey), 0, 450, 200, 250);
                var prototypeTextLayout = new TextingBox(new DocumentReferenceController(_prototypeTwoImages.GetId(), TextFieldKey), 0, 0, 200, 50);
                var prototypeLayout = new StackingPanel(new[] { prototypeTextLayout.Document, prototypeImage1Layout.Document, prototypeTextLayout.Document, prototypeImage2Layout.Document }, true);
                prototypeLayout.Document.SetField(DashConstants.KeyStore.HeightFieldKey, new NumberFieldModelController(700), true);
                prototypeLayout.Document.SetField(DashConstants.KeyStore.WidthFieldKey, new NumberFieldModelController(200), true);

                return prototypeLayout.Document;
            }

            public TwoImages(bool displayFieldsAsDocuments)
            {
                Document = _prototypeTwoImages.MakeDelegate();
                Document.SetField(Image1FieldKey, new ImageFieldModelController(new Uri("ms-appx://Dash/Assets/cat.jpg")), true);
                Document.SetField(Image2FieldKey, new ImageFieldModelController(new Uri("ms-appx://Dash/Assets/cat2.jpeg")), true);
                Document.SetField(AnnotatedFieldKey, new DocumentFieldModelController(new AnnotatedImage(new Uri("ms-appx://Dash/Assets/cat2.jpeg"), "Yowling").Document), true);
                Document.SetField(TextFieldKey, new TextFieldModelController("Hello World!"), true);
                Document.SetField(RichTextKey, new RichTextFieldModelController(null), true);

                var docLayout = _prototypeLayout.MakeDelegate();
                docLayout.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
                docLayout.SetField(new Key("opacity", "opacity"), new NumberFieldModelController(0.8), true);
                SetLayoutForDocument(Document, docLayout, forceMask: true, addToLayoutList: true);

            }

            protected override DocumentController GetLayoutPrototype()
            {
                throw new NotImplementedException();
            }

            protected override DocumentController InstantiatePrototypeLayout()
            {
                throw new NotImplementedException();
            }
        }

        public class NestedDocExample : CourtesyDocument
        {
            public static DocumentType NestedDocExampleType =
                new DocumentType("700FAEE4-5520-4E5E-9AED-3C8C5C1BE58B", "Nested Doc Example");

            public static Key TextFieldKey = new Key("73A8E9AB-A798-4FA0-941E-4C4A5A2BF9CE", "TextField");
            public static Key TextField2Key = new Key("B53F1453-4C52-4302-96A3-A6B40DA7D587", "TextField2");
            public static Key TwoImagesKey = new Key("4E5C2B62-905D-4952-891D-24AADE14CA80", "TowImagesField");

            public NestedDocExample(bool displayFieldsAsDocuments)
            {
                // create a document with two images
                var twoModel = new DocumentFieldModelController(new TwoImages(displayFieldsAsDocuments).Document);
                var tModel = new TextFieldModelController("Nesting");
                var tModel2 = new TextFieldModelController("More Nesting");
                var fields = new Dictionary<Key, FieldModelController>
                {
                    [TextFieldKey] = tModel,
                    [TwoImagesKey] = twoModel,
                    [TextField2Key] = tModel2
                };
                Document = new DocumentController(fields, NestedDocExampleType);

                var tBox = new TextingBox(new DocumentReferenceController(Document.GetId(), TextFieldKey))
                    .Document;
                var imBox1 = twoModel.Data;
                var tBox2 = new TextingBox(new DocumentReferenceController(Document.GetId(), TextField2Key))
                    .Document;

                var stackPan = new StackingPanel(new DocumentController[] { tBox, imBox1, tBox2 }, false).Document;

                SetLayoutForDocument(Document, stackPan, forceMask: true, addToLayoutList: true);
            }

            protected override DocumentController GetLayoutPrototype()
            {
                throw new NotImplementedException();
            }

            protected override DocumentController InstantiatePrototypeLayout()
            {
                throw new NotImplementedException();
            }
        }

        public class Numbers : CourtesyDocument
        {
            public static DocumentType NumbersType =
                new DocumentType("8FC422AB-015E-4B72-A28B-16271808C888", "Numbers");

            public static Key Number1FieldKey = new Key("0D3B939F-1E74-4577-8ACC-0685111E451C", "Number1");
            public static Key Number2FieldKey = new Key("56162B53-B02D-4880-912F-9D66B5F1F15B", "Number2");
            public static Key Number3FieldKey = new Key("61C34393-7DF7-4F26-9FDF-E0B138532F39", "Number3");
            public static Key Number4FieldKey = new Key("953D09E5-5770-4ED3-BC3F-76DFB22619E8", "Number4");
            public static Key Number5FieldKey = new Key("F59AAEC1-FCB6-4543-89CB-13ED5C5FD893", "Number5");

            public Numbers()
            {
                // create a document with two images
                var fields = DefaultLayoutFields(0, 0, double.NaN, double.NaN, null);
                fields.Add(Number1FieldKey, new NumberFieldModelController(789));
                fields.Add(Number2FieldKey, new NumberFieldModelController(23));
                fields.Add(Number3FieldKey, new NumberFieldModelController(8));
                Random r = new Random();
                fields.Add(Number4FieldKey, new NumberFieldModelController((r.NextDouble() - 0.5) * 600));
                fields.Add(Number5FieldKey, new NumberFieldModelController((r.NextDouble() - 0.5) * 600));

                Document = new DocumentController(fields, NumbersType);

                var tBox1 = new TextingBox(new DocumentReferenceController(Document.GetId(), Number1FieldKey), 0,
                    0, 60, 35).Document;
                var tBox2 = new TextingBox(new DocumentReferenceController(Document.GetId(), Number2FieldKey), 0,
                    0, 60, 35).Document;
                var tBox3 = new TextingBox(new DocumentReferenceController(Document.GetId(), Number3FieldKey), 0,
                    0, 60, 35).Document;
                var tBox4 = new TextingBox(new DocumentReferenceController(Document.GetId(), Number4FieldKey), 0,
                    0, 60, 35).Document;
                var tBox5 = new TextingBox(new DocumentReferenceController(Document.GetId(), Number5FieldKey), 0,
                    0, 60, 35).Document;
                var tBox6 = new TextingBox(new DocumentReferenceController(Document.GetId(), Number3FieldKey), 0,
                    0, 60, 35).Document;

                var stackPan = new StackingPanel(new[] { tBox1, tBox2, tBox3, tBox4, tBox5, tBox6 }, false).Document;

                SetLayoutForDocument(Document, stackPan, forceMask: true, addToLayoutList: true);
            }

            protected override DocumentController GetLayoutPrototype()
            {
                throw new NotImplementedException();
            }

            protected override DocumentController InstantiatePrototypeLayout()
            {
                throw new NotImplementedException();
            }
        }

        // == API COURTESY DOCUMENTS ==


        /// <summary>
        /// Wrapper document to display the ApiSourceCreatorDisplay Usercontrol.
        /// </summary>
        public class ApiDocumentModel : CourtesyDocument
        {
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


            public ApiDocumentModel()
            {
                var fields = new Dictionary<Key, FieldModelController>
                {
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
                CheckBox required, Key parameterCollectionKey, ApiSourceDisplay sourceDisplay)
            {
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
                var fields = new Dictionary<Key, FieldModelController>
                {
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
                var sourceBinding = new Binding
                {
                    Source = textFieldModelController,
                    Path = new PropertyPath(nameof(textFieldModelController.Data)),
                    Mode = BindingMode.OneWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                apiprop.XKey.SetBinding(TextBlock.TextProperty, sourceBinding);
                bindToTextBox(apiprop.XValue, ret.GetField(ValueTextKey));

                // bind source visibility to display checkbox which is bound to backend display field of param document
                var binding = new Binding
                {
                    Source = display,
                    Path = new PropertyPath(nameof(display.IsChecked)),
                    Mode = BindingMode.OneWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    Converter = new BoolToVisibilityConverter()
                };
                apiprop.SetBinding(ApiProperty.VisibilityProperty, binding);

                // bind ApiRequired property to the required checkbox
                var bindin = new Binding
                {
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
                Key parameterCollectionKey, ApiSourceDisplay sourceDisplay)
            {
                Debug.Assert(docController.DocumentType == DocumentType);
                Debug.Assert(parameterCollectionKey == AuthParametersKey ||
                             parameterCollectionKey == AuthHeadersKey ||
                             parameterCollectionKey == ParametersKey || parameterCollectionKey == HeadersKey);

                DocumentCollectionFieldModelController col =
                    (DocumentCollectionFieldModelController)docController.GetField(parameterCollectionKey);
                col.RemoveDocument(docModelToRemove);

            }

            // inherited
            protected override DocumentController GetLayoutPrototype()
            {
                throw new NotImplementedException();
            }

            protected override DocumentController InstantiatePrototypeLayout()
            {
                throw new NotImplementedException();
            }

            public override FrameworkElement makeView(DocumentController docController,
                Context context, bool isInterfaceBuilderLayout = false) {
                return ApiDocumentModel.MakeView(docController, context);
            }

            /// <summary>
            /// Binds a textbox to a fieldModelController.
            /// </summary>
            private static void bindToTextBox(TextBox tb, FieldModelController field)
            {

                // bind URL
                TextFieldModelController textFieldModelController = field as TextFieldModelController;
                var sourceBinding = new Binding
                {
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
            private static void bindToCheckBox(CheckBox cb, FieldModelController field)
            {

                // bind URL
                NumberFieldModelController textFieldModelController = field as NumberFieldModelController;
                var sourceBinding = new Binding
                {
                    Source = textFieldModelController,
                    Path = new PropertyPath(nameof(textFieldModelController.Data)),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    Converter = new DoubleToBoolConverter()
                };
                cb.SetBinding(CheckBox.IsCheckedProperty, sourceBinding);
                textFieldModelController.Data = 1;
            }

            private static void makeBinding(ApiCreatorDisplay apiDisplay, DocumentController docController)
            {

                // set up text bindings
                bindToTextBox(apiDisplay.UrlTB, docController.GetField(BaseUrlKey));
                bindToTextBox(apiDisplay.AuthDisplay.UrlTB, docController.GetField(AuthBaseUrlKey));
                bindToTextBox(apiDisplay.AuthDisplay.KeyTB, docController.GetField(AuthKey));
                // bindToTextBox(apiDisplay.AuthDisplay.SecretTB, docController.Fields[AuthSecretKey));

                // bind drop down list
                NumberFieldModelController fmcontroller =
                    docController.GetField(HttpMethodKey) as NumberFieldModelController;
                var sourceBinding = new Binding
                {
                    Source = fmcontroller,
                    Path = new PropertyPath(nameof(fmcontroller.Data)),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                apiDisplay.RequestMethodCB.SetBinding(ComboBox.SelectedIndexProperty, sourceBinding);

            }

            public static void setResults(DocumentController docController, List<DocumentController> documents)
            {
                (docController.GetField(DocumentCollectionFieldModelController.CollectionKey) as
                    DocumentCollectionFieldModelController).SetDocuments(documents);
            }

            public static FrameworkElement MakeView(DocumentController docController,
                Context context, bool isInterfaceBuilderLayout = false) {

                ApiSourceDisplay sourceDisplay = new ApiSourceDisplay();
                ApiCreatorDisplay apiDisplay = new ApiCreatorDisplay(docController, sourceDisplay);
                makeBinding(apiDisplay, docController);

                // test bindings are working
                Debug.WriteLine((docController.GetDereferencedField(BaseUrlKey, context) as TextFieldModelController).Data);
                apiDisplay.UrlTB.Text = "https://itunes.apple.com/search";
                Debug.WriteLine((docController.GetDereferencedField(BaseUrlKey, context) as TextFieldModelController).Data);

                // generate collection view preview for results
                var resultView =
                    docController.GetDereferencedField(DocumentCollectionFieldModelController.CollectionKey, context) as
                        DocumentCollectionFieldModelController;

                // make collection view display framework element
                var data = resultView;
                var collectionFieldModelController = data.DereferenceToRoot<DocumentCollectionFieldModelController>(context);
                Debug.Assert(collectionFieldModelController != null);

                var collectionViewModel = new CollectionViewModel(collectionFieldModelController); 
                var collectionDisplay = new CollectionView(collectionViewModel);


                // this binding makes it s.t. either only the ApiSource or the ApiSourceCreator is visible at a single time
                // TODO: should clients be able to decide for themselves how this is displaying (separate superuser and regular user)
                // or should everyone just see the same view ?
                // bind URL
                var sourceBinding = new Binding
                {
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
                if (isInterfaceBuilderLayout)
                {
                    return new SelectableContainer(containerGrid, docController);
                }
                return containerGrid;
            }
        }

        /// <summary>
        /// Converts doubles to booleans and back. 0 = false, 1 = true (or any nonzero number). Used
        /// primarily to convert NumberFieldModels into boolean values.
        /// </summary>
        public class DoubleToBoolConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, string language)
            {
                return ((double)value != 0);
            }

            public object ConvertBack(object value, Type targetType, object parameter, string language)
            {
                if ((bool)value) return 1;
                return 0;
            }
        }


        /// <summary>
        /// Converts doubles to booleans and back. 0 = false, 1 = true (or any nonzero number). Used
        /// primarily to convert NumberFieldModels into boolean values.
        /// </summary>
        public class InverseVisibilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, string language)
            {
                if ((Windows.UI.Xaml.Visibility)value == Windows.UI.Xaml.Visibility.Collapsed)
                    return Windows.UI.Xaml.Visibility.Visible;
                else
                    return Windows.UI.Xaml.Visibility.Collapsed;
            }

            public object ConvertBack(object value, Type targetType, object parameter, string language)
            {
                if ((Windows.UI.Xaml.Visibility)value == Windows.UI.Xaml.Visibility.Collapsed)
                    return Windows.UI.Xaml.Visibility.Visible;
                else
                    return Windows.UI.Xaml.Visibility.Collapsed;
            }
        }
    }
}