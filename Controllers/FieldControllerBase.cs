using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Dash.Controllers;
using DashShared;
using DashShared.Models;
using TextWrapping = DashShared.TextWrapping;

namespace Dash
{
    /// <summary>
    /// abstract controller from which "FieldModelController<T>" should inherit.
    /// This class should hold all the abstract contracts that every FieldModelController must inherit
    /// </summary>
    public abstract class FieldControllerBase : IController<FieldModel>, IDisposable
    {
        public delegate void FieldUpdatedHandler(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context);

        public abstract TypeInfo TypeInfo { get; }
        public event FieldUpdatedHandler FieldModelUpdated;


        public FieldControllerBase(FieldModel model) : base(model)
        {
        }

        protected void OnFieldModelUpdated(FieldUpdatedEventArgs args, Context context = null)
        {
            UpdateOnServer();
            FieldModelUpdated?.Invoke(this,
                args ?? new FieldUpdatedEventArgs(TypeInfo.None, DocumentController.FieldUpdatedAction.Update),
                context);
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

        public abstract bool SetValue(object value);

        public abstract object GetValue(Context context);


        public virtual IEnumerable<DocumentController> GetReferences()
        {
            return new List<DocumentController>();
        }

        public abstract FieldControllerBase GetCopy();


        public virtual bool CheckType(FieldControllerBase fmc)
        {
            return (fmc.TypeInfo & TypeInfo) != TypeInfo.None;
        }


        public abstract FieldControllerBase GetDefaultController();

        /// <summary>
        ///     Returns a simple view of the model which the controller encapsulates, for use in a Table Cell
        /// </summary>
        /// <returns></returns>
        public virtual FrameworkElement GetTableCellView(Context context)
        {
            var tb = new TextingBox(this);
            tb.Document.SetField(TextingBox.FontSizeKey, new NumberController(11), true);
            tb.Document.SetField(TextingBox.TextAlignmentKey, new NumberController(0), true);
            tb.Document.SetHorizontalAlignment(HorizontalAlignment.Stretch);
            tb.Document.SetVerticalAlignment(VerticalAlignment.Stretch);
            tb.Document.SetHeight(Double.NaN);
            tb.Document.SetWidth(Double.NaN);
            return tb.makeView(tb.Document, context);
        }

        /// <summary>
        ///     Helper method for generating a table cell view in <see cref="GetTableCellView" /> for textboxes which may have to
        ///     scroll
        /// </summary>
        /// <param name="bindTextOrSetOnce">
        ///     A method which will create a binding on the passed in textbox, or set the text of the
        ///     textbox to some initial value
        /// </param>
        protected FrameworkElement GetTableCellViewOfScrollableText(Action<TextBlock> bindTextOrSetOnce)
        {
            var textBlock = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                TextAlignment = TextAlignment.Center,
                TextWrapping = Windows.UI.Xaml.TextWrapping.NoWrap,
                FontSize = 11
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

        public virtual void MakeAllViewUI(DocumentController container, KeyController kc, Context context, Panel sp, string id, bool isInterfaceBuilder = false)
        {
            var hstack = new StackPanel { Orientation = Orientation.Horizontal };
            var label = new TextBlock { Text = kc.Name + ": " };
            var refField = new DocumentReferenceController(id, kc);
            var dBox = this is ImageController
                ? new ImageBox(refField).Document
                : new TextingBox(refField).Document;
            hstack.Children.Add(label);
            var ele = dBox.MakeViewUI(context, isInterfaceBuilder);
            hstack.Children.Add(ele);
            sp.Children.Add(hstack);
        }

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
        }


        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~FieldControllerBase() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
