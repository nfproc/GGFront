// GGFront: A GHDL/GTKWave GUI Frontend
// Copyright (C) 2018-2025 Naoki FUJIEDA. New BSD License is applied.
//**********************************************************************
using Avalonia;
using System;
using GGFront.Models;
using Avalonia.Media;
using System.Collections.Generic;

namespace GGFront
{
    internal class Program
    {
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>().With(new FontManagerOptions()
            {
                DefaultFamilyName = "Noto Sans JP",
                FontFamilyMappings = new Dictionary<string, FontFamily>()
                {
                    {"Noto Sans JP", new FontFamily("avares://GGFront/Assets#Noto Sans JP") },
                    {"Noto Sans Mono", new FontFamily("avares://GGFront/Assets#Noto Sans Mono")  }
                }
            }).UsePlatformDetect().With(new X11PlatformOptions()
            {
                UseDBusFilePicker = false,
            });

        [STAThread]
        public static void Main() {
            Util.Initialize();
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(Array.Empty<string>());
        }
    }
}