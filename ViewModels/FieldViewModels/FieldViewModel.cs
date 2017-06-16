using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    /// <summary>
    /// Implements a view for modeling fields of a document.
    /// </summary>
    public abstract class FieldViewModel
    {
        public TemplateModel Template { get; set; }

        public FieldViewModel(TemplateModel templateModel)
        {
            Template = templateModel;
        }
    }
}
