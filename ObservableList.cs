using System.Collections.Specialized;
using System.ComponentModel;
using System.Collections;
using System;

namespace UList
{
    public class ObservableList<TEntity> : OptimizedList<TEntity>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        #region Private/Protected Member Variables
        private readonly SimpleMonitor monitor = new SimpleMonitor();
        private const string CountPropertyName = "Count";
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
        public ObservableList() : base() { }
        public ObservableList(int capacity) : base(capacity) { }
        public ObservableList(TEntity[] collection) : base(collection) { }
        #endregion

        #region Public Properties
        #endregion

        #region Public Methods
        public override void Insert(int index, TEntity item)
        {
            CheckReentrancy();
            base.Insert(index, item);
            RaisePropertyChanged(CountPropertyName);
            RaiseCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
        }
        public override void RemoveAt(int index)
        {
            TEntity item = this[index];
            base.RemoveAt(index);

            RaisePropertyChanged(CountPropertyName);
            RaiseCollectionChanged(NotifyCollectionChangedAction.Remove, item, index);
        }
        public override bool Remove(TEntity item)
        {
            CheckReentrancy();
            int itemIndex = IndexOf(item);

            if (itemIndex >= 0)
            {
                RemoveAt(itemIndex);

                RaisePropertyChanged(CountPropertyName);
                RaiseCollectionChanged(NotifyCollectionChangedAction.Remove, item, itemIndex);
                return true;
            }

            return false;
        }
        public override void Add(TEntity item)
        {
            CheckReentrancy();
            base.Add(item);
            RaisePropertyChanged(CountPropertyName);
            RaiseCollectionChanged(NotifyCollectionChangedAction.Add, item, (Count - 1));
        }
        public override void Clear()
        {
            CheckReentrancy();
            base.Clear();

            RaisePropertyChanged(CountPropertyName);
            RaiseCollectionReset();
        }

        public override void AddRange(TEntity[] collection)
        {
            if (collection != null && collection.Length > 0)
            {
                CheckReentrancy();
                int index = Count;

                base.AddRange(collection);

                RaisePropertyChanged(CountPropertyName);
                RaiseCollectionRangeChanged(NotifyCollectionChangedAction.Add, collection, index);
            }
        }
        #endregion

        #region Public Events
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        private void RaiseCollectionChanged(NotifyCollectionChangedAction action, object item, int index)
        {
            using (BlockReentrancy())
            {
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, item, index));
            }
        }
        private void RaiseCollectionRangeChanged(NotifyCollectionChangedAction action, IList items, int index)
        {
            using (BlockReentrancy())
            {
                //CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, items, index));
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }
        private void RaiseCollectionChanged(NotifyCollectionChangedAction action, object item, int index, int oldIndex)
        {
            using (BlockReentrancy())
            {
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, item, index, oldIndex));
            }
        }
        private void RaiseCollectionChanged(NotifyCollectionChangedAction action, object oldItem, object newItem, int index)
        {
            using (BlockReentrancy())
            {
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index));
            }
        }
        private void RaiseCollectionReset()
        {
            using (BlockReentrancy())
            {
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Non Generic
        public override void Insert(int index, object value)
        {
            Insert(index, CastValue(value));
        }
        public override void Remove(object value)
        {
            Remove(CastValue(value));
        }
        public override int Add(object value)
        {
            int index = Count;
            Add(CastValue(value));
            return index;
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
