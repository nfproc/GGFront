// GGFront: A GHDL/GTKWave GUI Frontend
// Copyright (C) 2018-2023 Naoki FUJIEDA. New BSD License is applied.
//**********************************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;

namespace GGFront
{
    // 諸々の処理
    public class Util
    {
        public static string baseDir, workDir, settingName;
        public static GGFrontSettings settings;
        public static GGFrontProject currentProject;
        public static GHDLErrorList errorList;
        public const string GGFrontDataVersion = "0.5";

        public static readonly int[] procLimits = new int[] { 3000, 5000, 10000, 15000, 20000 };
        public static readonly string[] simLimits = new string[] { "1ms", "10ms", "100ms", "1000ms", "10000ms" };

        // プログラム実行開始時に最初に行う処理
        public static void Initialize()
        {
            baseDir = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) + "\\";
            workDir = baseDir + "work\\";
            if (!Directory.Exists(workDir))
                Directory.CreateDirectory(workDir);
            settingName = baseDir + "setting.xml";
            settings = new GGFrontSettings();
            currentProject = new GGFrontProject();
            errorList = new GHDLErrorList();
            if (!settings.Load())
            {
                //Warn("初めにGHDLとGTKWaveのファイルを指定してください．");
                settings.guessGHDLPath = true;
                settings.guessGTKWavePath = true;
            }
        }

        // GHDLを何度か実行して，VHDLのコンパイルとシミュレーションを行う
        public static void CompileAndSimulate()
        {
            Stopwatch swAll, swGHDL, swOut;
            int numSources = 0, numErrors = 0;
            string args;
            const string compileOption = "-fexplicit -fsynopsys";
            string simulationOption = "--vcd=wave.vcd --ieee-asserts=disable --stop-time=" + settings.simLimit;
            List<VHDLSource> sources = new List<VHDLSource>();
            List<List<int>> lineNumber = new List<List<int>>();
            Dictionary<string, VHDLSource.VHDLEnumeration> enumSignals = new Dictionary<string, VHDLSource.VHDLEnumeration>();

            // 入力が空でないかをチェック
            if (!settings.Check())
                return;
            settings.Save();
            if (!currentProject.Check())
                return;

            swAll = new Stopwatch();
            swGHDL = new Stopwatch();
            swOut = new Stopwatch();

            // 入力のリストアップ・整形・解析
            swAll.Start();
            args = "-a " + compileOption;
            CleanWorkDir();
            GHDLResult analResult = null;
            foreach (string FileName in currentProject.sourceFiles)
            {
                numSources += 1;
                VHDLSource newSource = new VHDLSource(FileName, numSources);
                if (newSource.isValid)
                {
                    newSource.CheckDataFileReference(currentProject.hierarchy);
                    newSource.CopyToWorkDirectory(workDir);
                }
                if (! newSource.isValid)
                {
                    Warn(newSource.content);
                    return;
                }
                sources.Add(newSource);
                lineNumber.Add(newSource.origLineNumber);
                foreach (KeyValuePair<string, VHDLSource.VHDLEnumeration> en in newSource.enumSignals)
                    enumSignals[en.Key] = en.Value;

                args = "-a " + compileOption + " " + newSource.FileName.Internal;
                swGHDL.Start();
                analResult = ExecToolAndGetResult(GetGHDLPath(), args, analResult);
                swGHDL.Stop();
                if (analResult == null)
                    return;
                if (analResult.code != 0)
                    numErrors += 1;
            }

            // 解析にエラーがなければ，再解析（Elaborate）を行う
            if (numErrors == 0)
            {
                args = "-e " + compileOption + " " + currentProject.topModule;
                analResult = ExecToolAndGetResult(GetGHDLPath(), args, analResult);
                if (analResult == null)
                    return;
                if (analResult.code != 0)
                    numErrors = -1;
            }

            // ソースの解析結果の整形
            analResult.RestoreFileName(currentProject.sourceFiles, lineNumber);
            if (numErrors != 0)
            {
                string errorIn;
                if (numErrors == -1)
                    errorIn = "ファイル全体";
                else
                    errorIn = numErrors + "個のファイル";
                Warn(errorIn + "の解析中にエラーが発生しました．詳しくはログを参照してください．");
                analResult.code = 1;
                analResult.ShowMessage();
                return;
            }
            else if (analResult.message != "")
            {
                analResult.ShowMessage();
                if (!WarnAndConfirm("解析中に警告が発生しました．詳しくはログを参照してください．\n" +
                    "続けてシミュレーションを行いますか？"))
                    return;
            }

            // シミュレーションとその結果の整形
            args = "-r " + compileOption + " " + currentProject.topModule + " " + simulationOption;
            swGHDL.Start();
            GHDLResult simResult = ExecToolAndGetResult(GetGHDLPath(), args);
            swGHDL.Stop();
            if (simResult == null)
                return;
            simResult.RestoreFileName(currentProject.sourceFiles, lineNumber);
            swAll.Stop();
            if (simResult.violateAssertion)
            {
                String timeString = String.Format("{0:#,0.###}", simResult.simTime / 1000000.0);
                Info("シミュレーションは " + timeString + " ns 後に停止しました．");
            }
            else if (simResult.code != 0)
            {
                Warn("シミュレーション中にエラーが発生しました．詳しくはログを参照してください．");
                simResult.ShowMessage();
                return;
            }
            else
            {
                Warn("シミュレーションは " + settings.simLimit + " 以内に終了しませんでした．");
            }

            // 出力ファイル（波形・テストベンチ出力）のコピー
            swOut.Start();
            VCDResult wave = new VCDResult(workDir + "wave.vcd", simResult.simTime, enumSignals);
            wave.WriteTo(currentProject.wavePath);
            if (! wave.isValid)
                Warn(wave.content);

            foreach (VHDLSource source in sources)
            {
                source.CopyFromWorkDirectory(workDir);
                if (! source.isValid)
                    Warn(source.content);
            }
            swOut.Stop();
            string timeStringReal = "シミュレーションに " + swAll.Elapsed.TotalMilliseconds + " ミリ秒かかりました．\n";
            timeStringReal += "そのうち GHDL の実行に " + swGHDL.Elapsed.TotalMilliseconds + " ミリ秒かかりました．\n";
            timeStringReal += "シミュレーション後の結果の整形に " + swOut.Elapsed.TotalMilliseconds + " ミリ秒かかりました．";
            Info(timeStringReal);
        }

