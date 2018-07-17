using Dash.Controllers;
using Dash.Converters;
using DashShared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Type = Zu.TypeScript.TsTypes.Type;

namespace Dash
{
	/// <summary>
	/// Allows interactions with underlying DocumentModel.
	/// </summary>
	[DebuggerDisplay("DocumentController: {Tag}")]
    public class DocumentController : FieldModelController<DocumentModel>
    {
        public delegate void DocumentUpdatedHandler(DocumentController sender, DocumentFieldUpdatedEventArgs args,
            Context context);
        /// <summary>
        /// Dictionary mapping Key's to field updated event handlers. 
        /// </summary>
        private readonly Dictionary<KeyController, DocumentUpdatedHandler> _fieldUpdatedDictionary
            = new Dictionary<KeyController, DocumentUpdatedHandler>();

        public event EventHandler DocumentDeleted;

        public override string ToString()
        {
            return "@"+Title;
        }

        /// <summary>
        ///     A wrapper for <see cref="" />. Change this to propogate changes
        ///     to the server and across the client
        /// </summary>
        private Dictionary<KeyController, FieldControllerBase> _fields = new Dictionary<KeyController, FieldControllerBase>();

        public DocumentController() : this(new Dictionary<KeyController, FieldControllerBase>(), DocumentType.DefaultType) { }
        public DocumentController(DocumentModel model) : base(model)
        {
        }
        public DocumentController(IDictionary<KeyController, FieldControllerBase> fields, DocumentType type,
            string id = null, bool saveOnServer = true) : base(new DocumentModel(fields.ToDictionary(kv => kv.Key.KeyModel, kv => kv.Value.Model), type, id))
        {
            TypeInfo = TypeInfo.Document;
            if (saveOnServer)
            {
                IsOnServer(delegate (bool onServer)
                {
                    if (!onServer)
                    {
                        SaveOnServer();
                    }
                });
            }
            Init();
        }

        public override void Init()
        {
            // get the field controllers associated with the FieldModel id's stored in the document Model
            // put the field controllers in an observable dictionary
            var fields = DocumentModel.Fields.Select(kvp =>
                new KeyValuePair<KeyController, FieldControllerBase>(
                    ContentController<FieldModel>.GetController<KeyController>(kvp.Key),
                    ContentController<FieldModel>.GetController<FieldControllerBase>(kvp.Value))).ToList();

            SetFields(fields, true);
            DocumentType = DocumentType;
        }

        /// <summary>
        ///     A wrapper for <see cref="DashShared.DocumentType" />. Change this to propogate changes
        ///     to the server and across the client
        /// </summary>
        public DocumentType DocumentType
        {
            get => DocumentModel.DocumentType;
            set
            {
                DocumentModel.DocumentType = value;
                //If there is an issue here it is probably because 'enforceTypeCheck' is set to false.
                this.SetField<TextController>(KeyStore.DocumentTypeKey, value.Type, true, false);
            }
        }
        
        public DocumentModel DocumentModel => Model as DocumentModel;

        public string Title
        {
            get
            {
                var titleController = GetDereferencedField<TextController>(KeyStore.TitleKey, null)?.Data ??
                    GetDataDocument().GetDereferencedField<TextController>(KeyStore.TitleKey, null)?.Data;
                if (titleController != null)
                {
                    return titleController;
                }
                return DocumentType.Type;
            }
        }
        
        public DocumentController GetDataDocument()
        {
            return GetDereferencedField<DocumentController>(KeyStore.DocumentContextKey, null) ?? this;
        }

        public override FieldControllerBase Copy()
        {
            return this.MakeCopy();
        }
        public override FieldControllerBase CopyIfMapped(Dictionary<FieldControllerBase, FieldControllerBase> mapping)
        {
            if (mapping.ContainsKey(this))
                return mapping[this];
            return null;
        }

        /// <summary>
        /// looks up a document that whose primary keys match input keys
        /// </summary>
        /// <param name="fieldContents"></param>
        /// <returns></returns>
        public static DocumentController FindDocMatchingPrimaryKeys(IEnumerable<string> primaryKeyValues)
        {
            // Replace this method with a proper search function
            //foreach (var dmc in ContentController<FieldModel>.GetControllers<DocumentController>())
            //    if (!dmc.DocumentType.Type.Contains("Box") && !dmc.DocumentType.Type.Contains("Layout"))
            //    {
            //        var primaryKeys = dmc.GetDereferencedField(KeyStore.PrimaryKeyKey, null) as ListController<KeyController>;
            //        if (primaryKeys != null)
            //        {
            //            bool found = true;
            //            foreach (var value in primaryKeyValues)
            //            {
            //                bool foundValue = false;
            //                foreach (var key in primaryKeys.Data)
            //                {
            //                    var derefValue = (dmc.GetDereferencedField(key as KeyController, null) as TextController)?.Data;
            //                    if (derefValue != null)
            //                    {
            //                        if (value == derefValue)
            //                        {
            //                            foundValue = true;
            //                            break;
            //                        }
            //                    }
            //                }
            //                if (!foundValue)
            //                {
            //                    found = false;
            //                    break;
            //                }
            //            }
            //            if (found)
            //            {
            //                return dmc;
            //            }
            //        }
            //    }
            return null;
        }
        DocumentController lookupOperator(string opname)
        {
            if (opname == "Add")
                return OperatorDocumentFactory.CreateOperatorDocument(new AddOperatorController());
            if (opname == "Subtract")
            {
                return OperatorDocumentFactory.CreateOperatorDocument(new SubtractOperatorController());
            }
            if (opname == "Divide")
            {
                return OperatorDocumentFactory.CreateOperatorDocument(new DivideOperatorController());
            }
            if (opname == "Multiply")
            {
                return OperatorDocumentFactory.CreateOperatorDocument(new MultiplyOperatorController());
            }

            return null;
        }

