using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using static Dash.CourtesyDocuments;


namespace Dash
{
    public class DocumentController : ViewModelBase, IController
    {
        public enum FieldUpdatedAction
        {
            Add,
            Remove,
            Replace
        }

        public class DocumentFieldUpdatedEventArgs
        {
            public readonly FieldUpdatedAction Action;
            public readonly FieldModelController OldValue;
            public readonly FieldModelController NewValue;
            public readonly ReferenceFieldModelController Reference;

            public DocumentFieldUpdatedEventArgs(FieldModelController oldValue, FieldModelController newValue,
                FieldUpdatedAction action, ReferenceFieldModelController reference)
            {
                Action = action;
                OldValue = oldValue;
                NewValue = newValue;
                Reference = reference;
            }
        }

        public delegate void OnDocumentFieldUpdatedHandler(DocumentFieldUpdatedEventArgs args);

        public event OnDocumentFieldUpdatedHandler DocumentFieldUpdated;

        public ObservableCollection<DocumentController> DocContextList;


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

            // if the field contained a DocumentFieldModelController return it's data, otherwise return null
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
            
            // if the fields are reference equal just return
            if (ReferenceEquals(oldField, field))
            {
                return;
            }

            proto._fields[key] = field;
            proto.DocumentModel.Fields[key] = field.FieldModel.Id;

            FieldUpdatedAction action = oldField == null ? FieldUpdatedAction.Add : FieldUpdatedAction.Replace;
            ReferenceFieldModelController reference = new DocumentReferenceController(GetId(), key);
            OnDocumentFieldUpdated(new DocumentFieldUpdatedEventArgs(oldField, field, action, reference));
            field.FieldModelUpdated += delegate (FieldModelController sender)
            {
                OnDocumentFieldUpdated(new DocumentFieldUpdatedEventArgs(null, sender, FieldUpdatedAction.Replace, reference));
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
        /// <returns></returns>
        public FieldModelController GetField(Key key, Context context = null)
        {
            context = Context.SafeInitAndAddDocument(context, this);
            // search up the hiearchy starting at this for the first DocumentController which has the passed in key
            var firstProtoWithKeyOrNull = GetPrototypeWithFieldKey(key);

            return firstProtoWithKeyOrNull?._fields[key];
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

            // create and set a prototype field on the child, pointing to ourself
            var prototypeFieldController = new DocumentFieldModelController(this);
            delegateController.SetField(DashConstants.KeyStore.PrototypeKey, prototypeFieldController, true);

            // add the delegate to our delegates field
            var currentDelegates = GetDelegates();
            currentDelegates.AddDocument(delegateController);

            // return the now fully populated delegate
            return delegateController;
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
                _fields[DashConstants.KeyStore.DelegatesKey] = currentDelegates;
            }

            return currentDelegates;
        }


        public virtual void AddInputReference(Key fieldKey, ReferenceFieldModelController reference, Context context = null)
        {
            //TODO Remove existing output references and add new output reference
            //if (InputReferences.ContainsKey(fieldKey))
            //{
            //    FieldModel fm = docEndpoint.GetFieldInDocument(InputReferences[fieldKey]);
            //    fm.RemoveOutputReference(new ReferenceFieldModel {DocId = Id, Key = fieldKey});
            //}
            //reference.DocContextList = contextList;  //bcz : TODO This is wrong, but I need to understand input references more to know how to fix it.
            reference.Context = context;
            var field = GetField(fieldKey);
            var refField = reference.DereferenceToRoot(context);
            var controller = reference.GetDocumentController();
            //if (field == null)
            //{
            //    var op = GetField(OperatorDocumentModel.OperatorKey) as OperatorFieldModelController;
            //    if (op == null)
            //    {
            //        throw new ArgumentOutOfRangeException($"Key {fieldKey} does not exist in document");
            //    }
            //    if (!op.Inputs.ContainsKey(fieldKey))
            //    {
            //        throw new ArgumentOutOfRangeException($"Key {fieldKey} does not exist in document");
            //    }
            //    TypeInfo info = op.Inputs[fieldKey];
            //    if ((info & refField.TypeInfo) == refField.TypeInfo)
            //    {
            //        field = TypeInfoHelper.CreateFieldModelController(refField.TypeInfo);
            //        SetField(fieldKey, field, true);
            //    }
            //    else
            //    {
            //        throw new ArgumentException("Invalid types");
            //    }
            //}
            //else
            //{
            if (!field.CheckType(refField))
            {
                Debug.Assert(!refField.CheckType(field));
                throw new ArgumentException("Invalid types");
            }

            field.InputReference = reference;
            controller.DocumentFieldUpdated += delegate (DocumentFieldUpdatedEventArgs args)
            {
                if (args.Reference.FieldKey.Equals(reference.FieldKey))
                {
                    Execute();
                }
            };
            Execute();
        }

