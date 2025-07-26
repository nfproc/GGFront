// GGFront: A GHDL/GTKWave GUI Frontend
// Copyright (C) 2018-2025 Naoki FUJIEDA. New BSD License is applied.
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
        public class VHDLSourceState
        {
            public StreamReader Stream;
            public bool InMultiLineComment;
        }; 

        public bool IsValid;
        public string Content;
        public VHDLDataFile FileName;
        public List<string> Entities;
        public List<Component> Components;
        public List<VHDLDataFile> InFiles, OutFiles;
        public List<int> OriginalLineNumber;
        public Dictionary<string, VHDLEnumeration> EnumSignals;
        public bool IsVHDL2008;

        // コンストラクタ: ソースファイルの解析を行う
        public VHDLSource(string sourceName, int sourceIndex = 0)
        {
            Entities = new List<string>();
            Components = new List<Component>();
            InFiles = new List<VHDLDataFile>();
            OutFiles = new List<VHDLDataFile>();
            FileName = new VHDLDataFile
            {
                Original = sourceName,
                Internal = $"src{sourceIndex}.vhd"
            };
            OriginalLineNumber = new List<int>();
            OriginalLineNumber.Add(0);
            EnumSignals = new Dictionary<string, VHDLEnumeration>();
            IsVHDL2008 = false;
            Dictionary<string, List<string>> enumTypes = new Dictionary<string, List<string>>();
            char[] trimChars = { ' ', '\t', ',' };

            try
            {
                FileInfo fi = new FileInfo(sourceName);
                StringBuilder c = new StringBuilder((int)fi.Length);
                VHDLSourceState ss = new VHDLSourceState();
                ss.Stream = new StreamReader(sourceName, Encoding.GetEncoding("ISO-8859-1"));
                Match match;
                string line;
                string currentEntity = "", currentArchitecture = "";
                int srcLineNumber = 0;
                while ((line = ReadLineWithoutComments(ss)) != null)
                {
                    srcLineNumber++;
                    // 複数行にまたがる type, signal 宣言
                    match = Regex.Match(line, @"(type\s|signal\s)[^;]+$", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        string newLine;
                        while ((newLine = ReadLineWithoutComments(ss)) != null)
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
                        Entities.Add(match.Groups[1].Value.ToLower());
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
                        if (!Components.Contains(newComponent))
                            Components.Add(newComponent);
                    }

                    // 入出力ファイル1: file 宣言時に open する場合
                    //                            12                               3              4   5
                    line = Regex.Replace(line, @"^((\s*file\s[a-z0-9_:\s]+)\s+is\s+(in|out|)\s*"")(.+)(""\s*;)", m =>
                    {
                        bool isInput = m.Groups[2].Value.ToLower().EndsWith("read_mode") || m.Groups[3].Value.ToLower() == "in";
                        string inOut = (isInput) ? "in" : "out";
                        VHDLDataFile file = AddDataFile(sourceIndex, m.Groups[4].Value, isInput, currentEntity);
                        return m.Groups[1].Value + file.Internal + m.Groups[5].Value;
                    }, RegexOptions.IgnoreCase);
                    // 入出力ファイル2: process の中で file_open する場合
                    //                            1                 2                         3   4         5
                    line = Regex.Replace(line, @"^(\s*file_open\s*\((\s*[a-z0-9_]+\s*,\s*)+"")(.+)(""\s*,\s*(read_mode|write_mode)\s*\)\s*;)", m =>
                    {
                        bool isInput = m.Groups[5].Value.ToLower() == "read_mode";
                        string inOut = (isInput) ? "in" : "out";
                        VHDLDataFile file = AddDataFile(sourceIndex, m.Groups[3].Value, isInput, currentEntity);
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
                                string tempName = $"gf_src{sourceIndex}_enum{EnumSignals.Count}";
                                VHDLEnumeration newSignal = new VHDLEnumeration();
                                newSignal.Entity = currentEntity;
                                newSignal.TypeName = typeName;
                                newSignal.SignalName = s;
                                newSignal.Values = enumTypes[typeName];
                                EnumSignals[tempName] = newSignal;
                                c.Append($"signal {tempName} : integer;\n");
                                OriginalLineNumber.Add(-2);
                            }
                        }

                    }
                    // アーキテクチャ宣言の末尾
                    match = Regex.Match(line, @"end\s+([a-z0-9_]+)\s*;", RegexOptions.IgnoreCase);
                    if (match.Success && match.Groups[1].Value.ToLower() == currentArchitecture)
                    {
                        foreach (KeyValuePair<string, VHDLEnumeration> sig in EnumSignals)
                        {
                            if (sig.Value.Entity != currentEntity)
                                continue;
                            c.Append($"{sig.Key} <= {sig.Value.TypeName}'pos({sig.Value.SignalName});\n");
                            OriginalLineNumber.Add(-2);
                        }
                    }
                    // VHDL-2008 特有の記法
                    match = Regex.Match(line, @"ieee\.numeric_std_unsigned\.|std\.env\.|case\?|select\?|\?[=><\?]|\?/=", RegexOptions.IgnoreCase);
                    if (match.Success)
                        IsVHDL2008 = true;

                    c.Append(line).Append("\n");
                    OriginalLineNumber.Add(srcLineNumber);
                }
                ss.Stream.Close();
                OriginalLineNumber.Add(-1);
                Content = c.ToString();
                IsValid = true;
            }
            catch (IOException)
            {
                Content = $"ソースファイル {sourceName} の読み込みに失敗しました．";
                IsValid = false;
            }
            catch (Exception e)
            {
                Content = "VHDLソース読み込み中の予期せぬエラー．\n内容: " + e.ToString();
                IsValid = false;
            }
        }

        public string ReadLineWithoutComments (VHDLSourceState ss)
        {
            string line = ss.Stream.ReadLine();
            if (line == null)
                return null;

            // 文字列リテラル内の -- や /* を除外する
            //                                           1       23
            string escaped = Regex.Replace(" " + line, @"([^'])""(([^""]|"""")*)""", m =>
            {
                // */ または / 以外のすべての文字を . に変換
                string inner = Regex.Replace(m.Groups[2].Value, @"(?!\*/|/).", ".");
                return m.Groups[1].Value + "\"" + inner + "\"";
            });
            escaped = escaped.Substring(1);

            // -- 以降の文字列を削除
            escaped = Regex.Replace(escaped, @"--.*", "");
            line = line.Substring(0, escaped.Length);

            // /* と */ で囲まれた範囲を削除
            int posString = 0;
            int op = 0, cl;
            while (true)
            {
                if (! ss.InMultiLineComment)
                {
                    op = escaped.IndexOf("/*", posString);
                    if (op == -1)
                        break;
                    ss.InMultiLineComment = true;
                    posString = op + 2;
                }
                else
                {
                    cl = escaped.IndexOf("*/", posString);
                    line = line.Substring(0, op) + ((cl != -1) ? line.Substring(cl + 2) : "");
                    escaped = escaped.Substring(0, op) + ((cl != -1) ? escaped.Substring(cl + 2) : "");
                    if (cl == -1)
                        break;
                    posString = op;
                    ss.InMultiLineComment = false;
                }
            }
            return line;
        }

        // ソースファイルおよびテストベンチから読み込まれるデータをコピーする
        public void CopyToWorkDirectory (string workDir)
        {
            try
            {
                StreamWriter sw = new StreamWriter(workDir + FileName.Internal, false, Encoding.GetEncoding("ISO-8859-1"));
                sw.Write(Content);
                sw.Close();
            }
            catch (IOException)
            {
                Content = $"一時ファイル {FileName.Internal} の書き込みに失敗しました．";
                IsValid = false;
                return;
            }

            foreach (VHDLDataFile inFile in InFiles)
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
                    Content = $"ファイル {sName} から開かれるファイル {dName} のコピーに失敗しました．";
                    IsValid = false;
                    return;
                }
            }
        }

        // テストベンチから書き込まれるデータをコピーする
        public void CopyFromWorkDirectory (string workDir)
        {
            foreach (VHDLDataFile outFile in OutFiles)
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
                    Content = $"ファイル {sName} から開かれるファイル {dName} のコピーに失敗しました．";
                    IsValid = false;
                    return;
                }
            }
        }

        // テストベンチから読み書きされるファイルが使われるかチェックする
        public void CheckDataFileReference(EntityHierarchy hierarchy)
        {
            foreach (VHDLDataFile file in InFiles)
                file.referenced = hierarchy.Referenced(file.InEntity);
            foreach (VHDLDataFile file in OutFiles)
                file.referenced = hierarchy.Referenced(file.InEntity);
        }

        // テストベンチから読み書きされるファイルをリストに追加する
        VHDLDataFile AddDataFile(int SourceIndex, string OriginalName, bool isInput, string EntityName)
        {
            string inOut = (isInput) ? "in" : "out";
            int fileIndex = (isInput) ? InFiles.Count : OutFiles.Count;
            VHDLDataFile file = new VHDLDataFile
            {
                Original = Path.Combine(Path.GetDirectoryName(FileName.Original), OriginalName),
                Internal = $"src{SourceIndex}_{inOut}{fileIndex}",
                InEntity = EntityName
            };
            if (isInput)
                InFiles.Add(file);
            else
                OutFiles.Add(file);
            return file;
        }
    }
}