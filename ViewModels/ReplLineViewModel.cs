using System;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Dash
{
    public class ReplLineViewModel : ViewModelBase
    {
        public ReplLineViewModel() { }

        public ReplLineViewModel(string lineText, FieldControllerBase value, FieldControllerBase outputValue)
        {
            _outputValue = outputValue;
            LineText = lineText;
            //LineText = GetValueFromResult(value);
            Value = value;
        }

        public event EventHandler Updated;

        public void Update() => Updated?.Invoke(this, EventArgs.Empty);

        public FieldControllerBase Value { get; set; }

        private string _lineText = "";
        public string LineText
        {
            get => _lineText;
            set => SetProperty(ref _lineText, value);
        }

        public string GetLineText() => _lineText;
        
        private string _resultText;
        public string ResultText
        {
            get => _resultText;
            set => SetProperty(ref _resultText, value);
        }

        private bool _editTextValue = false;
        public bool EditTextValue
        {
            get => _editTextValue;
            set
            {
                _editTextValue = value;
                OnPropertyChanged();
                OnPropertyChanged($"NotEditTextValue");
            }
        }

        public bool DisplayableOnly { get; set; }

        public int Indent { get; set; }

        private FieldControllerBase _outputValue;

        public bool NotEditTextValue => !_editTextValue;

        public string GetValueFromResult(FieldControllerBase controller)
        {
            string result;
            try
            {
                if (controller != null)
                {
                    if (controller is ReferenceController)
                    {
                        var r = (ReferenceController)controller;
                        result = $"REFERENCE[{r.FieldKey.Name}  :  {r.GetDocumentController(null).ToString()}]";
                    }
                    else if (controller is FunctionOperatorController)
                    {
                        result = (controller as FunctionOperatorController).getFunctionString();
                    }
                    else
                    {

                        result = controller is BaseListController
                            ? string.Join("      ", (controller as BaseListController)?.Data?.Select(i => i?.ToString()))
                            : controller?.GetValue(null)?.ToString();
                    }

                }
                else
                {
                    result = "error, result controller was null";
                }
            }
            catch (DSLException e)
            {
                result = "      \nException: " + e.GetHelpfulString();
            }
            catch (Exception e)
            {
                result = "Unknown annoying error occurred : " + e.StackTrace;
            }

            return result;
        }



    }
}
