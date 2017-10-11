using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Dash.Controllers;
using DashShared;
using DashShared.Models;
using TextWrapping = Windows.UI.Xaml.TextWrapping;

namespace Dash
{
    public abstract class FieldModelController<T> : FieldControllerBase where T : FieldModel
    {

        public static int threadCount;
        public static object l = new object();

        /// <summary>
        ///     A wrapper for <see cref="DashShared.Models.FieldModel.OutputReferences" />. Change this to propogate changes
        ///     to the server and across the client
        /// </summary>
        public ObservableCollection<ReferenceFieldModelController> OutputReferences;


        protected FieldModelController(T fieldModel) : base(fieldModel)
        {
            
        }

        /// <summary>
        ///     Returns the <see cref="EntityBase.Id" /> for the entity which the controller encapsulates
        /// </summary>
        public string GetId()
        {
            return Model.Id;
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


        public event InkFieldModelController.InkUpdatedHandler InkUpdated;
    }
}