using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Collections;
using System.Threading;
using System;

namespace Mx.CustomList
{
    [Serializable]
    public class OptimizedList<T> : IList<T>, IList, IReadOnlyList<T>
    {
        #region Private/Protected Member Variables
        private T[] itemsSource = Array.Empty<T>();
        private const byte defaultCapacity = 5;
        private uint version = 1;
        private object syncRoot;
        #endregion

        #region Private/Protected Properties
        #endregion

        #region Private/Protected Methods
        private void ValidateIndex(int index, [CallerMemberName] string callerMember = "")
        {
            if (index < 0)
                throw new IndexOutOfRangeException($"List action { callerMember }( { index } ) could not finish. Index should be greater than zero.");

            if (index > Count)
                throw new IndexOutOfRangeException($"List action { callerMember }( { index } ) could not finish. Index should be smaller than items count.");
        }
        private T CastValue(object value, [CallerMemberName] string callerMember = "")
        {
            if (!(value is T))
                throw new InvalidCastException($"List action { callerMember }( ) could not finish. Object value can not be converted to '{ nameof(T) }' type.");

            return (T) value;
        }

        private void DefinitionInsertRange(int index, T[] collection)
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
            itemsSource = new T[defaultCapacity];
        }
        public OptimizedList(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException("capacity", "Capacity of list should be greater tha zero.");

            int initialCapacity = Math.Max(capacity, defaultCapacity);

            itemsSource = new T[initialCapacity];
        }
        public OptimizedList(T[] collection)
        {
            if (collection == null)
                throw new ArgumentNullException("collection", "Initial collection of list can not be null.");

            int initialCapacity = Math.Max(collection.Length, defaultCapacity);

            itemsSource = new T[initialCapacity];

            if (collection.Length > 0)
                DefinitionInsertRange(Count, collection);
        }
        #endregion

        #region Indexer
        public T this[int index]
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
                var relocatedItems = new T[value];
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
        public void Insert(int index, T item)
        {
            ValidateIndex(index);
            EnsureCapacity();

            if (index < Count)
                Array.Copy(itemsSource, index, itemsSource, (index + 1), (Count - index));

            itemsSource[index] = item;
            version++;
            Count++;
        }
        public void RemoveAt(int index)
        {
            ValidateIndex(index);

            if (index == Count)
                throw new IndexOutOfRangeException($"List action RemoveAt( { index } ) could not finish. Index can not be greater or equal than items count.");

            if (index < (--Count))
                Array.Copy(itemsSource, (index + 1), itemsSource, index, (Count - index));

            itemsSource[Count] = default(T);
            version++;
        }
        public bool Remove(T item)
        {
            int itemIndex = IndexOf(item);
            bool found = itemIndex >= 0;

            if (found)
                RemoveAt(itemIndex);

            return found;
        }
        public void Add(T item)
        {
            EnsureCapacity();
            itemsSource[Count++] = item;
            version++;
        }
        public void Clear()
        {
            if (Count > 0)
            {
                Array.Clear(itemsSource, 0, Count);
                Count = 0;
                version++;
            }
        }

        public bool Contains(T item)
        {
            var comparer = EqualityComparer<T>.Default;

            for (int index = 0; index < Count; index++)
                if (comparer.Equals(itemsSource[index], item))
                    return true;

            return false;
        }
        public int IndexOf(T item)
        {
            return Array.IndexOf(itemsSource, item, 0, Count);
        }

        public void InsertRange(int index, T[] collection)
        {
            ValidateIndex(index);

            if (collection == null)
                throw new ArgumentNullException("collection", "Collection to add must be diferent than null.");

            if (collection.Length != 0)
                DefinitionInsertRange(index, collection);
        }
        public void AddRange(T[] elements)
        {
            InsertRange(Count, elements);
        }

