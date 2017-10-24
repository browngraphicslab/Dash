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
        public Action Action { get; set; }
        public GoToTabItemViewModel(string title, Action action)
        {
            _title = title;
            Action = action; 
        }

        public void ExecuteFunc()
        {
            Action?.Invoke(); 
        }
    }
}
