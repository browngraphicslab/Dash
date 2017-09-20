﻿using DashShared;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
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

        /// <summary>
        /// Copies a document by copying each field of the document and making a copy of the
        /// ActiveLayout if it exists.  The layout is offset by 15, or set to 'where' if specified
        /// </summary>
        /// <returns></returns>
        public static DocumentController GetCopy(this DocumentController doc, Point? where)
        {
            var copy = doc.MakeCopy();
            var positionField = copy.GetPositionField();
            if (positionField != null)  // if original had a position field, then copy will, too.  Set it to 'where' or offset it 15 from original
            {
                positionField.Data = new Point((where == null ? positionField.Data.X +15:((Point)where).X), (where == null ? positionField.Data.Y + 15 : ((Point)where).Y));
            }
            return copy;
        }
        /// <summary>
        /// Creates a delegate of a document and sets the ActiveLayout field of the delegate to be a delegate of the original document's Activate Layout.
        /// The layout position will be offset by 15 or set to 'where' if specified
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public static DocumentController GetNewData(this DocumentController doc, Point? where = null)
        {
            var del = doc.MakeDelegate();
            var delLayout = doc.GetActiveLayout()?.Data?.MakeDelegate();
            if (delLayout != null)
                del.SetActiveLayout(delLayout, forceMask: true, addToLayoutList: false);
            else delLayout = del;
            var oldPosition = doc.GetPositionField();
            if (oldPosition != null)  // if original had a position field, then delegate need a new one -- just offset it
            {
                 delLayout.SetField(KeyStore.PositionFieldKey,
                new PointFieldModelController(new Point((where == null ? oldPosition.Data.X + 15 : ((Point) where).X), (where == null ? oldPosition.Data.Y + 15 : ((Point) where).Y))),
                    true);
            }
            return del;
        }
        public static DocumentController GetViewCopy(this DocumentController doc, Point? where = null)
        {
            var dataDoc = doc.GetDereferencedField<DocumentFieldModelController>(DocumentController.DocumentContextKey, new Context(doc))?.Data ??
                doc;
            var delLayout = doc.GetActiveLayout()?.Data?.GetCopy(where) ?? doc.MakeCopy(true);
            if (delLayout != null)
                delLayout.SetField(DocumentController.DocumentContextKey, new DocumentFieldModelController(dataDoc), true);
            var oldPosition = doc.GetPositionField();
            if (oldPosition != null)  // if original had a position field, then delegate need a new one -- just offset it
            {
                delLayout.SetField(KeyStore.PositionFieldKey,
                    new PointFieldModelController(new Point((where == null ? oldPosition.Data.X + 15 : ((Point)where).X), (where == null ? oldPosition.Data.Y + 15 : ((Point)where).Y))),
                    true);
            }
            return delLayout;
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
        public static DocumentController MakeActiveLayoutDelegate(this DocumentController doc, double ? width=null, double ? height=null, Point ? pos = null)
        {
            var layoutDelegateDoc = doc.GetActiveLayout(new Context(doc))?.Data?.MakeDelegate() ?? doc.MakeDelegate();
            if (layoutDelegateDoc != null)
            {
                if (width.HasValue)
                    layoutDelegateDoc.SetField(KeyStore.WidthFieldKey, new NumberFieldModelController((double)width), true);
                else
                    layoutDelegateDoc.SetField(CourtesyDocument.HorizontalAlignmentKey, new TextFieldModelController(Windows.UI.Xaml.HorizontalAlignment.Stretch.ToString()), true);
                if (height.HasValue)
                    layoutDelegateDoc.SetField(KeyStore.HeightFieldKey, new NumberFieldModelController((double)height), true);
                else
                    layoutDelegateDoc.SetField(CourtesyDocument.VerticalAlignmentKey, new TextFieldModelController(Windows.UI.Xaml.VerticalAlignment.Stretch.ToString()), true);
                if (pos.HasValue)
                    layoutDelegateDoc.SetField(KeyStore.PositionFieldKey, new PointFieldModelController((Point)pos), true);
            }
            else
                layoutDelegateDoc = doc;
            layoutDelegateDoc.SetField(DocumentController.DocumentContextKey, new DocumentFieldModelController(doc.GetField(DocumentController.DocumentContextKey) == null ? doc : doc.GetDereferencedField<DocumentFieldModelController>(DocumentController.DocumentContextKey, new Context(doc)).Data), true);
            return layoutDelegateDoc;
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
            var heightField = activeLayout?.Data.GetDereferencedField(KeyStore.HeightFieldKey, context) as NumberFieldModelController;
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
            var widthField = activeLayout?.Data.GetDereferencedField(KeyStore.WidthFieldKey, context) as NumberFieldModelController;
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
            var posField = activeLayout?.Data.GetDereferencedField(KeyStore.PositionFieldKey, context) as PointFieldModelController ??
                           doc.GetDereferencedField(KeyStore.PositionFieldKey, context) as PointFieldModelController;

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

        public static DocumentController MakeCopy(this DocumentController doc, bool shallow = false)
        {
            var refs = new List<ReferenceFieldModelController>();
            var docIds = new Dictionary<DocumentController, DocumentController>();
            var copy   = doc.makeCopy(ref refs, ref docIds, shallow);
            foreach (var d2 in docIds)
            {
                foreach (var r in refs)
                {
                    var rdoc = ContentController.GetController<DocumentController>((r.FieldReference as DocumentFieldReference)?.DocumentId);
                    if (rdoc?.GetId() == d2.Key.GetId())
                        r.ChangeFieldDoc(d2.Value.GetId());
                }
            }
            return copy;
        }
        private static DocumentController makeCopy(this DocumentController doc, ref List<ReferenceFieldModelController> refs,
                ref Dictionary<DocumentController, DocumentController> docs, bool shallow)
        {
            if (docs.ContainsKey(doc))
                return docs[doc];

            var copy = doc.GetPrototype()?.MakeDelegate() ??
                            new DocumentController(new Dictionary<KeyController, FieldModelController>(), doc.DocumentType);
            docs.Add(doc, copy);

            var fields = new Dictionary<KeyController, FieldModelController>();
            foreach (var kvp in doc.EnumFields(true))
            {
                if (kvp.Key.Equals(KeyStore.ThisKey))
                    fields[kvp.Key] = new DocumentFieldModelController(doc);
                else if (shallow || kvp.Key.Equals(KeyStore.DelegatesKey) || kvp.Key.Equals(KeyStore.LayoutListKey))
                    fields[kvp.Key] = kvp.Value.Copy();
                else if (kvp.Value is DocumentFieldModelController)
                    fields[kvp.Key] = new DocumentFieldModelController(kvp.Value.DereferenceToRoot<DocumentFieldModelController>(new Context(doc)).Data.makeCopy(ref refs, ref docs, shallow));
                else if (kvp.Value is DocumentCollectionFieldModelController)
                {
                    var docList = new List<DocumentController>();
                    foreach (var d in kvp.Value.DereferenceToRoot<DocumentCollectionFieldModelController>(new Context(doc)).Data)
                    {
                        docList.Add(d.makeCopy(ref refs, ref docs, shallow));
                    }
                    fields[kvp.Key] = new DocumentCollectionFieldModelController(docList);
                }
                else if (kvp.Value is ReferenceFieldModelController)
                    fields[kvp.Key] = kvp.Value.Copy();
                else
                    fields[kvp.Key] = kvp.Value.Copy();

                if (kvp.Value is ReferenceFieldModelController)
                {
                    refs.Add(fields[kvp.Key] as ReferenceFieldModelController);
                }

            }
            copy.SetFields(fields, true);

            return copy;
        }
    }
}
