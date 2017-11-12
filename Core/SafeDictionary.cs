using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace RaptorDB {
    internal class SafeDictionary<TKey, TValue> {
        private readonly object mutex = new object();
        private readonly Dictionary<TKey, TValue> dictionary = null;

        public SafeDictionary(int capacity) {
            dictionary = new Dictionary<TKey, TValue>(capacity);
        }

        public SafeDictionary() {
            dictionary = new Dictionary<TKey, TValue>();
        }

        public bool TryGetValue(TKey key, out TValue value) {
            lock(mutex) {
                return dictionary.TryGetValue(key, out value);
            }
        }

        public TValue this[TKey key] {
            get {
                lock(mutex) {
                    return dictionary[key];
                }
            }
            set {
                lock(mutex) {
                    dictionary[key] = value;
                }
            }
        }

        public int Count {
            get {
                lock(mutex) {
                    return dictionary.Count;
                }
            }
        }

        public ICollection<KeyValuePair<TKey, TValue>> GetList() {
            return (ICollection<KeyValuePair<TKey, TValue>>)dictionary;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
            return ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).GetEnumerator();
        }

        public void Add(TKey key, TValue value) {
            lock(mutex) {
                if(dictionary.ContainsKey(key) == false) {
                    dictionary.Add(key, value);
                }
            }
        }

        public TKey[] Keys() {
            lock(mutex) {
                var keys = new TKey[dictionary.Keys.Count];
                dictionary.Keys.CopyTo(keys, 0);
                return keys;
            }
        }

        public bool Remove(TKey key) {
            lock(mutex) {
                return dictionary.Remove(key);
            }
        }
    }

    internal class SafeSortedList<T, V> {
        private object mutex = new object();
        SortedList<T, V> list = new SortedList<T, V>();

        public int Count {
            get {
                lock(mutex) {
                    return List.Count;
                }
            }
        }

        public SortedList<T, V> List {
            get {
                return list;
            }
            set {
                list = value;
            }
        }

        public void Add(T key, V val) {
            lock(mutex) {
                List.Add(key, val);
            }
        }

        public void Remove(T key) {
            if(key == null) {
                return;
            }

            lock(mutex) {
                List.Remove(key);
            }
        }

        public T GetKey(int index) {
            lock(mutex) {
                return List.Keys[index];
            }
        }

        public V GetValue(int index) {
            lock(mutex) {
                return List.Values[index];
            }
        }
    }

    //------------------------------------------------------------------------------------------------------------------

    internal static class FastDateTime {
        public static TimeSpan LocalUtcOffset;

        public static DateTime Now {
            get { return DateTime.UtcNow + LocalUtcOffset; }
        }

        static FastDateTime() {
            LocalUtcOffset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);
        }
    }

    //------------------------------------------------------------------------------------------------------------------

    internal static class Helper {
        public static Murmur3 Murmur = new Murmur3();

        public static bool BytewiseEquals(byte[] left, byte[] right) {
            return left.SequenceEqual(right);
        }

        internal static int ToInt32(byte[] value, int startIndex, bool reverse) {
            if(reverse) {
                var b = new byte[4];
                Buffer.BlockCopy(value, startIndex, b, 0, 4);
                Array.Reverse(b);
                return ToInt32(b, 0);
            }

            return ToInt32(value, startIndex);
        }

        internal static int ToInt32(byte[] value, int startIndex) {
            return BitConverter.ToInt32(value, startIndex);
        }

        internal static long ToInt64(byte[] value, int startIndex, bool reverse) {
            if(reverse) {
                var b = new byte[8];
                Buffer.BlockCopy(value, startIndex, b, 0, 8);
                Array.Reverse(b);
                return ToInt64(b, 0);
            }
            return ToInt64(value, startIndex);
        }

        internal static long ToInt64(byte[] value, int startIndex) {
            return BitConverter.ToInt64(value, startIndex);
        }

        internal static short ToInt16(byte[] value, int startIndex, bool reverse) {
            if(reverse) {
                var b = new byte[2];
                Buffer.BlockCopy(value, startIndex, b, 0, 2);
                Array.Reverse(b);
                return ToInt16(b, 0);
            }
            return ToInt16(value, startIndex);
        }

        internal static short ToInt16(byte[] value, int startIndex) {
            return BitConverter.ToInt16(value, startIndex);
        }

        internal static byte[] GetBytes(long num, bool reverse) {
            var buffer = BitConverter.GetBytes(num);
            if(reverse) {
                Array.Reverse(buffer);
            }
            return buffer;
        }

        public static byte[] GetBytes(int num, bool reverse) {
            var buffer = BitConverter.GetBytes(num);
            if(reverse) {
                Array.Reverse(buffer);
            }
            return buffer;
        }

        public static byte[] GetBytes(short num, bool reverse) {
            var buffer = BitConverter.GetBytes(num);
            if(reverse) {
                Array.Reverse(buffer);
            }
            return buffer;
        }

        public static byte[] GetBytes(string s) {
            return Encoding.UTF8.GetBytes(s);
        }

        internal static string GetString(byte[] buffer, int index, short keylength) {
            return Encoding.UTF8.GetString(buffer, index, keylength);
        }

    }
}
