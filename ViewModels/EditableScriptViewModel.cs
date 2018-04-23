using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class EditableScriptViewModel : ViewModelBase
    {
        double _width;
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
        public int Row { get; set; }
        public FieldControllerBase Value => Reference.Dereference(Context);
        public EditableScriptViewModel(FieldReference reference)
        {
            Reference = reference;
            _dataReference = reference.GetReferenceController();
        }
    }
}
