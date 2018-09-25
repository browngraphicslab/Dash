using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;

namespace Dash
{
    /// <summary>
    /// abstract controller from which "Controller<T>" should inherit.
    /// This class should hold all the abstract contracts that every Controller must inherit
    /// </summary>
    public abstract class FieldControllerBase : IController<FieldModel>
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

        protected FieldControllerBase(FieldModel model) : base(model)
        {
        }

        /// <summary>
        /// Wrapper for the event called when a field model's data is updated
        /// </summary>
        /// <param name="args"></param>
        /// <param name="context"></param>
        protected void OnFieldModelUpdated(FieldUpdatedEventArgs args, Context context = null)
        {
            //UpdateOnServer();

            FieldModelUpdated?.Invoke(this, args ?? new FieldUpdatedEventArgs(TypeInfo, DocumentController.FieldUpdatedAction.Update), context);

            //Debug.Assert(ContentController<FieldModel>.CheckAllModels());
        }

        public virtual FieldControllerBase Dereference(Context context)
        {
            return this;
        }

        public virtual FieldControllerBase DereferenceToRoot(Context context)
        {
            return this;
        }

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
        public abstract object GetValue(Context context);


        public virtual IEnumerable<DocumentController> GetReferences()
        {
            return new List<DocumentController>();
        }

        public virtual bool CheckType(FieldControllerBase fmc)
        {
            return (fmc.TypeInfo & TypeInfo) != TypeInfo.None;
        }

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
        ///     Returns a simple view of the model which the controller encapsulates, for use in a Table Cell
        /// </summary>
        /// <returns></returns>
        public virtual FrameworkElement GetTableCellView(Context context)
        {
            var tb = new TextingBox(this);
            tb.Document.SetField<NumberController>(TextingBox.TextAlignmentKey, (int)TextAlignment.Left, true);
            tb.Document.SetHorizontalAlignment(HorizontalAlignment.Stretch);
            tb.Document.SetVerticalAlignment(VerticalAlignment.Stretch);
            tb.Document.SetHeight(double.NaN);
            tb.Document.SetWidth(double.NaN);
            return TextingBox.MakeView(tb.Document, context);
        }

        public virtual void MakeAllViewUI(DocumentController container, KeyController kc, Context context, Panel sp, DocumentController doc)
        {
            var hstack = new StackPanel { Orientation = Orientation.Horizontal };
            var label = new TextBlock { Text = kc.Name + ": " };
            var refField = new DocumentReferenceController(doc, kc);
            var dBox = this is ImageController
                ? new ImageBox(refField).Document
                : new TextingBox(refField).Document;
            hstack.Children.Add(label);
            var ele = dBox.MakeViewUI(context);
            hstack.Children.Add(ele);
            sp.Children.Add(hstack);
        }

        /// <summary>
        /// search method which should return whether this field contains the string being searched for.
        /// 
        /// The string should always be lowercased
        /// </summary>
        /// <param name="searchString"></param>
        /// <returns></returns>
        public abstract StringSearchModel SearchForString(string searchString);

        /// <summary>
        ///     Helper method that generates a table cell view for Collections and Lists -- an icon and a wrapped textblock
        ///     displaying the number of items stored in collection/list
        /// </summary>
        protected Grid GetTableCellViewForCollectionAndLists(string icon, Action<TextBlock> bindTextOrSetOnce)
        {
            var grid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var symbol = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                TextAlignment = TextAlignment.Center,
                FontSize = 40,
                Text = icon
            };
            grid.Children.Add(symbol);

            var textBlock = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                TextAlignment = TextAlignment.Center,
                TextWrapping = Windows.UI.Xaml.TextWrapping.Wrap
            };
            bindTextOrSetOnce(textBlock);
            grid.Children.Add(textBlock);
            Grid.SetRow(textBlock, 1);

            return grid;
        }

        public virtual void DisposeField()
        {
            //DeleteOnServer();
            Disposed?.Invoke(this);
        }

        public delegate void FieldControllerDisposedHandler(FieldControllerBase field);
        public event FieldControllerDisposedHandler Disposed;

    }
}
