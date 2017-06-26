using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using DashShared;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

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

        static public Key DataKey = new Key("Data");
        static public Key FontWeightKey = new Key("FontWeight");
        public List<FrameworkElement> MakeViewUI()
        {
            var uieles = new List<FrameworkElement>();

            if (DocumentType == new DocumentType("TextBox", "TextBox"))
            {
                var fw = GetField(FontWeightKey);
                var fontWeight = fw is TextFieldModelController ? ((fw as TextFieldModelController).Data == "Bold" ? Windows.UI.Text.FontWeights.Bold : Windows.UI.Text.FontWeights.Normal) : Windows.UI.Text.FontWeights.Normal;
                var data = GetField(DataKey) ?? null;
                if (data != null)
                    uieles.AddRange(new TextTemplateModel(0, 0, fontWeight, Windows.UI.Xaml.TextWrapping.NoWrap, Windows.UI.Xaml.Visibility.Visible).MakeViewUI(data, this));
            }
            if (DocumentType == DashConstants.DocumentTypeStore.ImageBoxDocumentType)
            {
                var data = GetField(DataKey) ?? null;
                if (data != null)
                    uieles.AddRange(new ImageTemplateModel(0, 0).MakeViewUI(data, this));
            }
            if (DocumentType == DashConstants.DocumentTypeStore.CollectionDocumentType)
            {
                var data = GetField(DashConstants.KeyStore.CollectionDocumentsListFieldKey) ?? null;
                if (data != null)
                    uieles.AddRange(new DocumentCollectionTemplateModel(0, 0).MakeViewUI(data, this));
            }
            if (DocumentType == new DocumentType("StackView", "StackView"))
            {
                foreach (var f in EnumFields())
                    //if (f.Value is DocumentModelFieldModelController)
                    //{
                    //    var fieldDoc = (f.Value as DocumentModelFieldModel).Data;
                    //    var tt = new TranslateTransform();
                    //    tt.Y = fieldDoc.DocumentType == new DocumentType("ImageBox", "ImageBox") ? 500 : 20;
                    //    var fieldEles = fieldDoc.MakeViewUI();
                    //    if (fieldEles != null)
                    //        foreach (var ele in fieldEles)
                    //        {
                    //            var tg = new TransformGroup();
                    //            tg.Children.Add(ele.RenderTransform);
                    //            tg.Children.Add(tt);

                    //            ele.RenderTransform = tg;
                    //            uieles.Add(ele);
                    //        }
                    //}
                    //else 
                    if (f.Value is DocumentCollectionFieldModelController)
                    {
                        var fieldDocs = (f.Value as DocumentCollectionFieldModelController).Documents;
                        foreach (var fdoc in fieldDocs)
                        {
                            var ues = fdoc.MakeViewUI();
                            if (ues != null)
                                foreach (var ele in ues)
                                {
                                    var tt = new TranslateTransform();
                                    tt.Y = ele.Height;
                                    var tg = new TransformGroup();
                                    tg.Children.Add(ele.RenderTransform);
                                    tg.Children.Add(tt);

                                    ele.RenderTransform = tg;
                                    uieles.Add(ele);
                                }
                        }
                    }
            }
            else // FreeFormCollectionDocumentType
            {
                foreach (var f in EnumFields())
                    if (f.Value is DocumentFieldModelController)
                    {
                        var fieldDoc = (f.Value as DocumentFieldModelController).Data;
                        var fieldEles = fieldDoc.MakeViewUI();
                        if (fieldEles != null)
                            foreach (var ele in fieldEles)
                            {
                                uieles.Add(ele);
                            }
                    }
                    else if (f.Value is DocumentCollectionFieldModelController)
                    {
                        var fieldDocs = (f.Value as DocumentCollectionFieldModelController).Documents;
                        foreach (var fdoc in fieldDocs)
                        {
                            var ues = fdoc.MakeViewUI();
                            if (ues != null)
                                foreach (var ele in ues)
                                {
                                    uieles.Add(ele);
                                }
                        }
                    }
            }
            return uieles;
        }
        }
}