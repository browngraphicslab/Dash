using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash.Models
{
    public class ImageTemplateModel : TemplateModel
    {
        public override FieldViewModel CreateViewModel(FieldModel field)
        {
            return new ImageViewModel(field, this);
        }
    }
}
