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

        } 
        /// <summary>
        /// Creates Image using layout information from template and Data 
        /// </summary>
        public override List<FrameworkElement> MakeView(FieldModel fieldModel, DocumentModel context, bool bindings=true)
        { 
            if (fieldModel is OperatorFieldModel)
            {
                OperatorView view = new OperatorView();
                 return new List<FrameworkElement> { view };
            }
            return null;
        }
    }
}
