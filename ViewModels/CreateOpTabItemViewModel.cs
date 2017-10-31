﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Dash
{
    class CreateOpTabItemViewModel : ITabItemViewModel
    {
        public Func<DocumentController> Function;
        public string _title; 
        public CreateOpTabItemViewModel(string title, Func<DocumentController> func) 
        {
            _title = title;  
            Function = func;
        }

        public string Title { get => _title; set => _title = value; }


        void ITabItemViewModel.ExecuteFunc()
        {
            if (TabMenu.Instance == null) return; 

            var opController = Function?.Invoke();
            var p = TabMenu.Instance.GetRelativePoint(); 
            
            // using this as a setter for the transform massive hack - LM
            var _ = new DocumentViewModel(opController)
            {
                GroupTransform = new TransformGroupData(p, new Point(), new Point(1, 1))
            };

            if (opController != null)
            {
                TabMenu.Instance.AddToFreeform(opController); 
            }
        }
    }
}
