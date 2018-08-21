using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Collections;
using System.Threading;
using System;

namespace Mx.CustomList
{
    [Serializable]
    public class OptimizedList<TEntity> : IList<TEntity>, IList, IReadOnlyList<TEntity>
    {
        #region Private/Protected Member Variables
        private TEntity[] itemsSource = Array.Empty<TEntity>();
        private const byte defaultCapacity = 5;
        private uint version = 1;
        private object syncRoot;
        #endregion

        #region Private/Protected Properties
        #endregion

        #region Private/Protected Methods
        protected void ValidateIndex(int index, [CallerMemberName] string callerMember = "")
        {
            if (index < 0)
                throw new IndexOutOfRangeException($"List action { callerMember }( { index } ) could not finish. Index should be greater than zero.");

            if (index > Count)
                throw new IndexOutOfRangeException($"List action { callerMember }( { index } ) could not finish. Index should be smaller than items count.");
        }
        protected TEntity CastValue(object value, [CallerMemberName] string callerMember = "")
        {
            if (!(value is TEntity))
                throw new InvalidCastException($"List action { callerMember }( ) could not finish. Object value can not be converted to '{ nameof(TEntity) }' type.");

            return (TEntity) value;
        }

        private void DefinitionInsertRange(int index, TEntity[] collection)
        {
            int collectionCount = collection.Length;
            EnsureCapacity(collectionCount);

            if (index < Count)
                Array.Copy(itemsSource, index, itemsSource, (index + collectionCount), (Count - index));

            collection.CopyTo(itemsSource, index);

            Count += collectionCount;
            version++;
        }
        private void EnsureCapacity(int minFreeCount = 1)
        {
            if (FreeCount < minFreeCount)
            {
                int estimatedMinCapacity = Count + minFreeCount,
                    newCapacity = (Capacity * 3) >> 1;

                if (newCapacity < estimatedMinCapacity)
                    newCapacity = estimatedMinCapacity + defaultCapacity;

                Capacity = newCapacity;
            }
        }
        #endregion

        #region Constructors
        public OptimizedList()
        {
            itemsSource = new TEntity[defaultCapacity];
        }
        public OptimizedList(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException("capacity", "Capacity of list should be greater tha zero.");

            int initialCapacity = Math.Max(capacity, defaultCapacity);

            itemsSource = new TEntity[initialCapacity];
        }
        public OptimizedList(TEntity[] collection)
        {
            if (collection == null)
                throw new ArgumentNullException("collection", "Initial collection of list can not be null.");

            int initialCapacity = Math.Max(collection.Length, defaultCapacity);

            itemsSource = new TEntity[initialCapacity];

            if (collection.Length > 0)
                DefinitionInsertRange(Count, collection);
        }
        #endregion

        #region Indexer
        public TEntity this[int index]
        {
            get
            {
                ValidateIndex(index);
                return itemsSource[index];
            }

            set
            {
                ValidateIndex(index);
                itemsSource[index] = value;
            }
        }
        #endregion

        #region Public Properties
        public bool IsSynchronized
        {
            get { return false; }
        }
        public bool IsFixedSize
        {
            get { return false; }
        }
        public object SyncRoot
        {
            get
            {
                if (syncRoot == null)
                    Interlocked.CompareExchange(ref syncRoot, new object(), null);

                return syncRoot;
            }
        }
        public bool IsReadOnly
        {
            get { return false; }
        }
        public int FreeCount
        {
            get { return Capacity - Count; }
        }
        public int Capacity
        {
            get { return itemsSource.Length; }

            private set
            {
                var relocatedItems = new TEntity[value];
                Array.Copy(itemsSource, 0, relocatedItems, 0, Count);

                itemsSource = relocatedItems;
            }
        }
        public int Count
        {
            get; private set;
        }
        #endregion

