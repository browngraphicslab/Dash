using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Dash.Converters;
using Dash.Views.TemplateEditor;
using DashShared;
using Color = Windows.UI.Color;
using HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment;
using Point = Windows.Foundation.Point;
using Size = Windows.Foundation.Size;

namespace Dash
{
    public class TemplateBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("21D67C5E-9A2E-42C8-975A-AD60C728DDAE", "Template Box");
        private static readonly string PrototypeId = "159D2321-FBB4-4A2D-9902-9BDE105CABEF";
	    //public static Grid grid;

        public TemplateBox(double x = 0, double y = 0, double w = 300, double h = 400)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), new ListController<DocumentController>());
            SetupDocument(DocumentType, PrototypeId, "Template Prototype Layout", fields);
        }

	    public static SolidColorBrush GetSolidColorBrush(string hex)
	    {
		    if (hex == null) return new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
		    hex = hex.Replace("#", string.Empty);
		    byte a = (byte)(Convert.ToUInt32(hex.Substring(0, 2), 16));
		    byte r = (byte)(Convert.ToUInt32(hex.Substring(2, 2), 16));
		    byte g = (byte)(Convert.ToUInt32(hex.Substring(4, 2), 16));
		    byte b = (byte)(Convert.ToUInt32(hex.Substring(6, 2), 16));
		    SolidColorBrush myBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(a, r, g, b));
		    return myBrush;
	    }

        public static FrameworkElement MakeView(DocumentController docController, Context context)
        {
            // retrieve the color and opacity (if existent) from the layout document passed in
            var color = GetSolidColorBrush(docController.GetField<TextController>(KeyStore.BackgroundColorKey)?.Data);
            color.Opacity = (docController.GetField<NumberController>(KeyStore.OpacitySliderValueKey)?.Data / 255) ?? 1;
            if (docController.GetField(KeyStore.TemplateStyleKey) == null)
                docController.SetField<NumberController>(KeyStore.TemplateStyleKey,
                    new NumberController(TemplateConstants.FreeformView), true);

            //var templateStyle =
            //docController.GetField<NumberController>(KeyStore.TemplateStyleKey)?.Data ?? TemplateConstants.FreeformView;

            //Debug.WriteLine(templateStyle);

            // create a grid to use for the main panel of the view
            var parentGrid = new Grid();


            var grid = new Grid()
            {
                Background = color
            };

            var stack = new StackPanel()
            {
                Background = color,
                Visibility = Visibility.Collapsed
            };

            docController.AddFieldUpdatedListener(KeyStore.TemplateStyleKey, OnTemplateStyleUpdatedHandler);
            docController.GetDataDocument().FieldModelUpdated += TemplateBox_FieldModelUpdated;

            void TemplateBox_FieldModelUpdated(FieldControllerBase fieldControllerBase,
                FieldUpdatedEventArgs fieldUpdatedEventArgs, Context secondContext)
            {
                LayoutDocuments(docController, context, grid);
            }

            // determine if the document controller is set up to utilize a grid view
            if (docController.GetField<ListController<NumberController>>(KeyStore.RowInfoKey) != null)
            {
                // if so, use the list of number controllers to create rows with that value as the height
                docController.GetField<ListController<NumberController>>(KeyStore.RowInfoKey).Data.ForEach(i =>
                    grid.RowDefinitions.Add(new RowDefinition {Height = new GridLength((i as NumberController).Data)}));
            }

            // determine if the document controller is set up to utilize a grid view
            if (docController.GetField<ListController<NumberController>>(KeyStore.ColumnInfoKey) != null)
            {
                // if so, use the list of number controllers to create columns with that value as the width
                docController.GetField<ListController<NumberController>>(KeyStore.ColumnInfoKey).Data.ForEach(i =>
                    grid.ColumnDefinitions.Add(
                        new ColumnDefinition {Width = new GridLength((i as NumberController).Data)}));
            }


            LayoutDocuments(docController, context, grid);
            LayoutDocuments(docController, context, stack);

            // add a clip to the grid and add functionality to update the clip
            grid.Clip = new RectangleGeometry();
            grid.SizeChanged += delegate(object sender, SizeChangedEventArgs args)
            {
                grid.Clip.Rect = new Rect(0, 0, args.NewSize.Width, args.NewSize.Height);
            };


            // add a clip to the grid and add functionality to update the clip
            stack.Clip = new RectangleGeometry();
            stack.SizeChanged += delegate(object sender, SizeChangedEventArgs args)
            {
                stack.Clip.Rect = new Rect(0, 0, args.NewSize.Width, args.NewSize.Height);
            };


            var newCtxt = new Context(context);

            void OnDocumentFieldUpdatedHandler(DocumentController sender,
                DocumentController.DocumentFieldUpdatedEventArgs args, Context secondContext)
            {
                if (!(args.FieldArgs is ListController<DocumentController>.ListFieldUpdatedEventArgs cfargs))
                {
                    LayoutDocuments(sender, newCtxt, grid);
                    LayoutDocuments(sender, newCtxt, stack);
                    return;
                }

                if (cfargs.ListAction ==
                    ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Add)
                {
                    AddDocuments(cfargs.NewItems, newCtxt, grid);
                    AddDocuments(cfargs.NewItems, newCtxt, stack);
                }
                else if (cfargs.ListAction != ListController<DocumentController>.ListFieldUpdatedEventArgs
                             .ListChangedAction.Content)
                {
                    LayoutDocuments(sender, newCtxt, grid);
                    LayoutDocuments(sender, newCtxt, stack);
                }

            }

            grid.Loaded += delegate
            {
                docController.AddFieldUpdatedListener(KeyStore.DataKey, OnDocumentFieldUpdatedHandler);
            };

            grid.Unloaded += delegate
            {
                docController.RemoveFieldUpdatedListener(KeyStore.DataKey, OnDocumentFieldUpdatedHandler);
            };

            void OnTemplateStyleUpdatedHandler(DocumentController sender,
                DocumentController.DocumentFieldUpdatedEventArgs args, Context secondContext)
            {
                //if (args.OldValue == args.NewValue) return;

                if (docController.GetField<NumberController>(KeyStore.TemplateStyleKey)?.Data ==
                    TemplateConstants.ListView)
                {
                    stack.Visibility = Visibility.Visible;
                    grid.Visibility = Visibility.Collapsed;
                }
                else if (docController.GetField<NumberController>(KeyStore.TemplateStyleKey)?.Data ==
                         TemplateConstants.FreeformView)
                {
                    stack.Visibility = Visibility.Collapsed;
                    grid.Visibility = Visibility.Visible;
                }



            }


            CourtesyDocument.SetupBindings(parentGrid, docController, context);

            parentGrid.Children.Add(stack);
            parentGrid.Children.Add(grid);

            return parentGrid;
        }

        private static void LayoutDocuments(DocumentController docController, Context context, Panel grid)
        {
            // get the list of layout documents and layout each one on the grid
            var layoutDocuments =
                docController.GetField<ListController<DocumentController>>(KeyStore.DataKey).TypedData;
            grid.Children.Clear();
            AddDocuments(layoutDocuments, context, grid);
        }

        private static ListController<DocumentController> GetLayoutDocumentCollection(DocumentController docController, Context context)
        {
            // returns the list of layout documents stored in the doc controller's data key
            context = Context.SafeInitAndAddDocument(context, docController);
            return docController.GetField(KeyStore.DataKey)?
                .DereferenceToRoot<ListController<DocumentController>>(context);
        }

        private static void AddDocuments(List<DocumentController> docs, Context context, Panel grid)
        {
            foreach (var layoutDoc in docs)
            {
                // create the view for the document controller
                var layoutView = layoutDoc.MakeViewUI(context);

                // creates a multibinding to figure out to figure out if we should use vertical alignment
                var vertBinding = new FieldMultiBinding<VerticalAlignment>(
                    new DocumentFieldReference(layoutDoc, KeyStore.UseVerticalAlignmentKey),
                    new DocumentFieldReference(layoutDoc, KeyStore.VerticalAlignmentKey))
                {
                    Mode = BindingMode.OneWay,
                    Converter = new VerticalAlignmentMultiBinding(),
                    Context = null, //TODO
                    CanBeNull = true
                };
                layoutView.AddFieldBinding(FrameworkElement.VerticalAlignmentProperty, vertBinding);

                // creates a multibinding to figure out to figure out if we should use horizontal alignment
                var horizBinding =
                    new FieldMultiBinding<HorizontalAlignment>(
                        new DocumentFieldReference(layoutDoc, KeyStore.UseHorizontalAlignmentKey),
                        new DocumentFieldReference(layoutDoc, KeyStore.HorizontalAlignmentKey))
                    {
                        Mode = BindingMode.OneWay,
                        Converter = new HorizontalAlignmentMultiBinding(),
                        Context = null, //TODO
                        CanBeNull = true
                    };
                layoutView.AddFieldBinding(FrameworkElement.HorizontalAlignmentProperty, horizBinding);

	            if (grid is StackPanel)
	            {
		            grid.Children.Add(layoutView);
	            }
	            else
	            {
		            // creates a multibinding to figure out to figure out if we should use both the x or the y positions
		            var renderBinding = new FieldMultiBinding<TranslateTransform>(
			            new DocumentFieldReference(layoutDoc, KeyStore.UseHorizontalAlignmentKey),
			            new DocumentFieldReference(layoutDoc, KeyStore.UseVerticalAlignmentKey),
			            new DocumentFieldReference(layoutDoc, KeyStore.PositionFieldKey))
		            {
			            Mode = BindingMode.OneWay,
			            Converter = new PositionWithAlignmentMultiBinding(),
			            Context = null,
			            CanBeNull = true
		            };
		            layoutView.AddFieldBinding(UIElement.RenderTransformProperty, renderBinding);

		            grid.Children.Add(layoutView);
		            if (layoutDoc.GetField<NumberController>(KeyStore.RowKey) != null)
		            {
			            Grid.SetRow(layoutView, (int)layoutDoc.GetField<NumberController>(KeyStore.RowKey).Data);
		            }
		            if (layoutDoc.GetField<NumberController>(KeyStore.ColumnKey) != null)
		            {
			            Grid.SetColumn(layoutView, (int)layoutDoc.GetField<NumberController>(KeyStore.ColumnKey).Data);
		            }
				}
				
            }
        }

        private class VerticalAlignmentMultiBinding : SafeDataToXamlConverter<List<object>, VerticalAlignment>
        {
            public override VerticalAlignment ConvertDataToXaml(List<object> data, object parameter = null)
            {
                // if we are told to use the vertical alignment, return it
                if (data[0] is bool useVert && useVert)
                {
                    if (Enum.TryParse<VerticalAlignment>((string) data[1], out var alignment))
                    {
                        return alignment;
                    }
                }

                // otherwise, default to top so the render transform applies properly
                return VerticalAlignment.Top;
            }

            public override List<object> ConvertXamlToData(VerticalAlignment xaml, object parameter = null)
            {
                throw new NotImplementedException();
            }
        }

        private class HorizontalAlignmentMultiBinding : SafeDataToXamlConverter<List<object>, HorizontalAlignment>
        {
            public override HorizontalAlignment ConvertDataToXaml(List<object> data, object parameter = null)
            {
                // if we are told to use the horizontal alignment, return it
                if (data[0] is bool useHoriz && useHoriz)
                {
                    if (Enum.TryParse<HorizontalAlignment>((string)data[1], out var alignment))
                    {
                        return alignment;
                    }
                }

                // otherwise default to left so the render transform applies properly
                return HorizontalAlignment.Left;
            }

            public override List<object> ConvertXamlToData(HorizontalAlignment xaml, object parameter = null)
            {
                throw new NotImplementedException();
            }
        }

        private class PositionWithAlignmentMultiBinding : SafeDataToXamlConverter<List<object>, TranslateTransform>
        {
            public override TranslateTransform ConvertDataToXaml(List<object> data, object parameter = null)
            {
                var transformations = new TranslateTransform();
                if (data[2] is Point pt)
                {
                    // if we aren't told to use the horizontal alignment or if there isn't a horizontal alignment key
                    if ((data[0] is bool useHoriz && !(useHoriz)) || data[0] == null)
                    {
                        // transform to the point's x value
                        transformations.X = pt.X;
                    }
                    else
                    {
                        // otherwise don't transform it at all
                        transformations.X = 0;
                    }

                    if ((data[1] is bool useVert && !(useVert)) || data[1] == null)
                    {
                        transformations.Y = pt.Y;
                    }
                    else
                    {
                        transformations.Y = 0;
                    }
                }

                return transformations;
            }

            public override List<object> ConvertXamlToData(TranslateTransform xaml, object parameter = null)
            {
                throw new NotImplementedException();
            }
        }
    }
}