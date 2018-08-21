using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System;

namespace Mx.CustomNotifyProperties
{
   #region Public Events
        /// <summary>
        /// Notify all subscribers changing property values.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region On Property Changed
        protected void RaisePropertiesChanged(params string[] propertiesNames)
        {
            int propertiesCount, index = 0;

            if (propertiesNames == null)
            {
                PropertyInfo[] properties = GetType().GetProperties();
                propertiesCount = properties.Length;

                if (propertiesCount != 0)
                {
                    propertiesNames = new string[propertiesCount];

                    for (index = 0; index < propertiesCount; index++)
                        propertiesNames[index] = properties[index].Name;
                }
            }

            propertiesCount = propertiesNames.Length;

            if (propertiesCount != 0)
            {
                string propertyName = string.Empty;

                for (index = 0; index < propertiesCount; index++)
                {
                    propertyName = propertiesNames[index];

                    if (!string.IsNullOrWhiteSpace(propertyName))
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                }
            }
        }
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (!string.IsNullOrWhiteSpace(propertyName))
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Get and Set Property Value
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (!(string.IsNullOrWhiteSpace(propertyName) || EqualityComparer<T>.Default.Equals(field, value)))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                return true;
            }
            return false;
        }
        protected bool SetProperty<T>(ref T field, T value, Action callback, [CallerMemberName] string propertyName = "")
        {
            bool propertyChanged = SetProperty(ref field, value, propertyName);

            if (propertyChanged)
                callback?.Invoke();

            return propertyChanged;
        }
        protected bool SetProperty<T>(ref T field, T value, Action<string> callback, [CallerMemberName] string propertyName = "")
        {
            bool propertyChanged = SetProperty(ref field, value, propertyName);

            if (propertyChanged)
                callback?.Invoke(propertyName);

            return propertyChanged;
        }

        protected T GetProperty<T>([CallerMemberName] string propertyName = "")
        {
            return string.IsNullOrWhiteSpace(propertyName) ?
                default(T) : (T) GetType().GetProperty(propertyName)?.GetValue(this, null);
        }
        #endregion
    }
}
