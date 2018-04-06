using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using DashShared.Models;
using System.Globalization;
using Windows.UI;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Dash.Converters;
using Microsoft.Toolkit.Uwp.UI.Extensions;

namespace Dash
{ 
    public class BackgroundBox : CourtesyDocument
    {
        /// <summary>
     /// The document type which is uniquely associated with pdf boxes
     /// </summary>
        public static DocumentType DocumentType = new DocumentType("B15BB50C-0C84-46F9-BFD7-D25BAF0E80A5", "Background Box");

        public enum AdornmentShape {
            Elliptical,
            Rectangular,
            Rounded
        }

       
        /// <summary>
        /// The prototype id is used to make sure that only one prototype is every created
        /// </summary>
        private static string PrototypeId = "88A3B7F5-7828-4251-ACFC-E56428316203";

        public BackgroundBox(AdornmentShape shape, double x = 0, double y = 0, double w = 200, double h = 200)
        {
            // set fields based on the parameters
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), null);
            
            // get a delegate of the prototype layout (which already has fields set on it)
            Document = GetLayoutPrototype().MakeDelegate();
            var r = new Random();
            var hexColor = Color.FromArgb(0x33, (byte)r.Next(255), (byte)r.Next(255), (byte)r.Next(255)).ToString();
            Document.SetField(KeyStore.BackgroundColorKey, new TextController(hexColor), true);
            Document.SetField(KeyStore.AdornmentShapeKey, new TextController(shape.ToString()), true);

            // replace any of the default fields on the prototype delegate with the new fields
            Document.SetFields(fields, true);
        }



        protected static void BindBackgroundColor(ContentPresenter Outelement, DocumentController docController,
            Context context)
        {
            

            //Shape element = (Shape)Outelement.Content;
            //element.AddFieldBinding(Shape.FillProperty, binding);
        }

        protected static void BindShape(ContentPresenter Outelement, DocumentController docController,
            Context context)
        {
            var backgroundBinding = new FieldBinding<TextController>()
            {
                Mode = BindingMode.TwoWay,
                Document = docController,
                Key = KeyStore.BackgroundColorKey,
                Converter = new StringToBrushConverter(),
                Context = context
            };

            var binding = new FieldBinding<TextController>()
            {
                Mode = BindingMode.TwoWay,
                Document = docController,
                Key = KeyStore.AdornmentShapeKey,
                Context = context,
                Converter = new ShapeNameToShapeConverter(),
                ConverterParameter = backgroundBinding
            };
            
            Outelement.AddFieldBinding(ContentPresenter.ContentProperty, binding);
        }

        /// <summary>
        /// Returns the prototype layout if it exists, otherwise creates a new prototype layout
        /// </summary>
        /// <returns></returns>
        protected override DocumentController GetLayoutPrototype()
        {
            var prototype = ContentController<FieldModel>.GetController<DocumentController>(PrototypeId) ??
                            InstantiatePrototypeLayout();
            return prototype;
        }
        /// <summary>
        /// Creates a new prototype layout
        /// </summary>
        /// <returns></returns>
        protected override DocumentController InstantiatePrototypeLayout()
        {
            var fields = DefaultLayoutFields(new Point(), new Size(double.NaN, double.NaN), null);
            var prototypeDocument = new DocumentController(fields, DocumentType, PrototypeId);
            return prototypeDocument;
        }
        public override FrameworkElement makeView(DocumentController docController, Context context)
        {
            return MakeView(docController, context);
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
            
            shape.Loaded += Background_Loaded;

            SetupBindings(shape, docController, context);

            //BindBackgroundColor(shape, docController, context);
            BindShape(shape, docController, context);

            return shape;
        }

        private static void Background_Loaded(object sender, RoutedEventArgs e)
        {
            var docView = (sender as UIElement).GetFirstAncestorOfType<DocumentView>();
            var cp = docView.GetFirstAncestorOfType<ContentPresenter>();
            Canvas.SetZIndex(cp, -100);
        }
    }
}
