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
    /* 
    public abstract class OperatorFieldModel : FieldModel
    {
        /// <summary>
        /// Keys of all inputs to the operator Document 
        /// </summary>
        public abstract List<Key> InputKeys { get; }

        /// <summary>
        /// Keys of all outputs of the operator Document 
        /// </summary>
        public abstract List<Key> OutputKeys { get; }

        //TODO This makes overriding operators not possible, should this be done differently?
        public abstract List<FieldModel> GetNewInputFields();
        public abstract List<FieldModel> GetNewOutputFields();

        /// <summary>
        /// Abstract method to execute the operator
        /// </summary>
        /// <returns></returns>
        public abstract void Execute(DocumentModel doc);
    }
    */

    public class OperatorFieldModel : FieldModel
    {
        /// <summary>
        /// Type of operator it is; to be used by the server to determine what controller to use for operations 
        /// </summary>
        public string Type { get; set; }

        public OperatorFieldModel(string type)
        {
            Type = type; 
        }
    }
}
