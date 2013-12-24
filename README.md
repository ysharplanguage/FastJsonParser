System.Text.Json
================

"Small is beautiful."
---------------------

This is a minimalistic and fast JSON parser/deserializer, for full .NET.

The complete source code of the parser is pretty short (in a [single source file less than 600 SLOC-long](https://github.com/ysharplanguage/FastJsonParser/blob/master/JsonTest/System.Text.Json/JsonParser.cs)) and comes with a few tests and sample data, all in the "JsonTest" folder.

Also available on NuGet
-----------------------

For convenience:

https://www.nuget.org/packages/System.Text.Json

Goal
----

This aims at parsing textual JSON data, and to deserialize it into our (strongly typed) [POCO](http://en.wikipedia.org/wiki/Plain_Old_CLR_Object)s, as fast as possible.

The only tiers of interest for this parser are the desktop/server tiers. There are other JSON librairies with good performances, and already well tested/documented, which ***also*** support mobile devices that run more limited flavors of .NET ([JSON.NET](http://james.newtonking.com/json) and [ServiceStack](https://github.com/ServiceStack/ServiceStack) come to mind).

This JSON parser/deserializer aims only at staying "simple", short, fast, and for use by some desktop tooling or server-side code that would require the full .NET anyway, and unlikely to ever be usable on mobile devices (unless those can eventually run the full .NET).

Early development status warning
--------------------------------

Although it is promisingly fast, please note this parser/deserializer is still experimental.

I do *not* recommend it for any use in production, at this stage. This may or may not evolve soon, but for one thing to begin with, it's in need of more extensive JSON conformance tests.

That being said, you can feel free to fork / bugfix / augment / improve it at your own will.

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
    
(IMO, the capability to parse JSON text coming thru a reader (or stream) is clearly a must-have, past a certain size of payload - "*have mercy for your CLR's large object heap*", [if you see what I mean](http://msdn.microsoft.com/en-us/magazine/cc534993.aspx).)

Note that if you don't care (i.e., don't need / don't want to bother) deserializing whatever input JSON into POCOs, you can then just call these methods with

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

In this case of, say, "loosely typed" deserialization, one may ask: "But what about the JSON number literals in the input - why deserializing them as *strings*?"

I would then ask - ".... in absence of more specific type information about the deserialization target, *"who"* is likely best placed to decide whether the number after the colon, in

    "SomeNumber": 123.456

should be deserialized into a *System.Single*, a *System.Double*, or a *System.Decimal* (but obviously not into some integer) - is it this parser, or is it the application?"

In my opinion, *in that case*, it's the application.

(Also, one can read [this very informative post of Eric Lippert](http://ericlippert.com/2013/07/25/what-is-the-type-of-the-null-literal/) about the so-called "null type".)

<a name="Performances"></a>

Performances
------------

Following below: a few figures, the outcome *average numbers* (only) that I obtain from the tests provided here, along with a few remarks.

Consistently enough, I also obtain similar performance ratios for the same 4 parsers/deserializers when compared one-to-one, after I adapt (for this JsonParser doesn't provide object-to-JSON text *serialization*) and I run "the burning monk's" simple speed tester for JSON, which has been around for a while and can be found at
:

http://theburningmonk.com/2013/09/binary-and-json-serializer-benchmarks-updated/

***Disclaimer***

Note such figures (either the "burning monk's", or these below) can always - potentially - be much dependent on the test data at hand, and/or the way testing is performed.

Of course, YMMV, so it's always a good idea to make *your own* benchmarks, using *your test data*, in the data "shape" you're interested in, and that you expect to encounter with *a good probability* in your domain.

***Other libraries, versions used ("the competition")***

* Out-of-the-box .NET 4.0's [JavaScriptSerializer](http://msdn.microsoft.com/en-us/library/system.web.script.serialization.javascriptserializer%28v=vs.100%29.aspx)
* [JSON.NET](http://james.newtonking.com/json) **v5.0 r8**, and
* [ServiceStack](https://github.com/ServiceStack/ServiceStack) **v3.9.59**

***Executable target, and H/W used***

.NET 4.0 target, on a humble Ideapad Intel Core i5 CPU @ 2.50GHz, 6 GB RAM, running Win7 64bit, with a ~ 98%..99% idle CPU (a nice enough personal laptop, but not exactly a beast of speed nowadays).

Just for comparison with the native code world, on the third row of the table below I also give (in one measure only, for an idea) the throughput achieved by <a href="http://www.ohler.com/oj/">Peter's "Oj"</a> ("Oj" is a C extension to Ruby of his) for 100,000 parses over his own sample JSON ([_oj-highly-nested.json.txt](https://raw.github.com/ysharplanguage/FastJsonParser/master/JsonTest/TestData/_oj-highly-nested.json.txt)) that I've reused to prepare this benchmark ( cf. http://www.ohler.com/dev/oj_misc/performance_strict.html )

So, without further ado... (larger figure means faster)

<table border="1" width="100%">
<tr>
<th>Test /<br/>JSON size /<br/># Iterations /<br/>POCO or<br/>loosely-typed?</th>
<th>JavaScript<br/>Serializer</th>
<th>JSON.NET</th>
<th>ServiceStack</th>
<th><a href="https://github.com/ysharplanguage/FastJsonParser/blob/master/JsonTest/System.Text.Json/JsonParser.cs">JsonParser</a></th>
<th><a href="http://www.ohler.com/oj/">Peter's  "Oj"</a><br/>(C extension to Ruby)</th>
</tr>
<tr>
<td><strong>_oj-highly-nested.json</strong><br/>257 bytes<br/>10,000 iter.<br/>Loosely-typed</td>
<td>11.4 K parses/sec</td>
<td>12.4 K parses/sec</td>
<td>N / A</td>
<td>42.5 K parses/sec</td>
<td>N / A</td>
</tr>
<tr>
<td><strong>_oj-highly-nested.json</strong><br/>257 bytes<br/>100,000 iter.<br/>Loosely-typed</td>
<td>11.0 K parses/sec</td>
<td>12.6 K parses/sec</td>
<td>N / A</td>
<td>36.9 K parses/sec</td>
<td><strong>76.6 K parses/sec</strong><br/>(on his machine)</td>
</tr>
<tr>
<td><strong>boon-small.json</strong><br/>79 bytes<br/>1,000,000 iter.<br/><strong>POCO</strong><br/>(<a href="#POCOs">see below</a>)</td>
<td>31.7 K parses/sec</td>
<td>139.9 K parses/sec</td>
<td>180.2 K parses/sec</td>
<td>261.1 K parses/sec</td>
<td>N / A</td>
</tr>
<tr>
<td><strong>boon-small.json</strong><br/>79 bytes<br/>10,000,000 iter.<br/><strong>POCO</strong><br/>(<a href="#POCOs">see below</a>)</td>
<td>33.1 K parses/sec</td>
<td>143.3 K parses/sec</td>
<td>182.5 K parses/sec</td>
<td>271.0 K parses/sec</td>
<td>N / A</td>
</tr>
<tr>
<td><strong>tiny.json</strong><br/>126 bytes<br/>10,000 iter.<br/><strong>POCO</strong><br/>(<a href="#POCOs">see below</a>)</td>
<td>18.2 K parses/sec</td>
<td>40.0 K parses/sec</td>
<td>80.0 K parses/sec</td>
<td>178.6 K parses/sec</td>
<td>N / A</td>
</tr>
<tr>
<td><strong>tiny.json</strong><br/>126 bytes<br/>100,000 iter.<br/><strong>POCO</strong><br/>(<a href="#POCOs">see below</a>)</td>
<td>20.4 K parses/sec</td>
<td>111.1 K parses/sec</td>
<td>153.8 K parses/sec</td>
<td>181.8 K parses/sec</td>
<td>N / A</td>
</tr>
<tr>
<td><strong>tiny.json</strong><br/>126 bytes<br/>1,000,000 iter.<br/><strong>POCO</strong><br/>(<a href="#POCOs">see below</a>)</td>
<td>20.1 K parses/sec</td>
<td>120.5 K parses/sec</td>
<td>163.9 K parses/sec</td>
<td>178.5 K parses/sec</td>
<td>N / A</td>
</tr>
<tr>
<td><strong>small.json</strong><br/>3.5 KB<br/>10,000 iter.<br/>Loosely-typed</td>
<td>1.5 K parses/sec</td>
<td>4.5 K parses/sec</td>
<td>N / A</td>
<td>8.8 K parses/sec</td>
<td>N / A</td>
</tr>
<tr>
<td><strong>small.json</strong><br/>3.5 KB<br/>100,000 iter.<br/>Loosely-typed</td>
<td>1.5 K parses/sec</td>
<td>Exception</td>
<td>N / A</td>
<td>8.6 K parses/sec</td>
<td>N / A</td>
</tr>
<tr>
<td><strong>fathers.json</strong><br/>12.4 MB<br/>(single parse)<br/><strong>POCO</strong><br/>(<a href="#POCOs">see below</a>)</td>
<td>5.0 MB/sec</td>
<td>26.0 MB/sec</td>
<td>22.6 MB/sec</td>
<td>45.5 MB/sec</td>
<td>N / A</td>
</tr>
<tr>
<td><strong>huge.json</strong><br/>180 MB<br/>(single parse)<br/>Loosely-typed</td>
<td>3.0 MB/sec</td>
<td>Exception</td>
<td>N / A</td>
<td>22.7 MB/sec</td>
<td>N / A</td>
</tr>
</table>

The same, with the test files and timings details: 

(smaller time means faster)

***Peter's "Oj Strict Mode Performance" test***

* "Loop" Test of Peter's "Oj Strict Mode Performance" sample (deserializing x times the JSON contained in the [_oj-highly-nested.json.txt](https://raw.github.com/ysharplanguage/FastJsonParser/master/JsonTest/TestData/_oj-highly-nested.json.txt) file = 257 bytes) - "loosely-typed" deserialization:
    * 10,000 iterations: in ~ 235 milliseconds
        * vs. JavaScriptSerializer in ~ 875 milliseconds
        * vs. JSON.NET in ~ 805 milliseconds
        * vs. ServiceStack... N / A
        * (Which yields System.Text.Json.JsonParser's throughput : 10,982,905 bytes / second)
    * 100,000 iterations: in ~ 2.71 seconds
        * vs. JavaScriptSerializer in ~ 9.05 seconds
        * vs. JSON.NET in ~ 7.95 seconds
        * vs. ServiceStack... N / A
        * (Which yields System.Text.Json.JsonParser's throughput : 9,486,895 bytes / second)
    * [_oj-highly-nested.json.txt](https://raw.github.com/ysharplanguage/FastJsonParser/master/JsonTest/TestData/_oj-highly-nested.json.txt) comes from Peter's sample and tests, at:
        * http://www.ohler.com/dev/oj_misc/performance_strict.html

I find JSON data sample from Peter interesting for its non-trivial "shape", and the presence of these "highly nested" arrays (so to speak) at end of the payload:

    {"a":"Alpha","b":true,"c":12345,"d":[true,[false,[-123456789,null],3.9676,["Something else.",false],null]],"e":{"zero":null,"one":1,"two":2,"three":[3],"four":[0,1,2,3,4]},"f":null,"h":{"a":{"b":{"c":{"d":{"e":{"f":{"g":null}}}}}}},"i":[[[[[[[null]]]]]]]}

As for that "vs. ServiceStack in... N / A":

unfortunately, quite unfamiliar with ServiceStack, I'm still trying to understand how, in absence of POCOs, to have it deserialize into merely trees of dictionaries + lists or arrays (just as we can do very easily with JSON.NET, the JavaScriptSerializer, or my parser here).

***Rick's "Boon" small test***

* Rick's "Boon" small test, slightly modified (deserializing x times the JSON contained in the [boon-small.json.txt](https://raw.github.com/ysharplanguage/FastJsonParser/master/JsonTest/TestData/boon-small.json.txt) file = 79 bytes) - with POCO target (1 class):
    * 1,000,000 iterations: in ~ 3.83 seconds
        * vs. JavaScriptSerializer in ~ 31.5 seconds (bad omen #1)
        * vs. JSON.NET in ~ 7.15 seconds
        * vs. ServiceStack in ~ 5.55 seconds
        * (Which yields System.Text.Json.JsonParser's throughput : 20,632,018 bytes / second)
    * 10,000,000 iterations: in ~ 36.9 seconds
        * vs. JavaScriptSerializer in ~ 302.3 seconds (bad omen #2)
        * vs. JSON.NET in ~ 69.8 seconds
        * vs. ServiceStack in ~ 54.8 seconds
        * (Which yields System.Text.Json.JsonParser's throughput : 21,379,664 bytes / second)

Rick's original test can be found at:

http://rick-hightower.blogspot.com/2013/11/benchmark-for-json-parsing-boon-scores.html

Note Rick is one of our fellows from the Java realm - and from his own comparative figures that I eventually noticed, it does seem that [Rick's "Boon"](https://github.com/RichardHightower/json-parsers-benchmark/blob/master/README.md) is also, indeed, *pretty fast*, among the number of Java toolboxes for JSON.

***"Tiny JSON" test***

* "Loop" Test of tiny JSON (deserializing x times the JSON contained in the [tiny.json.txt](https://raw.github.com/ysharplanguage/FastJsonParser/master/JsonTest/TestData/tiny.json.txt) file = 126 bytes) - with POCO target (1 class):
    * 10,000 iterations: in ~ 56 milliseconds (pretty good)
        * vs. JavaScriptSerializer in ~ 550 milliseconds (bad omen #3)
        * vs. JSON.NET in ~ 250 milliseconds
        * vs. ServiceStack in ~ 125 milliseconds
        * (Which yields System.Text.Json.JsonParser's throughput : 22,321,428 bytes / second)
    * 100,000 iterations: in ~ 550 milliseconds (not bad)
        * vs. JavaScriptSerializer in ~ 4.9 seconds (ahem!)
        * vs. JSON.NET in ~ 900 milliseconds
        * vs. ServiceStack in ~ 650 milliseconds
        * (Which yields System.Text.Json.JsonParser's throughput : 22,727,272 bytes / second)
    * 1,000,000 iterations: in ~ 5.6 seconds (not bad either)
        * vs. JavaScriptSerializer in 49.8 seconds (cough!)
        * vs. JSON.NET in ~ 8.3 seconds
        * vs. ServiceStack in ~ 6.1 seconds
        * (Which yields System.Text.Json.JsonParser's throughput : 22,301,516 bytes / second)
    * [tiny.json.txt](https://raw.github.com/ysharplanguage/FastJsonParser/master/JsonTest/TestData/tiny.json.txt)

***"Small JSON" test***

* "Loop" Test of small JSON (deserializing x times the JSON contained in the [small.json.txt](https://raw.github.com/ysharplanguage/FastJsonParser/master/JsonTest/TestData/small.json.txt) file ~ 3.5 KB) - "loosely-typed" deserialization:
    * 10,000 iterations: in ~ 1.14 second (pretty good)
        * vs. JavaScriptSerializer in ~ 6.7 seconds
        * vs. JSON.NET in ~ 2.2 seconds
        * vs. ServiceStack... N / A
        * (Which yields System.Text.Json.JsonParser's throughput : 31,174,408 bytes / second)
    * 100,000 iterations: in ~ 11.7 seconds (not bad)
        * vs. JavaScriptSerializer in ~ 66.5 seconds
        * vs. JSON.NET... OutOfMemoryException
        * vs. ServiceStack... N / A
        * (Which yields System.Text.Json.JsonParser's throughput : 30,318,786 bytes / second)
    * [small.json.txt](https://raw.github.com/ysharplanguage/FastJsonParser/master/JsonTest/TestData/small.json.txt) being just a copy of the "{ "web-app": { "servlet": [ ... ] ... } }" sample, at:
        * http://www.json.org/example.html

***"Fathers JSON" test***

* "Fathers" Test (12 MB JSON file) - with POCO targets (4 distinct classes):
    * Parsed in ~ 285 milliseconds (!)
        * vs. JavaScriptSerializer in ~ 2.6 seconds
        * vs. JSON.NET in ~ 500 milliseconds
        * vs. ServiceStack in ~ 575 milliseconds
        * (Which yields System.Text.Json.JsonParser's throughput : 45,494,340 bytes / second) (!)
    * Note: [fathers.json.txt](https://github.com/ysharplanguage/FastJsonParser/blob/master/JsonTest/TestData/fathers.json.txt) was generated using this nifty online helper:
        * http://experiments.mennovanslooten.nl/2010/mockjson/tryit.html

The latter, "fathers" test, is the one with the results that intrigued me the most the very first few times I ran it - and it still does. However, I haven't taken the time yet to do more serious profiling to fully explain these timing differences that I didn't expect to be *that significant*.

They are also interesting to notice, if only when comparing JSON.NET vs. ServiceStack.

***"Huge JSON" test***

* "Huge" Test (180 MB JSON file) - "loosely-typed" deserialization:
    * Parsed in ~ 8.3 seconds (not bad)
        * vs. JavaScriptSerializer in ~ 62 seconds
        * vs. JSON.NET... OutOfMemoryException
        * vs. ServiceStack... N / A
        * (Which yields System.Text.Json.JsonParser's throughput : 22,798,921 bytes / second)
    * As for huge.json.txt, it is just a copy of this file:
        * https://github.com/zeMirco/sf-city-lots-json

<a name="POCOs"></a>

Target POCOs (those used by some of the above tests)
----------------------------------------------------

Here they are, for the curious and/or impatient:

        // Used in the "boon-small.json" test
        public class BoonSmall
        {
            public string debug { get; set; }
            public IList<int> nums { get; set; }
        }


        // Used in the "tiny.json" test
        public class Person
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public bool Married { get; set; }
            public string Address { get; set; }
            // Just to be sure we support that one, too:
            public IEnumerable<int> Scores { get; set; }
            public object Data { get; set; }
        }


        // Used in the "fathers.json" test
        public class FathersData
        {
            public Father[] fathers { get; set; }
        }


        // Used in the "fathers.json" test
        public class Father
        {
            public int id { get; set; }
            public string name { get; set; }
            public bool married { get; set; }
            // Lists...
            public List<Son> sons { get; set; }
            // ... or arrays for collections, that's fine:
            public Daughter[] daughters { get; set; }
        }


        // Used in the "fathers.json" test
        public class Son
        {
            public int age { get; set; }
            public string name { get; set; }
        }


        // Used in the "fathers.json" test
        public class Daughter
        {
            public int age { get; set; }
            public string name { get; set; }
        }

Roadmap
-------

None really worth of the name for now.

However, one thing I would like to support as soon as I can, is the ability to deserialize into [C#'s anonymous types](http://en.wikipedia.org/wiki/Anonymous_type). I've done it before, but I need to put more thinking into it (vs. [my first, other attempt at it](https://code.google.com/p/ysharp/source/browse/trunk/TestJSONParser/%28System.Text.Json%29Parser.cs)), in order to avoid the potential significant loss in performances I'm aware of.

Another, quite obvious, item on the wish list is to provide some support for custom deserialization. Design-wise, I do have a preference for a [functional approach](http://en.wikipedia.org/wiki/First-class_function#Language_support) which would be based on (more or less) arbitrary "reviver" [delegate types](http://en.wikipedia.org/wiki/Delegate_%28CLI%29#Technical_Implementation_Details), for use by the parser's methods (for typical IoC/callback use cases).

Again, the main implementation challenge will be not drifting too much from the current speed performance ballpark.

In any case, I don't plan to make this small JSON deserializer as general-purpose and extensible as JSON.NET or ServiceStack's, I just want to keep it as simple, short, and fast as possible for my present and future needs (read on).

"But, why this ad-hoc parser, and 'need for speed', anyway?"
------------------------------------------------------------

"... Why do you even care, when you already have the excellent, fast, feature-rich [JSON.NET](http://james.newtonking.com/json) and [ServiceStack](https://github.com/ServiceStack/ServiceStack) around?"

Indeed, pure parsing + deserialization speed isn't in fact my *long term* goal, or not for any *arbitrary* JSON input, anyway. For another, and broader project - still in design stage - that I have, I plan to use JSON as a "malleable" IR (intermediary representation) for code and meta data transformations that I will need to make happen in-between a high level source language (e.g., C#, or F#,...) and the target CIL (or, even, some other low-level target).

[JSON.NET](http://james.newtonking.com/json)'s deserialization performances are great, and so are [ServiceStack](https://github.com/ServiceStack/ServiceStack)'s - [really, they are, already](http://theburningmonk.com/benchmarks/) - but I would like to have something of my own in my toolbox much smaller/more manageable (in terms of # of SLOC), and simpler to extend, for whatever unforeseen requirements of the deserialization process (from JSON text into CLR types and values) I may have to tackle.

From an earlier experiment on that other project, I found out that I will *not* need a *generic* way to solve a *specific* deserialization sub-problem very efficiently (which nobody can really do - as there is "no silver bullet" / "one size fits all" for that matter), but instead I will only need a *specific* way to solve it efficiently, by extending this small parser's functionality only where and how that's exactly needed (while trying to maintain its good performances).

Finally, this parser/deserializer is/was a nice learning opportunity for me with parsing JSON, and to verify by myself once again [what I had read about](http://msdn.microsoft.com/en-us/magazine/cc507639.aspx) and experienced many times before. That is: never try to merely guess about performances, but instead always do your best to measure and to find out *where exactly* the parsing and deserialization slowdowns (and memory consumption costs) *actually* come from.

Other questions?
----------------

Let yourself go:

ysharp {dot} design {at} gmail {dot} com
