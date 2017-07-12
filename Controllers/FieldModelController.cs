using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using TextWrapping = Windows.UI.Xaml.TextWrapping;
using System.Collections.Generic;

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


        public List<DocumentController> DocContextList = null;

        /// <summary>
        ///     A wrapper for <see cref="Dash.FieldModel.InputReference" />. Change this to propogate changes
        ///     to the server and across the client
        /// </summary>
        public ReferenceFieldModelController InputReference
        {
            get { return FieldModel.InputReference; }
            set
            {
                if (SetProperty(ref FieldModel.InputReference, value))
                {
                    // update local
                    var cont = ContentController.DereferenceToRootFieldModel(value, value.DocContextList);
                    cont.FieldModelUpdatedEvent += UpdateValue;
                    UpdateValue(cont);

                    // update server
                }
            }
        }


        public void FireFieldModelUpdated()
        {
            FieldModelUpdatedEvent?.Invoke(this);
        }

        /// <summary>
        ///     A wrapper for <see cref="Dash.FieldModel.OutputReferences" />. Change this to propogate changes
        ///     to the server and across the client
        /// </summary>
        public ObservableCollection<ReferenceFieldModelController> OutputReferences;

        public abstract TypeInfo TypeInfo { get; }

        public virtual bool CheckType(FieldModelController fmc)
        {
            return (fmc.TypeInfo & TypeInfo) != TypeInfo.None;
        }

        /// <summary>
        ///     This method is called whenever the <see cref="InputReference" /> changes, it sets the
        ///     Data which is stored in the FieldModel, and should propogate the event to the <see cref="OutputReferences" />
        /// </summary>
        /// <param name="fieldReference"></param>
        protected virtual void UpdateValue(FieldModelController fieldModel)
        {
        }

        protected FieldModelController(FieldModel fieldModel)
        {
            // Initialize Local Variables
            FieldModel = fieldModel;
            ContentController.AddModel(fieldModel);
            ContentController.AddController(this);

            // Add Events
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
            var freshList = sender as ObservableCollection<ReferenceFieldModelController>;
            Debug.Assert(freshList != null);

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

        /// <summary>
        /// Returns a simple view of the model which the controller encapsulates, for use in a Table Cell
        /// </summary>
        /// <returns></returns>
        public abstract FrameworkElement GetTableCellView();

        /// <summary>
        /// Helper method for generating a table cell view in <see cref="GetTableCellView"/> for textboxes which may have to scroll
        /// </summary>
        /// <param name="bindTextOrSetOnce">A method which will create a binding on the passed in textbox, or set the text of the textbox to some initial value</param>
        protected FrameworkElement GetTableCellViewOfScrollableText(Action<TextBlock> bindTextOrSetOnce)
        {
            var textBlock = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.NoWrap,
            };

            bindTextOrSetOnce(textBlock);

            var scrollViewer = new ScrollViewer
            {
                HorizontalScrollMode = ScrollMode.Enabled,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollMode = ScrollMode.Disabled,
                Content = textBlock
            };

            return scrollViewer;
        }

        public abstract FieldModelController GetDefaultController();
    }
}
