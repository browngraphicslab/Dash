using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using DashShared;
using Windows.UI.Xaml.Media.Imaging;

namespace Dash
{
    public class DocumentController : ViewModelBase, IController
    {
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
            public readonly FieldModelController OldValue;
            public readonly FieldModelController NewValue;
            public readonly DocumentFieldReference Reference;
            public readonly FieldUpdatedEventArgs FieldArgs;
            public readonly Context Context;
            public bool FromDelegate;

            public DocumentFieldUpdatedEventArgs(FieldModelController oldValue, FieldModelController newValue,
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

        private readonly Dictionary<KeyController, OnDocumentFieldUpdatedHandler> _fieldUpdatedDictionary = new Dictionary<KeyController, OnDocumentFieldUpdatedHandler>();
        public event OnDocumentFieldUpdatedHandler DocumentFieldUpdated;
        public event OnDocumentFieldUpdatedHandler PrototypeFieldUpdated;

        public void AddFieldUpdatedListener(KeyController key, OnDocumentFieldUpdatedHandler handler)
        {
            //++totalCount;
            //if (++addCount % 100 == 0)
            //{
            //    Debug.WriteLine($"Add          Add: {addCount}, Remove: {removeCount}, Total: {totalCount}, {addCount - removeCount}");
            //}
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
            //--totalCount;
            //if (++removeCount % 100 == 0)
            //{
            //    Debug.WriteLine($"Remove       Add: {addCount}, Remove: {removeCount}, Total: {totalCount}, {addCount - removeCount}");
            //}
            if (_fieldUpdatedDictionary.ContainsKey(key))
            {
                // ReSharper disable once DelegateSubtraction
                _fieldUpdatedDictionary[key] -= handler;
            }
        }

        /// <summary>
        ///     A wrapper for <see cref="DocumentModel.Fields" />. Change this to propogate changes
        ///     to the server and across the client
        /// </summary>
        private Dictionary<KeyController, FieldModelController> _fields = new Dictionary<KeyController, FieldModelController>();


        public DocumentController(IDictionary<KeyController, FieldModelController> fields, DocumentType type, string id = null)
        {
            DocumentModel model =
                new DocumentModel(fields.ToDictionary(kv => kv.Key, kv => kv.Value.FieldModel), type, id);
            ContentController.AddModel(model);
            // Initialize Local Variables
            DocumentModel = model;
            // get the field controllers associated with the FieldModel id's stored in the document Model
            // put the field controllers in an observable dictionary
            ContentController.AddController(this);
            foreach (var fieldModelController in fields)
            {
                SetField(fieldModelController.Key, fieldModelController.Value, true);
            }

            LayoutName = model.DocumentType.Type;
            // Add Events
        }


        /// <summary>
        ///     The <see cref="DocumentModel" /> associated with this <see cref="DocumentController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public DocumentModel DocumentModel { get; }

        public string LayoutName { get; set; }
        /// <summary>
        ///     A wrapper for <see cref="Dash.DocumentModel.DocumentType" />. Change this to propogate changes
        ///     to the server and across the client
        /// </summary>
        public DocumentType DocumentType
        {
            get { return DocumentModel.DocumentType; }
            set
            {
                if (SetProperty(ref DocumentModel.DocumentType, value))
                {
                    // update local
                    // update server  
                }
            }
        }


        /// <summary>
        ///     Returns the <see cref="Dash.DocumentModel.Id" /> for the document which this controller encapsulates
        /// </summary>
        public string GetId()
        {
            return DocumentModel.Id;
        }


        public override bool Equals(object obj)
        {
            if (obj == this)
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
            foreach (var dmc in ContentController.GetControllers<DocumentController>())
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
                return OperatorDocumentModel.CreateOperatorDocumentModel(new AddOperatorModelController(new OperatorFieldModel("Add")));
            if (opname == "Divide")
                return OperatorDocumentModel.CreateOperatorDocumentModel(new DivideOperatorFieldModelController());
            return null;
        }

        public FieldModelController ParseDocumentReference(string textInput, bool searchAllDocsIfFail)
        {
            var path = textInput.Trim(' ').Split('.');  // input has format <a>[.<b>]

            var docName = path[0];                       //search for <DocName=a>[.<FieldName=b>]
            var fieldName = (path.Count() > 1 ? path[1] : "");
            var refDoc = docName == "Proto" ? GetPrototype() : docName == "This" ? this : FindDocMatchingPrimaryKeys(new List<string>(new string[] { path[0] }));
            if (refDoc != null)
            {
                if (path.Count() == 1)
                {
                    return refDoc.GetField(KeyStore.ThisKey);  // found <DocName=a>
                }
                else
                    foreach (var e in refDoc.EnumFields())
                        if (e.Key.Name == path[1])
                        {
                            return new ReferenceFieldModelController(refDoc.GetId(), e.Key); // found <DocName=a>.<FieldName=b>
                        }
            }

            foreach (var e in this.EnumFields())
                if (e.Key.Name == path[0])
                {
                    return new ReferenceFieldModelController(refDoc.GetId(), e.Key);  // found This.<FieldName=a>
                }

            //if (searchAllDocsIfFail)
            //{
            //    var searchDoc = DBSearchOperatorFieldModelController.CreateSearch(this, DBTest.DBDoc, path[0], "");
            //    return new ReferenceFieldModelController(searchDoc.GetId(), DBSearchOperatorFieldModelController.ResultsKey); // return  {AllDocs}.<FieldName=a> = this
            //}
            return null;
        }

        /// <summary>
        /// parses text input into a field controller
        /// </summary>
        /// <param name="docController"></param>
        /// <param name="key"></param>
        /// <param name="textInput"></param>
        public bool ParseDocField(KeyController key, string textInput, FieldModelController curField = null)
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
                    var opFieldController = (opModel.GetField(OperatorDocumentModel.OperatorKey) as OperatorFieldModelController);
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
                    SetField(key, new ReferenceFieldModelController(opModel.GetId(), opFieldController.Outputs.First().Key), true, false);
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
        ///     Returns the first level of inheritance which references the passed in <see cref="KeyController" /> or
        ///     returns null if no level of inheritance has a <see cref="FieldModelController" /> associated with the passed in
        ///     <see cref="KeyController" />
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
        ///     Tries to get the Prototype of this <see cref="DocumentController" /> and associated <see cref="DocumentModel" />,
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

        private bool SetFieldHelper(KeyController key, FieldModelController field, bool forceMask, out FieldModelController replacedField)
        {
            var proto = forceMask ? this : GetPrototypeWithFieldKey(key) ?? this;

            FieldModelController oldField;
            proto._fields.TryGetValue(key, out oldField);

            // if the fields are reference equal just return
            if (ReferenceEquals(oldField, field))
            {
                replacedField = null;
                return false;
            }
            oldField?.Dispose();

            proto._fields[key] = field;
            proto.DocumentModel.Fields[key] = field == null ? "" : field.FieldModel.Id;

            replacedField = oldField;
            return true;
        }

        private void SetupNewFieldListeners(KeyController key, FieldModelController newField, FieldModelController oldField, Context context)
        {
            FieldUpdatedAction action = oldField == null ? FieldUpdatedAction.Add : FieldUpdatedAction.Replace;
            var reference = new DocumentFieldReference(GetId(), key);
            OnDocumentFieldUpdated(this, new DocumentFieldUpdatedEventArgs(oldField, newField, action, reference, null, context, false), true);
            FieldModelController.FieldModelUpdatedHandler handler =
                delegate (FieldModelController sender, FieldUpdatedEventArgs args, Context c)
                {
                    var newContext = new Context(c);
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
                oldField.FieldModelUpdated -= handler;
            }
            if (newField != null)
            {
                newField.FieldModelUpdated += handler;
            }
        }

        /// <summary>
        ///     Sets the <see cref="FieldModelController" /> associated with the passed in <see cref="KeyController" /> at the first
        ///     prototype in the hierarchy that contains it. If the <see cref="KeyController" /> is not used at any level then it is
        ///     created in this <see cref="DocumentController" />.
        ///     <para>
        ///         If <paramref name="forceMask" /> is set to true, then we never search for a prototype and simply override
        ///         any prototype that might exist by setting the field on this
        ///     </para>
        /// </summary>
        /// <param name="key">key index of field to update</param>
        /// <param name="field">FieldModel to update to</param>
        /// <param name="forceMask"></param>
        public bool SetField(KeyController key, FieldModelController field, bool forceMask, bool enforceTypeCheck = true)
        {
            // check field type compatibility
            if (enforceTypeCheck && !IsTypeCompatible(key, field)) return false;

            FieldModelController oldField;
            if (!SetFieldHelper(key, field, forceMask, out oldField))
            {
                return false;
            }

            // check field type compatibility if operator  
            //if (!IsTypeCompatible(key, field)) return false;

            SetupNewFieldListeners(key, field, oldField, new Context(this));

            Context c = new Context(this);
            if (ShouldExecute(c, key))
            {
                Execute(c, true);
            }
            // TODO either notify the delegates here, or notify the delegates in the FieldsOnCollectionChanged method
            //proto.notifyDelegates(new ReferenceFieldModel(Id, key));
            return true;
        }

        private bool IsTypeCompatible(KeyController key, FieldModelController field)
        {
            if (!IsOperatorTypeCompatible(key, field))
                return false;
            var cont = GetField(key);
            if (cont is ReferenceFieldModelController) cont = cont.DereferenceToRoot(null);
            if (cont == null) return true;
            var rawField = field.DereferenceToRoot(null);

            return cont.TypeInfo == TypeInfo.Reference || cont.TypeInfo == rawField.TypeInfo;
        }

        /// <summary>
        /// Method that returns whether the input fieldmodelcontroller type is compatible to the key; if the document is not an operator type, return true always 
        /// </summary>
        /// <param name="key">key that field is mapped to</param>
        /// <param name="field">reference field model that references the field to connect</param>
        private bool IsOperatorTypeCompatible(KeyController key, FieldModelController field)
        {
            var opCont = GetField(OperatorDocumentModel.OperatorKey) as OperatorFieldModelController;
            if (opCont == null) return true;
            if (!opCont.Inputs.ContainsKey(key)) return true;

            var rawField = field.DereferenceToRoot(null);
            return rawField == null || (opCont.Inputs[key].Type & rawField.TypeInfo) != 0;
        }


        /// <summary>
        ///     returns the <see cref="FieldModelController" /> for the specified <see cref="KeyController" /> by looking first in this
        ///     <see cref="DocumentController" />
        ///     and then sequentially up the hierarchy of prototypes of this <see cref="DocumentController" />. If the
        ///     key is not found then it returns null.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="context"></param>
        /// <param name="ignorePrototype"></param>
        /// <returns></returns>
        public FieldModelController GetField(KeyController key, bool ignorePrototype = false)
        {
            // search up the hiearchy starting at this for the first DocumentController which has the passed in key
            var firstProtoWithKeyOrNull = ignorePrototype ? this : GetPrototypeWithFieldKey(key);

            FieldModelController field = null;
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
        public void SetFields(IDictionary<KeyController, FieldModelController> fields, bool forceMask)
        {
            var oldFields = new List<Tuple<FieldModelController, FieldModelController, KeyController>>();

            Context c = new Context(this);
            bool shouldExecute = false;
            foreach (var field in fields)
            {
                FieldModelController oldField;
                if (SetFieldHelper(field.Key, field.Value, forceMask, out oldField))
                {
                    shouldExecute = shouldExecute || ShouldExecute(c, field.Key);
                    oldFields.Add(Tuple.Create(field.Value, oldField, field.Key));
                }
            }

            foreach (var f in oldFields)
            {
                SetupNewFieldListeners(f.Item3, f.Item1, f.Item2, c);
            }

            if (shouldExecute)
            {
                Execute(c, true);
            }
        }


        /// <summary>
        ///     Creates a delegate (child) of the given document that inherits all the fields of the prototype (parent)
        /// </summary>
        /// <returns></returns>
        public DocumentController MakeDelegate()
        {
            // create a controller for the child
            var delegateController = new DocumentController(new Dictionary<KeyController, FieldModelController>(), DocumentType);
            delegateController.DocumentFieldUpdated +=
                delegate (DocumentController sender, DocumentFieldUpdatedEventArgs args)
                {
                    args.FromDelegate = true;
                    OnDocumentFieldUpdated(sender, args, false);
                };
            PrototypeFieldUpdated += delegateController.OnPrototypeDocumentFieldUpdated;

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
            if (args.Context.IsApplicableTo(this)) 
            {
                Context c = new Context(this);
                var reference = new DocumentFieldReference(GetId(), args.Reference.FieldKey);
                OnDocumentFieldUpdated(this,
                    new DocumentFieldUpdatedEventArgs(args.OldValue, args.NewValue, FieldUpdatedAction.Add, reference,
                        args.FieldArgs, c, false), true);
            }
        }


        public bool IsDelegateOf(string id)
        {
            var proto = GetPrototype();
            if (proto != null)
                if (proto.GetId() == id)
                    return true;
                else return proto.IsDelegateOf(id);
            return false;
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

        public FieldModelController GetDereferencedField(KeyController key, Context context)
        {
            var fieldController = GetField(key);
            return fieldController?.DereferenceToRoot(context);
        }

        public T GetDereferencedField<T>(KeyController key, Context context) where T : FieldModelController
        {
            return GetDereferencedField(key, context) as T;
        }


        public bool ShouldExecute(Context context, KeyController updatedKey)
        {
            context = context ?? new Context(this);
            var opField = GetDereferencedField(OperatorDocumentModel.OperatorKey, context) as OperatorFieldModelController;
            if (opField == null)
            {
                return false;
            }
            if (opField.Inputs.ContainsKey(updatedKey))
            {
                return true;
            }
            if (opField.Outputs.ContainsKey(updatedKey))
            {
                return true;
            }
            return false;
        }

        public Context Execute(Context oldContext, bool update)
        {
            var context = new Context(oldContext);
            context.AddDocumentContext(this);
            var opField = GetDereferencedField(OperatorDocumentModel.OperatorKey, context) as OperatorFieldModelController;
            if (opField == null)
            {
                return context;
            }
            var inputs = new Dictionary<KeyController, FieldModelController>(opField.Inputs.Count);
            var outputs = new Dictionary<KeyController, FieldModelController>(opField.Outputs.Count);
            foreach (var opFieldInput in opField.Inputs)
            {
                var field = GetField(opFieldInput.Key);
                field = field?.DereferenceToRoot(context);
                if (field == null)
                {
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
            opField.Execute(inputs, outputs);
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

        public IEnumerable<KeyValuePair<KeyController, FieldModelController>> EnumFields(bool ignorePrototype = false)
        {
            foreach (KeyValuePair<KeyController, FieldModelController> fieldModelController in _fields)
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
        private FrameworkElement makeAllViewUI(Context context)
        {
            var fields = EnumFields().ToList();
            if (fields.Count > 15)
            {
                TextBlock block = new TextBlock
                {
                    Text = DocumentType.Type,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                return block;
            }

            //var sp = new ListView { SelectionMode = ListViewSelectionMode.None };
            var sp = new StackPanel();
            var source = new List<FrameworkElement>();

            var isInterfaceBuilder = false; // TODO make this a parameter

            Action<KeyValuePair<KeyController, FieldModelController>> a =
                delegate(KeyValuePair<KeyController, FieldModelController> f)
                {
                    if (f.Key.Equals(KeyStore.DelegatesKey) ||
                        f.Key.Equals(KeyStore.PrototypeKey) ||
                        f.Key.Equals(KeyStore.LayoutListKey) ||
                        f.Key.Equals(KeyStore.ActiveLayoutKey) ||
                        f.Key.Equals(KeyStore.IconTypeFieldKey))
                    {
                        return;
                    }

                    if (f.Value is ImageFieldModelController || f.Value is TextFieldModelController ||
                        f.Value is NumberFieldModelController)
                    {
                        var hstack = new StackPanel {Orientation = Orientation.Horizontal};
                        var label = new TextBlock {Text = f.Key.Name + ": "};
                        var refField = new ReferenceFieldModelController(GetId(), f.Key);
                        var dBox = f.Value is ImageFieldModelController
                            ? new ImageBox(refField).Document
                            : new TextingBox(refField).Document;

                        hstack.Children.Add(label);

                        var ele = dBox.MakeViewUI(context, isInterfaceBuilder);

                        //ele.MaxWidth = 200;
                        hstack.Children.Add(ele);
                        sp.Children.Add(hstack);
                        //source.Add(hstack);
                    }
                    else if (f.Value is DocumentFieldModelController)
                    {
                        var fieldDoc = (f.Value as DocumentFieldModelController).Data;
                        // bcz: commented this out because it generated exceptions after making a search List of Umpires
                        var view = new DocumentView(new DocumentViewModel(fieldDoc, isInterfaceBuilder));
                        sp.Children.Add(view);
                        //source.Add(view);
                    }
                    else if (f.Value is DocumentCollectionFieldModelController)
                    {
                        var colView =
                            new CollectionView(
                                new CollectionViewModel(new ReferenceFieldModelController(GetId(), f.Key),
                                    isInterfaceBuilder, context), CollectionView.CollectionViewType.Grid);
                        colView.HorizontalAlignment = HorizontalAlignment.Stretch;
                        colView.VerticalAlignment = VerticalAlignment.Stretch;
                        colView.Height = 300;

                        sp.Children.Add(colView);
                        //source.Add(border);
                    }
                };

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Low,
                async () =>
                {
                    foreach (var f in fields)
                    {
                        a(f);
                        await Task.Delay(5);
                    }
                });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            return sp;
        }

        public FrameworkElement MakeViewUI(Context context, bool isInterfaceBuilder, DocumentController dataDocument = null)
        {
            context = new Context(context);
            context.AddDocumentContext(this);

            //TODO we can probably just wrap the return value in a SelectableContainer here instead of in the MakeView methods.
            if (DocumentType == TextingBox.DocumentType)
            {
                return TextingBox.MakeView(this, context, isInterfaceBuilder, true);
            }
            if (DocumentType == ImageBox.DocumentType)
            {
                return ImageBox.MakeView(this, context, isInterfaceBuilder);
            }
            if (DocumentType == DocumentBox.DocumentType)
            {
                return DocumentBox.MakeView(this, context, isInterfaceBuilder);
            }
            if (DocumentType == KeyValueDocumentBox.DocumentType)
            {
                return KeyValueDocumentBox.MakeView(this, context, isInterfaceBuilder);
            }
            if (DocumentType == StackLayout.DocumentType)
            {
                return StackLayout.MakeView(this, context, dataDocument, isInterfaceBuilder);
            }
            if (DocumentType == WebBox.DocumentType)
            {
                return WebBox.MakeView(this, context, isInterfaceBuilder);
            }
            if (DocumentType == CollectionBox.DocumentType)
            {
                return CollectionBox.MakeView(this, context, dataDocument, isInterfaceBuilder);
            }
            if (DocumentType == OperatorBox.DocumentType)
            {
                return OperatorBox.MakeView(this, context, isInterfaceBuilder);
            }
            if (DocumentType == DashConstants.DocumentTypeStore.FreeFormDocumentLayout)
            {
                return FreeFormDocument.MakeView(this, context, dataDocument, isInterfaceBuilder);
            }
            if (DocumentType == InkBox.DocumentType)
            {
                return InkBox.MakeView(this, context, dataDocument, isInterfaceBuilder);
            }
            if (DocumentType == GridViewLayout.DocumentType)
            {
                return GridViewLayout.MakeView(this, context, dataDocument, isInterfaceBuilder);
            }
            if (DocumentType == ListViewLayout.DocumentType)
            {
                return ListViewLayout.MakeView(this, context, dataDocument, isInterfaceBuilder);
            }
            if (DocumentType == RichTextBox.DocumentType)
            {
                return RichTextBox.MakeView(this, context, isInterfaceBuilder);
            }
            if (DocumentType == GridLayout.GridPanelDocumentType)
            {
                return GridLayout.MakeView(this, context, dataDocument, isInterfaceBuilder);
            }
            if (DocumentType == FilterOperatorBox.DocumentType)
            {
                return FilterOperatorBox.MakeView(this, context, isInterfaceBuilder);
            }
            if (DocumentType == CollectionMapOperatorBox.DocumentType)
            {
                return CollectionMapOperatorBox.MakeView(this, context, isInterfaceBuilder);
            }
            if (DocumentType == DBSearchOperatorBox.DocumentType)
            {
                return DBSearchOperatorBox.MakeView(this, context, isInterfaceBuilder);
            }
            if (DocumentType == ApiOperatorBox.DocumentType)
            {
                return ApiOperatorBox.MakeView(this, context, isInterfaceBuilder);
            }
            // if document is not a known UI View, then see if it contains a Layout view field
            var fieldModelController = GetDereferencedField(KeyStore.ActiveLayoutKey, context);
            if (fieldModelController != null)
            {
                var doc = fieldModelController.DereferenceToRoot<DocumentFieldModelController>(context);

                if (doc.Data.DocumentType == DefaultLayout.DocumentType)
                {
                    if (isInterfaceBuilder)
                    {
                        var activeLayout = this.GetActiveLayout(context).Data;
                        return new SelectableContainer(makeAllViewUI(context), activeLayout, this);
                    }
                    return makeAllViewUI(context);
                }
                Debug.Assert(doc != null);

                return doc.Data.MakeViewUI(context, isInterfaceBuilder, this);
            }
            if (isInterfaceBuilder)
            {
                return new SelectableContainer(makeAllViewUI(context), this, dataDocument);
            }
            return makeAllViewUI(context);
        }

        protected virtual void OnDocumentFieldUpdated(DocumentController sender, DocumentFieldUpdatedEventArgs args, bool updateDelegates)
        {
            if (_fieldUpdatedDictionary.ContainsKey(args.Reference.FieldKey))
            {
                _fieldUpdatedDictionary[args.Reference.FieldKey]?.Invoke(sender, args);
            }
            DocumentFieldUpdated?.Invoke(sender, args);
            if (updateDelegates && !args.Reference.FieldKey.Equals(KeyStore.DelegatesKey))
            {
                PrototypeFieldUpdated?.Invoke(sender, args);
            }
        }
    }
}