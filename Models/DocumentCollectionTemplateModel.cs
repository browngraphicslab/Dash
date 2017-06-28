using System;
using Dash.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Dash
{
    public class DocumentCollectionTemplateModel : TemplateModel
    {


        public DocumentCollectionTemplateModel(double left = 0, double top = 0, double width = 0, double height = 0, Visibility visibility = Visibility.Visible) : base(left, top, width, height, visibility)
        {
        }
        
        protected override List<FrameworkElement> MakeView(FieldModelController fieldModel, DocumentController context)
        {
            var collectionFieldModelController = fieldModel as DocumentCollectionFieldModelController;
            Debug.Assert(collectionFieldModelController != null);
            var collectionModel = new CollectionModel(collectionFieldModelController.DocumentCollectionFieldModel, context);
            var collectionViewModel = new CollectionViewModel(collectionModel);
            var view = new CollectionView(collectionViewModel);

            var translateBinding = new Binding
            {
                Source = this,
                Path = new PropertyPath("Pos"),
                Mode = BindingMode.TwoWay,
                Converter = new PositionConverter()
            };
            view.SetBinding(UIElement.RenderTransformProperty, translateBinding);
            if (Width > 0)
                view.Width = Width;

            return new List<FrameworkElement> { view };
        }
    }
}