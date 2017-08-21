﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public interface ICollectionView
    {
        void ToggleSelectAllItems();

        BaseCollectionViewModel ViewModel { get; }
        
    }
}
