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
using Windows.UI.Xaml.Data;

namespace Dash
{
    public class GridViewLayout : CourtesyDocument
    {
        private static string PrototypeId = "C2EB5E08-1C04-44BF-970A-DB213949EE48";
        public static DocumentType DocumentType = new DocumentType("B7A022D4-B667-469C-B47E-3A84C0AA78A0", "Spacing Layout");
        public static Key SpacingKey = new Key("C4336303-8FD8-4ED6-8F03-540E95EE3CC8", "Spacing Key");
        public static double DefaultSpacing = 10;

        public GridViewLayout(IList<DocumentController> layoutDocuments, Point position = new Point(), Size size = new Size())
        {
            Document = GetLayoutPrototype().MakeDelegate();
            var layoutDocumentCollection = new DocumentCollectionFieldModelController(layoutDocuments);
            var fields = DefaultLayoutFields(position, size, layoutDocumentCollection);
            Document.SetFields(fields, true); //TODO add fields to constructor parameters   

            SetSpacingField(Document, DefaultSpacing, true);
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

            SetSpacingField(prototypeDocument, DefaultSpacing, true);
            return prototypeDocument;
        }

        private static void SetSpacingField(DocumentController docController, double spacing, bool forceMask)
        {
            var currentSpacingField = new NumberFieldModelController(spacing);
            docController.SetField(SpacingKey, currentSpacingField, forceMask);
        }

        public override FrameworkElement makeView(DocumentController docController, Context context, bool isInterfaceBuilderLayout = false)
        {
            throw new NotImplementedException("We don't have access to the data document here");
        }

        private static void BindSpacing(GridView gridView, DocumentController docController, Context context)
        {
            var spacingController = docController.GetDereferencedField(SpacingKey, context) as NumberFieldModelController;
            if (spacingController == null)
                return;

            var spacingBinding = new Binding
            {
                Source = spacingController,
                Path = new PropertyPath(nameof(spacingController.Data)),
                Mode = BindingMode.TwoWay,
                Converter = new SpacingToItemContainerStyleConverter()
            };
            gridView.SetBinding(ListView.ItemContainerStyleProperty, spacingBinding);
        }

        public class SpacingToItemContainerStyleConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, string language)
            {
                double spacing;

                if (!double.TryParse(value.ToString(), out spacing))                             // if it's a number
                {
                    spacing = 0;
                }
                var itemContainerStyle = new Style { TargetType = typeof(GridViewItem) };
                itemContainerStyle.Setters.Add(new Setter(GridView.MarginProperty, new Thickness(0, 0, spacing, spacing))); 

                return itemContainerStyle;
            }

            public object ConvertBack(object value, Type targetType, object parameter, string language)
            {
                throw new NotImplementedException();
            }
        }

        protected new static void SetupBindings(GridView gridView, DocumentController docController, Context context)
        {
            CourtesyDocument.SetupBindings(gridView, docController, context);

            AddBinding(gridView, docController, SpacingKey, context, BindSpacing);
        }

        public static FrameworkElement MakeView(DocumentController docController, Context context, DocumentController dataDocument, bool isInterfaceBuilderLayout = false)
        {

            var grid = new Grid();
            //SetupBindings(grid, docController, context);
            var gridView = new GridView
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            gridView.ItemContainerStyle = new Style { TargetType = typeof(GridViewItem) };
            SetupBindings(gridView, docController, context); 

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
                var container = new SelectableContainer(grid, docController, dataDocument);
                //SetupBindings(container, docController, context);
                return container;
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
                if(isInterfaceBuilder) SetupBindings(layoutView, layoutDocument, context);
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