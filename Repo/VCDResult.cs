// GGFront: A GHDL/GTKWave GUI Frontend
// Copyright (C) 2018-2023 Naoki FUJIEDA. New BSD License is applied.
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
    // 実行結果として得られた波形ファイルを整形する
    class VCDResult
    {
        public bool isValid;
        public string content;

        public VCDResult(string SourceName, long simTime, Dictionary<string, VHDLSource.VHDLEnumeration> enumSignals)
        {
            Dictionary<string, VHDLSource.VHDLEnumeration> enumIdents = new Dictionary<string, VHDLSource.VHDLEnumeration>();
            content = "";
            try
            {
                StreamReader sr = new StreamReader(SourceName, Encoding.GetEncoding("ISO-8859-1"));
                Match match;
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    // 列挙型信号に対応する integer の宣言
                    match = Regex.Match(line, @"^\$var integer 32 ([^ ]+) ([a-z0-9_]+)");
                    if (match.Success && enumSignals.ContainsKey(match.Groups[2].Value))
                    {
                        string ident = match.Groups[1].Value;
                        VHDLSource.VHDLEnumeration en = enumSignals[match.Groups[2].Value];
                        enumIdents[ident] = en;
                        line = $"$var string 1 {ident} {en.SignalName} $end";
                    }
                    // 上記 integer の信号の値変化
                    match = Regex.Match(line, @"b([01]+) ([^ ]+)");
                    if (match.Success && enumIdents.ContainsKey(match.Groups[2].Value))
                    {
                        int index = Convert.ToInt32(match.Groups[1].Value, 2);
                        string ident = match.Groups[2].Value;
                        string value = enumIdents[ident].Values[index];
                        line = $"s{value} {ident}";
                    }
                    content += line + "\n";
                }
                // シミュレーション終了時間の追記
                content += "#" + simTime.ToString() + "\n";
                sr.Close();
            }
            catch (IOException)
            {
                content = $"波形ファイル {SourceName} の読み込みに失敗しました．";
                isValid = false;
            }
            catch (Exception e)
            {
                content = "波形ファイル読み込み中の予期せぬエラー．\n内容: " + e.ToString();
                isValid = false;
            }
            isValid = true;
        }

        public void WriteTo(string DestName)
        {
            if (!isValid)
                return;
            try
            {
                StreamWriter sw = new StreamWriter(DestName, false, Encoding.GetEncoding("ISO-8859-1"));
                sw.Write(content);
                sw.Close();
            }
            catch (IOException)
            {
                content = $"波形ファイルの {DestName} への書き込みに失敗しました．";
                isValid = false;
            }
        }
    }
}
