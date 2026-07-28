﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axl.Base.Models
{
    public class VariableItem : System.ComponentModel.INotifyPropertyChanged, IDisposable
    {
        private string _key;
        public string Key
        {
            get => _key;
            set { _key = value; OnPropertyChanged(nameof(Key)); }
        }

        private byte[] _valueBytes;
        public string Value
        {
            get => _valueBytes != null ? Encoding.UTF8.GetString(_valueBytes) : null;
            set
            {
                if (value == null) _valueBytes = null;
                else _valueBytes = Encoding.UTF8.GetBytes(value);
                OnPropertyChanged(nameof(Value));
            }
        }

        public void Dispose()
        {
            if (_valueBytes != null)
            {
                Array.Clear(_valueBytes, 0, _valueBytes.Length);
                _valueBytes = null;
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string prop) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(prop));
    }
}

