System.Text.Json
================

"Small is beautiful." (Well, yes :)
-----------------------------------

A minimalistic, and pretty fast JSON parser/deserializer.

The complete source code (with a Visual Studio 2010 solution) is pretty short, coming with a few tests, and test data, all in the "JsonTest" folder.

Goal
----

This aims at parsing textual JSON data, and to deserialize it into our (strongly typed) [POCO](http://en.wikipedia.org/wiki/Plain_Old_CLR_Object)s, as fast as possible.

The only tiers of interest for this parser are the desktop/server tiers. There are other JSON librairies with good performances, and already well tested/documented, which support mobile devices running more limited flavors of .NET ([JSON.NET](http://james.newtonking.com/json) and [ServiceStack](https://servicestack.net/) come to mind).

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

Consists in three generic *instance* methods:

    T Parse<T>(string input)

and

    T Parse<T>(System.IO.TextReader input)

and

    T Parse<T>(System.IO.Stream input)
    
(The capability to parse JSON text coming thru a reader (or stream) being clearly a must-have, past a certain size of payload - "*have mercy for your CLR's large object heap*", [if you see what I mean...](http://msdn.microsoft.com/en-us/magazine/cc534993.aspx))

Note if you don't care about deserializing whatever input JSON into POCOs, you can just call these methods with

    object

for the generic type argument, as in, e.g.:

    parser.Parse<object>(@" [ { ""greetings"": ""hello"" } ] ")

It will then deserialize the input into a tree made of

    Dictionary<string, object>
   
instances, for JSON *objects* which are unordered sets of name/value pairs, and of

    List<object>
   
instances, for JSON *arrays* which are ordered collections of values.

The leaves will be from any of these types:

    null type
    bool
    string

In this case of, say, "loosely typed" deserialization, you may ask: "But what about the JSON number literals in the input - why deserializing them as *strings*?"

I would then ask my turn: "In absence of more specific type information about the deserialization target, who's likely best placed to decide whether the number after the colon, in

    "SomeNumber": 123.456

should be deserialized into a *System.Single*, a *System.Double*, or a *System.Decimal* (but obviously not into some integer) - is it this parser, or is it your application?"

In my opinion, it's the latter.

(Also, one can read [this very informative post of Eric Lippert](http://ericlippert.com/2013/07/25/what-is-the-type-of-the-null-literal/) about the so-called "null type", btw.)

Performances
------------

Following below: a few figures, outcome averages of the tests provided here.

Consistently enough, I also obtain quite similar performance ratios for the same 3 parsers/deserializers when compared one-to-one, after I adapt (for this JsonParser, which doesn't provide object-to-JSON text *serialization*) and run "the burning monk's" simple speed tester for JSON, which can be found at
:

http://theburningmonk.com/2013/09/binary-and-json-serializer-benchmarks-updated/

Note such figures (either the "burning monk's", or these below) are always much dependent on the test data and the way testing is performed. YMMV, so it's always a good idea to make *your own* benchmarks, using *your* test data, in the data "shape" you're interested in, and that you expect to encounter with a good probability in your domain.

Versions used: JSON.NET **v5.0 r8** and ServiceStack **v3.9.59**

(For a .NET 4.0 target, on a humble Ideapad Intel Core i5 CPU @ 2.50GHz, 6 GB RAM, running Win7 64bit, with a ~ 98%..99% idle CPU)

So, without further ado:

* "Loop" Test of tiny JSON (deserializing x times the JSON contained in the tiny.json.txt file = 126 bytes) - with POCO target (1 class):
    * 10,000 iterations: in ~ 65 milliseconds (pretty good)
        * vs. JSON.NET in ~ 250 milliseconds
        * vs. ServiceStack in ~ 125 milliseconds
        * Which yields System.Text.Json.JsonParser's throughput : 20,322,580 bytes / second
    * 100,000 iterations: in ~ 600 milliseconds (not bad)
        * vs. JSON.NET in ~ 900 milliseconds
        * vs. ServiceStack in ~ 650 milliseconds
        * Which yields System.Text.Json.JsonParser's throughput : 21,428,571 bytes / second
    * 1,000,000 iterations: in ~ 5.9 seconds (not bad either)
        * vs. JSON.NET in ~ 8.3 seconds
        * vs. ServiceStack in ~ 6.1 seconds
        * Which yields System.Text.Json.JsonParser's throughput : 21,265,822 bytes / second
    * [tiny.json.txt](https://raw.github.com/ysharplanguage/FastJsonParser/master/JsonTest/TestData/tiny.json.txt)

* "Loop" Test of small JSON (deserializing x times the JSON contained in the small.json.txt file ~ 3.5 KB) - "loosely-typed" deserialization:
    * 10,000 iterations: in ~ 1.2 second (pretty good)
        * vs. JSON.NET in ~ 2.2 seconds
        * vs. ServiceStack... N / A
        * Which yields System.Text.Json.JsonParser's throughput : 27,657,587 bytes / second
    * 100,000 iterations: in ~ 12.1 seconds (not bad)
        * vs. JSON.NET... OutOfMemoryException
        * vs. ServiceStack... N / A
        * Which yields System.Text.Json.JsonParser's throughput : 28,028,391 bytes / second
    * [small.json.txt](https://raw.github.com/ysharplanguage/FastJsonParser/master/JsonTest/TestData/small.json.txt) being just a copy of the "{ "web-app": { "servlet": [ ... ] ... } }" sample, at:
        * http://www.json.org/example.html

(unfortunately, quite unfamiliar with it, I'm still trying to understand how, in absence of POCOs, to have ServiceStack's willing to deserialize into trees of dictionaries and lists, just like we can do it easily with JSON.NET, the JavaScriptSerializer, or my parser here.)

* "Fathers" Test (12 MB JSON file) - with POCO targets (4 distinct classes):
    * Parsed in ~ 275 milliseconds (!)
        * vs. JSON.NET in ~ 500 milliseconds
        * vs. ServiceStack in ~ 575 milliseconds
        * Which yields System.Text.Json.JsonParser's throughput : 45,335,269 bytes / second
    * Note: [fathers.json.txt](https://github.com/ysharplanguage/FastJsonParser/blob/master/JsonTest/TestData/fathers.json.txt) was generated using this nifty online helper:
        * http://experiments.mennovanslooten.nl/2010/mockjson/tryit.html

(The latter, "fathers" test, is the one with the results that intrigued me the most the very first few times I ran it - and it still does - but I haven't taken the time yet to do more serious profiling to explain these timing differences I didn't quite expect, which are also interesting if only between JSON.NET vs. ServiceStack...)

* "Huge" Test (180 MB JSON file) - "loosely-typed" deserialization:
    * Parsed in ~ 8.7 seconds (not bad)
        * vs. JSON.NET... OutOfMemoryException
        * vs. ServiceStack... N / A
        * Which yields System.Text.Json.JsonParser's throughput : 21,778,542 bytes / second
    * As for huge.json.txt, it is just a copy of this file:
        * https://github.com/zeMirco/sf-city-lots-json

Roadmap
-------

None really worth of the name for now.

But... One thing I'm craving (so to speak) to support as soon as possible, is the ability to deserialize into [C#'s anonymous types](http://en.wikipedia.org/wiki/Anonymous_type). I've done it before, but I need to put more thinking into it (vs. [my first, other attempt at it](https://code.google.com/p/ysharp/source/browse/trunk/TestJSONParser/%28System.Text.Json%29Parser.cs)), in order to avoid the potential significant loss in performances I'm aware of.

Another, quite obvious, item on the wish list is to provide some support for custom deserialization. Design-wise, I do have a preference for a [functional approach](http://en.wikipedia.org/wiki/First-class_function#Language_support) which would be based on (more or less) arbitrary "reviver" [delegate types](http://en.wikipedia.org/wiki/Delegate_%28CLI%29#Technical_Implementation_Details), for use by the parser's methods (for typical IoC/callback use cases).

Again, the main implementation challenge will be not drifting too much from the current speed performance ballpark.

In any case, I don't plan to make this small JSON deserializer as general-purpose and extensible as JSON.NET or ServiceStack's, I just want to keep it as simple, short and fast as possible for my future needs (read on).

"But, why such an ad-hoc parser? Why 'speed', anyway?"
------------------------------------------------------

"... Why do you even care, when you already have the excellent, fast, feature-rich JSON.NET and ServiceStack around?"

Indeed, pure parsing + deserialization speed isn't in fact my *long term* goal, or not for any *arbitrary* JSON input, anyway. For another, and broader project - still in design stage - that I have, I plan to use JSON as a "malleable" IR (intermediary representation) for code and meta data transformations that I will need to make happen in-between a high level source language (e.g., C#, or F#,...) and the target CIL (or, even, some other low-level target).

JSON.NET's deserialization performances are great, and so are ServiceStack's - really, they are already - but I would like to have something of my own in my toolbox much smaller/manageable (in terms of # of SLOC), and simpler to extend, for whatever unforeseen requirements of the deserialization process (from JSON text into CLR types and values) I may have to tackle.

Put otherwise: by design, thru their respective feature sets, both JSON.NET's and ServiceStack's make (and that's perfectly natural) a number of assumptions for the most frequent use cases of custom deserialization they're interested in, with this or that "shape" of JSON, and how to easily solve these problems in a more or less generic fashion (i.e., via their "custom converters", and the like).

From an earlier experiment on that other project, I already know that my problem space will be quite different. I found out that I will *not* need a *generic* way to solve a *specific* deserialization sub-problem very efficiently (which nobody can really do - there is "no silver bullet" / "one size fits all" for that), but instead I will only need a *specific* way to solve it, by extending this small parser's functionality only where and how that's exactly needed (while trying to maintain its good performances).

Finally, this parser/deserializer is/was a nice learning opportunity for me to verify by myself what I had read many times before, that is, where exactly the parsing slowdowns and memory consumption costs most often come from.

Other questions?
----------------

ysharp {dot} design {at} gmail {dot} com
