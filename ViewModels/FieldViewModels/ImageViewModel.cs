using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Dash
{
    class ImageViewModel : FieldViewModel
    {
        private ImageFieldModel _field;
        public ImageViewModel(ImageFieldModel fieldModel, TemplateModel templateModel) : base(templateModel)
        {
            _field = fieldModel;
        }
    }
}
