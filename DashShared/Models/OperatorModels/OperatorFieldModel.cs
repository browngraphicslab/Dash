using System;
using DashShared;

namespace DashShared
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

        protected override FieldModelDTO GetFieldDTOHelper()
        {
            return new FieldModelDTO(TypeInfo.Reference, Type);
        }
    }
}
