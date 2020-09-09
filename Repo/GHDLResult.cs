// GGFront: A GHDL/GTKWave GUI Frontend
// Copyright (C) 2018-2020 Naoki FUJIEDA. New BSD License is applied.
//**********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace GGFront
{
    // 外部プログラムの実行結果を整形する
    public class GHDLResult
    {
        public int code;
        public string generatedDate;
        public bool violateAssertion;
        public long simTime;
        public string message;
        private GHDLErrorDescription[] descs;

        public GHDLResult()
        {
            generatedDate = DateTime.Now.ToString();
        }

        public void RestoreFileName(List<string> org)
        {
            if (message == "")
                return;

            string newMessage = "";
            string[] lines = message.Replace("\r\n", "\n").Split('\n');
            descs = new GHDLErrorDescription[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                Match match = Regex.Match(line, @"^src(\d+)\.vhd:(\d+):(\d+):(warning:)?(.*)");
                if (match.Success)
                {
                    string origFile = org[int.Parse(match.Groups[1].Value) - 1];
                    origFile = Path.GetFileName(origFile);
                    newMessage += "[" + origFile + " " + match.Groups[2].Value + "行 ";
                    newMessage += match.Groups[3].Value + "文字";
                    if (match.Groups[4].Value != "")
                        newMessage += " (警告)";
                    newMessage += "] " + match.Groups[5].Value + "\r\n";
                    descs[i] = Util.errorList.match(lines[i]);
                }
                else
                {
                    newMessage += line + "\r\n";
                }
                match = Regex.Match(line, @"@(\d+)([munpf])s:\(assertion failure\)");
                if (match.Success)
                {
                    violateAssertion = true;
                    simTime = long.Parse(match.Groups[1].Value);
                    if (match.Groups[2].Value == "p")
                        simTime *= 1000;
                    else if (match.Groups[2].Value == "n")
                        simTime *= 1000000;
                    else if (match.Groups[2].Value == "u")
                        simTime *= 1000000000;
                    else if (match.Groups[2].Value == "m")
                        simTime *= 1000000000000;
                }
            }
            message = newMessage;
        }

        public void ShowMessage()
        {
            ErrorWindow win = new ErrorWindow();
            string messageForCopy = "";
            win.Title = ((code != 0) ? "エラー" : "警告") + " [" + generatedDate + "]";
            win.Height = Util.settings.errorWindowHeight;
            win.Width = Util.settings.errorWindowWidth;
            win.Owner = Application.Current.MainWindow;
            win.txtError.FontSize = Util.settings.errorWindowTextSize;

            string[] lines = message.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < descs.Length; i++)
            {
                messageForCopy += lines[i] + "\r\n";
                Run newRun = new Run(lines[i] + "\n");
                if (descs[i] != null)
                {
                    TextBlock tb = new TextBlock();
                    tb.FontSize = Util.settings.errorWindowTextSize;
                    tb.MaxWidth = Util.settings.errorWindowWidth;
                    tb.TextWrapping = TextWrapping.Wrap;
                    tb.Inlines.Add(new Run
                    {
                        Text = descs[i].name + "\n",
                        Foreground = Brushes.Blue,
                        FontWeight = FontWeights.Bold,
                        TextDecorations = TextDecorations.Underline
                    });
                    tb.Inlines.Add(new Bold(new Run("説明: ")));
                    tb.Inlines.Add(new Run(descs[i].description + "\n"));
                    tb.Inlines.Add(new Bold(new Run("対処: ")));
                    tb.Inlines.Add(new Run(descs[i].handling));
                    newRun.ToolTip = tb;

                    messageForCopy += "  説明: " + descs[i].description + "\r\n";
                    messageForCopy += "  対処: " + descs[i].handling + "\r\n";
                }
                win.txtError.Inlines.Add(newRun);
            }

            win.messageForCopy = messageForCopy;
            win.Show();
        }
    }
}