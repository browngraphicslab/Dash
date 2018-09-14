using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using Windows.UI.Xaml.Data;

namespace Dash
{
    public class GridViewLayout : CourtesyDocument
    {
        private static string PrototypeId = "C2EB5E08-1C04-44BF-970A-DB213949EE48";
        public static DocumentType DocumentType = new DocumentType("B7A022D4-B667-469C-B47E-3A84C0AA78A0", "Grid View Layout");
        public static KeyController GridViewKey = new KeyController("Grid View Key", "C4336303-8FD8-4ED6-8F03-540E95EE3CC8");
        public static double DefaultSpacing = 10;

        public GridViewLayout(IList<DocumentController> layoutDocuments, Point position = new Point(), Size size = new Size())
        {
            var fields = DefaultLayoutFields(position, size, new ListController<DocumentController>(layoutDocuments));
            fields.Add(GridViewKey, new NumberController(DefaultSpacing));
            SetupDocument(DocumentType, PrototypeId, "GridViewLayout Prototype Layout", fields);
        }

        /// <summary>
        /// Bind the spacing between items in gridview 
        /// </summary>
        private static void BindSpacing(GridView gridView, DocumentController docController, Context context)
        {
            var spacingController = docController.GetDereferencedField(GridViewKey, context) as NumberController;
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

        /// <summary>
        /// Converter that uses the spacing value to create an ItemContainerStyle that gridview can bind to 
        /// </summary>
        public class SpacingToItemContainerStyleConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, string language)
            {
                double spacing;

                if (!double.TryParse(value.ToString(), out spacing))       // if it's a number
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
            AddBinding(gridView, docController, GridViewKey, context, BindSpacing);
        }

        public static FrameworkElement MakeView(DocumentController docController, Context context)
        {

            var grid = new Grid();
            var gridView = new GridView
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            gridView.ItemContainerStyle = new Style { TargetType = typeof(GridViewItem) };
            SetupBindings(gridView, docController, context); 

            LayoutDocuments(docController, context, gridView);
            var c = new Context(context);
            docController.FieldModelUpdated += delegate (FieldControllerBase sender,
                FieldUpdatedEventArgs args, Context context1)
            {
                var dargs = (DocumentController.DocumentFieldUpdatedEventArgs) args;
                if (dargs.Reference.FieldKey.Equals(KeyStore.DataKey))
                {
                    LayoutDocuments((DocumentController)sender, c, gridView);
                }
            };
            grid.Children.Add(gridView);
            return grid;
        }

        private static void LayoutDocuments(DocumentController docController, Context context, GridView grid)
        {
            var layoutDocuments = GetLayoutDocumentCollection(docController, context).GetElements();
            ObservableCollection<FrameworkElement> itemsSource = new ObservableCollection<FrameworkElement>();
            double maxHeight = 0;
            foreach (var layoutDocument in layoutDocuments)
            {
                var layoutView = layoutDocument.MakeViewUI(context);
                layoutView.HorizontalAlignment = HorizontalAlignment.Left;
                layoutView.VerticalAlignment = VerticalAlignment.Top;
                itemsSource.Add(layoutView);
            }
            grid.ItemsSource = itemsSource;
        }

        private static ListController<DocumentController> GetLayoutDocumentCollection(DocumentController docController, Context context)
        {
            context = Context.SafeInitAndAddDocument(context, docController);
            return docController.GetField(KeyStore.DataKey)?
                .DereferenceToRoot<ListController<DocumentController>>(context);
        }
    }
}