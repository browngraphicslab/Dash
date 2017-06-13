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
using DashShared;

namespace Dash
{
    public interface DocumentLayoutModelSourceBase
    {
        LayoutModel DocumentLayoutModel(DocumentModel docModel);
        void SetDocumentLayoutModel(DocumentType type, LayoutModel layoutModel);
    }

    public class DocumentLayoutModelSource : DocumentLayoutModelSourceBase
    {
        void setLayoutModel(DocumentType typename, LayoutModel template)
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
                if (doc.DocumentType.Type == "recipes")
                    setLayoutModel(doc.DocumentType, LayoutModel.Food2ForkRecipeModel(doc));
                else if (doc.DocumentType.Type == "Umpires")
                    setLayoutModel(doc.DocumentType, LayoutModel.UmpireModel(doc));
                else if (doc.DocumentType.Type == "oneimage")
                {
                    setLayoutModel(doc.DocumentType, LayoutModel.OneImageModel(doc));
                }
                else if (doc.DocumentType.Type == "twoimages")
                {
                    setLayoutModel(doc.DocumentType, LayoutModel.TwoImagesAndTextModel(doc));
                } else if (doc.DocumentType.Type == "example_api_object")
                {
                    setLayoutModel(doc.DocumentType, LayoutModel.ExampleApiObject(doc));
                }
            }
            if (LayoutTemplates.ContainsKey(doc.DocumentType))
                return LayoutTemplates[doc.DocumentType];
            return null;
        }

        public void SetDocumentLayoutModel(DocumentType type, LayoutModel layoutModel)
        {
            if (LayoutTemplates.ContainsKey(type))
                LayoutTemplates.Remove(type);
            
            setLayoutModel(type, layoutModel);
        }

        public Dictionary<DocumentType, LayoutModel> LayoutTemplates = new Dictionary<DocumentType, LayoutModel>();
        public static DocumentLayoutModelSource DefaultLayoutModelSource = new DocumentLayoutModelSource();
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