        public FieldControllerBase ParseDocumentReference(string textInput, bool searchAllDocsIfFail)
        {
            var path = textInput.Trim(' ').Split('.');  // input has format <a>[.<b>]

            var docName = path[0];                       //search for <DocName=a>[.<FieldName=b>]
            var fieldName = (path.Length > 1 ? path[1] : "");
            var refDoc = docName == "Proto" ? GetPrototype() : docName == "This" ? this : FindDocMatchingPrimaryKeys(new List<string>(new string[] { path[0] }));
            if (refDoc != null)
            {
                if (path.Length == 1)
                {
                    return refDoc; // found <DocName=a>
                }
                else
                    foreach (var e in refDoc.EnumFields())
                        if (e.Key.Name == path[1])
                        {
                            return new DocumentReferenceController(refDoc, e.Key); // found <DocName=a>.<FieldName=b>
                        }
            }

            foreach (var e in this.EnumFields())
                if (e.Key.Name == path[0])
                {
                    return new DocumentReferenceController(refDoc, e.Key);  // found This.<FieldName=a>
                }

            //if (searchAllDocsIfFail)
            //{
            //    var searchDoc = DBSearchOperatorController.CreateSearch(this, DBTest.DBDoc, path[0], "");
            //    return new ReferenceController(searchDoc.GetId(), KeyStore.CollectionOutputKey); // return  {AllDocs}.<FieldName=a> = this
            //}
            return null;
        }

        /// <summary>
        /// Parses text input into a field controller
        /// </summary>
        public bool ParseDocField(KeyController key, string textInput, FieldControllerBase curField = null, bool copy=false)
        {
            textInput = textInput.Trim(' ');
            if (textInput.StartsWith("="))
            {
                var fieldStr = textInput.Substring(1, textInput.Length - 1);
                var strings = fieldStr.Split('(');
                if (strings.Count() == 1)  //  a document from input <DocName>[.<FieldName>]  if no document matches DocName, search for This.<FieldName>  if still no document, search for {AllDocs}.<FieldName> = this
                {
                    var parse = ParseDocumentReference(strings[0], true);
                    if (parse != null)
                        SetField(key, parse, true, false);
                    else
                    {
                        double num;
                        if (double.TryParse(fieldStr, out num))
                            SetField(key, new NumberController(num), true, false);
                        else SetField(key, new TextController(fieldStr), true, false);
                    }
                }
                else if (lookupOperator(strings[0]) != null)
                {
                    var opModel = lookupOperator(strings[0]);
                    var opFieldController = (opModel.GetField(KeyStore.OperatorKey) as OperatorController);
                    var args = strings[1].TrimEnd(')').Split(',');
                    int count = 0;
                    foreach (var a in args)
                    {
                        var docRef = ParseDocumentReference(a, false);
                        if (docRef != null)
                        {
                            opModel.SetField(opFieldController.Inputs[count++].Key, docRef, true);
                        }
                        else
                        {
                            var target = opFieldController.Inputs[count++];
                            if (target.Value.Type == TypeInfo.Number)
                            {
                                var res = 0.0;
                                if (double.TryParse(a.Trim(' '), out res))
                                    opModel.SetField(target.Key, new NumberController(res), true);
                            }
                            else if (target.Value.Type == TypeInfo.Text)
                            {
                                opModel.SetField(target.Key, new TextController(a), true);
                            }
                            else if (target.Value.Type == TypeInfo.Image)
                            {
                                opModel.SetField(target.Key, new ImageController(new Uri(a)), true);
                            }
                            else if (target.Value.Type == TypeInfo.Video)
                            {
                                opModel.SetField(target.Key, new VideoController(new Uri(a)), true);
                            }

                            else if (target.Value.Type == TypeInfo.Audio)
                            {
                                opModel.SetField(target.Key, new AudioController(new Uri(a)), true);
                            }
                        }
                    }
                    SetField(key, new DocumentReferenceController(opModel, opFieldController.Outputs.First().Key), true, false);
                }
            }
            else
            {
                if (curField != null && !(curField is ReferenceController))
                    if (curField is NumberController nc)
                    {
                        double num;
                        if (double.TryParse(textInput, out num))
                            if (copy)
                                SetField(key, new NumberController(num), true);
                            else nc.Data = num;
                        else return false;
                    }
                    else if (curField is TextController tc)
                    {
                        if (copy)
                            SetField(key, new TextController(textInput), true);
                        else tc.Data = textInput;
                    }
                    else if (curField is ImageController ic)
                    {
                        try
                        {
                            if (copy)
                                SetField(key, new ImageController(new Uri(textInput)), true);
                            else ic.Data = new Uri(textInput);
                        }
                        catch (Exception)
                        {
                            ic.Data = null;
                        }
                    }
                    else if (curField is DateTimeController)
                    {
                        return curField.TrySetValue(new DateTimeToStringConverter().ConvertXamlToData(textInput));
                    }
                    else if (curField is VideoController vc)
                    {
                        try
                        {
                            if (copy)
                                SetField(key, new VideoController(new Uri(textInput)), true);
                            else vc.Data = new Uri(textInput);
                        }
                        catch (Exception)
                        {
                            vc.Data = null;
                        }
                    }
                    else if (curField is AudioController ac)
                    {
                        try
                        {
                            if (copy)
                                SetField(key, new AudioController(new Uri(textInput)), true);
                            else ac.Data = new Uri(textInput);
                        }
                        catch (Exception)
                        {
                            ac.Data = null;
                        }
                    }
                    else if (curField is DocumentController)
                    {
                        Debug.WriteLine("Warning: changing document field into a text field");
                        SetField(key, new TextController(textInput), true);
                        //TODO tfs: fix this 
                        //throw new NotImplementedException();
                        //curField = new Converters.DocumentControllerToStringConverter().ConvertXamlToData(textInput);
                    }
                    else if (curField is ListController<DocumentController> lc)
                    {
                        if (copy)
                            SetField(key, new ListController<DocumentController>(new DocumentCollectionToStringConverter().ConvertXamlToData(textInput)), true);
                        else lc.TypedData =
                            new DocumentCollectionToStringConverter().ConvertXamlToData(textInput);
                    }
                    else if (curField is RichTextController rtc)
                    {
                        rtc.Data = new RichTextModel.RTD(textInput);
                    }
                    else
                    {
                        return false;
                    }
            }
            return true;
        }

        public void Link(DocumentController target)
        {
            var linkDocument = new RichTextNote("<link description>").Document;
            linkDocument.GetDataDocument().AddToLinks(KeyStore.LinkFromKey, new List<DocumentController>{ this });
            linkDocument.GetDataDocument().AddToLinks(KeyStore.LinkToKey, new List<DocumentController> { target });
            target.GetDataDocument().AddToLinks(KeyStore.LinkFromKey, new List<DocumentController>{ linkDocument });
            GetDataDocument().AddToLinks(KeyStore.LinkToKey, new List<DocumentController>{ linkDocument });
        }

