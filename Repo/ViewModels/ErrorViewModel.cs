// GGFront: A GHDL/GTKWave GUI Frontend
// Copyright (C) 2018-2025 Naoki FUJIEDA. New BSD License is applied.
//**********************************************************************

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Data.Converters;

namespace GGFront.ViewModels
{
    // ■■ ErrorViewModel で使うリスト項目に対応するクラス
    // エラーリスト (lstErrors)
    public class ErrorListItem
    {
        public string Details { get; }
        public string Head { get; }
        public string Code { get; }

        public ErrorListItem(string head, string code, string details)
        {
            Head = head;
            Code = code;
            Details = details;
        }
    }

    // ■■ ErrorWindow のバインディングの解決用
    public class ErrorViewModel : INotifyPropertyChanged
    {
        // ■ プロパティ
        // 文字サイズ全般
        private int _textSize = 12;
        public int TextSize
        {
            get => _textSize;
            set
            {
                if (_textSize == value)
                    return;
                _textSize = value;
                OnPropertyChanged("TextSize");
            }
        }

        // ListBox lstErrors
        private ErrorListItem? _selectedError;
        public ErrorListItem? SelectedError
        {
            get => _selectedError;
            set
            {
                if (_selectedError == value)
                    return;
                _selectedError = value;
                OnPropertyChanged("SelectedError");
            }
        }
        public ObservableCollection<ErrorListItem> ErrorLists { get; set; } =
            new ObservableCollection<ErrorListItem>();
        
        // ■ イベントハンドラ
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}

namespace GGFront.ViewModels.ErrorConverter
{
    // ■■ コンバータクラス
    // txtDetails のコンバータクラス
    public class ErrorDetailsConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ErrorListItem e)
                return e.Details;
            return "ここに説明が表示されます．";
        }
    
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}