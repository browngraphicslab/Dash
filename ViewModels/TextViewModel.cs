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
        private TextFieldModel _field;

        public TextViewModel(TextFieldModel fieldModel, TemplateModel templateModel) : base(templateModel)
        {
            _field = fieldModel;
        }
    }
}
