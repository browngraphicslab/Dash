using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Sources.Api.XAML_Elements {
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
