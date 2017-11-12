using RaptorDB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Raptor.Unity {

    /// <summary>
    /// A wrapper around RaptorDB to provide a simplified interface for the most common use-cases.
    /// Additionally, this provides coroutines for storing and accessing to enable Unity style cooperative multi-tasking.
    /// </summary>
    public class Raptor3D {

        /// <summary>
        /// Create a new instance of Raptor3D that is intended to be stored and used throughout the life of the program.
        /// When running on iOS, this constructor must be called from the main thread.
        /// </summary>
        /// <param name="name">The name of the storage directory, the default 'raptor' is recommended unless creating multiple document stores.</param>
        public Raptor3D(string name = "raptor") {
            if(!ValidCrossPlatformName(name)) {
                throw new ArgumentException($"In order to enable the best cross platform ability, names are limited to {validNamePattern}", nameof(name));
            }
            indexDirectory = new DirectoryInfo(Path.Combine(Application.persistentDataPath, name, "raptor"));
            datastore = new RaptorDB<string>(indexDirectory.FullName, 64, false);
        }

        /// <summary>
        /// Defines a document type that can be stored in the document store by providing functions for accessing key, serializer and deserializers.
        /// </summary>
        /// <typeparam name="T">The type of the objects that are converted using these functions.</typeparam>
        /// <param name="key">A function that returns a string key, this key is limited to 64 characters.</param>
        /// <param name="serializer">A function that serializes objects to documents, typically inject Newtonsoft.Json or XmlSerializer.</param>
        /// <param name="deserializer">A function that deserializes documents back to objects, typically inject Newtonsoft.Json or XmlSerializer.</param>
        public void DefineDocument<T>(Func<T, string> key, Func<T, string> serializer, Func<string, T> deserializer) {
            if(key == null) {
                throw new ArgumentNullException(nameof(key), "Document must provide a key function.");
            }
            if(serializer == null) {
                throw new ArgumentNullException(nameof(serializer), "Document must provide a serializer.  E.g. e => JsonConvert.Serialize(e);");
            }
            if(deserializer == null) {
                throw new ArgumentNullException(nameof(deserializer), "Document must provide a deserializer.  E.g. e => JsonConvert.Deserialize<T>(e);");
            }
            var type = typeof(T);
            if(documentTypes.ContainsKey(type)) {
                throw new ArgumentException("Define document can only be called once per type.", type.Name);
            }
            var definition = new DocumentDefinition {
                Type = type,
                KeyFunc = TypedToUntypedLambdaSerializer(key),
                Serializer = TypedToUntypedLambdaSerializer(serializer),
                Deserializer = TypedToUntypedLambdaDeserializer(deserializer),
            };
            documentTypes.Add(type, definition);
        }

        /// <summary>
        /// Stores a single item in the document store.
        /// </summary>
        /// <typeparam name="T">The implicit type of the item.</typeparam>
        /// <param name="item">The item to be stored.</param>
        public void Store<T>(T item) {
            if(item == null) {
                throw new ArgumentNullException(nameof(item));
            }
            StoreInternal(new T[] { item }, processYields: false);
        }

        /// <summary>
        /// Stores a collection of items in the document store.
        /// </summary>
        /// <typeparam name="T">The implicit type of the items.</typeparam>
        /// <param name="items">The list of items to be stored.</param>
        public void Store<T>(IEnumerable<T> items) {
            StoreInternal(items, processYields: false);
        }

        /// <summary>
        /// Index the given objects, where the key text and serializer must have been previously defined using `DefineDocument`.
        /// </summary>
        /// <typeparam name="T">The implicit type of the items.</typeparam>
        /// <param name="items">The list of items to be stored.</param>
        /// <param name="timeSlice">The maximum amount of time before the coroutine yields for other processing.  Use to strike a balance between not dropping frames and storage performance.</param>
        public IEnumerator StoreCoroutine<T>(IEnumerable<T> items, int timeSlice = 13) {
            yield return StoreInternal<T>(items, timeSlice, true);
        }

        private IEnumerator StoreInternal<T>(IEnumerable<T> items, int timeSlice = 13, bool processYields = true) {
            if(items == null) {
                throw new ArgumentNullException(nameof(items));
            }
            var type = typeof(T);
            if(!documentTypes.ContainsKey(type)) {
                throw new ArgumentOutOfRangeException(nameof(items), "The type must be defined using `DefineDocument` before it can be stored.");
            }
            var definition = documentTypes[type];
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var count = 0;
            var notNullItems = items.Where(e => e != null);
            var total = notNullItems.Count();
            OnProgress("Storing", 0, total);
            foreach(var item in notNullItems) {
                var key = definition.KeyFunc(item);
                var doc = definition.Serializer(item);
                datastore.Set(key, doc);
                if(stopwatch.ElapsedMilliseconds >= timeSlice) {
                    OnProgress("Storing", count, total);
                    if(processYields) {
                        yield return null;
                    }
                    stopwatch.Restart();
                }
                ++count;
            }
            OnProgress("Storing", count, total);
            if(processYields) {
                yield return null;
            }
        }

        public T Retrieve<T>(string key) {
            if(key == null) {
                throw new ArgumentNullException(nameof(key));
            }
            var type = typeof(T);
            if(!documentTypes.ContainsKey(type)) {
                throw new ArgumentOutOfRangeException(nameof(T), "The type must be defined using `DefineDocument` before it can be stored.");
            }
            var definition = documentTypes[type];
            //OnProgress("Retrieving", 0, 1);
            T result;
            string text;
            var success = datastore.Get(key, out text);
            if(!success) {
                throw new IndexOutOfRangeException("The given key could not be found in the data store.");
            }
            result = (T)definition.Deserializer(text);
            //OnProgress("Retrieving", 1, 1);
            return result;
        }

        public bool Delete(string key) {
            throw new NotImplementedException();
        }

        public bool Delete(IEnumerable<string> keys) {
            throw new NotImplementedException();
        }

        public bool DeleteAll() {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The progress event provides callbacks for updates on processing within Raptor.
        /// Typically used to provide user feedback on long running storage operations over large data sets.
        /// </summary>
        public event EventHandler<RaptorProgressEventArgs> Progress;
        private RaptorProgressEventArgs progressEventArgs = new RaptorProgressEventArgs();
        private Stopwatch progressStopwatch = new Stopwatch();
        private void OnProgress(string title, int count, int total) {
            if(count == 0) {
                progressStopwatch.Restart();
            }
            if(Progress != null) {
                progressEventArgs.Title = title;
                progressEventArgs.Count = count;
                progressEventArgs.Total = total;
                progressEventArgs.Duration = (int)progressStopwatch.ElapsedMilliseconds;
                Progress(this, progressEventArgs);
            }
        }

        private Dictionary<Type, DocumentDefinition> documentTypes = new Dictionary<Type, DocumentDefinition>();

        private DirectoryInfo indexDirectory;

        private RaptorDB<string> datastore;

        private class DocumentDefinition {
            public Type Type { get; set; }
            public Func<object, string> KeyFunc { get; set; }
            public Func<object, string> Serializer { get; set; }
            public Func<string, object> Deserializer { get; set; }
        }

        private Func<object, string> TypedToUntypedLambdaSerializer<T>(Func<T, string> func) {
            if(func == null) {
                return null;
            }
            else {
                return new Func<object, string>(o => func((T)o));
            }
        }

        private Func<string, object> TypedToUntypedLambdaDeserializer<T>(Func<string, T> func) {
            if(func == null) {
                return null;
            }
            else {
                return new Func<string, object>(s => (object)func(s));
            }
        }

        private bool ValidCrossPlatformName(string name) {
            return validName.IsMatch(name);
        }
        private const string validNamePattern = "[a-zA-Z0-9_]{1,64}";
        private Regex validName = new Regex(validNamePattern);

    }

}