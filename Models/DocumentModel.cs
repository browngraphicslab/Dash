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
        /// A dictionary of keys to ElementModels.
        /// </summary>
        public Dictionary<string, object> Fields = new Dictionary<string, Object>();

        /// <summary>
        /// The type of this document.
        /// </summary>
        public string DocumentType { get; set; }

        /// <summary>
        /// Initializes a document with given data and type.
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="type"></param>
        public DocumentModel(IDictionary<string, object> fields, string type)
        {
            if (type == null)
            {
                throw new ArgumentNullException();
            }
            DocumentType = type;
            foreach (var val in fields)
                if (!Fields.ContainsKey(val.Key))
                    Fields.Add(val.Key, fields[val.Key]);
        }


        public static DocumentModel UmpireDocumentModel()
        {
            Dictionary<string, object> fields = new Dictionary<string, object>();
            fields["name"] = "Mr.U";
            fields["experience"] = "100 years"; 
            return new DocumentModel(fields, "Umpires");
        }

        public static DocumentModel Food2ForkRecipeDocumentModel()
        {
            Dictionary<string, object> fields = new Dictionary<string, object>();
            fields["publisher"] = "Penguin"; 
            fields["source_url"] = "httpthisisaurl.com";
            fields["title"] = "good food";
            fields["f2f_url"] = "thisisaf2furl.com";
            return new DocumentModel(fields, "recipes");
        }
    }
}