        private bool IsTypeCompatible(KeyController key, FieldControllerBase field)
        {
            if (!IsOperatorTypeCompatible(key, field))
                return false;
            var cont = GetField(key);
            if (cont is ReferenceController) cont = cont.DereferenceToRoot(null);
            if (cont == null) return true;
            var rawField = field.DereferenceToRoot(null);

            return cont.TypeInfo == TypeInfo.Reference || cont.TypeInfo == rawField?.TypeInfo;
        }


        /// <summary>
        /// Removes a value from a list field, and then propagates that change to all delegates
        /// of this document.
        /// </summary>
        /// <param name="key">the key for the list field being modified</param>
        /// <param name="value">the value being removed from the list</param>
        public void  RemoveFromListField<T>(KeyController key, T value) where T: FieldControllerBase
        {
            GetDereferencedField<ListController<T>>(key, null)?.Remove(value);

            foreach (var delegDoc in GetDelegates().TypedData)
            {
                var items = delegDoc.GetField<ListController<T>>(key, true);
                items?.Remove(value);
                // if we're removing a document then we need to check if our delegates contain a delegate of the removed document and remove that.
                if (value is DocumentController && items != null)
                {
                    foreach (var delegateValue in items.Data.OfType<DocumentController>().Where((d) => d.IsDelegateOf(value as DocumentController)).ToArray())
                    {
                        delegDoc.RemoveFromListField<DocumentController>(key, delegateValue);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a value to a list field, and then propagates that change to all delegates
        /// of this document. This includes copying any datadocument self-references so that they 
        /// will reference the delegate.
        /// </summary>
        /// <param name="key">the key for the list field being modified</param>
        /// <param name="value">the value being added to the list</param>
        public void AddToListField<T>(KeyController key, T value) where T: FieldControllerBase
        {
            GetDereferencedField<ListController<T>>(key, null)?.Add(value);

            foreach (var d in GetDelegates().TypedData)
            {
                var items = d.GetField<ListController<T>>(key, true);
                var mapping = new Dictionary<FieldControllerBase, FieldControllerBase>();
                mapping.Add(this, d);
                if (value is DocumentController)
                {
                    // if we're adding a document, then we really add a delegate of the document to facilitate copy on write
                    var delgateValue = (value as DocumentController).MakeDelegate();
                    delgateValue.MapDocuments(mapping);

                    // bcz: if we added a document that references a field on this document, then
                    //      we need to add a field to our delegates that points to a field on the mapped delegate.  
                    //      For copy-on-write semantics,
                    //      we want the default value of the field to be a reference to the prototype's field
                    foreach (var f in EnumDisplayableFields())
                        if ((mapping[this] as DocumentController).GetField(f.Key, true) == null)
                            (mapping[this] as DocumentController).SetField(f.Key, new DocumentReferenceController(this, f.Key, true), true);

                    d.AddToListField(key, delgateValue);
                }
                else
                {
                    items.Add(value);
                }
            }
        }

        // == CYCLE CHECKING ==
        #region Cycle Checking
        private List<KeyController> GetRelevantKeys(KeyController key, Context c)
        {
            var opField = GetDereferencedField(KeyStore.OperatorKey, c) as OperatorController;
            if (opField == null)
            {
                return new List<KeyController> { key };
            }
            return new List<KeyController>(opField.Inputs.Select(i => i.Key));
        }

        /// <summary>
        /// Checks if adding the given field at the given key would cause a cycle
        /// </summary>
        /// <param name="key">The key that the given field would be inserted at</param>
        /// <param name="field">The field that would be inserted into the document</param>
        /// <returns>True if the field would cause a cycle, false otherwise</returns>
        /// TODO Make cycle detection work with two operator inputs going to the same field
        private bool CheckCycle(KeyController key, FieldControllerBase field)
        {
            if (!(field is ReferenceController))
            {
                return false;
            }
            var visitedFields = new HashSet<FieldReference>();
            visitedFields.Add(new DocumentFieldReference(this, key));
            var rfms = new Queue<Tuple<FieldControllerBase, Context>>();

            //TODO If this is a DocPointerFieldReference this might not work
            rfms.Enqueue(Tuple.Create(field, new Context(this)));

            while (rfms.Count > 0)
            {
                var t = rfms.Dequeue();
                var fm = t.Item1;
                var c = t.Item2;
                if (!(fm is ReferenceController)) continue;
                var rfm = (ReferenceController)fm;
                var fieldRef = rfm.GetFieldReference().Resolve(c);
                var doc = rfm.GetDocumentController(c);
                Context c2;
                if (c.DocContextList.Contains(doc))
                {
                    c2 = c;
                }
                else
                {
                    c2 = new Context(c);
                    c2.AddDocumentContext(doc);
                }

                foreach (var fieldReference in visitedFields)
                    if (fieldReference.Resolve(c2).Equals(fieldRef))
                        return true;

                visitedFields.Add(fieldRef);

                var keys = doc.GetRelevantKeys(rfm.FieldKey, c2);
                foreach (var keyController in keys)
                {
                    var f = doc.GetField(keyController);
                    if (f != null)
                    {
                        rfms.Enqueue(Tuple.Create(f, c2));
                    }
                }
            }

            var delegates = GetField(KeyStore.DelegatesKey, true) as ListController<DocumentController>;
            if (delegates != null)
            {
                bool cycle = false;
                foreach (var documentController in delegates.TypedData)
                {
                    cycle = cycle || documentController.CheckCycle(key, field);
                }
                return cycle;
            }
            return false;
        }
        #endregion

        // == PROTOTYPE / DELEGATE MANAGEMENT ==
        #region Delegate Management

        /// <summary>
        ///     Returns the first level of inheritance which references the passed in <see cref="KeyControllerGeneric{T}" /> or
        ///     returns null if no level of inheritance has a <see cref="Controller" /> associated with the passed in
        ///     <see cref="KeyControllerGeneric{T}" />
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public DocumentController GetPrototypeWithFieldKey(KeyController key)
        {
            if (key == null)
                return null;
            // if we mask the key by storing it as a field return ourself
            if (_fields.ContainsKey(key))
                return this;

            // otherwise get our prototype and see if it associated a Field with the Key
            var proto = GetPrototype();

            // keep searching through prototypes until we find one with the key, if we never found one return null
            return proto?.GetPrototypeWithFieldKey(key);
        }

        /// <summary>
        ///     Tries to get the Prototype of this <see cref="DocumentController" /> and associated <see cref="Model" />,
        ///     and returns null if no prototype exists
        /// </summary>
        public DocumentController GetPrototype()
        {
            // if there is no prototype return null
            if (!_fields.ContainsKey(KeyStore.PrototypeKey))
                return null;

            // otherwise try to convert the field associated with the prototype key into a DocumentController
            var documentController =
                _fields[KeyStore.PrototypeKey] as DocumentController;


            // if the field contained a DocumentController return its data, otherwise return null
            return documentController;
        }


        /// <summary>
        /// Method that returns a list of prototypes' documentcontrollers and itself, in hierarchical order 
        /// </summary>
        public LinkedList<DocumentController> GetAllPrototypes()
        {
            LinkedList<DocumentController> result = new LinkedList<DocumentController>();

            var prototype = GetPrototype();
            while (prototype != null)
            {
                result.AddFirst(prototype);
                prototype = prototype.GetPrototype();
            }
            result.AddLast(this);
            return result;
        }

        /// <summary>
        ///  Creates a delegate (child) of the given document that inherits all the fields of the prototype (parent)
        /// </summary>
        /// <returns></returns>
        public DocumentController MakeDelegate()
        {
            //var delegateModel = new DocumentModel(new Dictionary<KeyModel, FieldModel>(),
            //    DocumentType, "delegate-of-" + GetId() + "-" + Guid.NewGuid());

            //// create a controller for the child
            //var delegateController = new DocumentController(delegateModel);
            var delegateController = new DocumentController(new Dictionary<KeyController, FieldControllerBase>(), DocumentType, "delegate-of-" + Id + "-" + Guid.NewGuid());
            delegateController.Tag = (Tag ?? "") + "DELEGATE";

            // create and set a prototype field on the child, pointing to ourself
            var prototypeFieldController = this;
            delegateController.SetField(KeyStore.PrototypeKey, prototypeFieldController, true);

            // add the delegate to our delegates field
            var currentDelegates = GetDelegates();
            currentDelegates.Add(delegateController);

            var mapping = new Dictionary<FieldControllerBase, FieldControllerBase>();
            mapping.Add(this, delegateController);
            delegateController.MapDocuments(mapping);

            // return the now fully populated delegate
            return delegateController;
        }

        public void MapDocuments(Dictionary<FieldControllerBase, FieldControllerBase> mapping)
        {
            // copy all fields containing mapped elements 
            foreach (var f in EnumFields())
                if (f.Key.Equals(KeyStore.PrototypeKey) || f.Key.Equals(KeyStore.DelegatesKey))
                    continue;
                else if (f.Value is ReferenceController || f.Value is DocumentController)
                {
                    var mappedField = f.Value.CopyIfMapped(mapping);
                    if (mappedField != null)
                        SetField(f.Key, mappedField, true);
                }
                else if (f.Value is ListController<DocumentController> listDocs)
                {
                    var newListDocs = new ListController<DocumentController>();
                    foreach (var l in listDocs.TypedData)
                    {
                        var lnew = l.MakeDelegate();
                        lnew.MapDocuments(mapping);
                        newListDocs.Add(lnew);
                    }
                    SetField(f.Key, newListDocs, true);
                }
        }

        /// <summary>
        /// Returns true if the document with the passed in id is a prototype 
        /// of this document. Searches up the entire hierarchy recursively
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public bool IsDelegateOf(DocumentController doc)
        {
            var proto = GetPrototype();
            if (proto == null) return false;
            return proto.Equals(doc) || proto.IsDelegateOf(doc);
        }


        /// <summary>
        /// Gets the delegates for this <see cref="DocumentController" /> or creates a delegates field
        /// and returns it if no delegates field existed
        /// </summary>
        /// <returns></returns>
        public ListController<DocumentController> GetDelegates()
        {
            // see if we have a populated delegates field
            var currentDelegates = _fields.ContainsKey(KeyStore.DelegatesKey)
                ? _fields[KeyStore.DelegatesKey] as ListController<DocumentController>
                : null;

            // if not then populate it with a new list of documents
            if (currentDelegates == null)
            {
                currentDelegates =
                    new ListController<DocumentController>(new List<DocumentController>());
                SetField(KeyStore.DelegatesKey, currentDelegates, true);
            }
            return currentDelegates;
        }
        #endregion

        // == FIELD MANAGEMENT ==
        #region Field Management

        /// <summary>
        /// Returns the TypeInfo type of the field mapped to by key. If document contains an OperatorFieldController,
        /// checks that operator's outputs for the desired key.
        /// </summary>
        public TypeInfo GetFieldType(KeyController key)
        {
            var operatorController = GetField<ListController<OperatorController>>(key).TypedData.First();
            if (operatorController != null && operatorController.Outputs.ContainsKey(key))
            {
                return operatorController.Outputs[key];
            }

            return GetField(key)?.TypeInfo ?? TypeInfo.Any;
        }

        /// <summary>
        /// Returns the root (highest prototype level) TypeInfo type of the field mapped to by key. If document 
        /// contains an OperatorFieldController, checks that operator's outputs for the desired key.
        /// </summary>
        public TypeInfo GetRootFieldType(KeyController key)
        {
            var operatorControllerStart = GetField<ListController<OperatorController>>(KeyStore.OperatorKey);
            if (operatorControllerStart != null)
            {
                foreach (var controller in operatorControllerStart.TypedData)
                {
                    if (controller != null && controller.Outputs.ContainsKey(key))
                    {
                        return controller.Outputs[key];
                    }
                    
                }
            }
            return GetField(key)?.RootTypeInfo ?? TypeInfo.Any;
        }
        /// <summary>
        /// Removes the field mapped to by <paramref name="key"/> from the document. Fails if the
        /// field exists in the document's Prototype, since documents cannot remove inherited fields
        /// (only the owner of a field can remove it.)
        /// </summary>
        public bool RemoveField(KeyController key)
        {
            var proto = GetPrototypeWithFieldKey(key);
            if (proto == null)
            {
                return false;
            }

            if (!proto._fields.ContainsKey(key))
                return false;

            proto._fields.Remove(key, out var value);

            generateDocumentFieldUpdatedEvents(new DocumentFieldUpdatedEventArgs(value, null, FieldUpdatedAction.Remove, new DocumentFieldReference(this, key), null, false), new Context(this));

            //TODO Make this undo-able
            value.DisposeField();

            return true;
        }

        /// <summary>
        /// returns the <see cref="Controller" /> for the specified <see cref="KeyControllerGeneric{T}" /> by looking first in this
        /// <see cref="DocumentController" />
        /// and then sequentially up the hierarchy of prototypes of this <see cref="DocumentController" />. If the
        /// key is not found then it returns null.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="ignorePrototype">if false, check in </param>
        /// <returns></returns>
        public FieldControllerBase GetField(KeyController key, bool ignorePrototype = false)
        {
            // TODO this should cause an operator to execute and return the proper value
            // search up the hiearchy starting at this for the first DocumentController which has the passed in key
            var firstProtoWithKeyOrNull = ignorePrototype ? this : GetPrototypeWithFieldKey(key);

            FieldControllerBase field = null;
            firstProtoWithKeyOrNull?._fields.TryGetValue(key, out field);
            return field;
        }

        public T GetField<T>(KeyController key, bool ignorePrototype = false) where T : FieldControllerBase
        {
            return GetField(key, ignorePrototype) as T;
        }

        public T GetFieldOrCreateDefault<T>(KeyController key, bool ignorePrototype = false) where T : FieldControllerBase, new()
        {
            var field = GetField(key, ignorePrototype);
            if (field != null)
            {
                return field as T;
            }
            T t = new T();
            SetField(key, t, true);
            return t;
        }

        /// <summary>
        /// Returns whether or not the field has changed that is associated with the passed in key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="field"></param>
        /// <param name="forceMask"></param>
        /// <returns></returns>
        bool SetFieldHelper(KeyController key, FieldControllerBase field, bool forceMask)
        {
            if (field == null)
            {
                return RemoveField(key);
            }
            // get the prototype with the desired key or just get ourself
            var proto = GetPrototypeWithFieldKey(key) ?? this;
            var doc = forceMask ? this : proto;

            // get the old value of the field
            FieldControllerBase oldField;
            proto._fields.TryGetValue(key, out oldField);
            var overwrittenField = (forceMask && !this.Equals(proto)) ? null : oldField;

            // if the old and new field reference the exact same controller then we're done unless we're force-masking a field
            if (!ReferenceEquals(oldField, field) || (forceMask && !proto.Equals(doc)))
            {
                //if (proto.CheckCycle(key, field))
                //{
                //    return false;
                //}

                //field.SaveOnServer();
                overwrittenField?.DisposeField();

                doc._fields[key] = field;
                doc.DocumentModel.Fields[key.Id] = field == null ? "" : field.Model.Id;

                // fire document field updated if the field has been replaced or if it did not exist before
                var action = oldField == null ? FieldUpdatedAction.Add : FieldUpdatedAction.Replace;
                var reference = new DocumentFieldReference(this, key);
                var updateArgs = new DocumentFieldUpdatedEventArgs(oldField, field, action, reference, null, false);
                //if (key.Name != "_Cache Access Key")
                    generateDocumentFieldUpdatedEvents(updateArgs, new Context(doc));

                if (key.Equals(KeyStore.PrototypeKey))
                    ; // need to see if any prototype operators need to be run
                else if (key.Equals(KeyStore.DocumentContextKey))
                    ; // do we need to watch anything when the DocumentContext field is set?
                else
                    setupFieldChangedListeners(key, field, oldField, new Context(doc));

                return true;
            }
            return false;
        }


        /// <summary>
        ///     Sets the <see cref="Controller" /> associated with the passed in <see cref="KeyControllerGeneric{T}" /> at the first
        ///     prototype in the hierarchy that contains it. If the <see cref="KeyControllerGeneric{T}" /> is not used at any level then it is
        ///     created in this <see cref="DocumentController" />.
        ///     <para>
        ///         If <paramref name="forceMask" /> is set to true, then we never search for a prototype and simply override
        ///         any prototype that might exist by setting the field on this
        ///     </para>
        /// </summary>
        /// <param name="key">key index of field to update</param>
        /// <param name="field">FieldModel to update to</param>
        /// <param name="forceMask">add field to this document even if the field already exists on a prototype</param>
        public bool SetField(KeyController key, FieldControllerBase field, bool forceMask, bool enforceTypeCheck = true, bool withUndo = true)
        {
            var oldVal = GetField(key);
            UndoCommand newEvent = new UndoCommand(() => SetField(key, field, forceMask, false), 
                () => SetField(key, oldVal, forceMask, false));

            var fieldChanged = SetFieldHelper(key, field, forceMask);
            if (fieldChanged)
            {
                UpdateOnServer(withUndo ? newEvent : null);
            }

            if (key.Equals(KeyStore.ActiveLayoutKey) && field is DocumentController doc)
            {
                if (doc.DocumentType.Equals(TemplateBox.DocumentType))
                {
                    // TODO: ask tyler about this next line? -sy
                    //TypeInfo = TypeInfo.Template;
                }
            }

            return fieldChanged;
        }
        public bool SetField<TDefault>(KeyController key, object v, bool forceMask, bool enforceTypeCheck = true) 
            where TDefault : FieldControllerBase, new()
        {
            var field = GetField<TDefault>(key, forceMask);
            if (field != null)
            {
                if (field.TrySetValue(v))
                {
                    return true;
                }
            }
            else
            {
                var f = new TDefault();
                if (f.TrySetValue(v))
                    return SetField(key, f, forceMask, enforceTypeCheck);
            }

            return false;
        }

        /// <summary>
        ///     Sets all of the document's fields to a given Dictionary of Key FieldModel
        ///     pairs. If <paramref name="forceMask" /> is true, all the fields are set on this <see cref="DocumentController" />
        ///     otherwise each
        ///     field is written on the first prototype in the hierarchy which contains it
        /// </summary>
        public void SetFields(IEnumerable<KeyValuePair<KeyController, FieldControllerBase>> fields, bool forceMask, bool withUndo = true)
        {
            bool shouldSave = false;
            var oldFields = new Dictionary<KeyController, FieldControllerBase>();
            foreach (var kv in fields)
            {
                oldFields[kv.Key] = GetField(kv.Key);
            }
            // update with each of the new fields
            foreach (var field in fields.ToArray().Where((f) => f.Key != null))
            {
                if (SetFieldHelper(field.Key, field.Value, forceMask))
                {
                    shouldSave = true;
                }
            }
            if (shouldSave)
            {
                UndoCommand newEvent = new UndoCommand(() => SetFields(fields, forceMask, false), () => SetFields(oldFields, forceMask, false));
                UpdateOnServer(withUndo ? newEvent : null);
            }
        }

        /// <summary>
        /// Returns the Field at the given KeyController's key. If the field is a Reference to another
        /// field, follows the regerences up until a non-reference field is found and returns that.
        /// </summary>
        public FieldControllerBase GetDereferencedField(KeyController key, Context context)
        {
            // TODO this should cause an operator to execute and return the proper value
            var fieldController = GetField(key);
            context = new Context(context); //  context ?? new Context();  // bcz: THIS SHOULD BE SCRUTINIZED.  I don't think it's ever correct for a function to modify the context that's passed in.
            context.AddDocumentContext(this);
            return fieldController?.DereferenceToRoot(context ?? new Context(this));
        }

        /// <summary>
        /// Returns the Field from stored from key within the given context.
        /// </summary>
        public T GetDereferencedField<T>(KeyController key, Context context) where T : FieldControllerBase
        {
            // TODO: this should cause an operator to execute and return the proper value
            return GetDereferencedField(key, context) as T;
        }

        /// <summary>
        /// Gets a list of KewValuePairs for all Fields on the given Document.
        /// </summary>
        /// <param name="ignorePrototype">if false, will also include fields from prototype document</param>
        /// <returns></returns>
        public IEnumerable<KeyValuePair<KeyController, FieldControllerBase>> EnumFields(bool ignorePrototype = false)
        {
            foreach (KeyValuePair<KeyController, FieldControllerBase> keyFieldPair in _fields.ToArray())
            {
                yield return keyFieldPair;
            }

            if (!ignorePrototype)
            {
                var prototype = GetPrototype();
                if (prototype != null)
                    foreach (var field in prototype.EnumFields().Where(f => !_fields.ContainsKey(f.Key)))
                        yield return field;
            }
        }

        /// <summary>
        /// Gets a list of KeyValuePairs for all Fields on the given Document that can be displayed visually on the canvas.
        /// </summary>
        /// <param name="ignorePrototype">if false, will also include displayable fields from prototype document</param>
        /// <returns></returns>
        public IEnumerable<KeyValuePair<KeyController, FieldControllerBase>> EnumDisplayableFields(bool ignorePrototype = false)
        {
            foreach (KeyValuePair<KeyController, FieldControllerBase> keyFieldPair in _fields.ToArray())
            {
                if (!keyFieldPair.Key.Name.StartsWith("_"))
                    yield return keyFieldPair;
            }

            if (!ignorePrototype)
            {
                var prototype = GetPrototype();
                if (prototype != null)
                    foreach (var field in prototype.EnumDisplayableFields().Where(f => !_fields.ContainsKey(f.Key)))
                        yield return field;
            }
        }
        #endregion

        // == OPERATOR MANAGEMENT ==
        #region Operator Management
        /// <summary>
        /// Method that returns whether the input fieldmodelcontroller type is compatible to the key; if the document is not an operator type, return true always 
        /// </summary>
        /// <param name="key">key that field is mapped to</param>
        /// <param name="field">reference field model that references the field to connect</param>
        private bool IsOperatorTypeCompatible(KeyController key, FieldControllerBase field)
        {
            var opCont = GetField(KeyStore.OperatorKey) as OperatorController;
            if (opCont == null) return true;
            if (!opCont.Inputs.Any(i => i.Key.Equals(key))) return true;

            var rawField = field.DereferenceToRoot(null);
            return rawField == null || opCont.Inputs.First(i => i.Key.Equals(key)).Value.Type
                       .HasFlag(rawField.TypeInfo);
        }

        /// <summary>
        /// Returns whether or not the current document should execute.
        /// <para>
        /// Documents should execute if all the following are true
        ///     1. they are an operator
        ///     2. the input contains the updated key or the output contains the updated key
        /// </para>
        /// </summary>
        public Context ShouldExecute(Context context, KeyController updatedKey, DocumentFieldUpdatedEventArgs args, bool update=true)
        {
            context = context ?? new Context(this);
            var opFields = GetDereferencedField<ListController<OperatorController>>(KeyStore.OperatorKey, context);
            if (opFields != null)
                foreach (var opField in opFields.TypedData)
                {
                    var exec = opField.Inputs.Any(i => i.Key.Equals(updatedKey)) || opField.Outputs.ContainsKey(updatedKey);
                    if (exec)
                        context = Execute(opField, context, update, args);
                }
            return context;
        }

        public Context Execute(OperatorController opField, Context oldContext, bool update, DocumentFieldUpdatedEventArgs updatedArgs = null)
        {
            // add this document to the context
            var context = new Context(oldContext);
            context.AddDocumentContext(this);

            // create dictionaries to hold the inputs and outputs, these are being prepared
            // to be used in the actual operator's execute method
            var inputs = new Dictionary<KeyController, FieldControllerBase>(opField.Inputs.Count);
            var outputs = new Dictionary<KeyController, FieldControllerBase>(opField.Outputs.Count);

            // iterate over the operator inputs adding them to our preparing dictionaries if they 
            // exist, and returning if there is a required field that we are missing
            foreach (var opFieldInput in opField.Inputs)
            {
                // get the operator inputs based on the input keys (these are always references)
                var field = GetField(opFieldInput.Key);
                // dereference the inputs so that the field is now the actual field from the output document
                field = field?.DereferenceToRoot(context);

                if (field == null)
                {
                    // if the reference was null and the reference was recquired just return the context
                    // since the operator cannot execute
                    if (opFieldInput.Value.IsRequired)
                    {
                        return context;
                    }
                }
                else
                {
                    inputs[opFieldInput.Key] = field;
                }
            }

            //bool needsToExecute = updatedArgs != null;
            //var id = inputs.Values.Select(f => f.Id).Aggregate(0, (sum, next) => sum + next.GetHashCode());
            //var key = new KeyController(DashShared.UtilShared.GetDeterministicGuid(id.ToString()),
            //    "_Cache Access Key");

            ////TODO We should get rid of old cache values that aren't necessary at some point
            //var cache = GetFieldOrCreateDefault<DocumentController>(KeyStore.OperatorCacheKey);
            //if (updatedArgs == null)
            //{
            //    foreach (var opFieldOutput in opField.Outputs)
            //    {
            //        var field = cache.GetFieldOrCreateDefault<DocumentController>(opFieldOutput.Key)?.GetField(key);
            //        if (field == null)
            //        {
            //            needsToExecute = true;
            //            outputs.Clear();
            //            break;
            //        }
            //        else
            //        {
            //            outputs[opFieldOutput.Key] = field;
            //        }
            //    }
            //}


            //if (needsToExecute)
            {
                // execute the operator
                opField.Execute(inputs, outputs, updatedArgs);
            }

            // pass the updates along 
            foreach (var fieldModel in outputs)
            {
                //if (needsToExecute)
                //{
                //    cache.GetFieldOrCreateDefault<DocumentController>(fieldModel.Key)
                //        .SetField(key, fieldModel.Value, true);
                //}
                var reference = new DocumentFieldReference(this, fieldModel.Key);
                context.AddData(reference, fieldModel.Value);
                if (update)
                {
                    OnDocumentFieldUpdated(this, new DocumentFieldUpdatedEventArgs(null, fieldModel.Value,
                        FieldUpdatedAction.Replace, reference, null, false), context, true);
                }
            }
            return context;
        }
        #endregion

        // == VIEW GENERATION ==
        #region View Generation
        /// <summary>
        /// Generates a UI view that showcases document fields as a list of key value pairs, where key is the
        /// string key of the field and value is the rendered UI element representing the value.
        /// </summary>
        /// <returns></returns>
        private FrameworkElement makeAllViewUI(Context context)
        {
            var fields = EnumFields().Where((f) => !f.Key.IsUnrenderedKey()).ToList();
            if (fields.Count > 15)
                return MakeAllViewUIForManyFields(fields);
            var panel = fields.Count() > 1 ? (Panel)new StackPanel() : new Grid();
            void Action(KeyValuePair<KeyController, FieldControllerBase> f)
            {
                f.Value.MakeAllViewUI(this, f.Key, context, panel, this);
            }


#pragma warning disable CS4014
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Low,
                async () =>
                {
                    foreach (var f in fields)
                    {
                        Action(f);
                        await Task.Delay(5);
                    }
                });
#pragma warning restore CS4014
            return panel;
        }

        private static FrameworkElement MakeAllViewUIForManyFields(
            List<KeyValuePair<KeyController, FieldControllerBase>> fields)
        {
            var sp = new StackPanel();
            for (var i = 0; i < 16; i++)
            {
                var block = new TextBlock
                {
                    Text = i == 15
                        ? "+ " + (fields.Count - 15) + " more"
                        : "Field " + (i + 1) + ": " + fields[i].Key,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                sp.Children.Add(block);
            }
            return sp;
        }
        /// <summary>
        /// Builds the underlying XAML Framework Element representation of this document.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public FrameworkElement MakeViewUI(Context context)
        {
			//Debug.WriteLine("DOCUMENT TYPE: " + DocumentType);
			//Debug.WriteLine("DOCUMENTCONTROLLER THIS: " + this);

			// set up contexts information
			context = new Context(context);
            context.AddDocumentContext(this);
            context.AddDocumentContext(GetDataDocument());

            // if the document has a layout already, use that underlying layout's data to generate
            // the view
            var fieldModelController = GetDereferencedField(KeyStore.ActiveLayoutKey, context);
            if (fieldModelController != null)
            {
                var doc = fieldModelController.DereferenceToRoot<DocumentController>(context);

                if (doc.DocumentType.Equals(DefaultLayout.DocumentType))
                {
                    return makeAllViewUI(context);
                }
                Debug.Assert(doc != null);
                return doc.MakeViewUI(context);
            }

            if (KeyStore.TypeRenderer.ContainsKey(DocumentType))
            {
                return KeyStore.TypeRenderer[DocumentType](this, context);
            }
            else

                return makeAllViewUI(context);
        }

        #endregion

        // == OVERRIDDEN from ICOLLECTION ==
        #region ICollection Overrides
        public override void DeleteOnServer(Action success = null, Action<Exception> error = null)
        {
            if (_fields.ContainsKey(KeyStore.DelegatesKey))
            {
                var delegates = (ListController<DocumentController>)_fields[KeyStore.DelegatesKey];
                foreach (var del in delegates.Data)
                {
                    del.DeleteOnServer();
                }
            }

            foreach (var field in _fields)
            {
                field.Value.DeleteOnServer();
            }
            base.DeleteOnServer(success, error);

            DocumentDeleted?.Invoke(this, EventArgs.Empty);
        }

        public override TypeInfo TypeInfo { get; }

        public override bool TrySetValue(object value)
        {
            throw new NotImplementedException();
        }

        public override object GetValue(Context context)
        {
            return this;
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new DocumentController();
        }

        public override StringSearchModel SearchForString(string searchString)
        {
            return StringSearchModel.False;
            //return _fields.Any(field => field.Value.SearchForString(searchString) || field.Key.SearchForString(searchString));
        }
        #endregion

        // == OVERRIDEN FROM OBJECT ==
        #region Overriden from Object
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
        

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, this))
            {
                return true;
            }
            DocumentController controller = obj as DocumentController;
            if (controller == null)
            {
                return false;
            }
            return Id.Equals(controller.Id);
        }
        #endregion

        // == EVENT MANAGEMENT ==
        #region Event Management

        /// <summary>
        /// Adds a field updated listener which is only fired when the field associated with the passed in key
        /// has changed
        /// </summary>
        public void AddFieldUpdatedListener(KeyController key, DocumentUpdatedHandler handler)
        {
            if (_fieldUpdatedDictionary.ContainsKey(key))
                _fieldUpdatedDictionary[key] += handler;
            else
                _fieldUpdatedDictionary[key] = handler;
        }

        /// <summary>
        /// Removes a field listener associated with the given key's update event.
        /// </summary>
        public void RemoveFieldUpdatedListener(KeyController key, DocumentUpdatedHandler handler)
        {
            if (_fieldUpdatedDictionary.ContainsKey(key))
            {
                // ReSharper disable once DelegateSubtraction
                _fieldUpdatedDictionary[key] -= handler;
            }
        }
        /// <summary>
        /// Adds listeners to the field model updated event which fire the document model updated event
        /// </summary>
        /// <summary>
        /// Adds listeners to the field model updated event which fire the document model updated event
        /// </summary>
        void setupFieldChangedListeners(KeyController key, FieldControllerBase newField, FieldControllerBase oldField, Context context)
        {
            var reference = new DocumentFieldReference(this, key);
            ///<summary>
            /// Generates a DocumentFieldUpdated event when a fieldModelUpdated event has been fired for a field in this document.
            ///</summary>
            void TriggerDocumentFieldUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context c)
            {
                var refSender = sender as ReferenceController;
                var proto =this.GetPrototypeWithFieldKey(reference.FieldKey);
                //if (new Context(proto).IsCompatibleWith(c))
                {
                    var newContext = new Context(c);
                    if (newContext.DocContextList.Count(d => d.IsDelegateOf(this)) == 0)  // don't add This if a delegate of This is already in the Context.
                        newContext.AddDocumentContext(this);                                 // TODO lsm don't we get deepest delegate anyway, why would we not add it???

                    var updateArgs = new DocumentFieldUpdatedEventArgs(null, sender, FieldUpdatedAction.Update, reference, args, false);
                    generateDocumentFieldUpdatedEvents(updateArgs, newContext);
                }
            };
            if (newField != null && key != KeyStore.DelegatesKey /*&& key.Name != "_Cache Access Key"*/)
            {
                newField.FieldModelUpdated += TriggerDocumentFieldUpdated;

                void DisposedHandler(FieldControllerBase field)
                {
                    newField.FieldModelUpdated -= TriggerDocumentFieldUpdated;
                    newField.Disposed -= DisposedHandler;
                };
                newField.Disposed += DisposedHandler;
            }
        }


