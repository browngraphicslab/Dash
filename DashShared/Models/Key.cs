﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashShared
{
    public class Key : EntityBase
    {
        public Key(string name)
        {
            Name = name;
        }
    }
}
