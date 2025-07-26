// GGFront: A GHDL/GTKWave GUI Frontend
// Copyright (C) 2018-2025 Naoki FUJIEDA. New BSD License is applied.
//**********************************************************************

using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace GGFront
{
    // ■■ SettingWindow のバインディングの解決用
    class SettingViewModel : INotifyPropertyChanged
    {
        private string _ghdlPath;
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

        private string _gtkWavePath;
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }

    public class VHDLVersionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int.Parse((string) parameter) == (int) value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool) value ? parameter : Binding.DoNothing;
        }
    }
}
