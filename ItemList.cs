using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System;

namespace Wb.TaskManager.Data.Models
{
    [Serializable]
    [DebuggerDisplay("Count = {Count}, Capacity = {Capacity}, Free = {FreeCount}")]
    public class ItemList<T> : IList<T>, IList, IReadOnlyList<T>, IReadOnlyCollection<T>
    {
        #region Private/Protected Member Variables
        private const byte defaultCapacity = 5;
        private T[] items = Array.Empty<T>();
        private uint version = 1;
        private int count = 0;
        #endregion

        #region Private/Protected Properties
        #endregion

        #region Private/Protected Methods
        private void ValidateIndex(int index, [CallerMemberName] string callerMember = "")
        {
            if (index < 0)
                throw new IndexOutOfRangeException($"List action { callerMember }( { index } ) could not finish. Index should be greater than zero.");

            if (index > count)
                throw new IndexOutOfRangeException($"List action { callerMember }( { index } ) could not finish. Index should be smaller than items count.");
        }
        private T CastValue(object value, [CallerMemberName] string callerMember = "")
        {
            if (!(value is T))
                throw new InvalidCastException($"List action { callerMember }( ) could not finish. Object value can not be converted to '{ nameof(T) }' type.");

            return (T) value;
        }

        private void DefinitionInsertRange(int index, ICollection<T> collection)
        {
            int collectionCount = collection.Count;
            EnsureCapacity(collectionCount);

            if (index < count)
                Array.Copy(items, index, items, (index + collectionCount), (count - index));

            T[] itemsToInsert = new T[collectionCount];
            collection.CopyTo(itemsToInsert, 0);

            itemsToInsert.CopyTo(items, index);

            count += collectionCount;
            version++;
        }
        private void EnsureCapacity(int minFreeCount = 1)
        {
            if (FreeCount < minFreeCount)
            {
                int estimatedMinCapacity = count + minFreeCount,
                    newCapacity = (items.Length * 3) >> 1;

                if (newCapacity < estimatedMinCapacity)
                    newCapacity = estimatedMinCapacity + defaultCapacity;

                Capacity = newCapacity;
            }
        }
        #endregion

        #region Constructors
        public ItemList()
        {
            items = new T[defaultCapacity];
        }
        public ItemList(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException("capacity", "Capacity of list should be greater tha zero.");

            items = new T[capacity];
        }
        public ItemList(ICollection<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException("collection", "Initial collection of list can not be null.");

            if (collection.Count > 0)
            {
                items = new T[collection.Count];
                DefinitionInsertRange(count, collection);
            }
            else
            {
                items = new T[defaultCapacity];
            }

        }
        #endregion

