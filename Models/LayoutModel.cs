using Dash;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Dash.Models;

namespace Dash
{
    /// <summary>
    /// A mapping of keys to ElementModels. Has a Type field for which the layout is valid.
    /// </summary>
    public class LayoutModel
    {
        /// <summary>
        /// A dictionary of keys to ElementModels.
        /// </summary>
        public Dictionary<string, TemplateModel> Fields = new Dictionary<string, TemplateModel>();

        /// <summary>
        /// The type for which this layout is valid.
        /// </summary>
        public string DocumentType { get; set; }

        /// <summary>
        /// Initializes a LayoutModel with a given dictionary and type.
        /// </summary>
        /// <param name="fields"></param> Should contain the keys for which this layout is defined.
        /// <param name="type"></param> The string type for which this layout is valid.
        public LayoutModel(IDictionary<string, TemplateModel> fields, string type)
        {
            if (type == null)
            {
                throw new ArgumentNullException();
            }

            Fields = new Dictionary<string, TemplateModel>(fields);
            Fields.Add("Type", new TextTemplateModel(-10000, -1000, FontWeights.Bold, TextWrapping.NoWrap, Visibility.Collapsed));
            DocumentType = type;
        }

        /// <summary>
        /// A helpe method for the prototype. This will be removed!
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        static public LayoutModel Food2ForkRecipeModel(DocumentModel doc)
        {
            Dictionary<string, TemplateModel> fields = new Dictionary<string, TemplateModel>();
            fields["publisher"] = new TextTemplateModel(10, 10, FontWeights.Normal, TextWrapping.Wrap, Visibility.Visible);
            fields["source_url"] = new TextTemplateModel(10, 250, FontWeights.Normal, TextWrapping.NoWrap, Visibility.Visible);
            fields["title"] = new TextTemplateModel(30, 115, FontWeights.Bold, TextWrapping.Wrap, Visibility.Visible);
            fields["f2f_url"] = new TextTemplateModel(10, 275, FontWeights.Normal, TextWrapping.NoWrap, Visibility.Visible);

            Debug.Assert(doc.DocumentType.Equals("recipes"));
            return new LayoutModel(fields, doc.DocumentType);
        }

        /// <summary>
        /// A helper method for the prototype. This will be removed!
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        static public LayoutModel UmpireModel(DocumentModel doc)
        {
            Dictionary<string, TemplateModel> fields = new Dictionary<string, TemplateModel>();

            fields["name"] = new TextTemplateModel(10, 10, FontWeights.Bold, TextWrapping.Wrap);
            fields["experience"] = new TextTemplateModel(10, 250, FontWeights.Normal);

            Debug.Assert(doc.DocumentType.Equals("Umpires"));

            return new LayoutModel(fields, doc.DocumentType);
        }

        static public LayoutModel OneImageModel(DocumentModel doc)
        {
            Dictionary<string, TemplateModel> fields = new Dictionary<string, TemplateModel>();
            fields["content"] = new ImageTemplateModel(5, 20, 100, 100);

            Debug.Assert(doc.DocumentType.Equals("oneimage"));
            return new LayoutModel(fields, doc.DocumentType);
        }

        static public LayoutModel TwoImagesAndTextModel(DocumentModel doc)
        {
            Dictionary<string, TemplateModel> fields = new Dictionary<string, TemplateModel>();
            fields["content2"] = new ImageTemplateModel(5, 20, 100, 100);
            fields["content"] = new ImageTemplateModel(5, 140, 100, 100);
            fields["text"] = new TextTemplateModel(5, 260, FontWeights.Normal);

            Debug.Assert(doc.DocumentType.Equals("twoimages"));
            return new LayoutModel(fields, doc.DocumentType);
        }
    }
}
