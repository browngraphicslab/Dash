using System;
using Windows.UI.Xaml;

namespace Dash
{

    /// <summary>
    /// A generic data wrappe document display type used to display images or text fields.
    /// </summary>
    public class DataBox : CourtesyDocument
    {
        CourtesyDocument _doc;

        public DataBox(ReferenceFieldModelController refToField, bool isImage)
        {
            if (isImage)
                _doc = new ImageBox(refToField);
            else
                _doc = new TextingBox(refToField);
        }

        public override DocumentController Document
        {
            get { return _doc.Document; }
            set { _doc.Document = value; }
        }

        protected override DocumentController InstantiatePrototypeLayout()
        {
            throw new NotImplementedException();
        }

        protected override DocumentController GetLayoutPrototype()
        {
            throw new NotImplementedException();
        }

        public override FrameworkElement makeView(DocumentController docController,
            Context context, bool isInterfaceBuilderLayout = true)
        {
            return _doc.makeView(docController, context);
        }
    }
}