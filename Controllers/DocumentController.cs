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
using Windows.UI.Xaml.Data;
using Microsoft.Toolkit.Uwp.Helpers;

// ReSharper disable once CheckNamespace
namespace Dash
{
    /// <summary>
    /// Allows interactions with underlying DocumentModel.
    /// </summary>
    //[DebuggerDisplay("DocumentController")]
    public sealed class DocumentController : FieldModelController<DocumentModel>
    {
        public delegate void DocumentUpdatedHandler(DocumentController sender, DocumentFieldUpdatedEventArgs args);

        /// <summary>
        /// Dictionary mapping Key's to field updated event handlers. 
        /// </summary>
        private readonly Dictionary<KeyController, DocumentUpdatedHandler> _fieldUpdatedDictionary = new Dictionary<KeyController, DocumentUpdatedHandler>();

        public event EventHandler DocumentDeleted;

        private static readonly List<KeyController> BehaviorKeys = new List<KeyController>
        {
            // tapped events
            KeyStore.LeftTappedOpsKey,
            KeyStore.RightTappedOpsKey,
            KeyStore.DoubleTappedOpsKey,
            KeyStore.FieldUpdatedOpsKey,
            KeyStore.LowPriorityOpsKey,
            KeyStore.ModeratePriorityOpsKey,
            KeyStore.HighPriorityOpsKey
        };

        public override string ToString()
        {
            string prefix = GetField<TextController>(KeyStore.CollectionViewTypeKey) == null ? "@" : "#";
            return $"{prefix}{Title}";
        }

        private bool _initialized = true;
        private bool _initializing = false;

        /// <summary>
        ///     A wrapper for <see cref="" />. Change this to propogate changes
        ///     to the server and across the client
        /// </summary>
        private Dictionary<KeyController, FieldControllerBase> _fields = new Dictionary<KeyController, FieldControllerBase>();

        public DocumentController() : this(new Dictionary<KeyController, FieldControllerBase>(), DocumentType.DefaultType) { }

        public static DocumentController CreateFromServer(DocumentModel model)
        {
            return new DocumentController(model);
        }

        internal void AddBehavior(KeyController triggerKey, OperatorController op)
        {
            var existingOps = GetField<ListController<OperatorController>>(triggerKey);
            if (existingOps == null)
            {
                var ops = new ListController<OperatorController> { op };
                SetField(triggerKey, ops, true);
                return;
            }

            existingOps.Add(op);
        }

        public override async Task InitializeAsync()
        {
            if (_initializing || _initialized)
            {
                return;
            }

            _initializing = true;

            var endpoint = RESTClient.Instance.Fields;

            var keys = await endpoint.GetControllersAsync<KeyController>(DocumentModel.Fields.Keys);
            var values = await endpoint.GetControllersAsync(DocumentModel.Fields.Values);
            SetFields(new Dictionary<KeyController, FieldControllerBase>(keys.Zip(values,
                    (k, v) => new KeyValuePair<KeyController, FieldControllerBase>(k, v))), true);
            _initialized = true;
            _initializing = false;
        }

        private DocumentController(DocumentModel model) : base(model)
        {
            _initialized = false;
        }

        public DocumentController(IDictionary<KeyController, FieldControllerBase> fields, DocumentType type, string id = null) : base(new DocumentModel(fields.ToDictionary(kv => kv.Key.Id, kv => kv.Value.Id), type, id))
        {
            //TODO RefCount
            //_fields = new Dictionary<KeyController, FieldControllerBase>(fields);
            SetFields(fields, true);
            DocumentType = DocumentType;
        }

        public bool IsMovingCollections { get; set; }

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
        public string        Title         => this.GetTitle();
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

