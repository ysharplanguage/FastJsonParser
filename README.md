System.Text.Json
================

* <a href="#Overview">Overview</a>
* <a href="#Goal">Goal</a>
* <a href="#Interface">Public interface</a>
* <a href="#JSONPath">JSONPath support</a>
* <a href="#Performance">Performance</a>
    * <a href="#PerfOverview">Speed Tests Results : Overview</a>
    * <a href="#PerfDetailed">Speed Tests Results : Detailed</a>
* <a href="#POCOs">Test target POCOs</a>
* <a href="#Roadmap">Roadmap</a>
* <a href="#Background">Background</a>

<a name="Overview"></a>

"Small is beautiful."
---------------------

This is a minimalistic and fast JSON parser/deserializer, for full .NET.

The complete source code of the parser is pretty short (in a [single source file less than 1,500 SLOC-long](https://github.com/ysharplanguage/FastJsonParser/blob/master/JsonTest/System.Text.Json/JsonParser.cs)) and comes with some speed tests and their sample data, all in the "JsonTest" and "TestData" folders.

The console test program includes a few unit tests (for both nominal cases vs. error cases), and it will attempt to execute them before the speed tests - see [ParserTests.cs](https://github.com/ysharplanguage/FastJsonParser/blob/master/JsonTest/ParserTests.cs).

Do not hesitate to add more of the former (i.e., unit tests) and to raise here whatever issues you may find.

There's a link to this repository at [json.org](http://www.json.org/) (in the C# implementations section).

Also available on NuGet
-----------------------

For convenience:

https://www.nuget.org/packages/System.Text.Json

<a name="Goal"></a>

Goal
----

This aims at parsing textual JSON data, and to deserialize it into our (strongly typed) [POCO](http://en.wikipedia.org/wiki/Plain_Old_CLR_Object)s, as fast as possible.

For now(~), the only tiers of interest for this parser are the desktop/server tiers. There are other JSON librairies with good performance, and already well tested/documented, which *also* support mobile devices that run more limited flavors of .NET ([JSON.NET](http://james.newtonking.com/json) and [ServiceStack](https://github.com/ServiceStack/ServiceStack) come to mind).

(~ i.e., until it gets stable enough and I hear people's interest for it to run elsewhere)

This JSON parser/deserializer thus aims first at staying "simple", short, fast, and for use by some desktop tooling or server-side code that would require the full .NET anyway, and unlikely to ever be usable on mobile devices (unless those can eventually run the full .NET).

Nevertheless, [Sami](https://github.com/sami1971) has just recently been able to do a quick performance test on his Android device (as the code could luckily compile for it *as-is*), and came up with a few benchmark results (~ a dozen or so) which do seem encouraging - i.e., see this thread of Xamarin's Android forum:

http://forums.xamarin.com/discussion/comment/39011/#Comment_39011

Early development status warning
--------------------------------

Although it is promisingly fast, lightweight, and easy to use, please note this parser/deserializer is still experimental.

I do *not* recommend it for any use in production, at this point. This may or may not evolve soon, but for one simple thing to begin with, it's in need of more extensive JSON conformance tests.

That being said, just feel free to [fork / bugfix / augment / improve it](https://github.com/ysharplanguage/FastJsonParser/graphs/contributors) at your own will.

Of course, I welcome your informed input and feedback.

Please read the [LICENSE](https://github.com/ysharplanguage/FastJsonParser/blob/master/LICENSE.md).

<a name="Interface"></a>

Public interface
----------------

Consists mainly in these six (three generic ones, and three non-generic ones) *instance* methods:

    T Parse<T>(string input)

and

    T Parse<T>(System.IO.TextReader input)

and

    T Parse<T>(System.IO.Stream input)

and

    object Parse(string input)

and

    object Parse(System.IO.TextReader input)

and

    object Parse(System.IO.Stream input)

IMO, the capability to parse JSON text coming thru a reader (or stream) is clearly a must-have, past a certain size of payload - "*have mercy for your CLR's large object heap*", [if you see what I mean](http://msdn.microsoft.com/en-us/magazine/cc534993.aspx).

Note that if you don't care (i.e., don't need / don't want to bother) deserializing whatever input JSON into POCOs, you can then just call these methods with

    object

for the generic type argument, as in, e.g.:

    parser.Parse<object>(@" [ { ""greetings"": ""hello"" } ] ")

or, equivalently, just:

    parser.Parse(@" [ { ""greetings"": ""hello"" } ] ")
    
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

I would then ask - ".... in absence of more specific type information about the deserialization target, *"who"* is likely best placed to decide whether the number after the colon, in

    "SomeNumber": 123.456

should be deserialized into a *System.Single*, a *System.Double*, or a *System.Decimal* (but obviously not into some integer) - is it this parser, or is it the application?"

In my opinion, *in that case*, it's the application.

(Also, one can read [this very informative post of Eric Lippert](http://ericlippert.com/2013/07/25/what-is-the-type-of-the-null-literal/) about the so-called "null type".)


<a name="JSONPath"></a>

JSONPath support
----------------

Since version 1.9.9.2, [JSONPath](http://goessner.net/articles/JsonPath) is also supported. E.g., for a classic example in four steps:

\#1 :

    public class Data
    {
        public int dummy { get; set; }
        public Store store { get; set; }
    }


    public class Store
    {
        public Book[] book { get; set; }
        public Bicycle bicycle { get; set; }
    }


    public class Book
    {
        public string category { get; set; }
        public string author { get; set; }
        public string title { get; set; }
        public decimal price { get; set; }
    }


    public class Bicycle
    {
        public string color { get; set; }
        public decimal price { get; set; }
    }

\#2 :

            string input = @"
              { ""store"": {
                    ""book"": [ 
                      { ""category"": ""reference"",
                            ""author"": ""Nigel Rees"",
                            ""title"": ""Sayings of the Century"",
                            ""price"": 8.95
                      },
                      { ""category"": ""fiction"",
                            ""author"": ""Evelyn Waugh"",
                            ""title"": ""Sword of Honour"",
                            ""price"": 12.99
                      },
                      { ""category"": ""fiction"",
                            ""author"": ""Herman Melville"",
                            ""title"": ""Moby Dick"",
                            ""isbn"": ""0-553-21311-3"",
                            ""price"": 8.99
                      },
                      { ""category"": ""fiction"",
                            ""author"": ""J. R. R. Tolkien"",
                            ""title"": ""The Lord of the Rings"",
                            ""isbn"": ""0-395-19395-8"",
                            ""price"": 22.99
                      }
                    ],
                    ""bicycle"": {
                      ""color"": ""red"",
                      ""price"": 19.95
                    }
              }
            }
            ";

\#3 :

            JsonPathScriptEvaluator evaluator =
                delegate(string script, object value, string context)
                {
                    return
                    (
                        ((value is Type) && (context == script))
                        ?
                        ExpressionParser.Parse((Type)value, script, true, typeof(Data).Namespace).Compile()
                        :
                        null
                    );
                };
            JsonPathSelection scope;
            JsonPathNode[] nodes;

\#4 :

            var typed = new JsonParser().Parse<Data>(input); // (Data typed = ...)
            scope = typed.ToJsonPath(evaluator);
            nodes = scope.SelectNodes("$.store.book[?(@.title == \"Moby Dick\")].price");
            System.Diagnostics.Debug.Assert
            (
                nodes != null &&
                nodes.Length == 1 &&
                nodes[0].Value is decimal &&
                (decimal)nodes[0].Value == 8.99m
            );

<a name="Performance"></a>

Performance
-----------

Following in the table below: a few figures, the outcome *average numbers* (only) that I obtain from the tests provided here.

Consistently enough, I also obtain similar performance ratios for the same 4 parsers/deserializers when compared one-to-one, after I adapt (for this JsonParser doesn't provide object-to-JSON text *serialization*) and I run "the burning monk's" simple speed tester for JSON, which can be found at:

http://theburningmonk.com/2014/09/binary-and-json-benchmarks-updated

<a name="PerfOverview"></a>
***Speed Tests Results : Overview***

<table border="1" width="100%">
<tr>
<th><a href="https://github.com/ysharplanguage/FastJsonParser/blob/master/JsonTest/System.Text.Json/JsonParser.cs">This<br/>JsonParser</a><br/>versus...</th>
<th>Microsoft's<br/><a href="http://msdn.microsoft.com/en-us/library/system.web.script.serialization.javascriptserializer%28v=vs.100%29.aspx">JavaScript Serializer</a></th>
<th>James Newton-King's<br/><a href="http://james.newtonking.com/json">JSON.NET</a></th>
<th>Demis<br/>Bellot's<br/><a href="https://github.com/ServiceStack/ServiceStack">ServiceStack</a></th>
<th><a href="https://github.com/ysharplanguage/FastJsonParser/blob/master/JsonTest/System.Text.Json/JsonParser.cs">This<br/>JsonParser</a></th>
<th>Peter Ohler's<br/><a href="http://www.ohler.com/oj/">"Oj"</a><br/>C extension<br/>to Ruby</th>
</tr>
<tr>
<td align="center"><strong>Performance<br/>+/- %</strong></td>
<td align="center"><strong>+ 425 %</strong><br/>(faster)</td>
<td align="center"><strong>+ 127 %</strong><br/>(faster)</td>
<td align="center"><strong>+ 32 %</strong><br/>(faster)</td>
<td align="center"><strong>=</strong></td>
<td align="center"><strong>- 108 %</strong><br/>(slower)</td>
</tr>
</table>

***Disclaimer***

Note such figures (either the "burning monk's", or the following) can always - potentially - be much dependent on the test data at hand, and/or the way testing is performed.

Of course, YMMV, so it's always a good idea to make *your own* benchmarks, using *your test data*, in the data "shape" you're interested in, and that you expect to encounter with *a good probability* in your domain.

***Other libraries, versions used ("the competition")***

* Out-of-the-box .NET 4.0's [JavaScriptSerializer](http://msdn.microsoft.com/en-us/library/system.web.script.serialization.javascriptserializer%28v=vs.100%29.aspx)
* [JSON.NET](http://james.newtonking.com/json) **v5.0 r8**, and
* [ServiceStack](https://github.com/ServiceStack/ServiceStack) **v3.9.59**

Before you try to run the speed tests against the test data provided, please note this repository does **not** include the binaries for the versions of JSON.NET and ServiceStack mentioned (you can obtain those from their respective links above).

***Executable target, and H/W used***

.NET 4.0 target, on a humble Ideapad Intel Core i5 CPU @ 2.50GHz, 6 GB RAM, running Win7 64bit, with a ~ 98%..99% idle CPU (a nice enough personal laptop, but not exactly a beast of speed nowadays).

Just for comparison out of curiosity, on the third row of the table below I also give (this one measure only, for a glimpse) the throughput achieved, in the native code realm, by <a href="http://www.ohler.com/oj/">Peter Ohler's "Oj"</a> ("Oj" - *Optimized JSON* : a C extension to Ruby) for 100,000 parses over his own JSON sample that I've reused to prepare this benchmark.

(Refer to [_oj-highly-nested.json.txt](https://raw.github.com/ysharplanguage/FastJsonParser/master/JsonTest/TestData/_oj-highly-nested.json.txt), copied from Peter's: http://www.ohler.com/dev/oj_misc/performance_strict.html)

<a name="PerfDetailed"></a>
***Speed Tests Results : Detailed***

So, without further ado... (larger figure - # parses per second - means faster)

<table border="1" width="100%">
<tr>
<th>Test /<br/>JSON size /<br/># Iterations /<br/>POCO or<br/>loosely-typed?</th>
<th>Microsoft's<br/><a href="http://msdn.microsoft.com/en-us/library/system.web.script.serialization.javascriptserializer%28v=vs.100%29.aspx">JavaScript<br/>Serializer</a></th>
<th>James<br/>Newton-<br/>King's<br/><a href="http://james.newtonking.com/json">JSON.NET</a></th>
<th>Demis<br/>Bellot's<br/><a href="https://github.com/ServiceStack/ServiceStack">ServiceStack</a></th>
<th><a href="https://github.com/ysharplanguage/FastJsonParser/blob/master/JsonTest/System.Text.Json/JsonParser.cs">This<br/>JsonParser</a></th>
<th>Peter<br/>Ohler's <a href="http://www.ohler.com/oj/">"Oj"</a><br/>C extension<br/>to Ruby</th>
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
<td><strong>76.6 K parses/sec</strong><br/>(informative;<br/>on his machine)</td>
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
<td><strong>tiny.json</strong><br/>127 bytes<br/>10,000 iter.<br/><strong>POCO</strong><br/>(<a href="#POCOs">see below</a>)</td>
<td>18.2 K parses/sec</td>
<td>40.0 K parses/sec</td>
<td>80.0 K parses/sec</td>
<td>178.6 K parses/sec</td>
<td>N / A</td>
</tr>
<tr>
<td><strong>tiny.json</strong><br/>127 bytes<br/>100,000 iter.<br/><strong>POCO</strong><br/>(<a href="#POCOs">see below</a>)</td>
<td>18.5 K parses/sec</td>
<td>90.9 K parses/sec</td>
<td>133.3 K parses/sec</td>
<td>173.9 K parses/sec</td>
<td>N / A</td>
</tr>
<tr>
<td><strong>tiny.json</strong><br/>127 bytes<br/>1,000,000 iter.<br/><strong>POCO</strong><br/>(<a href="#POCOs">see below</a>)</td>
<td>18.4 K parses/sec</td>
<td>101.0 K parses/sec</td>
<td>147.0 K parses/sec</td>
<td>169.5 K parses/sec</td>
<td>N / A</td>
</tr>
<tr>
<td><strong>dicos.json</strong><br/>922 bytes<br/>10,000 iter.<br/><strong>POCO</strong><br/>(<a href="#POCOs">see below</a>)</td>
<td>N / A</td>
<td>6.6 K parses/sec</td>
<td>16.7 K parses/sec</td>
<td>41.8 K parses/sec</td>
<td>N / A</td>
</tr>
<tr>
<td><strong>dicos.json</strong><br/>922 bytes<br/>100,000 iter.<br/><strong>POCO</strong><br/>(<a href="#POCOs">see below</a>)</td>
<td>N / A</td>
<td>7.0 K parses/sec</td>
<td>19.1 K parses/sec</td>
<td>39.7 K parses/sec</td>
<td>N / A</td>
</tr>
<tr>
<td><strong>dicos.json</strong><br/>922 bytes<br/>1,000,000 iter.<br/><strong>POCO</strong><br/>(<a href="#POCOs">see below</a>)</td>
<td>N / A</td>
<td>7.2 K parses/sec</td>
<td>18.7 K parses/sec</td>
<td>38.8 K parses/sec</td>
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

<a name="Peters"></a>

***Peter's "Oj Strict Mode Performance" test***

* "Loop" Test over Peter's "Oj Strict Mode Performance" sample (deserializing x times the JSON contained in the [_oj-highly-nested.json.txt](https://raw.github.com/ysharplanguage/FastJsonParser/master/JsonTest/TestData/_oj-highly-nested.json.txt) file = 257 bytes) - "loosely-typed" deserialization:
    * Performed 10,000 iterations: in ~ 235 milliseconds ( * )
        * vs. JavaScriptSerializer in ~ 875 milliseconds (... **272 %** slower)
        * vs. JSON.NET in ~ 805 milliseconds (... **242 %** slower)
        * vs. ServiceStack... N / A
        * ( * Which yields System.Text.Json.JsonParser's throughput : 10,982,905 bytes / second)
    * Performed 100,000 iterations: in ~ 2.71 seconds ( * )
        * vs. JavaScriptSerializer in ~ 9.05 seconds (... **239 %** slower)
        * vs. JSON.NET in ~ 7.95 seconds (... **193 %** slower)
        * vs. ServiceStack... N / A
        * ( * Which yields System.Text.Json.JsonParser's throughput : 9,486,895 bytes / second)
    * [_oj-highly-nested.json.txt](https://raw.github.com/ysharplanguage/FastJsonParser/master/JsonTest/TestData/_oj-highly-nested.json.txt) comes from Peter's sample and tests, at:
        * http://www.ohler.com/dev/oj_misc/performance_strict.html

I find the JSON data sample from Peter interesting for its non-trivial "shape", and the presence of these "highly nested" arrays (so to speak) at end of the payload:

    {"a":"Alpha","b":true,"c":12345,"d":[true,[false,[-123456789,null],3.9676,
    ["Something else.",false],null]],"e":{"zero":null,"one":1,"two":2,"three":[3],"four":[0,1,2,3,4]},
    "f":null,"h":{"a":{"b":{"c":{"d":{"e":{"f":{"g":null}}}}}}},
    "i":[[[[[[[null]]]]]]]}

As for that "vs. ServiceStack in... N / A":

unfortunately, quite unfamiliar with ServiceStack, I'm still trying to understand how, in absence of POCOs, to have it deserialize into merely trees of dictionaries + lists or arrays (just as we can do very easily using JSON.NET, or Microsoft's JavaScriptSerializer, or this parser here).

***Rick's "Boon" small test***

* Rick's "Boon" small test, slightly modified (deserializing x times the JSON contained in the [boon-small.json.txt](https://raw.github.com/ysharplanguage/FastJsonParser/master/JsonTest/TestData/boon-small.json.txt) file = 79 bytes) - with POCO target (1 class):
    * Performed 1,000,000 iterations: in ~ 3.83 seconds ( * )
        * vs. JavaScriptSerializer in ~ 31.5 seconds (... **722 %** slower)
        * vs. JSON.NET in ~ 7.15 seconds (... **86 %** slower)
        * vs. ServiceStack in ~ 5.55 seconds (... **45 %** slower)
        * ( * Which yields System.Text.Json.JsonParser's throughput : 20,632,018 bytes / second)
    * Performed 10,000,000 iterations: in ~ 36.9 seconds ( * )
        * vs. JavaScriptSerializer in ~ 302.3 seconds (... **719 %** slower)
        * vs. JSON.NET in ~ 69.8 seconds (... **89 %** slower)
        * vs. ServiceStack in ~ 54.8 seconds (... **48 %** slower)
        * ( * Which yields System.Text.Json.JsonParser's throughput : 21,379,664 bytes / second)

Rick's original test can be found at:

http://rick-hightower.blogspot.com/2013/11/benchmark-for-json-parsing-boon-scores.html

Note Rick is one of our fellows from the Java realm - and from [his own comparative figures](http://rick-hightower.blogspot.com/2013/12/boon-json-parser-seems-to-be-fastest.html) that I eventually noticed, it does seem that [Rick's "Boon"](https://github.com/RichardHightower/json-parsers-benchmark/blob/master/README.md) is also, indeed, *pretty fast*, among the number of Java toolboxes for JSON.

***"Tiny JSON" test***

* "Loop" Test over tiny JSON (deserializing x times the JSON contained in the [tiny.json.txt](https://raw.github.com/ysharplanguage/FastJsonParser/master/JsonTest/TestData/tiny.json.txt) file = 127 bytes) - with POCO target (1 class):
    * Performed 10,000 iterations: in ~ 56 milliseconds (pretty good) ( * )
        * vs. JavaScriptSerializer in ~ 550 milliseconds (... **882 %** slower)
        * vs. JSON.NET in ~ 250 milliseconds (... **346 %** slower)
        * vs. ServiceStack in ~ 125 milliseconds (... **123 %** slower)
        * ( * Which yields System.Text.Json.JsonParser's throughput : 22,321,428 bytes / second)
    * Performed 100,000 iterations: in ~ 575 milliseconds (not bad) ( * )
        * vs. JavaScriptSerializer in ~ 5.4 seconds (... **839 %** slower)
        * vs. JSON.NET in ~ 1.1 seconds (... **91 %** slower)
        * vs. ServiceStack in ~ 750 milliseconds (... **30 %** slower)
        * ( * Which yields System.Text.Json.JsonParser's throughput : 21,865,184 bytes / second)
    * Performed 1,000,000 iterations: in ~ 5.9 seconds (not bad either) ( * )
        * vs. JavaScriptSerializer in 54.5 seconds (... **823 %** slower)
        * vs. JSON.NET in ~ 9.9 seconds (... **67 %** slower)
        * vs. ServiceStack in ~ 6.8 seconds (... **15 %** slower)
        * ( * Which yields System.Text.Json.JsonParser's throughput : 21,594,966 bytes / second)
    * [tiny.json.txt](https://raw.github.com/ysharplanguage/FastJsonParser/master/JsonTest/TestData/tiny.json.txt)

<a name="Dicos"></a>

***"Dicos JSON" test***

* "Loop" Test over JSON "dictionaries" (deserializing x times the JSON contained in the [dicos.json.txt](https://raw.github.com/ysharplanguage/FastJsonParser/master/JsonTest/TestData/dicos.json.txt) file = 922 bytes) - with POCO target (1 class):
    * Performed 10,000 iterations: in ~ 239 milliseconds (pretty good) ( * )
        * vs. JavaScriptSerializer... N / A
        * vs. JSON.NET in ~ 1,525 milliseconds (... **538 %** slower)
        * vs. ServiceStack in ~ 600 milliseconds (... **108 %** slower)
        * ( * Which yields System.Text.Json.JsonParser's throughput : 38,661,087 bytes / second)
    * Performed 100,000 iterations: in ~ 2.52 seconds (not bad) ( * )
        * vs. JavaScriptSerializer... N / A
        * vs. JSON.NET in ~ 14.2 seconds (... **463 %** slower)
        * vs. ServiceStack in ~ 5.25 seconds (... **108 %** slower)
        * ( * Which yields System.Text.Json.JsonParser's throughput : 36,623,067 bytes / second)
    * Performed 1,000,000 iterations: in ~ 25.8 seconds (not bad either) ( * )
        * vs. JavaScriptSerializer... N / A
        * vs. JSON.NET in ~ 2 minutes 19 seconds (... **439 %** slower)
        * vs. ServiceStack in ~ 53.4 seconds (... **107 %** slower)
        * ( * Which yields System.Text.Json.JsonParser's throughput : 35,788,984 bytes / second)
    * [dicos.json.txt](https://raw.github.com/ysharplanguage/FastJsonParser/master/JsonTest/TestData/dicos.json.txt)

Note: this reads "JavaScriptSerializer... N / A" for this test because I couldn't get Microsoft's JavaScriptSerializer to deserialize [dicos.json.txt](https://raw.github.com/ysharplanguage/FastJsonParser/master/JsonTest/TestData/dicos.json.txt)'s data properly... and easily.

***"Small JSON" test***

* "Loop" Test over small JSON (deserializing x times the JSON contained in the [small.json.txt](https://raw.github.com/ysharplanguage/FastJsonParser/master/JsonTest/TestData/small.json.txt) file ~ 3.5 KB) - "loosely-typed" deserialization:
    * Performed 10,000 iterations: in ~ 1.14 second (pretty good) ( * )
        * vs. JavaScriptSerializer in ~ 6.7 seconds (... **488 %** slower)
        * vs. JSON.NET in ~ 2.2 seconds (... **93 %** slower)
        * vs. ServiceStack... N / A
        * ( * Which yields System.Text.Json.JsonParser's throughput : 31,174,408 bytes / second)
    * Performed 100,000 iterations: in ~ 11.7 seconds (not bad) ( * )
        * vs. JavaScriptSerializer in ~ 66.5 seconds (... **468 %** slower)
        * vs. JSON.NET... OutOfMemoryException
        * vs. ServiceStack... N / A
        * ( * Which yields System.Text.Json.JsonParser's throughput : 30,318,786 bytes / second)
    * [small.json.txt](https://raw.github.com/ysharplanguage/FastJsonParser/master/JsonTest/TestData/small.json.txt) being just a copy of the "{ "web-app": { "servlet": [ ... ] ... } }" sample, at:
        * http://www.json.org/example.html

***"Fathers JSON" test***

* "Fathers" Test (12 MB JSON file) - with POCO targets (4 distinct classes):
    * Parsed in ~ 285 milliseconds ( * )
        * vs. JavaScriptSerializer in ~ 2.6 seconds (... **812 %** slower)
        * vs. JSON.NET in ~ 500 milliseconds (... **75 %** slower)
        * vs. ServiceStack in ~ 575 milliseconds (... **101 %** slower)
        * ( * Which yields System.Text.Json.JsonParser's throughput : 45,494,340 bytes / second)
    * Note: [fathers.json.txt](https://github.com/ysharplanguage/FastJsonParser/blob/master/JsonTest/TestData/fathers.json.txt) was generated using this nifty online helper:
        * http://experiments.mennovanslooten.nl/2010/mockjson/tryit.html

The latter, "fathers" test, is the one with the results that intrigued me the most the very first few times I ran it - and it still does. However, I haven't taken the time yet to do more serious profiling to fully explain these timing differences that I didn't expect to be *that significant*.

They are also interesting to notice, if only when comparing JSON.NET vs. ServiceStack.

***"Huge JSON" test***

* "Huge" Test (180 MB JSON file) - "loosely-typed" deserialization:
    * Parsed in ~ 8.3 seconds ( * )
        * vs. JavaScriptSerializer in ~ 62 seconds (... **646 %** slower)
        * vs. JSON.NET... OutOfMemoryException
        * vs. ServiceStack... N / A
        * ( * Which yields System.Text.Json.JsonParser's throughput : 22,798,921 bytes / second)
    * As for huge.json.txt, it is just a copy of this file:
        * https://github.com/zeMirco/sf-city-lots-json

<a name="POCOs"></a>

Test target POCOs
-----------------

These are used by some of the above tests:

        // Used in the "boon-small.json" test
        public class BoonSmall
        {
            public string debug { get; set; }
            public IList<int> nums { get; set; }
        }


        // Used in the "tiny.json" test AND the unit tests
        public enum Status { Single, Married, Divorced }
        
        // Used in the "tiny.json" test AND the unit tests
        public class Person
        {
            public int Id { get; set; }
            public string Name { get; set; }
            
            // Both string and integral enum value representations can be parsed:
            public Status Status { get; set; }
            
            public string Address { get; set; }
            
            // Just to be sure we support that one, too:
            public IEnumerable<int> Scores { get; set; }
            
            public object Data { get; set; }
            
            // Generic dictionaries are also supported; e.g.:
            // '{
            //    "Name": "F. Bastiat", ...
            //    "History": [
            //       { "key": "1801-06-30", "value": "Birth date" }, ...
            //    ]
            //  }'
            public IDictionary<DateTime, string> History { get; set; }
            
            // 1-char-long strings in the JSON can be deserialized into System.Char:
            public char Abc { get; set; }
        }


        // Used in the "dicos.json" test
        public enum SomeKey
        {
            Key0, Key1, Key2, Key3, Key4,
            Key5, Key6, Key7, Key8, Key9
        }


        // Used in the "dicos.json" test
        public class DictionaryData
        {
            public IList<IDictionary<SomeKey, string>> Dictionaries { get; set; }
        }


        // Used in the "dicos.json" test
        // (adapted for JSON.NET and ServiceStack to deserialize OK)
        public class DictionaryDataAdaptJsonNetServiceStack
        {
            public IList<
                IList<KeyValuePair<SomeKey, string>>
            > Dictionaries { get; set; }
        }


        // Used in the "fathers.json" test
        public class FathersData
        {
            public Father[] fathers { get; set; }
        }


        // Used in the "fathers.json" test
        public class Someone
        {
            public string name { get; set; }
        }
        
        
        // Used in the "fathers.json" test
        public class Father : Someone
        {
            public int id { get; set; }
            public bool married { get; set; }
            // Lists...
            public List<Son> sons { get; set; }
            // ... or arrays for collections, that's fine:
            public Daughter[] daughters { get; set; }
        }


        // Used in the "fathers.json" test
        public class Child : Someone
        {
            public int age { get; set; }
        }
        
        
        // Used in the "fathers.json" test
        public class Son : Child
        {
        }


        // Used in the "fathers.json" test
        public class Daughter : Child
        {
            public string maidenName { get; set; }
        }


<a name="Roadmap"></a>

Roadmap
-------

None really worth of the name for now.

However, one thing I would like to support as soon as I can, is the ability to deserialize into [C#'s anonymous types](http://en.wikipedia.org/wiki/Anonymous_type). I've done it before, but I need to put more thinking into it (vs. [my first, other attempt at it](https://code.google.com/p/ysharp/source/browse/trunk/TestJSONParser/%28System.Text.Json%29Parser.cs)), in order to avoid the potential significant loss in performance I'm aware of.

Another, quite obvious, item on the wish list is to provide some support for custom deserialization. Design-wise, I do have a preference for a [functional approach](http://en.wikipedia.org/wiki/First-class_function#Language_support) which would be based on (more or less) arbitrary "reviver" [delegate types](http://en.wikipedia.org/wiki/Delegate_%28CLI%29#Technical_Implementation_Details), for use by the parser's methods (for typical IoC/callback use cases).

Again, the main implementation challenge will be not drifting too much from the current parsing speed ballpark.

In any case, I don't plan to make this small JSON deserializer as general-purpose and extensible as JSON.NET or ServiceStack's, I just want to keep it as simple, short, and fast as possible for my present and future needs (read on).

<a name="Background"></a>

"But, why this ad-hoc parser, and 'need for speed', anyway?"
------------------------------------------------------------

"... Why do you even care, when you already have the excellent, fast, feature-rich [JSON.NET](http://james.newtonking.com/json) and [ServiceStack](https://github.com/ServiceStack/ServiceStack) around?"

Indeed, pure parsing + deserialization speed isn't in fact my *long term* goal, or not for any *arbitrary* JSON input, anyway. For another, and broader project - still in design stage - that I have, I plan to use JSON as a "malleable" IR (intermediate representation) for code and meta data transformations that I'll have to make happen in-between a high level source language (e.g., C#, or F#,...) and the target CIL (or some other lower target).

[JSON.NET](http://james.newtonking.com/json)'s deserialization performance is great, and so is [ServiceStack](https://github.com/ServiceStack/ServiceStack)'s - [really, they are, already](http://theburningmonk.com/benchmarks/) - but I would like to have something of my own in my toolbox much smaller/more manageable (in terms of # of SLOC), and simpler to extend, for whatever unforeseen requirements of the deserialization process (from JSON text into CLR types and values) I may have to tackle.

From an earlier experiment on that other project, I found out that I will *not* need a *generic* way to solve a *specific* deserialization sub-problem very efficiently (which nobody can really do - as there is "no silver bullet" / "one size fits all" for that matter), but instead I will only need a *specific* way to solve it efficiently, by extending this small parser's functionality only where and how that's exactly needed (while trying to maintain its good performance).

Finally, this parser/deserializer is/was a nice learning opportunity for me with parsing JSON, and to verify by myself once again [what I had read about](http://msdn.microsoft.com/en-us/magazine/cc507639.aspx) and experienced many times before. That is: never try to merely guess about performance, but instead always do your best to measure and to find out *where exactly* the parsing and deserialization slowdowns (and memory consumption costs) *actually* come from.

Other questions?
----------------

Let yourself go:

ysharp {dot} design {at} gmail {dot} com
