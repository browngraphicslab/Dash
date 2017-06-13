using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using DashShared;
using Microsoft.Extensions.DependencyInjection;

namespace Dash
{
    public abstract class OperatorFieldModel : FieldModel
    {
        public override UIElement MakeView(TemplateModel template)
        {
            throw new NotImplementedException();
        }

        public abstract Dictionary<Key, FieldModel> Execute(Dictionary<Key, ReferenceFieldModel> inputReferences);
    }
}
