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
using DashShared;
using Microsoft.Extensions.DependencyInjection;

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
        public Dictionary<Key, TemplateModel> Fields;

        /// <summary>
        /// The type for which this layout is valid.
        /// </summary>
        public DocumentType DocumentType { get; set; }

        /// <summary>
        /// Initializes a LayoutModel with a given dictionary and type.
        /// </summary>
        /// <param name="fields"></param> Should contain the keys for which this layout is defined.
        /// <param name="type"></param> The string type for which this layout is valid.
        public LayoutModel(IDictionary<Key, TemplateModel> fields, DocumentType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException();
            }

            Fields = new Dictionary<Key, TemplateModel>(fields);
            //TODO Add this back in
            //Fields.Add("Type", new TextTemplateModel(-10000, -1000, FontWeights.Bold, TextWrapping.NoWrap, Visibility.Collapsed));
            DocumentType = type;
        }

        /// <summary>
        /// A helpe method for the prototype. This will be removed!
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        static public LayoutModel Food2ForkRecipeModel(DocumentModel doc)
        {
            var keyController = App.Instance.Container.GetRequiredService<KeyController>();

            Dictionary<Key, TemplateModel> fields = new Dictionary<Key, TemplateModel>();
            var keys = doc.Fields.Keys;
            //TODO REALLY BAD CODE
            var publisherKey = keys.Where(key => key.Name.Equals("publisher")).ElementAt(0);
            fields[publisherKey] = new TextTemplateModel(10, 10, FontWeights.Normal, TextWrapping.Wrap,
                Visibility.Visible);
            var sourceUrlKey = keys.Where(key => key.Name.Equals("source_url")).ElementAt(0);
            fields[sourceUrlKey] = new TextTemplateModel(10, 250, FontWeights.Normal, TextWrapping.NoWrap,
                Visibility.Visible);
            var titleKey = keys.Where(key => key.Name.Equals("title")).ElementAt(0);
            fields[titleKey] = new TextTemplateModel(30, 115, FontWeights.Bold, TextWrapping.Wrap, Visibility.Visible);
            var f2fKey = keys.Where(key => key.Name.Equals("f2f_url")).ElementAt(0);
            fields[f2fKey] = new TextTemplateModel(10, 275, FontWeights.Normal, TextWrapping.NoWrap, Visibility.Visible);

            Debug.Assert(doc.DocumentType.Type.Equals("recipes"));
            return new LayoutModel(fields, doc.DocumentType);
        }

        /// <summary>
        /// A helper method for the prototype. This will be removed!
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public static LayoutModel UmpireModel(DocumentModel doc)
        {
            var keyController = App.Instance.Container.GetRequiredService<KeyController>();

            Dictionary<Key, TemplateModel> fields = new Dictionary<Key, TemplateModel>();
            var keys = doc.Fields.Keys;
            //TODO REALLY BAD CODE
            var nameKey = keys.Where(key => key.Name.Equals("name")).ElementAt(0);
            fields[nameKey] = new TextTemplateModel(10, 10, FontWeights.Bold, TextWrapping.Wrap);
            var experienceKey = keys.Where(key => key.Name.Equals("experience")).ElementAt(0);
            fields[experienceKey] = new TextTemplateModel(10, 250, FontWeights.Normal);

            Debug.Assert(doc.DocumentType.Type.Equals("Umpires"));

            return new LayoutModel(fields, doc.DocumentType);
        }

        public static LayoutModel OneImageModel(DocumentModel doc)
        {
            var keyController = App.Instance.Container.GetRequiredService<KeyController>();

            Dictionary<Key, TemplateModel> fields = new Dictionary<Key, TemplateModel>();
            var keys = doc.Fields.Keys;
            //TODO REALLY BAD CODE
            var contentKey = keys.Where(key => key.Name.Equals("content")).ElementAt(0);
            fields[contentKey] = new ImageTemplateModel(5, 20, 100, 100);

            Debug.Assert(doc.DocumentType.Type.Equals("oneimage"));
            return new LayoutModel(fields, doc.DocumentType);
        }

        public static LayoutModel TwoImagesAndTextModel(DocumentModel doc)
        {
            var keyController = App.Instance.Container.GetRequiredService<KeyController>();

            Dictionary<Key, TemplateModel> fields = new Dictionary<Key, TemplateModel>();
            var keys = doc.Fields.Keys;
            //TODO REALLY BAD CODE
            var contentKey = keys.Where(key => key.Name.Equals("content")).ElementAt(0);
            fields[contentKey] = new ImageTemplateModel(5, 140, 100, 100);
            var content2Key = keys.Where(key => key.Name.Equals("content2")).ElementAt(0);
            fields[content2Key] = new ImageTemplateModel(5, 20, 100, 100);
            var textKey = keys.Where(key => key.Name.Equals("text")).ElementAt(0);
            fields[textKey] = new TextTemplateModel(5, 260, FontWeights.Normal);

            Debug.Assert(doc.DocumentType.Type.Equals("twoimages"));
            return new LayoutModel(fields, doc.DocumentType);
        }

        public static LayoutModel ExampleApiObject(DocumentModel doc)
        {
            Dictionary<Key, TemplateModel> fields = new Dictionary<Key, TemplateModel>();

            var Keys = doc.Fields.Keys;
            fields[Keys.First(k => k.Name == "id")] = new TextTemplateModel(0, 20, FontWeights.Normal);
            fields[Keys.First(k => k.Name == "first_name")] = new TextTemplateModel(0, 60, FontWeights.Normal);
            fields[Keys.First(k => k.Name == "last_name")] = new TextTemplateModel(0, 100, FontWeights.Normal);
            fields[Keys.First(k => k.Name == "email")] = new TextTemplateModel(0, 140, FontWeights.Normal);
            fields[Keys.First(k => k.Name == "gender")] = new TextTemplateModel(0, 180, FontWeights.Normal);
            fields[Keys.First(k => k.Name == "ip_address")] = new TextTemplateModel(0, 220, FontWeights.Normal);

            return new LayoutModel(fields, doc.DocumentType);
        }

        public static LayoutModel ExampleCollectionModel(DocumentModel doc)
        {
            Dictionary<Key, TemplateModel> fields = new Dictionary<Key, TemplateModel>();

            var Keys = doc.Fields.Keys;
            fields[Keys.First(k => k.Name == "documents")] = new DocumentCollectionTemplateModel(0, 0, 400, 400);

            return new LayoutModel(fields, doc.DocumentType);
        }
    }
}
