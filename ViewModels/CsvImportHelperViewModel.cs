using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class CsvImportHelperViewModel : ViewModelBase
    {

        public ObservableCollection<string> Headers { get; set; }

        public ObservableCollection<DocumentTypeToColumnMapViewModel> DocumentTypeMaps { get; set; }

        public CsvImportHelperViewModel(IEnumerable<string> headers)
        {
            Headers = new ObservableCollection<string>(headers);
            DocumentTypeMaps = new ObservableCollection<DocumentTypeToColumnMapViewModel>();
        }

    }

    public class DocumentTypeToColumnMapViewModel : ViewModelBase
    {
        private DocumentType _documentType;

        public DocumentType DocumentType
        {
            get => _documentType;
            set => SetProperty(ref _documentType, value);
        }

        public ObservableCollection<string> Headers { get; set; }

        public DocumentTypeToColumnMapViewModel(DocumentType docType)
        {
            Headers = new ObservableCollection<string>();
            DocumentType = docType;
        }

    }
}
