using DashShared;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Foundation;
using Dash.Controllers;
using System;
using Windows.UI.Xaml;

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
            if (layoutList.Contains(newLayoutController))
            {
                return;
            }
            // otherwise add the new layout to the layout list
            layoutList.Add(newLayoutController);
        }


        /// <summary>
        /// Gets the layout list which should always be in the deepestPrototype for the document
        /// </summary>
        public static ListController<DocumentController> GetLayoutList(this DocumentController doc, Context context)
        {
            context = Context.SafeInitAndAddDocument(context, doc);
            var layoutList = doc.GetField(KeyStore.LayoutListKey) as ListController<DocumentController>;

            if (layoutList == null)
            {
                layoutList = InitializeLayoutList();
                var deepestPrototype = doc.GetDeepestPrototype(); // layout list has to be treated like a global field for each document hierarchy
                deepestPrototype.SetField(KeyStore.LayoutListKey, layoutList, false);
            }
            return layoutList;
        }

        private static ListController<DocumentController> InitializeLayoutList()
        {
            return new ListController<DocumentController>(new List<DocumentController>());
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

        public static DocumentController CreateSnapshot(this DocumentController collection, bool copyData = false)
        {
            var docs = collection.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);
            if (docs != null)
            {
                var snap = new CollectionNote(new Point(), CollectionViewType.Freeform, double.NaN, double.NaN,
                    copyData ? docs.Select(doc => doc.GetDataCopy()) : docs.Select(doc => doc.GetViewCopy())).Document;

                var snapshots = collection.GetDataDocument().GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.SnapshotsKey);
                snapshots.Add(snap);
                snap.GetDataDocument().SetTitle(collection.Title + $"_snapshot{snapshots.Count}");
                snap.SetFitToParent(true);
                return snap;
            }
            return null;
        }

        /// <summary>
        /// Copies a document by copying each field of the document.  The layout is offset by 15, or set to 'where' if specified
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
        /// Copies a document and replaces its data document context with a copy of the original documents data context
        /// The layout position will be offset by 15 or set to 'where' if specified
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public static DocumentController GetDataCopy(this DocumentController doc, Point? where = null)
        {
            var activeLayout = doc;
            var docContext = doc.GetDereferencedField<DocumentController>(KeyStore.DocumentContextKey, new Context(doc));
            DocumentController newDoc = null;
            if ( docContext != null)  // has DocumentContext
            {
                var copiedData = docContext.MakeCopy(new List<KeyController>(new KeyController[] { KeyStore.LayoutListKey, KeyStore.DelegatesKey })); // copy the data and skip any layouts
                activeLayout = doc.MakeDelegate(); // inherit the layout
                activeLayout.SetField(KeyStore.DocumentContextKey, copiedData, true); // point the inherited layout at the copied document
                docContext = copiedData;
                newDoc = activeLayout;
            }
            var oldPosition = doc.GetPositionField();
            if (oldPosition != null)  // if original had a position field, then delegate need a new one -- just offset it
            {
                activeLayout.SetPosition(new Point((where == null ? oldPosition.Data.X + 15 : ((Point) where).X), 
                                                   (where == null ? oldPosition.Data.Y + 15 : ((Point) where).Y)));
            }
            return newDoc;
        }

        /// <summary>
        /// Creates an instance of a document and overrides width/height/and position
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public static DocumentController GetViewInstance(this DocumentController doc, Point? where=null)
        {
            var docDelegate = doc.MakeDelegate();
            docDelegate.SetPosition(where ?? new Point());
            docDelegate.SetWidth(docDelegate.GetDereferencedField<NumberController>(KeyStore.WidthFieldKey, null).Data);
            docDelegate.SetHeight(docDelegate.GetDereferencedField<NumberController>(KeyStore.HeightFieldKey, null).Data);
            return docDelegate;
        }
        /// <summary>
        /// Creates an instance of a document's data and its view.
        /// </summary>
        /// <param name="where"></param
        /// <returns></returns>
        public static DocumentController GetDataInstance(this DocumentController doc, Point? where = null)
        {
            var del = doc;
            var origDocContext = doc.GetDataDocument();
            var mapping        = new Dictionary<FieldControllerBase, FieldControllerBase>();
            DocumentController newDoc = null, newLayout = null;
            if (origDocContext != null)  // has DocumentContext
            {
                var newDocContext = origDocContext.MakeDelegate(); // instance the data
                newLayout = doc.MakeDelegate();
                mapping.Add(origDocContext, newDocContext);
                mapping.Add(doc, newLayout);
                newDocContext.MapDocuments(mapping);
                newLayout.MapDocuments(mapping);// point the inherited layout at the copied document
                newDoc = newLayout;
            }
            var oldPosition = doc.GetPositionField();
            if (oldPosition != null)  // if original had a position field, then delegate need a new one -- just offset it
            {
                newLayout.SetPosition(new Point(where?.X ?? oldPosition.Data.X + 15, where?.Y ?? oldPosition.Data.Y + 15));
            }
            // bcz: shouldn't have to explicitly mask the fields like this, but since we don't have copy-on-write, we need to.
            // Note: bindings might need to be changed to create copy-on-write
            foreach (var f in origDocContext.EnumDisplayableFields())
                if ((mapping[origDocContext] as DocumentController).GetField(f.Key, true) == null)
                    (mapping[origDocContext] as DocumentController).SetField(f.Key, new DocumentReferenceController(origDocContext, f.Key, true), true);

            return newDoc;
        }
        public static DocumentController GetSameCopy(this DocumentController doc, Point where)
        {
            doc.SetPosition(where);
            return doc;
        }
        public static DocumentController GetKeyValueAlias(this DocumentController doc, Point? where = null)
        {
            var keyValueLayout =  new KeyValueDocumentBox(null).Document;
            keyValueLayout.Tag = "KeyValueBox";
            keyValueLayout.SetField(KeyStore.DocumentContextKey, doc, true);
            keyValueLayout.SetHeight(500);
            if (where != null)
            {
                keyValueLayout.SetPosition((Point)where);
            }
            var oldPosition = doc.GetPositionField();
            if (oldPosition != null)  // if original had a position field, then delegate needs a new one -- just offset it
            {
                keyValueLayout.SetPosition(
                        new Point(where?.X ?? oldPosition.Data.X + (doc.GetActualSize()?.X ?? 0) + 70, 
                        where?.Y ?? oldPosition.Data.Y));
            }

            return keyValueLayout;
        }

        public static DocumentController GetPreviewDocument(this DocumentController doc, Point? where = null)
        {
            var oldPosition = doc.GetPositionField();
            if (oldPosition != null)  // if original had a position field, then delegate needs a new one -- just offset it
            {
                where = new Point(where?.X ?? oldPosition.Data.X + doc.GetField<PointController>(KeyStore.ActualSizeKey).Data.X + 70,
                            where?.Y ?? oldPosition.Data.Y);
            }
            Debug.Assert(where != null);
            var previewDoc =
                new PreviewDocument(
                    new DocumentReferenceController(doc, KeyStore.SelectedSchemaRow), where.Value);
            throw new Exception("ActiveLayout code has not been updated");
            return new DocumentController(new Dictionary<KeyController, FieldControllerBase>
            {
                //[KeyStore.ActiveLayoutKey] = previewDoc.Document,
                [KeyStore.TitleKey] = new TextController("Preview Document")
            }, new DocumentType());
        }
    


        public static DocumentController GetViewCopy(this DocumentController doc, Point? where = null)
        {
            var docContext = doc.GetDataDocument();
            var newDoc = doc;
            if (docContext != null || doc.GetPosition() != null)  // has DocumentContext
            {
                newDoc = doc.MakeCopy(new List<KeyController>(new KeyController[] { KeyStore.LayoutListKey, KeyStore.DelegatesKey, KeyStore.PrototypeKey }), // skip layout & delegates
                                            new List<KeyController>(new KeyController[] { KeyStore.DocumentContextKey })); // don't copy the document context
            }
            else
            {
                var newLayout = new KeyValueDocumentBox(null).Document;
                newLayout.SetField(KeyStore.DocumentContextKey, doc, true);
                newLayout.SetHeight(200);
                if (where != null)
                {
                    newLayout.SetPosition((Point)where);
                }
                newDoc = newLayout;
            }
            var oldPosition = doc.GetPositionField();
            if (oldPosition != null)  // if original had a position field, then delegate needs a new one
            {
                newDoc.SetPosition(new Point((where == null ? oldPosition.Data.X : ((Point)where).X), (where == null ? oldPosition.Data.Y : ((Point)where).Y)));
            }

            return newDoc;
        }

        public static void RestoreNeighboringContext(this DocumentController doc)
        {
            var dataDocument = doc.GetDataDocument();
            var neighboringRaw = dataDocument.GetDereferencedField(KeyStore.WebContextKey, null);
            string url = null;
            var type = neighboringRaw?.TypeInfo.ToString();
            if (type == "List")
            {
                var neighboring = neighboringRaw as ListController<TextController>;
                if (neighboring != null && neighboring.Count > 0)
                {
                    var context = doc.GetFirstContext();
                    MainPage.Instance.WebContext.SetScroll(context.Scroll);
                    url = context.Url;
                }
            } else if (type == "Text")
            {
                url = (neighboringRaw as TextController).Data;
            }

            if (url != null)
            {
                MainPage.Instance.WebContext?.SetUrl(url);
                
            }

        }

        public static void CaptureNeighboringContext(this DocumentController doc)
        {
            if (doc == null)
            {
                return;
            }
            DocumentController dataDocument = doc.GetDataDocument();
            dataDocument.SetField<DateTimeController>(KeyStore.DateModifiedKey, DateTime.Now, true);

            if (MainPage.Instance.WebContext == null) return;

            //var neighboring = dataDocument.GetDereferencedField<ListController<TextController>>(KeyStore.WebContextKey, null);

            //if (neighboring == null)
            //{
            //    neighboring = new ListController<TextController>();
            //    dataDocument.SetField(KeyStore.WebContextKey, neighboring, true);
            //}

            //var context = MainPage.Instance.WebContext.GetAsContext();

            //if (neighboring.TypedData.Count > 0 && neighboring.TypedData.Last() != null)
            //{
            //    var last = neighboring.TypedData.Last().Data.CreateObject<DocumentContext>();
            //    if (context.Equals(last))
            //    {
            //        neighboring.Remove(neighboring.TypedData.Last());
            //    }
            //    neighboring.Add(new TextController(context.Serialize()));
            //}
            //else
            //{
            //    neighboring.Add(new TextController(context.Serialize()));
            //}
        }


        public static DocumentContext GetLongestViewedContext(this DocumentController doc)
        {
            var dataDocument = doc.GetDataDocument();
            var neighboring = dataDocument.GetDereferencedField<ListController<TextController>>(KeyStore.WebContextKey, null);
            if (neighboring != null && neighboring.Count > 0)
            {
                var contexts = neighboring.Select(td => td.Data.CreateObject<DocumentContext>());
                var maxDuration = contexts.Max(context => context.ViewDuration);
                var longestViewed = contexts.First(context => context.ViewDuration == maxDuration);
                return longestViewed;
            }
            return null;
        }

        public static string GetLongestViewedContextUrl(this DocumentController doc)
        {
            var dataDocument = doc.GetDataDocument();
            var neighboring = dataDocument.GetDereferencedField<ListController<TextController>>(KeyStore.WebContextKey, null);
            if (neighboring != null && neighboring.Count > 0)
            {
                var contexts = neighboring.Select(td => td.Data.CreateObject<DocumentContext>());
                var grouped = contexts.GroupBy(c => c.Url);
                var enumerable = grouped as IGrouping<string, DocumentContext>[] ?? grouped.ToArray();
                var maxSum = enumerable.Max(group => group.Sum(i => i.ViewDuration));
                var url = enumerable.First(group => group.Sum(i => i.ViewDuration) == maxSum).Key;
                return url;
            }
            return null;
        }

        public static DocumentContext GetLastContext(this DocumentController doc)
        {
            var dataDocument = doc.GetDataDocument();
            var neighboring = dataDocument.GetDereferencedField<ListController<TextController>>(KeyStore.WebContextKey, null);
            if (neighboring != null && neighboring.Count > 0)
            {
                return neighboring.Last().Data.CreateObject<DocumentContext>();
            }
            return null;
        }

        public static DocumentContext GetFirstContext(this DocumentController doc)
        {
            var dataDocument = doc.GetDataDocument();
            var neighboring = dataDocument.GetDereferencedField<ListController<TextController>>(KeyStore.WebContextKey, null);
            if (neighboring != null && neighboring.Count > 0)
            {
                return neighboring.First().Data.CreateObject<DocumentContext>();
            }
            return null;
        }

        public static List<DocumentContext> GetAllContexts(this DocumentController doc)
        {
            var dataDocument = doc.GetDataDocument();
            var neighboring = dataDocument.GetDereferencedField<ListController<TextController>>(KeyStore.WebContextKey, null);
            if (neighboring != null && neighboring.Count > 0)
            {
                return neighboring.Select(d => d.Data.CreateObject<DocumentContext>()).ToList();
            }
            return null;
        }


        // TODO bcz: this feels hacky -- is there a better way to get a reasonable layout for a document?
        public static DocumentController GetLayoutFromDataDocAndSetDefaultLayout(this DocumentController doc)
        {
            return doc;
        }
        public static void SetLayoutDimensions(this DocumentController doc, double ? width=null, double ? height=null, Point ? pos = null)
        {
            if (width.HasValue)
                doc.SetWidth((double)width);
            else
                doc.SetHorizontalAlignment( Windows.UI.Xaml.HorizontalAlignment.Stretch);
            if (height.HasValue)
                doc.SetHeight((double)height);
            else
                doc.SetVerticalAlignment(Windows.UI.Xaml.VerticalAlignment.Stretch);
            if (pos.HasValue)
                doc.SetPosition((Point)pos);
        }
        public static TextController GetTitleFieldOrSetDefault(this DocumentController doc)
        {
            var context = new Context(doc);
            var titleKey = doc.GetField(KeyStore.TitleKey) as TextController ?? doc.GetDereferencedField<TextController>(KeyStore.TitleKey, context);
            if (titleKey == null)
            {
                doc.SetTitle("Title");
                titleKey = doc.GetField(KeyStore.TitleKey) as TextController;
            }
            return titleKey;
        }
        

        public static NumberController GetHeightField(this DocumentController doc, Context context = null)
        {
            return  doc.GetDereferencedField(KeyStore.HeightFieldKey, null) as NumberController;
        }

        public static NumberController GetWidthField(this DocumentController doc, Context context = null)
        {
            return doc.GetDereferencedField(KeyStore.WidthFieldKey, null) as NumberController;
        }

        public static PointController GetPositionField(this DocumentController doc, Context context = null)
        {
            context = Context.SafeInitAndAddDocument(context, doc);
            return doc.GetDereferencedField(KeyStore.PositionFieldKey, context) as PointController;
        }

        public static PointController GetScaleAmountField(this DocumentController doc, Context context = null)
        {
            context = Context.SafeInitAndAddDocument(context, doc);
            return doc.GetDereferencedField(KeyStore.ScaleAmountFieldKey, context) as PointController;
        }

        public static DocumentController MakeCopy(this DocumentController doc, List<KeyController> excludeKeys = null, List<KeyController> dontCopyKeys = null)
        {
            var refs = new List<ReferenceController>();
            var oldToNewDocMappings = new Dictionary<DocumentController, DocumentController>();
            var copy = doc.makeCopy(ref refs, ref oldToNewDocMappings, excludeKeys, dontCopyKeys);
            foreach (var oldToNewDoc in oldToNewDocMappings)
            {
                foreach (var r in refs)
                {
                    var referenceDoc = r as DocumentReferenceController ?? (r as PointerReferenceController)?.DocumentReference as DocumentReferenceController;
                    if (referenceDoc?.DocumentController == oldToNewDoc.Key) // if reference pointed to a doc that got copied
                       referenceDoc.DocumentController = oldToNewDoc.Value;  // then update the reference to point to the new doc
                }
            }
            return copy;
        }


        public static Op.Name GetDishName<T>(this T controller) where T : OperatorController => DSL.GetFuncName(controller);

        static DocumentController makeCopy(
                this DocumentController doc, 
                ref List<ReferenceController> refs,
                ref Dictionary<DocumentController, DocumentController> oldToNewDocMappings, 
                List<KeyController> excludeKeys, 
                List<KeyController> dontCopyKeys)
        {
            if (excludeKeys == null)
            {
                excludeKeys = new List<KeyController>{KeyStore.LayoutListKey, KeyStore.DelegatesKey};
            }
            if (doc == null)
                return doc;
            if (doc.GetField(KeyStore.AbstractInterfaceKey, true) != null)
                return doc;
            if (oldToNewDocMappings.ContainsKey(doc))
                return oldToNewDocMappings[doc];

            var copy = doc.GetPrototype()?.MakeDelegate() ??
                            new DocumentController(new Dictionary<KeyController, FieldControllerBase>(), doc.DocumentType);
            oldToNewDocMappings.Add(doc, copy);

            var fields = new Dictionary<KeyController, FieldControllerBase>();

            foreach (var kvp in doc.EnumFields(true))
            {
                if (excludeKeys != null && excludeKeys.Contains(kvp.Key))
                    continue;
                else if (dontCopyKeys != null && dontCopyKeys.Contains(kvp.Key)) //  point to the same field data.
                    fields[kvp.Key] = kvp.Value;
                else if (kvp.Value is DocumentController)
                    fields[kvp.Key] = kvp.Value.DereferenceToRoot<DocumentController>(new Context(doc)).makeCopy(ref refs, ref oldToNewDocMappings, excludeKeys, dontCopyKeys);
                else if (kvp.Value is ListController<DocumentController>)
                {
                    var docList = new List<DocumentController>();
                    foreach (var d in kvp.Value.DereferenceToRoot<ListController<DocumentController>>(new Context(doc)))
                    {
                        docList.Add(d.makeCopy(ref refs, ref oldToNewDocMappings, excludeKeys, dontCopyKeys));
                    }
                    fields[kvp.Key] = new ListController<DocumentController>(docList);
                }
                else if (kvp.Value is ReferenceController refCtrl)
                {
                    fields[kvp.Key] = refCtrl.Copy();
                    refs.Add(fields[kvp.Key]as ReferenceController);
                }
                else
                    fields[kvp.Key] = kvp.Value.Copy();
            }
            copy.SetFields(fields, true);

            return copy;
        }
    }
}
