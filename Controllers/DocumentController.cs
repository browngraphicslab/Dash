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
using DashShared.Models;

namespace Dash
{
    public class DocumentController : IController<DocumentModel>
    {
        public bool HasDelegatesOrPrototype => HasDelegates || HasPrototype;
        
        public bool HasDelegates
        {
            get
            {
                var currentDelegates = _fields.ContainsKey(KeyStore.DelegatesKey) ? 
                    _fields[KeyStore.DelegatesKey] as DocumentCollectionFieldModelController : null;

                if (currentDelegates == null)
                    return false;
                return currentDelegates.Data.Count() > 0;
            }
        }

        
        public bool HasPrototype {
            get
            {
                return _fields.ContainsKey(KeyStore.PrototypeKey) &&
                                        (_fields[KeyStore.PrototypeKey] as DocumentFieldModelController)?.Data
                                        ?.GetField(KeyStore.AbstractInterfaceKey, true) == null;
            }
        }

        public enum FieldUpdatedAction
        {
            Add,
            Remove,
            Replace,
            Update
        }

        public class DocumentFieldUpdatedEventArgs
        {
            public readonly FieldUpdatedAction Action;
            public readonly FieldControllerBase OldValue;
            public readonly FieldControllerBase NewValue;
            public readonly DocumentFieldReference Reference;
            public readonly FieldUpdatedEventArgs FieldArgs;
            public readonly Context Context;
            public bool FromDelegate;

            public DocumentFieldUpdatedEventArgs(FieldControllerBase oldValue, FieldControllerBase newValue,
                FieldUpdatedAction action, DocumentFieldReference reference, FieldUpdatedEventArgs fieldArgs, Context context, bool fromDelegate)
            {
                Action = action;
                OldValue = oldValue;
                NewValue = newValue;
                Reference = reference;
                FieldArgs = fieldArgs;
                Context = context;
                FromDelegate = fromDelegate;
            }
        }

        public delegate void OnDocumentFieldUpdatedHandler(DocumentController sender, DocumentFieldUpdatedEventArgs args);

        /// <summary>
        /// Dictionary mapping Key's to field updated event handlers. TODO what if there is more than one DocumentFieldUpdatedEventHandler associated with a single key
        /// </summary>
        private readonly Dictionary<KeyController, OnDocumentFieldUpdatedHandler> _fieldUpdatedDictionary = new Dictionary<KeyController, OnDocumentFieldUpdatedHandler>();
        public event OnDocumentFieldUpdatedHandler DocumentFieldUpdated;
        public event OnDocumentFieldUpdatedHandler PrototypeFieldUpdated;

        public event EventHandler DocumentDeleted;

        public string Title
        {
            get
            {
                if (GetField(KeyStore.TitleKey) is TextFieldModelController)
                {
                    var textFieldModelController = GetField(KeyStore.TitleKey) as TextFieldModelController;
                    if (textFieldModelController != null)
                        return textFieldModelController.Data;
                }
                return DocumentType.Type;
            }
        }

        public bool IsConnected { get; set; }
        /// <summary>
        /// Adds a field updated listener which is only fired when the field associated with the passed in key
        /// has changed
        /// </summary>
        /// <param name="key"></param>
        /// <param name="handler"></param>
        public void AddFieldUpdatedListener(KeyController key, OnDocumentFieldUpdatedHandler handler)
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

        public void RemoveFieldUpdatedListener(KeyController key, OnDocumentFieldUpdatedHandler handler)
        {
            if (_fieldUpdatedDictionary.ContainsKey(key))
            {
                // ReSharper disable once DelegateSubtraction
                _fieldUpdatedDictionary[key] -= handler;
            }
        }

        /// <summary>
        ///     A wrapper for <see cref="Model.Fields" />. Change this to propogate changes
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
        }

        public void LoadFields()
        {
            // get the field controllers associated with the FieldModel id's stored in the document Model
            // put the field controllers in an observable dictionary
            var fields = Model.Fields.Select(kvp =>
                new KeyValuePair<KeyController, FieldControllerBase>(
                    ContentController<KeyModel>.GetController<KeyController>(kvp.Key),
                    ContentController<FieldModel>.GetController<FieldControllerBase>(kvp.Value)));

            SetFields(fields, true);
        }

