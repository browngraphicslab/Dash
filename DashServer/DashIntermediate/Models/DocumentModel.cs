using Dash;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
