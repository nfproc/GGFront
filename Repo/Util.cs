// GGFront: A GHDL/GTKWave GUI Frontend
// Copyright (C) 2018-2021 Naoki FUJIEDA. New BSD License is applied.
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
            int numSources = 0;
            string args;
            const string compileOption = "-fexplicit --ieee=synopsys --warn-default-binding";
            string simulationOption = "--vcd=wave.vcd --ieee-asserts=disable --stop-time=" + settings.simLimit;
            List<VHDLSource> sources = new List<VHDLSource>();

            // 入力が空でないかをチェック
            if (!settings.Check())
                return;
            settings.Save();
            if (!currentProject.Check())
                return;

            // 入力のリストアップと整形
            args = "-a " + compileOption;
            CleanWorkDir();
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
                args += " " + newSource.FileName.Internal;
            }

            // ソースの解析とその結果の整形
            GHDLResult analResult = ExecToolAndGetResult(GetGHDLPath(), args);
            if (analResult == null)
                return;
            if (analResult.code == 0 && analResult.message != "")
            {
                // entityが見つからない警告が出ているが，ソースの指定順の問題かもしれない．再実行する．
                analResult = ExecToolAndGetResult(GetGHDLPath(), args);
                if (analResult == null)
                    return;
            }
            analResult.RestoreFileName(currentProject.sourceFiles);
            if (analResult.code != 0)
            {
                Warn("解析中にエラーが発生しました．詳しくはログを参照してください．");
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
            GHDLResult simResult = ExecToolAndGetResult(GetGHDLPath(), args);
            if (simResult == null)
                return;
            simResult.RestoreFileName(currentProject.sourceFiles);
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
            try
            {
                File.Copy(workDir + "wave.vcd", currentProject.wavePath, true);
                StreamWriter sw = new StreamWriter(currentProject.wavePath, true, Encoding.GetEncoding("ISO-8859-1"));
                sw.WriteLine("#" + simResult.simTime);
                sw.Close();
            }
            catch (IOException)
            {
                Warn("波形ファイルのコピー中にエラーが発生しました．");
            }
            foreach (VHDLSource source in sources)
            {
                source.CopyFromWorkDirectory(workDir);
                if (! source.isValid)
                    Warn(source.content);
            }
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
        public static GHDLResult ExecToolAndGetResult(string FileName, string args)
        {
            String outMessage = "";
            String errMessage = "";
            GHDLResult result = new GHDLResult();
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
            result.message = outMessage + errMessage;
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
