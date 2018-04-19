using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class EditableScriptViewModel : ViewModelBase
    {
        bool _isSelected;
        ReferenceController _dataReference;
        public bool Selected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
        public FieldReference Reference { get; }

        public ReferenceController ReferenceController
        {
            get => _dataReference;
            set => SetProperty(ref _dataReference, value);
        }
        public Context Context { get; }

        public KeyController Key => Reference.FieldKey;

        public FieldControllerBase Value => Reference.Dereference(Context);

        public EditableScriptViewModel(FieldReference reference, Context context = null)
        {
            Reference = reference;
            _dataReference = reference.GetReferenceController();
            Context = context;
        }
    }
}
