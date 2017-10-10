using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public interface IHeaderViewModel
    {
        void AddHeader(string header);
        void RemoveHeader(string header);
    }

    public interface IDataDocTypeViewModel
    {
        void AddDataDocType(DocumentType docType);
        void RemoveDataDocType(DocumentType docType);
    }

    public class CsvImportHelperViewModel : ViewModelBase, IHeaderViewModel, IDataDocTypeViewModel
    {

        public ObservableCollection<string> Headers { get; set; }

        public ObservableCollection<DocumentTypeToColumnMapViewModel> DocumentTypeMaps { get; set; }

        public ObservableCollection<DocumentType> DataDocTypes { get; set; }

        public CsvImportHelperViewModel(IEnumerable<string> headers)
        {
            Headers = new ObservableCollection<string>(headers);
            DocumentTypeMaps = new ObservableCollection<DocumentTypeToColumnMapViewModel>();
            DataDocTypes = new ObservableCollection<DocumentType>();
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

        public void AddDataDocType(DocumentType docType)
        {
            if (!DataDocTypes.Contains(docType))
            {
                DataDocTypes.Add(docType);
            }
        }

        public void RemoveDataDocType(DocumentType docType)
        {
            if (DataDocTypes.Contains(docType))
            {
                DataDocTypes.Remove(docType);
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

}
