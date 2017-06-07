using Dash;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Text;
using Windows.UI.Xaml;

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
        public Dictionary<string, ElementModel> Fields = new Dictionary<string, ElementModel>();

        /// <summary>
        /// The type for which this layout is valid.
        /// </summary>
        public string DocumentType { get; set; }

        /// <summary>
        /// Initializes a LayoutModel with a given dictionary and type.
        /// </summary>
        /// <param name="fields"></param> Should contain the keys for which this layout is defined.
        /// <param name="type"></param> The string type for which this layout is valid.
        public LayoutModel(IDictionary<string, object> fields, string type)
        {
            if (type == null)
            {
                throw new ArgumentNullException();
            }
            Fields.Add("Type", new ElementModel(-10000, -1000, FontWeights.Bold, TextWrapping.NoWrap, Visibility.Collapsed));
            DocumentType = type;
            foreach (var val in fields)
                if (!Fields.ContainsKey(val.Key))
                    Fields.Add(val.Key, new ElementModel(-10000, -1000, FontWeights.Bold, TextWrapping.NoWrap, Visibility.Collapsed));
        }

        /// <summary>
        /// A helpe method for the prototype. This will be removed!
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        static public LayoutModel Food2ForkRecipeModel(DocumentModel doc)
        {
            var dom = new LayoutModel(doc.Fields, "recipes");
            dom.Fields["publisher"].Left = 10;
            dom.Fields["publisher"].Top = 10;
            dom.Fields["publisher"].TextWrapping = TextWrapping.Wrap;
            dom.Fields["publisher"].Visibility = Visibility.Visible;

            dom.Fields["source_url"].Left = 10;
            dom.Fields["source_url"].Top = 250;
            dom.Fields["source_url"].Visibility = Visibility.Visible;

            dom.Fields["title"].Left = 30;
            dom.Fields["title"].Top = 115;
            dom.Fields["title"].FontWeight = FontWeights.Bold;
            dom.Fields["title"].TextWrapping = TextWrapping.Wrap;
            dom.Fields["title"].Visibility = Visibility.Visible;

            dom.Fields["f2f_url"].Left = 10;
            dom.Fields["f2f_url"].Top = 275;
            dom.Fields["f2f_url"].Visibility = Visibility.Visible;

            return dom;
        }

        /// <summary>
        /// A helper method for the prototype. This will be removed!
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        static public LayoutModel UmpireModel(DocumentModel doc)
        {
            var dom = new LayoutModel(doc.Fields, "Umpires");
            dom.Fields["name"].Left = 10;
            dom.Fields["name"].Top = 10;
            dom.Fields["name"].TextWrapping = TextWrapping.Wrap;
            dom.Fields["name"].FontWeight = FontWeights.Bold;
            dom.Fields["name"].Visibility = Visibility.Visible;

            dom.Fields["experience"].Left = 10;
            dom.Fields["experience"].Top = 250;
            dom.Fields["experience"].Visibility = Visibility.Visible;

            return dom;
        }
    }
}
