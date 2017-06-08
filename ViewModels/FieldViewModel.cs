using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Dash
{
    public abstract class FieldViewModel
    {
        public FieldModel Field { get; set; }
        public TemplateModel Template { get; set; }

        public FieldViewModel(FieldModel fieldModel, TemplateModel templateModel)
        {
            Field = fieldModel;
            Template = templateModel;
        }

        public abstract UIElement GetView();
    }
}
