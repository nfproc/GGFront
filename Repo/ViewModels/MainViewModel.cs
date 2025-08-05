// GGFront: A GHDL/GTKWave GUI Frontend
// Copyright (C) 2018-2025 Naoki FUJIEDA. New BSD License is applied.
//**********************************************************************

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace GGFront.ViewModels
{
    // ■■ MainViewModel で使うリスト項目に対応するクラス
    // ソースコードのリスト (lstSources)
    public class SourceItem
    {
        public string? Name { get; set; }
        public bool Selected { get; set; }
    }
        
    // Entity の参照関係の解析結果 (lstHierarchy)
    public class EntityHierarchyItem
    {
        public bool IsValid { get; set; }
        public int Level { get; set; }
        public string? Name { get; set; }
        public string? ShortPath { get; set; }
        public string? LongPath { get; set; }
        public bool IsTop { get; set; }
    }
    
    // ■■ MainWindow のバインディングの解決用
    public class MainViewModel : INotifyPropertyChanged
    {
        // ■ プロパティ
        // ComboBox cmbProcLimit
        private int _procLimit = 0;
        public int ProcLimit
        {
            get => _procLimit;
            set
            {
                if (_procLimit == value)
                    return;
                _procLimit = value;
                OnPropertyChanged("ProcLimit");
            }
        }
        public ObservableCollection<int> ProcLimits { get; set; } = new ObservableCollection<int>();

        // ComboBox cmbSimLimit
        private int _simLimit = 0;
        public int SimLimit
        {
            get => _simLimit;
            set
            {
                if (_simLimit == value)
                    return;
                _simLimit = value;
                OnPropertyChanged("SimLimit");
            }
        }
        public ObservableCollection<int> SimLimits { get; set; } = new ObservableCollection<int>();

        // Label lblVHDLStd
        public enum VHDLVersions
        {
            None,
            VHDL93,
            VHDL2008,
            VHDL93Guess,
            VHDL2008Guess
        }

        public static readonly Dictionary<VHDLVersions, string> VHDLVersionString = new Dictionary<VHDLVersions, string>
        {
            { VHDLVersions.VHDL93, "VHDL-93" },
            { VHDLVersions.VHDL2008, "VHDL-2008" },
            { VHDLVersions.VHDL93Guess, "VHDL-93 (Guessed)" },
            { VHDLVersions.VHDL2008Guess, "VHDL-2008 (Guessed)" }
        };

        private VHDLVersions _vhdlVersion = VHDLVersions.VHDL93;
        public VHDLVersions VHDLVersion
        {
            get => _vhdlVersion;
            set
            {
                if (_vhdlVersion == value)
                    return;
                _vhdlVersion = value;
                OnPropertyChanged("VHDLVersion");
            }
        }

        // Label lblTopModule
        private string _wavePath = "";
        public string WavePath
        {
            get => _wavePath;
            set
            {
                if (_wavePath == value)
                    return;
                _wavePath = value;
                OnPropertyChanged("WavePath");
            }
        }

        // ListBox lstSources
        public ObservableCollection<SourceItem> SourceCollection { get; set; } =
            new ObservableCollection<SourceItem>();

        // ListBox lstHierarchy
        public ObservableCollection<EntityHierarchyItem> HierarchyCollection { get; set; } =
            new ObservableCollection<EntityHierarchyItem>();

        // ■ イベントハンドラ
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}

namespace GGFront.ViewModels.MainConverter
{
    // ■■ コンバータクラス
    // EntityHierarchyItem のコンバータクラス
    public class HierarchyLevelConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return new Thickness((int)(value ?? 0) * 30, 0, 0, 0);
        }
    
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    public class HierarchyPathConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
                return "";
            if ((string)value == "")
                return "";
    
            return "(in " + (string)value + ")";
        }
    
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    public class HierarchyTopColorConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (! (values[0] is Boolean && values[1] is Boolean))
                return Brushes.Black;
            return (! (bool) (values[0] ?? false)) ? Brushes.Brown :
                     ((bool) (values[1] ?? false)) ? Brushes.Blue : Brushes.Black;
        }
    
        public IList<object?> ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    public class HierarchyTopFontConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return ((bool) (value ?? false)) ? "Bold" : "Regular";
        }
    
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    // ProcLimits, SimLimits の要素のコンバータクラス
    public class TimeLimitConverter : IValueConverter
    {
        private string MsecToString(int t)
        {
            return (t % 1000 == 0) ? ((t / 1000) + " sec.") : (t + " msec.");
        }
    
        private int StringToMsec(string s)
        {
            return int.Parse(s.Replace(" sec.", "").Replace(" msec.", "")) * (s.EndsWith(" sec.") ? 1000 : 1);
        }
    
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ObservableCollection<int>)
            {
                List<string> strs = new List<string>();
                foreach (int v in (ObservableCollection<int>)value)
                    strs.Add(MsecToString(v));
                return strs;
            }
            else
            {
                return MsecToString((int) (value ?? 0));
            }
        }
    
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string)
                return StringToMsec((string) value);
            else if (value == null)
                return null;
            else
                throw new NotImplementedException();
        }
    }
    
    // VHDLVersion のコンバータクラス
    public class VHDLVersionConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is MainViewModel.VHDLVersions ver && ver != MainViewModel.VHDLVersions.None)
                return "VHDL Version: " + MainViewModel.VHDLVersionString[ver];
            return "";
        }
    
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    // WavePath のコンバータクラス
    public class WavePathConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string p && p != "")
                return "(WaveForm: " + Path.GetFileName(p) + ")";
            return "";
        }
    
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}