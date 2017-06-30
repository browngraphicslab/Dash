using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using DashShared;
using Microsoft.Extensions.DependencyInjection;

namespace Dash
{
    public class OperatorFieldModel : FieldModel
    {
        /// <summary>
        /// Type of operator it is; to be used by the server to determine what controller to use for operations 
        /// This should probably eventually be an enum
        /// </summary>
        public string Type { get; set; }

        public OperatorFieldModel(string type)
        {
            Type = type; 
        }
    }
}
