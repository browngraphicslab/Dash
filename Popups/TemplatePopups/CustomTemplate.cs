﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash.Popups.TemplatePopups
{
    public interface CustomTemplate : DashPopup
    {
        Task<List<string>> GetLayout();
    }
}
