using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class DocumentAndKeyToEditableScriptBoxConverter : SafeDataToXamlConverter<DocumentController, EditableScriptViewModel>
    {
        public override EditableScriptViewModel ConvertDataToXaml(DocumentController data, object parameter = null)
        {
            if (parameter is KeyController key)
            {
                return new EditableScriptViewModel(new DocumentFieldReference(data.Id, key));
            }

            return null;
        }

        public override DocumentController ConvertXamlToData(EditableScriptViewModel xaml, object parameter = null)
        {
            throw new NotImplementedException();
        }
    }
}
