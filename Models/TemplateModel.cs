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
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace Dash
{
    public abstract class TemplateModel : ViewModelBase
    {
        

        public Point _point;
        public Point Pos
        {
            get { return _point; }
            set { SetProperty(ref _point, value); }
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
            Pos = new Point {X = left, Y = top};
            Width = width;
            Height = height;
            Visibility = visibility;
        }


        /// <summary>
        /// Creates a UI view of the field based on this templates display parameters
        /// </summary>
        public virtual List<FrameworkElement> MakeView(FieldModel fieldModel, DocumentModel context, bool bindings=true)
        {
            return null;
        }


        /// <summary>
        /// Creates a UI view of the field based on this templates display parameters
        /// </summary>
        public virtual List<FrameworkElement> MakeViewUI(FieldModel fieldModel, DocumentModel context)
        {
            while (fieldModel is ReferenceFieldModel)
            {
                var docController = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
                fieldModel = docController.GetDocumentAsync((fieldModel as ReferenceFieldModel).DocId).Field((fieldModel as ReferenceFieldModel).FieldKey);
            }
            if (fieldModel is DocumentModelFieldModel)
            {
                var doc = (fieldModel as DocumentModelFieldModel).Data;
                return new DocumentViewModel(doc).GetUiElements(new Rect(Pos.X, Pos.Y, Width, Height));
            }
            return MakeView(fieldModel, context);
        }


        protected class PositionConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, string language)
            {
                Point p = (Point)value;
                return new TranslateTransform
                {
                    X = p.X,
                    Y = p.Y
                };
            }

            public object ConvertBack(object value, Type targetType, object parameter, string language)
            {
                return null;
                //throw new NotImplementedException();
            }
        }
    }
}