        //links this => target
        public DocumentController Link(DocumentController target, LinkBehavior behavior, string specTitle = null)
        {
            //document that represents the actual link
            var linkDocument = new RichTextNote("New link description...").Document;

            if (specTitle == null)
            {
                //create unique, default tag 
                specTitle = "Annotation";
            }

            linkDocument.GetDataDocument().GetFieldOrCreateDefault<ListController<OperatorController>>(KeyStore.OperatorKey, true).Add(new LinkDescriptionTextOperator());
            linkDocument.GetDataDocument().SetLinkBehavior(behavior);
            linkDocument.GetDataDocument().SetField<TextController>(KeyStore.LinkTagKey, specTitle, true);
            linkDocument.GetDataDocument().SetField(KeyStore.LinkSourceKey, this, true);
            linkDocument.GetDataDocument().SetField(KeyStore.LinkDestinationKey, target, true);
            target?.GetDataDocument().AddToLinks(KeyStore.LinkFromKey, new List<DocumentController> { linkDocument });
            GetDataDocument().AddToLinks(KeyStore.LinkToKey, new List<DocumentController> { linkDocument });
            return linkDocument;
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
        public void RemoveFromListField<T>(KeyController key, T value) where T : FieldControllerBase
        {
            GetDereferencedField<ListController<T>>(key, null)?.Remove(value);

            foreach (var delegDoc in GetDelegates())
            {
                var items = delegDoc.GetField<ListController<T>>(key, true);
                items?.Remove(value);
                // if we're removing a document then we need to check if our delegates contain a delegate of the removed document and remove that.
                if (value is DocumentController && items != null)
                {
                    foreach (var delegateValue in items.OfType<DocumentController>().Where((d) => d.IsDelegateOf(value as DocumentController)).ToArray())
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
        public void AddToListField<T>(KeyController key, T value, int? index = null) where T : FieldControllerBase
        {
            if (index is int intIndex)
            {
                GetFieldOrCreateDefault<ListController<T>>(key).Insert(intIndex, value);
            }
            else
            {
                GetFieldOrCreateDefault<ListController<T>>(key).Add(value);
            }

            foreach (var d in GetDelegates())
            {
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

                    d.AddToListField(key, delgateValue, index);
                }
                else
                {
                    d.AddToListField(key, value, index);
                }
            }
        }

        protected override IEnumerable<FieldControllerBase> GetReferencedFields()
        {
            foreach (var kvp in _fields)
            {
                yield return kvp.Key;
                yield return kvp.Value;
            }
        }

        private readonly Dictionary<KeyController, FieldUpdatedHandler> _fieldHandlerDictionary = new Dictionary<KeyController, FieldUpdatedHandler>();
        private void ReferenceContainedField(KeyController key, FieldControllerBase field)
        {
            var reference = new DocumentFieldReference(this, key);

            void TriggerDocumentFieldUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context c)
            {
                var updateArgs = new DocumentFieldUpdatedEventArgs(null, sender, FieldUpdatedAction.Update, reference, args, false);
                generateDocumentFieldUpdatedEvents(updateArgs);
            }
            //TODO RefCount
            ReferenceField(field);
            ReferenceField(key);
            if (key != KeyStore.DelegatesKey && key != KeyStore.PrototypeKey && key != KeyStore.DocumentContextKey)
            {
                if (IsReferenced)
                {
                    field.FieldModelUpdated += TriggerDocumentFieldUpdated;
                    Debug.Assert(!_fieldHandlerDictionary.ContainsKey(key));
                    _fieldHandlerDictionary[key] = TriggerDocumentFieldUpdated;
                }
            }

        }

        private void ReleaseContainedField(KeyController key, FieldControllerBase field)
        {
            ReleaseField(key);
            ReleaseField(field);
            if (key != KeyStore.DelegatesKey && key != KeyStore.PrototypeKey && key != KeyStore.DocumentContextKey)
            {
                if (IsReferenced)
                {
                    Debug.Assert(_fieldHandlerDictionary.ContainsKey(key));
                    var handler = _fieldHandlerDictionary[key];
                    field.FieldModelUpdated -= handler;
                    _fieldHandlerDictionary.Remove(key);
                }
            }
        }

        protected override void RefInit()
        {
            foreach (var fieldControllerBase in _fields)
            {
                ReferenceContainedField(fieldControllerBase.Key, fieldControllerBase.Value);
            }
        }

        protected override void RefDestroy()
        {
            foreach (var fieldControllerBase in _fields)
            {
                ReleaseContainedField(fieldControllerBase.Key, fieldControllerBase.Value);
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
                foreach (var documentController in delegates)
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
            if (_fields.TryGetValue(KeyStore.PrototypeKey, out var prototype))
            {
                return prototype as DocumentController;
            }

            return null;
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
                    foreach (var l in listDocs)
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
            var operatorController = GetField<ListController<OperatorController>>(key).First();
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
                foreach (var controller in operatorControllerStart)
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
        public bool RemoveField(KeyController key, bool force = false)
        {
            var (removed, doc, args) = RemoveFieldHelper(key, force);

            if (!removed)
            {
                return false;
            }

            doc.UpdateOnServer(new UndoCommand(() => doc.RemoveField(key), () => doc.SetField(key, args.OldValue, true)));

            doc.generateDocumentFieldUpdatedEvents(args);

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
            if (field as T != null)
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
        private (bool updated, DocumentController, DocumentFieldUpdatedEventArgs args) SetFieldHelper(KeyController key, FieldControllerBase field, bool forceMask)
        {
            if (field == null)
            {
                return RemoveFieldHelper(key, forceMask);
            }
            // get the prototype with the desired key or just get ourself
            var proto = GetPrototypeWithFieldKey(key) ?? this;
            var doc = forceMask ? this : proto;

            // get the old value of the field
            proto._fields.TryGetValue(key, out var oldField);

            // if the old and new field reference the exact same controller and 
            // the document with the field is the document we're setting it on, we're done
            if (ReferenceEquals(oldField, field) && proto.Equals(doc))
            {
                return (false, null, null);
            }

            //if (proto.CheckCycle(key, field))
            //{
            //    return false;
            //}

            // if doc == proto, then we are actually replacing the field,
            // so we need to release it
            if (doc == proto && oldField != null)
            {
                doc.ReleaseContainedField(key, oldField);
            }
            doc.ReferenceContainedField(key, field);

            doc._fields[key] = field;
            doc.DocumentModel.Fields[key.Id] = field.Id;

            // fire document field updated if the field has been replaced or if it did not exist before
            var action     = oldField == null ? FieldUpdatedAction.Add : FieldUpdatedAction.Replace;
            var reference  = new DocumentFieldReference(doc, key);
            var updateArgs = new DocumentFieldUpdatedEventArgs(oldField, field, action, reference, null, false);

            return (true, doc, updateArgs);
        }

        private (bool, DocumentController, DocumentFieldUpdatedEventArgs) RemoveFieldHelper(KeyController key, bool forceMask)
        {
            var doc = forceMask ? this : GetPrototypeWithFieldKey(key);
            if (doc == null)
            {
                return (false, null, null);
            }

            if (!doc._fields.TryGetValue(key, out var oldField))
            {
                return (false, null, null);
            }

            doc.ReleaseContainedField(key, oldField);
            var removedField = doc._fields.Remove(key);
            var removedModel = doc.DocumentModel.Fields.Remove(key.Id);
            Debug.Assert(removedField);
            Debug.Assert(removedModel);

            return (true, doc, new DocumentFieldUpdatedEventArgs(oldField, null, FieldUpdatedAction.Remove,
                    new DocumentFieldReference(doc, key), null, false));
        }

        public void SendMessage(KeyController key, FieldControllerBase value)
        {
            generateDocumentFieldUpdatedEvents(new DocumentFieldUpdatedEventArgs(null, value, FieldUpdatedAction.Add, new DocumentFieldReference(this, key), null, false));
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
        public bool SetField(KeyController key, FieldControllerBase field, bool forceMask, bool enforceTypeCheck = true, bool updateBindings = true)
        {
            //TODO tfs: Shouldn't this be in SetFieldHelper?
            if (updateBindings)
            {
                RemoveOperatorForKey(key);
            }

            var (set, doc, args) = SetFieldHelper(key, field, forceMask);

            if (!set)
            {
                return false;
            }

            var oldField = args.OldValue;
            doc.UpdateOnServer(new UndoCommand(() => doc.SetField(key, field, true), () => doc.SetField(key, oldField, true)));

            doc.generateDocumentFieldUpdatedEvents(args);

            return true;
        }

        private void RemoveOperatorForKey(KeyController key)
        {
            var opFields = GetField<ListController<OperatorController>>(KeyStore.OperatorKey,        false) ?? new ListController<OperatorController>();
            var rmFields = GetField<ListController<OperatorController>>(KeyStore.RemoveOperatorsKey, false) ?? new ListController<OperatorController>();
            bool removedField = false;
            foreach (var opfield in opFields.ToArray())
            {
                foreach (var output in opfield.Outputs)
                {
                    if (output.Key.Equals(key) && !rmFields.Contains(opfield))
                    {
                        rmFields.Add(opfield);
                        removedField = true;
                    }
                }
            }
            if (removedField)
            {
                SetField(KeyStore.RemoveOperatorsKey, rmFields, true, updateBindings: false);
            }
        }

        public bool SetField<TDefault>(KeyController key, object v, bool forceMask, bool enforceTypeCheck = true)
            where TDefault : FieldControllerBase, new()
        {
            if (v is FieldControllerBase)
            {
                Debug.Fail("This method should be used when you have the data for a field, not a field itself. If you have a field, use the non-generic SetField, if you are passing in a field you just created, just pass the data into this instead of making a new field");
            }
            var field = GetField<TDefault>(key, forceMask);
            if (field != null)
            {
                if (field.TrySetValue(v))
                {
                    RemoveOperatorForKey(key);
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
        public void SetFields(IEnumerable<KeyValuePair<KeyController, FieldControllerBase>> fields, bool forceMask)
        {
            var keyValuePairs = fields.ToList();

            var changedFields = new Dictionary<DocumentController, List<DocumentFieldUpdatedEventArgs>>();

            // update with each of the new fields
            foreach (var field in keyValuePairs.Where(f => f.Key != null))
            {
                var (updated, doc, args) = SetFieldHelper(field.Key, field.Value, forceMask);
                if (!updated)
                {
                    continue;
                }
                if (changedFields.TryGetValue(doc, out var l))
                {
                    l.Add(args);
                }
                else
                {
                    changedFields.Add(doc, new List<DocumentFieldUpdatedEventArgs>{args});
                }
            }

            foreach (var kvp in changedFields)
            {
                var doc = kvp.Key;
                var l = kvp.Value;
                var oldFields = l.ToDictionary(k => k.Reference.FieldKey, f => f.OldValue);
                var newFields = l.ToDictionary(k => k.Reference.FieldKey, f => f.NewValue);

                UndoCommand newEvent = new UndoCommand(() => doc.SetFields(newFields, true), () => SetFields(oldFields, true));
                UpdateOnServer(newEvent);
            }

            //TODO This can probably be merged into the previous for loop, this just makes so 
            // updates aren't sent until everything is UpdateOnServer'ed
            foreach (var changedField in changedFields)
            {
                foreach (var args in changedField.Value)
                {
                    changedField.Key.generateDocumentFieldUpdatedEvents(args);
                }
            }
        }

        /// <summary>
        /// Returns the Field at the given KeyController's key. If the field is a Reference to another
        /// field, follows the regerences up until a non-reference field is found and returns that.
        /// </summary>
        public FieldControllerBase GetDereferencedField(KeyController key, Context context = null)
        {
            // TODO this should cause an operator to execute and return the proper value
            context = new Context(context); 
            context.AddDocumentContext(this);
            return new DocumentFieldReference(this, key).DereferenceToRoot(context);
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
        public void ShouldExecute(KeyController updatedKey, DocumentFieldUpdatedEventArgs args, bool update = true)
        {
            if (args.NewValue?.DereferenceToRoot(null) is DocumentController) return;

            var ops           = new List<OperatorController>();
            var remOps        = new HashSet<OperatorController>();
            for (var proto = this; proto != null; proto = proto.GetPrototype())
            {
                var opFields = proto.GetField<ListController<OperatorController>>(KeyStore.OperatorKey, true) ?? new ListController<OperatorController>();
                foreach (var operatorController in opFields)
                {
                    ops.Add(operatorController);
                }
                var remOpFields = proto.GetField<ListController<OperatorController>>(KeyStore.RemoveOperatorsKey, true) ?? new ListController<OperatorController>();
                foreach (var operatorController in remOpFields)
                {
                    remOps.Add(operatorController);
                }
            }

            foreach (var opField in ops.Where(op => !remOps.Contains(op)))
            {
                if (opField.Inputs.Any(i => i.Key.Equals(updatedKey)))
                {
                    Execute(opField, update, args);
                }
            }
        }

        public async void Execute(OperatorController opField, bool update, DocumentFieldUpdatedEventArgs updatedArgs = null)
        {
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
                field = field?.DereferenceToRoot(null);

                if (field == null && opFieldInput.Value.IsRequired)
                {
                    // if the reference was null and the reference was recquired just return the context
                    // since the operator cannot execute
                    if (opFieldInput.Value.IsRequired)
                    {
                        return;
                    }
                }
                else
                {
                    inputs[opFieldInput.Key] = field;
                }
            }

            //if (needsToExecute)
            try
            {
                // execute the operator
                await opField.Execute(inputs, outputs, updatedArgs);
            }
            catch (ScriptExecutionException e)
            {
                return;
            }

            // pass the updates along 
            foreach (var fieldModel in outputs)
            {
                SetField(fieldModel.Key, fieldModel.Value, true, updateBindings: false);
            }
        }
        #endregion

        // == VIEW GENERATION ==
        #region View Generation
        /// <summary>
        /// Builds the underlying XAML Framework Element representation of this document.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public FrameworkElement MakeViewUI(Context context)
        {
            Debug.Assert(IsReferenced, "Making a view of an unreferenced document is usually a bad idea, as many event handlers won't be set up." +
                                       " Consider storing this document in another referenced document/list if it is an embeded view of some type, or make it a root to make it referenced");

            if (this.GetXaml() is string xamlField)
            {
                try
                {
                    var fe = (FrameworkElement)Windows.UI.Xaml.Markup.XamlReader.Load(xamlField);
                    fe.Loaded += Grid_Loaded;
                    return fe;
                }
                catch (Exception e)
                {
                }
            }
            if (KeyStore.TypeRenderer.ContainsKey(DocumentType))
            {
                return KeyStore.TypeRenderer[DocumentType](this, null);
            }

            return KeyValueDocumentBox.MakeView(this, null);
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            (sender as FrameworkElement).Loaded -= Grid_Loaded;
            var descendants = (sender as FrameworkElement).GetDescendants().ToList();
            var textFields = descendants.OfType<TextBlock>().Where((ggg) => ggg.Name.StartsWith("xTextField"));
            foreach (var fieldReplacement in textFields)
            {
                var fieldName = fieldReplacement.Name.Replace("xTextField", "");
                var fieldKey = KeyController.Get(fieldName);
                TextingBox.SetupBindings(fieldReplacement, GetDataDocument().GetDataDocument(), fieldKey, null);
            }
            var editTextFields = descendants.OfType<EditableTextBlock>().Where((ggg) => ggg.Name.StartsWith("xTextField"));
            foreach (var fieldReplacement in editTextFields)
            {
                var fieldName = fieldReplacement.Name.Replace("xTextField", "");
                var fieldKey = KeyController.Get(fieldName);
                TextingBox.SetupBindings(fieldReplacement, GetDataDocument().GetDataDocument(), fieldKey, null);
            }
            var richTextFields = descendants.OfType<RichEditView>().Where((rtv) => rtv.Name.StartsWith("xRichTextField"));
            foreach (var fieldReplacement in richTextFields)
            {
                var fieldName = fieldReplacement.Name.Replace("xRichTextField", "");
                var fieldKey = KeyController.Get(fieldName);
                RichTextBox.SetupBindings(fieldReplacement, GetDataDocument().GetDataDocument(), fieldKey, null);
            }
            var imageFields = descendants.OfType<EditableImage>().Where((rtv) => rtv.Name.StartsWith("xImageField"));
            foreach (var fieldReplacement in imageFields)
            {
                var fieldName = fieldReplacement.Name.Replace("xImageField", "");
                var fieldKey = KeyController.Get(fieldName);
                ImageBox.SetupBinding(fieldReplacement, GetDataDocument().GetDataDocument(), fieldKey,  null);
            }
            var pdfFields = descendants.OfType<PdfView>().Where((rtv) => rtv.Name.StartsWith("xPdfField"));
            foreach (var fieldReplacement in pdfFields)
            {
                var fieldName = fieldReplacement.Name.Replace("xPdfField", "");
                var fieldKey = KeyController.Get(fieldName);
                PdfBox.SetupPdfBinding(fieldReplacement, GetDataDocument().GetDataDocument(), fieldKey, null);
            }
            var listFields = descendants.OfType<CollectionView>().Where((rtv) => rtv.Name.StartsWith("xCollectionField"));
            foreach (var fieldReplacement in listFields)
            {
                var fieldName = fieldReplacement.Name.Replace("xCollectionField", "");
                var fieldKey = KeyController.Get(fieldName);
                var cvm = new CollectionViewModel(this, fieldKey);
                fieldReplacement.DataContext = cvm;
            }
            var contentFields = descendants.OfType<ContentPresenter>().Where((rtv) => rtv.Name.StartsWith("xDataField"));
            foreach (var fieldReplacement in contentFields)
            {
                var fieldName = fieldReplacement.Name.Replace("xDataField", "");
                var fieldKey = KeyController.Get(fieldName);
                DataBox.BindContent(fieldReplacement, GetDataDocument().GetDataDocument(), fieldKey);
            }
            var doclistFields = descendants.OfType<ListView>().Where((rtv) => rtv.Name.StartsWith("xDocumentList"));
            foreach (var fieldReplacement in doclistFields)
            {
                var fieldName = fieldReplacement.Name.Replace("xDocumentList", "");
                var fieldKey = KeyController.Get(fieldName);
                var binding = new FieldBinding<ListController<DocumentController>>()
                {
                    Converter=new DocsToViewModelsConverter(),
                    Mode = BindingMode.OneWay,
                    Document = GetDataDocument(),
                    Key = fieldKey,
                    Tag="bind ItemSource in DocumentController",
                    CanBeNull=true
                };
                fieldReplacement.AddFieldBinding(ListView.ItemsSourceProperty, binding);
            }
            var docFields = descendants.OfType<DocumentView>().Where((rtv) => rtv.Name.StartsWith("xDocumentField"));
            foreach (var fieldReplacement in docFields)
            {
                var fieldName = fieldReplacement.Name.Replace("xDocumentField", "");
                var fieldKey = KeyController.Get(fieldName);
                fieldReplacement.DataContext = new DocumentViewModel(GetDereferencedField<DocumentController>(fieldKey,null));
            }
        }

        #endregion

        // == OVERRIDDEN from ICOLLECTION ==
        #region ICollection Overrides

        public override TypeInfo TypeInfo => TypeInfo.Document;

        public override bool TrySetValue(object value)
        {
            throw new NotImplementedException();
        }

        public override object GetValue(Context context)
        {
            return this;
        }

        public override FieldControllerBase GetDefaultController() => new DocumentController();

        public override StringSearchModel SearchForString(Search.SearchMatcher matcher)
        {
            //var positiveKeys = EnumDisplayableFields().Where(field => field.Key.SearchForString(searchString) != StringSearchModel.False).ToList();
            //var positiveVals = EnumDisplayableFields().Where(field => field.Value.SearchForString(searchString) != StringSearchModel.False).ToList();
            //if (positiveVals.Any()) return new StringSearchModel(positiveVals[0].Value.ToString()); 
            return StringSearchModel.False;
        }

        public override string ToScriptString(DocumentController thisDoc)
        {
            if (this == thisDoc)
            {
                return "this";
            }
            return DSL.GetFuncName<IdToDocumentOperator>() + $"(\"{Id}\")";
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
        /// This acts the same as <see cref="AddFieldUpdatedListener"/> except it adds a weak handler, and so should usually be used  instead if adding an event from a view
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <param name="key"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        public WeakEventListener<T, DocumentController, DocumentFieldUpdatedEventArgs>
            AddWeakFieldUpdatedListener<T>(T instance, KeyController key,
            Action<T, DocumentController, DocumentFieldUpdatedEventArgs> handler) where T : class
        {
            var weakHandler = new WeakEventListener<T, DocumentController, DocumentFieldUpdatedEventArgs>(instance)
            {
                OnEventAction = handler,
                OnDetachAction = listener => RemoveFieldUpdatedListener(key, listener.OnEvent)
            };
            AddFieldUpdatedListener(key, weakHandler.OnEvent);
            return weakHandler;
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


        static string spaces = "";

        void generateDocumentFieldUpdatedEvents(DocumentFieldUpdatedEventArgs args)
        {
            // try { Debug.WriteLine(spaces + this.Title + " -> " + args.Reference.FieldKey + " = " + args.NewValue); } catch (Exception) { }
            //TODO: If operators are added, the operator should be run, and if an operator is removed it's outputs should maybe be removed
            if (!_initialized)
            {
                return;
            }
            spaces += "  ";
            ShouldExecute(args.Reference.FieldKey, args);
            OnDocumentFieldUpdated(this, args, true);
            try
            {
                spaces = spaces.Substring(2);
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Called whenever a field on this document has been modified directly or indirectly 
        /// (by an operator, prototype, or delegate).
        /// 
        /// This then invokes the listeners added in <see cref="AddFieldUpdatedListener"/> as well as the
        /// listeners to <see cref="DocumentFieldUpdated"/>
        /// </summary>
        /// <param name="updateDelegates">whether to bubble event down to delegates</param>
        private void OnDocumentFieldUpdated(DocumentController sender, DocumentFieldUpdatedEventArgs args, bool updateDelegates)
        {
            // this invokes listeners which have been added on a per key level of granularity
            if (_fieldUpdatedDictionary.ContainsKey(args.Reference.FieldKey) )
                _fieldUpdatedDictionary[args.Reference.FieldKey]?.Invoke(sender, args);

            // this invokes listeners which have been added on a per doc level of granularity
            if (!args.Reference.FieldKey.Equals(KeyStore.DocumentContextKey))
            {
                OnFieldModelUpdated(args);
            }

            // bubbles event down to delegates
            //if (updateDelegates && !args.Reference.FieldKey.Equals(KeyStore.DelegatesKey)) //TODO TFS Can't we still use this event to let delegates know that our field was updated?
            //    PrototypeFieldUpdated?.Invoke(sender, args, c);

            // now propagate this field model change to all delegates that don't override this field
            foreach (var d in GetDelegates())
            {
                if (d.GetField(args.Reference.FieldKey, true) == null)
                    d.generateDocumentFieldUpdatedEvents(args);
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

        public void ClearBehaviors()
        {
            var fieldUpdatedBehaviors = GetField<ListController<DocumentController>>(KeyStore.FieldUpdatedOpsKey);
            if (fieldUpdatedBehaviors != null)
            {
                foreach (var behave in fieldUpdatedBehaviors)
                {
                    var ops = behave.GetField<ListController<OperatorController>>(KeyStore.OperatorKey);
                    behave.RemoveField(KeyStore.OperatorKey);
                    foreach (var operatorController in ops)
                    {
                        foreach (var operatorControllerInput in operatorController.Inputs)
                        {
                            behave.RemoveField(operatorControllerInput.Key);
                        }
                    }
                }
            }
            GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.LowPriorityOpsKey).ToList().ForEach(opDoc => MainPage.Instance.LowPriorityOps.Remove(opDoc));
            GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.ModeratePriorityOpsKey).ToList().ForEach(opDoc => MainPage.Instance.ModeratePriorityOps.Remove(opDoc));
            GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.HighPriorityOpsKey).ToList().ForEach(opDoc => MainPage.Instance.HighPriorityOps.Remove(opDoc));
            BehaviorKeys.ForEach(k => RemoveField(k));
        }
    }
}
