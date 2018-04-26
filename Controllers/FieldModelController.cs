using System.Collections.ObjectModel;
using DashShared;

namespace Dash
{
    public abstract class FieldModelController<T> : FieldControllerBase where T : FieldModel
    {

        /// <summary>
        ///     A wrapper for <see cref="DashShared.Models.FieldModel.OutputReferences" />. Change this to propogate changes
        ///     to the server and across the client
        /// </summary>
        public ObservableCollection<ReferenceController> OutputReferences;


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

        public override FieldControllerBase GetCopy()
        {
            return Copy();
        }

        public abstract FieldModelController<T> Copy();

        public T Copy<T>() where T : FieldControllerBase
        {
            return Copy() as T;
        }
        public override string GetSearchableString()
        {
            return string.Empty;
        }

        public virtual void Dispose()
        {
            // TODO why is the dispose not implemented for most field model controllers!
        }


        public event InkController.InkUpdatedHandler InkUpdated;
    }
}