using Dash;
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

namespace Dash
{
    /// <summary>
    /// A mapping of keys to FieldModels.
    /// </summary>
    public class DocumentModel : AuthorizableEntityBase
    {

        /// <summary>
        /// A dictionary of keys to FieldModels.
        /// </summary>
        public Dictionary<Key, FieldModel> Fields;

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

            Fields = new Dictionary<Key, FieldModel>(fields);
        }

        public DocumentModel()
        {
        }

        protected virtual void OnDocumentFieldUpdated(ReferenceFieldModel fieldReference)
        {
            DocumentFieldUpdated?.Invoke(fieldReference);
        }

        // Hard coded document models 

        public static DocumentModel UmpireDocumentModel()
        {
            // get access to controllers, 
            var docController = App.Instance.Container.GetRequiredService<DocumentController>();
            var keyController = App.Instance.Container.GetRequiredService<KeyController>();

            // create fields for document
            var fields = new Dictionary<Key, FieldModel>();

            // create each of the keys //TODO this is not going to work in the real world
            var nameKey = keyController.CreateKeyAsync("name");
            fields[nameKey] = new TextFieldModel("Mr.U");
            var experienceKey = keyController.CreateKeyAsync("experience");
            fields[experienceKey] = new TextFieldModel("100 years");

            // create the type //TODO this is not going to work in the real world
            var dm = docController.CreateDocumentAsync("Umpires");
            dm.Fields = fields;
            return dm;
        }

        public static DocumentModel Food2ForkRecipeDocumentModel()
        {
            // get access to controllers, 
            var docController = App.Instance.Container.GetRequiredService<DocumentController>();
            var keyController = App.Instance.Container.GetRequiredService<KeyController>();

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
            dm.Fields = fields;
            return dm;
        }

        public static DocumentModel OneImage()
        {
            // get access to controllers, 
            var docController = App.Instance.Container.GetRequiredService<DocumentController>();
            var keyController = App.Instance.Container.GetRequiredService<KeyController>();

            // create fields for document
            var fields = new Dictionary<Key, FieldModel>();

            var contentKey = keyController.CreateKeyAsync("content");
            fields[contentKey] = new ImageFieldModel(new Uri("ms-appx://Dash/Assets/cat.jpg"));

            var dm = docController.CreateDocumentAsync("oneimage");
            dm.Fields = fields;
            return dm;
        }

        public static DocumentModel TwoImagesAndText()
        {
            // get access to controllers, 
            var docController = App.Instance.Container.GetRequiredService<DocumentController>();
            var keyController = App.Instance.Container.GetRequiredService<KeyController>();

            // create fields for document
            var fields = new Dictionary<Key, FieldModel>();

            var contentKey = keyController.CreateKeyAsync("content");
            fields[contentKey] = new ImageFieldModel(new Uri("ms-appx://Dash/Assets/cat.jpg"));
            var content2Key = keyController.CreateKeyAsync("content2");
            fields[content2Key] = new ImageFieldModel(new Uri("ms-appx://Dash/Assets/cat2.jpeg"));
            var textKey = keyController.CreateKeyAsync("text");
            fields[textKey] = new TextFieldModel("These are 2 cats");

            var dm = docController.CreateDocumentAsync("twoimages");
            dm.Fields = fields;
            return dm;
        }

        
        
    }
}
