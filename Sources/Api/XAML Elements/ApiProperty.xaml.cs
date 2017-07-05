using System;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class ApiProperty : UserControl {
        public ApiProperty() {
            DataContext = this;
            this.InitializeComponent();
        }

        // == MEMBERS ==
        private ApiPropertyType type;

        // == GETTERS / SETTERS ==
        public string Key { get { return xKey.Text; } }
        public string Value { get { return xValue.Text; } set { xValue.Text = Value;  } }
        public bool IsRequired { get { return xRequired.IsChecked.Value; } }
        public ApiPropertyType Type { get { return type; } }

        public TextBlock XKey { get { return xKey; } set { xKey = value; } }
        public TextBox XValue { get { return xValue; } set { xValue = value; } }
        public CheckBox XRequired { get { return xRequired; } set { xRequired = value; } }
        public DocumentController docControllerRef;

        public enum ApiPropertyType {
            Parameter,
            Header,
            AuthParameter,
            AuthHeader
        };

        // == CONSTRUCTOR ==
        public ApiProperty(string key, string val, ApiPropertyType type, DocumentController docControllerRef = null, bool required = false) {
            DataContext = this;
            this.InitializeComponent();
            this.docControllerRef = docControllerRef;
            xValue.Text = val;
            this.type = type;
            xKey.Text = key;
        }

        // == METHODS ==
        public bool isInvalid() {
            return (IsRequired && (String.IsNullOrWhiteSpace(Value)));
        }

    }
}
