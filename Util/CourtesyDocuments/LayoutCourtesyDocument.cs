using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml;
using DashShared;

namespace Dash
{
    /// <summary>
    /// Given a document, this provides an API for getting all of the layout documents that define it's view.
    /// </summary>
    public class LayoutCourtesyDocument : CourtesyDocument
    {

        // the active layout for the doc that was passed in
        public DocumentController ActiveLayoutDocController = null;

        public LayoutCourtesyDocument(DocumentController docController)
        {
            Document = docController;
            var activeLayout = Document.GetActiveLayout();
            ActiveLayoutDocController = activeLayout == null ? InstantiateActiveLayout(Document) : activeLayout.Data;
        }

        public IEnumerable<DocumentController> GetLayoutDocuments()
        {
            var layoutDataField =
                ActiveLayoutDocController?.GetDereferencedField(DashConstants.KeyStore.DataKey, null);

            if (layoutDataField is DocumentCollectionFieldModelController) // layout data is a collection of documents each referencing some field
                foreach (var d in (layoutDataField as DocumentCollectionFieldModelController).GetDocuments())
                    yield return d;
            else if (layoutDataField is DocumentFieldModelController) // layout data is a document referencing some field
                yield return (layoutDataField as DocumentFieldModelController).Data;
            else yield return ActiveLayoutDocController; // TODO why would the layout be any other type of field model controller
        }

        protected override DocumentController GetLayoutPrototype()
        {
            throw new NotImplementedException();
        }

        protected override DocumentController InstantiatePrototypeLayout()
        {
            throw new NotImplementedException();
        }

        public override FrameworkElement makeView(DocumentController docController, Context context, bool isInterfaceBuilderLayout = true)
        {
            return MakeView(docController, context);
        }

        public static FrameworkElement MakeView(DocumentController docController, Context context = null)
        {
            context = Context.SafeInitAndAddDocument(context, docController);

            var docViewModel = new DocumentViewModel(docController)
            {
                IsDetailedUserInterfaceVisible = false,
                IsMoveable = false
            };
            var docView = new DocumentView(docViewModel);
            return docView;
        }


        private DocumentController InstantiateActiveLayout(DocumentController doc)
        {
            // instantiate default fields
            var fields = DefaultLayoutFields(new Point(), new Size(double.NaN, double.NaN));
            var newLayout = new DocumentController(fields, DashConstants.DocumentTypeStore.DefaultLayout);
            // since this is the first view of the document, set the prototype active layout to the new layout
            doc.SetPrototypeActiveLayout(newLayout);
            return newLayout;
        }
    }
}