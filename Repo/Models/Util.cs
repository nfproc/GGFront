// GGFront: A GHDL/GTKWave GUI Frontend
// Copyright (C) 2018-2025 Naoki FUJIEDA. New BSD License is applied.
//**********************************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace GGFront
{
    // 諸々の処理
    public class Util
    {
        public static string BaseDir, WorkDir, SettingName;
        public static GGFrontSettings Settings;
        public static GGFrontProject CurrentProject;
        public static GHDLErrorList ErrorList;
        public const string GGFrontDataVersion = "0.8";

        public static readonly int[] ProcLimits = new int[] { 3000, 5000, 10000, 15000, 20000 };
        public static readonly int[] SimLimits  = new int[] { 1, 10, 100, 1000, 10000 };

        // プログラム実行開始時に最初に行う処理
        public static void Initialize()
        {
            BaseDir = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) + "\\";
            WorkDir = BaseDir + "work\\";
            if (!Directory.Exists(WorkDir))
                Directory.CreateDirectory(WorkDir);
            SettingName = BaseDir + "setting.xml";
            Settings = new GGFrontSettings();
            CurrentProject = new GGFrontProject();
            ErrorList = new GHDLErrorList();
            if (!Settings.Load())
            {
                //Warn("初めにGHDLとGTKWaveのファイルを指定してください．");
                Settings.GuessGHDLPath = true;
                Settings.GuessGTKWavePath = true;
            }
        }

        // GHDLを何度か実行して，VHDLのコンパイルとシミュレーションを行う
        public static void CompileAndSimulate()
        {
            int numSources = 0, numErrors = 0;
            string args;
            string compileOption = (CurrentProject.UseVHDL2008) ? "--std=08" : "-fexplicit -fsynopsys";
            string simulationOption = "--vcd=wave.vcd --ieee-asserts=disable --stop-time=" + Settings.SimLimit + "ms";
            List<VHDLSource> sources = new List<VHDLSource>();
            List<List<int>> lineNumber = new List<List<int>>();
            Dictionary<string, VHDLSource.VHDLEnumeration> enumSignals = new Dictionary<string, VHDLSource.VHDLEnumeration>();

            // 入力が空でないかをチェック
            if (!Settings.Check())
                return;
            Settings.Save();
            if (!CurrentProject.Check())
                return;

            // 入力のリストアップ・整形・解析
            args = "-a " + compileOption;
            CleanWorkDir();
            GHDLResult analResult = null;
            foreach (string FileName in CurrentProject.SourceFiles)
            {
                numSources += 1;
                VHDLSource newSource = new VHDLSource(FileName, numSources);
                if (newSource.IsValid)
                {
                    newSource.CheckDataFileReference(CurrentProject.Hierarchy);
                    newSource.CopyToWorkDirectory(WorkDir);
                }
                if (! newSource.IsValid)
                {
                    Warn(newSource.Content);
                    return;
                }
                sources.Add(newSource);
                lineNumber.Add(newSource.OriginalLineNumber);
                foreach (KeyValuePair<string, VHDLSource.VHDLEnumeration> en in newSource.EnumSignals)
                    enumSignals[en.Key] = en.Value;

                args = "-a " + compileOption + " " + newSource.FileName.Internal;
                analResult = ExecToolAndGetResult(GetGHDLPath(), args, analResult);
                if (analResult == null)
                    return;
                if (analResult.ExitCode != 0)
                    numErrors += 1;
            }

            // 解析にエラーがなければ，再解析（Elaborate）を行う
            if (numErrors == 0)
            {
                args = "-e " + compileOption + " " + CurrentProject.TopModule;
                analResult = ExecToolAndGetResult(GetGHDLPath(), args, analResult);
                if (analResult == null)
                    return;
                if (analResult.ExitCode != 0)
                    numErrors = -1;
            }

            // ソースの解析結果の整形
            analResult.RestoreFileName(CurrentProject.SourceFiles, lineNumber, CurrentProject.UseVHDL2008);
            if (numErrors != 0)
            {
                string errorIn;
                if (numErrors == -1)
                    errorIn = "ファイル全体";
                else
                    errorIn = numErrors + "個のファイル";
                Warn(errorIn + "の解析中にエラーが発生しました．詳しくはログを参照してください．");
                analResult.ExitCode = 1;
                analResult.ShowMessage();
                return;
            }
            else if (analResult.Message != "")
            {
                analResult.ShowMessage();
                if (!WarnAndConfirm("解析中に警告が発生しました．詳しくはログを参照してください．\n" +
                    "続けてシミュレーションを行いますか？"))
                    return;
            }

            // シミュレーションとその結果の整形
            args = "-r " + compileOption + " " + CurrentProject.TopModule + " " + simulationOption;
            GHDLResult simResult = ExecToolAndGetResult(GetGHDLPath(), args);
            if (simResult == null)
                return;
            simResult.RestoreFileName(CurrentProject.SourceFiles, lineNumber, CurrentProject.UseVHDL2008);
            if (simResult.SimFinished)
            {
                String timeString = String.Format("{0:#,0.###}", simResult.SimTime / 1000000.0);
                Info("シミュレーションは " + timeString + " ns 後に停止しました．");
            }
            else if (simResult.ExitCode != 0)
            {
                Warn("シミュレーション中にエラーが発生しました．詳しくはログを参照してください．");
                simResult.ShowMessage();
                return;
            }
            else
            {
                Warn("シミュレーションは " + Settings.SimLimit + " 以内に終了しませんでした．");
            }

            // 出力ファイル（波形・テストベンチ出力）のコピー
            VCDResult wave = new VCDResult(WorkDir + "wave.vcd", simResult.SimTime, enumSignals);
            wave.WriteTo(CurrentProject.WavePath);
            if (! wave.IsValid)
                Warn(wave.Content);

            foreach (VHDLSource source in sources)
            {
                source.CopyFromWorkDirectory(WorkDir);
                if (! source.IsValid)
                    Warn(source.Content);
            }
        }

        // work ディレクトリを削除
        public static bool CleanWorkDir()
        {
            try
            {
                foreach (string FileName in Directory.EnumerateFiles(BaseDir + "work", "*.*"))
                    File.Delete(FileName);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        // 外部プログラムを実行（出力を必要とする場合）
        public static GHDLResult ExecToolAndGetResult(string FileName, string args, GHDLResult result = null)
        {
            string outMessage = "";
            string errMessage = "";
            if (result == null)
                result = new GHDLResult();
            Process p = ExecTool(FileName, args, true);
            if (p == null)
                return null;
            p.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
            {
                if (e.Data != null)
                    outMessage += e.Data + "\n";
            });
            p.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
            {
                if (e.Data != null)
                    errMessage += e.Data + "\n";
            });
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            if (! p.WaitForExit(Settings.ProcLimit))
            {
                p.Kill();
                Warn("GHDLが指定した時間内に終了しなかったため，強制停止しました．\n再度試すか，無限ループとなる記述がないか確認してください．");
                p.Close();
                return null;
            }
            result.ExitCode = p.ExitCode;
            if (! string.IsNullOrEmpty(result.Message) && (outMessage != "" || errMessage != ""))
                result.Message += "\n";
            result.Message += outMessage + errMessage;
            p.Close();

            return result;
        }

        // 外部プログラムを実行（主に出力を必要としない場合）
        public static Process ExecTool(string FileName, string args, bool NoWindow = false, bool ShellExec = false)
        {
            // FileName が有効なパスかどうか
            try
            {
                Path.GetFullPath(FileName);
            }
            catch (Exception ex)
            {
                Warn(FileName + " は無効なパスです(" + ex.Message +
                    ")．\n設定画面で実行ファイルのパスを適切に設定してください．");
                return null;
            }

            // FileName が存在するかどうか
            if (! File.Exists(FileName))
            {
                Warn(Path.GetFileName(FileName) + " が " + Path.GetDirectoryName(FileName) +
                    " に見つかりません．\n設定画面で実行ファイルのパスを適切に設定してください．");
                return null;
            }

            // 問題なければ実行を試みる
            Process p = new Process();
            try
            {
                p.StartInfo.FileName = FileName;
                p.StartInfo.UseShellExecute = ShellExec;
                p.StartInfo.RedirectStandardOutput = (!ShellExec);
                p.StartInfo.RedirectStandardError = (!ShellExec);
                p.StartInfo.CreateNoWindow = NoWindow;
                p.StartInfo.WorkingDirectory = WorkDir;
                p.StartInfo.Arguments = args;
                p.Start();
            }
            catch (Win32Exception)
            {
                Warn(Path.GetFileName(FileName) + " の起動中にエラーが発生しました．");
                return null;
            }
            return p;
        }

        // GHDL, GTKWave の置き場所として指定されたパスを返す
        public static string GetGHDLPath()
        {
            if (Settings.GuessGHDLPath)
                return BaseDir + "GHDL\\bin\\ghdl.exe";
            else
                return Settings.GHDLPath;
        }

        public static string GetGTKWavePath()
        {
            if (Settings.GuessGTKWavePath)
                return BaseDir + "gtkwave\\bin\\gtkwave.exe";
            else
                return Settings.GTKWavePath;
        }

        // メッセージボックス
        public static void Info(string Message)
        {
            MessageBox.Show(Message, "情報", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void Warn(string Message)
        {
            MessageBox.Show(Message, "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public static bool WarnAndConfirm(string Message)
        {
            MessageBoxResult result = MessageBox.Show(Message, "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            return (result == MessageBoxResult.Yes);
        }
    }
}
