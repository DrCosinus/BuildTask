﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildTask
{
    class Program
    {
        static void Main(string[] args)
        {
            Environment.Exit(new Builder().Run(args));
        }
    }
}
