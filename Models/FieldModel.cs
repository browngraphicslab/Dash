using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using DashShared;
using Microsoft.Extensions.DependencyInjection;

namespace Dash
{
    /// <summary>
    /// Base data class for documents; holds data and displays it as UIElement 
    /// </summary>
    public abstract class FieldModel : ViewModelBase //TODO Should ViewModelBase be named something else or should FieldModel have a ViewModel
    {
        public delegate void FieldUpdatedEvent(FieldModel model);

        public event FieldUpdatedEvent FieldUpdated;

        private ReferenceFieldModel _inputReference;

        /// <summary>
        /// Optional reference to a field that this field takes as input
        /// </summary>
        public ReferenceFieldModel InputReference
        {
            get { return _inputReference; }
            set
            {
                _inputReference = value;
                DocumentEndpoint cont = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
                cont.GetFieldInDocument(value).FieldUpdated += UpdateValue;
                UpdateValue(cont.GetFieldInDocument(value));
            }
        }

        /// <summary>
        /// Virtual method to update the value of the FieldModel when the Data in the field the
        /// InputReference references is updated
        /// </summary>
        /// <param name="fieldReference"></param>
        protected virtual void UpdateValue(FieldModel model)
        {
        }

        /// <summary>
        /// List of references to fields that take this field as input
        /// </summary>
        protected List<ReferenceFieldModel> OutputReferences { get; set; } = new List<ReferenceFieldModel>();

        public void AddOutputReference(ReferenceFieldModel reference)
        {
            OutputReferences.Add(reference);
        }

        public void RemoveOutputReference(ReferenceFieldModel reference)
        {
            OutputReferences.Remove(reference); 
        }

        public FieldModel Copy()
        {
            return MemberwiseClone() as FieldModel;
        }

        protected virtual void OnFieldUpdated()
        {
            FieldUpdated?.Invoke(this);
        }
    }
}
