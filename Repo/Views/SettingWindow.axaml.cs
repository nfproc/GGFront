using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;

namespace GGFront
{
    // ■■ 設定ウィンドウ ■■
    public partial class SettingWindow : Window
    {
        private SettingViewModel VM;
        public GGFrontSettings NewSetting;

        public SettingWindow(GGFrontSettings oldSetting)
        {
            InitializeComponent();
            VM = new SettingViewModel();
            DataContext = VM;
            VM.GHDLPath = oldSetting.GHDLPath;
            VM.GTKWavePath = oldSetting.GTKWavePath;
            VM.GuessGHDLPath = oldSetting.GuessGHDLPath;
            VM.GuessGTKWavePath = oldSetting.GuessGTKWavePath;
            VM.VHDLStd = oldSetting.VHDLStd;
        }

        // ファイルを検索するボタン（..）が押された場合
        private void PathSearch_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).Name.Equals("btnGHDLPathSearch"))
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Filter = "GHDL (ghdl.exe)|ghdl.exe";
                dialog.FileName = "ghdl.exe";
                if (dialog.ShowDialog() == true)
                    VM.GHDLPath = dialog.FileName;
            }
            else if (((Button)sender).Name.Equals("btnGTKWavePathSearch"))
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Filter = "GTKWave (gtkwave.exe)|gtkwave.exe";
                dialog.FileName = "gtkwave.exe";
                if (dialog.ShowDialog() == true)
                    VM.GTKWavePath = dialog.FileName;
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