        // work ディレクトリを削除
        public static bool CleanWorkDir()
        {
            try
            {
                foreach (string FileName in Directory.EnumerateFiles(baseDir + "work", "*.*"))
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
            if (! p.WaitForExit(settings.procLimit))
            {
                p.Kill();
                Warn("GHDLが指定した時間内に終了しなかったため，強制停止しました．\n再度試すか，無限ループとなる記述がないか確認してください．");
                p.Close();
                return null;
            }
            result.code = p.ExitCode;
            if (! string.IsNullOrEmpty(result.message) && (outMessage != "" || errMessage != ""))
                result.message += "\n";
            result.message += outMessage + errMessage;
            p.Close();

            return result;
        }

        // 外部プログラムを実行（主に出力を必要としない場合）
        public static Process ExecTool(string FileName, string args, bool NoWindow = false, bool ShellExec = false)
        {
            Process p = new Process();
            try
            {
                p.StartInfo.FileName = FileName;
                p.StartInfo.UseShellExecute = ShellExec;
                p.StartInfo.RedirectStandardOutput = (!ShellExec);
                p.StartInfo.RedirectStandardError = (!ShellExec);
                p.StartInfo.CreateNoWindow = NoWindow;
                p.StartInfo.WorkingDirectory = workDir;
                p.StartInfo.Arguments = args;
                p.Start();
            }
            catch (Win32Exception)
            {
                Warn("プロセス起動中のエラー．実行ファイルのファイル名を確認してください．\n" +
                                "対象: " + FileName);
                return null;
            }
            return p;
        }

        // GHDL, GTKWave の置き場所として指定されたパスを返す
        public static string GetGHDLPath()
        {
            if (settings.guessGHDLPath)
                return baseDir + "GHDL\\bin\\ghdl.exe";
            else
                return settings.GHDLPath;
        }

        public static string GetGTKWavePath()
        {
            if (settings.guessGTKWavePath)
                return baseDir + "gtkwave\\bin\\gtkwave.exe";
            else
                return settings.GTKWavePath;
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
