Google App Engine .NET (GAE.NET)
====================================

This (unofficial) project brings Google App Engine to .NET in a way that is simple and familiar to .NET developers. This project currently supports:

* Google Datastore
* Google Cloud Storage

Status: pre-alpha


Compatibility
-------------

* .NET Core / vNext (Linux, Mac, Windows)
* .NET Framework 4.x (Windows)


License
-------

Apache v2 license.


LINQ to Datastore (LINQ to GQL)
======================================

Google's Datastore is a fast and extremely scalable NoSQL solution in the cloud. LINQ to Datastore provides a simple LINQ interface to perform CRUD (create, read, update, and delete) operations on Datastore, complete with transactional support and fully parameterized queries.


Getting Started
---------------

To get started with Datastore, login to the Google Developer Console -> select your project -> click "Credentials" under "APIs & auth" -> create a new Service Account (OAuth) -> click "Generate a new P12 key" -> import the downloaded file into your project.

Add the following `using` statements at the top:

```
using GoogleAppEngine;
using GoogleAppEngine.Datastore;
```

Create a service account object.

```
var serviceAccount = new ServiceAccountAuthenticator(
				"YOUR-PROJECT-ID", // As seen in your Google Developer Console
                "SERVICE-ACCOUNT-EMAIL-ADDRESS",  // Generated using the steps above
				"./path/to/credentials.p12", // Generated using the steps above
				"notasecret"); // Generated using the steps above
```

Pass the service account to the DatastoreService class.

```
var Datastore = new DatastoreService(serviceAccount);
```

You can now do any of the following:

* Create or update (**upsert**) data:

```
Datastore.Upsert<Artist>(new Artist { Rating = 5.0, Name = "Pablo Picasso" });

// or

Datastore.UpsertRange<Artist>(myListOfArtists);
```

* Read data:

```
var greatArtists = Datastore.Find<Artist>()
						.Where(artist => artist.Rating > 4.5)
						.Select(artist => new { artist.FirstName, artist.LastName });
```

* Delete data:

```
Datastore.Delete<Artist>().Where(artist => artist.Rating > 4.5);

// or

Datastore.Delete<Artist>(myArtistObject);

// or

Datastore.DeleteRange<Artist>(myListOfArtists);
```


Limitations and Caveats
-----------------------

