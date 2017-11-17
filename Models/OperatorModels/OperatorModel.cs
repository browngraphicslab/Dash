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
        public OperatorType Type { get; set; }

        public OperatorModel(OperatorType type, bool isCompound = false, string id = null) : base(id)
        {
            Type = type;
            IsCompound = isCompound;
        }
        public override string ToString() {
            return Type.ToString();
        }

        /// <summary>
        /// True if the operators is a compound operator
        /// </summary>
        public bool IsCompound { get; set; }
    }
}
