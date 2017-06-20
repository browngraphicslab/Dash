using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace Dash.Models.OperatorModels
{
    class OperatorTemplateModel : TemplateModel
    {
        public OperatorTemplateModel(double left = 0, double top = 0, double width = 0, double height = 0,
            Visibility visibility = Visibility.Visible)
            : base(left, top, width, height, visibility)
        {

        } /// <summary>
            /// Creates Image using layout information from template and Data 
            /// </summary>
        protected override List<UIElement> MakeView(FieldModel fieldModel)
        {
            if (fieldModel is OperatorFieldModel)
            {
                OperatorView view = new OperatorView();
                view.DataContext = fieldModel;
                 return new List<UIElement>(new UIElement[] { view });
            }
            return null;
        }
    }
}
