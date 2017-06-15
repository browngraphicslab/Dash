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
        public Key Key { get; set; }

        private ReferenceFieldModel _inputReference;

        public ReferenceFieldModel InputReference
        {
            get { return _inputReference; }
            set
            {
                if (value == null)//TODO Remove this when JSON serialization is removed
                {
                    return;
                }
                _inputReference = value;
                DocumentController cont = App.Instance.Container.GetRequiredService<DocumentController>();
                cont.GetDocumentAsync(value.DocId).DocumentFieldUpdated += FieldModel_DocumentFieldUpdated;
                UpdateValue(value);
            }
        }

        private void FieldModel_DocumentFieldUpdated(ReferenceFieldModel fieldReference)
        {
            if (fieldReference.Equals(InputReference))
            {
                UpdateValue(fieldReference);
            }
        }

        protected virtual void UpdateValue(ReferenceFieldModel fieldReference)
        {
        }

        protected List<ReferenceFieldModel> OutputReferences { get; set; } = new List<ReferenceFieldModel>();

        public void AddOutputReference(ReferenceFieldModel reference)
        {
            OutputReferences.Add(reference);
        }

        public void RemoveOutputReference(ReferenceFieldModel reference)
        {
            OutputReferences.Remove(reference); 
        }

        /// <summary>
        /// Abstract method to return views using layout information from templates 
        /// </summary>
        public abstract UIElement MakeView(TemplateModel template);

        public FieldModel Copy()
        {
            return MemberwiseClone() as FieldModel;
        }
    }
}
