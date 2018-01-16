using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class SearchResultViewModel
    {

        public SearchResultViewModel(string title, string contextualText)
        {
            ContextualText = contextualText;
            Title = title;
        }

        public string Title { get; private set; }

        public string ContextualText { get; private set; }

    }
}