        public void ForEach(Action<T> action)
        {
            if (action == null)
                throw new ArgumentNullException("action", "Action to execute can not be null.");

            for (int i = 0; i < Count; i++)
                action(itemsSource[i]);
        }
        public T[] FindAll(Func<T, bool> match)
        {
            if (match == null)
                throw new ArgumentNullException("match", "Match item predicate can not be null.");

            var list = new OptimizedList<T>();
            var item = default(T);

            for (int i = 0; i < Count; i++)
            {
                item = itemsSource[i];

                if (match(item))
                    list.Add(item);
            }

            return list.ToArray();
        }
        public T Find(Func<T, bool> match)
        {
            if (match == null)
                throw new ArgumentNullException("match", "Match item predicate can not be null.");

            var item = default(T);

            for (int i = 0; i < Count; i++)
            {
                item = itemsSource[i];

                if (match(item))
                    return item;
            }

            return default(T);
        }

        public OptimizedList<T> GetRange(int index, int itemsCount)
        {
            ValidateIndex(index);

            if (itemsCount < 0)
                throw new IndexOutOfRangeException("Items count should be greater than zero.");

            if (itemsCount > (Count - index))
                throw new IndexOutOfRangeException("New items count should be smaller than actual items count.");

            var newItems = new T[itemsCount];
            Array.Copy(itemsSource, index, newItems, 0, itemsCount);

            return new OptimizedList<T>(newItems);
        }
        public void CopyTo(T[] array, int arrayIndex = 0)
        {
            Array.Copy(itemsSource, 0, array, arrayIndex, Count);
        }
        public T[] ToArray()
        {
            var array = Array.Empty<T>();

            if (Count > 0)
            {
                array = new T[Count];
                CopyTo(array);
            }

            return array;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new CoreEnumerator(this);
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

        #region Internal IEnumerator
        IEnumerator IEnumerable.GetEnumerator()
        {
            return itemsSource.GetEnumerator();
        }
        #endregion

        #region Non Generic List
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

        object IList.this[int index]
        {
            get { return this[index]; }
            set { this[index] = CastValue(value); }
        }

        public void Insert(int index, object value)
        {
            Insert(index, CastValue(value));
        }
        public void Remove(object value)
        {
            Remove(CastValue(value));
        }
        public int Add(object value)
        {
            int index = Count;
            Add(CastValue(value));
            return index;
        }

        public void CopyTo(Array array, int index = 0)
        {
            CopyTo((T[]) array, index);
        }
        public bool Contains(object value)
        {
            return Contains(CastValue(value));
        }
        public int IndexOf(object value)
        {
            return IndexOf(CastValue(value));
        }
        #endregion

        #region SynchronizedList
        [Serializable]
        internal class SynchronizedList : IList<T>
        {
            private readonly OptimizedList<T> list;
            private readonly object root;

            internal SynchronizedList(OptimizedList<T> list)
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

            public void Add(T item)
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

            public bool Contains(T item)
            {
                lock (root)
                {
                    return list.Contains(item);
                }
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                lock (root)
                {
                    list.CopyTo(array, arrayIndex);
                }
            }

            public bool Remove(T item)
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

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                lock (root)
                {
                    return list.GetEnumerator();
                }
            }

            public T this[int index]
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

            public int IndexOf(T item)
            {
                lock (root)
                {
                    return list.IndexOf(item);
                }
            }

            public void Insert(int index, T item)
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
        public struct CoreEnumerator : IEnumerator<T>, IEnumerator
        {
            #region Private/Protected Member Variables
            private readonly OptimizedList<T> list;
            private readonly uint version;
            private int index;
            #endregion

            #region Private/Protected Methods
            #endregion

            #region Constructors
            internal CoreEnumerator(OptimizedList<T> list)
            {
                this.list = list;

                version = list.version;
                index = 0;

                Current = default(T);
            }
            #endregion

            #region Public Properties
            public T Current
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
                Current = default(T);
                return false;
            }
            public bool MoveNext()
            {
                OptimizedList<T> localList = list;

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
                Current = default(T);
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

                Current = default(T);
                index = 0;
            }
            #endregion
        }
        #endregion
    }
}
