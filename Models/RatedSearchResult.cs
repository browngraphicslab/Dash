﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class RatedSearchResult : EntityBase
    {
        public double Rating { get; set; }
        public string HelpfulText { get; set; }
        public string ResultDocumentViewId { get; set; }
        public List<StringSearchModel> SearchModels { get; set; } = new List<StringSearchModel>();
    }
}
