using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Dash
{
    class TextViewModel : FieldViewModel
    {
        public TextViewModel(FieldModel fieldModel, TemplateModel templateModel) : base(fieldModel, templateModel)
        {
        }

        public override UIElement GetView()
        {
            TextBlock tb = new TextBlock();
            tb.Text = (Field as TextFieldModel).Data;
            return tb;
        }
    }
}
