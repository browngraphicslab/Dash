using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;


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
            public readonly Context Context;
            public bool FromDelegate;

            public DocumentFieldUpdatedEventArgs(FieldModelController oldValue, FieldModelController newValue,
                FieldUpdatedAction action, DocumentFieldReference reference, Context context, bool fromDelegate)
            {
                Action = action;
                OldValue = oldValue;
                NewValue = newValue;
                Reference = reference;
                Context = context;
                FromDelegate = fromDelegate;
            }
        }

        public delegate void OnDocumentFieldUpdatedHandler(DocumentController sender, DocumentFieldUpdatedEventArgs args);

        private readonly Dictionary<Key, OnDocumentFieldUpdatedHandler> _fieldUpdatedDictionary = new Dictionary<Key, OnDocumentFieldUpdatedHandler>();
        public event OnDocumentFieldUpdatedHandler DocumentFieldUpdated;
        public event OnDocumentFieldUpdatedHandler PrototypeFieldUpdated;

        public void AddFieldUpdatedListener(Key key, OnDocumentFieldUpdatedHandler handler)
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

        public void RemoveFieldUpdatedListener(Key key, OnDocumentFieldUpdatedHandler handler)
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
        private Dictionary<Key, FieldModelController> _fields = new Dictionary<Key, FieldModelController>();


        public DocumentController(IDictionary<Key, FieldModelController> fields, DocumentType type, string id = null)
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
            // Add Events
        }


        /// <summary>
        ///     The <see cref="DocumentModel" /> associated with this <see cref="DocumentController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public DocumentModel DocumentModel { get; }


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
        ///     Returns the first level of inheritance which references the passed in <see cref="Key" /> or
        ///     returns null if no level of inheritance has a <see cref="FieldModelController" /> associated with the passed in
        ///     <see cref="Key" />
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public DocumentController GetPrototypeWithFieldKey(Key key)
        {
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
            if (!_fields.ContainsKey(DashConstants.KeyStore.PrototypeKey))
                return null;

            // otherwise try to convert the field associated with the prototype key into a DocumentFieldModelController
            var documentFieldModelController =
                _fields[DashConstants.KeyStore.PrototypeKey] as DocumentFieldModelController;


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
        ///     Sets the <see cref="FieldModelController" /> associated with the passed in <see cref="Key" /> at the first
        ///     prototype in the hierarchy that contains it. If the <see cref="Key" /> is not used at any level then it is
        ///     created in this <see cref="DocumentController" />.
        ///     <para>
        ///         If <paramref name="forceMask" /> is set to true, then we never search for a prototype and simply override
        ///         any prototype that might exist by setting the field on this
        ///     </para>
        /// </summary>
        /// <param name="key">key index of field to update</param>
        /// <param name="field">FieldModel to update to</param>
        /// <param name="forceMask"></param>
        public void SetField(Key key, FieldModelController field, bool forceMask)
        {
            var proto = forceMask ? this : GetPrototypeWithFieldKey(key) ?? this;

            FieldModelController oldField;
            proto._fields.TryGetValue(key, out oldField);
            oldField?.Dispose();

            // if the fields are reference equal just return
            if (ReferenceEquals(oldField, field))
            {
                return;
            }

            proto._fields[key] = field;
            proto.DocumentModel.Fields[key] = field.FieldModel.Id;

            FieldUpdatedAction action = oldField == null ? FieldUpdatedAction.Add : FieldUpdatedAction.Replace;
            var reference = new DocumentFieldReference(GetId(), key);
            Context c = new Context(this);
            if (ShouldExecute(c, key))
            {
                Execute(c, true);
            }
            OnDocumentFieldUpdated(this, new DocumentFieldUpdatedEventArgs(oldField, field, action, reference, new Context(this), false), true);
            field.FieldModelUpdated += delegate (FieldModelController sender, Context context)
            {
                context = context ?? new Context();
                context.AddDocumentContext(this);
                if (ShouldExecute(context, reference.FieldKey))
                {
                    Execute(context, true);
                }
                OnDocumentFieldUpdated(this, new DocumentFieldUpdatedEventArgs(null, sender, FieldUpdatedAction.Replace, reference, context, false), true);//TODO Should be Action.Update
            };

            // TODO either notify the delegates here, or notify the delegates in the FieldsOnCollectionChanged method
            //proto.notifyDelegates(new ReferenceFieldModel(Id, key));
        }


        /// <summary>
        ///     returns the <see cref="FieldModelController" /> for the specified <see cref="Key" /> by looking first in this
        ///     <see cref="DocumentController" />
        ///     and then sequentially up the hierarchy of prototypes of this <see cref="DocumentController" />. If the
        ///     key is not found then it returns null.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="context"></param>
        /// <param name="ignorePrototype"></param>
        /// <returns></returns>
        public FieldModelController GetField(Key key, bool ignorePrototype = false)
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
        public void SetFields(IDictionary<Key, FieldModelController> fields, bool forceMask)
        {
            foreach (var f in fields)
                SetField(f.Key, f.Value, forceMask);
        }


        /// <summary>
        ///     Creates a delegate (child) of the given document that inherits all the fields of the prototype (parent)
        /// </summary>
        /// <returns></returns>
        public DocumentController MakeDelegate()
        {
            // create a controller for the child
            var delegateController = new DocumentController(new Dictionary<Key, FieldModelController>(), DocumentType);
            delegateController.DocumentFieldUpdated +=
                delegate(DocumentController sender, DocumentFieldUpdatedEventArgs args)
                {
                    args.FromDelegate = true;
                    OnDocumentFieldUpdated(sender, args, false);
                };
            PrototypeFieldUpdated += delegateController.OnPrototypeDocumentFieldUpdated;

            // create and set a prototype field on the child, pointing to ourself
            var prototypeFieldController = new DocumentFieldModelController(this);
            delegateController.SetField(DashConstants.KeyStore.PrototypeKey, prototypeFieldController, true);

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
            OnDocumentFieldUpdated(this, new DocumentFieldUpdatedEventArgs(args.OldValue, args.NewValue, FieldUpdatedAction.Add, reference, c, false), true);
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
            var currentDelegates = _fields.ContainsKey(DashConstants.KeyStore.DelegatesKey)
                ? _fields[DashConstants.KeyStore.DelegatesKey] as DocumentCollectionFieldModelController
                : null;

            // if not then populate it with a new list of documents
            if (currentDelegates == null)
            {
                currentDelegates =
                    new DocumentCollectionFieldModelController(new List<DocumentController>());
                SetField(DashConstants.KeyStore.DelegatesKey, currentDelegates, true);
            }

            return currentDelegates;
        }

        public FieldModelController GetDereferencedField(Key key, Context context)
        {
            context = Context.SafeInitAndAddDocument(context, this);
            var fieldController = GetField(key);
            return fieldController?.DereferenceToRoot(context);
        }


        private bool ShouldExecute(Context context, Key updatedKey)
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
                var inputs = new Dictionary<Key, FieldModelController>(opField.Inputs.Count);
                var outputs = new Dictionary<Key, FieldModelController>(opField.Outputs.Count);
                foreach (var opFieldInput in opField.Inputs.Keys)
                {
                    var field = GetField(opFieldInput);
                    inputs[opFieldInput] = field?.DereferenceToRoot(context) ??
                        TypeInfoHelper.CreateFieldModelController(opField.Inputs[opFieldInput]);
                }
                opField.Execute(inputs, outputs);
                foreach (var fieldModel in outputs)
                {
                    var reference = new DocumentFieldReference(GetId(), fieldModel.Key);
                    context.AddData(reference, fieldModel.Value);
                    if (update)
                    {
                        OnDocumentFieldUpdated(this, new DocumentFieldUpdatedEventArgs(null, fieldModel.Value,
                            FieldUpdatedAction.Add, reference, context, false), true);
                    }
                }
            }
            catch (KeyNotFoundException e)
            {
                Debug.WriteLine("Operator Execution failed: Input not set");
            }
        }


        public IEnumerable<KeyValuePair<Key, FieldModelController>> PropFields => EnumFields();


        public IEnumerable<KeyValuePair<Key, FieldModelController>> EnumFields(bool ignorePrototype = false)
        {
            foreach (KeyValuePair<Key, FieldModelController> fieldModelController in _fields)
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
            var sp = new StackPanel();
            var isInterfaceBuilder = false;

            foreach (var f in EnumFields())
            {
                if (f.Key.Equals(DashConstants.KeyStore.DelegatesKey) ||
                    f.Key.Equals(DashConstants.KeyStore.PrototypeKey) ||
                    f.Key.Equals(DashConstants.KeyStore.LayoutListKey) ||
                    f.Key.Equals(DashConstants.KeyStore.ActiveLayoutKey))
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

                    ele.MaxWidth = 200;
                    hstack.Children.Add(ele);

                    sp.Children.Add(hstack);
                }
                else if (f.Value is DocumentFieldModelController)
                {
                    var fieldDoc = (f.Value as DocumentFieldModelController).Data;
                    sp.Children.Add(new DocumentView(new DocumentViewModel(fieldDoc, isInterfaceBuilder)));
                    (sp.Children.Last() as FrameworkElement).MaxWidth = 300;
                    (sp.Children.Last() as FrameworkElement).MaxHeight = 300;
                }
                else if (f.Value is DocumentCollectionFieldModelController)
                {
                    foreach (var fieldDoc in (f.Value as DocumentCollectionFieldModelController).GetDocuments())
                    {
                        sp.Children.Add(new DocumentView(new DocumentViewModel(fieldDoc, isInterfaceBuilder)));
                        (sp.Children.Last() as FrameworkElement).MaxWidth = 300;
                        (sp.Children.Last() as FrameworkElement).MaxHeight = 300;
                    }
                }
            }
            return sp;
        }


        public FrameworkElement MakeViewUI(Context context, bool isInterfaceBuilder, DocumentController dataDocument = null)
        {
            context = context ?? new Context();
            //context = context == null ? new Context() : new Context(context);//TODO Should we copy the context or not?
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
            if (DocumentType == StackingPanel.DocumentType)
            {
                return StackingPanel.MakeView(this, context, dataDocument, isInterfaceBuilder);
            }
            if (DocumentType == CollectionBox.DocumentType)
            {
                return CollectionBox.MakeView(this, context, isInterfaceBuilder);
            }
            if (DocumentType == OperatorBox.DocumentType)
            {
                return OperatorBox.MakeView(this, context, isInterfaceBuilder);
            }
            if (DocumentType == ApiDocumentModel.DocumentType)
            {
                return ApiDocumentModel.MakeView(this, context, isInterfaceBuilder);
            }
            if (DocumentType == DashConstants.DocumentTypeStore.FreeFormDocumentLayout)
            {
                return FreeFormDocument.MakeView(this, context, dataDocument, isInterfaceBuilder);
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
            if (DocumentType == GridPanel.GridPanelDocumentType)
            {
                return GridPanel.MakeView(this, context, isInterfaceBuilder);
            }

            // if document is not a known UI View, then see if it contains a Layout view field
            var fieldModelController = GetDereferencedField(DashConstants.KeyStore.ActiveLayoutKey, context);
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
            Debug.Assert(false, "Everything should have an active layout maybe");
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
            if (updateDelegates)
            {
                PrototypeFieldUpdated?.Invoke(sender, args);
            }
        }
    }
}