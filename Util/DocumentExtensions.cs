using DashShared;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using DashShared.Models;

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
            var layoutList = doc.GetLayoutList(null);
            // if the layoutlist contains the new layout do nothing
            if (layoutList.GetDocuments().Contains(newLayoutController))
            {
                return;
            }
            // otherwise add the new layout to the layout list
            layoutList.AddDocument(newLayoutController);
        }


        /// <summary>
        /// Gets the layout list which should always be in the deepestPrototype for the document
        /// </summary>
        public static DocumentCollectionFieldModelController GetLayoutList(this DocumentController doc, Context context)
        {
            context = Context.SafeInitAndAddDocument(context, doc);
            var layoutList = doc.GetField(KeyStore.LayoutListKey) as DocumentCollectionFieldModelController;

            if (layoutList == null)
            {
                layoutList = InitializeLayoutList();
                var deepestPrototype = doc.GetDeepestPrototype(); // layout list has to be treated like a global field for each document hierarchy
                deepestPrototype.SetField(KeyStore.LayoutListKey, layoutList, false);
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


        public static void SetActiveLayout(this DocumentController doc, DocumentController activeLayout, bool forceMask, bool addToLayoutList)
        {
            if (addToLayoutList)
            {
                doc.AddLayoutToLayoutList(activeLayout);
            }

            // set the layout on the document that was calling this
            var layoutWrapper = new DocumentFieldModelController(activeLayout);
            doc.SetField(KeyStore.ActiveLayoutKey, layoutWrapper, forceMask);
        }


        public static DocumentFieldModelController GetActiveLayout(this DocumentController doc, Context context = null)
        {
            context = Context.SafeInitAndAddDocument(context, doc);
            return doc.GetDereferencedField(KeyStore.ActiveLayoutKey, context) as DocumentFieldModelController;
        }

        public static void SetPrototypeActiveLayout(this DocumentController doc, DocumentController activeLayout, Context context = null)
        {
            context = Context.SafeInitAndAddDocument(context, doc);
            doc.AddLayoutToLayoutList(activeLayout);

            // set the active layout on the deepest prototype since its the first one
            var deepestPrototype = doc.GetDeepestPrototype();
            deepestPrototype.SetActiveLayout(activeLayout, forceMask: true, addToLayoutList: true);
        }

        public static NumberFieldModelController GetHeightField(this DocumentController doc, Context context = null)
        {
            context = Context.SafeInitAndAddDocument(context, doc);
            var activeLayout = doc.GetActiveLayout(context);
            var heightField = activeLayout?.Data?.GetDereferencedField(KeyStore.HeightFieldKey, context) as NumberFieldModelController;
            if (heightField == null)
            {
                heightField = doc.GetDereferencedField(KeyStore.HeightFieldKey, context) as NumberFieldModelController;
            }

            return heightField;
        }

        public static NumberFieldModelController GetWidthField(this DocumentController doc, Context context = null)
        {
            context = Context.SafeInitAndAddDocument(context, doc);
            var activeLayout = doc.GetActiveLayout(context);
            var widthField =  activeLayout?.Data?.GetDereferencedField(KeyStore.WidthFieldKey, context) as NumberFieldModelController;
            if (widthField == null)
            {
                widthField = doc.GetDereferencedField(KeyStore.WidthFieldKey, context) as NumberFieldModelController;
            }
            return widthField;
        }

        public static PointFieldModelController GetPositionField(this DocumentController doc, Context context = null)
        {
            context = Context.SafeInitAndAddDocument(context, doc);
            var activeLayout = doc.GetActiveLayout(context);
            var posField = activeLayout?.Data?.GetDereferencedField(KeyStore.PositionFieldKey, context) as PointFieldModelController;
            if (posField == null)
            {
                posField = doc.GetDereferencedField(KeyStore.PositionFieldKey, context) as PointFieldModelController;
            }

            return posField;
        }

        public static PointFieldModelController GetScaleCenterField(this DocumentController doc, Context context = null)
        {
            var activeLayout = doc.GetActiveLayout()?.Data;
            var scaleCenterField = activeLayout?.GetDereferencedField(KeyStore.ScaleCenterFieldKey,
                                       new Context(context)) as PointFieldModelController ?? doc.GetDereferencedField(KeyStore.ScaleCenterFieldKey, context) as PointFieldModelController;
            return scaleCenterField;
        }

        public static PointFieldModelController GetScaleAmountField(this DocumentController doc, Context context = null)
        {
            var activeLayout = doc.GetActiveLayout()?.Data;
            var scaleAmountField = activeLayout?.GetDereferencedField(KeyStore.ScaleAmountFieldKey,
                                       new Context(context)) as PointFieldModelController ??
                                   doc.GetDereferencedField(KeyStore.ScaleAmountFieldKey, context) as
                                       PointFieldModelController;
            return scaleAmountField;
        }

        public static DocumentController GetCopy(this DocumentController doc, Context context = null)
        {
            var model = new DocumentModel(new Dictionary<KeyModel, FieldModel>(), doc.DocumentType);
            var copy = doc.GetPrototype()?.MakeDelegate() ?? new DocumentController(model);
            var fields = new ObservableDictionary<KeyControllerBase, FieldControllerBase>();
            foreach (var kvp in doc.EnumFields(true))
            {
                if (kvp.Key.Equals(KeyStore.WidthFieldKey) ||
                    kvp.Key.Equals(KeyStore.HeightFieldKey)
                    )
                {
                    fields[kvp.Key] = new NumberFieldModelController((kvp.Value as NumberFieldModelController)?.Data ?? 0);
                }
                else if (kvp.Key.Equals(KeyStore.PositionFieldKey))
                {
                    fields[kvp.Key] = new PointFieldModelController((kvp.Value as PointFieldModelController)?.Data ?? new Point());
                }
                else
                {
                    if (kvp.Key.Model == DashConstants.KeyStore.ThisKey)
                        fields[kvp.Key] = new DocumentFieldModelController(copy);
                    else fields[kvp.Key] = kvp.Value.GetCopy();
                }
            }
            copy.SetFields(fields, true);

            return copy;
        }
    }
}
