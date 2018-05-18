using System;
using DashShared;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI;
using Windows.UI.Xaml.Shapes;
using Dash.Converters;
using Microsoft.Toolkit.Uwp.UI.Extensions;

namespace Dash
{ 
    public class BackgroundShape : CourtesyDocument
    {
        /// <summary>
     /// The document type which is uniquely associated with pdf boxes
     /// </summary>
        public static DocumentType DocumentType = new DocumentType("B15BB50C-0C84-46F9-BFD7-D25BAF0E80A5", "Background Shape");

        public enum AdornmentShape {
            Elliptical,
            Rectangular,
            Rounded
        }

       
        /// <summary>
        /// The prototype id is used to make sure that only one prototype is every created
        /// </summary>
        private static string PrototypeId = "88A3B7F5-7828-4251-ACFC-E56428316203";

        public BackgroundShape(FieldControllerBase refToBackground, double x = 0, double y = 0, double w = 200, double h = 200)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToBackground);
            fields.Add(KeyStore.AdornmentKey, new TextController("true"));
            SetupDocument(DocumentType, PrototypeId, "Background Box Prototype Layout", fields);
        }

        protected static void BindShape(ContentPresenter Outelement, DocumentController docController,
            Context context)
        {
            var backgroundBinding = new FieldBinding<TextController>()
            {
                Mode = BindingMode.TwoWay,
                Document = docController.GetDataDocument(),
                Key = KeyStore.BackgroundColorKey,
                Converter = new StringToBrushConverter(),
                Context = context
            };
            (Outelement.Content as Shape).AddFieldBinding(Shape.FillProperty, backgroundBinding);
            (Outelement.Content as Shape).Fill = new Windows.UI.Xaml.Media.SolidColorBrush(Colors.Red);

            var binding = new FieldBinding<TextController>()
            {
                Mode = BindingMode.TwoWay,
                Document = docController,
                Key = KeyStore.DataKey,
                Context = context,
                Converter = new ShapeNameToShapeConverter(),
                ConverterParameter = backgroundBinding
            };
            
            Outelement.AddFieldBinding(ContentPresenter.ContentProperty, binding);
        }

        public static FrameworkElement MakeView(DocumentController docController, Context context)
        {
            // create the  view
            ContentPresenter shape = new ContentPresenter();
            AdornmentShape ashape = AdornmentShape.Rectangular;
            Enum.TryParse<AdornmentShape>(docController.GetDereferencedField<TextController>(KeyStore.AdornmentShapeKey, context)?.Data ?? AdornmentShape.Rounded.ToString(), out ashape);
            switch (ashape) {
                case AdornmentShape.Elliptical:
                    shape.Content = new Ellipse();
                    break;
                case AdornmentShape.Rectangular:
                    shape.Content = new Rectangle();
                    break;
                case AdornmentShape.Rounded:
                    Shape innerRectangle = new Rectangle();
                    (innerRectangle as Rectangle).RadiusX = (innerRectangle as Rectangle).RadiusY = 40;
                    shape.Content = innerRectangle;
                    break;
            }

            SetupBindings(shape, docController, context);

            BindShape(shape, docController, context);

            return shape;
        }
    }
}
