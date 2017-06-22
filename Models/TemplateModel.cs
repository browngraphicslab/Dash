using Dash.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace Dash
{
    public abstract class TemplateModel : ViewModelBase
    {
        private double _left;

        public double Left
        {
            get { return _left; }
            set { SetProperty(ref _left, value); }
        }

        private double _top;

        public double Top
        {
            get { return _top; }
            set { SetProperty(ref _top, value); }
        }

        private Visibility _visibility;

        public Visibility Visibility
        {
            get { return _visibility; }
            set { SetProperty(ref _visibility, value); }
        }

        private double _width;

        public double Width
        {
            get { return _width; }
            set { SetProperty(ref _width, value); }
        }

        private double _height;

        public double Height
        {
            get { return _height; }
            set { SetProperty(ref _height, value); }
        }


        public TemplateModel(double left = 0, double top = 0, double width = 0, double height = 0, Visibility visibility = Visibility.Visible)
        {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
            Visibility = visibility;
        }

        /// <summary>
        /// Creates a UI view of the field based on this templates display parameters
        /// </summary>
        protected virtual List<UIElement> MakeView(FieldModel fieldModel, DocumentModel context)
        {
            return null;
        }
        /// <summary>
        /// Creates a UI view of the field based on this templates display parameters
        /// </summary>
        public virtual List<UIElement> MakeViewUI(FieldModel fieldModel, DocumentModel context)
        {
            while (fieldModel is ReferenceFieldModel)
            {
                var docController = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
                fieldModel = docController.GetDocumentAsync((fieldModel as ReferenceFieldModel).DocId).Field((fieldModel as ReferenceFieldModel).FieldKey);
            }
            if (fieldModel is DocumentModelFieldModel)
            {
                var doc = (fieldModel as DocumentModelFieldModel).Data;
                return new DocumentViewModel(doc).GetUiElements(new Rect(Left, Top, Width, Height));
            }
            return MakeView(fieldModel, context);
        }
    }
}
