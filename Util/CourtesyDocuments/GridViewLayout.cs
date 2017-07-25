using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Dash;
using DashShared;

namespace Dash
{
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
            // bind the grid height
            var heightController = GetHeightField(docController, context);
            BindHeight(grid, heightController);

            // bind the grid width
            var widthController = GetWidthField(docController, context);
            BindWidth(grid, widthController);
            var gridView = new GridView
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            LayoutDocuments(docController, context, gridView, isInterfaceBuilderLayout);

            var c = new Context(context);
            docController.DocumentFieldUpdated += delegate (DocumentController sender,
                DocumentController.DocumentFieldUpdatedEventArgs args)
            {
                if (args.Reference.FieldKey.Equals(DashConstants.KeyStore.DataKey))
                {
                    LayoutDocuments(sender, c, gridView, isInterfaceBuilderLayout);
                }
            };
            grid.Children.Add(gridView);
            if (isInterfaceBuilderLayout)
            {
                var icon = new TextBlock()
                {
                    Text = "▦",
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

        private static void LayoutDocuments(DocumentController docController, Context context, GridView grid, bool isInterfaceBuilder)
        {
            var layoutDocuments = GetLayoutDocumentCollection(docController, context).GetDocuments();
            ObservableCollection<FrameworkElement> itemsSource = new ObservableCollection<FrameworkElement>();
            double maxHeight = 0;
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
}