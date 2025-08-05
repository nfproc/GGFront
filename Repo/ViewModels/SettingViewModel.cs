// GGFront: A GHDL/GTKWave GUI Frontend
// Copyright (C) 2018-2025 Naoki FUJIEDA. New BSD License is applied.
//**********************************************************************

using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace GGFront.ViewModels
{
    // ■■ SettingWindow のバインディングの解決用
    public class SettingViewModel : INotifyPropertyChanged
    {
        // ■ プロパティ
        // TextBox txtGHDLPath
        private string _ghdlPath = "";
        public string GHDLPath
        {
            get => _ghdlPath;
            set
            {
                if (_ghdlPath == value)
                    return;
                _ghdlPath = value;
                OnPropertyChanged("GHDLPath");
            }
        }

        // TextBox txtGTKWavePath
        private string _gtkWavePath = "";
        public string GTKWavePath
        {
            get => _gtkWavePath;
            set
            {
                if (_gtkWavePath == value)
                    return;
                _gtkWavePath = value;
                OnPropertyChanged("GTKWavePath");
            }
        }

        // CheckBox chkGuessGHDLPath
        private bool _guessGHDLPath;
        public bool GuessGHDLPath
        {
            get => _guessGHDLPath;
            set
            {
                if (_guessGHDLPath == value)
                    return;
                _guessGHDLPath = value;
                OnPropertyChanged("GuessGHDLPath");
            }
        }

        // CheckBox chkGuessGTKWavePath
        private bool _guessGTKWavePath;
        public bool GuessGTKWavePath
        {
            get => _guessGTKWavePath;
            set
            {
                if (_guessGTKWavePath == value)
                    return;
                _guessGTKWavePath = value;
                OnPropertyChanged("GuessGTKWavePath");
            }
        }

        // RadioButton rdoVHDLVersion*
        private int _vhdlStd = 0;
        public int VHDLStd
        {
            get => _vhdlStd;
            set
            {
                if (_vhdlStd == value)
                    return;
                _vhdlStd = value;
                OnPropertyChanged("VHDLStd");
            }
        }
        
        // ■ イベントハンドラ
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}

namespace GGFront.ViewModels.SettingConverter
{
    // ■■ コンバータクラス
    // rdoVHDLVersion* のコンバータクラス
    public class VHDLVersionConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return (int.Parse((string) (parameter ?? "0")) == (int) (value ?? 0));
        }
    
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return (bool) (value ?? false) ? parameter : BindingOperations.DoNothing;
        }
    }
}
