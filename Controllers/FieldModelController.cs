using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using TextWrapping = Windows.UI.Xaml.TextWrapping;

namespace Dash
{
    public abstract class FieldModelController : ViewModelBase, IController
    {
        // == FIELDS & EVENTS ==
        public FieldModel FieldModel { get; set; }
        public delegate void FieldModelUpdatedHandler(FieldModelController sender, Context context);
        public event FieldModelUpdatedHandler FieldModelUpdated;
        protected virtual bool IsLocal { get { return false; } }

        protected void OnFieldModelUpdated(Context context = null)
        {
            FieldModelUpdated?.Invoke(this, context);
        }

        // == CONSTRUCTOR ==
        /// <summary>
        /// Creates a new FieldModelController from a given FieldModel
        /// </summary>
        /// <param name="fieldModel"></param>
        protected FieldModelController(FieldModel fieldModel = null)
        {
            if (fieldModel != null)
            {
                // Initialize Local Variables
                FieldModel = fieldModel;
                ContentController.AddModel(fieldModel);
                ContentController.AddController(this);

                if (IsLocal)
                {
                    Debug.WriteLine("it is local");
                }
            }
        }

        // == METHODS ==

        /// <summary>
        /// Returns the <see cref="EntityBase.Id"/> for the entity which the controller encapsulates
        /// </summary>
        public string GetId()
        {
            return FieldModel.Id;
        }

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
                TextWrapping = TextWrapping.NoWrap
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

        /// <summary>
        /// Typed Copy() command.
        /// </summary>
        public T Copy<T>() where T : FieldModelController
        {
            return Copy() as T;
        }

        // - OVERRIDEN -
        public override bool Equals(object obj)
        {
            FieldModelController cont = obj as FieldModelController;
            if (cont == null)
            {
                return false;
            }
            return FieldModel.Equals(cont.FieldModel);
        }

        public override int GetHashCode()
        {
            return FieldModel.GetHashCode();
        }

        // - VIRTUAL -
        /// <summary>
        /// Dereferences the current FieldModelController by one level. Override this in special cases. Normally,
        /// this method just returns 'this' FieldModelController.
        /// </summary>
        public virtual FieldModelController Dereference(Context context)
        {
            return this;
        }

        /// <summary>
        /// Dereferences the current FieldModelController until Dereference() does not return a ReferenceFieldController.
        /// Usually, this method just returns 'this' FieldModelController.
        /// </summary>
        public virtual FieldModelController DereferenceToRoot(Context context)
        {
            return this;
        }

        /// <summary>
        /// Typed version of DereferenceToRoot().
        /// </summary>
        public virtual T DereferenceToRoot<T>(Context context) where T : FieldModelController
        {
            return DereferenceToRoot(context) as T;
        }

        /// <summary>
        /// Returns true if this FieldModelController and fmc's TypeInfo fields are the same and are non-None.
        /// </summary>
        public virtual bool CheckType(FieldModelController fmc)
        {
            return (fmc.TypeInfo & TypeInfo) != TypeInfo.None;
        }

        /// <summary>
        ///     This method is called whenever the <see cref="InputReference" /> changes, it sets the
        ///     Data which is stored in the FieldModel, and should propogate the event to the <see cref="OutputReferences" />
        /// </summary>
        /// <param name="fieldReference"></param>
        protected virtual void UpdateValue(FieldModelController fieldModel) { }

        /// <summary>
        /// TODO: Tyler what does this do?
        /// </summary>
        public virtual void Dispose() { }

        // - ABSTRACT -
        /// <summary>
        /// Returns a simple view of the model which the controller encapsulates, for use in a Table Cell
        /// </summary>
        /// <returns></returns>
        public abstract FrameworkElement GetTableCellView();

        /// <summary>
        /// Returns a copy the given FieldModelController which references a new FieldModel / FieldModelDTO.
        /// </summary>
        /// <returns></returns>
        public abstract FieldModelController Copy();

        public abstract FieldModelController GetDefaultController();
        public abstract TypeInfo TypeInfo { get; }

    }
}
