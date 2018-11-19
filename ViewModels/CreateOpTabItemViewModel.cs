using System;

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
            if (TabMenu.Instance != null)
            {
                var opController = _function?.Invoke();
                if (opController != null)
                {
                    opController.SetPosition(TabMenu.Instance.GetRelativePoint());
                    TabMenu.Instance.AddToFreeform(opController);
                }
            }
        }
    }
}
