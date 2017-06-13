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

            Fields = new Dictionary<Key, FieldModel>(fields);
        }

        public DocumentModel()
        {
        }

        // Hard coded document models 

        public static DocumentModel UmpireDocumentModel()
        {
            // get access to controllers, 
            var keyController = App.Instance.Container.GetRequiredService<KeyController>();
            var typeController = App.Instance.Container.GetRequiredService<TypeController>();

            // create fields for document
            var fields = new Dictionary<Key, FieldModel>();

            // create each of the keys //TODO this is not going to work in the real world
            var nameKey = keyController.CreateKeyAsync("name");
            fields[nameKey] = new TextFieldModel("Mr.U");
            var experienceKey = keyController.CreateKeyAsync("experience");
            fields[experienceKey] = new TextFieldModel("100 years");

            // create the type //TODO this is not going to work in the real world
            var typeKey = typeController.CreateTypeAsync("umpire");
            return new DocumentModel(fields, typeKey);
        }

        public static DocumentModel Food2ForkRecipeDocumentModel()
        {
            // get access to controllers, 
            var keyController = App.Instance.Container.GetRequiredService<KeyController>();
            var typeController = App.Instance.Container.GetRequiredService<TypeController>();

            // create fields for document
            var fields = new Dictionary<Key, FieldModel>();

            // create each of the keys //TODO this is not going to work in the real world
            var publisherKey = keyController.CreateKeyAsync("publisher");
            fields[publisherKey] = new TextFieldModel("Penguin");
            var sourceKey = keyController.CreateKeyAsync("source_url");
            fields[sourceKey] = new TextFieldModel("httpthisisaurl.com");
            var titleKey = keyController.CreateKeyAsync("title");
            fields[titleKey] = new TextFieldModel("good food");

            // create the type //TODO this is not going to work in the real world
            var typeKey = typeController.CreateTypeAsync("recipes");
            return new DocumentModel(fields, typeKey);
        }

        public static DocumentModel OneImage()
        {
            // get access to controllers, 
            var keyController = App.Instance.Container.GetRequiredService<KeyController>();
            var typeController = App.Instance.Container.GetRequiredService<TypeController>();

            // create fields for document
            var fields = new Dictionary<Key, FieldModel>();

            var contentKey = keyController.CreateKeyAsync("content");
            fields[contentKey] = new ImageFieldModel(new Uri("ms-appx://Dash/Assets/cat.jpg"));

            var type = typeController.CreateTypeAsync("oneimage");

            return new DocumentModel(fields, type);
        }

        public static DocumentModel TwoImagesAndText()
        {
            // get access to controllers, 
            var keyController = App.Instance.Container.GetRequiredService<KeyController>();
            var typeController = App.Instance.Container.GetRequiredService<TypeController>();

            // create fields for document
            var fields = new Dictionary<Key, FieldModel>();

            var contentKey = keyController.CreateKeyAsync("content");
            fields[contentKey] = new ImageFieldModel(new Uri("ms-appx://Dash/Assets/cat.jpg"));
            var content2Key = keyController.CreateKeyAsync("content2");
            fields[content2Key] = new ImageFieldModel(new Uri("ms-appx://Dash/Assets/cat2.jpeg"));
            var textKey = keyController.CreateKeyAsync("text");
            fields[textKey] = new TextFieldModel("These are 2 cats");

            var type = typeController.CreateTypeAsync("twoimages");

            return new DocumentModel(fields, type);
        }

        
        
    }
}
