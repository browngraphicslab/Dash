using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Windows.UI;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using DashShared;
using Windows.UI.Xaml.Media.Imaging;
using Dash.Controllers;
using Dash.Controllers.Operators;
using DashShared.Models;

namespace Dash
{
    public class DocumentController : FieldModelController<DocumentModel>
    {
        public bool HasDelegatesOrPrototype => HasDelegates || HasPrototype;

        public bool HasDelegates
        {
            get
            {
                var currentDelegates = _fields.ContainsKey(KeyStore.DelegatesKey) ?
                    _fields[KeyStore.DelegatesKey] as ListController<DocumentController> : null;

                if (currentDelegates == null)
                    return false;
                return currentDelegates.Data.Any();
            }
        }
        public bool HasPrototype
        {
            get
            {
                return _fields.ContainsKey(KeyStore.PrototypeKey) &&
                                        (_fields[KeyStore.PrototypeKey] as DocumentController)
                                        ?.GetField(KeyStore.AbstractInterfaceKey, true) == null;
            }
        }


        public bool HasTitle => _fields.ContainsKey(KeyStore.TitleKey) &&
                                _fields[KeyStore.TitleKey].DereferenceToRoot<TextController>(null)?.Data != "Title";

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

        /// <summary>
        /// Dictionary mapping Key's to field updated event handlers. TODO what if there is more than one DocumentFieldUpdatedEventHandler associated with a single key
        /// </summary>
        private readonly Dictionary<KeyController, FieldUpdatedHandler> _fieldUpdatedDictionary = new Dictionary<KeyController, FieldUpdatedHandler>();
        public event FieldUpdatedHandler PrototypeFieldUpdated;

        public event EventHandler DocumentDeleted;

        public string Title
        {
            get
            {
                var titleController = GetDereferencedField<TextController>(KeyStore.TitleKey, null)?.Data ??
 GetDataDocument(null).GetDereferencedField<TextController>(KeyStore.TitleKey, null)?.Data;
                if (titleController != null)
                {
                    return titleController;
                }
                return DocumentType.Type;
            }
            set
            {
                var textFieldModelController = GetField(KeyStore.TitleKey) as TextController;
                if (textFieldModelController != null)
                    textFieldModelController.Data = value;
            }
        }

        public bool IsConnected { get; set; }
        public bool HasMatchingKey(string keyName)
        {
            if (string.IsNullOrWhiteSpace(keyName))
                return false;
            foreach (KeyController key in _fields.Keys)
            {
                if (key.Name.StartsWith("_"))
                    continue;
                if (key.Name.ToLowerInvariant().Contains(keyName.ToLowerInvariant()))
                {
                    return true;
                }
            }
            return false;
        }

        public override string ToString()
        {
            return Title;
        }


        /// <summary>
        /// Adds a field updated listener which is only fired when the field associated with the passed in key
        /// has changed
        /// </summary>
        /// <param name="key"></param>
        /// <param name="handler"></param>
        public void AddFieldUpdatedListener(KeyController key, FieldUpdatedHandler handler)
        {
            if (_fieldUpdatedDictionary.ContainsKey(key))
            {
                _fieldUpdatedDictionary[key] += handler;
            }
            else
            {
                _fieldUpdatedDictionary[key] = handler;
            }
        }

        public void RemoveFieldUpdatedListener(KeyController key, FieldUpdatedHandler handler)
        {
            if (_fieldUpdatedDictionary.ContainsKey(key))
            {
                // ReSharper disable once DelegateSubtraction
                _fieldUpdatedDictionary[key] -= handler;
            }
        }

        /// <summary>
        ///     A wrapper for <see cref="" />. Change this to propogate changes
        ///     to the server and across the client
        /// </summary>
        private Dictionary<KeyController, FieldControllerBase> _fields = new Dictionary<KeyController, FieldControllerBase>();

        public DocumentController(DocumentModel model, bool setFields = true, bool saveOnServer = true) : base(model)
        {
            if (setFields)
            {
                LoadFields();
            }

            if (saveOnServer)
            {
                SaveOnServer();
            }
        }

