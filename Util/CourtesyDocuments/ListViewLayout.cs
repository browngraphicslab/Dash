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
using Windows.UI.Xaml.Shapes;

namespace Dash
{
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
            throw new NotImplementedException("We don't have the dataDocument here and right now this is never called anyway");
        }

        public static FrameworkElement MakeView(DocumentController docController, Context context, DocumentController dataDocument, bool isInterfaceBuilderLayout = false)
        {

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // bind the grid height
            var heightController = GetHeightField(docController, context);
            BindHeight(grid, heightController);

            // bind the grid width
            var widthController = GetWidthField(docController, context);
            BindWidth(grid, widthController);
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
            //Grid.SetRow(listView, 0);
            if (isInterfaceBuilderLayout)
            {
                var icon = new TextBlock()
                {
                    Text = "🖹",
                    FontSize = 100,
                    Foreground = new SolidColorBrush(Colors.LightBlue),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                grid.Children.Insert(0, icon);
                return new SelectableContainer(grid, docController, dataDocument);
            }

            Ellipse dragEllipse = new Ellipse
            {
                Fill = new SolidColorBrush(Color.FromArgb(255, 53, 197, 151)),
                Width = 20,
                Height = 20, 
                Margin = new Thickness(5)
            };
            Grid.SetColumn(dragEllipse, 1);
            grid.Children.Add(dragEllipse);

            var referenceToText = new DocumentReferenceController(dataDocument.GetId(), DashConstants.KeyStore.DataKey);
            BindOperationInteractions(dragEllipse, referenceToText);                                        // TODO must test if this actually works I feel like it doesn't lol

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

}