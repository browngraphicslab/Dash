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
}
