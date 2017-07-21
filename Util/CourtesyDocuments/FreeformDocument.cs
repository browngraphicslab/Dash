using System;
using System.Collections.Generic;
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
            // bind the grid height
            var heightController = GetHeightField(docController, context);
            BindHeight(grid, heightController);

            // bind the grid width
            var widthController = GetWidthField(docController, context);
            BindWidth(grid, widthController);
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
                var icon = new TextBlock()
                {
                    Text = "⊡",
                    FontSize = 100,
                    Foreground = new SolidColorBrush(Colors.LightBlue),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                grid.Children.Insert(0, icon);
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

}