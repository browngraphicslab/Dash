using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using DashShared;
using Windows.Foundation;

namespace Dash
{
    /// <summary>
    /// Wrapper document to display the ApiSourceCreatorDisplay Usercontrol.
    /// </summary>
    internal class ApiDocumentModel : CourtesyDocument
    {
        public static readonly DocumentType DocumentType = ApiOperatorController.ApiType;
        private static string PrototypeId = "683EA5CB-A2FE-4B34-A461-7BCC7BDC7754";


        public static readonly KeyController BaseUrlKey        = new KeyController("Base URL", new Guid("C20E4B2B-A633-4C2C-ACBF-757FF6AC8E5A"));
        public static readonly KeyController HttpMethodKey     = new KeyController("Http Method", new Guid("1CE4047D-1813-410B-804E-BA929D8CB4A4"));
        public static readonly KeyController HeadersKey        = new KeyController("Headers", new Guid("6E9D9F12-E978-4E61-85C7-707A0C13EFA7"));
        public static readonly KeyController ParametersKey     = new KeyController("Parameter", new Guid("654A4BDF-1AE0-432A-9C90-CCE9B4809870"));

        public static readonly KeyController AuthHttpMethodKey = new KeyController("Auth Method", new Guid("D37CCAC0-ABBC-4861-BEB4-8C079049DCF8"));
        public static readonly KeyController AuthBaseUrlKey    = new KeyController("Auth URL", new Guid("7F8709B6-2C9B-43D0-A86C-37F3A1517884"));
        public static readonly KeyController AuthKey           = new KeyController("Auth Key", new Guid("1E5B5398-9349-4585-A420-EDBFD92502DE"));
        public static readonly KeyController AuthSecretKey     = new KeyController("Auth Secret", new Guid("A690EFD0-FF35-45FF-9795-372D0D12711E"));
        public static readonly KeyController AuthHeadersKey    = new KeyController("Auth Header", new Guid("E1773B06-F54C-4052-B888-AE85278A7F88"));
        public static readonly KeyController AuthParametersKey = new KeyController("Auth Parameter", new Guid("CD546F0B-A0BA-4C3B-B683-5B2A0C31F44E"));

        public static readonly KeyController KeyTextKey        = new KeyController("Key", new Guid("388F7E20-4424-4AC0-8BB7-E8CCF2279E60"));
        public static readonly KeyController ValueTextKey      = new KeyController("Value", new Guid("F89CAD72-271F-48E6-B233-B6BA766E613F"));
        public static readonly KeyController RequiredKey       = new KeyController("Required", new Guid("D4FCBA25-B540-4E17-A17A-FCDE775B97F9"));
        public static readonly KeyController DisplayKey        = new KeyController("Display", new Guid("2B80D6A8-4224-4EC7-9BDF-DFD2CC20E463"));


        public ApiDocumentModel()
        {
            var fields = new Dictionary<KeyController, FieldControllerBase>
            {
                [BaseUrlKey] = new TextController(""),
                [HttpMethodKey] = new NumberController(0),
                [AuthBaseUrlKey] = new TextController(""),
                [AuthHttpMethodKey] = new NumberController(0),
                [AuthSecretKey] = new TextController(""),
                [AuthKey] = new TextController(""),
                [ParametersKey] = new ListController<DocumentController>(new List<DocumentController>()),
                [HeadersKey] = new ListController<DocumentController>(new List<DocumentController>()),
                [AuthParametersKey] =
                new ListController<DocumentController>(new List<DocumentController>()),
                [AuthHeadersKey] = new ListController<DocumentController>(new List<DocumentController>()),
                [KeyStore.WidthFieldKey] = new NumberController(550),
                [KeyStore.HeightFieldKey] = new NumberController(400),
                [KeyStore.PositionFieldKey] = new PointController(new Windows.Foundation.Point(0,0)),
                [KeyStore.ScaleAmountFieldKey] = new PointController(1, 1),
                [KeyStore.IconTypeFieldKey] = new NumberController((double)IconTypeEnum.Api),

                // TODO: differentiating similar fields in different documents for operator view (Not sure what this means Anna)
                [KeyStore.DataKey] =
                new ListController<DocumentController>(new List<DocumentController>())
            };
            SetupDocument(DocumentType, PrototypeId, "API Document Prototype Layout", fields);
            //Document.SetActiveLayout(new DefaultLayout(0, 0, 400, 400).Document, true, true);
        }

