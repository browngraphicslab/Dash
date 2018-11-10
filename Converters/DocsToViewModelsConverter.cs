using Dash;
using DashShared;
using System;
using System.Collections.Generic;
using System.Linq;

public class DocsToViewModelsConverter : SafeDataToXamlConverter<List<DocumentController>, List<DocumentViewModel>>
{
    public override List<DocumentViewModel> ConvertDataToXaml(List<DocumentController> wrapping, object parameter = null)
    {
        return wrapping.Select((d) => new DocumentViewModel(d) { ResizersVisible = false, IsDimensionless = true }).ToList();
    }

    public override List<DocumentController> ConvertXamlToData(List<DocumentViewModel> xaml, object parameter = null)
    {
        throw new NotImplementedException();
    }
}
