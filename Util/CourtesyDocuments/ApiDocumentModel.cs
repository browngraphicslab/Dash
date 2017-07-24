using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Dash;
using DashShared;

namespace Dash
{
    /// <summary>
    /// Wrapper document to display the ApiSourceCreatorDisplay Usercontrol.
    /// </summary>
    internal class ApiDocumentModel : CourtesyDocument
    {
        public static DocumentType DocumentType =
            new DocumentType("453ACC23-14EF-4990-A36D-53D5EBE2734D", "Api Source Creator");

        public static Key BaseUrlKey = new Key("C20E4B2B-A633-4C2C-ACBF-757FF6AC8E5A", "Base URL");
        public static Key HttpMethodKey = new Key("1CE4047D-1813-410B-804E-BA929D8CB4A4", "Http Method");
        public static Key HeadersKey = new Key("6E9D9F12-E978-4E61-85C7-707A0C13EFA7", "Headers");
        public static Key ParametersKey = new Key("654A4BDF-1AE0-432A-9C90-CCE9B4809870", "Parameter");

        public static Key AuthHttpMethodKey = new Key("D37CCAC0-ABBC-4861-BEB4-8C079049DCF8", "Auth Method");
        public static Key AuthBaseUrlKey = new Key("7F8709B6-2C9B-43D0-A86C-37F3A1517884", "Auth URL");
        public static Key AuthKey = new Key("1E5B5398-9349-4585-A420-EDBFD92502DE", "Auth Key");
        public static Key AuthSecretKey = new Key("A690EFD0-FF35-45FF-9795-372D0D12711E", "Auth Secret");
        public static Key AuthHeadersKey = new Key("E1773B06-F54C-4052-B888-AE85278A7F88", "Auth Header");
        public static Key AuthParametersKey = new Key("CD546F0B-A0BA-4C3B-B683-5B2A0C31F44E", "Auth Parameter");

        public static Key KeyTextKey = new Key("388F7E20-4424-4AC0-8BB7-E8CCF2279E60", "Key");
        public static Key ValueTextKey = new Key("F89CAD72-271F-48E6-B233-B6BA766E613F", "Value");
        public static Key RequiredKey = new Key("D4FCBA25-B540-4E17-A17A-FCDE775B97F9", "Required");
        public static Key DisplayKey = new Key("2B80D6A8-4224-4EC7-9BDF-DFD2CC20E463", "Display");


        public ApiDocumentModel()
        {
            var fields = new Dictionary<Key, FieldModelController>
            {
                [BaseUrlKey] = new TextFieldModelController(""),
                [HttpMethodKey] = new NumberFieldModelController(0),
                [AuthBaseUrlKey] = new TextFieldModelController(""),
                [AuthHttpMethodKey] = new NumberFieldModelController(0),
                [AuthSecretKey] = new TextFieldModelController(""),
                [AuthKey] = new TextFieldModelController(""),
                [ParametersKey] = new DocumentCollectionFieldModelController(new List<DocumentController>()),
                [HeadersKey] = new DocumentCollectionFieldModelController(new List<DocumentController>()),
                [AuthParametersKey] =
                new DocumentCollectionFieldModelController(new List<DocumentController>()),
                [AuthHeadersKey] = new DocumentCollectionFieldModelController(new List<DocumentController>()),
                [DashConstants.KeyStore.WidthFieldKey] = new NumberFieldModelController(550),
                [DashConstants.KeyStore.HeightFieldKey] = new NumberFieldModelController(400),
                [DashConstants.KeyStore.PositionFieldKey] = new PointFieldModelController(new Windows.Foundation.Point(0,0)),
                [DashConstants.KeyStore.ScaleAmountFieldKey] = new PointFieldModelController(1, 1),
                [DashConstants.KeyStore.ScaleCenterFieldKey] = new PointFieldModelController(0, 0),

                // TODO: differentiating similar fields in different documents for operator view (Not sure what this means Anna)
                [DocumentCollectionFieldModelController.CollectionKey] =
                new DocumentCollectionFieldModelController(new List<DocumentController>())
            };
            Document = new DocumentController(fields, DocumentType);
            Document.SetField(DashConstants.KeyStore.IconTypeFieldKey, new NumberFieldModelController((double)IconTypeEnum.Api), true);
            Document.SetActiveLayout(new DefaultLayout(0, 0, 400, 400).Document, true, true);
        }

