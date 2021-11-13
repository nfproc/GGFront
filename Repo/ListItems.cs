// GGFront: A GHDL/GTKWave GUI Frontend
// Copyright (C) 2018-2021 Naoki FUJIEDA. New BSD License is applied.
//**********************************************************************

using System;
using System.Windows.Data;
using System.Windows.Media;

namespace GGFront
{
    // ソースコードのリスト項目を保持するクラス
    public class SourceItem
    {
        public string Name { get; set; }
        public bool Selected { get; set; }
    }

    // Entity の参照関係の解析結果を保持するクラス
    public class EntityHierarchyItem
    {
        public bool IsValid { get; set; }
        public int Level { get; set; }
        public string Name { get; set; }
        public string ShortPath { get; set; }
        public string LongPath { get; set; }
        public bool IsTop { get; set; }
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
            return (! (bool)values[0]) ? Brushes.Brown : ((bool)values[1]) ? Brushes.Blue : Brushes.Black;
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

    // コンボボックスのリスト項目を保持するクラス
    public class LimitSelectorItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ProcTime { get; set; } = 0;
        public string SimTime { get; set; } = "";
    }
}