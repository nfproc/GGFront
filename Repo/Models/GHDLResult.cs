// GGFront: A GHDL/GTKWave GUI Frontend
// Copyright (C) 2018-2025 Naoki FUJIEDA. New BSD License is applied.
//**********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
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
        public int ExitCode;
        public string GeneratedDate;
        public bool SimFinished;
        public long SimTime;
        public string Message;
        private GHDLErrorDescription[] Descs;

        public GHDLResult()
        {
            GeneratedDate = DateTime.Now.ToString();
        }

        private string GetOriginalFileName(List<string> org, List<List<int>> lineNumber, Match match)
        {
            int fileID = int.Parse(match.Groups[1].Value) - 1;
            string origFile = org[fileID];
            string result = Path.GetFileName(origFile);

            int fileLine = int.Parse(match.Groups[2].Value);
            int origLine = lineNumber[fileID][fileLine];
            if (origLine >= 0)
            {
                result += " " + origLine + "行";
                if (match.Groups[3].Value != "")
                    result += " " + match.Groups[3].Value + "文字";
            }
            else if (origLine == -1)
            {
                result += " ファイル末尾";
            }
            else {
                result += " GGFrontの内部エラー";
            }
            return result;
        }

        public void RestoreFileName(List<string> org, List<List<int>> lineNumber, bool use2008)
        {
            if (Message == "")
                return;

            string newMessage = "";
            string[] lines = Message.Replace("\r\n", "\n").Split('\n');
            Descs = new GHDLErrorDescription[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                // ファイル名の復元
                Match match = Regex.Match(line, @":error:");
                if (match.Success)
                {
                    line = line.Substring(match.Index + 1);
                    Descs[i] = Util.ErrorList.match(line);
                }
                match = Regex.Match(line, @"^src(\d+)\.vhd:(\d+):(\d+):(warning:)?(.*)");
                if (match.Success)
                {
                    line = "[" + GetOriginalFileName(org, lineNumber, match);
                    if (match.Groups[4].Value != "")
                        line += " (警告)";
                    line += "] " + match.Groups[5].Value;
                    Descs[i] = Util.ErrorList.match(line);
                }
                match = Regex.Match(line, @"at src(\d+)\.vhd:(\d+):?(\d*)$");
                if (match.Success)
                    line = line.Substring(0, match.Index + 3) + "[" + GetOriginalFileName(org, lineNumber, match) + "]";
                newMessage += line + "\r\n";

                // シミュレーション終了時刻の取得
                string query = (use2008) ? @"simulation (?:finished|stopped) @(\d+)([munpf])s" : @"@(\d+)([munpf])s:\(assertion failure\)";
                match = Regex.Match(line, query);
                if (match.Success)
                {
                    SimFinished = true;
                    SimTime = long.Parse(match.Groups[1].Value);
                    if (match.Groups[2].Value == "p")
                        SimTime *= 1000;
                    else if (match.Groups[2].Value == "n")
                        SimTime *= 1000000;
                    else if (match.Groups[2].Value == "u")
                        SimTime *= 1000000000;
                    else if (match.Groups[2].Value == "m")
                        SimTime *= 1000000000000;
                }
            }
            Message = newMessage;
        }

        public void ShowMessage()
        {
            ErrorWindow win = new ErrorWindow();
            string messageForCopy = "";
            FontFamily consolas = new FontFamily("Consolas");
            win.Title = ((ExitCode != 0) ? "エラー" : "警告") + " [" + GeneratedDate + "]";
            win.Height = Util.Settings.ErrorWindowHeight;
            win.Width = Util.Settings.ErrorWindowWidth;
            win.Owner = Application.Current.MainWindow;
            win.txtError.FontSize = Util.Settings.ErrorWindowTextSize;

            string[] lines = Message.Replace("\r\n", "\n").Split('\n');
            bool[] isCode = new bool[lines.Length];
            for (int i = 1; i < Descs.Length; i++)
            {
                if (Regex.IsMatch(lines[i], @"^\s*\^"))
                {
                    isCode[i] = true;
                    isCode[i - 1] = true;
                }
            }
            for (int i = 0; i < Descs.Length; i++)
            {
                messageForCopy += lines[i] + "\r\n";
                Run newRun = new Run(lines[i] + "\n");
                if (isCode[i])
                {
                    newRun.FontFamily = consolas;
                    newRun.Foreground = Brushes.Brown;

                }
                if (Descs[i] != null)
                {
                    TextBlock tb = new TextBlock();
                    tb.FontSize = Util.Settings.ErrorWindowTextSize;
                    tb.MaxWidth = Util.Settings.ErrorWindowWidth;
                    tb.TextWrapping = TextWrapping.Wrap;
                    tb.Inlines.Add(new Run
                    {
                        Text = Descs[i].Name + "\n",
                        Foreground = Brushes.Blue,
                        FontWeight = FontWeights.Bold,
                        TextDecorations = TextDecorations.Underline
                    });
                    tb.Inlines.Add(new Bold(new Run("説明: ")));
                    tb.Inlines.Add(new Run(Descs[i].Description + "\n"));
                    tb.Inlines.Add(new Bold(new Run("対処: ")));
                    tb.Inlines.Add(new Run(Descs[i].Handling));
                    newRun.ToolTip = tb;
                    ToolTipService.SetShowDuration(newRun, int.MaxValue);

                    messageForCopy += "  説明: " + Descs[i].Description + "\r\n";
                    messageForCopy += "  対処: " + Descs[i].Handling + "\r\n";
                }
                win.txtError.Inlines.Add(newRun);
            }

            win.MessageForCopy = messageForCopy;
            win.Show();
        }
    }
}