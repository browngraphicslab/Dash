using System;
using System.Collections.Generic;
using DashShared;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
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
            RoundedRectangle,
            RoundedFrame,
            Pentagonal,
            Hexagonal,
            Octagonal,
            CustomPolygon,
            Clover,
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

        public static FrameworkElement MakeView(DocumentController docController, Context context)
        {
            var pathBox = new Viewbox {Stretch = Stretch.Fill};
            var corePath = new Path {Fill = new SolidColorBrush(Colors.Blue), StrokeThickness = 0};
            pathBox.Child = corePath;

            SetupBindings(pathBox, docController, context);
            BindPathShape(corePath, docController, context);
            BindPathFill(corePath, docController, context);

            return pathBox;
        }

        private static void BindPathShape(Path corePath, DocumentController docController, Context context)
        {
            var shapeBinding = new FieldBinding<TextController>()
            {
                Mode = BindingMode.OneWay,
                Document = docController,
                Key = KeyStore.DataKey,
                Converter = new ShapeNameToGeometryConverter(),
                Context = context,
                Tag = "BackgroundShape Shape"
            };
            corePath.AddFieldBinding(Path.DataProperty, shapeBinding);
        }

        private static void BindPathFill(Path corePath, DocumentController docController, Context context)
        {
            var fillBinding = new FieldBinding<TextController>()
            {
                Mode = BindingMode.TwoWay,
                Document = docController.GetDataDocument(),
                Key = KeyStore.BackgroundColorKey,
                Converter = new StringToBrushConverter(),
                Context = context,
                Tag = "BackgroundShape Fill"
            };
            corePath.AddFieldBinding(Shape.FillProperty, fillBinding);
        }
    }
}
