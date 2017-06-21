﻿using Dash;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.UI.Text;
using Windows.UI.Xaml;
using DashShared;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Dash.Models;

namespace Dash
{
    /// <summary>
    /// A mapping of keys to FieldModels.
    /// </summary>
    public class DocumentModel : AuthorizableEntityBase
    {

        public static Key LayoutKey = new Key("4CD28733-93FB-4DF4-B878-289B14D5BFE1", "Layout");

        /// <summary>
        /// A dictionary of keys to FieldModels.
        /// </summary>
        Dictionary<Key, FieldModel> Fields;

        /// <summary>
        /// The type of this document.
        /// </summary>
        public DocumentType DocumentType { get; set; }
        //{
        //    get { return (Fields["Type"] as TextFieldModel).Data; }
        //    set { (Fields["Type"] as TextFieldModel).Data = value; }
        //}


        public delegate void FieldUpdatedEvent(ReferenceFieldModel fieldReference);

        public event FieldUpdatedEvent DocumentFieldUpdated;

        /// <summary>
        /// Initializes a document with given data and type.
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="type"></param>
        public DocumentModel(IDictionary<Key, FieldModel> fields, DocumentType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException();
            }
            DocumentType = type;
            SetFields(fields);
        }

        /// <summary>
        /// Sets the value of the field indexed by key. If key is not a valid index in the
        /// fields, this function adds it or throws an error.
        /// </summary>
        /// <param name="key">key index of field to update</param>
        /// <param name="field">FieldModel to update to</param>
        public void SetField(Key key, FieldModel field)
        {
                Fields[key] = field;
                OnDocumentFieldUpdated(new ReferenceFieldModel(Id, key));
                var delegates = Field(GetFieldKeyByName("Delegates")) as DocumentCollectionFieldModel;
                if (delegates != null)
                    foreach (var d in delegates.EnumDocuments())
                        d.OnDocumentFieldUpdated(new ReferenceFieldModel(Id, key));
            
        }

        /// <summary>
        /// Sets all of the document's fields to a given Dictionary of Key FieldModel
        /// pairs. Overwrites existing fields.
        /// </summary>
        /// <param name="fields"></param>
        public void SetFields(IDictionary<Key,FieldModel> fields)
        {
            Fields = new Dictionary<Key, FieldModel>();
            foreach (var f in fields)
                SetField(f.Key, f.Value, true);
        }

        public DocumentModel()
        {
            Fields = new Dictionary<Key, FieldModel>();
        }

        static public Key GetFieldKeyByName(string name)
        {
            var keyController = App.Instance.Container.GetRequiredService<KeyEndpoint>();
            var key = keyController.GetKeyAsync(name);
            if (key == null)
                key = keyController.CreateKeyAsync(name);
            return key;
        }

        /// <summary>
        /// Creates a delegate (child) of the given document that inherits all the fields of the prototype (parent)
        /// </summary>
        /// <returns></returns>
        public DocumentModel MakeDelegate()
        {
            var docController = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
            var dm = docController.CreateDocumentAsync(DocumentType);
            dm.SetField(GetFieldKeyByName("Parent"), new DocumentModelFieldModel(this));
            var currentDelegates = Field(GetFieldKeyByName("Delegates")) as DocumentCollectionFieldModel;
            if (currentDelegates == null)
                currentDelegates = new DocumentCollectionFieldModel(new List<DocumentModel>());
            currentDelegates.AddDocumentModel(dm);
            SetField(GetFieldKeyByName("Delegates"), currentDelegates);
            return dm;
        }

        public DocumentModel GetPrototype()
        {

            if (Fields.ContainsKey(GetFieldKeyByName("Parent")))
                return (Fields[GetFieldKeyByName("Parent")] as DocumentModelFieldModel).Data;
            return null;
        }

        public IEnumerable<KeyValuePair<Key, FieldModel>> EnumFields()
        {
            foreach (var field in Fields)
                yield return field;

            var prototype = GetPrototype();
            if (prototype != null)
                foreach (var field in prototype.EnumFields())
                    yield return field;
        }


        /// <summary>
        /// returns the fieldModel for the specified key by looking first in the delegate, and then sequentially in all prototypes
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public FieldModel Field(Key key)
        {
            if (Fields.ContainsKey(key))
                return Fields[key];
            if (Fields.ContainsKey(GetFieldKeyByName("Parent")))
            {
                var parent = Fields[GetFieldKeyByName("Parent")] as DocumentModelFieldModel;
                if (parent != null)
                    return parent.Data.Field(key);
            }
            return null;
        }

        /// <summary>
        /// sets the fieldModel for a specified key by first trying to find the field in the document, then in each prototype.
        /// if the field does not exist anywhere, it is created in this document, otherwise the found field is modified.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="force"></param>
        /// <returns></returns>
        public bool SetField(Key key, FieldModel value, bool force = true)
        {
            if (Fields.ContainsKey(key)) {
                Fields[key] = value;
                return true;
            }
            if (Fields.ContainsKey(GetFieldKeyByName("Parent")))
            {
                var parent = Fields[GetFieldKeyByName("Parent")] as DocumentModelFieldModel;
                if (parent != null && parent.Data.SetField(key, value, false))
                    return true;
            }
            if (force)
            {
                Fields[key] = value;
                return true;
            }
            return false;
        }

