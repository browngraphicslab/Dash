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
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Controls.Primitives;
using System.Diagnostics;

namespace Dash
{
    public class ListViewLayout : CourtesyDocument
    {
        private static string PrototypeId = "C512FC2E-CDD1-4E94-A98F-35A65E821C08";
        public static DocumentType DocumentType = new DocumentType("3E5C2739-A511-40FF-9B2E-A875901B296D", "ListView Layout");
        public static KeyControllerBase SpacingKey = new KeyControllerBase("E89037A5-B7CC-4DD7-A89B-E15EDC69AF7C", "Spacing Key");
        public static double DefaultSpacing = 30;

        public ListViewLayout(IList<DocumentController> layoutDocuments, Point position = new Point(), Size size = new Size())
        {
            Document = GetLayoutPrototype().MakeDelegate();
            var layoutDocumentCollection = new DocumentCollectionFieldModelController(layoutDocuments);
            var fields = DefaultLayoutFields(position, size, layoutDocumentCollection);
            Document.SetFields(fields, true); //TODO add fields to constructor parameters   

            SetSpacingField(Document, DefaultSpacing, true);
        }

        public ListViewLayout() : this(new List<DocumentController>()) { }

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
            throw new NotImplementedException("We don't have the dataDocument here and right now this is never called anyway");
        }

        private static void BindSpacing(ListView listView, DocumentController docController, Context context)
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
            listView.SetBinding(ListView.ItemContainerStyleProperty, spacingBinding); 
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
                var itemContainerStyle = new Style { TargetType = typeof(ListViewItem) };
                itemContainerStyle.Setters.Add(new Setter(ListView.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
                itemContainerStyle.Setters.Add(new Setter(ListView.MinHeightProperty, spacing));

                return itemContainerStyle; 
            }

            public object ConvertBack(object value, Type targetType, object parameter, string language)
            {
                throw new NotImplementedException();
            }
        }

        protected new static void SetupBindings(ListView listview, DocumentController docController, Context context)
        {
            CourtesyDocument.SetupBindings(listview, docController, context);

            AddBinding(listview, docController, SpacingKey, context, BindSpacing);
        }

        public static FrameworkElement MakeView(DocumentController docController, Context context, DocumentController dataDocument, bool isInterfaceBuilderLayout = false)
        {

            var grid = new Grid();

            //SetupBindings(grid, docController, context);
            var listView = new ListView
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            listView.ItemContainerStyle = new Style { TargetType = typeof(ListViewItem) };

            listView.HorizontalContentAlignment = HorizontalAlignment.Center; 
            SetupBindings(listView, docController, context); 

            LayoutDocuments(docController, context, listView, isInterfaceBuilderLayout);

            var c = new Context(context);
            docController.DocumentFieldUpdated += delegate (DocumentController sender,
                DocumentController.DocumentFieldUpdatedEventArgs args)
            {
                if (args.Reference.FieldKey.Equals(KeyStore.DataKey))
                {
                    LayoutDocuments(sender, c, listView, isInterfaceBuilderLayout);
                }
            };
            grid.Children.Add(listView);
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
                var container = new SelectableContainer(grid, docController, dataDocument);
                SetupBindings(container, docController, context);
                return container;
            }

            /*          // commented this out for now 
            Ellipse dragEllipse = new Ellipse
            {
                Fill = new SolidColorBrush(Color.FromArgb(255, 53, 197, 151)),
                Width = 20,
                Height = 20, 
                Margin = new Thickness(5)
            };
            Grid.SetColumn(dragEllipse, 1);
            grid.Children.Add(dragEllipse);

            var referenceToText = new ReferenceFieldModelController(dataDocument.GetId(), KeyStore.DataKey);
            BindOperationInteractions(dragEllipse, referenceToText.FieldReference.Resolve(context));            // TODO must test if this actually works I feel like it doesn't lol
            */ 
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
                if (isInterfaceBuilder) SetupBindings(layoutView, layoutDocument, context);
                itemsSource.Add(layoutView);
                
            }
            list.ItemsSource = itemsSource;
            list.SelectionMode = ListViewSelectionMode.None;
            foreach (var item in list.Items)
            {
                var elem = (item as UIElement);
                if (elem != null) elem.IsHitTestVisible = true;
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