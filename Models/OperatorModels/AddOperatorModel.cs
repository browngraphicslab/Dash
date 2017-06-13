using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash.Models.OperatorModels
{
    public class AddOperatorModel : OperatorFieldModel
    {
        public override Dictionary<string, FieldModel> Execute()
        {
            Dictionary<string, FieldModel> result = new Dictionary<string, FieldModel>(1);
            double a = 0;//DocumentController.GetFieldInDocument(InputReferences["a"]);
            double b = 0; //DocumentController.GetFieldInDocument(InputReferences["b"]);
            result["sum"] = new NumberFieldModel(a + b);
            return result;
        }
    }
}
