// GGFront: A GHDL/GTKWave GUI Frontend
// Copyright (C) 2018-2025 Naoki FUJIEDA. New BSD License is applied.
//**********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using GGFront.ViewModels;
using GGFront.Models;

namespace GGFront.Views
{
    // ■■ メインウィンドウ ■■
    public partial class MainWindow : Window
    {
        private MainViewModel VM;

        public MainWindow()
        {
            InitializeComponent();
            Width = Util.Settings.MainWindowWidth;
            Height = Util.Settings.MainWindowHeight;
            VM = new MainViewModel();
            foreach (int item in Util.ProcLimits)
                VM.ProcLimits.Add(item);
            foreach (int item in Util.SimLimits)
                VM.SimLimits.Add(item);
            VM.SimLimit = Util.Settings.SimLimit;
            VM.ProcLimit = Util.Settings.ProcLimit;
            DataContext = VM;
            AddHandler(DragDrop.DropEvent, Sources_Drop); // DragDrop.DropEvent は XAML から登録不可
            DialogBox.DefaultOwner = this;
            UpdateHierarchy();
        }

        // 設定を表示するボタン（Setting）が押された場合
        private async void ShowSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingWindow win = new SettingWindow(Util.Settings);
            win.Width = (this.WindowState == WindowState.Normal) ? this.Bounds.Width : Util.Settings.MainWindowWidth;
            await win.ShowDialog(this);
            if (win.NewSetting != null)
            {
                Util.Settings.GHDLPath = win.NewSetting.GHDLPath;
                Util.Settings.GTKWavePath = win.NewSetting.GTKWavePath;
                Util.Settings.GuessGHDLPath = win.NewSetting.GuessGHDLPath;
                Util.Settings.GuessGTKWavePath = win.NewSetting.GuessGTKWavePath;
                Util.Settings.VHDLStd = win.NewSetting.VHDLStd;
                Util.Settings.Save();
                UpdateHierarchy();
            }
        }

        // ソースを追加するボタン（Add）が押された場合
        private async void AddSource_Click(object sender, RoutedEventArgs e)
        {
            List<string> exts = new List<string> {"*.vhd", "*.vhdl"};
            List<string> files = await DialogBox.PickFiles(
                this, "Select source file(s)", "", "VHDL Sources", exts, Util.Settings.LastlyUsedFolder);
            if (files.Count > 0) {
                foreach (string file in files)
                    AddSource(file);
                UpdateHierarchy();
                Util.Settings.Save();
            }
        }

        // ソースの一覧にファイルがドロップされた場合      
        private void Sources_Drop(object? sender, DragEventArgs e)
        {
            if (e.Data.GetFiles() is IEnumerable<IStorageItem> files)
            {
                foreach (IStorageItem file in files)
                {
                    string lfile = StorageProviderExtensions.TryGetLocalPath(file) ?? "";
                    if (Regex.IsMatch(lfile, @"\.vhdl?$", RegexOptions.IgnoreCase))
                        AddSource(lfile);
                }
                UpdateHierarchy();
                Util.Settings.Save();
            }
        }

        // ソースファイルの実際の追加処理（AddSource_Click, Sources_Drop 共通）
        private void AddSource(string FileName)
        {
            List<string> currentSources = new List<string>();
            foreach (SourceItem item in VM.SourceCollection)
                if (item.Name != null && item.Name.Equals(FileName))
                    return;

            SourceItem newItem = new SourceItem();
            newItem.Name = FileName;
            newItem.Selected = false;
            VM.SourceCollection.Add(newItem);
            Util.Settings.LastlyUsedFolder = Path.GetDirectoryName(FileName) ?? "";
        }

        // ソースを削除するボタン（Remove）が押された場合
        private void RemoveSource_Click(object sender, RoutedEventArgs e)
        {
            RemoveSelectedSources();
        }