        protected virtual void OnDocumentFieldUpdated(ReferenceFieldModel fieldReference)
        {
            DocumentFieldUpdated?.Invoke(fieldReference);
        }

        // Hard coded document models 

        public static DocumentModel UmpireDocumentModel()
        {
            // get access to controllers, 
            var docController = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
            var keyController = App.Instance.Container.GetRequiredService<KeyEndpoint>();

            // create fields for document
            var fields = new Dictionary<Key, FieldModel>();

            // create each of the keys //TODO this is not going to work in the real world
            var nameKey = keyController.CreateKeyAsync("name");
            fields[nameKey] = new TextFieldModel("Mr.U");
            var experienceKey = keyController.CreateKeyAsync("experience");
            fields[experienceKey] = new TextFieldModel("100 years");

            // create the type //TODO this is not going to work in the real world
            var dm = docController.CreateDocumentAsync("Umpires");
            dm.SetFields(fields);
            return dm;
        }

        public static DocumentModel Food2ForkRecipeDocumentModel()
        {
            // get access to controllers, 
            var docController = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
            var keyController = App.Instance.Container.GetRequiredService<KeyEndpoint>();


            // create fields for document
            var fields = new Dictionary<Key, FieldModel>();

            // create each of the keys //TODO this is not going to work in the real world
            var publisherKey = keyController.CreateKeyAsync("publisher");
            fields[publisherKey] = new TextFieldModel("Penguin");
            var sourceKey = keyController.CreateKeyAsync("source_url");
            fields[sourceKey] = new TextFieldModel("httpthisisaurl.com");
            var titleKey = keyController.CreateKeyAsync("title");
            fields[titleKey] = new TextFieldModel("good food");
            var f2fKey = keyController.CreateKeyAsync("f2f_url");
            fields[f2fKey] = new TextFieldModel("f2furl.com");

            // create the type //TODO this is not going to work in the real world
            var dm = docController.CreateDocumentAsync("recipes");
            dm.SetFields(fields);
            return dm;
        }

        public static DocumentModel OneImage()
        {
            // get access to controllers, 
            var docController = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
            var keyController = App.Instance.Container.GetRequiredService<KeyEndpoint>();

            // create fields for document
            var fields = new Dictionary<Key, FieldModel>();

            var contentKey = keyController.CreateKeyAsync("content");
            fields[contentKey] = new ImageFieldModel(new Uri("ms-appx://Dash/Assets/cat.jpg"));
            var dm = docController.CreateDocumentAsync("oneimage");
            dm.SetFields(fields);
            return dm;
        }

        public static DocumentModel TwoImagesAndText()
        {
            // get access to controllers, 
            var docController = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
            var keyController = App.Instance.Container.GetRequiredService<KeyEndpoint>();

            // create fields for document
            var fields = new Dictionary<Key, FieldModel>();

            var contentKey = DocumentModel.GetFieldKeyByName("content"); //  keyController.CreateKeyAsync("content");
            fields[contentKey] = new ImageFieldModel(new Uri("ms-appx://Dash/Assets/cat.jpg"));
            var content2Key = DocumentModel.GetFieldKeyByName("content2"); // keyController.CreateKeyAsync("content2");
            fields[content2Key] = new ImageFieldModel(new Uri("ms-appx://Dash/Assets/cat2.jpeg"));
            var textKey = DocumentModel.GetFieldKeyByName("text"); //  keyController.CreateKeyAsync("text");
            fields[textKey] = new TextFieldModel("These are 2 cats");

            var dm = docController.CreateDocumentAsync("twoimages");
            dm.SetFields(fields);
            return dm;
        }


        public static async Task<DocumentModel> CollectionExample()
        {
            var apiSource = App.Instance.Container.GetRequiredService<ExampleApiSource>();
            await apiSource.Initialize();

            var docController = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
            var keyController = App.Instance.Container.GetRequiredService<KeyEndpoint>();

            // create fields for document
            var fields = new Dictionary<Key, FieldModel>();

            var documentsKey = keyController.CreateKeyAsync("documents");
            fields[documentsKey] = new DocumentCollectionFieldModel(apiSource.GetDocumentsAsync());

            var dm = docController.CreateDocumentAsync("collection_example");
            dm.SetFields(fields);
            return dm;
        }


        public static async Task<DocumentModel> PricePerSquareFootExample()
        {
            var apiSource = App.Instance.Container.GetRequiredService<PricePerSquareFootApi>();
            await apiSource.Initialize();

            var docController = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
            var keyController = App.Instance.Container.GetRequiredService<KeyEndpoint>();

            // create fields for document
            var fields = new Dictionary<Key, FieldModel>();

            var documentsKey = keyController.CreateKeyAsync("documents");
            //fields[documentsKey] = new DocumentCollectionFieldModel(apiSource.GetDocumentsAsync());



            //var dm = docController.CreateDocumentAsync("collection_example");
            //dm.Fields = fields;

            return apiSource.GetDocumentsAsync().First();
        }
    }
}
