﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Dash
{
    public interface ICommandBarBased
    {
        void CommandBarOpen(bool status);
        ComboBox GetComboBox();
    }
}