        #region Indexer
        public T this[int index]
        {
            get
            {
                ValidateIndex(index);
                return items[index];
            }

            set
            {
                ValidateIndex(index);
                items[index] = value;
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
            get { return items.Length - count; }
        }
        public int Capacity
        {
            get { return items.Length; }

            private set
            {
                T[] relocatedItems = new T[value];
                Array.Copy(items, 0, relocatedItems, 0, count);

                items = relocatedItems;
            }
        }
        public int Count
        {
            get { return count; }
        }
        #endregion

        #region Public Methods
        public void Insert(int index, T item)
        {
            ValidateIndex(index);
            EnsureCapacity();

            if (index < count)
                Array.Copy(items, index, items, (index + 1), (count - index));

            items[index] = item;
            version++;
            count++;
        }
        public void RemoveAt(int index)
        {
            ValidateIndex(index);

            if (index == count)
                throw new IndexOutOfRangeException($"List action RemoveAt( { index } ) could not finish. Index can not be greater or equal than items count.");

            count--;

            if (index < count)
                Array.Copy(items, (index + 1), items, index, (count - index));

            items[count] = default(T);
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
            items[count++] = item;
            version++;
        }
        public void Clear()
        {
            if (count > 0)
            {
                Array.Clear(items, 0, count);
                count = 0;
                version++;
            }
        }

        public bool Contains(T item)
        {
            var comparer = EqualityComparer<T>.Default;

            for (int index = 0; index < count; index++)
                if (comparer.Equals(items[index], item))
                    return true;

            return false;
        }
        public int IndexOf(T item)
        {
            return Array.IndexOf(items, item, 0, count);
        }

        public void InsertRange(int index, ICollection<T> collection)
        {
            ValidateIndex(index);

            if (collection == null)
                throw new ArgumentNullException("collection", "Collection to add must be diferent than null.");

            else if (collection.Count == 0)
                throw new ArgumentOutOfRangeException("collection", "Collection count must be greater than zero.");

            DefinitionInsertRange(index, collection);
        }
        public void AddRange(ICollection<T> elements)
        {
            InsertRange(count, elements);
        }

        public ItemList<T> GetRange(int index, int itemsCount)
        {
            ValidateIndex(index);

            if (itemsCount < 0)
                throw new IndexOutOfRangeException("Items count should be greater than zero.");

            if (itemsCount > (count - index))
                throw new IndexOutOfRangeException("New items count should be smaller than actual items count.");

            T[] newItems = new T[itemsCount];
            Array.Copy(items, index, newItems, 0, itemsCount);

            return new ItemList<T>(newItems);
        }
        public void CopyTo(T[] array, int arrayIndex)
        {
            Array.Copy(items, 0, array, arrayIndex, count);
        }
        public void CopyTo(T[] array)
        {
            CopyTo(array, 0);
        }
        public T[] ToArray()
        {
            T[] array = new T[count];
            CopyTo(array, 0);
            return array;
        }

        public sealed override string ToString()
        {
            switch (count)
            {
                case 0:
                    return "[]";
                case 1:
                    return $"[{ items[0] }]";
                default:
                    return $"[{ string.Join(",", items) }]";
            }
        }
        public IEnumerator<T> GetEnumerator()
        {
            return new CoreEnumerator(this);
        }
        public void TrimExcess()
        {
            if (FreeCount > defaultCapacity)
                Capacity = count + defaultCapacity;
        }
        #endregion

        #region Internal IEnumerator
        IEnumerator IEnumerable.GetEnumerator()
        {
            return items.GetEnumerator();
        }
        #endregion

        #region Non Generic List
        public bool IsFixedSize
        {
            get { return false; }
        }
        public object SyncRoot
        {
            get { return null; }
        }
        public bool IsSynchronized
        {
            get { return false; }
        }

        object IList.this[int index]
        {
            get { return this[index]; }
            set { this[index] = CastValue(value); }
        }
        public int Add(object value)
        {
            int index = count;
            Add(CastValue(value));
            return index;
        }
        public bool Contains(object value)
        {
            return Contains(CastValue(value));
        }
        public int IndexOf(object value)
        {
            return IndexOf(CastValue(value));
        }
        public void Insert(int index, object value)
        {
            Insert(index, CastValue(value));
        }
        public void Remove(object value)
        {
            Remove(CastValue(value));
        }
        public void CopyTo(Array array, int index)
        {
            CopyTo((T[]) array, index);
        }
        #endregion

        #region Publict Struct
        [Serializable]
        public struct CoreEnumerator : IEnumerator<T>, IEnumerator
        {
            #region Private/Protected Member Variables
            private readonly ItemList<T> list;
            private uint version, index;
            private T current;
            #endregion

            #region Private/Protected Methods
            public bool MoveNext()
            {
                ItemList<T> localList = list;

                if (version == localList.version && (index < (uint) localList.Count))
                {
                    current = localList.items[index];
                    index++;

                    return true;
                }

                return MoveNextRare();
            }
            public T Current
            {
                get { return current; }
            }
            #endregion

            #region Constructors
            internal CoreEnumerator(ItemList<T> list)
            {
                this.list = list;

                index = 0;
                version = list.version;

                current = default(T);
            }
            #endregion

            #region Public Properties
            public void Dispose() { }
            #endregion

            #region Public Methods
            private bool MoveNextRare()
            {
                if (version != list.version)
                    throw new InvalidOperationException();

                index = (uint) (list.count + 1);
                current = default(T);
                return false;
            }
            #endregion

            #region IEnumerator Implementation
            object IEnumerator.Current
            {
                get
                {
                    if (index == 0 || index > list.count)
                        throw new IndexOutOfRangeException();

                    return Current;
                }
            }
            void IEnumerator.Reset()
            {
                if (version != list.version)
                    throw new InvalidOperationException();

                index = 0;
                current = default(T);
            }
            #endregion
        }
        #endregion
    }
}
