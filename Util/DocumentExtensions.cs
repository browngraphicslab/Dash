using DashShared;
using System.Collections.Generic;

namespace Dash
{
    public static class DocumentExtensions
    {

        public static void AddLayoutToLayoutList(this DocumentController doc, DocumentController newLayoutController)
        {
            var layoutList = doc.GetLayoutList();
            // if the layoutlist contains the new layout do nothing
            if (new HashSet<DocumentController>(layoutList.GetDocuments()).Contains(newLayoutController))
            {
                return;
            }
            // otherwise add the new layout to the layout list
            layoutList.AddDocument(newLayoutController);
        }


        private static DocumentCollectionFieldModelController GetLayoutList(this DocumentController doc, Context context = null)
        {
            context = Context.SafeInitAndAddDocument(context, doc);
            var layoutList = doc.GetField(DashConstants.KeyStore.LayoutListKey, context) as DocumentCollectionFieldModelController;
            
            if (layoutList == null)
            {
                layoutList = InitializeLayoutList();
                var deepestPrototype = doc.GetDeepestPrototype(); // layout list has to be treated like a global field for each document hierarchy
                deepestPrototype.SetField(DashConstants.KeyStore.LayoutListKey, layoutList, false);
            }
            return layoutList;
        }

        private static DocumentCollectionFieldModelController InitializeLayoutList()
        {
            return new DocumentCollectionFieldModelController(new List<DocumentController>());
        }

        private static DocumentController GetDeepestPrototype(this DocumentController doc)
        {
            DocumentController nextPrototype;
            while ((nextPrototype = doc.GetPrototype()) != null)
            {
                doc = nextPrototype;
            }
            return doc;
        }


        public static void SetActiveLayout(this DocumentController doc, DocumentController activeLayout, bool forceMask = true)
        {
            doc.AddLayoutToLayoutList(activeLayout);

            // set the layout on the document that was calling this
            var layoutWrapper = new DocumentFieldModelController(activeLayout);
            doc.SetField(DashConstants.KeyStore.ActiveLayoutKey, layoutWrapper, forceMask);
        }


        public static DocumentFieldModelController GetActiveLayout(this DocumentController doc, Context context=null)
        {
            context = Context.SafeInitAndAddDocument(context, doc);
            return doc.GetDereferencedField(DashConstants.KeyStore.ActiveLayoutKey, context) as DocumentFieldModelController;
        }
    }
}
