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

        public virtual List<Key> Inputs { get; } = new List<Key>();
        public virtual List<Key> Outputs{ get; } = new List<Key>();

        public override UIElement MakeView(TemplateModel template)
        {
            throw new NotImplementedException();
        }

        public abstract Dictionary<Key, FieldModel> Execute(Dictionary<Key, ReferenceFieldModel> inputReferences);
    }
}
