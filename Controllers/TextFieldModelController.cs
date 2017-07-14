using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Dash
{
    public class TextFieldModelController : FieldModelController
    {
        public TextFieldModelController(string data) : base(new TextFieldModel(data)) { }

        /// <summary>
        ///     The <see cref="TextFieldModel" /> associated with this <see cref="TextFieldModelController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public TextFieldModel TextFieldModel => FieldModel as TextFieldModel;

        public string Data
        {
            get { return TextFieldModel.Data; }
            set
            {
                if (SetProperty(ref TextFieldModel.Data, value))
                {
                    // update local
                    // update server
                }
                FireFieldModelUpdated();
            }
        }

        public override TypeInfo TypeInfo => TypeInfo.Text;

        protected override void UpdateValue(FieldModelController fieldModel)
        {
            Data = (fieldModel as TextFieldModelController).Data;
        }

        public override FrameworkElement GetTableCellView()
        {
            return GetTableCellViewOfScrollableText(BindTextOrSetOnce);
        }

        private void BindTextOrSetOnce(TextBlock textBlock)
        {
            var textBinding = new Binding
            {
                Source = this,
                Path = new PropertyPath(nameof(Data)),
                Mode = BindingMode.OneWay
            };
            textBlock.SetBinding(TextBlock.TextProperty, textBinding);
        }

        public override FieldModelController GetDefaultController()
        {
            return new TextFieldModelController("Default Value");
        }

        public override string ToString()
        {
            return Data;
        }

        public override FieldModelController Copy()
        {
            return new TextFieldModelController(Data);
        }
    }
}