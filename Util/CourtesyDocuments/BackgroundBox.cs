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

namespace Dash
{ 
    class BackgroundBox : CourtesyDocument
    {
        /// <summary>
     /// The document type which is uniquely associated with pdf boxes
     /// </summary>
        public static DocumentType DocumentType = new DocumentType("B15BB50C-0C84-46F9-BFD7-D25BAF0E80A5", "Background Box");

       
        /// <summary>
        /// The prototype id is used to make sure that only one prototype is every created
        /// </summary>
        private static string PrototypeId = "88A3B7F5-7828-4251-ACFC-E56428316203";

        public BackgroundBox( double x = 0, double y = 0, double w = 200, double h = 200)
        {  
            // set fields based on the parameters
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), null);
            
            // get a delegate of the prototype layout (which already has fields set on it)
            Document = GetLayoutPrototype().MakeDelegate();
            var r = new Random();
            var hexColor = Color.FromArgb(0x33, (byte)r.Next(255), (byte)r.Next(255), (byte)r.Next(255)).ToString();
            Document.SetField(KeyStore.BackgroundColorKey, new TextController(hexColor), true);

            // replace any of the default fields on the prototype delegate with the new fields
            Document.SetFields(fields, true);
           // Document.SetField(KeyStore.DocumentContextKey, Document, true);

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
            var background = new Grid();
            background.Loaded += Background_Loaded;

            // make the pdf respond to resizing, interactions etc...
            SetupBindings(background, docController, context);
            
            return background;
        }

        private static void Background_Loaded(object sender, RoutedEventArgs e)
        {
            var docView = (sender as Grid).GetFirstAncestorOfType<DocumentView>();
            var cp = docView.GetFirstAncestorOfType<ContentPresenter>();
            Canvas.SetZIndex(cp, -100);

        }
    }
}
