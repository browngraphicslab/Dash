using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Dash
{
    public class EditableScriptViewModel : ViewModelBase
    {
        public FieldReference Reference { get; }

        public Context Context { get; }

        public KeyController Key => Reference.FieldKey;
        public FieldControllerBase Value => Reference.Dereference(Context);


        public EditableScriptViewModel(FieldReference reference)
        {
            Reference = reference;
        }

    }
}
