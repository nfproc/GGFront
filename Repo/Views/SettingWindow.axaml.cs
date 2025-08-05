// GGFront: A GHDL/GTKWave GUI Frontend
// Copyright (C) 2018-2025 Naoki FUJIEDA. New BSD License is applied.
//**********************************************************************

using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using GGFront.Models;
using GGFront.ViewModels;

namespace GGFront.Views
{
    // ■■ 設定ウィンドウ ■■
    public partial class SettingWindow : Window
    {
        private SettingViewModel VM;
        public GGFrontSettings? NewSetting;

        public SettingWindow()
        { 
            InitializeComponent();
            Width = Util.Settings.MainWindowWidth;
            VM = new SettingViewModel();
            DataContext = VM;
        }
     
        public SettingWindow(GGFrontSettings oldSetting) : this()
        {
            VM = new SettingViewModel();
            VM.GHDLPath = oldSetting.GHDLPath;
            VM.GTKWavePath = oldSetting.GTKWavePath;
            VM.GuessGHDLPath = oldSetting.GuessGHDLPath;
            VM.GuessGTKWavePath = oldSetting.GuessGTKWavePath;
            VM.VHDLStd = oldSetting.VHDLStd;
            DataContext = VM;
        }

        // ファイルを検索するボタン（..）が押された場合
        private async void PathSearch_Click(object sender, RoutedEventArgs e)
        {
            List<string> exts = new List<string> { "*" + Util.ExecutableExt };
            string senderName = ((Button) sender).Name ?? "";
            if (senderName.Equals("btnGHDLSearch"))
            {
                string? f = await DialogBox.PickFile
                    (this, "Select GHDL Executable", "ghdl" + Util.ExecutableExt, "GHDL", exts, Util.BaseDir);
                if (f != null)
                {
                    VM.GuessGHDLPath = false;
                    VM.GHDLPath = f;
                }
            }
            else if (senderName.Equals("btnGTKWaveSearch"))
            {
                string? f = await DialogBox.PickFile
                    (this, "Select GTKWave Executable", "gtkwave" + Util.ExecutableExt, "GTKWave", exts, Util.BaseDir);
                if (f != null)
                {
                    VM.GuessGTKWavePath = false;
                    VM.GTKWavePath = f;
                }
            }
        }

        // OKボタンが押された場合
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            NewSetting = new GGFrontSettings();
            NewSetting.GHDLPath = VM.GHDLPath;
            NewSetting.GTKWavePath = VM.GTKWavePath;
            NewSetting.GuessGHDLPath = VM.GuessGHDLPath;
            NewSetting.GuessGTKWavePath = VM.GuessGTKWavePath;
            NewSetting.VHDLStd = VM.VHDLStd;
            Close();
        }

        // Cancelボタンが押された場合
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
