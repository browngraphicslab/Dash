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
    public interface DocumentLayoutModelSourceBase
    {
        LayoutModel DocumentLayoutModel(DocumentModel docModel);
        void SetDocumentLayoutModel(string type, LayoutModel layoutModel);
    }

    public class DocumentLayoutModelSource : DocumentLayoutModelSourceBase
    {
        void setLayoutModel(string typename, LayoutModel template)
        {
            if (!LayoutTemplates.ContainsKey(typename))
                LayoutTemplates.Remove(typename);
            LayoutTemplates.Add(typename, template);
        }
        public LayoutModel DocumentLayoutModel(DocumentModel doc)
        {
            if (!LayoutTemplates.ContainsKey(doc.DocumentType))
            {
                // bcz: hack to have a default layout for known types: recipes, Umpires
                if (doc.DocumentType == "recipes")
                    setLayoutModel(doc.DocumentType, LayoutModel.Food2ForkRecipeModel(doc));
                else if (doc.DocumentType == "Umpires")
                    setLayoutModel(doc.DocumentType, LayoutModel.UmpireModel(doc));
                else if (doc.DocumentType == "oneimage")
                {
                    setLayoutModel(doc.DocumentType, LayoutModel.OneImageModel(doc));
                }
                else if (doc.DocumentType == "twoimages")
                {
                    setLayoutModel(doc.DocumentType, LayoutModel.TwoImagesAndTextModel(doc));
                }
            }
            if (LayoutTemplates.ContainsKey(doc.DocumentType))
                return LayoutTemplates[doc.DocumentType];
            return null;
        }

        public void SetDocumentLayoutModel(string type, LayoutModel layoutModel)
        {
            if (LayoutTemplates.ContainsKey(type))
                LayoutTemplates.Remove(type);
            
            setLayoutModel(type, layoutModel);
        }

        public Dictionary<string, LayoutModel> LayoutTemplates = new Dictionary<string, LayoutModel>();
        static public DocumentLayoutModelSource DefaultLayoutModelSource = new DocumentLayoutModelSource();
    }

    public class DocumentViewModel
    {
        public DocumentViewModel() { }
        public DocumentViewModel(DocumentModel docModel, DocumentLayoutModelSource docViewModelSource)
        {
            DocumentModel = docModel;
            DocumentViewModelSource = docViewModelSource;
        }
        public DocumentModel DocumentModel { get; set; }
        public DocumentLayoutModelSource DocumentViewModelSource { get; set; }

        public List<UIElement> GetUIElements()
        {
            List<UIElement> uiElements = new List<UIElement>(DocumentModel.Fields.Count);
            LayoutModel layout = DocumentViewModelSource.DocumentLayoutModel(DocumentModel);
            foreach (var field in DocumentModel.Fields)
            {
                if (!layout.Fields.ContainsKey(field.Key))
                {
                    continue;
                }
                uiElements.Add(field.Value.MakeView(layout.Fields[field.Key]));
            }
            return uiElements;
        }
    }
}
