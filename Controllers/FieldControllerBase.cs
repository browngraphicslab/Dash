using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using Microsoft.Office.Interop.Word;
using Task = System.Threading.Tasks.Task;

namespace Dash
{
    /// <summary>
    /// abstract controller from which "Controller<T>" should inherit.
    /// This class should hold all the abstract contracts that every Controller must inherit
    /// </summary>
    public abstract class FieldControllerBase : Controller<FieldModel>
    {
        public delegate void FieldUpdatedHandler(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context);

        /// <summary>
        ///  Used to flag a field as not being able to be modified.
        ///  Example: When an operator's output is not defined, it may return a Controller for a default field value.
        ///  If someone wants to edit this value, this will indicate that a new Controller needs to be created
        ///  instead of modifying the value in this controller.
        /// </summary>
        public bool ReadOnly = false;
        public abstract TypeInfo TypeInfo { get; }
        public virtual TypeInfo RootTypeInfo => TypeInfo;
        public event FieldUpdatedHandler FieldModelUpdated;

        public object Tag = null;

        protected FieldControllerBase(FieldModel model) : base(model) { }

        public virtual Task InitializeAsync() { return Task.CompletedTask; }

        /// <summary>
        /// Wrapper for the event called when a field model's data is updated
        /// </summary>
        /// <param name="args"></param>
        /// <param name="context"></param>
        protected void OnFieldModelUpdated(FieldUpdatedEventArgs args, Context context = null)
        {
            FieldModelUpdated?.Invoke(this, args ?? new FieldUpdatedEventArgs(TypeInfo, DocumentController.FieldUpdatedAction.Update), context);
        }

        public virtual FieldControllerBase Dereference(Context context) { return this; }
        public virtual FieldControllerBase DereferenceToRoot(Context context)  { return this; }

        public virtual T DereferenceToRoot<T>(Context context) where T : FieldControllerBase
        {
            return DereferenceToRoot(context) as T;
        }

        /// <summary>
        /// Try to set the value on a field, return true if the value was set to the passed in object
        /// and false if the value fails to be set
        /// </summary>
        public abstract bool TrySetValue(object value);

        /// <summary>
        /// Gets the value from the field as an object. 
        /// </summary>
        public abstract object GetValue();


        public virtual bool CheckType(FieldControllerBase fmc) { return (fmc.TypeInfo & TypeInfo) != TypeInfo.None; }

        public virtual bool CheckTypeEquality(FieldControllerBase fmc) => fmc.TypeInfo == TypeInfo;

        public abstract FieldControllerBase Copy();

        public virtual FieldControllerBase CopyIfMapped(Dictionary<FieldControllerBase, FieldControllerBase> mapping) { return null; }

        /// <summary>
        /// Gets the default representation of this fieldcontroller. For example with a number
        /// the default value could be 0. With a string the default value could be an empty string.
        /// </summary>
        /// <returns></returns>
        public abstract FieldControllerBase GetDefaultController();

        /// <summary>
        /// search method which should return whether this field contains the string being searched for.
        /// 
        /// The string should always be lowercased
        /// </summary>
        /// <param name="searchString"></param>
        /// <returns></returns>
        public abstract StringSearchModel SearchForString(Search.SearchMatcher matcher);

        /// <summary>
        /// Convert a field to a script that will evaluate to that field
        /// </summary>
        /// <returns>A string that is a script that will evaluate to this field</returns>
        public abstract string ToScriptString(DocumentController thisDoc = null);

        private bool _fromServer;

        public void MarkFromServer() { _fromServer = true; }

        protected sealed override void SaveOnServer() { base.SaveOnServer(); }
        protected sealed override void UpdateOnServer(UndoCommand command)
        {
            if (IsReferenced)
            {
                base.UpdateOnServer(command);
            }
        }

        protected sealed override void DeleteOnServer() { base.DeleteOnServer(); }

        #region Reference Counting

        private int _refCount = 0;
        /// <summary>
        /// This should only be used for debugging purposes
        /// </summary>
        public int RefCount => _refCount;

        private void AddReference()
        {
            ++_refCount;
            if (_refCount == 1)
            {
                RefInit();
                if (!_fromServer)
                {
                    SaveOnServer();
                }
                //else
                //{
                //    //TODO tfs: This shouldn't technically be necessary
                //    UpdateOnServer(null);
                //}
                _fromServer = false;
            }
        }

        private void ReleaseReference()
        {
            if (_refCount == 1)
            {
                DeleteOnServer();
                RefDestroy();
            }

            --_refCount;
            Debug.Assert(_refCount >= 0);
        }

        protected void ReferenceField(FieldControllerBase field)
        {
            //TODO RefCount: This assert is probably really slow
            //Debug.Assert(field == null || GetReferencedFields().Contains(field));
            if (IsReferenced && field != null)
            {
                field.AddReference();
            }
        }

        protected virtual void ReleaseField(FieldControllerBase field)
        {
            //TODO RefCount: This assert is probably really slow
            //Debug.Assert(field == null || GetReferencedFields().Contains(field));
            if (IsReferenced && field != null)
            {
                field.ReleaseReference();
            }
        }

        protected virtual IEnumerable<FieldControllerBase> GetReferencedFields()
        {
            yield break;
        }

        protected bool IsReferenced => _refCount > 0;

        protected virtual void RefInit()
        {
        }

        protected virtual void RefDestroy()
        {
        }

        public static void MakeRoot(FieldControllerBase field)
        {
            field.AddReference();
        }

        #endregion

    }
}
