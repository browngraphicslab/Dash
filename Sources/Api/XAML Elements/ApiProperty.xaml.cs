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
        private string key, val;
        private bool requiredToQuery; // if user must fill in value to query API
        private bool isParameter; // true = param property; false = header property
        private bool isDisplayed;
        private bool forAuth;

        // == GETTERS / SETTERS ==
        public string Key { get { return key; } }
        public string Value { get { return xValue.Text; } set { val = value; xValue.Text = Value;  } }
        public bool IsParameter { get { return isParameter; } }
        public bool IsRequired { get { return requiredToQuery; } }
        public bool IsDisplayed { get { return isDisplayed; }  }
        public bool ForAuth { get {return forAuth; } }

        // == CONSTRUCTOR ==
        public ApiProperty(string key, string val, bool isParameter, bool required = false, bool displaying = false, bool forAuth = false) {
            this.isParameter = isParameter;
            this.key = key;
            if (val == null)
                val = "";
            this.val = val;
            this.forAuth = forAuth;
            this.requiredToQuery = required;
            this.isDisplayed = displaying;
            DataContext = this;
            this.InitializeComponent();
            xValue.Text = val;
            xKey.Text = key;
            if (required)
                xKey.Text += "*";
            if (!displaying)
                xProperty.Visibility = Visibility.Collapsed;
        }

        // == METHODS ==
        public bool isInvalid() {
            return (IsRequired && (Value == null || Value == ""));
        }

    }
}
