using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using DashShared;

namespace Dash
{
    public class DefaultLayout : CourtesyDocument
    {
        private const string PrototypeId = "BDDF5F47-172D-4A12-8E26-BA95C22F0950";
        public static DocumentType DocumentType = new DocumentType("5226FDA9-268A-4325-8090-C1100EE6AB50", "Default Layout");

        public DefaultLayout(double x = 0, double y = 0, double w = 200, double h = 200)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h));
            Document = GetLayoutPrototype().MakeDelegate();
            Document.SetFields(fields, true);
        }

        protected override DocumentController GetLayoutPrototype()
        {
            var prototype = ContentController.GetController<DocumentController>(PrototypeId);
            if (prototype == null)
            {
                prototype = InstantiatePrototypeLayout();
            }
            return prototype;
        }


        protected override DocumentController InstantiatePrototypeLayout()
        {
            var fields = DefaultLayoutFields(new Point(), new Size(double.NaN, double.NaN));
            var prototypeDocument = new DocumentController(fields, DocumentType, PrototypeId);
            return prototypeDocument;
        }
    }
}
