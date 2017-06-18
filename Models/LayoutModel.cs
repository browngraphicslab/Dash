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
                //throw new ArgumentNullException();
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
        static public LayoutModel Food2ForkRecipeModel(DocumentType docType)
        {
            var fields = new Dictionary<Key, TemplateModel>();
            //TODO REALLY BAD CODE
            fields[DocumentModel.GetFieldKeyByName("publisher")]  = new TextTemplateModel(10, 10,  FontWeights.Normal, TextWrapping.Wrap,   Visibility.Visible);
            fields[DocumentModel.GetFieldKeyByName("source_url")] = new TextTemplateModel(10, 250, FontWeights.Normal, TextWrapping.NoWrap, Visibility.Visible);
            fields[DocumentModel.GetFieldKeyByName("title")]      = new TextTemplateModel(30, 115, FontWeights.Bold,   TextWrapping.Wrap,   Visibility.Visible);
            fields[DocumentModel.GetFieldKeyByName("f2f_url")]    = new TextTemplateModel(10, 275, FontWeights.Normal, TextWrapping.NoWrap, Visibility.Visible);

            Debug.Assert(docType.Type.Equals("recipes"));
            return new LayoutModel(fields, docType);
        }

        /// <summary>
        /// A helper method for the prototype. This will be removed!
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public static LayoutModel UmpireModel(DocumentType docType)
        {
            Dictionary<Key, TemplateModel> fields = new Dictionary<Key, TemplateModel>();
            //TODO REALLY BAD CODE
            fields[DocumentModel.GetFieldKeyByName("name")]       = new TextTemplateModel(10, 10, FontWeights.Bold, TextWrapping.Wrap);
            fields[DocumentModel.GetFieldKeyByName("experience")] = new TextTemplateModel(10, 250, FontWeights.Normal);

            Debug.Assert(docType.Type.Equals("Umpires"));

            return new LayoutModel(fields, docType);
        }

        public static LayoutModel OneImageModel(DocumentType docType)
        {
            Dictionary<Key, TemplateModel> fields = new Dictionary<Key, TemplateModel>();
            //TODO REALLY BAD CODE
            fields[DocumentModel.GetFieldKeyByName("content")] = new ImageTemplateModel(5, 20, 100, 100);

            Debug.Assert(docType.Type.Equals("oneimage"));
            return new LayoutModel(fields, docType);
        }


        public static LayoutModel TwoImagesAndTextModel(DocumentType docType, bool editable = false)
        {
            var keyController = App.Instance.Container.GetRequiredService<KeyEndpoint>();


            Dictionary<Key, TemplateModel> fields = new Dictionary<Key, TemplateModel>();
            //TODO REALLY BAD CODE
            fields[DocumentModel.GetFieldKeyByName("content")]  = new ImageTemplateModel(5, 140, 100, 100);
            fields[DocumentModel.GetFieldKeyByName("content2")] = new ImageTemplateModel(5, 20, 100, 100);
            fields[DocumentModel.GetFieldKeyByName("text")]     = new TextTemplateModel(5, 260, FontWeights.Normal, TextWrapping.NoWrap, Visibility.Visible, editable);

            Debug.Assert(docType.Type.Equals("twoimages"));
            return new LayoutModel(fields, docType);
        }

        public static LayoutModel OperatorLayoutModel(DocumentType docType)
        {
            Dictionary<Key, TemplateModel> fields = new Dictionary<Key, TemplateModel>();

            fields[OperatorDocumentModel.OperatorKey] = new TextTemplateModel(0, 0, FontWeights.Normal);

            Debug.Assert(docType.Type.Equals("operator"));
            return new LayoutModel(fields, docType);
        }

        public static LayoutModel ExampleApiObject(DocumentType docType)
        {
            Dictionary<Key, TemplateModel> fields = new Dictionary<Key, TemplateModel>();
            
            fields[DocumentModel.GetFieldKeyByName("id")] = new TextTemplateModel(0, 20, FontWeights.Normal);
            fields[DocumentModel.GetFieldKeyByName("first_name")] = new TextTemplateModel(0, 60, FontWeights.Normal);
            fields[DocumentModel.GetFieldKeyByName("last_name")] = new TextTemplateModel(0, 100, FontWeights.Normal);
            fields[DocumentModel.GetFieldKeyByName("email")] = new TextTemplateModel(0, 140, FontWeights.Normal);
            fields[DocumentModel.GetFieldKeyByName("gender")] = new TextTemplateModel(0, 180, FontWeights.Normal);
            fields[DocumentModel.GetFieldKeyByName("ip_address")] = new TextTemplateModel(0, 220, FontWeights.Normal);

            return new LayoutModel(fields, docType);
        }

        public static LayoutModel PricePerSquareFootApiObject(DocumentType docType)
        {
            Dictionary<Key, TemplateModel> fields = new Dictionary<Key, TemplateModel>();
            
            fields[PricePerSquareFootApi.PriceKey] = new TextTemplateModel(0,0, FontWeights.Normal);
            fields[PricePerSquareFootApi.SqftKey] = new TextTemplateModel(0, 100, FontWeights.Normal);
            fields[PricePerSquareFootApi.TestKey] = new TextTemplateModel(0, 200, FontWeights.Normal);

            return new LayoutModel(fields, docType);
        }

        public static LayoutModel ExampleCollectionModel(DocumentType docType)
        {
            Dictionary<Key, TemplateModel> fields = new Dictionary<Key, TemplateModel>();
            
            fields[DocumentModel.GetFieldKeyByName("documents")] = new DocumentCollectionTemplateModel(0, 0, 400, 400);

            return new LayoutModel(fields, docType);
        }
    }
}
