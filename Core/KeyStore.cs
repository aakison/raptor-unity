﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace RaptorDB
{
    #region [   KeyStoreString   ]
    public class RaptorDBString : IDisposable
    {
        public RaptorDBString(string filename, bool caseSensitve)
        {
            _db = KeyStore<int>.Open(filename, true);
            _caseSensitive = caseSensitve;
        }
        bool _caseSensitive = false;

        KeyStore<int> _db;


        public void Set(string key, string val)
        {
            Set(key, Encoding.Unicode.GetBytes(val));
        }

        public void Set(string key, byte[] val)
        {
            string str = (_caseSensitive ? key : key.ToLower());
            var bkey = Encoding.Unicode.GetBytes(str);
            int hc = (int)Helper.Murmur.Hash(bkey);
            var ms = new MemoryStream();
            ms.Write(Helper.GetBytes(bkey.Length, false), 0, 4);
            ms.Write(bkey, 0, bkey.Length);
            ms.Write(val, 0, val.Length);

            _db.Set(hc, ms.ToArray());
        }

        public bool Get(string key, out string val)
        {
            val = null;
            byte[] bval;
            bool b = Get(key, out bval);
            if (b)
            {
                val = Encoding.Unicode.GetString(bval);
            }
            return b;
        }

        public bool Get(string key, out byte[] val)
        {
            string str = (_caseSensitive ? key : key.ToLower());
            val = null;
            var bkey = Encoding.Unicode.GetBytes(str);
            int hc = (int)Helper.Murmur.Hash(bkey);

            if (_db.Get(hc, out val))
            {
                // unpack data
                byte[] g;
                if(UnpackData(val, out val, out g)) {
                    if(!Helper.BytewiseEquals(bkey, g)) {
                        // if data not equal check duplicates (hash conflict)
                        var ints = new List<int>(_db.GetDuplicates(hc));
                        ints.Reverse();
                        foreach(int i in ints) {
                            var bb = _db.FetchRecordBytes(i);
                            if(UnpackData(bb, out val, out g)) {
                                if(Helper.BytewiseEquals(bkey, g)) {
                                    return true;
                                }
                            }
                        }
                        return false;
                    }
                    return true;
                }
            }
            return false;
        }

        public int Count()
        {
            return (int)_db.Count();
        }

        public int RecordCount()
        {
            return (int)_db.RecordCount();
        }

        public bool RemoveKey(string key)
        {
            var bkey = Encoding.Unicode.GetBytes(key);
            int hc = (int)Helper.Murmur.Hash(bkey);
            var ms = new MemoryStream();
            ms.Write(Helper.GetBytes(bkey.Length, false), 0, 4);
            ms.Write(bkey, 0, bkey.Length);
            return _db.Delete(hc, ms.ToArray());
        }

        public void SaveIndex()
        {
            _db.SaveIndex();
        }

        public void Shutdown()
        {
            _db.Shutdown();
        }

        public void Dispose()
        {
            _db.Shutdown();
        }

        private bool UnpackData(byte[] buffer, out byte[] val, out byte[] key)
        {
            int len = Helper.ToInt32(buffer, 0, false);
            key = new byte[len];
            Buffer.BlockCopy(buffer, 4, key, 0, len);
            val = new byte[buffer.Length - 4 - len];
            Buffer.BlockCopy(buffer, 4 + len, val, 0, buffer.Length - 4 - len);

            return true;
        }

        public string ReadData(int recnumber)
        {
            var b = _db.FetchRecordBytes(recnumber);
            byte[] val, key;
            if (UnpackData(b, out val, out key))
            {
                return Encoding.Unicode.GetString(val);
            }
            return "";
        }
    }
    #endregion

    #region [   KeyStoreGuid   ]
    public class RaptorDBGuid : IDisposable
    {
        public RaptorDBGuid(string filename)
        {
            _db = KeyStore<int>.Open(filename, true);
        }

        KeyStore<int> _db;

        public void Set(Guid key, string val)
        {
            Set(key, Encoding.Unicode.GetBytes(val));
        }

        public int Set(Guid key, byte[] val)
        {
            var bkey = key.ToByteArray();
            int hc = (int)Helper.Murmur.Hash(bkey);
            var ms = new MemoryStream();
            ms.Write(Helper.GetBytes(bkey.Length, false), 0, 4);
            ms.Write(bkey, 0, bkey.Length);
            ms.Write(val, 0, val.Length);

            return _db.Set(hc, ms.ToArray());
        }

        public bool Get(Guid key, out string val)
        {
            val = null;
            byte[] bval;
            bool b = Get(key, out bval);
            if (b)
            {
                val = Encoding.Unicode.GetString(bval);
            }
            return b;
        }

        public bool Get(Guid key, out byte[] val)
        {
            val = null;
            var bkey = key.ToByteArray();
            int hc = (int)Helper.Murmur.Hash(bkey);

            if (_db.Get(hc, out val))
            {
                // unpack data
                byte[] g;
                if(UnpackData(val, out val, out g)) {
                    if(!Helper.BytewiseEquals(bkey, g)) {
                        // if data not equal check duplicates (hash conflict)
                        var ints = new List<int>(_db.GetDuplicates(hc));
                        ints.Reverse();
                        foreach(int i in ints) {
                            var bb = _db.FetchRecordBytes(i);
                            if(UnpackData(bb, out val, out g)) {
                                if(Helper.BytewiseEquals(bkey, g)) {
                                    return true;
                                }
                            }
                        }
                        return false;
                    }
                    return true;
                }
            }
            return false;
        }

        public void SaveIndex()
        {
            _db.SaveIndex();
        }

        public void Shutdown()
        {
            _db.Shutdown();
        }

        public void Dispose()
        {
            _db.Shutdown();
        }

        public byte[] FetchRecordBytes(int record)
        {
            return _db.FetchRecordBytes(record);
        }

        public int Count()
        {
            return (int)_db.Count();
        }

        public int RecordCount()
        {
            return (int)_db.RecordCount();
        }

        private bool UnpackData(byte[] buffer, out byte[] val, out byte[] key)
        {
            int len = Helper.ToInt32(buffer, 0, false);
            key = new byte[len];
            Buffer.BlockCopy(buffer, 4, key, 0, len);
            val = new byte[buffer.Length - 4 - len];
            Buffer.BlockCopy(buffer, 4 + len, val, 0, buffer.Length - 4 - len);

            return true;
        }

        internal byte[] Get(int recnumber, out Guid docid)
        {
            bool isDeleted;
            return Get(recnumber, out docid, out isDeleted);
        }

        public bool RemoveKey(Guid key)
        {
            var bkey = key.ToByteArray();
            int hc = (int)Helper.Murmur.Hash(bkey);
            var ms = new MemoryStream();
            ms.Write(Helper.GetBytes(bkey.Length, false), 0, 4);
            ms.Write(bkey, 0, bkey.Length);
            return _db.Delete(hc, ms.ToArray());
        }

        internal byte[] Get(int recnumber, out Guid docid, out bool isdeleted)
        {
            docid = Guid.Empty;
            var buffer = _db.FetchRecordBytes(recnumber, out isdeleted);
            if (buffer == null) {
                return null;
            }

            if (buffer.Length == 0) {
                return null;
            }

            // unpack data
            byte[] val, key;
            UnpackData(buffer, out val, out key);
            docid = new Guid(key);
            return val;
        }

        internal int CopyTo(StorageFile<int> backup, int start)
        {
            return _db.CopyTo(backup, start);
        }
    }
    #endregion

    public class KeyStore<T> : IDisposable where T : IComparable<T>
    {
        public KeyStore(string Filename, byte MaxKeySize, bool AllowDuplicateKeys)
        {
            Initialize(Filename, MaxKeySize, AllowDuplicateKeys);
        }

        public KeyStore(string Filename, bool AllowDuplicateKeys)
        {
            Initialize(Filename, Global.DefaultStringKeySize, AllowDuplicateKeys);
        }

        private ILog log = LogManager.GetLogger(typeof(KeyStore<T>));

        private string _Path = "";
        private string _FileName = "";
        private byte _MaxKeySize;
        private StorageFile<T> _archive;
        private MGIndex<T> _index;
        private string _datExtension = ".mgdat";
        private string _idxExtension = ".mgidx";
        IGetBytes<T> _T = null;
        private System.Timers.Timer _savetimer;
        private BoolIndex _deleted;


        public static KeyStore<T> Open(string Filename, bool AllowDuplicateKeys)
        {
            return new KeyStore<T>(Filename, AllowDuplicateKeys);
        }

        public static KeyStore<T> Open(string Filename, byte MaxKeySize, bool AllowDuplicateKeys)
        {
            return new KeyStore<T>(Filename, MaxKeySize, AllowDuplicateKeys);
        }

        object _savelock = new object();
        public void SaveIndex()
        {
            if (_index == null) {
                return;
            }

            lock (_savelock)
            {
                log.Debug("saving to disk");
                _index.SaveIndex();
                _deleted.SaveIndex();
                log.Debug("index saved");
            }
        }

        public IEnumerable<int> GetDuplicates(T key)
        {
            // get duplicates from index
            return _index.GetDuplicates(key);
        }

        public byte[] FetchRecordBytes(int record)
        {
            return _archive.ReadData(record);
        }

        public string FetchRecordString(int record)
        {
            var b = _archive.ReadData(record);

            return Encoding.Unicode.GetString(b);
        }

        public IEnumerable<StorageData> EnumerateStorageFile()
        {
            return _archive.Enumerate();
        }

        public IEnumerable<KeyValuePair<T, int>> Enumerate(T fromkey)
        {
            // generate a list from the start key using forward only pages
            return _index.Enumerate(fromkey);
        }

        public bool RemoveKey(T key)
        {
            // remove and store key in storage file
            var bkey = _T.GetBytes(key);
            var ms = new MemoryStream();
            ms.Write(Helper.GetBytes(bkey.Length, false), 0, 4);
            ms.Write(bkey, 0, bkey.Length);
            return Delete(key, ms.ToArray());
        }

        public long Count()
        {
            int c = _archive.Count();
            return c - _deleted.GetBits().CountOnes() * 2;
        }

        public bool Get(T key, out string val)
        {
            val = "";
            byte[] b;
            bool ret = Get(key, out b);
            if (ret) {
                val = Encoding.Unicode.GetString(b);
            }

            return ret;
        }

        public bool Get(T key, out byte[] val)
        {
            val = null;
            var k = key;
            // search index
            int off;
            if (_index.Get(k, out off))
            {
                val = _archive.ReadData(off);
                return true;
            }
            return false;
        }

        public int Set(T key, string data)
        {
            return Set(key, Encoding.Unicode.GetBytes(data));
        }

        public int Set(T key, byte[] data)
        {
            int recno = -1;
            // save to storage
            recno = _archive.WriteData(key, data, false);
            // save to index
            _index.Set(key, recno);

            return recno;
        }

        private object _shutdownlock = new object();
        public void Shutdown()
        {
            lock (_shutdownlock)
            {
                if (_index != null) {
                    log.Debug("Shutting down");
                }
                else {
                    return;
                }

                SaveIndex();
                SaveLastRecord();

                if (_deleted != null) {
                    _deleted.Shutdown();
                }

                if (_index != null) {
                    _index.Shutdown();
                }

                if (_archive != null) {
                    _archive.Shutdown();
                }

                _index = null;
                _archive = null;
                _deleted = null;
                log.Debug("Shutting down log");
                LogManager.Shutdown();
            }
        }

        public void Dispose()
        {
            Shutdown();
        }

        #region [            P R I V A T E     M E T H O D S              ]
        private void SaveLastRecord()
        {
            // save the last record number in the index file
            _index.SaveLastRecordNumber(_archive.Count());
        }

        private void Initialize(string filename, byte maxkeysize, bool AllowDuplicateKeys)
        {
            _MaxKeySize = RDBDataType<T>.GetByteSize(maxkeysize);
            _T = RDBDataType<T>.ByteHandler();

            _Path = Path.GetDirectoryName(filename);
            Directory.CreateDirectory(_Path);

            _FileName = Path.GetFileNameWithoutExtension(filename);
            string db = _Path + Path.DirectorySeparatorChar + _FileName + _datExtension;
            //string idx = _Path + Path.DirectorySeparatorChar + _FileName + _idxExtension;

            LogManager.Configure(_Path + Path.DirectorySeparatorChar + _FileName + ".txt", 500, false);

            _index = new MGIndex<T>(_Path, _FileName + _idxExtension, _MaxKeySize, Global.PageItemCount, AllowDuplicateKeys);

            _archive = new StorageFile<T>(db);

            _deleted = new BoolIndex(_Path, _FileName + "_deleted.idx");

            _archive.SkipDateTime = true;

            log.Debug("Current Count = " + RecordCount().ToString("#,0"));

            CheckIndexState();

            log.Debug("Starting save timer");
            _savetimer = new System.Timers.Timer();
            _savetimer.Elapsed += new System.Timers.ElapsedEventHandler(_savetimer_Elapsed);
            _savetimer.Interval = Global.SaveIndexToDiskTimerSeconds * 1000;
            _savetimer.AutoReset = true;
            _savetimer.Start();

        }

        private void CheckIndexState()
        {
            log.Debug("Checking Index state...");
            int last = _index.GetLastIndexedRecordNumber();
            int count = _archive.Count();
            if (last < count)
            {
                log.Debug("Rebuilding index...");
                log.Debug("   last index count = " + last);
                log.Debug("   data items count = " + count);
                // check last index record and archive record
                //       rebuild index if needed
                for (int i = last; i < count; i++)
                {
                    bool deleted;
                    var key = _archive.GetKey(i, out deleted);
                    if (deleted == false) {
                        _index.Set(key, i);
                    }
                    else {
                        _index.RemoveKey(key);
                    }

                    if (i % 100000 == 0) {
                        log.Debug("100,000 items re-indexed");
                    }
                }
                log.Debug("Rebuild index done.");
            }
        }

        void _savetimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            SaveIndex();
        }

        #endregion

        internal int RecordCount()
        {
            return _archive.Count();
        }

        internal byte[] FetchRecordBytes(int record, out bool isdeleted)
        {
            return _archive.ReadData(record, out isdeleted);
        }

        internal bool Delete(T id, byte[] data)
        {
            // write a delete record
            int rec = _archive.WriteData(id, data, true);
            _deleted.Set(true, rec);
            return _index.RemoveKey(id);
        }

        internal int CopyTo(StorageFile<int> storagefile, int start)
        {
            return _archive.CopyTo(storagefile, start);
        }
    }
}
