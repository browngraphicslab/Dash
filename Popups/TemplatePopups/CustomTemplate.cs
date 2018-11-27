using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash.Popups.TemplatePopups
{
    public interface ICustomTemplate : DashPopup
    {
        Task<List<string>> GetLayout();
    }
}