        /// <summary>
        /// Generates a new document containing the parameter information and adds that document to
        /// the corresponding DocumentCollectionFieldModel representing that parameter's list (i.e. Header, AuthParameters).
        /// </summary>
        /// <returns>The newly generated document representing the newly added parameter.</returns>
        public static DocumentController addParameter(DocumentController docController, TextBox key,
            TextBox value, CheckBox display,
            CheckBox required, Key parameterCollectionKey, ApiSourceDisplay sourceDisplay)
        {
            Debug.Assert(docController.DocumentType == DocumentType);
            Debug.Assert(parameterCollectionKey == AuthParametersKey ||
                         parameterCollectionKey == AuthHeadersKey ||
                         parameterCollectionKey == ParametersKey || parameterCollectionKey == HeadersKey);

            // fetch parameter list to add to
            var col =
                (DocumentCollectionFieldModelController)docController.GetField(parameterCollectionKey);

            double displayDouble = ((bool)display.IsChecked) ? 0 : 1;
            double requiredDouble = ((bool)required.IsChecked) ? 0 : 1;

            // generate new doc with information to add
            var fields = new Dictionary<Key, FieldModelController>
            {
                [ValueTextKey] = new TextFieldModelController(key.Text),
                [DisplayKey] = new NumberFieldModelController(displayDouble),
                [KeyTextKey] = new TextFieldModelController(value.Text),
                [RequiredKey] = new NumberFieldModelController(requiredDouble),
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
            TextFieldModelController textFieldModelController =
                ret.GetField(KeyTextKey) as TextFieldModelController;
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


            col.AddDocument(ret);
            return ret;
        }

        /// <summary>
        /// Removes a parameter from a given list of parameter documents.
        /// </summary>
        public static void removeParameter(DocumentController docController,
            DocumentController docModelToRemove,
            Key parameterCollectionKey, ApiSourceDisplay sourceDisplay)
        {
            Debug.Assert(docController.DocumentType == DocumentType);
            Debug.Assert(parameterCollectionKey == AuthParametersKey ||
                         parameterCollectionKey == AuthHeadersKey ||
                         parameterCollectionKey == ParametersKey || parameterCollectionKey == HeadersKey);

            DocumentCollectionFieldModelController col =
                (DocumentCollectionFieldModelController)docController.GetField(parameterCollectionKey);
            col.RemoveDocument(docModelToRemove);

        }

        // inherited
        protected override DocumentController GetLayoutPrototype()
        {
            throw new NotImplementedException();
        }

        protected override DocumentController InstantiatePrototypeLayout()
        {
            throw new NotImplementedException();
        }

        public override FrameworkElement makeView(DocumentController docController,
            Context context, bool isInterfaceBuilderLayout = false)
        {
            return MakeView(docController, context);
        }

        /// <summary>
        /// Binds a textbox to a fieldModelController.
        /// </summary>
        private static void bindToTextBox(TextBox tb, FieldModelController field)
        {

            // bind URL
            TextFieldModelController textFieldModelController = field as TextFieldModelController;
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
        private static void bindToCheckBox(CheckBox cb, FieldModelController field)
        {

            // bind URL
            NumberFieldModelController textFieldModelController = field as NumberFieldModelController;
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
            NumberFieldModelController fmcontroller =
                docController.GetField(HttpMethodKey) as NumberFieldModelController;
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
            (docController.GetField(DocumentCollectionFieldModelController.CollectionKey) as
                DocumentCollectionFieldModelController).SetDocuments(documents);
        }

        public static FrameworkElement MakeView(DocumentController docController,
            Context context, bool isInterfaceBuilderLayout = false) {

            ApiSourceDisplay sourceDisplay = new ApiSourceDisplay();
            ApiCreatorDisplay apiDisplay = new ApiCreatorDisplay(docController, sourceDisplay);
            makeBinding(apiDisplay, docController);

            // test bindings are working
            Debug.WriteLine((docController.GetDereferencedField(BaseUrlKey, context) as TextFieldModelController).Data);
            apiDisplay.UrlTB.Text = "https://itunes.apple.com/search";
            Debug.WriteLine((docController.GetDereferencedField(BaseUrlKey, context) as TextFieldModelController).Data);

            // generate collection view preview for results
            var resultView =
                docController.GetDereferencedField(DocumentCollectionFieldModelController.CollectionKey, context) as
                    DocumentCollectionFieldModelController;

            // make collection view display framework element
            var data = resultView;
            var collectionFieldModelController = data.DereferenceToRoot<DocumentCollectionFieldModelController>(context);
            Debug.Assert(collectionFieldModelController != null);

            throw new Exception("Need to temporarily change the arguments to CollectionViewModel");
            var collectionViewModel = new CollectionViewModel(null, null); //  collectionFieldModelController);
            var collectionDisplay = new CollectionView(collectionViewModel);



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
            Grid.SetColumn(collectionDisplay, 1);
            containerGrid.Children.Add(apiDisplay);
            containerGrid.Children.Add(sourceDisplay);
            containerGrid.Children.Add(collectionDisplay);

            collectionDisplay.MaxWidth = 550;
            collectionDisplay.HorizontalAlignment = HorizontalAlignment.Left;
            collectionDisplay.Width = 500;
            collectionDisplay.Height = 500;

            // return all results
            if (isInterfaceBuilderLayout) {
                return new SelectableContainer(containerGrid, docController);
            }
            return containerGrid;
        }
    }
}