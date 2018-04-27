using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class ReplLineViewModel : ViewModelBase
    {
        public ReplLineViewModel(string lineText, string valueText, FieldControllerBase outputValue)
        {
            _outputValue = outputValue;
            LineText = lineText;
            LineValueText = valueText;
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
