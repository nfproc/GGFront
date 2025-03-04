// GGFront: A GHDL/GTKWave GUI Frontend
// Copyright (C) 2018-2025 Naoki FUJIEDA. New BSD License is applied.
//**********************************************************************

using System.Windows;
using System.Windows.Controls;

namespace GGFront
{
    /// <summary>
    /// ErrorWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ErrorWindow : Window
    {
        public string MessageForCopy;
        public ErrorWindow()
        {
            InitializeComponent();
        }

        // - ボタンがクリックされたとき (8 <- 9 <- ... <- 15 <- 16 <- 18 <- ... <- 30 <- 32)
        private void Shrink_Click(object sender, RoutedEventArgs e)
        {
            int currentSize = (int) this.txtError.FontSize;
            int newSize = (currentSize > 16) ? currentSize - 2 :
                          (currentSize > 8) ? currentSize - 1 : currentSize;
            setTextSize(newSize);
        }

        // + ボタンがクリックされたとき (8 -> 9 -> ... -> 15 -> 16 -> 18 -> ... -> 30 -> 32)
        private void Expand_Click(object sender, RoutedEventArgs e)
        {
            int currentSize = (int)this.txtError.FontSize;
            int newSize = (currentSize < 16) ? currentSize + 1 :
                          (currentSize < 32) ? currentSize + 2 : currentSize;
            setTextSize(newSize);

        }

        // -, + ボタンがクリックされたときの共通処理
        private void setTextSize(int size)
        {
            this.txtError.FontSize = size;
            foreach (var line in this.txtError.Inlines)
            {
                if (line.ToolTip is TextBlock)
                {
                    (line.ToolTip as TextBlock).FontSize = size;
                }
            }
            Util.Settings.ErrorWindowTextSize = size;
            Util.Settings.Save();
        }

        // Copy to Clipboard ボタンがクリックされたとき
        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(MessageForCopy);
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
                Util.Settings.ErrorWindowHeight = (int) this.ActualHeight;
                Util.Settings.ErrorWindowWidth = (int) this.ActualWidth;
                Util.Settings.Save();
            }
        }
    }
}
