// GGFront: A GHDL/GTKWave GUI Frontend
// Copyright (C) 2018-2025 Naoki FUJIEDA. New BSD License is applied.
//**********************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace GGFront.Models
{
    internal class Util
    {
        public static string BaseDir = "", WorkDir = "", SettingName = "";
        public static string ExecutableExt = "";
        public static GGFrontSettings Settings = new GGFrontSettings();
        public static GGFrontProject CurrentProject = new GGFrontProject();
        public static GHDLErrorList ErrorList = new GHDLErrorList();
        public const string GGFrontDataVersion = "0.9";

        public static readonly int[] ProcLimits = new int[] { 3000, 5000, 10000, 15000, 20000 };
        public static readonly int[] SimLimits = new int[] { 1, 10, 100, 1000, 10000 };

        // プログラム実行開始時に最初に行う処理
        public static void Initialize()
        {
            BaseDir = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) + "/";
            WorkDir = BaseDir + "work/";
            if (! Directory.Exists(WorkDir))
                Directory.CreateDirectory(WorkDir);
            SettingName = BaseDir + "setting.xml";
            Settings.Load();
            if (OperatingSystem.IsWindows())
                ExecutableExt = ".exe";
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
            Dictionary<string, VHDLSource.EnumDecl> enumSignals = new Dictionary<string, VHDLSource.EnumDecl>();

            // 入力が空でないかをチェック
            if (! Settings.Check())
                return;
            Settings.Save();

            if (! CurrentProject.Check())
                return;

            // 入力のリストアップ・整形・解析
            args = "-a " + compileOption;
            CleanWorkDir();
            GHDLResult? analResult = null;
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
                    DialogBox.Warn(newSource.Content);
                    return;
                }
                sources.Add(newSource);
                lineNumber.Add(newSource.OriginalLineNumber);
                foreach (KeyValuePair<string, VHDLSource.EnumDecl> en in newSource.EnumSignals)
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
            if (analResult == null) // 不要な if 文だが，null チェックの誤判定防止のため
                return;

            // ソースの解析結果の整形
            analResult.Analyze(CurrentProject.SourceFiles, lineNumber, CurrentProject.UseVHDL2008);

            if (numErrors != 0)
            {
                string errorIn = (numErrors == -1) ? "ファイル全体" : (numErrors + "個のファイル");
                DialogBox.Warn(errorIn + "の解析中にエラーが発生しました．詳しくはログを参照してください．");
                analResult.ExitCode = 1;
                DialogBox.ShowGHDLErrors(analResult);
                return;
            }
            else if (analResult.Message != "")
            {
                DialogBox.ShowGHDLErrors(analResult);
                if (! DialogBox.WarnAndConfirm("解析中に警告が発生しました．詳しくはログを参照してください．\n" +
                    "続けてシミュレーションを行いますか？"))
                    return;
            }

            // シミュレーションとその結果の整形
            args = "-r " + compileOption + " " + CurrentProject.TopModule + " " + simulationOption;
            GHDLResult? simResult = ExecToolAndGetResult(GetGHDLPath(), args);
            if (simResult == null)
                return;
            simResult.Analyze(CurrentProject.SourceFiles, lineNumber, CurrentProject.UseVHDL2008);
            if (simResult.SimFinished)
            {
                string timeString = String.Format("{0:#,0.###}", simResult.SimTime / 1000000.0);
                DialogBox.Info("シミュレーションは " + timeString + " ns 後に停止しました．");
            }
            else if (simResult.ExitCode != 0)
            {
                DialogBox.Warn("シミュレーション中にエラーが発生しました．詳しくはログを参照してください．");
                DialogBox.ShowGHDLErrors(simResult);
                return;

            }
            else
            {
                DialogBox.Warn("シミュレーションは " + Settings.SimLimit + " ms 以内に終了しませんでした．");
            }

            // 出力ファイル（波形・テストベンチ出力）のコピー
            VCDResult wave = new VCDResult(WorkDir + "wave.vcd", simResult.SimTime, enumSignals);
            wave.WriteTo(CurrentProject.WavePath);
            if (! wave.IsValid)
                DialogBox.Warn(wave.Content);

            foreach (VHDLSource source in sources)
            {
                source.CopyFromWorkDirectory(WorkDir);
                if (! source.IsValid)
                    DialogBox.Warn(source.Content);
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
        public static GHDLResult? ExecToolAndGetResult(string FileName, string args, GHDLResult? result = null)
        {
            string outMessage = "";
            string errMessage = "";
            if (result == null)
                result = new GHDLResult();
            Process? p = ExecTool(FileName, args, true);
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
                DialogBox.Warn("GHDLが指定した時間内に終了しなかったため，強制停止しました．\n" +
                    "再度試すか，無限ループとなる記述がないか確認してください．");
                p.Close();
                return null;
            }
            p.WaitForExit(); // 引数なしの WaitForExit で DataReceivedEventHandler の処理を待つ
            result.ExitCode = p.ExitCode;
            if (! string.IsNullOrEmpty(result.Message) && (outMessage != "" || errMessage != ""))
                result.Message += "\n";
            result.Message += outMessage + errMessage;
            p.Close();
            return result;
        }


        // 外部プログラムを実行（主に出力を必要としない場合）
        public static Process? ExecTool(string FileName, string args, bool NoWindow = false, bool ShellExec = false)
        {
            // FileName が有効なパスかどうか
            try
            {
                Path.GetFullPath(FileName);
            }
            catch (Exception ex)
            {
                DialogBox.Warn(FileName + " は無効なパスです(" + ex.Message +
                    ")．\n設定画面で実行ファイルのパスを適切に設定してください．");
                return null;
            }

            // FileName が存在するかどうか
            if (! File.Exists(FileName))
            {
                DialogBox.Warn(Path.GetFileName(FileName) + " が " + Path.GetDirectoryName(FileName) +
                    " に見つかりません．\n設定画面で実行ファイルのパスを適切に設定してください．");
                return null;
            }

            // 問題なければ実行を試みる
            Process p = new Process();
            try
            {
                p.StartInfo.FileName = FileName.Replace("\\", "/");
                p.StartInfo.UseShellExecute = ShellExec;
                p.StartInfo.RedirectStandardOutput = (! ShellExec);
                p.StartInfo.RedirectStandardError = (! ShellExec);
                p.StartInfo.CreateNoWindow = NoWindow;
                p.StartInfo.WorkingDirectory = WorkDir;
                p.StartInfo.Arguments = args;
                // mac 版 GTKWave のライブラリ参照に関する問題の回避
                if (OperatingSystem.IsMacOS())
                    p.StartInfo.EnvironmentVariables.Add("GDK_PIXBUF_MODULE_FILE", "/dev/null");
                p.Start();
            }
            catch (Exception ex)
            {
                DialogBox.Warn(Path.GetFileName(FileName) + " の起動中にエラーが発生しました．\nエラー:" + ex.Message);
                return null;
            }

            return p;
        }

        // GHDL の置き場所として指定されたパスを返す
        public static string GetGHDLPath()
        {
            return (Settings.GuessGHDLPath) ? (BaseDir + "GHDL/bin/ghdl" + ExecutableExt) : Settings.GHDLPath;
        }

        // GTKWave の置き場所として指定されたパスを返す
        public static string GetGTKWavePath()
        {
            return (Settings.GuessGTKWavePath) ? (BaseDir + "gtkwave/bin/gtkwave" + ExecutableExt) : Settings.GTKWavePath;
        }        
    }
}
