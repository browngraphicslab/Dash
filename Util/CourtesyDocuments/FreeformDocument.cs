using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Dash;
using DashShared;

namespace Dash
{
    public class FreeFormDocument : CourtesyDocument
    {
        private static string PrototypeId = "A5614540-0A50-40F3-9D89-965B8948F2A2";

        public FreeFormDocument(IList<DocumentController> layoutDocuments, Point position = new Point(), Size size = new Size())
        {
            Document = GetLayoutPrototype().MakeDelegate();
            var layoutDocumentCollection = new DocumentCollectionFieldModelController(layoutDocuments);
            var fields = DefaultLayoutFields(position, size, layoutDocumentCollection);
            Document.SetFields(fields, true); //TODO add fields to constructor parameters     

            //Document.SetField(KeyStore.IconTypeFieldKey, new NumberFieldModelController((double)IconTypeEnum.Api), true);
        }

        public FreeFormDocument() : this(new List<DocumentController>()) { }

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
            var layoutDocCollection = new DocumentCollectionFieldModelController(new List<DocumentController>());
            var fields = DefaultLayoutFields(new Point(), new Size(double.NaN, double.NaN), layoutDocCollection);
            var prototypeDocument = new DocumentController(fields, DashConstants.TypeStore.FreeFormDocumentLayout, PrototypeId);
            return prototypeDocument;
        }

        public override FrameworkElement makeView(DocumentController docController, Context context, bool isInterfaceBuilderLayout = false)
        {
            throw new NotImplementedException("We don't have the dataDocument here and right now this is never called anyway");
        }

        public static FrameworkElement MakeView(DocumentController docController, Context context, DocumentController dataDocument, bool isInterfaceBuilderLayout = false)
        {

            var grid = new Grid();
            SetupBindings(grid, docController, context);
            LayoutDocuments(docController, context, grid, isInterfaceBuilderLayout);

            grid.Clip = new RectangleGeometry();
            grid.SizeChanged += delegate (object sender, SizeChangedEventArgs args)
            {
                grid.Clip.Rect = new Rect(0, 0, args.NewSize.Width, args.NewSize.Height);
            };

            var c = new Context(context);
            DocumentController.OnDocumentFieldUpdatedHandler onDocumentFieldUpdatedHandler = delegate (DocumentController sender,
                DocumentController.DocumentFieldUpdatedEventArgs args)
            {
                var collFieldArgs =
                    args.FieldArgs as DocumentCollectionFieldModelController.CollectionFieldUpdatedEventArgs;
                if (collFieldArgs.CollectionAction == DocumentCollectionFieldModelController
                        .CollectionFieldUpdatedEventArgs.CollectionChangedAction.Add)
                {
                    AddDocuments(collFieldArgs.ChangedDocuments, c, grid, isInterfaceBuilderLayout);
                }
                else
                {
                    LayoutDocuments(sender, c, grid, isInterfaceBuilderLayout);
                }
            };
            grid.Loaded += delegate
            {
                Debug.WriteLine($"Add freeform listener {++i}");
                docController.AddFieldUpdatedListener(KeyStore.DataKey, onDocumentFieldUpdatedHandler);
            };
            grid.Unloaded += delegate
            {
                Debug.WriteLine($"Remove freeform listener {--i}");
                docController.RemoveFieldUpdatedListener(KeyStore.DataKey, onDocumentFieldUpdatedHandler);
            };
            if (isInterfaceBuilderLayout)
            {
                var icon = new TextBlock()
                {
                    Text = "⊡",
                    FontSize = 100,
                    Foreground = new SolidColorBrush(Colors.LightBlue),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                grid.Children.Insert(0, icon);
                var container = new SelectableContainer(grid, docController, dataDocument);
                SetupBindings(container, docController, context);
                return container;
            }
            return grid;
        }

        private static void LayoutDocuments(DocumentController docController, Context context, Grid grid, bool isInterfaceBuilder)
        {
            var layoutDocuments = GetLayoutDocumentCollection(docController, context).GetDocuments();
            grid.Children.Clear();
            if (isInterfaceBuilder)
            {
                var icon = new TextBlock()
                {
                    Text = "⊡",
                    FontSize = 100,
                    Foreground = new SolidColorBrush(Colors.LightBlue),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                grid.Children.Add(icon);
            }
            AddDocuments(layoutDocuments, context, grid, isInterfaceBuilder);
        }

        private static int i = 0;
        private static void AddDocuments(List<DocumentController> docs, Context context, Grid grid, bool isInterfaceBuilder)
        {
            foreach (var layoutDocument in docs)
            {
                var layoutView = layoutDocument.MakeViewUI(context, isInterfaceBuilder);
                layoutView.HorizontalAlignment = HorizontalAlignment.Left;
                layoutView.VerticalAlignment = VerticalAlignment.Top;

                var positionField = layoutDocument.GetPositionField(context);
                BindTranslation(layoutView, positionField);

                if (isInterfaceBuilder) SetupBindings(layoutView, layoutDocument, context);

                grid.Children.Add(layoutView);
            }
        }

        private static DocumentCollectionFieldModelController GetLayoutDocumentCollection(DocumentController docController, Context context)
        {
            context = Context.SafeInitAndAddDocument(context, docController);
            return docController.GetField(KeyStore.DataKey)?
                .DereferenceToRoot<DocumentCollectionFieldModelController>(context);
        }
    }

}