        public DocumentController(IDictionary<KeyController, FieldControllerBase> fields, DocumentType type,
            string id = null, bool saveOnServer = true) : base(new DocumentModel(fields.ToDictionary(kv => kv.Key.Model, kv => kv.Value.Model), type, id))
        {
            if (saveOnServer)
            {
                SaveOnServer();
            }
            Init();
        }

        /*
        public static int threadCount = 0;
        public static object l = new object();

        public static async Task<DocumentController> CreateFromServer(DocumentModel docModel)
        {

            lock (l)
            {
                threadCount++;
                Debug.WriteLine($"enter dc : {threadCount}");
            }

            var localDocController = ContentController<DocumentModel>.GetController<DocumentController>(docModel.Id);
            if (localDocController != null)
            {
                return localDocController;
            }

            var fieldList = docModel.Fields.Values.ToArray();
            var keyList = docModel.Fields.Keys.ToArray();

            var fieldDict = new Dictionary<KeyController, FieldModelController>();

            for (var i = 0; i < docModel.Fields.Count(); i++)
            {
                var field = fieldList[i];
                var key = keyList[i];

                var keyController = new KeyController(key, sendToServer: false);

                if (keyController.Equals(KeyStore.DelegatesKey))
                    continue;
                if (keyController.Equals(KeyStore.ThisKey))
                    continue;
                if (keyController.Equals(KeyStore.LayoutListKey))
                    continue;

                var fieldController = await FieldModelController.CreateFromServer(field);

                if (keyController.Equals(KeyStore.ActiveLayoutKey))
                {
                    var brk = 1;
                }

                fieldDict.Add(keyController, fieldController);
            }

            var type = docModelDto.DocumentType;
            var id = docModelDto.Id;

            lock (l)
            {
                threadCount--;
                Debug.WriteLine($"exit dc : {threadCount}");
            }

            return new DocumentController(fieldDict, type, id, sendToServer: false);
        }*/

        /// <summary>
        ///     The <see cref="Model" /> associated with this <see cref="DocumentController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>

        public string LayoutName { get { return Model.DocumentType.Type; } }
        /// <summary>
        ///     A wrapper for <see cref="DashShared.DocumentType" />. Change this to propogate changes
        ///     to the server and across the client
        /// </summary>
        public DocumentType DocumentType
        {
            get { return Model.DocumentType; }
            set { Model.DocumentType = value; }
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
            return GetId().Equals(controller.GetId());
        }

        public DocumentController GetDataDocument(Context context)
        {
            return GetDereferencedField<DocumentFieldModelController>(KeyStore.DocumentContextKey, context)?.Data ?? this;
        }
        public override int GetHashCode()
        {
            return GetId().GetHashCode();
        }

