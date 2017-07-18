﻿using DashShared;
using System.Collections.Generic;
using Windows.Foundation.Metadata;

namespace Dash
{
    public static class DocumentExtensions
    {

        /// <summary>
        /// Adds a new layout to the layout list, if that layout does not already exist in the layout list. The
        /// layout list is found in the deepestPrototype for the document
        /// </summary>
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


        /// <summary>
        /// Gets the layout list which should always be in the deepestPrototype for the document
        /// </summary>
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


        [Deprecated("We currently set a default active layout in courtesydocument.layoutCourtesyDocument", DeprecationType.Deprecate, 1)]
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

        public static void SetPrototypeActiveLayout(this DocumentController doc, DocumentController activeLayout, Context context = null)
        {
            context = Context.SafeInitAndAddDocument(context, doc);
            doc.AddLayoutToLayoutList(activeLayout);

            // set the active layout on the deepest prototype since its the first one
            var deepestPrototype = doc.GetDeepestPrototype();
            deepestPrototype.SetActiveLayout(activeLayout);
        }

        public static NumberFieldModelController GetHeightField(this DocumentController doc, Context context = null)
        {
            context = Context.SafeInitAndAddDocument(context, doc);
            var activeLayout = doc.GetActiveLayout(context);
            var heightField = activeLayout?.Data.GetDereferencedField(DashConstants.KeyStore.HeightFieldKey, context) as NumberFieldModelController;
            if (heightField == null)
            {
                heightField = doc.GetDereferencedField(DashConstants.KeyStore.HeightFieldKey, context) as NumberFieldModelController;
            }

            return heightField;
        }

        public static NumberFieldModelController GetWidthField(this DocumentController doc, Context context = null)
        {
            context = Context.SafeInitAndAddDocument(context, doc);
            var activeLayout = doc.GetActiveLayout(context);
            var widthField =  activeLayout?.Data.GetDereferencedField(DashConstants.KeyStore.WidthFieldKey, context) as NumberFieldModelController;
            if (widthField == null)
            {
                widthField = doc.GetDereferencedField(DashConstants.KeyStore.WidthFieldKey, context) as NumberFieldModelController;
            }
            return widthField;
        }

        public static PointFieldModelController GetPositionField(this DocumentController doc, Context context = null)
        {
            context = Context.SafeInitAndAddDocument(context, doc);
            var activeLayout = doc.GetActiveLayout(context);
            var posField = activeLayout?.Data.GetDereferencedField(DashConstants.KeyStore.PositionFieldKey, context) as PointFieldModelController;
            if (posField == null)
            {
                posField = doc.GetDereferencedField(DashConstants.KeyStore.PositionFieldKey, context) as PointFieldModelController;
            }

            return posField;
        }
    }
}
