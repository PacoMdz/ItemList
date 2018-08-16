using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.ComponentModel;

namespace UNotifyProperties
{
    public abstract class NotifyPropertiesData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RisePropertyChanged(params string[] properties)
        {
            if (properties != null && properties.Length > 0)
            {
                foreach (string prop in properties)
                {
                    if (!string.IsNullOrWhiteSpace(prop))
                    {
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
                    }
                }
            }
        }
        protected void RisePropertyChanged([CallerMemberName] string prop = "")
        {
            if (!string.IsNullOrWhiteSpace(prop))
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string prop = "")
        {
            if (!string.IsNullOrWhiteSpace(prop) && !EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
                return true;
            }
            return false;
        }
        protected T GetProperty<T>([CallerMemberName] string prop = "")
        {
            return string.IsNullOrWhiteSpace(prop) ?
                default(T) : (T)GetType().GetProperty(prop)?.GetValue(this, null);
        }
    }
}
