using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using DashShared;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Controls;
using static Dash.MainPage;

namespace Dash
{
    public class DocumentController : ViewModelBase, IController
    {
        /// <summary>
        ///     A wrapper for <see cref="DocumentModel.Fields" />. Change this to propogate changes
        ///     to the server and across the client
        /// </summary>
        public ObservableDictionary<Key, FieldModelController> Fields;

        public DocumentController(DocumentModel documentModel)
        {
            // Initialize Local Variables
            DocumentModel = documentModel;
            // get the field controllers associated with the FieldModel id's stored in the document Model
            var fieldControllers =
                ContentController.GetControllers<FieldModelController>(documentModel.Fields.Values);
            // put the field controllers in an observable dictionary
            Fields =
                new ObservableDictionary<Key, FieldModelController>(documentModel.Fields.ToDictionary(kvp => kvp.Key,
                    kvp => fieldControllers.First(controller => controller.GetId() == kvp.Value)));

            // Add Events
            Fields.CollectionChanged += FieldsOnCollectionChanged;
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

        /// <summary>
        ///     Called whenver the Data in <see cref="Fields" /> changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FieldsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //// we could fine tune this
            //switch (e.Action)
            //{
            //    case NotifyCollectionChangedAction.Add:
            //        break;
            //    case NotifyCollectionChangedAction.Move:
            //        break;
            //    case NotifyCollectionChangedAction.Remove:
            //        break;
            //    case NotifyCollectionChangedAction.Replace:
            //        break;
            //    case NotifyCollectionChangedAction.Reset:
            //        break;
            //    default:
            //        throw new ArgumentOutOfRangeException();
            //}
            var freshList = sender as ObservableDictionary<Key, FieldModelController>;
            Debug.Assert(freshList != null);
            DocumentModel.Fields = freshList.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.GetId());

            // Update Local
            // Update Server
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
            if (Fields.ContainsKey(key))
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
            if (!Fields.ContainsKey(DashConstants.KeyStore.PrototypeKey))
                return null;

            // otherwise try to convert the field associated with the prototype key into a DocumentFieldModelController
            var documentFieldModelController =
                Fields[DashConstants.KeyStore.PrototypeKey] as DocumentFieldModelController;

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

            proto.Fields[key] = field;

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
        public FieldModelController GetField(Key key)
        {
            // search up the hiearchy starting at this for the first DocumentController which has the passed in key
            var firstProtoWithKeyOrNull = GetPrototypeWithFieldKey(key);

            return firstProtoWithKeyOrNull?.Fields[key];
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
            // create the child with all the same fields
            var delegateModel = new DocumentModel(DocumentModel.Fields, DocumentType);

            // create a controller for the child
            var delegateController = new DocumentController(delegateModel);

            // create and set a prototype field on the child, pointing to ourself
            var prototypeFieldModel = new DocumentModelFieldModel(DocumentModel);
            var prototypeFieldController = new DocumentFieldModelController(prototypeFieldModel);
            delegateController.SetField(DashConstants.KeyStore.PrototypeKey, prototypeFieldController, true);

            // add the delegate to our delegates field
            var currentDelegates = GetDelegates();
            currentDelegates.Documents.Add(delegateController);

            // return the now fully populated delegate
            return delegateController;
        }

        /// <summary>
        ///     Gets the delegates for this <see cref="DocumentController" /> or creates a delegates field
        ///     and returns it if no delegates field existed
        /// </summary>
        /// <returns></returns>
        public DocumentCollectionFieldModelController GetDelegates()
        {
            // see if we have a populated delegates field
            var currentDelegates = Fields.ContainsKey(DashConstants.KeyStore.DelegatesKey)
                ? Fields[DashConstants.KeyStore.DelegatesKey] as DocumentCollectionFieldModelController
                : null;

            // if not then populate it with a new list of documents
            if (currentDelegates == null)
                currentDelegates =
                    new DocumentCollectionFieldModelController(
                        new DocumentCollectionFieldModel(new List<DocumentModel>()));

            return currentDelegates;
        }

        public virtual void AddInputReference(Key fieldKey, ReferenceFieldModel reference)
        {
            //TODO Remove existing output references and add new output reference
            //if (InputReferences.ContainsKey(fieldKey))
            //{
            //    FieldModel fm = docEndpoint.GetFieldInDocument(InputReferences[fieldKey]);
            //    fm.RemoveOutputReference(new ReferenceFieldModel {DocId = Id, Key = fieldKey});
            //}
            GetField(fieldKey).InputReference = reference;
            ContentController.GetController<DocumentController>(reference.DocId).GetField(reference.FieldKey).FieldModelUpdatedEvent += DocumentController_FieldModelUpdatedEvent;
            Execute();
        }

