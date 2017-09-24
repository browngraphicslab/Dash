using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public abstract class FieldReference
    {
        public KeyController FieldKey { get; set; }

        protected FieldReference(KeyController fieldKey)
        {
            FieldKey = fieldKey;
        }

        public string GetDocumentId(Context context = null)
        {
            return GetDocumentController(context)?.GetId();
        }

        public abstract FieldReference Copy();

        public abstract DocumentController GetDocumentController(Context context);

        /// <summary>
        /// Resolve this reference field model to the lowest delegate in the given context
        /// </summary>
        /// <param name="context">Context to look for delegates in</param>
        /// <returns>A new FieldModelController that points to the same field in the lowest delegate of the pointed to document</returns>
        public abstract FieldReference Resolve(Context context);

        public FieldModelController Dereference(Context context)
        {
            FieldModelController controller;
            if (context != null)
            {
                if (context.TryDereferenceToRoot(this, out controller))
                {
                    return controller;
                }
            }
            DocumentController doc = GetDocumentController(context);
            context = context ?? new Context();
            var newContext = context;
            if (doc.ShouldExecute(context, FieldKey))
            {
                {

                    newContext = doc.Execute(context, false);
                    if (newContext.TryDereferenceToRoot(this, out controller))
                    {
                        return controller;
                    }
                }
            }

            var fmc = GetDocumentController(newContext)?.GetField(FieldKey);

            return fmc;
        }

        public FieldModelController DereferenceToRoot(Context context)
        {
            FieldModelController reference = Dereference(context);
            while (reference is ReferenceFieldModelController)
            {
                reference = reference.Dereference(context);
            }
            //if (reference == null)
            //{
            //    return null;
            //}
            //if (reference.InputReference != null)
            //{
            //    return reference.InputReference.DereferenceToRoot(context);
            //}
            return reference;
        }

        public T DereferenceToRoot<T>(Context context) where T : FieldModelController
        {
            return DereferenceToRoot(context) as T;
        }

        public override bool Equals(object obj)
        {
            FieldReference reference = obj as FieldReference;
            if (reference == null)
            {
                return false;
            }

            return reference.FieldKey.Equals(FieldKey);
        }

        public override int GetHashCode()
        {
            return FieldKey.GetHashCode();
        }
    }
}
