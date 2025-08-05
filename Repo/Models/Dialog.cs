// GGFront: A GHDL/GTKWave GUI Frontend
// Copyright (C) 2018-2025 Naoki FUJIEDA. New BSD License is applied.
//**********************************************************************

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;
using GGFront.Views;

namespace GGFront.Models
{
    public class DialogBox
    {
        public static Window? DefaultOwner;

        public static readonly List<ButtonDefinition> DialogOK = new List<ButtonDefinition>
            {
                new ButtonDefinition { Name = "OK", },
            };
        public static readonly List<ButtonDefinition> DialogYesNo = new List<ButtonDefinition>
            {
                new ButtonDefinition { Name = "Yes", },
                new ButtonDefinition { Name = "No", },
            };

        // メッセージボックスの共通設定（Info, Warn, WarnAndConfirm で使う）
        private static MessageBoxCustomParams GetDefaultMsBoxOptions
            (Window? owner, string title, string message, Icon icon, List<ButtonDefinition> buttons)
        {
            return new MessageBoxCustomParams
            {
                ButtonDefinitions = buttons,
                ContentTitle = title,
                ContentMessage = message,
                Icon = icon,
                WindowStartupLocation = owner == null ? WindowStartupLocation.CenterScreen :
                                                          WindowStartupLocation.CenterOwner,
                CanResize = false,
                Topmost = false
            };
        }
        
        // メッセージボックスを開く（Info, Warn, WarnAndConfirm で使う）
        private static string ShowMsBox(Window? owner, IMsBox<string> msbox)
        {
            Task<string> task = owner == null ? msbox.ShowWindowAsync() : msbox.ShowWindowDialogAsync(owner);
            WaitOnDispatcherFrame(task);
            return task.Result;
        }

        // メッセージボックスが閉じるのを待つ（Info, Warn, WarnAndConfirm で使う）
        private static T WaitOnDispatcherFrame<T>(Task<T> task)
        {
            if (! task.IsCompleted)
            {
                DispatcherFrame frame = new DispatcherFrame();
                task.ContinueWith(static (_, s) => {
                    if (s == null)
                        throw new ArgumentNullException();
                    ((DispatcherFrame) s).Continue = false;
                }, frame);
                Dispatcher.UIThread.PushFrame(frame);
            }
            return task.GetAwaiter().GetResult();
        }

        // メッセージボックス（情報）
        public static void Info(string message) { Info(DefaultOwner, message); }
        public static void Info(Window? owner, string message)
        {
            IMsBox<string> MsBox = MessageBoxManager.GetMessageBoxCustom(
                GetDefaultMsBoxOptions(owner, "情報", message, Icon.Success, DialogOK));
            ShowMsBox(owner, MsBox);
        }
        
        // メッセージボックス（警告）
        public static void Warn(string message) { Warn(DefaultOwner, message); }
        public static void Warn(Window? owner, string message)
        {
            IMsBox<string> MsBox = MessageBoxManager.GetMessageBoxCustom(
                GetDefaultMsBoxOptions(owner, "警告", message, Icon.Warning, DialogOK));
            ShowMsBox(owner, MsBox);
        }
        
        // メッセージボックス（はい・いいえ選択つきの警告）
        public static bool WarnAndConfirm(string message) { return WarnAndConfirm(DefaultOwner, message); }
        public static bool WarnAndConfirm(Window? owner, string message)
        {
            IMsBox<string> MsBox = MessageBoxManager.GetMessageBoxCustom(
                GetDefaultMsBoxOptions(owner, "警告", message, Icon.Warning, DialogYesNo));
            return ShowMsBox(owner, MsBox).Equals("Yes");
        }

        // エラー内容を表示するダイアログ
        public static void ShowGHDLErrors(GHDLResult result) { ShowGHDLErrors(DefaultOwner, result); }
        public static void ShowGHDLErrors(Window? owner, GHDLResult result)
        {
            ErrorWindow win = new ErrorWindow(result.ErrorDetails);
            if (owner == null)
                win.Show();
            else
                win.Show(owner);
        }
        
        // ファイルを選択するダイアログの共通設定（PickFile, PickFiles で使う）
        private static FilePickerOpenOptions GetDefaultPickerOptions(
            Window owner, string title, string file, string type, List<string> extensions, string? start)
        {
            IStorageFolder? startFolder = null;
            if (start != null)
            {
                Task<IStorageFolder?> task = owner.StorageProvider.TryGetFolderFromPathAsync(start);
                startFolder = WaitOnDispatcherFrame(task);
            }
            return new FilePickerOpenOptions()
            {
                Title = title,
                SuggestedStartLocation = startFolder,
                SuggestedFileName = file,
                FileTypeFilter = [
                    new FilePickerFileType(type)
                    {
                        Patterns = extensions
                    }
                ]
            };
        }
        
        // ファイルを選択するダイアログ（単一ファイル）
        public async static Task<string?> PickFile
            (string title, string file, string type, List<string> exts, string? start)
            { return await PickFile(DefaultOwner, title, file, type, exts, start); }
        public async static Task<string?> PickFile(
            Window? owner, string title, string file, string type, List<string> exts, string? start)
        {
            if (owner == null)
                throw new ArgumentNullException();
            FilePickerOpenOptions dialog = GetDefaultPickerOptions(owner, title, file, type, exts, start);
            IReadOnlyList<IStorageFile> files = await owner.StorageProvider.OpenFilePickerAsync(dialog);

            if (files.Count > 0)
            {
                if (files[0].TryGetLocalPath() is string result)
                    return result;
            }
            return null;
        }
        
        // ファイルを選択するダイアログ（複数ファイル）
        public async static Task<List<string>> PickFiles
            (string title, string file, string type, List<string> exts, string? start)
            { return await PickFiles(DefaultOwner, title, file, type, exts, start); }
        public async static Task<List<string>> PickFiles(
            Window? owner, string title, string file, string type, List<string> exts, string? start)
        {
            if (owner == null)
                throw new ArgumentNullException();
            FilePickerOpenOptions dialog = GetDefaultPickerOptions(owner, title, file, type, exts, start);
            dialog.AllowMultiple = true;
            IReadOnlyList<IStorageFile> files = await owner.StorageProvider.OpenFilePickerAsync(dialog);
            
            List<string> results = new List<string>();
            foreach(IStorageFile f in files) {
                if (f.TryGetLocalPath() is string result)
                    results.Add(result);
            }
            return results;
        }
    }
}
