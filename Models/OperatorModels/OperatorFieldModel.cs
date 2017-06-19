using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using DashShared;
using Microsoft.Extensions.DependencyInjection;

namespace Dash
{
    public abstract class OperatorFieldModel : FieldModel
    {
        public string DocumentID { get; set; } //TODO probably remove this along with code in setter in OperatorDocumentModel
        public virtual List<Key> Inputs { get; } = new List<Key>();
        public virtual List<Key> Outputs{ get; } = new List<Key>();

        public override FrameworkElement MakeView(TemplateModel template)
        {
            OperatorView view = new OperatorView();
            view.DataContext = this;
            return view;
        }

        public abstract Dictionary<Key, FieldModel> Execute(Dictionary<Key, ReferenceFieldModel> inputReferences);
    }
}
