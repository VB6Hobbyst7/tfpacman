﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace TFlex.PackageManager.Common
{
    /// <summary>
    /// Provides a thread-safe dictionary for use with data binding.
    /// </summary>
    /// <typeparam name="TKey">
    /// Specifies the type of the keys in this collection.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// Specifies the type of the values in this collection.
    /// </typeparam>
    public class ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private const string countString = "Count";
        private const string indexerName = "Item[]";
        private const string keysName    = "Keys";
        private const string valuesName  = "Values";
        private IDictionary<TKey, TValue> _dictionary;

        protected IDictionary<TKey, TValue> Dictionary
        {
            get { return (_dictionary); }
        }

        #region Constructors
        public ObservableDictionary()
        {
            _dictionary = new Dictionary<TKey, TValue>();
        }

        public ObservableDictionary(IDictionary<TKey, TValue> dictionary)
        {
            _dictionary = new Dictionary<TKey, TValue>(dictionary);
        }

        public ObservableDictionary(IEqualityComparer<TKey> comparer)
        {
            _dictionary = new Dictionary<TKey, TValue>(comparer);
        }

        public ObservableDictionary(int capacity)
        {
            _dictionary = new Dictionary<TKey, TValue>(capacity);
        }

        public ObservableDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
        {
            _dictionary = new Dictionary<TKey, TValue>(dictionary, comparer);
        }

        public ObservableDictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            _dictionary = new Dictionary<TKey, TValue>(capacity, comparer);
        }
        #endregion

        #region IDictionary<TKey,TValue> Members
        /// <summary>
        /// Adds a key/value pair to collection.
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="value">value</param>
        public void Add(TKey key, TValue value)
        {
            Insert(key, value, true);
        }

        public bool ContainsKey(TKey key)
        {
            return Dictionary.ContainsKey(key);
        }

        public ICollection<TKey> Keys
        {
            get { return Dictionary.Keys; }
        }

        public bool Remove(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            Dictionary.TryGetValue(key, out TValue value);
            var removed = Dictionary.Remove(key);
            if (removed)
                OnCollectionChanged(NotifyCollectionChangedAction.Remove, 
                    new KeyValuePair<TKey, TValue>(key, value));

            return removed;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return Dictionary.TryGetValue(key, out value);
        }

        public ICollection<TValue> Values
        {
            get { return Dictionary.Values; }
        }

        public TValue this[TKey key]
        {
            get
            {
                return Dictionary[key];
            }
            set
            {
                Insert(key, value, false);
            }
        }
        #endregion

        #region ICollection<KeyValuePair<TKey,TValue>> Members
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Insert(item.Key, item.Value, true);
        }

        public void Clear()
        {
            if (Dictionary.Count > 0)
            {
                Dictionary.Clear();
                OnCollectionChanged();
            }
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return Dictionary.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            Dictionary.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return Dictionary.Count; }
        }

        public bool IsReadOnly
        {
            get { return Dictionary.IsReadOnly; }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
        }
        #endregion

        #region IEnumerable<KeyValuePair<TKey,TValue>> Members
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return Dictionary.GetEnumerator();
        }
        #endregion

        #region IEnumerable Members
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Dictionary).GetEnumerator();
        }
        #endregion

        #region INotifyCollectionChanged Members
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        #endregion

        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        public void RenameKey(TKey oldKey, TKey newKey)
        {
            TValue value = Dictionary[oldKey];
            Insert(newKey, value, true);
            Remove(oldKey);
        }

        public void AddRange(IDictionary<TKey, TValue> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            if (items.Count > 0)
            {
                if (Dictionary.Count > 0)
                {
                    if (items.Keys.Any((k) => Dictionary.ContainsKey(k)))
                        throw new ArgumentException("An item with the same key has already been added.");
                    else
                        foreach (var item in items) Dictionary.Add(item);
                }
                else
                    _dictionary = new Dictionary<TKey, TValue>(items);

                OnCollectionChanged(NotifyCollectionChangedAction.Add, items.ToArray());
            }
        }

        private void Insert(TKey key, TValue value, bool add)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));


            if (Dictionary.TryGetValue(key, out TValue item))
            {
                if (add) throw new ArgumentException("An item with the same key has already been added.");
                if (Equals(item, value)) return;
                Dictionary[key] = value;

                OnCollectionChanged(NotifyCollectionChangedAction.Replace, 
                    new KeyValuePair<TKey, TValue>(key, value), 
                    new KeyValuePair<TKey, TValue>(key, item));
            }
            else
            {
                Dictionary[key] = value;
                OnCollectionChanged(NotifyCollectionChangedAction.Add, 
                    new KeyValuePair<TKey, TValue>(key, value));
            }
        }

        private void OnPropertyChanged()
        {
            OnPropertyChanged(countString);
            OnPropertyChanged(indexerName);
            OnPropertyChanged(keysName);
            OnPropertyChanged(valuesName);
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnCollectionChanged()
        {
            OnPropertyChanged();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, KeyValuePair<TKey, TValue> changedItem)
        {
            OnPropertyChanged();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, changedItem));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, 
            KeyValuePair<TKey, TValue> newItem, 
            KeyValuePair<TKey, TValue> oldItem)
        {
            OnPropertyChanged();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, newItem, oldItem));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, IList newItems)
        {
            OnPropertyChanged();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, newItems));
        }
    }
}