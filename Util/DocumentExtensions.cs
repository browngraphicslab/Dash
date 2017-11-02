using DashShared;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Dash.Controllers;
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

        /// <summary>
        /// Copies a document by copying each field of the document and making a copy of the
        /// ActiveLayout if it exists.  The layout is offset by 15, or set to 'where' if specified
        /// </summary>
        /// <returns></returns>
        public static DocumentController GetCopy(this DocumentController doc, Point? where = null)
        {
            var copy = doc.MakeCopy(new List<KeyController>(new KeyController[] { KeyStore.LayoutListKey, KeyStore.DelegatesKey } ));
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
        public static DocumentController GetDataCopy(this DocumentController doc, Point? where = null)
        {
            //return GetViewCopy(doc, where);
            var del = doc;
            var activeLayout = doc.GetActiveLayout()?.Data;
            var docContext = doc.GetDereferencedField<DocumentFieldModelController>(KeyStore.DocumentContextKey, new Context(doc))?.Data;
            DocumentController newDoc = null;
            if (activeLayout == null && docContext != null)  // has DocumentContext
            {
                var copiedData = docContext.MakeCopy(new List<KeyController>(new KeyController[] { KeyStore.LayoutListKey, KeyStore.DelegatesKey, KeyStore.ActiveLayoutKey })); // copy the data and skip any layouts
                activeLayout = doc.MakeDelegate(); // inherit the layout
                activeLayout.SetField(KeyStore.DocumentContextKey, new DocumentFieldModelController(copiedData), true); // point the inherited layout at the copied document
                docContext = copiedData;
                newDoc = activeLayout;
            }
            else if (docContext == null && activeLayout != null) // has a layout
            {
                var copiedLayout = activeLayout.MakeDelegate(); // inherit the layout (so we can at least override location ... maybe width, height, too)
                docContext = doc.MakeCopy(new List<KeyController>(new KeyController[] { KeyStore.LayoutListKey, KeyStore.DelegatesKey, KeyStore.ActiveLayoutKey }));// copy the data but skip the layout
                docContext.SetField(KeyStore.ActiveLayoutKey, new DocumentFieldModelController(copiedLayout), true); // add the inherited layout back
                activeLayout = copiedLayout;
                newDoc = docContext;
            }
            var oldPosition = doc.GetPositionField();
            if (oldPosition != null)  // if original had a position field, then delegate need a new one -- just offset it
            {
                activeLayout.SetField(KeyStore.PositionFieldKey,
                    new PointFieldModelController(new Point((where == null ? oldPosition.Data.X + 15 : ((Point) where).X), (where == null ? oldPosition.Data.Y + 15 : ((Point) where).Y))),
                        true);
            }
            return newDoc;
        } 
        /// <summary>
        /// Creates an instance of a document's data and copies the documents view.
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public static DocumentController GetDataInstance(this DocumentController doc, Point? where = null)
        {
            //return GetViewCopy(doc, where);
            var del = doc;
            var activeLayout = doc.GetActiveLayout()?.Data;
            var docContext = doc.GetDereferencedField<DocumentFieldModelController>(KeyStore.DocumentContextKey, new Context(doc))?.Data;
            DocumentController newDoc = null;
            if (activeLayout == null && docContext != null)  // has DocumentContext
            {
                var copiedData = docContext.MakeDelegate(); // instance the data
                activeLayout = GetViewCopy(doc, where);
                activeLayout.SetField(KeyStore.DocumentContextKey, new DocumentFieldModelController(copiedData), true); // point the inherited layout at the copied document
                docContext = copiedData;
                newDoc = activeLayout;
            }
            else if (docContext == null && activeLayout != null) // has a layout
            {
                docContext = GetViewCopy(doc, where);
                activeLayout = docContext.GetActiveLayout()?.Data;
                activeLayout.SetField(KeyStore.PositionFieldKey, new PointFieldModelController(where== null ? new Point() : (Point)where), true);
                activeLayout.SetField(KeyStore.WidthFieldKey, new NumberFieldModelController(activeLayout.GetDereferencedField<NumberFieldModelController>(KeyStore.WidthFieldKey, null).Data), true);
                activeLayout.SetField(KeyStore.HeightFieldKey, new NumberFieldModelController(activeLayout.GetDereferencedField<NumberFieldModelController>(KeyStore.HeightFieldKey, null).Data), true);

                newDoc = docContext;
            }
            var oldPosition = doc.GetPositionField();
            if (oldPosition != null)  // if original had a position field, then delegate need a new one -- just offset it
            {
                activeLayout.SetField(KeyStore.PositionFieldKey,
                    new PointFieldModelController(new Point((where == null ? oldPosition.Data.X + 15 : ((Point)where).X), (where == null ? oldPosition.Data.Y + 15 : ((Point)where).Y))),
                        true);
            }
            return newDoc;
        }
        public static DocumentController GetSameCopy(this DocumentController doc, Point where)
        {
            var activeLayout = doc.GetActiveLayout()?.Data ?? doc;
            activeLayout?.SetField(KeyStore.PositionFieldKey, new PointFieldModelController(where), true);
            return doc;
        }
        public static DocumentController GetViewCopy(this DocumentController doc, Point? where = null)
        {
            var activeLayout = doc.GetActiveLayout()?.Data;
            var docContext = doc.GetDereferencedField<DocumentFieldModelController>(KeyStore.DocumentContextKey, new Context(doc))?.Data;
            var newDoc = doc;
            if (activeLayout == null && docContext != null)  // has DocumentContext
            {
                activeLayout = doc.MakeCopy(new List<KeyController>(new KeyController[] { KeyStore.LayoutListKey, KeyStore.DelegatesKey, KeyStore.DocumentContextKey, KeyStore.ActiveLayoutKey })); // copy the layout and skip document contexts
                newDoc = activeLayout;
            }
            else if (docContext == null && activeLayout != null) // has a layout
            {
                docContext = doc.MakeDelegate(); // inherit the document so we can override its layout
                var copiedLayout = activeLayout.MakeCopy(new List<KeyController>(new KeyController[] { KeyStore.LayoutListKey, KeyStore.DelegatesKey, KeyStore.DocumentContextKey, KeyStore.ActiveLayoutKey })); // copy the layout and skip document contexts
                docContext.SetField(KeyStore.ActiveLayoutKey, new DocumentFieldModelController(copiedLayout), true);
                newDoc = docContext;
                activeLayout = copiedLayout;
            }
            else
            {
                activeLayout = new KeyValueDocumentBox(null).Document;
                activeLayout.SetField(KeyStore.DocumentContextKey, new DocumentFieldModelController(doc), true);
                activeLayout.SetField(KeyStore.HeightFieldKey, new NumberFieldModelController(200), false);
                if (where != null)
                {
                    activeLayout.SetField(KeyStore.PositionFieldKey, new PointFieldModelController((Point)where), true);
                }
                newDoc = activeLayout;
            }
            var oldPosition = doc.GetPositionField();
            if (oldPosition != null)  // if original had a position field, then delegate needs a new one -- just offset it
            {
                activeLayout.SetField(KeyStore.PositionFieldKey,
                    new PointFieldModelController(new Point((where == null ? oldPosition.Data.X + 15 : ((Point)where).X), (where == null ? oldPosition.Data.Y + 15 : ((Point)where).Y))),
                        true);
            }

            return newDoc;
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
        public static void SetLayoutDimensions(this DocumentController doc, double ? width=null, double ? height=null, Point ? pos = null)
        {
            var layoutDelegateDoc = (doc.GetField(KeyStore.ActiveLayoutKey) as DocumentFieldModelController)?.Data ?? doc;
            if (layoutDelegateDoc != null)
            {
                if (width.HasValue)
                    layoutDelegateDoc.SetField(KeyStore.WidthFieldKey, new NumberFieldModelController((double)width), true);
                else
                    layoutDelegateDoc.SetField(KeyStore.HorizontalAlignmentKey, new TextFieldModelController(Windows.UI.Xaml.HorizontalAlignment.Stretch.ToString()), true);
                if (height.HasValue)
                    layoutDelegateDoc.SetField(KeyStore.HeightFieldKey, new NumberFieldModelController((double)height), true);
                else
                    layoutDelegateDoc.SetField(KeyStore.VerticalAlignmentKey, new TextFieldModelController(Windows.UI.Xaml.VerticalAlignment.Stretch.ToString()), true);
                if (pos.HasValue)
                    layoutDelegateDoc.SetField(KeyStore.PositionFieldKey, new PointFieldModelController((Point)pos), true);
            }
        }

        public static void SetPrototypeActiveLayout(this DocumentController doc, DocumentController activeLayout, Context context = null)
        {
            context = Context.SafeInitAndAddDocument(context, doc);
            doc.AddLayoutToLayoutList(activeLayout);

            // set the active layout on the deepest prototype since its the first one
            var deepestPrototype = doc.GetDeepestPrototype();
            deepestPrototype.SetActiveLayout(activeLayout, forceMask: true, addToLayoutList: true);
        }

        public static TextFieldModelController GetTitleFieldOrSetDefault(this DocumentController doc, Context context = null)
        {
            doc = Util.GetDataDoc(doc, context);
            context = Context.SafeInitAndAddDocument(context, doc);
            var titleKey = doc.GetField(KeyStore.TitleKey) as TextFieldModelController ?? doc.GetDereferencedField<TextFieldModelController>(KeyStore.TitleKey, context);
            if (titleKey == null)
            {
                doc.SetField(KeyStore.TitleKey, new TextFieldModelController("Untitled"), false);
                titleKey = doc.GetField(KeyStore.TitleKey) as TextFieldModelController;
            }
            return titleKey;
        }

        public static void SetTitleField(this DocumentController doc, string newTitle, Context context = null)
        {
            doc = Util.GetDataDoc(doc, context);
            context = Context.SafeInitAndAddDocument(context, doc);
            var titleKey = doc.GetField(KeyStore.TitleKey) as TextFieldModelController ?? doc.GetDereferencedField<TextFieldModelController>(KeyStore.TitleKey, context);
            if (titleKey == null)
            {
                doc.SetField(KeyStore.TitleKey, new TextFieldModelController("Untitled"), false);
                titleKey = doc.GetField(KeyStore.TitleKey) as TextFieldModelController;
            }
            Debug.Assert(titleKey != null);
            titleKey.Data = newTitle;
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

        public static DocumentController MakeCopy(this DocumentController doc, List<KeyController> excludeKeys)
        {
            var refs = new List<ReferenceFieldModelController>();
            var docIds = new Dictionary<DocumentController, DocumentController>();
            var copy   = doc.makeCopy(ref refs, ref docIds, excludeKeys);
            foreach (var d2 in docIds)
            {
                foreach (var r in refs)
                {
                    if (r is DocumentReferenceFieldController)
                    {
                        var rDoc = (DocumentReferenceFieldController)r;
                        string rId = rDoc.DocumentId;
                        if (rId == d2.Key.GetId())
                            rDoc.ChangeFieldDoc(d2.Value.GetId());
                    }
                }
            }
            return copy;
        }
        private static DocumentController makeCopy(this DocumentController doc, ref List<ReferenceFieldModelController> refs,
                ref Dictionary<DocumentController, DocumentController> docs, List<KeyController> excludeKeys)
        {
            if (doc == null)
                return doc;
            if (doc.GetField(KeyStore.AbstractInterfaceKey, true) != null)
                return doc;
            if (docs.ContainsKey(doc))
                return docs[doc];

            var copy = doc.GetPrototype()?.MakeDelegate() ??
                            new DocumentController(new Dictionary<KeyController, FieldControllerBase>(), doc.DocumentType);
            docs.Add(doc, copy);

            var fields = new Dictionary<KeyController, FieldControllerBase>();

            foreach (var kvp in doc.EnumFields(true))
            {
                if (kvp.Key.Equals(KeyStore.ThisKey))
                    fields[kvp.Key] = new DocumentFieldModelController(copy);
                else if (excludeKeys.Contains(kvp.Key))
                    fields[kvp.Key] = kvp.Value.GetCopy();
                else if (kvp.Value is DocumentFieldModelController)
                    fields[kvp.Key] = new DocumentFieldModelController(kvp.Value.DereferenceToRoot<DocumentFieldModelController>(new Context(doc)).Data.makeCopy(ref refs, ref docs, excludeKeys));
                else if (kvp.Value is DocumentCollectionFieldModelController)
                {
                    var docList = new List<DocumentController>();
                    foreach (var d in kvp.Value.DereferenceToRoot<DocumentCollectionFieldModelController>(new Context(doc)).Data)
                    {
                        docList.Add(d.makeCopy(ref refs, ref docs, excludeKeys));
                    }
                    fields[kvp.Key] = new DocumentCollectionFieldModelController(docList);
                }
                else if (kvp.Value is ReferenceFieldModelController)
                    fields[kvp.Key] = kvp.Value.GetCopy();
                else
                    fields[kvp.Key] = kvp.Value.GetCopy();

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