        #region Public Methods
        public virtual void Insert(int index, TEntity item)
        {
            ValidateIndex(index);
            EnsureCapacity();

            if (index < Count)
                Array.Copy(itemsSource, index, itemsSource, (index + 1), (Count - index));

            itemsSource[index] = item;
            version++;
            Count++;
        }
        public virtual void RemoveAt(int index)
        {
            ValidateIndex(index);

            if (index == Count)
                throw new IndexOutOfRangeException($"List action RemoveAt( { index } ) could not finish. Index can not be greater or equal than items count.");

            if (index < (--Count))
                Array.Copy(itemsSource, (index + 1), itemsSource, index, (Count - index));

            itemsSource[Count] = default(TEntity);
            version++;
        }
        public virtual bool Remove(TEntity item)
        {
            int itemIndex = IndexOf(item);
            bool found = itemIndex >= 0;

            if (found)
                RemoveAt(itemIndex);

            return found;
        }
        public virtual void Add(TEntity item)
        {
            EnsureCapacity();
            itemsSource[Count++] = item;
            version++;
        }
        public virtual void Clear()
        {
            if (Count > 0)
            {
                Array.Clear(itemsSource, 0, Count);
                Count = 0;
                version++;
            }
        }

        public bool Contains(TEntity item)
        {
            var comparer = EqualityComparer<TEntity>.Default;

            for (int index = 0; index < Count; index++)
                if (comparer.Equals(itemsSource[index], item))
                    return true;

            return false;
        }
        public int IndexOf(TEntity item)
        {
            return Array.IndexOf(itemsSource, item, 0, Count);
        }

        public virtual void InsertRange(int index, TEntity[] collection)
        {
            ValidateIndex(index);

            if (collection == null)
                throw new ArgumentNullException("collection", "Collection to add must be diferent than null.");

            if (collection.Length != 0)
                DefinitionInsertRange(index, collection);
        }
        public virtual void AddRange(TEntity[] elements)
        {
            InsertRange(Count, elements);
        }

        public void ForEach(Action<TEntity> action)
        {
            if (action == null)
                throw new ArgumentNullException("action", "Action to execute can not be null.");

            for (int i = 0; i < Count; i++)
                action(itemsSource[i]);
        }
        public TEntity[] FindAll(Func<TEntity, bool> match)
        {
            if (match == null)
                throw new ArgumentNullException("match", "Match item predicate can not be null.");

            var list = new OptimizedList<TEntity>();
            var item = default(TEntity);

            for (int i = 0; i < Count; i++)
            {
                item = itemsSource[i];

                if (match(item))
                    list.Add(item);
            }

            return list.ToArray();
        }
        public TEntity Find(Func<TEntity, bool> match)
        {
            if (match == null)
                throw new ArgumentNullException("match", "Match item predicate can not be null.");

            var item = default(TEntity);

            for (int i = 0; i < Count; i++)
            {
                item = itemsSource[i];

                if (match(item))
                    return item;
            }

            return default(TEntity);
        }

        public OptimizedList<TEntity> GetRange(int index, int itemsCount)
        {
            ValidateIndex(index);

            if (itemsCount < 0)
                throw new IndexOutOfRangeException("Items count should be greater than zero.");

            if (itemsCount > (Count - index))
                throw new IndexOutOfRangeException("New items count should be smaller than actual items count.");

            var newItems = new TEntity[itemsCount];
            Array.Copy(itemsSource, index, newItems, 0, itemsCount);

            return new OptimizedList<TEntity>(newItems);
        }
        public void CopyTo(TEntity[] array, int arrayIndex = 0)
        {
            Array.Copy(itemsSource, 0, array, arrayIndex, Count);
        }
        public TEntity[] ToArray()
        {
            var array = Array.Empty<TEntity>();

            if (Count > 0)
            {
                array = new TEntity[Count];
                CopyTo(array);
            }

            return array;
        }

        public IEnumerator<TEntity> GetEnumerator()
        {
            return new CoreEnumerator(this);
        }
        public void CopyTo(Array array, int index)
        {
            CopyTo((TEntity[]) array, index);
        }
        public override string ToString()
        {
            return $"Count = { Count }, Capacity = { Capacity }, Free = { FreeCount }";
        }
        public void TrimExcess()
        {
            if (FreeCount > defaultCapacity)
                Capacity = Count + defaultCapacity;
        }
        public string Print()
        {
            switch (Count)
            {
                case 0:
                    return "[]";

                case 1:
                    return $"[{ itemsSource[0] }]";

                default:
                    return $"[{ string.Join(", ", itemsSource) }]";
            }
        }
        #endregion

