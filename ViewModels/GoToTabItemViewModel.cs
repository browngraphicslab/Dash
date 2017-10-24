using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    class GoToTabItemViewModel : ITabItemViewModel
    {
        private string _title;
        public string Title { get => _title; set => _title = value; }
        public List<DocumentViewModel> Documents { get; set; }

        public GoToTabItemViewModel(string title, List<DocumentViewModel> docs)
        {
            _title = title;
            Documents = docs; 
        }

        public void ExecuteFunc()
        {
            foreach (DocumentViewModel doc in Documents)
            {
                doc.DocumentController.
            }
        }
    }
}
