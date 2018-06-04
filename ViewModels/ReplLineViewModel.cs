using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class ReplLineViewModel : ViewModelBase
    {
        //TODO have this value be dragged out onto the workspace
        //this is the stored value of every line;
        private FieldControllerBase _value;
        public ReplLineViewModel(string lineText, FieldControllerBase value, FieldControllerBase outputValue)
        {
            _outputValue = outputValue;
            LineText = lineText;
            LineValueText = GetValueFromResult(value);
            _value = value;
        }

        private string GetValueFromResult(FieldControllerBase controller)
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
                result = e.GetHelpfulString();
            }
            catch (Exception e)
            {
                result = "Unknown annoying error occurred : " + e.StackTrace;
            }

            return result;
        }

        private string _lineText = "";
        public string LineText
        {
            get => ">> "+_lineText;
            set => SetProperty(ref _lineText, value);
        }

        private string _lineValueText = "";
        public string LineValueText
        {
            get => "     " + _lineValueText;
            set => SetProperty(ref _lineValueText, value);
        }


        public string GetLineText()
        {
            return _lineText;

        }

        private FieldControllerBase _outputValue;
    }
}