        #region Non Generic List
        object IList.this[int index]
        {
            get { return this[index]; }
            set { this[index] = CastValue(value); }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public virtual void Insert(int index, object value)
        {
            Insert(index, CastValue(value));
        }
        public virtual void Remove(object value)
        {
            Remove(CastValue(value));
        }
        public virtual int Add(object value)
        {
            int index = Count;
            Add(CastValue(value));
            return index;
        }

        public virtual bool Contains(object value)
        {
            return Contains(CastValue(value));
        }
        public virtual int IndexOf(object value)
        {
            return IndexOf(CastValue(value));
        }
        #endregion

        #region SynchronizedList
        [Serializable]
        internal class SynchronizedList : IList<TEntity>
        {
            private readonly OptimizedList<TEntity> list;
            private readonly object root;

            internal SynchronizedList(OptimizedList<TEntity> list)
            {
                this.list = list;
                root = list.SyncRoot;
            }

            public int Count
            {
                get
                {
                    lock (root)
                    {
                        return list.Count;
                    }
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    return list.IsReadOnly;
                }
            }

            public void Add(TEntity item)
            {
                lock (root)
                {
                    list.Add(item);
                }
            }

            public void Clear()
            {
                lock (root)
                {
                    list.Clear();
                }
            }

            public bool Contains(TEntity item)
            {
                lock (root)
                {
                    return list.Contains(item);
                }
            }

            public void CopyTo(TEntity[] array, int arrayIndex)
            {
                lock (root)
                {
                    list.CopyTo(array, arrayIndex);
                }
            }

            public bool Remove(TEntity item)
            {
                lock (root)
                {
                    return list.Remove(item);
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                lock (root)
                {
                    return list.GetEnumerator();
                }
            }

            IEnumerator<TEntity> IEnumerable<TEntity>.GetEnumerator()
            {
                lock (root)
                {
                    return list.GetEnumerator();
                }
            }

            public TEntity this[int index]
            {
                get
                {
                    lock (root)
                    {
                        return list[index];
                    }
                }
                set
                {
                    lock (root)
                    {
                        list[index] = value;
                    }
                }
            }

            public int IndexOf(TEntity item)
            {
                lock (root)
                {
                    return list.IndexOf(item);
                }
            }

            public void Insert(int index, TEntity item)
            {
                lock (root)
                {
                    list.Insert(index, item);
                }
            }

            public void RemoveAt(int index)
            {
                lock (root)
                {
                    list.RemoveAt(index);
                }
            }
        }
        #endregion

        #region Publict Struct
        [Serializable]
        public struct CoreEnumerator : IEnumerator<TEntity>, IEnumerator
        {
            #region Private/Protected Member Variables
            private readonly OptimizedList<TEntity> list;
            private readonly uint version;
            private int index;
            #endregion

            #region Private/Protected Methods
            #endregion

            #region Constructors
            internal CoreEnumerator(OptimizedList<TEntity> list)
            {
                this.list = list;

                version = list.version;
                index = 0;

                Current = default(TEntity);
            }
            #endregion

            #region Public Properties
            public TEntity Current
            {
                get; private set;
            }
            #endregion

            #region Public Methods
            private bool MoveNextRare()
            {
                if (version != list.version)
                    throw new InvalidOperationException();

                index = list.Count + 1;
                Current = default(TEntity);
                return false;
            }
            public bool MoveNext()
            {
                OptimizedList<TEntity> localList = list;

                if (version == localList.version && (index < localList.Count))
                {
                    Current = localList.itemsSource[index];
                    index++;

                    return true;
                }

                return MoveNextRare();
            }
            public void Dispose()
            {
                Current = default(TEntity);
            }
            #endregion

            #region IEnumerator Implementation
            object IEnumerator.Current
            {
                get
                {
                    if (index == 0 || index > list.Count)
                        throw new IndexOutOfRangeException();

                    return Current;
                }
            }
            void IEnumerator.Reset()
            {
                if (version != list.version)
                    throw new InvalidOperationException();

                Current = default(TEntity);
                index = 0;
            }
            #endregion
        }
        #endregion
    }
}
