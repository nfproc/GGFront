// GGFront: A GHDL/GTKWave GUI Frontend
// Copyright (C) 2018-2020 Naoki FUJIEDA. New BSD License is applied.
//**********************************************************************

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace GGFront
{
    // 外部プログラムの実行結果を整形する
    public class GHDLResult
    {
        public int code;
        public bool violateAssertion;
        public long simTime;
        public string message;

        public void RestoreFileName(List<string> org)
        {
            if (message == "")
                return;

            string newMessage = "";
            foreach (string line in message.Replace("\r\n", "\n").Split('\n'))
            {
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
            string errorFile = Util.workDir + ((code != 0) ? "error.txt" : "warning.txt");
            StreamWriter sw = new StreamWriter(errorFile, false, new UTF8Encoding(false));
            sw.Write(message);
            sw.Close();
            Util.ExecTool(errorFile, "", true, true);
        }
    }
}