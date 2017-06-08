using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Dash
{
    class ImageViewModel : FieldViewModel
    {
        public ImageViewModel(FieldModel fieldModel, TemplateModel templateModel) : base(fieldModel, templateModel)
        {
        }

        public override UIElement GetView()
        {
            throw new NotImplementedException();
        }
    }
}
