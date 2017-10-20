﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    /// <summary>
    /// Abstract class which contains the utilities for referencing fields on documents
    /// in different ways
    /// </summary>
    public abstract class FieldReference
    {
        /// <summary>
        /// The key for the field that is being referenced
        /// </summary>
        public KeyController FieldKey { get; set; }

        /// <summary>
        /// create a new field reference to some field associated with the passed in key controller
        /// </summary>
        /// <param name="fieldKey"></param>
        protected FieldReference(KeyController fieldKey)
        {
            FieldKey = fieldKey;
        }

        /// <summary>
        /// Returns the document id for the document which contains the passed in field
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
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


        public FieldControllerBase Dereference(Context context)
        {
            FieldControllerBase controller;
            if (context != null)
            {
                if (context.TryDereferenceToRoot(this, out controller))
                {
                    return controller;
                }
            }
            var doc = GetDocumentController(context);
            if (doc != null)
            {
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
            return null;
        }

        public FieldControllerBase DereferenceToRoot(Context context)
        {
            FieldControllerBase reference = Dereference(context);
            while (reference is ReferenceFieldModelController)
            {
                reference = reference.Dereference(context);
            }

            return reference;
        }

        public T DereferenceToRoot<T>(Context context) where T : FieldControllerBase
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

        public abstract ReferenceFieldModelController GetReferenceController();
    }
}
