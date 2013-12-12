System.Text.Json
================

A minimalistic, and pretty fast JSON parser/deserializer.

The complete source code (Visual Studio 2010 solution), tests, and test data, are in the "JsonTest" folder.

Goal
----

This aims at parsing textual JSON data, and to deserialize it into our (strongly typed) [POCO](http://en.wikipedia.org/wiki/Plain_Old_CLR_Object)s, as fast as possible.

The only tier of interest for this parser is the server tier. There are other JSON librairies with good performances, and already well tested/documented, which support mobile devices running .NET ([JSON.NET](http://james.newtonking.com/json) and [ServiceStack](https://servicestack.net/) come to mind).

Early development status warning
--------------------------------

Although it is promisingly fast, please note this parser/deserializer is still experimental, also.

I do *not* recommend it for any use in production, at this stage. This may evolve soon, but for one thing to begin with, it's in need of more extensive JSON conformance tests.

That being said, feel free to fork/bugfix/augment/improve it at your own will.

I do think there is still room for raw performance improvement. In fact, I am not quite done experimenting with my ideas in that respect, and will try to continue doing so in this repository.

Of course, I welcome your informed input and feedback.

Please read the [LICENSE](https://github.com/ysharplanguage/FastJsonParser/blob/master/LICENSE.md).

Public interface
----------------

Consists in two generic *instance* methods:

    T Parse<T>(string input)

and

    T Parse<T>(System.IO.Stream input)
    
(being able to parse JSON text coming from a stream is a must-have, IMO - "*have mercy for your CLR's large object heap*", [if you see what I mean...](http://msdn.microsoft.com/en-us/magazine/cc534993.aspx) - thus, similar support for StreamReader should be easy to add along the same lines; coming soon hopefully)

Performances
------------

Here are some figures, from the tests that are provided here (note: such figures are much dependent on the test data/the way testing is performed; YMMV, so it's always a good idea to make your own benchmarks with *your* test data)...

(.NET 4.0 target, on an Ideapad w/ Intel Core i5 CPU @ 2.50GHz, 6GB RAM, running Win7 64bit, 98% idle CPU)

* "Loop" Test of tiny JSON (deserializing x times the JSON contained in the tiny.json.txt file = 126 bytes):
    * 10,000 iterations: in ~ 65 milliseconds vs. JSON.NET 5.0 r8 in ~ 250 milliseconds vs. ServiceStack in ~ 125 milliseconds
        * Yields System.Text.Json.JsonParser's throughput : 20,322,580 bytes / second
    * 100,000 iterations: in ~ 600 milliseconds vs. JSON.NET 5.0 r8 in ~ 900 milliseconds vs. ServiceStack in ~ 650 milliseconds
        * Yields System.Text.Json.JsonParser's throughput : 21,428,571 bytes / second
    * 1,000,000 iterations: in ~ 5.9 seconds vs. JSON.NET 5.0 r8 in ~ 8.3 seconds vs. ServiceStack in ~ 6.1 seconds
        * Yields System.Text.Json.JsonParser's throughput : 21,265,822 bytes / second

* "Loop" Test of small JSON (deserializing x times the JSON contained in the small.json.txt file ~ 3.5 KB):
    * 10,000 iterations: in ~ 1.2 second vs. JSON.NET 5.0 r8 in ~ 2.2 seconds vs. ServiceStack... N/A
        * Yields System.Text.Json.JsonParser's throughput : 27,657,587 bytes / second
    * 100,000 iterations: in ~ 12.4 seconds vs. JSON.NET 5.0 r8... OutOfMemoryException vs. ServiceStack... N/A
        * Yields System.Text.Json.JsonParser's throughput : 28,028,391 bytes / second

* Note: fathers.json.txt was generated using:
    * http://experiments.mennovanslooten.nl/2010/mockjson/tryit.html

* "Fathers" Test (12 MB JSON file):
    * Parsed in ~ 275 milliseconds vs. JSON.NET 5.0 r8 in ~ 500 milliseconds vs. ServiceStack in ~ 575 milliseconds
        * Yields System.Text.Json.JsonParser's throughput : 45,335,269 bytes / second

* "Huge" Test (180 MB JSON file):
    * Parsed in ~ 9.75 seconds vs. JSON.NET 5.0 r8... OutOfMemoryException vs. ServiceStack... N/A
        * Yields System.Text.Json.JsonParser's throughput : 19,899,152 bytes / second

As for huge.json.txt, it is just a copy of this file:

https://github.com/zeMirco/sf-city-lots-json

Roadmap
-------

None worth of the name for now. Yet, one thing I'm craving (so to speak) to support as soon as possible, is the ability to deserialize into anonymous types. I've done it before, but I need more thinking about it (vs. my first attempt), in order to avoid the potential significant loss in performances I'm aware of.

Another, quite obvious, item on the wish list is to provide some support for custom deserialization. Design-wise, I do have a preference for a [functional approach](http://en.wikipedia.org/wiki/First-class_function#Language_support) which would be based on (more or less) arbitrary "reviver" [delegate types](http://en.wikipedia.org/wiki/Delegate_%28CLI%29#Technical_Implementation_Details), for use by the parser's methods. Again, the main implementation challenge will be not drifting too much from the current speed performance ballpark.

Questions?
----------

ysharp {dot} design {at} gmail {dot} com
