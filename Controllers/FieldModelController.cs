using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public abstract class FieldModelController : ViewModelBase, IController
    {
        /// <summary>
        ///     The fieldModel associated with this <see cref="FieldModelController"/>, You should only set values on the controller, never directly
        ///     on the fieldModel!
        /// </summary>
        public FieldModel FieldModel { get; set; }
        public delegate void FieldModelUpdated(FieldModelController sender);
        public event FieldModelUpdated FieldModelUpdatedEvent;

        /// <summary>
        ///     A wrapper for <see cref="Dash.FieldModel.InputReference" />. Change this to propogate changes
        ///     to the server and across the client
        /// </summary>
        public ReferenceFieldModel InputReference
        {
            get { return FieldModel.InputReference; }
            set
            {
                if (SetProperty(ref FieldModel.InputReference, value))
                {
                    // update local
                    UpdateValue(ContentController.GetController<DocumentController>(value.DocId).GetField(value.FieldKey));
                    ContentController.GetController<DocumentController>(value.DocId).GetField(value.FieldKey)
                        .FieldModelUpdatedEvent += UpdateValue;
                    // update server
                }
            }
        }


        public void OnDataUpdated()
        {
            FieldModelUpdatedEvent?.Invoke(this);
        }

        /// <summary>
        ///     A wrapper for <see cref="Dash.FieldModel.OutputReferences" />. Change this to propogate changes
        ///     to the server and across the client
        /// </summary>
        public ObservableCollection<ReferenceFieldModel> OutputReferences;

        /// <summary>
        ///     This method is called whenever the <see cref="InputReference" /> changes, it sets the
        ///     Data which is stored in the FieldModel, and should propogate the event to the <see cref="OutputReferences" />
        /// </summary>
        /// <param name="fieldReference"></param>
        protected virtual void UpdateValue(FieldModelController fieldModel)
        {
        }

        public FieldModelController(FieldModel fieldModel)
        {
            // Initialize Local Variables
            FieldModel = fieldModel;
            OutputReferences = new ObservableCollection<ReferenceFieldModel>(fieldModel.OutputReferences);

            // Add Events
            OutputReferences.CollectionChanged += OutputReferences_CollectionChanged;
        }

        private void OutputReferences_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //// we could fine tune this
            //switch (e.Action)
            //{
            //    case NotifyCollectionChangedAction.Add:
            //        break;
            //    case NotifyCollectionChangedAction.Move:
            //        break;
            //    case NotifyCollectionChangedAction.Remove:
            //        break;
            //    case NotifyCollectionChangedAction.Replace:
            //        break;
            //    case NotifyCollectionChangedAction.Reset:
            //        break;
            //    default:
            //        throw new ArgumentOutOfRangeException();
            //}
            var freshList = sender as ObservableCollection<ReferenceFieldModel>;
            Debug.Assert(freshList != null);
            FieldModel.OutputReferences = freshList.ToList();

            // Update Local
            // Update Server
        }

        /// <summary>
        /// Returns the <see cref="EntityBase.Id"/> for the entity which the controller encapsulates
        /// </summary>
        public string GetId()
        {
            return FieldModel.Id;
        }
    }
}
