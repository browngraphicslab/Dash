using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

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


        private static DocumentCollectionFieldModelController GetLayoutList(this DocumentController doc, List<DocumentController> contextList = null)
        {
            var layoutList = doc.GetField(DashConstants.KeyStore.LayoutListKey, contextList) as DocumentCollectionFieldModelController;
            // if the layout list is null create it on the deepest prototype
            if (layoutList == null)
            {
                layoutList = new DocumentCollectionFieldModelController(new List<DocumentController>());
                doc.SetField(DashConstants.KeyStore.LayoutListKey, layoutList, false);
            }
            return layoutList;
        }


        public static void SetActiveLayout(this DocumentController doc, DocumentController activeLayout, bool forceMask = true)
        {
            doc.AddLayoutToLayoutList(activeLayout);

            // set the layout on the document that was calling this
            var layoutWrapper = new DocumentFieldModelController(activeLayout);
            doc.SetField(DashConstants.KeyStore.ActiveLayoutKey, layoutWrapper, forceMask);
        }


        public static DocumentFieldModelController GetActiveLayout(this DocumentController doc, IList<DocumentController> contextList)
        {
            return doc.GetDereferencedField(DashConstants.KeyStore.ActiveLayoutKey, contextList) as DocumentFieldModelController;
        }
    }
}
