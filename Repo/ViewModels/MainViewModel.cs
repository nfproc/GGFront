// GGFront: A GHDL/GTKWave GUI Frontend
// Copyright (C) 2018-2025 Naoki FUJIEDA. New BSD License is applied.
//**********************************************************************

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Media;

namespace GGFront
{
    // ■■ MainViewModel で使うリスト項目に対応するクラス
    // ソースコードのリスト (lstSources)
    public class SourceItem
    {
        public string Name { get; set; }
        public bool Selected { get; set; }
    }

    // Entity の参照関係の解析結果 (lstHierarchy)
    public class EntityHierarchyItem
    {
        public bool IsValid { get; set; }
        public int Level { get; set; }
        public string Name { get; set; }
        public string ShortPath { get; set; }
        public string LongPath { get; set; }
        public bool IsTop { get; set; }
    }

    // ■■ MainWindow のバインディングの解決用
    class MainViewModel : INotifyPropertyChanged
    {
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

        public ObservableCollection<int> ProcLimits { get; set; } = new ObservableCollection<int>();
        public ObservableCollection<int> SimLimits { get; set; } = new ObservableCollection<int>();
        public ObservableCollection<SourceItem> SourceCollection { get; set; } = new ObservableCollection<SourceItem>();
        public ObservableCollection<EntityHierarchyItem> HierarchyCollection { get; set; } = new ObservableCollection<EntityHierarchyItem>();

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }

    // EntityHierarchyItem のコンバータクラス
    public class HierarchyLevelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return String.Format("{0},0,0,0", (int)value * 30);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class HierarchyPathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return "";
            if ((string)value == "")
                return "";

            return "(in " + (string)value + ")";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class HierarchyTopColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (!(bool)values[0]) ? Brushes.Brown : ((bool)values[1]) ? Brushes.Blue : Brushes.Black;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class HierarchyTopFontConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ((bool)value) ? "Bold" : "Normal";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
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

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
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
                return MsecToString((int) value);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
            {
                return StringToMsec((string) value);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}