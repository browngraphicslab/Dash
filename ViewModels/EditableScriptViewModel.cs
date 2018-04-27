using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Dash
{
    public class EditableScriptViewModel : ViewModelBase
    {
        #region SchemaVariables

        double _width;
        Thickness _borderThickness;
        public Thickness BorderThickness
        {
            get => _borderThickness;
            set => SetProperty(ref _borderThickness, value);
        }
        public CollectionDBSchemaHeader.HeaderViewModel HeaderViewModel;
        public double Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        #endregion

        public FieldReference Reference { get; }

        public Context Context { get; }

        public KeyController Key => Reference.FieldKey;
        public FieldControllerBase Value => Reference.Dereference(Context);


        public EditableScriptViewModel(FieldReference reference, CollectionDBSchemaHeader.HeaderViewModel headerViewModel = null)
        {
            Reference = reference;
            if (headerViewModel != null)
            {
                HeaderViewModel = headerViewModel;
                BorderThickness = HeaderViewModel.HeaderBorder.BorderThickness; // not expected to change at run-time, so not registering for callbacks
                Width = BorderThickness.Left + BorderThickness.Right + (double)HeaderViewModel.Width;
                HeaderViewModel.PropertyChanged += (sender, e) => Width = BorderThickness.Left + BorderThickness.Right + HeaderViewModel.Width;
            }

        }
    }
}
