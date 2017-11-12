# raptor-unity
Adaptation of RaptorDb to work with Unity3D, including coroutine and behaviour facades.


## Installation
This repo is designed to be used as a submodule of an existing Unity3D repo.  E.g.

```
    git submodule add https://github.com/aakison/raptor-unity.git Assets/Raptor
```

There are no demo scenes or resources in this repo, just the required files so that your solution is not polluted with unnecessary files.

## Original Usage

Once installed, you have full access to the RaptorDB v2.7.5 library.
For complete usage examples, please see the official Raptor documentation at the [https://www.codeproject.com/articles/316816/raptordb-the-key-value-store-v2](https://www.codeproject.com/articles/316816/raptordb-the-key-value-store-v2).

## Getting Started with Unity Wrapper

For simplicity, there is a wrapper designed for use with Unity which provides an easier interface for Unity apps.
To get started, use the `Raptor3D` class.

Before you start, create a POCO for the data that you want indexed along with any additional meta-data, e.g.:

```
    public class Article {
        public string url;
        public string headline;
        public string body;
    }
```

Note that the use of public fields is because we're going to use the built in Unity JSON serializer in this example.

To start, create the `Raptor3D` class, no parameters are required. 

```
    var raptor = new Raptor3D();
```

This will create the raptor document store in the Unity Application.persistentDataPath directory.
This class is a long-life class and should be instantiated once for the life of the app.
Note that the constructor must be called on the main thread as Unity requires that Application is only accessed from that thread.

Next, define how you want your POCO serialized and provide a unique key:

```
    raptor.DefineDocument<Article>(
        e => e.url, 
        e => JsonUtility.ToJson(e), 
        e => JsonUtility.FromJson<Article>(e)
    );
```

In this example, we serialize and deserialize using the internal Unity `JsonUtility` which uses the normal Unity serialization rules.
This could easily be replaced by `XmlSerializer` in .NET or by downloading a Unity compatible version of NewtonsoftJson.
The key is defined to be the Url (this is an example only, as the key limit is 64 characters, many urls wouldn't fit).

Then, add your corpus of articles to the index:

```
    var articles = GetArticlesFromSomewhere();

    // Option 1, one at a time:
    raptor.Store(articles[0]);

    // Option 2, let the wrapper add them (suitable for a background thread)
    raptor.Store(articles);

    // Option 3, use coroutines for cooperative multi-tasking
    StartCoroutine(raptor.StoreCoroutine(articles));
```

At this point, the application can be closed and re-opened as the documents are stored on disk.

Finally, retrieve the documents using `Retrieve`:

```
    var key = "http://www.unity3D.com/sampledoc.html";
    var article = raptor.Retrieve<Article>(key);
```

The `article` contains a deserialied copy of the original article with that key.

In addition to this primary use-case, we can also monitor progress using the `Progress` event.

## License Info

This software was originally published as RaptorDB at [https://www.codeproject.com/articles/316816/raptordb-the-key-value-store-v2](https://www.codeproject.com/articles/316816/raptordb-the-key-value-store-v2).
This version has been modified to work with Unity through
* non-functional changes to the core code to remove warnings, 
* to refactor out unsafe byte array access code, 
* to replace unsafe version of Murmur hash with https://github.com/sebas77/Murmur3.net
* and to add a wrapper to make consumption from Unity easier.