        // Del キーが押された場合
        private void Sources_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
                RemoveSelectedSources();
        }

        // ソースファイルの実際の削除処理（RemoveSource_Click, Sources_KeyDown 共通）
        private void RemoveSelectedSources()
        {
            for (int i = VM.SourceCollection.Count - 1; i >= 0; i -= 1)
                if (VM.SourceCollection[i].Selected)
                    VM.SourceCollection.RemoveAt(i);
            UpdateHierarchy();
        }

        // ソースをリセットするボタン（Reset）が押された場合
        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            if (VM.SourceCollection.Count == 0)
                return;
            if (! DialogBox.WarnAndConfirm("ソースファイル一覧がリセットされます．続けますか？"))
                return;
            VM.SourceCollection.Clear();
            UpdateHierarchy();
        }
        
        // トップモジュールを指定するボタン （Set as Top）が押された場合（未実装）
        private void SetAsTop_Click(object sender, RoutedEventArgs e)
        {
            if (lstHierarchy.SelectedItem is EntityHierarchyItem item)
            {
                Util.CurrentProject.TopModule = item.Name ?? "";
                Util.CurrentProject.GuessTopModule = false;
                UpdateHierarchy();
            }
        }

        // 階層構造を更新するボタン（Refresh）が押された場合
        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            UpdateHierarchy();
        }

        // 階層構造を更新し，リストに表示
        private void UpdateHierarchy()
        {
            Util.CurrentProject.SourceFiles.Clear();
            foreach (SourceItem item in VM.SourceCollection)
                if (item.Name != null)
                    Util.CurrentProject.SourceFiles.Add(item.Name);

            List<EntityHierarchyItem> items = Util.CurrentProject.Hierarchy.Update();
            VM.HierarchyCollection.Clear();
            foreach (EntityHierarchyItem item in items)
                VM.HierarchyCollection.Add(item);

            if (items[0].IsValid)
            {
                VM.WavePath = Util.CurrentProject.WavePath;
                VM.VHDLVersion =
                    (Util.Settings.VHDLStd == 1993) ? MainViewModel.VHDLVersions.VHDL93 :
                    (Util.Settings.VHDLStd == 2008) ? MainViewModel.VHDLVersions.VHDL2008 :
                    (Util.CurrentProject.Hierarchy.IsVHDL2008) ? MainViewModel.VHDLVersions.VHDL2008Guess :
                    MainViewModel.VHDLVersions.VHDL93Guess;
                Util.CurrentProject.UseVHDL2008 =
                    (Util.Settings.VHDLStd == 2008) ||
                    (Util.Settings.VHDLStd == 0 && Util.CurrentProject.Hierarchy.IsVHDL2008);
            }
            else
            {
                VM.WavePath = "";
                VM.VHDLVersion = MainViewModel.VHDLVersions.None;
            }
        }

        // Compile and Simulate ボタンが押された場合
        private void Compile_Click(object sender, RoutedEventArgs e)
        {
            Util.CompileAndSimulate();
        }

        // View Waveform ボタンが押された場合
        private void ViewWave_Click(object sender, RoutedEventArgs e)
        {
            // 入力をチェック
            if (! Util.Settings.Check())
                return;
            Util.Settings.Save();
            if (! Util.CurrentProject.Check())
                return;
            if (! File.Exists(Util.CurrentProject.WavePath))
            {
                DialogBox.Warn("波形ファイルが作成されていません．");
                return;
            }
            // GTKWave を起動
            Util.ExecTool(Util.GetGTKWavePath(), "\"" + Util.CurrentProject.WavePath + "\"", true);
        }
        
        // シミュレーション時間の設定が変更された場合
        private void Simlimit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Util.Settings.SimLimit = VM.SimLimit;
            Util.Settings.Save();
        }

        // GHDL の実行時間制限の設定が変更された場合
        private void Proclimit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Util.Settings.ProcLimit = VM.ProcLimit;
            Util.Settings.Save();
        }

        // 閉じる前にウィンドウのサイズを保存する
        private void Window_Closed(object sender, System.EventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                Util.Settings.MainWindowHeight = (int) this.Bounds.Height;
                Util.Settings.MainWindowWidth = (int) this.Bounds.Width;
                Util.Settings.Save();
            }
        }
    }
}