        /// <summary>
        /// Generates a new document containing the parameter information and adds that document to
        /// the corresponding DocumentCollectionFieldModel representing that parameter's list (i.e. Header, AuthParameters).
        /// </summary>
        /// <returns>The newly generated document representing the newly added parameter.</returns>
        public static DocumentController addParameter(DocumentController docController, TextBox key,
            TextBox value, CheckBox display,
            CheckBox required, KeyController parameterCollectionKey, ApiSourceDisplay sourceDisplay)
        {
            Debug.Assert(docController.DocumentType == DocumentType);
            Debug.Assert(parameterCollectionKey == AuthParametersKey ||
                         parameterCollectionKey == AuthHeadersKey ||
                         parameterCollectionKey == ParametersKey || parameterCollectionKey == HeadersKey);

            // fetch parameter list to add to
            var col =
                (ListController<DocumentController>)docController.GetField(parameterCollectionKey);

            double displayDouble = ((bool)display.IsChecked) ? 0 : 1;
            double requiredDouble = ((bool)required.IsChecked) ? 0 : 1;

            // generate new doc with information to add
            var fields = new Dictionary<KeyController, FieldControllerBase>
            {
                [ValueTextKey] = new TextController(key.Text),
                [DisplayKey] = new NumberController(displayDouble),
                [KeyTextKey] = new TextController(value.Text),
                [RequiredKey] = new NumberController(requiredDouble),
            };

            // add to collection & return new document result
            var ret = new DocumentController(fields, DocumentType);

            // apply textbox bindings
            bindToTextBox(key, ret.GetField(KeyTextKey));
            bindToTextBox(value, ret.GetField(ValueTextKey));

            // apply checkbox bindings
            bindToCheckBox(display, ret.GetField(DisplayKey));
            bindToCheckBox(required, ret.GetField(RequiredKey));

            // get the property's type
            ApiProperty.ApiPropertyType type = ApiProperty.ApiPropertyType.Parameter;
            if (parameterCollectionKey == HeadersKey)
                type = ApiProperty.ApiPropertyType.Header;
            if (parameterCollectionKey == AuthHeadersKey)
                type = ApiProperty.ApiPropertyType.AuthHeader;
            if (parameterCollectionKey == AuthParametersKey)
                type = ApiProperty.ApiPropertyType.AuthParameter;

            // make new property in source view
            ApiProperty apiprop = new ApiProperty(key.Text, value.Text, type, ret, required.IsChecked.Value);
            sourceDisplay.AddToListView(apiprop);
            Debug.WriteLine("here: " + key.Text);

            // bind source's fields to those of the editor (key, value)
            TextController textFieldModelController =
                ret.GetField(KeyTextKey) as TextController;
            var sourceBinding = new Binding
            {
                Source = textFieldModelController,
                Path = new PropertyPath(nameof(textFieldModelController.Data)),
                Mode = BindingMode.OneWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            apiprop.XKey.SetBinding(TextBlock.TextProperty, sourceBinding);
            bindToTextBox(apiprop.XValue, ret.GetField(ValueTextKey));

            // bind source visibility to display checkbox which is bound to backend display field of param document
            var binding = new Binding
            {
                Source = display,
                Path = new PropertyPath(nameof(display.IsChecked)),
                Mode = BindingMode.OneWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Converter = new BoolToVisibilityConverter()
            };
            apiprop.SetBinding(ApiProperty.VisibilityProperty, binding);

            // bind ApiRequired property to the required checkbox
            var bindin = new Binding
            {
                Source = display,
                Path = new PropertyPath(nameof(required.IsChecked)),
                Mode = BindingMode.OneWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            apiprop.XRequired.SetBinding(CheckBox.IsCheckedProperty, bindin);


            col.Add(ret);
            return ret;
        }

        /// <summary>
        /// Removes a parameter from a given list of parameter documents.
        /// </summary>
        public static void removeParameter(DocumentController docController,
            DocumentController docModelToRemove,
            KeyController parameterCollectionKey, ApiSourceDisplay sourceDisplay)
        {
            Debug.Assert(docController.DocumentType == DocumentType);
            Debug.Assert(parameterCollectionKey == AuthParametersKey ||
                         parameterCollectionKey == AuthHeadersKey ||
                         parameterCollectionKey == ParametersKey || parameterCollectionKey == HeadersKey);

            ListController<DocumentController> col =
                (ListController<DocumentController>)docController.GetField(parameterCollectionKey);
            col.Remove(docModelToRemove);

        }
        

        /// <summary>
        /// Binds a textbox to a fieldModelController.
        /// </summary>
        private static void bindToTextBox(TextBox tb, FieldControllerBase field)
        {

            // bind URL
            TextController textFieldModelController = field as TextController;
            var sourceBinding = new Binding
            {
                Source = textFieldModelController,
                Path = new PropertyPath(nameof(textFieldModelController.Data)),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            tb.SetBinding(TextBox.TextProperty, sourceBinding);

        }

        /// <summary>
        /// Binds a textbox to a fieldModelController.
        /// </summary>
        private static void bindToCheckBox(CheckBox cb, FieldControllerBase field)
        {

            // bind URL
            NumberController textFieldModelController = field as NumberController;
            var sourceBinding = new Binding
            {
                Source = textFieldModelController,
                Path = new PropertyPath(nameof(textFieldModelController.Data)),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Converter = new DoubleToBoolConverter()
            };
            cb.SetBinding(CheckBox.IsCheckedProperty, sourceBinding);
            textFieldModelController.Data = 1;
        }

        private static void makeBinding(ApiCreatorDisplay apiDisplay, DocumentController docController)
        {

            // set up text bindings
            bindToTextBox(apiDisplay.UrlTB, docController.GetField(BaseUrlKey));
            bindToTextBox(apiDisplay.AuthDisplay.UrlTB, docController.GetField(AuthBaseUrlKey));
            bindToTextBox(apiDisplay.AuthDisplay.KeyTB, docController.GetField(AuthKey));
            // bindToTextBox(apiDisplay.AuthDisplay.SecretTB, docController.Fields[AuthSecretKey));

            // bind drop down list
            NumberController fmcontroller =
                docController.GetField(HttpMethodKey) as NumberController;
            var sourceBinding = new Binding
            {
                Source = fmcontroller,
                Path = new PropertyPath(nameof(fmcontroller.Data)),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            apiDisplay.RequestMethodCB.SetBinding(ComboBox.SelectedIndexProperty, sourceBinding);

        }

        public static void setResults(DocumentController docController, List<DocumentController> documents)
        {
            (docController.GetField(KeyStore.DataKey) as
                ListController<DocumentController>).Set(documents);
        }

        public static FrameworkElement MakeView(DocumentController docController, Context context) {

            ApiSourceDisplay sourceDisplay = new ApiSourceDisplay();
            ApiCreatorDisplay apiDisplay = new ApiCreatorDisplay(docController, sourceDisplay);
            makeBinding(apiDisplay, docController);

            // test bindings are working
            //Debug.WriteLine((docController.GetDereferencedField(BaseUrlKey, context) as TextFieldModelController).Data);
            apiDisplay.UrlTB.Text = "https://itunes.apple.com/search";
            //Debug.WriteLine((docController.GetDereferencedField(BaseUrlKey, context) as TextFieldModelController).Data);

            // this binding makes it s.t. either only the ApiSource or the ApiSourceCreator is visible at a single time
            // TODO: should clients be able to decide for themselves how this is displaying (separate superuser and regular user)
            // or should everyone just see the same view ?
            // bind URL
            var sourceBinding = new Binding {
                Source = apiDisplay,
                Path = new PropertyPath(nameof(apiDisplay.Visibility)),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Converter = new InverseVisibilityConverter()
            };
            sourceDisplay.SetBinding(ApiSourceDisplay.VisibilityProperty, sourceBinding);

            // set up grid to hold UI elements: api size is fixed, results display resizes w/ document container
            Grid containerGrid = new Grid();
            containerGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
            containerGrid.VerticalAlignment = VerticalAlignment.Stretch;
            containerGrid.RowDefinitions.Add(new RowDefinition());
            containerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(450) });
            containerGrid.ColumnDefinitions.Add(new ColumnDefinition());
            containerGrid.Children.Add(apiDisplay);
            containerGrid.Children.Add(sourceDisplay);
            
            return containerGrid;
        }
    }
}
