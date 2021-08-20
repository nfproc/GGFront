// GGFront: A GHDL/GTKWave GUI Frontend
// Copyright (C) 2018-2021 Naoki FUJIEDA. New BSD License is applied.
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

        public bool isValid;
        public string content;
        public VHDLDataFile FileName;
        public List<string> entities;
        public List<Component> components;
        public List<VHDLDataFile> inFiles, outFiles;

        // コンストラクタ: ソースファイルの解析を行う
        public VHDLSource(string SourceName, int SourceIndex = 0)
        {
            entities = new List<string>();
            components = new List<Component>();
            inFiles = new List<VHDLDataFile>();
            outFiles = new List<VHDLDataFile>();
            content = "";
            FileName = new VHDLDataFile
            {
                Original = SourceName,
                Internal = $"src{SourceIndex}.vhd"
            };

            try
            {
                StreamReader sr = new StreamReader(SourceName, Encoding.GetEncoding("ISO-8859-1"));
                Match match;
                string line;
                string currentEntity = "";
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.IndexOf("--") != -1)
                        line = line.Substring(0, line.IndexOf("--"));

                    match = Regex.Match(line, @"entity\s+([a-z0-9_]+)\s+is", RegexOptions.IgnoreCase);
                    if (match.Success)
                        entities.Add(match.Groups[1].Value.ToLower());
                    match = Regex.Match(line, @"architecture\s+[a-z0-9_]+\s+of\s+([a-z0-9_]+)\s+is", RegexOptions.IgnoreCase);
                    if (match.Success)
                        currentEntity = match.Groups[1].Value.ToLower();
                    match = Regex.Match(line, @"component\s+([a-z0-9_]+)\s+is", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        Component newComponent = new Component();
                        newComponent.Name = match.Groups[1].Value.ToLower();
                        newComponent.From = currentEntity;
                        if (!components.Contains(newComponent))
                            components.Add(newComponent);
                    }
                    //                            12                               3              4   5
                    line = Regex.Replace(line, @"^((\s*file\s[a-z0-9_:\s]+)\s+is\s+(in|out|)\s*"")(.+)(""\s*;)", m =>
                    {
                        bool isInput = m.Groups[2].Value.ToLower().EndsWith("read_mode") || m.Groups[3].Value.ToLower() == "in";
                        string inOut = (isInput) ? "in" : "out";
                        VHDLDataFile file = AddDataFile(SourceIndex, m.Groups[4].Value, isInput, currentEntity);
                        return m.Groups[1].Value + file.Internal + m.Groups[5].Value;
                    }, RegexOptions.IgnoreCase);
                    //                            1                                        2   3         4
                    line = Regex.Replace(line, @"^(\s*file_open\s*\(\s*[a-z0-9_]+\s*,\s*"")(.+)(""\s*,\s*(read_mode|write_mode)\s*\)\s*;)", m =>
                    {
                        bool isInput = m.Groups[4].Value.ToLower() == "read_mode";
                        string inOut = (isInput) ? "in" : "out";
                        VHDLDataFile file = AddDataFile(SourceIndex, m.Groups[2].Value, isInput, currentEntity);
                        return m.Groups[1].Value + file.Internal + m.Groups[3].Value;
                    }, RegexOptions.IgnoreCase);
                    content += line + "\n";
                }
                sr.Close();
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