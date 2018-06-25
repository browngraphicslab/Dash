namespace Dash
{

    public class LiteralExpression : ScriptExpression
    {
        private readonly FieldControllerBase _field;

        public LiteralExpression(FieldControllerBase field)
        {
            this._field = field;
        }

        public override FieldControllerBase Execute(Scope scope)
        {
            return _field;
        }

        public FieldControllerBase GetField()
        {
            return _field;
        }

        public override FieldControllerBase CreateReference(Scope scope)
        {
            return _field;
        }

        public override DashShared.TypeInfo Type => _field.TypeInfo;

    }
}
