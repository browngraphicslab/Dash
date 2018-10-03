using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace Dash
{
    public class ListViewLayout : CourtesyDocument
    {
        private static string PrototypeId = "C512FC2E-CDD1-4E94-A98F-35A65E821C08";
        public static DocumentType DocumentType = new DocumentType("3E5C2739-A511-40FF-9B2E-A875901B296D", "ListView Layout");
        public static KeyController SpacingKey = new KeyController("Spacing Key", "E89037A5-B7CC-4DD7-A89B-E15EDC69AF7C");
        public static double DefaultSpacing = 30;

        public ListViewLayout(IList<DocumentController> layoutDocuments, Point position = new Point(), Size size = new Size())
        {
            var fields = DefaultLayoutFields(position, size, new ListController<DocumentController>(layoutDocuments));
            fields.Add(SpacingKey, new NumberController(DefaultSpacing));
            SetupDocument(DocumentType, PrototypeId, "ListViewLayout Prototype Layout", fields);
        }

        public ListViewLayout() : this(new List<DocumentController>()) { }

        private static void BindSpacing(ListView listView, DocumentController docController,  Context context)
        {
            var spacingController = docController.GetDereferencedField(SpacingKey, context) as NumberController;
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

        public static FrameworkElement MakeView(DocumentController docController, Context context)
        {

            var grid = new Grid();
	        grid.Background = new SolidColorBrush(Colors.Blue);
            
            var listView = new ListView
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            listView.Loaded += (s, e) =>
            {
                Util.FixListViewBaseManipulationDeltaPropagation(listView);
            };
			
			listView.Background = new SolidColorBrush(Colors.Red);
            listView.ItemContainerStyle = new Style { TargetType = typeof(ListViewItem) };

            listView.HorizontalContentAlignment = HorizontalAlignment.Center; 
            SetupBindings(listView, docController, context); 

            var itemsSource = LayoutDocuments(docController, context, listView);
	        grid.Drop += (s, e) =>
	        {
		        Debug.WriteLine("SOMETHING WAS DROPPED ON LIST VIEW: " + s + e);
	        };


            var c = new Context(context);
            docController.FieldModelUpdated += delegate (FieldControllerBase sender,
                FieldUpdatedEventArgs args, Context context1)
            {
                var dargs = (DocumentController.DocumentFieldUpdatedEventArgs) args;
                if (dargs.Reference.FieldKey.Equals(KeyStore.DataKey))
                {
                    LayoutDocuments((DocumentController)sender, c, listView);
                }
            };
            grid.Children.Add(listView);

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

        private static ObservableCollection<FrameworkElement> LayoutDocuments(DocumentController docController, Context context, ListView list)
        {
            var layoutDocuments = GetLayoutDocumentCollection(docController, context).GetElements();
            ObservableCollection<FrameworkElement> itemsSource = new ObservableCollection<FrameworkElement>();
            foreach (var layoutDocument in layoutDocuments)
            {
                var layoutView = layoutDocument.MakeViewUI(context);
                layoutView.HorizontalAlignment = HorizontalAlignment.Left;
                layoutView.VerticalAlignment = VerticalAlignment.Top;
                itemsSource.Add(layoutView);
                
            }
            list.ItemsSource = itemsSource;
            list.SelectionMode = ListViewSelectionMode.None;
            foreach (var item in list.Items)
            {
                var elem = (item as UIElement);
                if (elem != null) elem.IsHitTestVisible = true;
            }

	        return itemsSource;
        }

	    private static void AddDocument(DocumentController doc, ListView list)
	    {
		   /* var layoutView = doc.MakeViewUI(new Context());
			//// layoutView.HorizontalAlignment = HorizontalAlignment.Left;
			// layoutView.VerticalAlignment = VerticalAlignment.Top;
			// list.ItemsSource = 

		    
		    list.ItemsSource = itemsSource;
		    list.SelectionMode = ListViewSelectionMode.None;
		    foreach (var item in list.Items)
		    {
			    var elem = (item as UIElement);
			    if (elem != null) elem.IsHitTestVisible = true;
		    }
			*/
		}

        private static ListController<DocumentController> GetLayoutDocumentCollection(DocumentController docController, Context context)
        {
            context = Context.SafeInitAndAddDocument(context, docController);
            return docController.GetField(KeyStore.DataKey)?
                .DereferenceToRoot<ListController<DocumentController>>(context);
        }
    }

}
