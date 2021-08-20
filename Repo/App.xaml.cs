// GGFront: A GHDL/GTKWave GUI Frontend
// Copyright (C) 2018-2021 Naoki FUJIEDA. New BSD License is applied.
//**********************************************************************

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace GGFront
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        [STAThread]
        public static void Main()
        {
            Util.Initialize();
            App app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