        /// <summary>
        /// looks up a document that whose primary keys match input keys
        /// </summary>
        /// <param name="fieldContents"></param>
        /// <returns></returns>
        public static DocumentController FindDocMatchingPrimaryKeys(IEnumerable<string> primaryKeyValues)
        {
            foreach (var dmc in ContentController<DocumentModel>.GetControllers<DocumentController>())
                if (!dmc.DocumentType.Type.Contains("Box") && !dmc.DocumentType.Type.Contains("Layout"))
                {
                    var primaryKeys = dmc.GetDereferencedField(KeyStore.PrimaryKeyKey, null) as ListFieldModelController<TextFieldModelController>;
                    if (primaryKeys != null)
                    {
                        bool found = true;
                        foreach (var value in primaryKeyValues)
                        {
                            bool foundValue = false;
                            foreach (var kf in primaryKeys.Data)
                            {
                                var key = new KeyController((kf as TextFieldModelController).Data);
                                var derefValue = (dmc.GetDereferencedField(key, null) as TextFieldModelController)?.Data;
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
                return OperatorDocumentFactory.CreateOperatorDocument(new AddOperatorFieldModelController());
            if (opname == "Subtract")
                return OperatorDocumentFactory.CreateOperatorDocument(new SubtractOperatorFieldModelController());
            if (opname == "Divide")
                return OperatorDocumentFactory.CreateOperatorDocument(new DivideOperatorFieldModelController());
            if (opname == "Multiply")
                return OperatorDocumentFactory.CreateOperatorDocument(new MultiplyOperatorFieldModelController());

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
                            return new DocumentReferenceFieldController(refDoc.GetId(), e.Key); // found <DocName=a>.<FieldName=b>
                        }
            }

            foreach (var e in this.EnumFields())
                if (e.Key.Name == path[0])
                {
                    return new DocumentReferenceFieldController(refDoc.GetId(), e.Key);  // found This.<FieldName=a>
                }

            //if (searchAllDocsIfFail)
            //{
            //    var searchDoc = DBSearchOperatorFieldModelController.CreateSearch(this, DBTest.DBDoc, path[0], "");
            //    return new ReferenceFieldModelController(searchDoc.GetId(), KeyStore.CollectionOutputKey); // return  {AllDocs}.<FieldName=a> = this
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
                            SetField(key, new NumberFieldModelController(num), true, false);
                        else SetField(key, new TextFieldModelController(fieldStr), true, false);
                    }
                }
                else
                {
                    var opModel = lookupOperator(strings[0]);
                    var opFieldController = (opModel.GetField(KeyStore.OperatorKey) as OperatorFieldModelController);
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
                                    opModel.SetField(target.Key, new NumberFieldModelController(res), true);
                            }
                            else if (target.Value.Type == TypeInfo.Text)
                            {
                                opModel.SetField(target.Key, new TextFieldModelController(a), true);
                            }
                            else if (target.Value.Type == TypeInfo.Image)
                            {
                                opModel.SetField(target.Key, new ImageFieldModelController(new Uri(a)), true);
                            }
                        }
                    }
                    SetField(key, new DocumentReferenceFieldController(opModel.GetId(), opFieldController.Outputs.First().Key), true, false);
                }
            }
            else
            {
                if (curField != null && !(curField is ReferenceFieldModelController))
                {
                    if (curField is NumberFieldModelController)
                    {
                        double num;
                        if (double.TryParse(textInput, out num))
                            (curField as NumberFieldModelController).Data = num;
                        else return false;
                    }
                    else if (curField is TextFieldModelController)
                        (curField as TextFieldModelController).Data = textInput;
                    else if (curField is ImageFieldModelController)
                        ((curField as ImageFieldModelController).Data as BitmapImage).UriSource = new Uri(textInput);
                    else if (curField is DocumentFieldModelController)
                        (curField as DocumentFieldModelController).Data = new Converters.DocumentControllerToStringConverter().ConvertXamlToData(textInput);
                    else if (curField is DocumentCollectionFieldModelController)
                        (curField as DocumentCollectionFieldModelController).Data = new Converters.DocumentCollectionToStringConverter().ConvertXamlToData(textInput);
                    else return false;
                }
                else
                {
                    double num;
                    if (double.TryParse(textInput, out num))
                        SetField(key, new NumberFieldModelController(num), true);
                    else SetField(key, new TextFieldModelController(textInput), true);
                }
            }
            return true;
        }

        /// <summary>
        ///     Returns the first level of inheritance which references the passed in <see cref="KeyControllerGeneric{T}" /> or
        ///     returns null if no level of inheritance has a <see cref="FieldModelController" /> associated with the passed in
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

            // otherwise try to convert the field associated with the prototype key into a DocumentFieldModelController
            var documentFieldModelController =
                _fields[KeyStore.PrototypeKey] as DocumentFieldModelController;


            // if the field contained a DocumentFieldModelController return its data, otherwise return null
            return documentFieldModelController?.Data;
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

            if (key.Id == KeyStore.PrototypeKey.Id)
            {
                var oldPrototype = (oldField as DocumentFieldModelController)?.Data;
                if (oldPrototype != null)
                {
                    DocumentFieldUpdated -= delegate (DocumentController sender, DocumentFieldUpdatedEventArgs args) {
                                                args.FromDelegate = true;
                                                oldPrototype.OnDocumentFieldUpdated(sender, args, false);
                                            };
                }

                var prototype = (field as DocumentFieldModelController)?.Data;
                if (prototype != null)
                {
                    DocumentFieldUpdated += delegate (DocumentController sender, DocumentFieldUpdatedEventArgs args) {
                                                args.FromDelegate = true;
                                                prototype.OnDocumentFieldUpdated(sender, args, false);
                                            };
                }
            }

            // if the old and new field reference the exact same controller then we're done
            if (!ReferenceEquals(oldField, field))
            {
                if (proto.CheckCycle(key, field))
                {
                    return false;
                }

                field.SaveOnServer();
                oldField?.DisposeField();

                proto._fields[key] = field;
                proto.Model.Fields[key.Id] = field == null ? "" : field.Model.Id;

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
            OnDocumentFieldUpdated(this, new DocumentFieldUpdatedEventArgs(oldField, newField, action, reference, null, context, false), true);

            FieldControllerBase.FieldModelUpdatedHandler handler =
                delegate (FieldControllerBase sender, FieldUpdatedEventArgs args, Context c)
                {
                    var newContext = new Context(c);
                    if (newContext.DocContextList.Where((d) => d.IsDelegateOf(GetId())).Count() == 0) // don't add This if a delegate of This is already in the Context. // TODO lsm don't we get deepest delegate anyway, why would we not add it???
                        newContext.AddDocumentContext(this);
                    if (ShouldExecute(newContext, reference.FieldKey))
                    {
                        newContext = Execute(newContext, true);
                    }
                    OnDocumentFieldUpdated(this,
                        new DocumentFieldUpdatedEventArgs(null, sender, args.Action, reference, args, newContext, false), true);
                };
            if (oldField != null)
            {
                oldField.FieldModelUpdated -= handler; // TODO does this even work, isn't it removing the new reference to handler not the old one
            }
            if (newField != null)
            {
                newField.FieldModelUpdated += handler;
            }
        }

        /// <summary>
        ///     Sets the <see cref="FieldModelController" /> associated with the passed in <see cref="KeyControllerGeneric{T}" /> at the first
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
            if (key.Equals(KeyStore.PrototypeKey))
            {
                GetPrototype().PrototypeFieldUpdated -= this.OnPrototypeDocumentFieldUpdated;
                GetPrototype().PrototypeFieldUpdated += this.OnPrototypeDocumentFieldUpdated;
            }
            return fieldChanged;
        }

        private bool IsTypeCompatible(KeyController key, FieldControllerBase field)
        {
            if (!IsOperatorTypeCompatible(key, field))
                return false;
            var cont = GetField(key);
            if (cont is ReferenceFieldModelController) cont = cont.DereferenceToRoot(null);
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
            var opCont = GetField(KeyStore.OperatorKey) as OperatorFieldModelController;
            if (opCont == null) return true;
            if (!opCont.Inputs.ContainsKey(key)) return true;

            var rawField = field.DereferenceToRoot(null);
            return rawField == null || (opCont.Inputs[key].Type & rawField.TypeInfo) != 0;
        }


        /// <summary>
        ///     returns the <see cref="FieldModelController" /> for the specified <see cref="KeyControllerGeneric{T}" /> by looking first in this
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
            // search up the hiearchy starting at this for the first DocumentController which has the passed in key
            var firstProtoWithKeyOrNull = ignorePrototype ? this : GetPrototypeWithFieldKey(key);

            FieldControllerBase field = null;
            firstProtoWithKeyOrNull?._fields.TryGetValue(key, out field);
            return field;
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
                if (SetFieldHelper(field.Key, field.Value, forceMask))
                {
                    shouldSave = true;
                    shouldExecute = shouldExecute || ShouldExecute(c, field.Key);
                }
                if (field.Key.Equals(KeyStore.PrototypeKey))
                {
                    (field.Value as DocumentFieldModelController).Data.PrototypeFieldUpdated -= this.OnPrototypeDocumentFieldUpdated;
                    (field.Value as DocumentFieldModelController).Data.PrototypeFieldUpdated += this.OnPrototypeDocumentFieldUpdated;
                }
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
            var prototypeFieldController = new DocumentFieldModelController(this);
            delegateController.SetField(KeyStore.PrototypeKey, prototypeFieldController, true);

            // add the delegate to our delegates field
            var currentDelegates = GetDelegates();
            currentDelegates.AddDocument(delegateController);

            // return the now fully populated delegate
            return delegateController;
        }


        private void OnPrototypeDocumentFieldUpdated(DocumentController sender, DocumentFieldUpdatedEventArgs args)
        {
            if (_fields.ContainsKey(args.Reference.FieldKey))//This document overrides its prototypes value so its value didn't actually change
            {
                return;
            }
            if (args.Context.ContainsAncestorOf(this))
            {
                Context c = new Context(this);
                var reference = new DocumentFieldReference(GetId(), args.Reference.FieldKey);
                OnDocumentFieldUpdated(this,
                    new DocumentFieldUpdatedEventArgs(args.OldValue, args.NewValue, FieldUpdatedAction.Update, reference,
                        args.FieldArgs, c, false), true);
            }
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
            if (!(field is ReferenceFieldModelController))
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
                if (!(fm is ReferenceFieldModelController))
                {
                    continue;
                }
                var rfm = (ReferenceFieldModelController)fm;
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

            var delegates = GetField(KeyStore.DelegatesKey, true) as DocumentCollectionFieldModelController;
            if (delegates != null)
            {
                bool cycle = false;
                foreach (var documentController in delegates.Data)
                {
                    cycle = cycle || documentController.CheckCycle(key, field);
                }
                return cycle;
            }
            return false;
        }

        private List<KeyController> GetRelevantKeys(KeyController key, Context c)
        {
            var opField = GetDereferencedField(KeyStore.OperatorKey, c) as OperatorFieldModelController;
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
        public DocumentCollectionFieldModelController GetDelegates()
        {
            // see if we have a populated delegates field
            var currentDelegates = _fields.ContainsKey(KeyStore.DelegatesKey)
                ? _fields[KeyStore.DelegatesKey] as DocumentCollectionFieldModelController
                : null;

            // if not then populate it with a new list of documents
            if (currentDelegates == null)
            {
                currentDelegates =
                    new DocumentCollectionFieldModelController(new List<DocumentController>());
                SetField(KeyStore.DelegatesKey, currentDelegates, true);
            }
            return currentDelegates;
        }

        public FieldControllerBase GetDereferencedField(KeyController key, Context context)
        {
            var fieldController = GetField(key);
            return fieldController?.DereferenceToRoot(context);
        }

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
            var opField = GetDereferencedField(KeyStore.OperatorKey, context) as OperatorFieldModelController;
            if (opField != null)
                return opField.Inputs.ContainsKey(updatedKey) || opField.Outputs.ContainsKey(updatedKey);
            return false;
        }

        public Context Execute(Context oldContext, bool update)
        {
            // add this document to the context
            var context = new Context(oldContext);
            context.AddDocumentContext(this);

            // check to see if there is an operator on this document, if so it would be stored at the
            // operator key
            var opField = GetDereferencedField(KeyStore.OperatorKey, context) as OperatorFieldModelController;
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
                        foreach (var opfieldOutput in opField.Outputs)
                        {
                            context.AddData(new DocumentFieldReference(GetId(), opfieldOutput.Key), FieldControllerFactory.CreateDefaultFieldController(opfieldOutput.Value));
                        }
                        return context;
                    }
                }
                else
                {
                    inputs[opFieldInput.Key] = field;
                }
            }

            // execute the operator
            opField.Execute(inputs, outputs);

            // pass the updates along 
            // TODO comment how this works
            foreach (var fieldModel in outputs)
            {
                var reference = new DocumentFieldReference(GetId(), fieldModel.Key);
                context.AddData(reference, fieldModel.Value);
                if (update)
                {
                    OnDocumentFieldUpdated(this, new DocumentFieldUpdatedEventArgs(null, fieldModel.Value,
                        FieldUpdatedAction.Replace, reference, null, context, false), true);
                }
            }
            return context;
        }

        public IEnumerable<KeyValuePair<KeyController, FieldControllerBase>> EnumFields(bool ignorePrototype = false)
        {
            foreach (KeyValuePair<KeyController, FieldControllerBase> fieldModelController in _fields)
            {
                yield return fieldModelController;
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
        /// Generates a UI view that showcases document fields as a list of key value pairs, where key is the
        /// string key of the field and value is the rendered UI element representing the value.
        /// </summary>
        /// <returns></returns>
        private FrameworkElement makeAllViewUI(Context context, bool isInterfaceBuilder = false)
        {
            var fields = EnumFields().Where((f) => !f.Key.IsUnrenderedKey()).ToList();
            if (fields.Count > 15)
                return MakeAllViewUIForManyFields(fields);
            var panel = fields.Count() > 1 ? (Panel)new StackPanel() : new Grid();
            void Action(KeyValuePair<KeyController, FieldControllerBase> f)
            {
                f.Value.MakeAllViewUI(this, f.Key, context, panel, GetId(), isInterfaceBuilder);
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

        public FrameworkElement MakeViewUI(Context context, bool isInterfaceBuilder, Dictionary<KeyController, FrameworkElement> keysToFrameworkElementsIn = null, DocumentController dataDocument = null)
        {
            context = new Context(context);
            context.AddDocumentContext(this);
            context.AddDocumentContext(GetDataDocument(null));

            //TODO we can probably just wrap the return value in a SelectableContainer here instead of in the MakeView methods.
            if (DocumentType.Equals(TextingBox.DocumentType))
            {
                return TextingBox.MakeView(this, context, keysToFrameworkElementsIn, isInterfaceBuilder, true); //
            }
            if (DocumentType.Equals(ImageBox.DocumentType))
            {
                return ImageBox.MakeView(this, context, keysToFrameworkElementsIn, isInterfaceBuilder); //
            }
            if (DocumentType.Equals(DocumentBox.DocumentType))
            {
                return DocumentBox.MakeView(this, context, keysToFrameworkElementsIn, isInterfaceBuilder);//
            }
            if (DocumentType.Equals(KeyValueDocumentBox.DocumentType))
            {
                return KeyValueDocumentBox.MakeView(this, context, dataDocument, keysToFrameworkElementsIn, isInterfaceBuilder);//
            }
            if (DocumentType.Equals(StackLayout.DocumentType))
            {
                return StackLayout.MakeView(this, context, dataDocument, isInterfaceBuilder, keysToFrameworkElementsIn); //
            }
            if (DocumentType.Equals(WebBox.DocumentType))
            {
                return WebBox.MakeView(this, context,keysToFrameworkElementsIn, isInterfaceBuilder); //
            }
            if (DocumentType.Equals(DashConstants.TypeStore.CollectionBoxType))
            {
                return CollectionBox.MakeView(this, context, dataDocument, keysToFrameworkElementsIn, isInterfaceBuilder);//
            }
            if (DocumentType.Equals(DashConstants.TypeStore.OperatorBoxType))
            {
                return OperatorBox.MakeView(this, context, keysToFrameworkElementsIn, isInterfaceBuilder); //
            }
            if (DocumentType.Equals(DashConstants.TypeStore.FreeFormDocumentLayout))
            {
                return FreeFormDocument.MakeView(this, context, dataDocument, keysToFrameworkElementsIn, isInterfaceBuilder); //
            }
            if (DocumentType.Equals(InkBox.DocumentType))
            {
                return InkBox.MakeView(this, context, dataDocument, keysToFrameworkElementsIn, isInterfaceBuilder);
            }
            if (DocumentType.Equals(GridViewLayout.DocumentType))
            {
                return GridViewLayout.MakeView(this, context, dataDocument, keysToFrameworkElementsIn, isInterfaceBuilder); //
            }
            if (DocumentType.Equals(ListViewLayout.DocumentType))
            {
                return ListViewLayout.MakeView(this, context, dataDocument, keysToFrameworkElementsIn, isInterfaceBuilder); //
            }
            if (DocumentType.Equals(RichTextBox.DocumentType))
            {
                return RichTextBox.MakeView(this, context, keysToFrameworkElementsIn, isInterfaceBuilder); //
            }
            if (DocumentType.Equals(GridLayout.GridPanelDocumentType))
            {
                return GridLayout.MakeView(this, context, dataDocument, isInterfaceBuilder, keysToFrameworkElementsIn); //
            }
            if (DocumentType.Equals(DashConstants.TypeStore.FilterOperatorDocumentType))
            {
                return FilterOperatorBox.MakeView(this, context, keysToFrameworkElementsIn, isInterfaceBuilder); //
            }
            if (DocumentType.Equals(DashConstants.TypeStore.MapOperatorBoxType))
            {
                return CollectionMapOperatorBox.MakeView(this, context, keysToFrameworkElementsIn, isInterfaceBuilder);
            }
            if (DocumentType.Equals(DashConstants.TypeStore.MeltOperatorBoxDocumentType))
            {
                return MeltOperatorBox.MakeView(this, context, keysToFrameworkElementsIn, isInterfaceBuilder);
            }
            if (DocumentType.Equals(DashConstants.TypeStore.ExtractSentencesDocumentType))
            {
                return ExtractSentencesOperatorBox.MakeView(this, context, keysToFrameworkElementsIn, isInterfaceBuilder);
            }
            if (DocumentType.Equals(DBFilterOperatorBox.DocumentType))
            {
                return DBFilterOperatorBox.MakeView(this, context, isInterfaceBuilder);
            }
            if (DocumentType.Equals(DBSearchOperatorBox.DocumentType))
            {
                return DBSearchOperatorBox.MakeView(this, context, isInterfaceBuilder);
            }
            if (DocumentType.Equals(ApiOperatorBox.DocumentType))
            {
                return ApiOperatorBox.MakeView(this, context, keysToFrameworkElementsIn, isInterfaceBuilder); //I set the framework element as the operator view for now
            }
            if (DocumentType.Equals(PreviewDocument.PreviewDocumentType))
            {
                return PreviewDocument.MakeView(this, context, keysToFrameworkElementsIn, isInterfaceBuilder);
            }
            // if document is not a known UI View, then see if it contains a Layout view field
            var fieldModelController = GetDereferencedField(KeyStore.ActiveLayoutKey, context);
            if (fieldModelController != null)
            {
                var doc = fieldModelController.DereferenceToRoot<DocumentFieldModelController>(context);

                if (doc.Data.DocumentType.Equals(DefaultLayout.DocumentType))
                {
                    if (isInterfaceBuilder)
                    {
                        var activeLayout = this.GetActiveLayout(context).Data;
                        return new SelectableContainer(makeAllViewUI(context), activeLayout, this);
                    }
                    return makeAllViewUI(context);
                }
                Debug.Assert(doc != null);

                return doc.Data.MakeViewUI(context, isInterfaceBuilder, keysToFrameworkElementsIn, this);
            }
            if (isInterfaceBuilder)
            {
                return new SelectableContainer(makeAllViewUI(context), this, dataDocument);
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
        protected virtual void OnDocumentFieldUpdated(DocumentController sender, DocumentFieldUpdatedEventArgs args, bool updateDelegates)
        {
            // this invokes listeners which have been added on a per key level of granularity
            if (_fieldUpdatedDictionary.ContainsKey(args.Reference.FieldKey))
            {
                _fieldUpdatedDictionary[args.Reference.FieldKey]?.Invoke(sender, args);
            }

            // this invokes listeners which have been added on a per doc level of granularity
            DocumentFieldUpdated?.Invoke(sender, args);


            if (updateDelegates && !args.Reference.FieldKey.Equals(KeyStore.DelegatesKey))
            {
                PrototypeFieldUpdated?.Invoke(sender, args);
            }
        }

        public override void DeleteOnServer(Action success = null, Action<Exception> error = null)
        {
            if (_fields.ContainsKey(KeyStore.DelegatesKey))
            {
                var delegates = (DocumentCollectionFieldModelController) _fields[KeyStore.DelegatesKey];
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
    }
}