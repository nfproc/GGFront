﻿// GGFront: A GHDL/GTKWave GUI Frontend
// Copyright (C) 2018-2022 Naoki FUJIEDA. New BSD License is applied.
//**********************************************************************

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GGFront
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<SourceItem> sourceCollection;
        ObservableCollection<EntityHierarchyItem> hierarchyCollection;

        public MainWindow()
        {
            InitializeComponent();
            txtGHDLPath.Text = Util.settings.GHDLPath;
            txtGTKWavePath.Text = Util.settings.GTKWavePath;
            chkGuessGHDLPath.IsChecked = Util.settings.guessGHDLPath;
            chkGuessGTKWavePath.IsChecked = Util.settings.guessGTKWavePath;
            sourceCollection = new ObservableCollection<SourceItem>();
            hierarchyCollection = new ObservableCollection<EntityHierarchyItem>();
            UpdateHierarchy();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            lstSources.ItemsSource = sourceCollection;
            lstHierarchy.ItemsSource = hierarchyCollection;
            InitializeTimeLimit();
        }

        // 時間制限のコンボボックスの準備
        public void InitializeTimeLimit()
        {
            int numProcItems = 0, numSimItems = 0;
            int selProc = 0, selSim = 0;
            ObservableCollection<LimitSelectorItem> procItems = new ObservableCollection<LimitSelectorItem>();
            ObservableCollection<LimitSelectorItem> simItems = new ObservableCollection<LimitSelectorItem>();
            foreach (int item in Util.procLimits)
            {
                if (Util.settings.procLimit == item)
                    selProc = numProcItems;
                procItems.Add(new LimitSelectorItem { Id = numProcItems, Name = (item / 1000) + " sec.", ProcTime = item });
                numProcItems += 1;
            }
            foreach (string item in Util.simLimits)
            {
                if (Util.settings.simLimit == item)
                    selSim = numSimItems;
                string limitName = Regex.Replace(item, @"([0-9]+)000ms", "$1 sec.");
                limitName = Regex.Replace(limitName, @"([0-9]+)ms", "$1 msec.");
                simItems.Add(new LimitSelectorItem { Id = numSimItems, Name = limitName, SimTime = item });
                numSimItems += 1;
            }
            cmbRealLimit.ItemsSource = procItems;
            cmbRealLimit.SelectedIndex = selProc;
            cmbSimLimit.ItemsSource = simItems;
            cmbSimLimit.SelectedIndex = selSim;
        }

        // ファイルを検索するボタン（..）が押された場合
        private void PathSearch_Click(object sender, RoutedEventArgs e)
        {
            if (((Button) sender).Name.Equals("btnGHDLPathSearch"))
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Filter = "GHDL (ghdl.exe)|ghdl.exe";
                dialog.FileName = "ghdl.exe";
                if (dialog.ShowDialog() == true)
                {
                    txtGHDLPath.Text = dialog.FileName;
                    Util.settings.Save();
                }
            } else if (((Button)sender).Name.Equals("btnGTKWavePathSearch"))
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Filter = "GTKWave (gtkwave.exe)|gtkwave.exe";
                dialog.FileName = "gtkwave.exe";
                if (dialog.ShowDialog() == true)
                {
                    txtGTKWavePath.Text = dialog.FileName;
                    Util.settings.Save();
                }
            }
        }

        // ソースを追加するボタン（Add）が押された場合
        private void AddSource_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "VHDL Sources (*.vhd, *.vhdl)|*.vhd;*.vhdl";
            dialog.Multiselect = true;
            if (dialog.ShowDialog() == true)
            {
                foreach (string FileName in dialog.FileNames)
                    AddSource(FileName);
                UpdateHierarchy();
            }
        }

        // ソースの一覧にファイルがドロップされた場合
        private void Sources_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                foreach (string FileName in (string[]) e.Data.GetData(DataFormats.FileDrop))
                    if (Regex.IsMatch(FileName, @"\.[vV][hH][dD][lL]?$"))
                        AddSource(FileName);
                UpdateHierarchy();
            }
        }

        private void AddSource (string FileName)
        {
            List<string> currentSources = new List<string>();
            foreach (SourceItem item in sourceCollection)
                if (item.Name.Equals(FileName))
                    return;

            SourceItem newItem = new SourceItem();
            newItem.Name = FileName;
            newItem.Selected = false;
            sourceCollection.Add(newItem);
        }

        // ソースを削除するボタン（Remove）またはDelキーが押された場合
        private void RemoveSource_Click(object sender, RoutedEventArgs e)
        {
            RemoveSelectedSources();
        }

        private void Sources_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
                RemoveSelectedSources();
        }

        private void RemoveSelectedSources()
        {
            for (int i = sourceCollection.Count - 1; i >= 0; i -= 1)
                if (sourceCollection[i].Selected)
                    sourceCollection.RemoveAt(i);
            UpdateHierarchy();
        }

        // ソースをリセットするボタン（Reset）が押された場合
        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            if (sourceCollection.Count == 0)
                return;
            if (!Util.WarnAndConfirm("ソースファイル一覧がリセットされます．続けますか？"))
                return;
            sourceCollection.Clear();
            UpdateHierarchy();
        }

        // トップモジュールを指定するボタン （Set as Top）が押された場合
        private void SetAsTop_Click(object sender, RoutedEventArgs e)
        {
            EntityHierarchyItem item = (EntityHierarchyItem)lstHierarchy.SelectedItem;
            if (item == null)
                return;
            Util.currentProject.topModule = item.Name;
            Util.currentProject.guessTopModule = false;
            UpdateHierarchy();
        }

        // 階層構造を更新するボタン（Refresh）が押された場合
        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            UpdateHierarchy();
        }

        // 階層構造を更新し，リストに表示
        private void UpdateHierarchy()
        {
            Util.currentProject.sourceFiles.Clear();
            foreach (SourceItem item in sourceCollection)
                Util.currentProject.sourceFiles.Add(item.Name);

            List<EntityHierarchyItem> items = Util.currentProject.hierarchy.Update();
            hierarchyCollection.Clear();
            foreach (EntityHierarchyItem item in items)
                hierarchyCollection.Add(item);

            if (items[0].IsValid)
            {
                string wavePath = Util.currentProject.wavePath;
                lblTopModule.Text = "(Waveform: " + System.IO.Path.GetFileName(wavePath) + ")";
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
            if (!Util.settings.Check())
                return;
            Util.settings.Save();
            if (!Util.currentProject.Check())
                return;
            if (!File.Exists(Util.currentProject.wavePath))
            {
                Util.Warn("波形ファイルが作成されていません．");
                return;
            }
            Util.ExecTool(Util.GetGTKWavePath(), "\"" + Util.currentProject.wavePath + "\"", true);
        }

        // GHDL, GTKWave のパスが入力された場合
        private void AppPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (((TextBox) sender).Name.Equals("txtGHDLPath"))
            {
                Util.settings.GHDLPath = txtGHDLPath.Text;
            }
            if (((TextBox)sender).Name.Equals("txtGTKWavePath"))
            {
                Util.settings.GTKWavePath = txtGTKWavePath.Text;
            }
        }

        // GHDL, GTKWave を所定の場所とするチェックがされた場合
        private void GuessAppPath_Checked(object sender, RoutedEventArgs e)
        {
            if (((CheckBox) sender).Name.Equals("chkGuessGHDLPath"))
            {
                Util.settings.guessGHDLPath = true;
                if (! File.Exists(Util.GetGHDLPath()))
                {
                    Util.Warn("GHDLが所定の場所に見つかりません．自分で指定してください．");
                    Util.settings.guessGHDLPath = false;
                    chkGuessGHDLPath.IsChecked = false;
                }
                else
                {
                    txtGHDLPath.IsEnabled = false;
                    btnGHDLPathSearch.IsEnabled = false;
                }
                Util.settings.Save();
            }
            if (((CheckBox) sender).Name.Equals("chkGuessGTKWavePath"))
            {
                Util.settings.guessGTKWavePath = true;
                if (! File.Exists(Util.GetGTKWavePath()))
                {
                    Util.Warn("GTKWaveが所定の場所に見つかりません．自分で指定してください．");
                    Util.settings.guessGTKWavePath = false;
                    chkGuessGTKWavePath.IsChecked = false;
                }
                else
                {
                    txtGTKWavePath.IsEnabled = false;
                    btnGTKWavePathSearch.IsEnabled = false;
                }
                Util.settings.Save();
            }
        }

        // GHDL, GTKWave を所定の場所とするチェックが外れた場合
        private void GuessAppPath_Unchecked(object sender, RoutedEventArgs e)
        {
            if (((CheckBox)sender).Name.Equals("chkGuessGHDLPath"))
            {
                Util.settings.guessGHDLPath = false;
                txtGHDLPath.IsEnabled = true;
                btnGHDLPathSearch.IsEnabled = true;
                Util.settings.Save();
            }
            if (((CheckBox)sender).Name.Equals("chkGuessGTKWavePath"))
            {
                Util.settings.guessGTKWavePath = false;
                txtGTKWavePath.IsEnabled = true;
                btnGTKWavePathSearch.IsEnabled = true;
                Util.settings.Save();
            }
        }

        // シミュレーション時間の設定が変更された場合
        private void Simlimit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LimitSelectorItem item = (LimitSelectorItem) cmbSimLimit.SelectedItem;
            Util.settings.simLimit = item.SimTime;
            Util.settings.Save();
        }

        // GHDL の実行時間制限の設定が変更された場合
        private void Reallimit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LimitSelectorItem item = (LimitSelectorItem)cmbRealLimit.SelectedItem;
            Util.settings.procLimit = item.ProcTime;
            Util.settings.Save();
        }
    }
}
