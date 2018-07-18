using DashShared;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Foundation;
using Dash.Controllers;
using System;

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
            if (layoutList.GetElements().Contains(newLayoutController))
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
            var activeLayout = doc.GetActiveLayout();
            var docContext = doc.GetDereferencedField<DocumentController>(KeyStore.DocumentContextKey, new Context(doc));
            DocumentController newDoc = null;
            if (activeLayout == null && docContext != null)  // has DocumentContext
            {
                var copiedData = docContext.MakeCopy(new List<KeyController>(new KeyController[] { KeyStore.LayoutListKey, KeyStore.DelegatesKey, KeyStore.ActiveLayoutKey })); // copy the data and skip any layouts
                activeLayout = doc.MakeDelegate(); // inherit the layout
                activeLayout.SetField(KeyStore.DocumentContextKey, copiedData, true); // point the inherited layout at the copied document
                docContext = copiedData;
                newDoc = activeLayout;
            }
            else if (docContext == null && activeLayout != null) // has a layout
            {
                var copiedLayout = activeLayout.MakeDelegate(); // inherit the layout (so we can at least override location ... maybe width, height, too)
                docContext = doc.MakeCopy(new List<KeyController>(new KeyController[] { KeyStore.LayoutListKey, KeyStore.DelegatesKey, KeyStore.ActiveLayoutKey }));// copy the data but skip the layout
                docContext.SetField(KeyStore.ActiveLayoutKey, copiedLayout, true); // add the inherited layout back
                activeLayout = copiedLayout;
                newDoc = docContext;
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
        /// Creates an instance of a document's activeLayout and overrides width/height/and position
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public static DocumentController GetViewInstance(this DocumentController doc, Point? where=null)
        {
            var activeLayout = (doc.GetActiveLayout() ?? doc).MakeDelegate();
            activeLayout.SetPosition(where ?? new Point());
            activeLayout.SetWidth(activeLayout.GetDereferencedField<NumberController>(KeyStore.WidthFieldKey, null).Data);
            activeLayout.SetHeight(activeLayout.GetDereferencedField<NumberController>(KeyStore.HeightFieldKey, null).Data);
            return activeLayout;
        }
        /// <summary>
        /// Creates an instance of a document's data and copies the documents view.
        /// </summary>
        /// <param name="where"></param
        /// <returns></returns>
        public static DocumentController GetDataInstance(this DocumentController doc, Point? where = null)
        {
            var del = doc;
            var origLayout     = doc.GetActiveLayout();
            var origDocContext = doc.GetDataDocument();
            var mapping        = new Dictionary<FieldControllerBase, FieldControllerBase>();
            DocumentController newDoc = null, newLayout = null;
            if (origLayout == null && origDocContext != null)  // has DocumentContext
            {
                var newDocContext = origDocContext.MakeDelegate(); // instance the data
                newLayout = doc.MakeDelegate();
                mapping.Add(origDocContext, newDocContext);
                mapping.Add(doc, newLayout);
                newDocContext.MapDocuments(mapping);
                newLayout.MapDocuments(mapping);// point the inherited layout at the copied document
                newDoc = newLayout;
            }
            else if (origDocContext == null && origLayout != null) // has a layout
            {
                newDoc = GetViewCopy(doc, where);
                newLayout = newDoc.GetActiveLayout();
                newLayout.SetPosition(where ?? new Point());
                newLayout.SetWidth(newLayout.GetDereferencedField<NumberController>(KeyStore.WidthFieldKey, null).Data);
                newLayout.SetHeight(newLayout.GetDereferencedField<NumberController>(KeyStore.HeightFieldKey, null).Data);
            }
            else if (origDocContext != null && origLayout != null)
            {
                newDoc = doc.MakeDelegate();
                var newDocContext = origDocContext.MakeDelegate(); // instance the data
                newLayout = origLayout.MakeDelegate();
                mapping.Add(origDocContext, newDocContext);
                mapping.Add(origLayout, newLayout);
                newLayout.MapDocuments(mapping);
                newLayout.SetField(KeyStore.DocumentContextKey, newDocContext, true); // point the inherited layout at the copied document
                newDoc.SetField(KeyStore.DocumentContextKey, newDocContext, true);
                newDoc.SetField(KeyStore.ActiveLayoutKey,    newLayout, true);
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
            var activeLayout = doc.GetActiveLayout() ?? doc;
            activeLayout?.SetPosition(where);
            return doc;
        }
        public static DocumentController GetKeyValueAlias(this DocumentController doc, Point? where = null)
        {
            var activeLayout =  new KeyValueDocumentBox(null).Document;
            activeLayout.Tag = "KeyValueBox";
            activeLayout.SetField(KeyStore.DocumentContextKey, doc, true);
            activeLayout.SetHeight(500);
            if (where != null)
            {
                activeLayout.SetPosition((Point)where);
            }
            var oldPosition = doc.GetPositionField();
            if (oldPosition != null)  // if original had a position field, then delegate needs a new one -- just offset it
            {
                activeLayout.SetPosition(
                        new Point(where?.X ?? oldPosition.Data.X + (doc.GetActualSize()?.X ?? doc.GetActiveLayout().GetActualSize()?.X ?? 0) + 70, 
                        where?.Y ?? oldPosition.Data.Y));
            }

            return activeLayout;
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
            return new DocumentController(new Dictionary<KeyController, FieldControllerBase>
            {
                [KeyStore.ActiveLayoutKey] = previewDoc.Document,
                [KeyStore.TitleKey] = new TextController("Preview Document")
            }, new DocumentType());
        }
    


        public static DocumentController GetViewCopy(this DocumentController doc, Point? where = null)
        {
            var activeLayout = doc.GetActiveLayout();
            var docContext = doc.GetDataDocument();
            var newDoc = doc;
            var newLayout = activeLayout;
            if (activeLayout == null && (docContext != null || doc.GetPosition() != null))  // has DocumentContext
            {
                newDoc = newLayout = doc.MakeCopy(new List<KeyController>(new KeyController[] { KeyStore.LayoutListKey, KeyStore.DelegatesKey, KeyStore.ActiveLayoutKey, KeyStore.PrototypeKey }), // skip layout & delegates
                                            new List<KeyController>(new KeyController[] { KeyStore.DocumentContextKey })); // don't copy the document context
            }
            else if (activeLayout != null) // has a layout
            {
                newDoc = doc.MakeDelegate(); // inherit the document so we can override its layout
                newLayout = activeLayout.MakeCopy(new List<KeyController>(new KeyController[] { KeyStore.LayoutListKey, KeyStore.DelegatesKey,  KeyStore.ActiveLayoutKey }),
                                                  new List<KeyController>(new KeyController[] { KeyStore.DocumentContextKey})); // copy the layout and skip document contexts
                newDoc.SetField(KeyStore.ActiveLayoutKey, newLayout, true);
            }
            else
            {
                newLayout = new KeyValueDocumentBox(null).Document;
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
                newLayout.SetPosition(new Point((where == null ? oldPosition.Data.X : ((Point)where).X), (where == null ? oldPosition.Data.Y : ((Point)where).Y)));
            }

            return newDoc;
        }
        /*
        public class SetContextClass
        {
            public DocumentController DataDocument;
            int yPos;
            public SetContextClass()
            {
                MainPage.Instance.WebContext.LoadCompleted += WebContext_LoadCompleted;
            }

            private void WebContext_LoadCompleted(object sender, Windows.UI.Xaml.Navigation.NavigationEventArgs e)
            {
                MainPage.Instance.WebContext.InvokeScriptAsync("eval", new[] { "window.scrollTo(0," + yPos + ");" });
            }

            public void UpdateNeighboringContext()
            {
                var neighboring = DataDocument.GetDereferencedField<ListController<TextController>>(KeyStore.NeighboringDocumentsKey, null);
                if (neighboring != null && neighboring.TypedData.Count == 2)
                {
                    var uri = neighboring.TypedData.First().Data;
                    var where = neighboring.TypedData.Last().Data;
                    if (int.TryParse(where, out yPos))
                    {
                        MainPage.Instance.WebContext.Navigate(new Uri(uri));
                    }
                }
            }
        }
        public class GetContextClass
        {
            public DocumentController DataDocument;
            public void CaptureNeighboringContext()
            {
                MainPage.Instance.WebContext.InvokeScriptAsync("eval", new[] { "window.external.notify(window.scrollY.toString());" });
            }
            public GetContextClass()
            {
                MainPage.Instance.WebContext.ScriptNotify -= scriptNotify;
                MainPage.Instance.WebContext.ScriptNotify += scriptNotify;
            }
            private void scriptNotify(object sender, NotifyEventArgs e)
            {
                MainPage.Instance.WebContext.ScriptNotify -= scriptNotify;
                
                DataDocument.SetField(KeyStore.NeighboringDocumentsKey, new ListController<TextController>(new TextController[] {
                    new TextController(MainPage.Instance.WebContextUri.AbsoluteUri),
                    new TextController(e.Value)}), true);
            }
        }*/
        public static void RestoreNeighboringContext(this DocumentController doc)
        {
            var dataDocument = doc.GetDataDocument();
            var neighboring = dataDocument.GetDereferencedField<ListController<TextController>>(KeyStore.WebContextKey, null);
            if (neighboring != null && neighboring.TypedData.Count > 0)
            {
                var context = doc.GetFirstContext();
                if (context != null)
                {
                    MainPage.Instance.WebContext.SetUrl(context.Url);
                    MainPage.Instance.WebContext.SetScroll(context.Scroll);
                }
            }
        }

        public static void CaptureNeighboringContext(this DocumentController doc)
        {
            var dataDocument = doc.GetDataDocument();
            dataDocument.SetField<DateTimeController>(KeyStore.ModifiedTimestampKey, DateTime.Now, true);
            if (MainPage.Instance.WebContext == null)
            {
                return;
            }

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
            if (neighboring != null && neighboring.TypedData.Count > 0)
            {
                var contexts = neighboring.TypedData.Select(td => td.Data.CreateObject<DocumentContext>());
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
            if (neighboring != null && neighboring.TypedData.Count > 0)
            {
                var contexts = neighboring.TypedData.Select(td => td.Data.CreateObject<DocumentContext>());
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
            if (neighboring != null && neighboring.TypedData.Count > 0)
            {
                return neighboring.TypedData.Last().Data.CreateObject<DocumentContext>();
            }
            return null;
        }

        public static DocumentContext GetFirstContext(this DocumentController doc)
        {
            var dataDocument = doc.GetDataDocument();
            var neighboring = dataDocument.GetDereferencedField<ListController<TextController>>(KeyStore.WebContextKey, null);
            if (neighboring != null && neighboring.TypedData.Count > 0)
            {
                return neighboring.TypedData.First().Data.CreateObject<DocumentContext>();
            }
            return null;
        }

        public static List<DocumentContext> GetAllContexts(this DocumentController doc)
        {
            var dataDocument = doc.GetDataDocument();
            var neighboring = dataDocument.GetDereferencedField<ListController<TextController>>(KeyStore.WebContextKey, null);
            if (neighboring != null && neighboring.TypedData.Count > 0)
            {
                return neighboring.TypedData.Select(d => d.Data.CreateObject<DocumentContext>()).ToList();
            }
            return null;
        }

        public static void SetActiveLayout(this DocumentController doc, DocumentController activeLayout, bool forceMask, bool addToLayoutList)
        {
            if (addToLayoutList)
            {
                doc.AddLayoutToLayoutList(activeLayout);
            }

            // set the layout on the document that was calling this
            var layoutWrapper = activeLayout;
            doc.SetField(KeyStore.ActiveLayoutKey, layoutWrapper, forceMask);
        }


        // TODO bcz: this feels hacky -- is there a better way to get a reasonable layout for a document?
        public static DocumentController GetLayoutFromDataDocAndSetDefaultLayout(this DocumentController doc)
        {
            var isLayout = doc.GetField(KeyStore.DocumentContextKey) != null;
            var layoutDocType = (doc.GetField(KeyStore.ActiveLayoutKey) as DocumentController)?.DocumentType;
            if (!isLayout && (layoutDocType == null || layoutDocType.Equals(DefaultLayout.DocumentType)))
            {
                var layoutDoc = new KeyValueDocumentBox(doc);

                layoutDoc.Document.SetWidth(300);
                layoutDoc.Document.SetHeight(100);
                doc.SetActiveLayout(layoutDoc.Document, forceMask: true, addToLayoutList: false);
            }

            return isLayout ? doc : doc.GetActiveLayout(null);
        }
        public static DocumentController GetActiveLayout(this DocumentController doc, Context context = null)
        {
            context = Context.SafeInitAndAddDocument(context, doc);
            return doc.GetDereferencedField<DocumentController>(KeyStore.ActiveLayoutKey, context);
        }
        public static void SetLayoutDimensions(this DocumentController doc, double ? width=null, double ? height=null, Point ? pos = null)
        {
            var layoutDelegateDoc = (doc.GetField(KeyStore.ActiveLayoutKey) as DocumentController) ?? doc;
            if (layoutDelegateDoc != null)
            {
                if (width.HasValue)
                    layoutDelegateDoc.SetWidth((double)width);
                else
                    layoutDelegateDoc.SetHorizontalAlignment( Windows.UI.Xaml.HorizontalAlignment.Stretch);
                if (height.HasValue)
                    layoutDelegateDoc.SetHeight((double)height);
                else
                    layoutDelegateDoc.SetVerticalAlignment(Windows.UI.Xaml.VerticalAlignment.Stretch);
                if (pos.HasValue)
                    layoutDelegateDoc.SetPosition((Point)pos);
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

        public static TextController GetTitleFieldOrSetDefault(this DocumentController doc)
        {
            var dataDoc = doc.GetDataDocument();
            var context = new Context(dataDoc);
            var titleKey = dataDoc.GetField(KeyStore.TitleKey) as TextController ?? dataDoc.GetDereferencedField<TextController>(KeyStore.TitleKey, context);
            if (titleKey == null)
            {
                dataDoc.SetTitle("Title");
                titleKey = dataDoc.GetField(KeyStore.TitleKey) as TextController;
            }
            return titleKey;
        }
        

        public static NumberController GetHeightField(this DocumentController doc, Context context = null)
        {
            context = Context.SafeInitAndAddDocument(context, doc);
            var activeLayout = doc.GetActiveLayout(context);
            var heightField = activeLayout?.GetDereferencedField(KeyStore.HeightFieldKey, context) as NumberController ??
                              doc.GetDereferencedField(KeyStore.HeightFieldKey, context) as NumberController;

            return heightField;
        }

        public static NumberController GetWidthField(this DocumentController doc, Context context = null)
        {
            context = Context.SafeInitAndAddDocument(context, doc);
            var activeLayout = doc.GetActiveLayout(context);
            var widthField =  activeLayout?.GetDereferencedField(KeyStore.WidthFieldKey, context) as NumberController ??
                              doc.GetDereferencedField(KeyStore.WidthFieldKey, context) as NumberController;
            return widthField;
        }

        public static PointController GetPositionField(this DocumentController doc, Context context = null)
        {
            context = Context.SafeInitAndAddDocument(context, doc);
            var activeLayout = doc.GetActiveLayout(context);
            var posField = activeLayout?.GetDereferencedField(KeyStore.PositionFieldKey, context) as PointController ??
                           doc.GetDereferencedField(KeyStore.PositionFieldKey, context) as PointController;

            return posField;
        }

        public static PointController GetScaleAmountField(this DocumentController doc, Context context = null)
        {
            context = Context.SafeInitAndAddDocument(context, doc);
            var activeLayout = doc.GetActiveLayout();
            var scaleAmountField = activeLayout?.GetDereferencedField(KeyStore.ScaleAmountFieldKey, context) as PointController ??
                                   doc.GetDereferencedField(KeyStore.ScaleAmountFieldKey, context) as PointController;

            return scaleAmountField;
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
                       referenceDoc.ChangeFieldDoc(oldToNewDoc.Value);  // then update the reference to point to the new doc
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

            //TODO tfs: why do we make a delegate in copy?
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
                    foreach (var d in kvp.Value.DereferenceToRoot<ListController<DocumentController>>(new Context(doc)).TypedData)
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
