// GGFront: A GHDL/GTKWave GUI Frontend
// Copyright (C) 2018-2025 Naoki FUJIEDA. New BSD License is applied.
//**********************************************************************

using System.Collections.Generic;
using System;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using GGFront.Models;
using GGFront.ViewModels;

namespace GGFront.Views
{
    // ■■ エラー表示用ウィンドウ ■■
    public partial class ErrorWindow : Window
    {
        private ErrorViewModel VM;
        private string MessageForCopy;

        public ErrorWindow()
        {
            InitializeComponent();
            Width = Util.Settings.ErrorWindowWidth;
            Height = Util.Settings.ErrorWindowHeight;
            VM = new ErrorViewModel();
            VM.TextSize = Util.Settings.ErrorWindowTextSize;
            DataContext = VM;
            MessageForCopy = "";
        }

        public ErrorWindow(List<ErrorListItem> errorLists) : this()
        {
            foreach (ErrorListItem item in errorLists)
                VM.ErrorLists.Add(item);

            // コピーペースト用の文字列
            StringBuilder sb = new StringBuilder();
            foreach (ErrorListItem e in errorLists)
            {
                sb.Append(e.Head).Append("\n");
                if (e.Code.Length > 0)
                    sb.Append(e.Code).Append("\n");
                int idx = e.Details.IndexOf("\n");
                if (idx >= 0)
                    sb.Append(e.Details.Substring(idx + 1)).Append("\n");
            }
            MessageForCopy = sb.ToString().Replace("\n", Environment.NewLine);
        }

        // - ボタンがクリックされたとき (8 <- 9 <- ... <- 15 <- 16 <- 18 <- ... <- 30 <- 32)
        private void Shrink_Click(object sender, RoutedEventArgs e)
        {
            VM.TextSize = (VM.TextSize > 16) ? VM.TextSize - 2 :
                          (VM.TextSize >  8) ? VM.TextSize - 1 : VM.TextSize;
        }

        // + ボタンがクリックされたとき (8 -> 9 -> ... -> 15 -> 16 -> 18 -> ... -> 30 -> 32)
        private void Expand_Click(object sender, RoutedEventArgs e)
        {
            VM.TextSize = (VM.TextSize < 16) ? VM.TextSize + 1 :
                          (VM.TextSize < 32) ? VM.TextSize + 2 : VM.TextSize;
        }

        // Copy to Clipboard ボタンがクリックされたとき
        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            if (this.Clipboard is IClipboard)
            {
                Task task = Task.Run(async () => { await this.Clipboard.SetTextAsync(MessageForCopy); });
                task.Wait();
            }
        }

        // Close ボタンがクリックされたとき
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // 閉じる前にウィンドウのサイズを保存する
        private void Window_Closed(object sender, System.EventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                Util.Settings.ErrorWindowHeight = (int) this.Bounds.Height;
                Util.Settings.ErrorWindowWidth = (int) this.Bounds.Width;
                Util.Settings.Save();
            }
        }
    }
}