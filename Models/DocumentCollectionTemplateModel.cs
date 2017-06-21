using Dash.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Dash
{
    public class DocumentCollectionTemplateModel : TemplateModel
    {
        public DocumentCollectionTemplateModel(double left = 0, double top = 0, double width = 0, double height = 0, Visibility visibility = Visibility.Visible) : base(left, top, width, height, visibility)
        {
        }

        public override FrameworkElement MakeView(FieldModel fieldModel)
        {
            var collectionFieldModel = fieldModel as DocumentCollectionFieldModel;
            Debug.Assert(collectionFieldModel != null);
            var collectionModel = new CollectionModel(new ObservableCollection<DocumentModel>(collectionFieldModel.Documents));
            var collectionViewModel = new CollectionViewModel(collectionModel);
            var view = new CollectionView(collectionViewModel);

            Canvas.SetTop(view, Top);
            Canvas.SetLeft(view, Left);

            return view;
        }
    }
}