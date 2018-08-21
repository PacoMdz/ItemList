using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.ComponentModel;
using System;

namespace UNotifyProperties
{
   public abstract class NotifyPropertyChanged : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RisePropertyChanged(params string[] properties)
        {
            if (properties != null && properties.Length > 0)
            {
                foreach (string propertyName in properties)
                    if (!string.IsNullOrWhiteSpace(propertyName))
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        protected void RisePropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (!string.IsNullOrWhiteSpace(propertyName))
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T property, T value, [CallerMemberName] string propertyName = "")
        {
            if (!(string.IsNullOrWhiteSpace(propertyName) || EqualityComparer<T>.Default.Equals(property, value)))
            {
                property = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                return true;
            }
            return false;
        }

        protected bool SetProperty<T>(ref T property, T value, Action callback, [CallerMemberName] string propertyName = "")
        {
            bool propertyChanged = SetProperty(ref property, value, propertyName);

            if (propertyChanged)
                callback?.Invoke();

            return propertyChanged;
        }

        protected T GetProperty<T>([CallerMemberName] string propertyName = "")
        {
            return string.IsNullOrWhiteSpace(propertyName) ?
                default(T) : (T) GetType().GetProperty(propertyName)?.GetValue(this, null);
        }
    }
}
