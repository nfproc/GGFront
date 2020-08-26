// GGFront: A GHDL/GTKWave GUI Frontend
// Copyright (C) 2018-2020 Naoki FUJIEDA. New BSD License is applied.
//**********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace GGFront
{
    // 設定ファイルに対応するクラス
    public class GGFrontSettings
    {
        public string GGFrontVersion;
        public bool guessGHDLPath, guessGTKWavePath;
        public string GHDLPath, GTKWavePath;
        public string simLimit; // v0.4.4+
        public int procLimit;   // v0.4.4+
        private bool disableSaveSettings;

        public const string simLimitDefault = "1ms";
        public const int procLimitDefault = 3000; // ms

        public GGFrontSettings()
        {
            GGFrontVersion = Util.GGFrontDataVersion;
            disableSaveSettings = false;
        }

        private void Reset()
        {
            GHDLPath = "";
            GTKWavePath = "";
            guessGHDLPath = guessGTKWavePath = false;
        }

        public bool Load()
        {
            try
            {
                XmlSerializer serial = new XmlSerializer(typeof(GGFrontSettings));
                FileStream fs = new FileStream(Util.settingName, FileMode.Open);
                GGFrontSettings newSettings = (GGFrontSettings)serial.Deserialize(fs);
                double version = Double.Parse(newSettings.GGFrontVersion);
                if (version < 0.3)
                {
                    guessGHDLPath = false;
                    guessGTKWavePath = false;
                }
                else
                {
                    guessGHDLPath = newSettings.guessGHDLPath;
                    guessGTKWavePath = newSettings.guessGTKWavePath;
                }
                if (version < 0.4)
                {
                    simLimit = simLimitDefault;
                    procLimit = procLimitDefault;
                }
                else
                {
                    simLimit = newSettings.simLimit;
                    procLimit = newSettings.procLimit;
                }
                GHDLPath = newSettings.GHDLPath;
                GTKWavePath = newSettings.GTKWavePath;
                fs.Close();
            }
            catch (FileNotFoundException)
            {
                Reset();
                return false;
            }
            catch (InvalidOperationException)
            {
                Util.Warn("設定ファイルのロードに失敗しました．形式が正しくありません．");
                Reset();
                return false;
            }
            catch (Exception ex)
            {
                Util.Warn("設定ファイルのロード中にエラーが発生しました．\n" +
                    "設定ファイルを保存しません．\n\nエラー内容: " + ex.Message);
                Reset();
                disableSaveSettings = true;
                return false;
            }
            return true;
        }

        public void Save()
        {
            if (disableSaveSettings)
                return;
            try
            {
                XmlSerializer serial = new XmlSerializer(typeof(GGFrontSettings));
                FileStream fs = new FileStream(Util.settingName, FileMode.Create);
                serial.Serialize(fs, this);
                fs.Close();
            }
            catch (Exception ex)
            {
                Util.Warn("設定ファイルのセーブ中にエラーが発生しました．\n" +
                    "これ以降，設定ファイルを保存しません．\n\nエラー内容: " + ex.Message);
                disableSaveSettings = true;
            }
        }

        public bool Check()
        {
            if (GHDLPath == "" && !guessGHDLPath)
            {
                Util.Warn("GHDLのパスが指定されていません．");
                return false;
            }
            if (GTKWavePath == "" && !guessGTKWavePath)
            {
                Util.Warn("GTKWaveのパスが指定されていません．");
                return false;
            }
            return true;
        }
    }

    // プロジェクトファイルに対応するクラス
    public class GGFrontProject
    {
        public string GGFrontVersion;
        public string wavePath, topModule;
        public List<string> sourceFiles;
        public EntityHierarchy hierarchy;

        public GGFrontProject()
        {
            sourceFiles = new List<string>();
            hierarchy = new EntityHierarchy(this);
        }

        public bool Check()
        {
            if (topModule == "")
            {
                Util.Warn("Hierarchy リストに表示された問題を解決してください．");
                return false;
            }
            return true;
        }
    }
}