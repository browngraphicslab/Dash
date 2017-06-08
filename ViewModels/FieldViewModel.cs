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
        public TemplateModel Template { get; set; }

        public FieldViewModel(TemplateModel templateModel)
        {
            Template = templateModel;
        }
    }
}
