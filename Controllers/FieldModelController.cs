using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using DashShared.Models;
using TextWrapping = Windows.UI.Xaml.TextWrapping;

namespace Dash
{
    public abstract class FieldModelController<T> : FieldControllerBase, IDisposable where T : FieldModel
    {

        public static int threadCount;
        public static object l = new object();

        /// <summary>
        ///     A wrapper for <see cref="DashShared.Models.FieldModel.OutputReferences" />. Change this to propogate changes
        ///     to the server and across the client
        /// </summary>
        public ObservableCollection<ReferenceFieldModelController> OutputReferences;


        protected FieldModelController(T fieldModel) : base(fieldModel)
        {
            
        }

        /// <summary>
        ///     Returns the <see cref="EntityBase.Id" /> for the entity which the controller encapsulates
        /// </summary>
        public string GetId()
        {
            return Model.Id;
        }

        public virtual void Dispose()
        {
        }
        
        public override bool Equals(object obj)
        {
            var cont = obj as FieldModelController<T>;
            if (cont == null)
                return false;
            return Model.Equals(cont.Model);
        }

        public override int GetHashCode()
        {
            return Model.GetHashCode();
        }

        public override FieldControllerBase GetCopy()
        {
            return Copy();
        }

        public abstract FieldModelController<T> Copy();

        public T Copy<T>() where T : FieldControllerBase
        {
            return Copy() as T;
        }

        public static async Task<FieldControllerBase> CreateFromServer(FieldModel fieldModel)
        {
            throw new NotImplementedException();
            FieldControllerBase returnController = null;
            if (fieldModel is NumberFieldModel)
            {
                returnController = NumberFieldModelController.CreateFromServer(fieldModel as NumberFieldModel);
            }
            else if (fieldModel is TextFieldModel)
            {
                returnController = TextFieldModelController.CreateFromServer(fieldModel as TextFieldModel);
            }


            //TODO fill this is

            return returnController;
            /*
            FieldModelController returnController;


            switch (fieldModel.GetType())
            {
                case TypeInfo.None:
                    throw new NotImplementedException();
                case NumberFieldModel:
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
                        await DocumentCollectionFieldModelController.CreateFromServer(
                            fieldModel as DocumentCollectionFieldModel);
                    break;
                case TypeInfo.Document:
                    returnController = await DocumentFieldModelController.CreateFromServer(fieldModel as DocumentFieldModel);
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
                    returnController = new ListFieldModelController<TextFieldModelController>();
                    break;
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

            return returnController;*/
        }

        public event InkFieldModelController.InkUpdatedHandler InkUpdated;
    }
}