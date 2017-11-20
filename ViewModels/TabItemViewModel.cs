using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public interface ITabItemViewModel
    {
        string Title { get; set; }

        void ExecuteFunc(); 

    }

    /// <summary>
    /// Tab Item View Model used to display when there are no results found
    /// </summary>
    public class NoResultTabViewModel : ITabItemViewModel
    {
        public string Title
        {
            get => "No Results";
            set { } // no setter for this
        }

        public void ExecuteFunc() // no func for this
        {
            
        }
    }
    /// <summary>
    /// Tab Item View Model used to display documents that can be navigated to 
    /// </summary>
    public class GoToTabItemViewModel : ITabItemViewModel
    {
        private string _title;
        public string Title { get => _title; set => _title = value; }
        public Func<DocumentController> Action { get; set; }
        public DocumentController Document; 

        public GoToTabItemViewModel(string title, Func<DocumentController> action, DocumentController dc)
        {
            _title = "Get: " + title;
            Action = action;
            Document = dc; 
        }
            
        public void ExecuteFunc()
        {
            Action?.Invoke();
        }
    }
}
