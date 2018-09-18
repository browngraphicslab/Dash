using DashShared;

namespace Dash
{
    public abstract class FieldModelController<T> : FieldControllerBase where T : FieldModel
    {

        protected FieldModelController(T fieldModel) : base(fieldModel)
        {
            
        }

        public override bool Equals(object obj)
        {
            var cont = obj as FieldModelController<T>;
            if (cont == null)
                return false;
            return Model.Equals(cont.Model);
        }

        public override int GetHashCode()
        {
            return Model.GetHashCode();
        }

        public T Copy<T>() where T : FieldControllerBase
        {
            return Copy() as T;
        }

        public virtual void Dispose()
        {
            // TODO why is the dispose not implemented for most field model controllers!
        }
    }
}
