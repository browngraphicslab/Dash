namespace Dash
{

    public class LiteralExpression : ScriptExpression
    {
        private FieldControllerBase field;

        public LiteralExpression(FieldControllerBase field)
        {
            this.field = field;
        }

        public override FieldControllerBase Execute(ScriptState state)
        {
            return field;
        }

        public FieldControllerBase GetField()
        {
            return field;
        }

        public override FieldControllerBase CreateReference(ScriptState state)
        {
            if (field is TextController && false)
            {
                return new TextController('"' + ((TextController) field).Data +
                                         '"');
            }
            return field;
        }

        public override DashShared.TypeInfo Type => field.TypeInfo;

    }
}
