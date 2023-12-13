// GGFront: A GHDL/GTKWave GUI Frontend
// Copyright (C) 2018-2023 Naoki FUJIEDA. New BSD License is applied.
//**********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace GGFront
{
    // ソースコードの解析結果を保持するクラス
    public class VHDLSource
    {
        public struct Component
        {
            public string Name, From;
        };
        public class VHDLDataFile
        {
            public string Original { get; set; }
            public string Internal { get; set; }
            public string InEntity { get; set; }
            public bool referenced;
        };
        public struct VHDLEnumeration
        {
            public string Entity;
            public string TypeName;
            public string SignalName;
            public List<string> Values;
        };

        public bool isValid;
        public string content;
        public VHDLDataFile FileName;
        public List<string> entities;
        public List<Component> components;
        public List<VHDLDataFile> inFiles, outFiles;
        public List<int> origLineNumber;
        public Dictionary<string, VHDLEnumeration> enumSignals;

        // コンストラクタ: ソースファイルの解析を行う
        public VHDLSource(string SourceName, int SourceIndex = 0)
        {
            entities = new List<string>();
            components = new List<Component>();
            inFiles = new List<VHDLDataFile>();
            outFiles = new List<VHDLDataFile>();
            FileName = new VHDLDataFile
            {
                Original = SourceName,
                Internal = $"src{SourceIndex}.vhd"
            };
            origLineNumber = new List<int>();
            origLineNumber.Add(0);
            enumSignals = new Dictionary<string, VHDLEnumeration>();
            Dictionary<string, List<string>> enumTypes = new Dictionary<string, List<string>>();
            char[] trimChars = { ' ', '\t', ',' };

            try
            {
                FileInfo fi = new FileInfo(SourceName);
                StringBuilder c = new StringBuilder((int)fi.Length);
                StreamReader sr = new StreamReader(SourceName, Encoding.GetEncoding("ISO-8859-1"));
                Match match;
                string line;
                string currentEntity = "", currentArchitecture = "";
                int srcLineNumber = 0;
                while ((line = ReadLineWithoutComments(sr)) != null)
                {
                    srcLineNumber++;
                    // 複数行にまたがる type, signal 宣言
                    match = Regex.Match(line, @"(type\s|signal\s)[^;]+$", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        string newLine;
                        while ((newLine = ReadLineWithoutComments(sr)) != null)
                        {
                            line = line + " " + newLine;
                            srcLineNumber++;
                            if (newLine.IndexOf(";") != -1)
                                break;
                        }
                    }

                    // entity 宣言（階層構造作成用）
                    match = Regex.Match(line, @"entity\s+([a-z0-9_]+)\s+is", RegexOptions.IgnoreCase);
                    if (match.Success)
                        entities.Add(match.Groups[1].Value.ToLower());
                    // architecture 宣言（階層構造作成用）
                    match = Regex.Match(line, @"architecture\s+([a-z0-9_]+)\s+of\s+([a-z0-9_]+)\s+is", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        currentEntity = match.Groups[2].Value.ToLower();
                        currentArchitecture = match.Groups[1].Value.ToLower();
                    }
                    // component 宣言（階層構造作成用）
                    match = Regex.Match(line, @"component\s+([a-z0-9_]+)\s+is", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        Component newComponent = new Component();
                        newComponent.Name = match.Groups[1].Value.ToLower();
                        newComponent.From = currentEntity;
                        if (!components.Contains(newComponent))
                            components.Add(newComponent);
                    }

                    // 入出力ファイル1: file 宣言時に open する場合
                    //                            12                               3              4   5
                    line = Regex.Replace(line, @"^((\s*file\s[a-z0-9_:\s]+)\s+is\s+(in|out|)\s*"")(.+)(""\s*;)", m =>
                    {
                        bool isInput = m.Groups[2].Value.ToLower().EndsWith("read_mode") || m.Groups[3].Value.ToLower() == "in";
                        string inOut = (isInput) ? "in" : "out";
                        VHDLDataFile file = AddDataFile(SourceIndex, m.Groups[4].Value, isInput, currentEntity);
                        return m.Groups[1].Value + file.Internal + m.Groups[5].Value;
                    }, RegexOptions.IgnoreCase);
                    // 入出力ファイル2: process の中で file_open する場合
                    //                            1                 2                         3   4         5
                    line = Regex.Replace(line, @"^(\s*file_open\s*\((\s*[a-z0-9_]+\s*,\s*)+"")(.+)(""\s*,\s*(read_mode|write_mode)\s*\)\s*;)", m =>
                    {
                        bool isInput = m.Groups[5].Value.ToLower() == "read_mode";
                        string inOut = (isInput) ? "in" : "out";
                        VHDLDataFile file = AddDataFile(SourceIndex, m.Groups[3].Value, isInput, currentEntity);
                        return m.Groups[1].Value + file.Internal + m.Groups[4].Value;
                    }, RegexOptions.IgnoreCase);

                    // 列挙型の宣言
                    match = Regex.Match(line, @"type\s+([a-z0-9_]+)\s+is\s+\((\s*[a-z0-9_]+\s*,)*(\s*[a-z0-9_]+\s*)\)", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        string typeName = match.Groups[1].Value.ToLower();
                        List<string> values = new List<string>();
                        foreach (Capture cp in match.Groups[2].Captures)
                            values.Add(cp.Value.Trim(trimChars));
                        values.Add(match.Groups[3].Value.Trim(trimChars));
                        enumTypes[typeName] = values;
                    }
                    // 列挙型信号の宣言
                    match = Regex.Match(line, @"signal\s(\s*[a-z0-9_]+\s*,)*(\s*[a-z0-9_]+\s*):\s+([a-z0-9_]+)", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        string typeName = match.Groups[3].Value.ToLower();
                        if (enumTypes.ContainsKey(typeName))
                        {
                            List<string> signalNames = new List<string>();
                            foreach (Capture cp in match.Groups[1].Captures)
                                signalNames.Add(cp.Value.Trim(trimChars));
                            signalNames.Add(match.Groups[2].Value.Trim(trimChars));

                            foreach (string s in signalNames)
                            {
                                string tempName = $"gf_src{SourceIndex}_enum{enumSignals.Count}";
                                VHDLEnumeration newSignal = new VHDLEnumeration();
                                newSignal.Entity = currentEntity;
                                newSignal.TypeName = typeName;
                                newSignal.SignalName = s;
                                newSignal.Values = enumTypes[typeName];
                                enumSignals[tempName] = newSignal;
                                c.Append($"signal {tempName} : integer;\n");
                                origLineNumber.Add(-2);
                            }
                        }

                    }
                    // アーキテクチャ宣言の末尾
                    match = Regex.Match(line, @"end\s+([a-z0-9_]+)\s*;", RegexOptions.IgnoreCase);
                    if (match.Success && match.Groups[1].Value.ToLower() == currentArchitecture)
                    {
                        foreach (KeyValuePair<string, VHDLEnumeration> sig in enumSignals)
                        {
                            if (sig.Value.Entity != currentEntity)
                                continue;
                            c.Append($"{sig.Key} <= {sig.Value.TypeName}'pos({sig.Value.SignalName});\n");
                            origLineNumber.Add(-2);
                        }
                    }

                    c.Append(line).Append("\n");
                    origLineNumber.Add(srcLineNumber);
                }
                sr.Close();
                origLineNumber.Add(-1);
                content = c.ToString();
                isValid = true;
            }
            catch (IOException)
            {
                content = $"ソースファイル {SourceName} の読み込みに失敗しました．";
                isValid = false;
            }
            catch (Exception e)
            {
                content = "VHDLソース読み込み中の予期せぬエラー．\n内容: " + e.ToString();
                isValid = false;
            }
        }

        public string ReadLineWithoutComments (StreamReader sr)
        {
            string line = sr.ReadLine();
            if (line == null)
                return null;
            if (line.IndexOf("--") != -1)
            {
                bool inLiteral = false;
                int posString = 0;
                while (true)
                {
                    int quote = line.IndexOf("\"", posString);
                    int twoQuote = line.IndexOf("\"\"", posString);
                    int twoDash = line.IndexOf("--", posString);
                    int quoteChar = line.IndexOf("'\"'", posString);
                    if (twoDash == -1) // -- が見つからなくなったらそこまで
                    {
                        break;
                    }
                    else if (twoDash < quote || quote == -1) // -- が先に見つかった
                    {
                        if (!inLiteral)
                        {
                            line = line.Substring(0, twoDash); // 文字列中でなければ後続を削除
                            break;
                        }
                        posString = twoDash + 1;
                    }
                    else // " が先に見つかった
                    {
                        posString = quote + 1;
                        if (!inLiteral)
                        {
                            if (quote != quoteChar + 1) // '"' は除外
                                inLiteral = true;
                        }
                        else
                        {
                            if (quote != twoQuote) // 文字列中の "" は除外
                                inLiteral = false;
                            else
                                posString = quote + 2;
                        }
                    }
                }
            }
            return line.TrimEnd();
        }

        // ソースファイルおよびテストベンチから読み込まれるデータをコピーする
        public void CopyToWorkDirectory (string workDir)
        {
            try
            {
                StreamWriter sw = new StreamWriter(workDir + FileName.Internal, false, Encoding.GetEncoding("ISO-8859-1"));
                sw.Write(content);
                sw.Close();
            }
            catch (IOException)
            {
                content = $"一時ファイル {FileName.Internal} の書き込みに失敗しました．";
                isValid = false;
                return;
            }

            foreach (VHDLDataFile inFile in inFiles)
            {
                try
                {
                    if (inFile.referenced)
                        File.Copy(inFile.Original, workDir + inFile.Internal);
                }
                catch (IOException)
                {
                    string sName = Path.GetFileName(FileName.Original);
                    string dName = Path.GetFileName(inFile.Original);
                    content = $"ファイル {sName} から開かれるファイル {dName} のコピーに失敗しました．";
                    isValid = false;
                    return;
                }
            }
        }

        // テストベンチから書き込まれるデータをコピーする
        public void CopyFromWorkDirectory (string workDir)
        {
            foreach (VHDLDataFile outFile in outFiles)
            {
                try
                {
                    if (outFile.referenced)
                        File.Copy(workDir + outFile.Internal, outFile.Original, true);
                }
                catch (IOException)
                {
                    string sName = Path.GetFileName(FileName.Original);
                    string dName = Path.GetFileName(outFile.Original);
                    content = $"ファイル {sName} から開かれるファイル {dName} のコピーに失敗しました．";
                    isValid = false;
                    return;
                }
            }
        }

        // テストベンチから読み書きされるファイルが使われるかチェックする
        public void CheckDataFileReference(EntityHierarchy hierarchy)
        {
            foreach (VHDLDataFile file in inFiles)
                file.referenced = hierarchy.Referenced(file.InEntity);
            foreach (VHDLDataFile file in outFiles)
                file.referenced = hierarchy.Referenced(file.InEntity);
        }

        // テストベンチから読み書きされるファイルをリストに追加する
        VHDLDataFile AddDataFile(int SourceIndex, string OriginalName, bool isInput, string EntityName)
        {
            string inOut = (isInput) ? "in" : "out";
            int fileIndex = (isInput) ? inFiles.Count : outFiles.Count;
            VHDLDataFile file = new VHDLDataFile
            {
                Original = Path.Combine(Path.GetDirectoryName(FileName.Original), OriginalName),
                Internal = $"src{SourceIndex}_{inOut}{fileIndex}",
                InEntity = EntityName
            };
            if (isInput)
                inFiles.Add(file);
            else
                outFiles.Add(file);
            return file;
        }
    }
}