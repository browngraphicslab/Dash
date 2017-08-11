using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using TextWrapping = Windows.UI.Xaml.TextWrapping;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using System.Collections.Generic;

namespace Dash
{
    public abstract class FieldModelController : ViewModelBase, IController, IDisposable
    {
        /// <summary>
        ///     The fieldModel associated with this <see cref="FieldModelController"/>, You should only set values on the controller, never directly
        ///     on the fieldModel!
        /// </summary>
        public FieldModel FieldModel { get; set; }
        public delegate void FieldModelUpdatedHandler(FieldModelController sender, FieldUpdatedEventArgs args, Context context);
        public event FieldModelUpdatedHandler FieldModelUpdated;

        protected void OnFieldModelUpdated(FieldUpdatedEventArgs args, Context context = null)
        {
            FieldModelUpdated?.Invoke(this, args ?? new FieldUpdatedEventArgs(TypeInfo.None, DocumentController.FieldUpdatedAction.Update), context);
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


        protected FieldModelController(FieldModel fieldModel, bool isCreatedFromServer)
        {
            // Initialize Local Variables
            FieldModel = fieldModel;
            ContentController.AddModel(fieldModel);
            ContentController.AddController(this);

            if (isCreatedFromServer == false)
            {
                // Add Events
                RESTClient.Instance.Fields.AddField(fieldModel, fieldModelDto =>
                {
                    // Yay!

                }, exception =>
                {
                    // Haaay
                    Debug.WriteLine(exception);

                });
            } 


        }


        /// <summary>
        /// Returns the <see cref="EntityBase.Id"/> for the entity which the controller encapsulates
        /// </summary>
        public string GetId()
        {
            return FieldModel.Id;
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
        /// Returns a simple view of the model which the controller encapsulates, for use in a Table Cell
        /// </summary>
        /// <returns></returns>
        public abstract FrameworkElement GetTableCellView(Context context);

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
        /// Helper method that generates a table cell view for Collections and Lists -- an icon and a wrapped textblock displaying the number of items stored in collection/list 
        /// </summary>
        protected Grid GetTableCellViewForCollectionAndLists(string icon, Action<TextBlock> bindTextOrSetOnce)
        {
            Grid grid = new Grid
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
                TextWrapping = TextWrapping.Wrap
            };
            bindTextOrSetOnce(textBlock);
            grid.Children.Add(textBlock); 
            Grid.SetRow(textBlock, 1);

            return grid; 
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

        public virtual void Dispose()
        {
        }

        public static FieldModelController CreateFromServer(FieldModelDTO fieldModelDto)
        {
            var fieldModel = TypeInfoHelper.CreateFieldModel(fieldModelDto);

            switch (fieldModelDto.Type)
            {
                case TypeInfo.None:
                    throw new NotImplementedException();
                case TypeInfo.Number:
                    return NumberFieldModelController.CreateFromServer(fieldModel as NumberFieldModel);
                case TypeInfo.Text:
                    return TextFieldModelController.CreateFromServer(fieldModel as TextFieldModel);
                case TypeInfo.Image:
                    return ImageFieldModelController.CreateFromServer(fieldModel as ImageFieldModel);
                case TypeInfo.Collection:
                    return DocumentCollectionFieldModelController.CreateFromServer(fieldModel as DocumentCollectionFieldModel);
                case TypeInfo.Document:
                    return DocumentFieldModelController.CreateFromServer(fieldModel as DocumentFieldModel);
                case TypeInfo.Reference:
                    return ReferenceFieldModelController.CreateFromServer(fieldModel as ReferenceFieldModel);
                case TypeInfo.Operator:
                    throw new NotImplementedException();
                    //return OperatorFieldModelController.CreateFromServer(fieldModel as OperatorFieldModel);
                case TypeInfo.Point:
                    return PointFieldModelController.CreateFromServer(fieldModel as PointFieldModel);
                case TypeInfo.List:
                    throw new NotImplementedException();
                case TypeInfo.Ink:
                    return InkFieldModelController.CreateFromServer(fieldModel as InkFieldModel);
                case TypeInfo.RichTextField:
                    return RichTextFieldModelController.CreateFromServer(fieldModel as RichTextFieldModel);
                case TypeInfo.Rectangle:
                    return RectFieldModelController.CreateFromServer(fieldModel as RectFieldModel);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
