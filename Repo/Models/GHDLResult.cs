// GGFront: A GHDL/GTKWave GUI Frontend
// Copyright (C) 2018-2025 Naoki FUJIEDA. New BSD License is applied.
//**********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using GGFront.ViewModels;

namespace GGFront.Models
{
    // 外部プログラムの実行結果を解析する
    public class GHDLResult
    {
        public int ExitCode;
        public string GeneratedDate;
        public bool SimFinished;
        public long SimTime;
        public string Message;
        public List<ErrorListItem> ErrorDetails;
        
        public GHDLResult()
        {
            GeneratedDate = DateTime.Now.ToString();
            Message = "";
            ErrorDetails = new List<ErrorListItem>();
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
            else
            {
                result += " GGFrontの内部エラー";
            }
            return result;
        }

        public void Analyze(List<string> org, List<List<int>> lineNumber, bool use2008)
        {
            if (Message == "")
                return;

            ErrorDetails = new List<ErrorListItem>();
            string[] lines = Message.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                bool isErrorLine = false;
                string problematicCode = "";
                string detail = "詳細説明はありません．";
                
                // 置き換えるべきファイル名があるかどうか調べる
                Match match = Regex.Match(line, @"^src(\d+)\.vhd:(\d+):(\d+):(error:|warning:)?(.*)");
                if (match.Success)
                {
                    line = "[" + GetOriginalFileName(org, lineNumber, match);
                    if (match.Groups[4].Value == "warning:")
                        line += " (警告)";
                    line += "] " + match.Groups[5].Value;
                    isErrorLine = true;
                }
                match = Regex.Match(line, @":error:");
                if (match.Success)
                {
                    line = line.Substring(match.Index + 1);
                    isErrorLine = true;
                }
                match = Regex.Match(line, @"at src(\d+)\.vhd:(\d+):?(\d*)$");
                if (match.Success)
                    line = line.Substring(0, match.Index + 3) + "[" + GetOriginalFileName(org, lineNumber, match) + "]";

                // 2行下に ^ が見つかれば，下に問題のあるコードが記載されているとみなす
                if (isErrorLine && i + 2 < lines.Length && Regex.IsMatch(lines[i + 2], @"^\s*\^"))
                {
                    problematicCode = lines[i + 1] + "\n" + lines[i + 2];
                    i += 2;
                }

                // エラー内容をリストに追加
                GHDLErrorDescription? desc = (isErrorLine) ? Util.ErrorList.Match(line) : null;
                if (desc != null)
                {
                    detail = desc.Name + "\n";
                    detail += "　説明: " + desc.Description + "\n";
                    detail += "　対処: " + desc.Handling + "\n";
                }
                if (line.Length > 0)
                    ErrorDetails.Add(new ErrorListItem(line, problematicCode, detail));
                    
                // シミュレーション終了時刻の取得
                string query = (use2008) ? @"simulation (?:finished|stopped) @(\d+)([munpf])s" :
                    @"@(\d+)([munpf])s:\(assertion failure\)";
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
        }
    }
}

