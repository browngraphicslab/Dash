using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class SearchResultViewModel
    {

        public SearchResultViewModel(string title, string contextualText, string id)
        {
            ContextualText = contextualText;
            Title = title;
            Id = id;
        }

        public string Title { get; private set; }
        public string Id { get; private set; }
        public string ContextualText { get; private set; }

    }
}
