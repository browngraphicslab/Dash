using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using DashShared;

namespace Dash
{

    /// <summary>
    /// A generic document type containing a single image. The Data field on an ImageBox is a reference which eventually
    /// ends in an
    /// ImageController or an ImageController
    /// </summary>
    public class TemplateBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("aaa", "Template Box");
        private static readonly string PrototypeId = "ABDDCBAF-20D7-400E-BE2E-3761313520CC";

        public TemplateBox(FieldControllerBase refToImage, double x = 0, double y = 0, double w = 200, double h = 200)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToImage);
            //(fields[KeyStore.HorizontalAlignmentKey] as TextController).Data = HorizontalAlignment.Left.ToString();
            //(fields[KeyStore.VerticalAlignmentKey] as TextController).Data = VerticalAlignment.Top.ToString();
            SetupDocument(DocumentType, PrototypeId, "ImageBox Prototype Layout", fields);
        }

        public static FrameworkElement MakeView(DocumentController docController, Context context)
        {
            // create the template view
            var tv = new TemplateView(docController);

            tv.PointerEntered += (sender, args) => tv.ManipulationMode = ManipulationModes.None;
            tv.GotFocus += (sender, args) => tv.ManipulationMode = ManipulationModes.None;
            tv.LostFocus += (sender, args) => tv.ManipulationMode = ManipulationModes.All;

            // setup bindings on the template
            SetupBindings(tv, docController, context);

            return tv;

        }

    }

}