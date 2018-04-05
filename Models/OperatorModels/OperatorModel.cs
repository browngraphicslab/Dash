using System;
using DashShared;
using DashShared.Models;

namespace Dash
{
    [FieldModelTypeAttribute(TypeInfo.Operator)]
    public class OperatorModel : FieldModel {
        /// <summary>
        /// Type of operator it is; to be used by the server to determine what controller to use for operations 
        /// This should probably eventually be an enum
        /// </summary>
        public KeyModel Type { get; set; }

        public OperatorModel(KeyModel type, string id = null) : base(id)
        {
            Type = type;
        }
        public override string ToString() {
            return Type.ToString();
        }
    }
}
