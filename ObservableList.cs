using System.Collections.Specialized;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections;
using System;

namespace Wb.TaskManager.Data.Models
{
    public class ObservableList<T> : IList<T>, IReadOnlyList<T>, IReadOnlyCollection<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        #region Private/Protected Member Variables
        private readonly SimpleMonitor monitor = new SimpleMonitor();
        private const string CountProperty = "Count";
        private readonly ItemList<T> items;
        #endregion

        #region Private/Protected Properties
        #endregion

        #region Private/Protected Methods
        protected IDisposable BlockReentrancy()
        {
            monitor.Enter();
            return monitor;
        }
        protected void CheckReentrancy()
        {
            if (monitor.Busy && CollectionChanged != null && (CollectionChanged.GetInvocationList().Length) > 1)
                throw new InvalidOperationException("ObservableCollectionReentrancyNotAllowed");
        }
        #endregion

        #region Constructor
        public ObservableList()
        {
            items = new ItemList<T>();
        }
        public ObservableList(int capacity)
        {
            items = new ItemList<T>(capacity);
        }
        public ObservableList(ICollection<T> collection)
        {
            items = new ItemList<T>(collection);
        }
        #endregion

        #region Indexer
        public T this[int index]
        {
            get { return items[index]; }
            set { items[index] = value; }
        }
        #endregion

        #region Public Properties
        public bool IsReadOnly
        {
            get { return items.IsReadOnly; }
        }
        public int Count
        {
            get { return items.Count; }
        }
        #endregion

        #region Public Methods
        public void AddRange(ICollection<T> collection)
        {
            if (collection != null && collection.Count > 0)
            {
                CheckReentrancy();
                int index = items.Count;

                items.AddRange(collection);

                OnPropertyChanged(CountProperty);

                var array = new T[collection.Count];
                collection.CopyTo(array, 0);

                OnCollectionRangeChanged(NotifyCollectionChangedAction.Add, array, index);
            }
        }
        public void Insert(int index, T item)
        {
            CheckReentrancy();
            items.Insert(index, item);
            OnPropertyChanged(CountProperty);
            OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
        }
        public void RemoveAt(int index)
        {
            T item = items[index];
            items.RemoveAt(index);

            OnPropertyChanged(CountProperty);
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, item, index);
        }
        public bool Remove(T item)
        {
            CheckReentrancy();
            int itemIndex = items.IndexOf(item);

            if (itemIndex >= 0)
            {
                items.RemoveAt(itemIndex);

                OnPropertyChanged(CountProperty);
                OnCollectionChanged(NotifyCollectionChangedAction.Remove, item, itemIndex);
                return true;
            }

            return false;
        }
        public void Add(T item)
        {
            CheckReentrancy();
            items.Add(item);
            OnPropertyChanged(CountProperty);
            OnCollectionChanged(NotifyCollectionChangedAction.Add, item, (items.Count - 1));
        }
        public void Clear()
        {
            CheckReentrancy();
            items.Clear();

            OnPropertyChanged(CountProperty);
            OnCollectionReset();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            items.CopyTo(array, arrayIndex);
        }
        public IEnumerator<T> GetEnumerator()
        {
            return items.GetEnumerator();
        }
        public bool Contains(T item)
        {
            return items.Contains(item);
        }
        public int IndexOf(T item)
        {
            return items.IndexOf(item);
        }
        #endregion

        #region Public Events
        public virtual event NotifyCollectionChangedEventHandler CollectionChanged;
        private void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index)
        {
            using (BlockReentrancy())
            {
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, item, index));
            }
        }
        private void OnCollectionRangeChanged(NotifyCollectionChangedAction action, IList items, int index)
        {
            using (BlockReentrancy())
            {
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, items, index));
            }
        }
        private void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index, int oldIndex)
        {
            using (BlockReentrancy())
            {
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, item, index, oldIndex));
            }
        }
        private void OnCollectionChanged(NotifyCollectionChangedAction action, object oldItem, object newItem, int index)
        {
            using (BlockReentrancy())
            {
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index));
            }
        }
        private void OnCollectionReset()
        {
            using (BlockReentrancy())
            {
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        protected virtual event PropertyChangedEventHandler PropertyChanged;
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { PropertyChanged += value; }
            remove { PropertyChanged -= value; }
        }
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Internal IEnumerator
        IEnumerator IEnumerable.GetEnumerator()
        {
            return items.GetEnumerator();
        }
        #endregion

        #region Private Class
        private class SimpleMonitor : IDisposable
        {
            private byte busyCount;

            public SimpleMonitor() { }

            public bool Busy => busyCount > 0;

            public void Dispose() => --busyCount;
            public void Enter() => ++busyCount;
        }
        #endregion
    }
}
