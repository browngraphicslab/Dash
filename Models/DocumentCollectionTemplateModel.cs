using Dash.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Dash
{
    public class DocumentCollectionTemplateModel : TemplateModel
    {
        public DocumentCollectionTemplateModel(double left = 0, double top = 0, double width = 0, double height = 0, Visibility visibility = Visibility.Visible) : base(left, top, width, height, visibility)
        {
        }
        
        protected override List<UIElement> MakeView(FieldModel fieldModel, DocumentModel context)
        { 
            var collectionFieldModel = fieldModel as DocumentCollectionFieldModel;
            Debug.Assert(collectionFieldModel != null);
            var collectionModel = new CollectionModel(new ObservableCollection<DocumentModel>(collectionFieldModel.Documents), context);
            var collectionViewModel = new CollectionViewModel(collectionModel);
            var view = new CollectionView(collectionViewModel);

            Canvas.SetTop(view, Top);
            Canvas.SetLeft(view, Left);
            
            return new List<UIElement>(new UIElement[] { view });
        }
    }
}