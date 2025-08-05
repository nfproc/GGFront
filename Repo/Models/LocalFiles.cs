// GGFront: A GHDL/GTKWave GUI Frontend
// Copyright (C) 2018-2025 Naoki FUJIEDA. New BSD License is applied.
//**********************************************************************

using GGFront.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace GGFront.Models
{
    // 設定ファイルに対応するクラス（～v0.7.x）
    [XmlType("GGFrontSettings")]
    public class GGFrontLegacySettings
    {
        public string GGFrontVersion = "";
        public bool guessGHDLPath, guessGTKWavePath;
        public string GHDLPath = "", GTKWavePath = "";
        public string simLimit = ""; // v0.4.4+
        public int procLimit;   // v0.4.4+
        public int errorWindowWidth;    // v0.5.0+
        public int errorWindowHeight;   // v0.5.0+
        public int errorWindowTextSize; // v0.5.0+
        
        [UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026:RequiresUnreferencedCode")]
        public static bool Load(GGFrontSettings newSettings)
        {
            try
            {
                XmlSerializer serial = new XmlSerializer(typeof(GGFrontLegacySettings));
                FileStream fs = new FileStream(Util.SettingName, FileMode.Open);
                GGFrontLegacySettings? legacySettings = (GGFrontLegacySettings?)serial.Deserialize(fs);
                if (legacySettings == null)
                    return false;

                double version = Double.Parse(newSettings.GGFrontVersion);

                newSettings.GHDLPath = legacySettings.GHDLPath; // v0.1.0+
                newSettings.GTKWavePath = legacySettings.GTKWavePath; // v0.1.0+
                if (version >= 0.3)
                {
                    newSettings.GuessGHDLPath = legacySettings.guessGHDLPath;
                    newSettings.GuessGTKWavePath = legacySettings.guessGTKWavePath;
                }
                if (version >= 0.4)
                {
                    newSettings.SimLimit = int.Parse(legacySettings.simLimit.Substring(
                        0, legacySettings.simLimit.Length - 2));
                    newSettings.ProcLimit = legacySettings.procLimit;
                }
                if (version >= 0.5)
                {
                    newSettings.ErrorWindowHeight = legacySettings.errorWindowHeight;
                    newSettings.ErrorWindowWidth = legacySettings.errorWindowWidth;
                    newSettings.ErrorWindowTextSize = legacySettings.errorWindowTextSize;
                }
                fs.Close();
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
    }

    // 設定ファイルに対応するクラス
    public class GGFrontSettings
    {
        public string GGFrontVersion;
        public bool GuessGHDLPath, GuessGTKWavePath;
        public string GHDLPath = "", GTKWavePath = "";
        public int SimLimit;
        public int ProcLimit;
        public int ErrorWindowWidth;
        public int ErrorWindowHeight;
        public int ErrorWindowTextSize;
        public int VHDLStd;                // v0.8.0+
        public int MainWindowWidth;        // v0.9.0+
        public int MainWindowHeight;       // v0.9.0+
        public string? LastlyUsedFolder;   // v0.9.0+
        private bool DisableSaveSettings;

        public const int SimLimitDefault = 1; // ms
        public const int ProcLimitDefault = 3000; // ms
        public const int ErrorWindowWidthDefault = 500;
        public const int ErrorWindowHeightDefault = 300;
        public const int ErrorWindowTextSizeDefault = 12;
        public const int VHDLStdDefault = 0; // guess
        public const int MainWindowWidthDefault = 640;
        public const int MainWindowHeightDefault = 600;

        public GGFrontSettings()
        {
            GGFrontVersion = Util.GGFrontDataVersion;
            DisableSaveSettings = false;
        }

        private void Reset()
        {
            GHDLPath = "";
            GTKWavePath = "";
            GuessGHDLPath = GuessGTKWavePath = true;
            SimLimit = SimLimitDefault;
            ProcLimit = ProcLimitDefault;
            ErrorWindowHeight = ErrorWindowHeightDefault;
            ErrorWindowWidth = ErrorWindowWidthDefault;
            ErrorWindowTextSize = ErrorWindowTextSizeDefault;
            VHDLStd = VHDLStdDefault;
            MainWindowWidth = MainWindowWidthDefault;
            MainWindowHeight = MainWindowHeightDefault;
        }
        
        [UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026:RequiresUnreferencedCode")]
        public bool Load()
        {
            try
            {
                Reset();
                XmlSerializer serial = new XmlSerializer(typeof(GGFrontSettings));
                FileStream fs = new FileStream(Util.SettingName, FileMode.Open);
                GGFrontSettings? newSettings = (GGFrontSettings?)serial.Deserialize(fs);
                fs.Close();
                if (newSettings == null)
                    return false;

                double version = Double.Parse(newSettings.GGFrontVersion);
                if (version < 0.8)
                {
                    if (GGFrontLegacySettings.Load(newSettings))
                    {
                        DialogBox.Info("旧バージョンの GGFront から設定を読み取りました．");
                    }
                    else
                    {
                        DialogBox.Warn("旧バージョンの GGFront からの設定ファイルの読み取りに失敗しました．\n" +
                            "設定ファイルを保存しません．");
                        DisableSaveSettings = true;
                        return false;
                    }
                }
                GHDLPath = newSettings.GHDLPath;
                GTKWavePath = newSettings.GTKWavePath;
                GuessGHDLPath = newSettings.GuessGHDLPath;
                GuessGTKWavePath = newSettings.GuessGTKWavePath;
                SimLimit = newSettings.SimLimit;
                ProcLimit = newSettings.ProcLimit;
                ErrorWindowHeight = newSettings.ErrorWindowHeight;
                ErrorWindowWidth = newSettings.ErrorWindowWidth;
                ErrorWindowTextSize = newSettings.ErrorWindowTextSize;
                VHDLStd = newSettings.VHDLStd;
                if (version >= 0.9)
                {
                    MainWindowWidth = newSettings.MainWindowWidth;
                    MainWindowHeight = newSettings.MainWindowHeight;
                    LastlyUsedFolder = newSettings.LastlyUsedFolder;
                }
                if (version < 0.8)
                    Save();
            }
            catch (FileNotFoundException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                DialogBox.Warn("設定ファイルのロードに失敗しました．形式が正しくありません．");
                return false;
            }
            catch (Exception ex)
            {
                DialogBox.Warn("設定ファイルのロード中にエラーが発生しました．\n" +
                    "設定ファイルを保存しません．\n\nエラー内容: " + ex.Message);
                DisableSaveSettings = true;
                return false;
            }
            return true;
        }
        
        [UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026:RequiresUnreferencedCode")]
        public void Save()
        {
            if (DisableSaveSettings)
                return;
            try
            {
                XmlSerializer serial = new XmlSerializer(typeof(GGFrontSettings));
                FileStream fs = new FileStream(Util.SettingName, FileMode.Create);
                serial.Serialize(fs, this);
                fs.Close();
            }
            catch (Exception ex)
            {
                DialogBox.Warn("設定ファイルのセーブ中にエラーが発生しました．\n" +
                    "これ以降，設定ファイルを保存しません．\n\nエラー内容: " + ex.Message);
                DisableSaveSettings = true;
            }
        }

        public bool Check()
        {
            if (GHDLPath == "" && !GuessGHDLPath)
            {
                DialogBox.Warn("GHDLのパスが指定されていません．");
                return false;
            }
            if (GTKWavePath == "" && !GuessGTKWavePath)
            {
                DialogBox.Warn("GTKWaveのパスが指定されていません．");
                return false;
            }
            return true;
        }
    }

    // プロジェクトファイル（だったもの）に対応するクラス
    public class GGFrontProject
    {
        public string WavePath, TopModule;
        public bool GuessTopModule;
        public List<string> SourceFiles;
        public EntityHierarchy Hierarchy;
        public bool UseVHDL2008;

        public GGFrontProject()
        {
            WavePath = TopModule = "";
            SourceFiles = new List<string>();
            Hierarchy = new EntityHierarchy(this);
            GuessTopModule = true;
            UseVHDL2008 = false;
        }

        public bool Check()
        {
            if (TopModule == "")
            {
                DialogBox.Warn("Hierarchy リストに表示された問題を解決してください．");
                return false;
            }
            return true;
        }
    }

    // エラー一覧の各要素に対応するクラス
    public class GHDLErrorDescription
    {
        public string Pattern, Name, Description, Handling;

        public GHDLErrorDescription(string pattern, string name, string description, string handling)
        {
            Pattern = pattern;
            Name = name;
            Description = description;
            Handling = handling;
        }
    }
    
    // エラー一覧に対応するクラス
    public class GHDLErrorList
    {
        List<GHDLErrorDescription> errors;

        public GHDLErrorList()
        {
            errors = new List<GHDLErrorDescription>();
            string[] strs = Resources.ErrorList.Replace("\r\n","\n").Split(new[]{ '\n'});
            for (int i = 0; i < strs.Length - 3; i += 4)
                errors.Add(new GHDLErrorDescription(
                    strs[i], strs[i + 1], strs[i + 2], strs[i + 3]));
        }

        public GHDLErrorDescription? Match (string str)
        {
            foreach(GHDLErrorDescription error in errors)
            {
                if (Regex.Match(str, error.Pattern).Success) {
                    return error;
                }
            }
            return null;
        }
    }
}