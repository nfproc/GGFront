// GGFront: A GHDL/GTKWave GUI Frontend
// Copyright (C) 2018-2025 Naoki FUJIEDA. New BSD License is applied.
//**********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace GGFront.Models
{
    // 実行結果として得られた波形ファイルを整形する
    public class VCDResult
    {
        public bool IsValid;
        public string Content;

        public VCDResult(string SourceName, long simTime, Dictionary<string, VHDLSource.EnumDecl> enumSignals)
        {
            Dictionary<string, VHDLSource.EnumDecl> enumIdents = new Dictionary<string, VHDLSource.EnumDecl>();
            try
            {
                FileInfo fi = new FileInfo(SourceName);
                StringBuilder c = new StringBuilder((int)fi.Length);
                StreamReader sr = new StreamReader(SourceName, Encoding.GetEncoding("ISO-8859-1"));
                Match match;
                while (sr.ReadLine() is string line)
                {
                    // 列挙型信号に対応する integer の宣言
                    match = Regex.Match(line, @"^\$var integer 32 ([^ ]+) ([a-z0-9_]+)");
                    if (match.Success && enumSignals.ContainsKey(match.Groups[2].Value))
                    {
                        string ident = match.Groups[1].Value;
                        VHDLSource.EnumDecl en = enumSignals[match.Groups[2].Value];
                        enumIdents[ident] = en;
                        line = $"$var string 1 {ident} {en.SignalName} $end";
                    }
                    // 上記 integer の信号の値変化
                    match = Regex.Match(line, @"^b([01]+) ([^ ]+)");
                    if (match.Success && enumIdents.ContainsKey(match.Groups[2].Value))
                    {
                        int index = Convert.ToInt32(match.Groups[1].Value, 2);
                        string ident = match.Groups[2].Value;
                        string value = enumIdents[ident].Values[index];
                        line = $"s{value} {ident}";
                    }
                    c.Append(line).Append("\n");
                }
                // シミュレーション終了時間の追記
                c.Append("#").Append(simTime.ToString()).Append("\n");
                Content = c.ToString();
                sr.Close();
            }
            catch (IOException)
            {
                Content = $"波形ファイル {SourceName} の読み込みに失敗しました．";
                IsValid = false;
            }
            catch (Exception e)
            {
                Content = "波形ファイル読み込み中の予期せぬエラー．\n内容: " + e.ToString();
                IsValid = false;
            }
            IsValid = true;
        }

        public void WriteTo(string DestName)
        {
            if (! IsValid)
                return;
            try
            {
                StreamWriter sw = new StreamWriter(DestName, false, Encoding.GetEncoding("ISO-8859-1"));
                sw.Write(Content);
                sw.Close();
            }
            catch (IOException)
            {
                Content = $"波形ファイルの {DestName} への書き込みに失敗しました．";
                IsValid = false;
            }
        }
    }
}
