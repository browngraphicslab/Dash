using System;
using System.Collections.Generic;
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

        public TemplateBox(double x = 0, double y = 0, double w = 200, double h = 200)
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
		    var color = GetSolidColorBrush(docController.GetField<TextController>(KeyStore.BackgroundColorKey)?.Data);
		    color.Opacity = (docController.GetField<NumberController>(KeyStore.OpacitySliderValueKey)?.Data / 255) ?? 1;
            var grid = new Grid()
	        {
		        // default size of the template editor box's workspace
				Background = color
			};
            
            LayoutDocuments(docController, context, grid);

            grid.Clip = new RectangleGeometry();
            grid.SizeChanged += delegate(object sender, SizeChangedEventArgs args)
            {
                grid.Clip.Rect = new Rect(0, 0, args.NewSize.Width, args.NewSize.Height);
            };

            var newCtxt = new Context(context);

            void OnDocumentFieldUpdatedHandler(DocumentController sender,
                DocumentController.DocumentFieldUpdatedEventArgs args, Context secondContext)
            {
                var cfargs = args.FieldArgs as ListController<DocumentController>.ListFieldUpdatedEventArgs;
                if (cfargs.ListAction ==
                    ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Add)
                {
                    AddDocuments(cfargs.ChangedDocuments, newCtxt, grid);
                }
                else if (cfargs.ListAction != ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Content)
                {
                    LayoutDocuments(sender, newCtxt, grid);
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

            CourtesyDocument.SetupBindings(grid, docController, context);

            return grid;
        }

	    private static void LayoutDocuments(DocumentController docController, Context context, Grid grid)
        {
            // get the list of layout documents and layout each one on the grid
            var layoutDocuments = GetLayoutDocumentCollection(docController, context).GetElements();
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

        private static void AddDocuments(List<DocumentController> docs, Context context, Grid grid)
        {
            foreach (var layoutDoc in docs)
            {
                // create the view for the document controller
                var layoutView = layoutDoc.MakeViewUI(context);
                // set width and render transform appropriately
                layoutDoc.SetField(KeyStore.WidthFieldKey,
                    new NumberController(layoutDoc.GetField<PointController>(KeyStore.ActualSizeKey).Data.X), true);
                var transformations = new TranslateTransform();
                if (layoutDoc.GetDataDocument().GetField<TextController>(KeyStore.HorizontalAlignmentKey) != null)
                {
                    layoutDoc.SetHorizontalAlignment(layoutDoc.GetDataDocument().GetHorizontalAlignment());
                }
                else
                {
                    layoutDoc.SetHorizontalAlignment(HorizontalAlignment.Left);
                    transformations.X = layoutDoc.GetField<PointController>(KeyStore.PositionFieldKey).Data.X;
                }

                if (layoutDoc.GetDataDocument().GetField<TextController>(KeyStore.VerticalAlignmentKey) != null)
                {
                    layoutDoc.SetVerticalAlignment(layoutDoc.GetDataDocument().GetVerticalAlignment());
                }
                else
                {
                    layoutDoc.SetVerticalAlignment(VerticalAlignment.Top);
                    transformations.Y = layoutDoc.GetField<PointController>(KeyStore.PositionFieldKey).Data.Y;
                }

                layoutView.RenderTransform = transformations;
                grid.Children.Add(layoutView);
            }
        }
    }

}