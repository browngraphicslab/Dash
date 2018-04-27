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
        #region SchemaVariables

        double _width;
        private Thickness BorderThickness { get; }
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
                BorderThickness = headerViewModel.HeaderBorder.BorderThickness; // not expected to change at run-time, so not registering for callbacks
                Width = BorderThickness.Left + BorderThickness.Right + headerViewModel.Width;
                headerViewModel.PropertyChanged += OnHeaderViewModelOnPropertyChanged;
            }
            else
            {
                Width = double.NaN;
            }

        }

        private void OnHeaderViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is CollectionDBSchemaHeader.HeaderViewModel hvm)
            {
                if (e.PropertyName == nameof(hvm.Width))
                {
                    Width = BorderThickness.Left + BorderThickness.Right + hvm.Width;
                }
            }
        }
    }
}
