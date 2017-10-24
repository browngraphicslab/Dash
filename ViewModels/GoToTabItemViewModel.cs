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

        public GoToTabItemViewModel(string title)
        {
            _title = title;
        }

        public void ExecuteFunc()
        {
            throw new NotImplementedException();
        }
    }
}
