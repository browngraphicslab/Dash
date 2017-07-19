﻿using System;
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
        /// <summary>
        ///     The fieldModel associated with this <see cref="FieldModelController"/>, You should only set values on the controller, never directly
        ///     on the fieldModel!
        /// </summary>
        public FieldModel FieldModel { get; set; }
        public delegate void FieldModelUpdatedHandler(FieldModelController sender, Context context);
        public event FieldModelUpdatedHandler FieldModelUpdated;

        protected void OnFieldModelUpdated(Context context = null)
        {
            FieldModelUpdated?.Invoke(this, context);
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


        /// <summary>
        /// Returns the <see cref="EntityBase.Id"/> for the entity which the controller encapsulates
        /// </summary>
        public string GetId()
        {
            return FieldModel.Id;
        }

        public virtual FieldModelController Dereference(Context context)
        {
            return this;
        }

        public virtual FieldModelController DereferenceToRoot(Context context)
        {
            return this;
        }

        public virtual T DereferenceToRoot<T>(Context context) where T : FieldModelController
        {
            return DereferenceToRoot(context) as T;
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

        public abstract FieldModelController Copy();

        public T Copy<T>() where T : FieldModelController
        {
            return Copy() as T;
        }

        public abstract FieldModelController GetDefaultController();
    }
}
