using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class EditableScriptViewModel : ViewModelBase
    {
        public FieldReference Reference { get; }
        public Context Context { get; }

        public KeyController Key => Reference.FieldKey;

        public FieldControllerBase Value => Reference.Dereference(Context);

        public EditableScriptViewModel(FieldReference reference, Context context = null)
        {
            Reference = reference;
            Context = context;
        }
    }
}