        static string spaces = "";
        void generateDocumentFieldUpdatedEvents(DocumentFieldUpdatedEventArgs args, Context newContext)
        {
            // try { Debug.WriteLine(spaces + this.Title + " -> " + args.Reference.FieldKey + " = " + args.NewValue); } catch (Exception) { }
            spaces += "  ";
            newContext =  ShouldExecute(newContext, args.Reference.FieldKey, args);
            OnDocumentFieldUpdated(this, args, newContext, true);
            spaces = spaces.Substring(2);
        }

        /// <summary>
        /// Called whenever a field on this document has been modified directly or indirectly 
        /// (by an operator, prototype, or delegate).
        /// 
        /// This then invokes the listeners added in <see cref="AddFieldUpdatedListener"/> as well as the
        /// listeners to <see cref="DocumentFieldUpdated"/>
        /// </summary>
        /// <param name="updateDelegates">whether to bubble event down to delegates</param>
        protected virtual void OnDocumentFieldUpdated(DocumentController sender, DocumentFieldUpdatedEventArgs args, Context c, bool updateDelegates)
        {
            // this invokes listeners which have been added on a per key level of granularity
            if (_fieldUpdatedDictionary.ContainsKey(args.Reference.FieldKey))
                _fieldUpdatedDictionary[args.Reference.FieldKey]?.Invoke(sender, args, c);

            // this invokes listeners which have been added on a per doc level of granularity
            if (!args.Reference.FieldKey.Equals(KeyStore.DocumentContextKey))
            {
                OnFieldModelUpdated(args, c);
            }

            // bubbles event down to delegates
            //if (updateDelegates && !args.Reference.FieldKey.Equals(KeyStore.DelegatesKey)) //TODO TFS Can't we still use this event to let delegates know that our field was updated?
            //    PrototypeFieldUpdated?.Invoke(sender, args, c);
            
            // now propagate this field model change to all delegates that don't override this field
            foreach (var d in GetDelegates().TypedData)
            {
                if (d.GetField(args.Reference.FieldKey, true) == null)
                    d.generateDocumentFieldUpdatedEvents(args, c);
            }
        }

