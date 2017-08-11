using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using DashShared;
using Dash.Controllers.Operators;

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

            SetField(InkBox.InkDataKey, new InkFieldModelController(), true);

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
        /// <summary>
        /// parses text input into a field controller
        /// </summary>
        /// <param name="docController"></param>
        /// <param name="key"></param>
        /// <param name="textInput"></param>
        public void ParseDocField(KeyController key, string textInput)
        {
            textInput = textInput.Trim(' ');
            if (textInput.StartsWith("@"))
            {
                var proto = GetPrototype() == null ? this : GetPrototype();
                if (proto.GetField(KeyStore.PrimaryKeyKey) == null)
                    proto.SetField(KeyStore.ThisKey, new DocumentFieldModelController(proto), true);
                var fieldStr = textInput.Substring(1, textInput.Length - 1);
                if (textInput.Contains("=")) // search globally for a document that has a field, FieldName, with contents that match FieldValue
                {                       // @ FieldName = FieldValue
                    var eqPos2 = fieldStr.IndexOfAny(new char[] { '=' });
                    var fieldValue = fieldStr.Substring(eqPos2 + 1, System.Math.Max(0, fieldStr.Length - eqPos2 - 1)).Trim(' ', '\r');
                    var fieldName = fieldStr.Substring(0, eqPos2).TrimEnd(' ').TrimStart(' ');

                    foreach (var doc in ContentController.GetControllers<DocumentController>())
                        foreach (var field in doc.EnumFields())
                            if (field.Key.Name == fieldName && (field.Value as TextFieldModelController)?.Data == fieldValue)
                            {
                                SetField(key, new DocumentFieldModelController(doc), true);
                                break;
                            }
                }
                else // search for documents that have a field matching FieldName.
                {    // #newField = @ func( @ doc.field, @ doc.field )
                    var strings = fieldStr.Split('(');
                    if (strings.Count() == 2)
                    {
                        var opModel = lookupOperator(strings[0]);
                        var args    = strings[1].TrimEnd(')').Split(',');
                        var refs    = new List<ReferenceFieldModelController>();
                        bool useProto = false;
                        foreach (var a in args)
                        {
                            if (a.Trim(' ').StartsWith("@"))
                            {
                                var path = a.Substring(1, a.Length - 1).Split('.');
                                useProto |= path[0] == "This";
                                var theDoc = path[0] == "This" ? proto : FindDocMatchingPrimaryKeys(new List<string>(new string[] { path[0] }));
                                if (theDoc != null)
                                {
                                    if (path.Count() > 1)
                                    {
                                        KeyController foundKey = null;
                                        foreach (var e in ((path[0] == "This") ? this : theDoc).EnumFields())
                                            if (e.Key.Name == path[1])
                                            {
                                                foundKey = e.Key;
                                                break;
                                            }
                                        refs.Add(new ReferenceFieldModelController(theDoc.GetId(), foundKey));
                                    }
                                    else
                                        refs.Add(new ReferenceFieldModelController(theDoc.GetId(), KeyStore.ThisKey));
                                }
                            }
                        }
                        int count = 0;
                        var opFieldController = (opModel.GetField(OperatorDocumentModel.OperatorKey) as OperatorFieldModelController);
                        foreach (var i in opFieldController.Inputs.ToArray())
                            if (count < refs.Count())
                                opModel.SetField(i.Key, refs[count++], true);
                        (useProto ? proto:this).SetField(key, new ReferenceFieldModelController(opModel.GetId(), opFieldController.Outputs.First().Key), true);
                        Debug.WriteLine("Value = " + (useProto ? proto : this).GetDereferencedField(key, null));
                    }
                    else
                    {
                        var path = strings[0].Trim(' ').Split('.');
                        var theDoc = path[0] == "This" ? proto : FindDocMatchingPrimaryKeys(new List<string>(new string[] { path[0] }));
                        if (theDoc != null)
                        {
                            if (path.Count() > 1)
                            {
                                foreach (var e in ((path[0] == "This") ? this : theDoc).EnumFields())
                                    if (e.Key.Name == path[1])
                                    {
                                        ((path[0] == "This") ? proto:this).SetField(key, new ReferenceFieldModelController(theDoc.GetId(), e.Key), (path[0] != "This"));
                                        break;
                                    }
                            }
                            else
                                SetField(key, new ReferenceFieldModelController(theDoc.GetId(), KeyStore.ThisKey), true);
                        }
                        else // start of path isn't a value ... treat it as a field and search for documents that reference this document that have it as a field
                        {
                            var searchDoc = DBSearchOperatorFieldModelController.CreateSearch(this, DBTest.DBDoc, strings[0], strings[0]);
                            SetField(key, new ReferenceFieldModelController(searchDoc.GetId(), DBSearchOperatorFieldModelController.ResultsKey), true);
                        }
                        Debug.WriteLine("Value = " + GetDereferencedField(key, null));
                    }
                }
            }
            else
            {
                double num;
                if (!double.TryParse(textInput, out num))
                    num = double.NaN;
                if (!double.IsNaN(num))
                    SetField(key, new NumberFieldModelController(num), true);
                else
                    SetField(key, new TextFieldModelController(textInput), true);
            }
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
                    c = c ?? new Context();
                    c.AddDocumentContext(this);
                    if (ShouldExecute(c, reference.FieldKey))
                    {
                        Execute(c, true);
                    }
                    OnDocumentFieldUpdated(this,
                        new DocumentFieldUpdatedEventArgs(null, sender, args.Action, reference, args, c, false), true);
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
        public void SetField(KeyController key, FieldModelController field, bool forceMask)
        {
            FieldModelController oldField;
            if (!SetFieldHelper(key, field, forceMask, out oldField))
            {
                return;
            }

            SetupNewFieldListeners(key, field, oldField, new Context(this));

            Context c = new Context(this);
            if (ShouldExecute(c, key))
            {
                Execute(c, true);
            }
            // TODO either notify the delegates here, or notify the delegates in the FieldsOnCollectionChanged method
            //proto.notifyDelegates(new ReferenceFieldModel(Id, key));
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
            Context c = new Context(this);
            //c.AddDocumentContext(this);
            var reference = new DocumentFieldReference(GetId(), args.Reference.FieldKey);
            OnDocumentFieldUpdated(this, new DocumentFieldUpdatedEventArgs(args.OldValue, args.NewValue, FieldUpdatedAction.Add, reference, args.FieldArgs, c, false), true);
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
            context = Context.SafeInitAndAddDocument(context, this);
            var fieldController = GetField(key);
            return fieldController?.DereferenceToRoot(context);
        }


        private bool ShouldExecute(Context context, KeyController updatedKey)
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

        public void Execute(Context context, bool update)
        {
            context = context ?? new Context(this);
            var opField = GetDereferencedField(OperatorDocumentModel.OperatorKey, context) as OperatorFieldModelController;
            if (opField == null)
            {
                return;
            }
            try
            {
                var inputs = new Dictionary<KeyController, FieldModelController>(opField.Inputs.Count);
                var outputs = new Dictionary<KeyController, FieldModelController>(opField.Outputs.Count);
                foreach (var opFieldInput in opField.Inputs.Keys)
                {
                    var field = GetField(opFieldInput);
                    field = field?.DereferenceToRoot(context);
                    if (field != null)
                    {
                        inputs[opFieldInput] = field;
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
                            FieldUpdatedAction.Add, reference, null, context, false), true);
                    }
                }
            }
            catch (KeyNotFoundException e)
            {
                Debug.WriteLine("Operator Execution failed: Input not set" + e);
            }
        }


        public IEnumerable<KeyValuePair<KeyController, FieldModelController>> PropFields => EnumFields();


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
            var sp = new ListView { SelectionMode = ListViewSelectionMode.None };
            var source = new ObservableCollection<FrameworkElement>();
            sp.ItemsSource = source;

            var isInterfaceBuilder = false; // TODO make this a parameter

            foreach (var f in EnumFields())
            {
                if (f.Key.Equals(KeyStore.DelegatesKey)  ||
                    f.Key.Equals(KeyStore.PrototypeKey)  ||
                    f.Key.Equals(KeyStore.LayoutListKey) ||
                    f.Key.Equals(KeyStore.ActiveLayoutKey))
                {
                    continue;
                }

                if (f.Value is ImageFieldModelController || f.Value is TextFieldModelController || f.Value is NumberFieldModelController)
                {
                    var hstack = new StackPanel { Orientation = Orientation.Horizontal };
                    var label = new TextBlock { Text = f.Key.Name + ": " };
                    var dBox = new DataBox(new ReferenceFieldModelController(GetId(), f.Key), f.Value is ImageFieldModelController).Document;

                    hstack.Children.Add(label);

                    var ele = dBox.MakeViewUI(context, isInterfaceBuilder);

                    //ele.MaxWidth = 200;
                    hstack.Children.Add(ele);

                    source.Add(hstack);
                }
                else if (f.Value is DocumentFieldModelController)
                {
                    var fieldDoc = (f.Value as DocumentFieldModelController).Data;
                    // bcz: commented this out because it generated exceptions after making a search List of Umpires
                    //sp.Children.Add(new DocumentView(new DocumentViewModel(fieldDoc, isInterfaceBuilder)));
                    //(sp.Children.Last() as FrameworkElement).MaxWidth = 300;
                    //(sp.Children.Last() as FrameworkElement).MaxHeight = 300;
                }
                else if (f.Value is DocumentCollectionFieldModelController)
                {
                    var colView = new CollectionGridView(new CollectionViewModel(f.Value, isInterfaceBuilder, context));

                    var border = new Border
                    {
                        MaxWidth = 275,
                        MaxHeight = 500,
                        BorderBrush = (SolidColorBrush)App.Instance.Resources["SelectedGrey"],
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(3),
                        Child = colView
                    };
                    source.Add(border);
                }
            }
            return sp;
        }


        public FrameworkElement MakeViewUI(Context context, bool isInterfaceBuilder, DocumentController dataDocument = null)
        {
            context = new Context(context);
            context.AddDocumentContext(this);

            //TODO we can probably just wrap the return value in a SelectableContainer here instead of in the MakeView methods.
            if (DocumentType == TextingBox.DocumentType)
            {
                return TextingBox.MakeView(this, context, isInterfaceBuilder);
            }
            if (DocumentType == ImageBox.DocumentType)
            {
                return ImageBox.MakeView(this, context, isInterfaceBuilder);
            }
            if (DocumentType == DocumentBox.DocumentType)
            {
                return DocumentBox.MakeView(this, context, isInterfaceBuilder);
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