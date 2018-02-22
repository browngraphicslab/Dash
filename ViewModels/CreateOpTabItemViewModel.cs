using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Dash
{
    class CreateOpTabItemViewModel : ITabItemViewModel
    {
        private Func<DocumentController> _function;
        public string _title; 
        public CreateOpTabItemViewModel(string title, Func<DocumentController> func) 
        {
            _title = title;
            _function = func;
        }

        public string Title { get => _title; set => _title = value; }


        void ITabItemViewModel.ExecuteFunc()
        {
            if (TabMenu.Instance == null) return; 

            var opController = _function?.Invoke();
            var p = TabMenu.Instance.GetRelativePoint(); 
            
            // using this as a setter for the transform massive hack - LM
            var _ = new DocumentViewModel(opController)
            {
                Position = p
            };

            if (opController != null)
            {
                //freeForm.ViewModel.AddDocument(opController, null);
                TabMenu.Instance.AddToFreeform(opController); 
            }
        }
    }
}
