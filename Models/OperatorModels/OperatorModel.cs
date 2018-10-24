using DashShared;
using Newtonsoft.Json;

namespace Dash
{
    [FieldModelTypeAttribute(TypeInfo.Operator)]
    public class OperatorModel : FieldModel {
        /// <summary>
        /// Type of operator it is; to be used by the server to determine what controller to use for operations 
        /// This should probably eventually be an enum
        /// </summary>
        public string TypeId { get; set; }

        public OperatorModel(KeyModel type, string id = null) : base(id)
        {
            TypeId = type.Id;
        }

        [JsonConstructor]
        public OperatorModel(string typeId, string id = null) : base(id)
        {
            TypeId = typeId;
        }

        public override string ToString()
        {
            return TypeId;
        }
    }
}
