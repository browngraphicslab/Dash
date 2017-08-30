using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using TextWrapping = Windows.UI.Xaml.TextWrapping;

namespace Dash
{
    public abstract class FieldModelController : ViewModelBase, IController, IDisposable
    {
        public delegate void FieldModelUpdatedHandler(FieldModelController sender, FieldUpdatedEventArgs args,
            Context context);

        public static int threadCount;
        public static object l = new object();

        /// <summary>
        ///     A wrapper for <see cref="Dash.FieldModel.OutputReferences" />. Change this to propogate changes
        ///     to the server and across the client
        /// </summary>
        public ObservableCollection<ReferenceFieldModelController> OutputReferences;


        protected FieldModelController(FieldModel fieldModel, bool isCreatedFromServer)
        {
            // Initialize Local Variables
            FieldModel = fieldModel;
            ContentController.AddModel(fieldModel);
            ContentController.AddController(this);

            if (isCreatedFromServer == false)
                RESTClient.Instance.Fields.AddField(fieldModel, fieldModelDto =>
                {
                    // Yay!
                }, exception =>
                {
                    // Haaay
                    Debug.WriteLine(exception);
                });
        }

        /// <summary>
        ///     The fieldModel associated with this <see cref="FieldModelController" />, You should only set values on the
        ///     controller, never directly
        ///     on the fieldModel!
        /// </summary>
        public FieldModel FieldModel { get; set; }

        public abstract TypeInfo TypeInfo { get; }


        /// <summary>
        ///     Returns the <see cref="EntityBase.Id" /> for the entity which the controller encapsulates
        /// </summary>
        public string GetId()
        {
            return FieldModel.Id;
        }

        public virtual void Dispose()
        {
        }

        public event FieldModelUpdatedHandler FieldModelUpdated;

        protected void OnFieldModelUpdated(FieldUpdatedEventArgs args, Context context = null)
        {
            FieldModelUpdated?.Invoke(this,
                args ?? new FieldUpdatedEventArgs(TypeInfo.None, DocumentController.FieldUpdatedAction.Update),
                context);
        }

        public virtual bool CheckType(FieldModelController fmc)
        {
            return (fmc.TypeInfo & TypeInfo) != TypeInfo.None;
        }

        public virtual IEnumerable<DocumentController> GetReferences()
        {
            return new List<DocumentController>();
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
        ///     Returns a simple view of the model which the controller encapsulates, for use in a Table Cell
        /// </summary>
        /// <returns></returns>
        public abstract FrameworkElement GetTableCellView(Context context);

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
            grid.RowDefinitions.Add(new RowDefinition {Height = GridLength.Auto});
            grid.RowDefinitions.Add(new RowDefinition {Height = GridLength.Auto});

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
                TextWrapping = TextWrapping.Wrap
            };
            bindTextOrSetOnce(textBlock);
            grid.Children.Add(textBlock);
            Grid.SetRow(textBlock, 1);

            return grid;
        }

        public override bool Equals(object obj)
        {
            var cont = obj as FieldModelController;
            if (cont == null)
                return false;
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

        public static FieldModelController CreateFromServer(FieldModelDTO fieldModelDto)
        {
            lock (l)
            {
                threadCount++;
                Debug.WriteLine($"enter fc : {threadCount}");
            }

            var fieldModel = TypeInfoHelper.CreateFieldModel(fieldModelDto);

            FieldModelController returnController;


            switch (fieldModelDto.Type)
            {
                case TypeInfo.None:
                    throw new NotImplementedException();
                case TypeInfo.Number:
                    returnController = NumberFieldModelController.CreateFromServer(fieldModel as NumberFieldModel);
                    break;
                case TypeInfo.Text:
                    returnController = TextFieldModelController.CreateFromServer(fieldModel as TextFieldModel);
                    break;
                case TypeInfo.Image:
                    returnController = ImageFieldModelController.CreateFromServer(fieldModel as ImageFieldModel);
                    break;
                case TypeInfo.Collection:
                    returnController =
                        DocumentCollectionFieldModelController.CreateFromServer(
                            fieldModel as DocumentCollectionFieldModel);
                    break;
                case TypeInfo.Document:
                    returnController = DocumentFieldModelController.CreateFromServer(fieldModel as DocumentFieldModel);
                    break;
                case TypeInfo.Reference:
                    returnController =
                        ReferenceFieldModelController.CreateFromServer(fieldModel as ReferenceFieldModel);
                    break;
                case TypeInfo.Operator:
                    throw new NotImplementedException();
                //returnController = OperatorFieldModelController.CreateFromServer(fieldModel as OperatorFieldModel);
                case TypeInfo.Point:
                    returnController = PointFieldModelController.CreateFromServer(fieldModel as PointFieldModel);
                    break;
                case TypeInfo.List:
                    throw new NotImplementedException();
                case TypeInfo.Ink:
                    returnController = InkFieldModelController.CreateFromServer(fieldModel as InkFieldModel);
                    break;
                case TypeInfo.RichTextField:
                    returnController = RichTextFieldModelController.CreateFromServer(fieldModel as RichTextFieldModel);
                    break;
                case TypeInfo.Rectangle:
                    returnController = RectFieldModelController.CreateFromServer(fieldModel as RectFieldModel);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            lock (l)
            {
                threadCount--;
                Debug.WriteLine($"exit fc : {threadCount}");
            }

            return returnController;
        }
    }
}