To improve scalability and performance, Google Datastore imposes limits on how you can access your data. These limitations can have serious implications on how to design your app to scale. Read about [Datastore limitations](https://cloud.google.com/appengine/docs/python/Datastore/queries#Python_Restrictions_on_queries).

* _Inequality operators_ (`>, <, >=, <=, !=`) may be used on only **one** property in a single query.
--* **Valid:** `(artist => artist.Rating > 4 && artist.LastName == "Picasso" && artist.Status == "Legendary")`
--* **Valid:** `(artist => artist.Rating > 4 && artist.Rating < 5 && artist.Name == "Picasso")` because you can use multiple inequality operators on one property.
--* **INVALID:** `(artist => artist.Rating > 4 && artist.NumOfCreations <= 50000 && artist.Status == "Legendary")` because there are inequality operators (`>, <=`) on more than one property. LINQ to Datastore will throw an exception on such a query.
* The `||` operator (logical OR) is not supported by Google Datastore.
--* Alternative: for example, the query `(artist => artist.Rating >= 4.9 || artist.Status == "Legendary")` can be split into two separate queries: `(artist => artist.Rating >= 4.9)` and `(artist => artist.Status == "Legendary")`.
* The `!=` operator (not equal to) is also not supported by Google Datastore.
--* Alternative: to say `(artist => artist.Rating != 4)`, execute two queries `(artist.Rating > 4)` and `(artist.Rating < 4)` and merge the results.
* Querying with child entities (e.g. `Datastore.Find<Order>().Where(x => x.OrderLineItems.Any(y => y.Quantity > 1))`) is not supported by Datastore.
* `short, int, long, string, enum, DateTime, double, decimal, bool, byte[]` are supported (including List<T> and Dictionary<string,T> where T is a basic type).
--* Non-primitive fields (for example, a field of type `Address` in a `Customer` object) are serialized into embedded entities whose fields must be basic types.
--* Non-primitive fields (including lists and dictionaries) cannot be queried. In fact, non-primitive fields (with the exception of lists and dictionaries) _must_ be tagged with the DatastoreNotIndexed attribute or the underlying Google API will throw an exception.
--* You can write a LINQ to Datastore serializer by implementing `IDatastoreSerializer` to add support for more complicated cases.
* Your id/key field must be a string. Integer keys are not supported (yet). In the background, LINQ to Datastore generates a GUID for the id/key field if it does not have one.
--* In the (extremely) unlikely event that the generated GUID is a duplicate, the old entity is updated. To avoid this, enable `DoubleCheckGeneratedIds` in the DatastoreConfiguration object to have LINQ to Datastore verify that the generated ID does not exist (internally, LINQ to Datastore then performs two transactions--one to check that the key does not exist, another to add/update).
* Since Google Datastore has no concept of migration, if you change the types or names of properties _after_ storing data, the previously stored data can no longer be queried directly with LINQ.

This project currently supports the following LINQ methods:

* Where
* Select
* Take
* Skip
* OrderBy
* OrderByDescending
* First
* FirstOrDefault
* Single
* SingleOrDefault
* Any

Attempting to use any other LINQ method will throw an exception. Note that you can still use any LINQ method in-memory (i.e after `ToList()` or `AsEnumerable()`).

Workarounds for Some Limitations
---------------------------

You can always filter in-memory. Doing so is simple:

```
// Create a query for all "legendary" artists
var query = Datastore.Find<Artist>().Where(artist => artist.Status == "Legendary");

// Next filter through the artists in-memory
var reallyGreatArtists = query.AsEnumerable().Where(artist => artist.Rating > 4 && artist.NumOfCreations <= 50000 && artist.TimePeriod != PERIOD_18TH_CENTURY).ToList();
```

However, this approach loads _all_ "legendary" artists into memory. This is only useful for small datasets where it may be OK filter in-memory.

Generating Indexes
------------------

Google API throws a `No matching index found [412]` exception if you compare properties that do not have an associated index. LINQ to Datastore can automatically build and maintain a YAML index file based on your LINQ queries. To enable automatic generation of indexes, pass in a `DatastoreConfiguration` object when creating the Datastore service:

```
var config = new DatastoreConfiguration { GenerateIndexYAMLFile = true };
var Datastore = new DatastoreService(serviceAccount, config);
```

> Do not enable automatic index generation on production machines.

As you debug, test, and build your application, LINQ to Datastore will create and maintain a `index.yaml` in the project's executing folder (usually bin or project folder for vNext applications).

As you build your application, if you run into any `No matching index found` exceptions, open the *Google Cloud SDK* shell, `cd` to the location where the index file is generated, and run `create-indexes` on the index.yaml file. After uploading the new index configuration, you can track the status of the index update on your Developer Console. Once the index is rebuilt, you can proceed testing your application normally (that time you should not see a `No matching index found` exception). See the [Cloud SDK](https://cloud.google.com/sdk/gcloud/reference/preview/datastore/create-indexes) documentation for more information.

> LINQ to Datastore does not yet generate indexes for OrderBy/OrderByDescending queries. You will have to edit the index.yaml file manually for that.

Design Considerations
---------------------

Knowing all of the above, it is important to design your application to scale. Datastore creates  `2^(number of filters) * (number of different order by operations)` indexes. This can lead to very high write costs (since updating an entity must update every index). A write operation on a simple class like

```class OrderInfo
{
	[DatastoreKey]
	public string OrderId { get; set; }
	
	public int CustomerId { get; set; }
	public List<string> LineItemIds { get; set; }
	public string FirstName { get; set; }
	public string LastName { get; set; }
	public string Phone { get; set; }
	public string StreetLine1 { get; set; }
	public string StreetLine2 { get; set; }
	public string StreetLine3 { get; set; }
	public string City { get; set; }
	public string State { get; set; }
	public string Country { get; set; }
}```

where everything is indexed can easily exceed the index cap (aka `exploding index`). You should decorate the properties that you do not use in `WHERE` clauses with the `[DatastoreNotIndexed]` attribute (however you can still filter and sort them in-memory). Thus, your class could look like

```class OrderInfo
{
	[DatastoreKey]
	public string OrderId { get; set; }
	
	public int CustomerId { get; set; }
	
	// Above, OrderId and CustomerId are indexed.
	// The properties below are not.
	
	[DatastoreNotIndexed]
	public List<string> LineItemIds { get; set; }
	
	[DatastoreNotIndexed]
	public string FirstName { get; set; }
	
	[DatastoreNotIndexed]
	public string LastName { get; set; }
	
	// ...
}```

This is especially true for collections (Lists and Dictionaries).

Note that even if you mark a property with `[DatastoreNotIndexed]`, it is still saved when upserting data (but won't be indexed). That means LINQ to Datastore will save the data but Google Datastore won't create an index on the property. To avoid saving a property altogether, mark it with the `[DatastoreNotSaved]` attribute.


Google Cloud Storage
=====================

Simplified storage access API. 

```
var myBucket = Storage.Bucket("MyBucket");

// Save a file to Goole Cloud Storage
myBucket.UploadFile("./Content/myFile.zip");

// Save text (the first parameter is the filename to save as)
myBucket.UploadText("SomeFileInStorage.html", "<b>Easily save text.</b>");

// Save from a byte array
byte[] myImage = ...;
myBucket.UploadData("butterflies.jpg", myImage, Permissions.OwnerOnly);

// Save from a stream
myBucket.UploadStream("butterflies.jpg", myStream, Permissions.OwnerOnly);

// You can also chain uploads:
myBucket
	.UploadFile("./data/SomeFile.zip", Permissions.PublicallyViewable)
	.UploadText("AnotherFile.txt", "Hello, world!");
	
// Download as byte[]
var myImage = myBucket.DownloadData("butterflies.jpg");

// Download to file
myBucket.DownloadFile("MyFile.txt", "./downloads/SavedFile.txt");

// Download text
var myText = myBucket.DownloadText("SomeFile.html");

// Download to stream
myBucket.DownloadStream("SomeFile.html", outputStream);
```