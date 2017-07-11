﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DashShared;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Dash
{
    public class DocumentController : ViewModelBase, IController
    {
        public delegate void OnDocumentFieldUpdatedHandler(FieldModelController oldValue, FieldModelController newValue,
            ReferenceFieldModelController reference);

        public event OnDocumentFieldUpdatedHandler DocumentFieldUpdated;

        /// <summary>
        ///     A wrapper for <see cref="DocumentModel.Fields" />. Change this to propogate changes
        ///     to the server and across the client
        /// </summary>
        private Dictionary<Key, FieldModelController> _fields;

        public DocumentController(IDictionary<Key, FieldModelController> fields, DocumentType type)
        {
            DocumentModel model = new DocumentModel(fields.ToDictionary(kv => kv.Key, kv => kv.Value.FieldModel), type);
            ContentController.AddModel(model);
            // Initialize Local Variables
            DocumentModel = model;
            // get the field controllers associated with the FieldModel id's stored in the document Model
            // put the field controllers in an observable dictionary
            _fields = new Dictionary<Key, FieldModelController>(fields);
            ContentController.AddController(this);
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

            FieldModelController oldValue;
            _fields.TryGetValue(key, out oldValue);

            proto._fields[key] = field;
            proto.DocumentModel.Fields[key] = field.FieldModel.Id;

            OnDocumentFieldUpdated(oldValue, field, new ReferenceFieldModelController(GetId(), key));

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
        public FieldModelController GetField(Key key, List<DocumentController> contextList = null)
        {
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
            // TODO WE NEED TO STORE THESE CONTROLLERS SOMEWHERE
            // create a controller for the child
            var delegateController = new DocumentController(new Dictionary<Key, FieldModelController>(), DocumentType);

            // create and set a prototype field on the child, pointing to ourself
            var prototypeFieldController = new DocumentFieldModelController(this);
            delegateController.SetField(DashConstants.KeyStore.PrototypeKey, prototypeFieldController, true);

            // add the delegate to our delegates field
            var currentDelegates = GetDelegates();
            currentDelegates.GetDocuments().Add(delegateController);

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
                currentDelegates =
                    new DocumentCollectionFieldModelController(new List<DocumentController>());

            return currentDelegates;
        }

        public virtual void AddInputReference(Key fieldKey, ReferenceFieldModelController reference, List<DocumentController> contextList = null)
        {
            //TODO Remove existing output references and add new output reference
            //if (InputReferences.ContainsKey(fieldKey))
            //{
            //    FieldModel fm = docEndpoint.GetFieldInDocument(InputReferences[fieldKey]);
            //    fm.RemoveOutputReference(new ReferenceFieldModel {DocId = Id, Key = fieldKey});
            //}
            reference.DocContextList = contextList;  //bcz : TODO This is wrong, but I need to understand input references more to know how to fix it.
            var field = GetField(fieldKey);
            var refField = GetDereferencedField(reference, contextList);
            if (field == null)
            {
                var op = GetField(OperatorDocumentModel.OperatorKey) as OperatorFieldModelController;
                if (op == null)
                {
                    throw new ArgumentOutOfRangeException($"Key {fieldKey} does not exist in document");
                }
                if (!op.Inputs.ContainsKey(fieldKey))
                {
                    throw new ArgumentOutOfRangeException($"Key {fieldKey} does not exist in document");
                }
                TypeInfo info = op.Inputs[fieldKey];
                if ((info & refField.TypeInfo) == refField.TypeInfo)
                {
                    field = TypeInfoHelper.CreateFieldModelController(refField.TypeInfo);
                    SetField(fieldKey, field, true);
                }
                else
                {
                    throw new ArgumentException("Invalid types");
                }
            }
            else
            {
                if (!field.CheckType(refField))
                {
                    Debug.Assert(!refField.CheckType(field));
                    throw new ArgumentException("Invalid types");
                }
            }
            field.InputReference = reference;
            refField.FieldModelUpdatedEvent += DocumentController_FieldModelUpdatedEvent;//TODO should this dereference to root?
            Execute();
        }

        private void DocumentController_FieldModelUpdatedEvent(FieldModelController sender)
        {
            Execute();
        }

        public static FieldModelController GetDereferencedField(FieldModelController fieldModelController, IEnumerable<DocumentController> contextList)
        {
            return ContentController.DereferenceToRootFieldModel(fieldModelController, contextList);
        }
        public FieldModelController GetDereferencedField(Key key, IEnumerable<DocumentController> contextList)
        {
            var fieldController = GetField(key);
            if (fieldController == null)
                return null;
            return ContentController.DereferenceToRootFieldModel(fieldController, contextList);
        }

        private void Execute(IEnumerable<DocumentController> contextList = null)
        {
            var opField = GetDereferencedField(OperatorDocumentModel.OperatorKey, contextList) as OperatorFieldModelController;
            if (opField == null)
            {
                return;
            }
            try
            {
                opField.Execute(this, contextList);//TODO Add Document fields updated in addition to the field updated event so that assigning to the field itself instead of data triggers updates
            }
            catch (KeyNotFoundException e)
            {
                return;
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
                    foreach (var field in prototype.EnumFields().Where((f) => !_fields.ContainsKey(f.Key)))
                        yield return field;
            }
        }

        /// <summary>
        /// Generates a UI view that showcases document fields as a list of key value pairs, where key is the
        /// string key of the field and value is the rendered UI element representing the value.
        /// </summary>
        /// <returns></returns>
        private FrameworkElement makeAllViewUI(List<DocumentController> docList)
        {
            var sp = new StackPanel();
            foreach (var f in EnumFields())
            {
                if (f.Value is ImageFieldModelController || f.Value is TextFieldModelController || f.Value is NumberFieldModelController)
                {
                    var hstack = new StackPanel() { Orientation = Orientation.Horizontal };
                    var label = new TextBlock() { Text = f.Key.Name + ": " };
                    var dBox = new CourtesyDocuments.DataBox(new ReferenceFieldModelController(GetId(), f.Key), f.Value is ImageFieldModelController).Document;

                    hstack.Children.Add(label);
                    var ele = dBox.makeViewUI(docList);
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

        public FrameworkElement MakeViewUI()
        {
            return makeViewUI(new List<DocumentController>());
        }

        public FrameworkElement makeViewUI(IEnumerable<DocumentController> docContextList)
        {
            var docList = docContextList == null ? new List<DocumentController>() : new List<DocumentController>(docContextList);
            docList.Add(this);
            var uieles = new List<FrameworkElement>();

            if (DocumentType == CourtesyDocuments.TextingBox.DocumentType)
            {
                return CourtesyDocuments.TextingBox.MakeView(this, docList);
            }
            if (DocumentType == CourtesyDocuments.ImageBox.DocumentType)
            {
                return CourtesyDocuments.ImageBox.MakeView(this, docList);
            }
            if (DocumentType == CourtesyDocuments.StackingPanel.DocumentType)
            {
                return CourtesyDocuments.StackingPanel.MakeView(this, docList);
            }
            if (DocumentType == CourtesyDocuments.CollectionBox.DocumentType)
            {
                return CourtesyDocuments.CollectionBox.MakeView(this, docList);
            }
            if (DocumentType == CourtesyDocuments.OperatorBox.DocumentType)
            {
                return CourtesyDocuments.OperatorBox.MakeView(this, docList);
            } 
            else if (DocumentType == CourtesyDocuments.ApiDocumentModel.DocumentType) 
            {
                return CourtesyDocuments.ApiDocumentModel.MakeView(this, docList);
            } 
            else // if document is not a known UI View, then see if it contains a Layout view field
            {
                var fieldModelController = GetDereferencedField(DashConstants.KeyStore.LayoutKey, docContextList);
                if (fieldModelController != null)
                {
                    var newDocContextList = docContextList == null ? new List<DocumentController>() : new List<DocumentController>(docContextList);
                    newDocContextList.Add(this);
                    var doc = GetDereferencedField(fieldModelController, newDocContextList) as DocumentFieldModelController;
                    Debug.Assert(doc != null);
                    return doc.Data.makeViewUI(docList);
                }
            }

            return makeAllViewUI(docList);
        }

        protected virtual void OnDocumentFieldUpdated(FieldModelController oldvalue, FieldModelController newvalue, ReferenceFieldModelController reference)
        {
            DocumentFieldUpdated?.Invoke(oldvalue, newvalue, reference);
        }
    }
}