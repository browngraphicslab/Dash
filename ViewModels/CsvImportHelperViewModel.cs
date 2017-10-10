using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class CsvImportHelperViewModel : ViewModelBase, IHeaderViewModel
    {

        public ObservableCollection<string> Headers { get; set; }

        public ObservableCollection<DocumentTypeToColumnMapViewModel> DocumentTypeMaps { get; set; }

        public CsvImportHelperViewModel(IEnumerable<string> headers)
        {
            Headers = new ObservableCollection<string>(headers);
            DocumentTypeMaps = new ObservableCollection<DocumentTypeToColumnMapViewModel>();
        }

        public void AddHeader(string header)
        {
            if (!Headers.Contains(header))
            {
                Headers.Add(header);
            }
        }

        public void RemoveHeader(string header)
        {
            if (Headers.Contains(header))
            {
                Headers.Remove(header);
            }
        }
    }

    public class DocumentTypeToColumnMapViewModel : ViewModelBase, IHeaderViewModel
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

        public void AddHeader(string header)
        {
            if (!Headers.Contains(header))
            {
                Headers.Add(header);
            }
        }

        public void RemoveHeader(string header)
        {
            if (Headers.Contains(header))
            {
                Headers.Remove(header);
            }
        }
    }

    public interface IHeaderViewModel
    {
        void AddHeader(string header);
        void RemoveHeader(string header);
    }
}