        /// <summary>
        /// Add: Used when a field is added to a document with a key that is didn't previously contain
        /// Remove: Used when a field is removed from a document
        /// Replace: Used when a field in the document is replaced with a different field
        /// Update: Used when the value of a field in a document changes, instead of the field being replaced
        /// </summary>
        public enum FieldUpdatedAction
        {
            Add,
            Remove,
            Replace,
            Update
        }

        /// <summary>
        /// Encompasses the different type of events triggers by changing document data.
        /// </summary>
        public class DocumentFieldUpdatedEventArgs : FieldUpdatedEventArgs
        {
            public readonly FieldControllerBase OldValue;
            public readonly FieldControllerBase NewValue;
            public readonly DocumentFieldReference Reference;
            public readonly FieldUpdatedEventArgs FieldArgs;
            public bool FromDelegate;

            public DocumentFieldUpdatedEventArgs(FieldControllerBase oldValue, FieldControllerBase newValue,
                FieldUpdatedAction action, DocumentFieldReference reference, FieldUpdatedEventArgs fieldArgs, bool fromDelegate) : base(TypeInfo.Document, action)
            {
                OldValue = oldValue;
                NewValue = newValue;
                Reference = reference;
                FieldArgs = fieldArgs;
                FromDelegate = fromDelegate;
            }
        }


        #endregion

		/// <summary>
		/// Decides whether or not this pin should now be hidden or stay shown, and then reverses the setting
		/// </summary>
		/// <returns></returns>
	    public void TogglePinUnpin()
	    {
		    var isCurrentlyPinned = GetField<BoolController>(KeyStore.AnnotationVisibilityKey).Data;

		    // reverse the setting
		    SetField(KeyStore.AnnotationVisibilityKey, new BoolController(!isCurrentlyPinned), true);
		    this.SetHidden(!isCurrentlyPinned);
	    }

		/// <summary>
		/// Sets the visibility based on pinned or unpinned.
		/// </summary>
	    public void ResetPinVisibility()
		{
			var isCurrentlyPinned = GetField<BoolController>(KeyStore.AnnotationVisibilityKey).Data;
			this.SetHidden(!isCurrentlyPinned);
		}

		
    }
}