        public FieldModelController GetDereferencedField(Key key, Context context = null)
        {
            context = Context.SafeInitAndAddDocument(context, this);
            var fieldController = GetField(key);
            return fieldController?.DereferenceToRoot(context);
        }


        private void Execute(Context context = null)
        {
            var opField = GetDereferencedField(OperatorDocumentModel.OperatorKey, context) as OperatorFieldModelController;
            if (opField == null)
            {
                return;
            }
            try
            {
                opField.Execute(this, context);//TODO Add Document fields updated in addition to the field updated event so that assigning to the field itself instead of data triggers updates
            }
            catch (KeyNotFoundException e)
            {
            }
            //foreach (var fieldModel in results)
            //{
            //    SetField(fieldModel.Key, fieldModel.Value);
            //}
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
        private FrameworkElement makeAllViewUI(Context context = null)
        {
            var sp = new StackPanel();
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
                    var dBox = new DataBox(new DocumentReferenceController(GetId(), f.Key), f.Value is ImageFieldModelController).Document;

                    hstack.Children.Add(label);

                    var ele = dBox.MakeViewUI(context);

                    ele.MaxWidth = 200;
                    hstack.Children.Add(ele);

                    sp.Children.Add(hstack);
                }
                else if (f.Value is DocumentFieldModelController)
                {
                    var fieldDoc = (f.Value as DocumentFieldModelController).Data;
                    sp.Children.Add(new DocumentView(new DocumentViewModel(fieldDoc)));
                    (sp.Children.Last() as FrameworkElement).MaxWidth = 300;
                    (sp.Children.Last() as FrameworkElement).MaxHeight = 300;
                }
                else if (f.Value is DocumentCollectionFieldModelController)
                {
                    foreach (var fieldDoc in (f.Value as DocumentCollectionFieldModelController).GetDocuments())
                    {
                        sp.Children.Add(new DocumentView(new DocumentViewModel(fieldDoc)));
                        (sp.Children.Last() as FrameworkElement).MaxWidth = 300;
                        (sp.Children.Last() as FrameworkElement).MaxHeight = 300;
                    }
                }
            }
            return sp;
        }


        public FrameworkElement MakeViewUI(Context context = null)
        {
            context = context ?? new Context();
            context.AddDocumentContext(this);

            if (DocumentType == TextingBox.DocumentType)
            {
                return TextingBox.MakeView(this, context);
            }
            if (DocumentType == ImageBox.DocumentType)
            {
                return ImageBox.MakeView(this, context);
            }
            if (DocumentType == DocumentBox.DocumentType)
            {
                return DocumentBox.MakeView(this, context);
            }
            if (DocumentType == StackingPanel.DocumentType)
            {
                return StackingPanel.MakeView(this, context);
            }
            if (DocumentType == CollectionBox.DocumentType)
            {
                return CollectionBox.MakeView(this, context);
            }
            if (DocumentType == OperatorBox.DocumentType)
            {
                return OperatorBox.MakeView(this, context);
            }
            if (DocumentType == ApiDocumentModel.DocumentType)
            {
                return ApiDocumentModel.MakeView(this, context);
            }
            if (DocumentType == DashConstants.DocumentTypeStore.FreeFormDocumentLayout)
            {
                return FreeFormDocument.MakeView(this, context);
            }

            // if document is not a known UI View, then see if it contains a Layout view field
            var fieldModelController = GetDereferencedField(DashConstants.KeyStore.ActiveLayoutKey, context);
            if (fieldModelController != null)
            {
                var doc = fieldModelController.DereferenceToRoot<DocumentFieldModelController>(context);

                if (doc.Data.DocumentType == DashConstants.DocumentTypeStore.DefaultLayout)
                {
                    return makeAllViewUI(context);
                }
                Debug.Assert(doc != null);
                return doc.Data.MakeViewUI(context);
            }

            return makeAllViewUI(context);
        }


        protected virtual void OnDocumentFieldUpdated(DocumentFieldUpdatedEventArgs args)
        {
            DocumentFieldUpdated?.Invoke(args);
        }
    }
}