using Dash;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Dash
{

    /// <summary>
    /// This class provides a source of layout models that can be applied to documents in a collection.
    /// </summary>
    public class DocumentLayoutModelSource
    {
        // == MEMBERS == 
        public Dictionary<string, LayoutModel> LayoutTemplates = new Dictionary<string, LayoutModel>();
        static public DocumentLayoutModelSource DefaultLayoutModelSource = new DocumentLayoutModelSource();

        // == METHODS ==

        /// <summary>
        /// Adds (or updates) a given document type's default layout model to the given
        /// layout model. 
        /// </summary>
        /// <param name="type">document type to update or add</param>
        /// <param name="layoutModel">new layout model</param>
        void SetDocumentLayoutModel(string typename, LayoutModel template)
        {
            if (!LayoutTemplates.ContainsKey(typename))
                LayoutTemplates.Remove(typename);
            LayoutTemplates.Add(typename, template);
        }

        /// <summary>
        /// Given a document, returns the LayoutModel(s) corresponding to that document's type.
        /// </summary>
        /// <param name="doc">document to fetch layout model of</param>
        /// <returns>The LayoutModel corresponding to the given document.</returns>
        public LayoutModel DocumentLayoutModel(DocumentModel doc)
        {
            if (!LayoutTemplates.ContainsKey(doc.DocumentType))
            {
                // bcz: hack to have a default layout for known types: recipes, Umpires
                if (doc.DocumentType == "recipes")
                    SetDocumentLayoutModel(doc.DocumentType, LayoutModel.Food2ForkRecipeModel(doc));
                else if (doc.DocumentType == "Umpires")
                    SetDocumentLayoutModel(doc.DocumentType, LayoutModel.UmpireModel(doc));
                else if (doc.DocumentType == "oneimage") {
                    SetDocumentLayoutModel(doc.DocumentType, LayoutModel.OneImageModel(doc));
                } else if (doc.DocumentType == "twoimages") {
                    SetDocumentLayoutModel(doc.DocumentType, LayoutModel.TwoImagesAndTextModel(doc));
                } else if (doc.DocumentType == "default") { // API TESTING
                    SetDocumentLayoutModel(doc.DocumentType, LayoutModel.DefaultLayoutModel(doc));
                }
            }
            if (LayoutTemplates.ContainsKey(doc.DocumentType))
                return LayoutTemplates[doc.DocumentType];
            return null; // TODO: shouldn't the else case for this do something to add a new layout model?
        }
    }

    /// <summary>
    /// Represents a view model for a given document.
    /// </summary>
    public class DocumentViewModel
    {
        // == MEMBERS ==
        public DocumentModel DocumentModel { get; set; }
        public DocumentLayoutModelSource DocumentViewModelSource { get; set; }

        // == CONSTRUCTORS ==
        public DocumentViewModel() { }
        public DocumentViewModel(DocumentModel docModel, DocumentLayoutModelSource docViewModelSource)
        {
            DocumentModel = docModel;
            DocumentViewModelSource = docViewModelSource;
        }

        // == METHODS ==
        /// <summary>
        /// Converts a document's in-code property fields into XAML UIElements.
        /// </summary>
        /// <returns>A list of UIElements corresponding to the document's fields.</returns>
        public List<UIElement> GetUIElements()
        {
            List<UIElement> uiElements = new List<UIElement>(DocumentModel.Fields.Count);
            LayoutModel layout = DocumentViewModelSource.DocumentLayoutModel(DocumentModel);
            foreach (var field in DocumentModel.Fields)
            {
                if (layout.Fields.ContainsKey(field.Key))
                    uiElements.Add(field.Value.MakeView(layout.Fields[field.Key]));
            }
            return uiElements;
        }
    }
}
