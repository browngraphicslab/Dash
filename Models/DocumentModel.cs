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

namespace Dash
{
    /// <summary>
    /// A mapping of keys to objects.
    /// </summary>
    public class DocumentModel
    {
        /// <summary>
        /// A dictionary of keys to objects.
        /// </summary>
        public Dictionary<string, FieldModel> Fields = new Dictionary<string, FieldModel>();

        /// <summary>
        /// The type of this document.
        /// </summary>
        public string DocumentType
        {
            get { return (Fields["Type"] as TextFieldModel).Data; }
            set { (Fields["Type"] as TextFieldModel).Data = value; }
        }

        /// <summary>
        /// Initializes a document with given data and type.
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="type"></param>
        public DocumentModel(IDictionary<string, FieldModel> fields, string type)
        {
            if (type == null)
            {
                throw new ArgumentNullException();
            }

            Fields = new Dictionary<string, FieldModel>(fields);
            Fields["Type"] = new TextFieldModel(type);
            //DocumentType = type;
        }


        public static DocumentModel UmpireDocumentModel()
        {
            Dictionary<string, FieldModel> fields = new Dictionary<string, FieldModel>();
            fields["name"] = new TextFieldModel("Mr.U");
            fields["experience"] = new TextFieldModel("100 years"); 
            return new DocumentModel(fields, "Umpires");
        }

        public static DocumentModel Food2ForkRecipeDocumentModel()
        {
            Dictionary<string, FieldModel> fields = new Dictionary<string, FieldModel>();
            fields["publisher"] = new TextFieldModel("Penguin"); 
            fields["source_url"] = new TextFieldModel("httpthisisaurl.com");
            fields["title"] = new TextFieldModel("good food");
            fields["f2f_url"] = new TextFieldModel("thisisaf2furl.com");
            return new DocumentModel(fields, "recipes");
        }

        public static DocumentModel OneImage()
        {
            Dictionary<string, FieldModel> fields = new Dictionary<string, FieldModel>();
            fields["content"] = new ImageFieldModel(new Uri("ms-appx://Dash/Assets/cat.jpg"));
            return new DocumentModel(fields, "oneimage");
        }

        public static DocumentModel TwoImagesAndText()
        {
            Dictionary<string, FieldModel> fields = new Dictionary<string, FieldModel>();
            fields["content"] = new ImageFieldModel(new Uri("ms-appx://Dash/Assets/cat.jpg"));
            fields["content2"] = new ImageFieldModel(new Uri("ms-appx://Dash/Assets/cat2.jpeg"));
            fields["text"] = new TextFieldModel("These are 2 cats");
            return new DocumentModel(fields, "twoimages");
        }
    }
}
