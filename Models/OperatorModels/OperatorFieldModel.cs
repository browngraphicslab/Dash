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

        public override string ToString()
        {
            return Type;
        }

        /// <summary>
        /// True if the operators is a compound operator
        /// </summary>
        public bool IsCompound { get; set; }
    }
}