        private void DocumentController_FieldModelUpdatedEvent(FieldModelController sender)
        {
            Execute();
        }

        private void Execute()
        {
            OperatorFieldModelController opField = GetField(OperatorDocumentModel.OperatorKey) as OperatorFieldModelController;
            if (opField == null)
            {
                return;
            }
            try
            {
                opField.Execute(this);//TODO Add Document fields updated in addition to the field updated event so that assigning to the field itself instead of data triggers updates
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
            foreach (KeyValuePair<Key, FieldModelController> fieldModelController in Fields)
            {
                yield return fieldModelController;
            }

            if (!ignorePrototype)
            {
                var prototype = GetPrototype();
                if (prototype != null)
                    foreach (var field in prototype.EnumFields())
                        yield return field;
            }
        }

        public FrameworkElement MakeAllViewUI()
        {
            var sp = new StackPanel();
            sp.Margin = new Thickness(10, 0, 0, 0);
            foreach (var f in EnumFields())
            {
                if (f.Value is ImageFieldModelController || f.Value is TextFieldModelController || f.Value is NumberFieldModelController)
                {
                    var hstack = new StackPanel() { Orientation = Orientation.Horizontal };
                    var label = new TextBlock() { Text = f.Key.Name + ": " };
                    var dBox = new DataBox(new ReferenceFieldModel(GetId(), f.Key), f.Value is ImageFieldModelController).Document;

                    hstack.Children.Add(label);
                    foreach (var ele in dBox.MakeViewUI())
                    {
                        ele.MaxWidth = 200;
                        hstack.Children.Add(ele);
                    }
                    sp.Children.Add(hstack);
                }
                else if (f.Value is DocumentFieldModelController)
                {
                    var fieldDoc = (f.Value as DocumentFieldModelController).Data;
                    var docEles = fieldDoc.MakeAllViewUI();
                    if (docEles != null)
                        sp.Children.Add(docEles);
                }
                else if (f.Value is DocumentCollectionFieldModelController)
                {
                    foreach (var fieldDoc in (f.Value as DocumentCollectionFieldModelController).Documents)
                    {
                        var docEles = fieldDoc.MakeAllViewUI();
                        if (docEles != null)
                            sp.Children.Add(docEles);
                    }
                }
            }
            return sp;
        }
        public List<FrameworkElement> MakeViewUI()
        {
            var uieles = new List<FrameworkElement>();

            if (DocumentType == TextingBox.DocumentType)
            {
                uieles.AddRange(TextingBox.MakeView(this));
            }
            else if (DocumentType == ImageBox.DocumentType)
            {
                uieles.AddRange(ImageBox.MakeView(this));
            }
            else if (DocumentType == StackingPanel.DocumentType)
            {
                uieles.AddRange(StackingPanel.MakeView(this));
            }
            else if (DocumentType == GenericCollection.DocumentType)
            {
                uieles.AddRange(GenericCollection.MakeView(this));
            }
            else if (DocumentType == OperatorBox.DocumentType)
            {
                uieles.AddRange(OperatorBox.MakeView(this));
            }
            else // if document is not a known UI View, then see if it contains any documents with known UI views
            {
                var fieldModelController = GetField(DashConstants.KeyStore.LayoutKey) as FieldModelController;
                if (fieldModelController != null)
                {
                    while (fieldModelController is ReferenceFieldModelController)
                    {
                        var refFM = (fieldModelController as ReferenceFieldModelController).ReferenceFieldModel;
                        fieldModelController = ContentController.GetController<DocumentController>(refFM.DocId).GetField(refFM.FieldKey);
                    }
                    if (fieldModelController is DocumentFieldModelController)
                    {
                        var fieldDoc = (fieldModelController as DocumentFieldModelController).Data;
                        var docEles = fieldDoc.MakeViewUI();
                        if (docEles != null)
                            uieles.AddRange(docEles);
                    }
                    else if (fieldModelController is DocumentCollectionFieldModelController)
                    {
                        foreach (var fieldDoc in (fieldModelController as DocumentCollectionFieldModelController).Documents)
                        {
                            var docEles = fieldDoc.MakeViewUI();
                            if (docEles != null)
                                uieles.AddRange(docEles);
                        }
                    }
                }
            }
            return uieles;
        }
    }
}