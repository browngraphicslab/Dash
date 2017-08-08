using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class DocumentControllerToViewModelConverter : SafeDataToXamlConverter<DocumentViewModelParameters, DocumentViewModel>
    {
        public override DocumentViewModel ConvertDataToXaml(DocumentViewModelParameters data, object parameter = null)
        {
            return new DocumentViewModel(data.Controller, data.IsInInterfaceBuilder, data.Context);
        }

        public override DocumentViewModelParameters ConvertXamlToData(DocumentViewModel xaml, object parameter = null)
        {
            throw new System.NotImplementedException();
        }
    }
}