        public override void Init()
        {
            LoadFields();
            DocumentType = DocumentType;
        }

        public void LoadFields()
        {
            // get the field controllers associated with the FieldModel id's stored in the document Model
            // put the field controllers in an observable dictionary
            var fields = DocumentModel.Fields.Select(kvp =>
                new KeyValuePair<KeyController, FieldControllerBase>(
                    ContentController<FieldModel>.GetController<KeyController>(kvp.Key),
                    ContentController<FieldModel>.GetController<FieldControllerBase>(kvp.Value))).ToList();

            SetFields(fields, true);
        }

        public DocumentController() : this(new Dictionary<KeyController, FieldControllerBase>(), DocumentType.DefaultType) { }

        public DocumentController(IDictionary<KeyController, FieldControllerBase> fields, DocumentType type,
            string id = null, bool saveOnServer = true) : base(new DocumentModel(fields.ToDictionary(kv => kv.Key.KeyModel, kv => kv.Value.Model), type, id))
        {
            if (saveOnServer)
            {
                SaveOnServer();
            }
            Init();
        }

        /// <summary>
        ///     The <see cref="Model" /> associated with this <see cref="DocumentController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>

        public string LayoutName => DocumentModel.DocumentType.Type;

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
                //Dear future coder,
                //
                //I knew you'd eventually find this line, probably because I set 'enforceTypeCheck' to false.
                //Before you change it, remember that Types Really Only Lessen Loads in the short term.
                //
                //Enjoy your day,
                //-Tyler
                this.SetField(KeyStore.DocumentTypeKey, new TextController(value.Type), true, false);
            }
        }

        public DocumentModel DocumentModel => Model as DocumentModel;


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
            return GetId().Equals(controller.GetId());
        }

        public DocumentController GetDataDocument(Context context = null)
        {
            return GetDereferencedField<DocumentController>(KeyStore.DocumentContextKey, context) ?? this;
        }
        public override int GetHashCode()
        {
            return GetId().GetHashCode();
        }

        public override FieldModelController<DocumentModel> Copy()
        {
            return this.MakeCopy();
        }

        /// <summary>
        /// looks up a document that whose primary keys match input keys
        /// </summary>
        /// <param name="fieldContents"></param>
        /// <returns></returns>
        public static DocumentController FindDocMatchingPrimaryKeys(IEnumerable<string> primaryKeyValues)
        {
            foreach (var dmc in ContentController<FieldModel>.GetControllers<DocumentController>())
                if (!dmc.DocumentType.Type.Contains("Box") && !dmc.DocumentType.Type.Contains("Layout"))
                {
                    var primaryKeys = dmc.GetDereferencedField(KeyStore.PrimaryKeyKey, null) as ListController<KeyController>;
                    if (primaryKeys != null)
                    {
                        bool found = true;
                        foreach (var value in primaryKeyValues)
                        {
                            bool foundValue = false;
                            foreach (var key in primaryKeys.Data)
                            {
                                var derefValue = (dmc.GetDereferencedField(key as KeyController, null) as TextController)?.Data;
                                if (derefValue != null)
                                {
                                    if (value == derefValue)
                                    {
                                        foundValue = true;
                                        break;
                                    }
                                }
                            }
                            if (!foundValue)
                            {
                                found = false;
                                break;
                            }
                        }
                        if (found)
                        {
                            return dmc;
                        }
                    }
                }
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
                    return refDoc.GetField(KeyStore.ThisKey); // found <DocName=a>
                }
                else
                    foreach (var e in refDoc.EnumFields())
                        if (e.Key.Name == path[1])
                        {
                            return new DocumentReferenceController(refDoc.GetId(), e.Key); // found <DocName=a>.<FieldName=b>
                        }
            }

            foreach (var e in this.EnumFields())
                if (e.Key.Name == path[0])
                {
                    return new DocumentReferenceController(refDoc.GetId(), e.Key);  // found This.<FieldName=a>
                }

            //if (searchAllDocsIfFail)
            //{
            //    var searchDoc = DBSearchOperatorController.CreateSearch(this, DBTest.DBDoc, path[0], "");
            //    return new ReferenceController(searchDoc.GetId(), KeyStore.CollectionOutputKey); // return  {AllDocs}.<FieldName=a> = this
            //}
            return null;
        }

        /// <summary>
        /// parses text input into a field controller
        /// </summary>
        public bool ParseDocField(KeyController key, string textInput, FieldControllerBase curField = null)
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
                        }
                    }
                    SetField(key, new DocumentReferenceController(opModel.GetId(), opFieldController.Outputs.First().Key), true, false);
                }
            }
            else
            {
                if (curField != null && !(curField is ReferenceController))
                {
                    if (curField is NumberController nc)
                    {
                        double num;
                        if (double.TryParse(textInput, out num))
                            nc.Data = num;
                        else return false;
                    }
                    else if (curField is TextController tc)
                        tc.Data = textInput;
                    else if (curField is ImageController ic)
                        try
                        {
                            ic.Data = new Uri(textInput);
                        }
                        catch (Exception)
                        {
                            ic.Data = null;
                        }
                    else if (curField is DocumentController)
                    {
                        //TODO tfs: fix this
                        throw new NotImplementedException();
                        curField = new Converters.DocumentControllerToStringConverter().ConvertXamlToData(textInput);
                    }
                    else if (curField is ListController<DocumentController> lc)
                        lc.TypedData =
                            new Converters.DocumentCollectionToStringConverter().ConvertXamlToData(textInput);
                    else if (curField is RichTextController rtc)
                        rtc.Data = new RichTextModel.RTD(textInput);
                    else return false;
                }
            }
            return true;
        }

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
        /// Returns whether or not the field has changed that is associated with the passed in key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="field"></param>
        /// <param name="forceMask"></param>
        /// <returns></returns>
        private bool SetFieldHelper(KeyController key, FieldControllerBase field, bool forceMask)
        {
            // get the prototype with the desired key or just get ourself
            var proto = forceMask ? this : GetPrototypeWithFieldKey(key) ?? this;

            // get the old value of the field
            FieldControllerBase oldField;
            proto._fields.TryGetValue(key, out oldField);

            if (key.Equals(KeyStore.PrototypeKey))
            {
                var oldPrototype = oldField as DocumentController;
                if (oldPrototype != null)
                {
                    FieldModelUpdated -= delegate (FieldControllerBase sender, FieldUpdatedEventArgs args, Context c)//TODO This doesnt do anything
                    {
                        ((DocumentFieldUpdatedEventArgs)args).FromDelegate = true;
                        oldPrototype.OnDocumentFieldUpdated((DocumentController)sender, (DocumentFieldUpdatedEventArgs)args, c, false);
                    };
                    oldPrototype.PrototypeFieldUpdated -= this.OnPrototypeDocumentFieldUpdated;
                }

                var prototype = field as DocumentController;
                if (prototype != null)
                {
                    FieldModelUpdated += delegate (FieldControllerBase sender, FieldUpdatedEventArgs args, Context c)
                    {
                        ((DocumentFieldUpdatedEventArgs)args).FromDelegate = true;
                        prototype.OnDocumentFieldUpdated((DocumentController)sender, (DocumentFieldUpdatedEventArgs)args, c, false);
                    };
                    prototype.PrototypeFieldUpdated -= this.OnPrototypeDocumentFieldUpdated;
                    prototype.PrototypeFieldUpdated += this.OnPrototypeDocumentFieldUpdated;
                }
            }

            // if the old and new field reference the exact same controller then we're done
            if (!ReferenceEquals(oldField, field))
            {
                //if (proto.CheckCycle(key, field))
                //{
                //    return false;
                //}

                field.SaveOnServer();
                oldField?.DisposeField();

                proto._fields[key] = field;
                proto.DocumentModel.Fields[key.Id] = field == null ? "" : field.Model.Id;

                SetupNewFieldListeners(key, field, oldField, new Context(proto));

                return true;
            }
            return false;
        }


        /// <summary>
        /// Adds listeners to the field model updated event which fire the document model updated event
        /// </summary>
        private void SetupNewFieldListeners(KeyController key, FieldControllerBase newField, FieldControllerBase oldField, Context context)
        {
            // fire document field updated if the field has been replaced or if it did not exist before
            var action = oldField == null ? FieldUpdatedAction.Add : FieldUpdatedAction.Replace;
            var reference = new DocumentFieldReference(GetId(), key);
            OnDocumentFieldUpdated(this, new DocumentFieldUpdatedEventArgs(oldField, newField, action, reference, null, false), context, true);

            if (!key.Equals(KeyStore.PrototypeKey) && !key.Equals(KeyStore.ThisKey) && !key.Equals(KeyStore.DocumentContextKey))
            {
                FieldControllerBase.FieldUpdatedHandler handler =
                    delegate (FieldControllerBase sender, FieldUpdatedEventArgs args, Context c)
                    {
                        //var refSender = sender as ReferenceController;
                        //var proto = GetDataDocument(null). GetPrototypeWithFieldKey(reference.FieldKey) ??
                        //            this.GetPrototypeWithFieldKey(reference.FieldKey);
                        //if (!new Context(proto).IsCompatibleWith(c) &&
                        //    GetDataDocument(null).GetId() != refSender?.GetDocumentId(null)) 
                        //{
                        //    return;
                        //}

                        var newContext = new Context(c);
                        if (newContext.DocContextList.Count(d => d.IsDelegateOf(GetId())) == 0)
                        // don't add This if a delegate of This is already in the Context. // TODO lsm don't we get deepest delegate anyway, why would we not add it???
                            newContext.AddDocumentContext(this);
                        var updateArgs = new DocumentFieldUpdatedEventArgs(null, sender, FieldUpdatedAction.Update,
                            reference, args, false);
                        if (ShouldExecute(newContext, reference.FieldKey))
                        {
                            newContext = Execute(newContext, true, updateArgs);
                        }
                        OnDocumentFieldUpdated(this,
                            updateArgs,
                            newContext, true);
                    };
                if (oldField != null)
                {
                    oldField.FieldModelUpdated -=
                        handler; // TODO does this even work, isn't it removing the new reference to handler not the old one (Yes it is, this needs to be fixed)
                }
                if (newField != null)
                {
                    newField.FieldModelUpdated += handler;
                }
            }
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
        public bool SetField(KeyController key, FieldControllerBase field, bool forceMask, bool enforceTypeCheck = true)
        {
            // TODO check field type compatibility
            var context = new Context(this);
            var shouldExecute = false;
            var fieldChanged = false;
            // ReSharper disable once AssignmentInConditionalExpression
            if (fieldChanged = SetFieldHelper(key, field, forceMask))
            {
                shouldExecute = ShouldExecute(context, key);
                UpdateOnServer();
                // TODO either notify the delegates here, or notify the delegates in the FieldsOnCollectionChanged method
                //proto.notifyDelegates(new ReferenceFieldModel(Id, key));
            }
            if (shouldExecute)
            {
                Execute(context, true);
            }

            return fieldChanged;
        }

        /// <summary>
        /// Removes the field mapped to by <paramref name="key"/> from the document
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool RemoveField(KeyController key)
        {
            var proto = GetPrototypeWithFieldKey(key);

            if (proto._fields.ContainsKey(key))
            {
                return false;
            }

            //TODO Remove fieldUpdated listener
            //var field = proto._fields[key];
            //field.FieldModelUpdated -= 

            return proto._fields.Remove(key);
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
        /// Method that returns whether the input fieldmodelcontroller type is compatible to the key; if the document is not an operator type, return true always 
        /// </summary>
        /// <param name="key">key that field is mapped to</param>
        /// <param name="field">reference field model that references the field to connect</param>
        private bool IsOperatorTypeCompatible(KeyController key, FieldControllerBase field)
        {
            var opCont = GetField(KeyStore.OperatorKey) as OperatorController;
            if (opCont == null) return true;
            if (!opCont.Inputs.ContainsKey(key)) return true;

            var rawField = field.DereferenceToRoot(null);
            return rawField == null || (opCont.Inputs[key].Type & rawField.TypeInfo) != 0;
        }


        /// <summary>
        ///     returns the <see cref="Controller" /> for the specified <see cref="KeyControllerGeneric{T}" /> by looking first in this
        ///     <see cref="DocumentController" />
        ///     and then sequentially up the hierarchy of prototypes of this <see cref="DocumentController" />. If the
        ///     key is not found then it returns null.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="context"></param>
        /// <param name="ignorePrototype"></param>
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

        public TypeInfo GetFieldType(KeyController key)
        {
            var operatorController = GetField<OperatorController>(key);
            if (operatorController != null && operatorController.Outputs.ContainsKey(key))
            {
                return operatorController.Outputs[key];
            }

            return GetField(key)?.TypeInfo ?? TypeInfo.Any;
        }

        public TypeInfo GetRootFieldType(KeyController key)
        {
            var operatorController = GetField<OperatorController>(KeyStore.OperatorKey);
            if (operatorController != null && operatorController.Outputs.ContainsKey(key))
            {
                return operatorController.Outputs[key];
            }

            return GetField(key)?.RootTypeInfo ?? TypeInfo.Any;
        }

        /// <summary>
        ///     Sets all of the document's fields to a given Dictionary of Key FieldModel
        ///     pairs. If <paramref name="forceMask" /> is true, all the fields are set on this <see cref="DocumentController" />
        ///     otherwise each
        ///     field is written on the first prototype in the hierarchy which contains it
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="forceMask"></param>
        public bool SetFields(IEnumerable<KeyValuePair<KeyController, FieldControllerBase>> fields, bool forceMask)
        {
            Context c = new Context(this);
            bool shouldExecute = false;
            bool shouldSave = false;

            var array = fields.ToArray();

            foreach (var field in array)
            {
                if (field.Key != null && SetFieldHelper(field.Key, field.Value, forceMask))
                {
                    shouldSave = true;
                    shouldExecute = shouldExecute || ShouldExecute(c, field.Key);
                }
                //TODO tfs: This shouldn't be necessary, it should get handled in SetFieldHelper
                //if (field.Key.Equals(KeyStore.PrototypeKey))
                //{
                //    (field.Value as DocumentController).PrototypeFieldUpdated -= this.OnPrototypeDocumentFieldUpdated;
                //    (field.Value as DocumentController).PrototypeFieldUpdated += this.OnPrototypeDocumentFieldUpdated;
                //}
            }

            if (shouldExecute)
            {
                Execute(c, true);
            }

            if (shouldSave)
            {
                UpdateOnServer();
            }

            return shouldExecute;
        }


        /// <summary>
        ///     Creates a delegate (child) of the given document that inherits all the fields of the prototype (parent)
        /// </summary>
        /// <returns></returns>
        public DocumentController MakeDelegate()
        {
            var delegateModel = new DocumentModel(new Dictionary<KeyModel, FieldModel>(), DocumentType, "delegate-of-" + GetId() + "-" + Guid.NewGuid());

            // create a controller for the child
            var delegateController = new DocumentController(delegateModel);

            //delegateController = new DocumentController(new Dictionary<KeyController, FieldControllerBase>(), DocumentType);

            // create and set a prototype field on the child, pointing to ourself
            var prototypeFieldController = this;
            delegateController.SetField(KeyStore.PrototypeKey, prototypeFieldController, true);

            // add the delegate to our delegates field
            var currentDelegates = GetDelegates();
            currentDelegates.Add(delegateController);

            // return the now fully populated delegate
            return delegateController;
        }


        private void OnPrototypeDocumentFieldUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            var dargs = (DocumentFieldUpdatedEventArgs)args;
            if (_fields.ContainsKey(dargs.Reference.FieldKey))//This document overrides its prototypes value so its value didn't actually change
            {
                return;
            }
         //   if (context.ContainsAncestorOf(this)) //TODO tfs: what was this doing and why do we need to comment it out?
            {
                Context c = new Context(this);
                var reference = new DocumentFieldReference(GetId(), dargs.Reference.FieldKey);
                OnDocumentFieldUpdated(this,
                    new DocumentFieldUpdatedEventArgs(dargs.OldValue, dargs.NewValue, FieldUpdatedAction.Update,
                        reference,
                        dargs.FieldArgs, false), c, true);
            }
            //else
            //{
            //    Debug.WriteLine("");
            //}
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
            visitedFields.Add(new DocumentFieldReference(GetId(), key));
            var rfms = new Queue<Tuple<FieldControllerBase, Context>>();

            //TODO If this is a DocPointerFieldReference this might not work
            rfms.Enqueue(Tuple.Create(field, new Context(this)));

            while (rfms.Count > 0)
            {
                var t = rfms.Dequeue();
                var fm = t.Item1;
                var c = t.Item2;
                if (!(fm is ReferenceController))
                {
                    continue;
                }
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
                {
                    if (fieldReference.Resolve(c2).Equals(fieldRef))
                    {
                        return true;
                    }
                }
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

        private List<KeyController> GetRelevantKeys(KeyController key, Context c)
        {
            var opField = GetDereferencedField(KeyStore.OperatorKey, c) as OperatorController;
            if (opField == null)
            {
                return new List<KeyController> { key };
            }
            return new List<KeyController>(opField.Inputs.Keys);
        }

        /// <summary>
        /// Returns true if the document with the passed in id is a prototype 
        /// of this document. Searches up the entire hierarchy recursively
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool IsDelegateOf(string id)
        {
            var proto = GetPrototype();
            if (proto == null) return false;
            return proto.GetId() == id || proto.IsDelegateOf(id);
        }


        /// <summary>
        ///     Gets the delegates for this <see cref="DocumentController" /> or creates a delegates field
        ///     and returns it if no delegates field existed
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

        public FieldControllerBase GetDereferencedField(KeyController key, Context context)
        {
            // TODO this should cause an operator to execute and return the proper value
            var fieldController = GetField(key);
            context = new Context(context); //  context ?? new Context();  // bcz: THIS SHOULD BE SCRUTINIZED.  I don't think it's ever correct for a function to modify the context that's passed in.
            context.AddDocumentContext(this);
            return fieldController?.DereferenceToRoot(context ?? new Context(this));
        }

        // TODO this should cause an operator to execute and return the proper value
        public T GetDereferencedField<T>(KeyController key, Context context) where T : FieldControllerBase
        {
            return GetDereferencedField(key, context) as T;
        }

        /// <summary>
        /// Returns whether or not the current document should execute.
        /// <para>
        /// Documents should execute if all the following are true
        ///     1. they are an operator
        ///     2. the input contains the updated key or the output contains the updated key
        /// </para>
        /// </summary>
        /// <param name="context"></param>
        /// <param name="updatedKey"></param>
        /// <returns></returns>
        public bool ShouldExecute(Context context, KeyController updatedKey)
        {
            context = context ?? new Context(this);
            var opField = GetDereferencedField<OperatorController>(KeyStore.OperatorKey, context);
            if (opField != null)
                return opField.Inputs.ContainsKey(updatedKey) || opField.Outputs.ContainsKey(updatedKey);
            return false;
        }

        public Context Execute(Context oldContext, bool update, FieldUpdatedEventArgs updatedArgs = null)
        {
            // add this document to the context
            var context = new Context(oldContext);
            context.AddDocumentContext(this);

            // check to see if there is an operator on this document, if so it would be stored at the
            // operator key
            var opField = GetDereferencedField(KeyStore.OperatorKey, context) as OperatorController;
            if (opField == null)
            {
                return context; // no operator so we're done
            }

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

            bool needsToExecute = updatedArgs != null;
            var id = inputs.Values.Select(f => f.Id).Aggregate(0, (sum, next) => sum + next.GetHashCode());
            var key = new KeyController(DashShared.UtilShared.GetDeterministicGuid(id.ToString()),
                "_Cache Access Key");

            //TODO We should get rid of old cache values that aren't necessary at some point
            var cache = GetFieldOrCreateDefault<DocumentController>(KeyStore.OperatorCacheKey);
            if (updatedArgs == null)
            {
                foreach (var opFieldOutput in opField.Outputs)
                {
                    var field = cache.GetFieldOrCreateDefault<DocumentController>(opFieldOutput.Key)?.GetField(key);
                    if (field == null)
                    {
                        needsToExecute = true;
                        outputs.Clear();
                        break;
                    }
                    else
                    {
                        outputs[opFieldOutput.Key] = field;
                    }
                }
            }


            if (needsToExecute)
            {
                // execute the operator
                opField.Execute(inputs, outputs, updatedArgs);
            }

            // pass the updates along 
            // TODO comment how this works
            foreach (var fieldModel in outputs)
            {
                if (needsToExecute)
                {
                    cache.GetFieldOrCreateDefault<DocumentController>(fieldModel.Key)
                        .SetField(key, fieldModel.Value, true);
                }
                var reference = new DocumentFieldReference(GetId(), fieldModel.Key);
                context.AddData(reference, fieldModel.Value);
                if (update)
                {
                    OnDocumentFieldUpdated(this, new DocumentFieldUpdatedEventArgs(null, fieldModel.Value,
                        FieldUpdatedAction.Replace, reference, null, false), context, true);
                }
            }
            return context;
        }

        public IEnumerable<KeyValuePair<KeyController, FieldControllerBase>> EnumFields(bool ignorePrototype = false)
        {
            foreach (KeyValuePair<KeyController, FieldControllerBase> keyFieldPair in _fields)
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

        public IEnumerable<KeyValuePair<KeyController, FieldControllerBase>> EnumDisplayableFields(bool ignorePrototype = false)
        {
            foreach (KeyValuePair<KeyController, FieldControllerBase> keyFieldPair in _fields)
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
                f.Value.MakeAllViewUI(this, f.Key, context, panel, GetId());
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

        public FrameworkElement MakeViewUI(Context context, DocumentController dataDocument = null)
        {
            context = new Context(context);
            context.AddDocumentContext(this);
            context.AddDocumentContext(GetDataDocument(null));

            // if document is not a known UI View, then see if it contains a Layout view field
            var fieldModelController = GetDereferencedField(KeyStore.ActiveLayoutKey, context);
            if (fieldModelController != null)
            {
                var doc = fieldModelController.DereferenceToRoot<DocumentController>(context);

                if (doc.DocumentType.Equals(DefaultLayout.DocumentType))
                {
                    return makeAllViewUI(context);
                }
                Debug.Assert(doc != null);

                return doc.MakeViewUI(context, GetDataDocument());
            }
            //TODO we can probably just wrap the return value in a SelectableContainer here instead of in the MakeView methods.
            if (DocumentType.Equals(TextingBox.DocumentType))
            {
                return TextingBox.MakeView(this, context); //
            }
            if (DocumentType.Equals(ImageBox.DocumentType))
            {
                return ImageBox.MakeView(this, context); //
            }
            if (DocumentType.Equals(PdfBox.DocumentType))
            {
                return PdfBox.MakeView(this, context);
            }
            if (DocumentType.Equals(KeyValueDocumentBox.DocumentType))
            {
                return KeyValueDocumentBox.MakeView(this, context, dataDocument);//
            }
            if (DocumentType.Equals(StackLayout.DocumentType))
            {
                return StackLayout.MakeView(this, context, dataDocument); //
            }
            if (DocumentType.Equals(WebBox.DocumentType))
            {
                return WebBox.MakeView(this, context); //
            }
            if (DocumentType.Equals(DashConstants.TypeStore.CollectionBoxType))
            {
                return CollectionBox.MakeView(this, context, dataDocument);//
            }
            if (DocumentType.Equals(DashConstants.TypeStore.OperatorBoxType))
            {
                return OperatorBox.MakeView(this, context); //
            }
            if (DocumentType.Equals(DashConstants.TypeStore.FreeFormDocumentLayout))
            {
                return FreeFormDocument.MakeView(this, context, dataDocument); //
            }
            if (DocumentType.Equals(InkBox.DocumentType))
            {
                return InkBox.MakeView(this, context, dataDocument);
            }
            if (DocumentType.Equals(GridViewLayout.DocumentType))
            {
                return GridViewLayout.MakeView(this, context, dataDocument); //
            }
            if (DocumentType.Equals(ListViewLayout.DocumentType))
            {
                return ListViewLayout.MakeView(this, context, dataDocument); //
            }
            if (DocumentType.Equals(ExecuteHtmlOperatorBox.DocumentType))
            {
                return ExecuteHtmlOperatorBox.MakeView(this, context); //
            }
            if (DocumentType.Equals(RichTextBox.DocumentType))
            {
                return RichTextBox.MakeView(this, context); //
            }
            if (DocumentType.Equals(GridLayout.GridPanelDocumentType))
            {
                return GridLayout.MakeView(this, context, dataDocument); //
            }
            if (DocumentType.Equals(DashConstants.TypeStore.MeltOperatorBoxDocumentType))
            {
                return MeltOperatorBox.MakeView(this, context);
            }
            if (DocumentType.Equals(DashConstants.TypeStore.QuizletOperatorType))
            {
                return QuizletOperatorBox.MakeView(this, context);
            }
            if (DocumentType.Equals(DashConstants.TypeStore.ExtractSentencesDocumentType))
            {
                return ExtractSentencesOperatorBox.MakeView(this, context);
            }
            if (DocumentType.Equals(DashConstants.TypeStore.SearchOperatorType))
            {
                return SearchOperatorBox.MakeView(this, context);
            }
            if (DocumentType.Equals(DBSearchOperatorBox.DocumentType))
            {
                return DBSearchOperatorBox.MakeView(this, context);
            }
            if (DocumentType.Equals(ApiOperatorBox.DocumentType))
            {
                return ApiOperatorBox.MakeView(this, context); //I set the framework element as the operator view for now
            }
            if (DocumentType.Equals(PreviewDocument.PreviewDocumentType))
            {
                return PreviewDocument.MakeView(this, context);
            }
            if (DocumentType.Equals(BackgroundBox.DocumentType))
            {
                return BackgroundBox.MakeView(this, context);
            }
            if (DocumentType.Equals(DataBox.DocumentType))
            {
                return DataBox.MakeView(this, context);
            }
            return makeAllViewUI(context);
        }

        /// <summary>
        /// Invokes the listeners added in <see cref="AddFieldUpdatedListener"/> as well as the
        /// listeners to <see cref="DocumentFieldUpdated"/>
        /// </summary>
        /// <param name="sender">The <see cref="DocumentController"/> which is being updated</param>
        /// <param name="args">Represents the behavior of the update</param>
        /// <param name="updateDelegates"></param>
        protected virtual void OnDocumentFieldUpdated(DocumentController sender, DocumentFieldUpdatedEventArgs args, Context c, bool updateDelegates)
        {
            // this invokes listeners which have been added on a per key level of granularity
            if (_fieldUpdatedDictionary.ContainsKey(args.Reference.FieldKey))
            {
                _fieldUpdatedDictionary[args.Reference.FieldKey]?.Invoke(sender, args, c);
            }
            if (!args.Reference.FieldKey.Equals(KeyStore.DocumentContextKey))
            {
                // this invokes listeners which have been added on a per doc level of granularity
                OnFieldModelUpdated(args, c);
            }

            if (updateDelegates && !args.Reference.FieldKey.Equals(KeyStore.DelegatesKey))
            {
                PrototypeFieldUpdated?.Invoke(sender, args, c);
            }
        }

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
        public override bool SetValue(object value)
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
    }
}