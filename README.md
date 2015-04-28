System.Text.Json
================

* <a href="#Overview">Overview</a>
    * <a href="#Introduction">Introduction</a>
    * <a href="#Goal">Design goal</a>
    * <a href="#NuGet">Available on NuGet</a> : PM&gt; Install-Package [System.Text.Json](https://www.nuget.org/packages/System.Text.Json)
    * <a href="#Status">Development status</a>
* <a href="#Interface">Public interface</a>
* <a href="#JSONPath">JSONPath support</a> (&gt;= 1.9.9.7)
* <a href="#AnonymousTypes">Anonymous types support</a> (&gt;= 1.9.9.8)
* <a href="#IntegralTypes">Integral types support</a> (&gt;= 2.0.0.0)
* <a href="#MiscellaneousTypes">Miscellaneous types support</a>
* <a href="#Performance">Performance</a>
    * <a href="#PerfOverview">Speed Tests Results : Overview</a>
    * <a href="#PerfDetailed">Speed Tests Results : Detailed</a>
* <a href="#POCOs">Test target POCOs</a>
* <a href="#Limitations">Known limitations / caveats</a>
* <a href="#Roadmap">Roadmap</a>
* <a href="#Background">Background</a>
* <a href="#FAQ">CFAQ</a>

<a name="Overview"></a>

<a name="Introduction"></a>

"Small is beautiful."
---------------------

This is a minimalistic and fast JSON parser / deserializer, for full .NET.

The complete source code of the parser is pretty short (in a [single source file less than 1,700 SLOC-long](https://raw.githubusercontent.com/ysharplanguage/FastJsonParser/master/JsonTest/System.Text.Json/JsonParser.cs), not counting the comments) and comes with some speed tests and their sample data, all in the "JsonTest" and "TestData" folders.

The console test program includes a few unit tests (for both nominal cases vs. error cases), and it will attempt to execute them before the speed tests - see [ParserTests.cs](https://raw.githubusercontent.com/ysharplanguage/FastJsonParser/master/JsonTest/ParserTests.cs).

**Do not hesitate** to add more of the former (i.e., unit tests) and/or to **raise here whatever issues** you may find.

There's a link to this repository at [json.org](http://www.json.org/) (in the C# implementations section).

<a name="Goal"></a>

Design goal
-----------

This aims at parsing textual JSON data, and to deserialize it into our (strongly typed) [POCO](http://en.wikipedia.org/wiki/Plain_Old_CLR_Object)s, as fast as possible.

For now ( * ), the only tiers of interest for this parser are the desktop / server tiers. There are other JSON librairies with good performance, and already well tested / documented, which *also* support mobile / handheld devices that run more limited flavors of .NET (e.g., [Json.NET](http://james.newtonking.com/json) and [ServiceStack](https://github.com/ServiceStack/ServiceStack) come to mind).

(* i.e., until it gets stable enough and I hear people's interest for it to run elsewhere)

This JSON parser / deserializer thus aims first at staying "simple", short, fast, and for use by some desktop- or server-side code that would require the full .NET anyway, and unlikely to ever be usable on mobile devices (unless those can eventually run the full .NET).

Nevertheless, [Sami](https://github.com/sami1971) has just recently been able to do a quick performance test on his Android device (as the code could luckily compile for it *as-is*), and came up with a few benchmark results (a dozen or so) which do seem encouraging - i.e., see this thread of Xamarin's Android forum :

http://forums.xamarin.com/discussion/comment/39011/#Comment_39011

<a name="NuGet"></a>

Available on NuGet
------------------

( https://www.nuget.org/packages/System.Text.Json )

For convenience :

PM&gt; Install-Package [System.Text.Json](https://www.nuget.org/packages/System.Text.Json)

<a name="Status"></a>

Development status
------------------

Although it is promisingly fast, lightweight, and easy to use, please note this parser / deserializer is still *mostly* experimental.

For one simple thing to begin with, it is in need of *more comprehensive* JSON conformance tests.

That said, just feel free to [fork / bugfix / augment / improve it](https://github.com/ysharplanguage/FastJsonParser/graphs/contributors) at your own will.

Of course, I welcome your informed input and feedback.

**Please read and accept** the terms of the [LICENSE](https://raw.githubusercontent.com/ysharplanguage/FastJsonParser/master/LICENSE.md), or else, do not use this library *as-is*.

<a name="Interface"></a>

Public interface
----------------

Consists mainly in these six (three generic ones, and three non-generic ones) *instance* methods :

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

The capability to parse JSON text coming thru a stream (reader) is clearly a must-have, past a certain size of payload - "*[have mercy on your CLR large object heap](http://msdn.microsoft.com/en-us/magazine/cc534993.aspx)*".

Note that if you don't care (i.e., don't need / don't want to bother) deserializing whatever input JSON into POCOs, you can then just call these methods with

    object

for the generic type argument, as in, e.g. :

    parser.Parse<object>(@" [ { ""greetings"": ""hello"" } ] ")

or, equivalently, just :

    parser.Parse(@" [ { ""greetings"": ""hello"" } ] ")
    
It will then deserialize the input into a tree made of

    Dictionary<string, object>
   
instances, for JSON *objects* which are unordered sets of name / value pairs, and of

    List<object>
   
instances, for JSON *arrays* which are ordered collections of values.

The leaves will be from any of these types :

    null type
    bool
    string

In this case of, say, "loosely typed" deserialization, you may ask : "But what about the JSON number literals in the input - why deserializing them as *strings*?"

I would then ask - "... in absence of more specific type information about the deserialization target, *"who"* is likely best placed to decide whether the number after the colon, in

    "SomeNumber": 123.456

should be deserialized into a [System.Single](http://msdn.microsoft.com/en-us/library/System.Single.aspx), a [System.Double](http://msdn.microsoft.com/en-us/library/System.Double.aspx), or a [System.Decimal](http://msdn.microsoft.com/en-us/library/System.Decimal.aspx) (but obviously not into some integer) - is it this parser, or is it the application?"

In my opinion, *in that case*, it's the application.

(Also, one can read [this very informative post of Eric Lippert](http://ericlippert.com/2013/07/25/what-is-the-type-of-the-null-literal/) about the so-called "null type".)

<a name="JSONPath"></a>

JSONPath support
----------------

Starting with [version 1.9.9.7](https://www.nuget.org/packages/System.Text.Json), Stefan Gössner's [JSONPath](http://goessner.net/articles/JsonPath) is also supported.

For a reference example, in four or five steps :

**Step \#1**, have defined somewhere a target object model / type system to hydrate :

             namespace Test
             {
                public class Data
                {
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
             }

**Step \#2**, in order to use this [JSONPath facility](https://github.com/ysharplanguage/FastJsonParser/blob/master/JsonTest/System.Text.Json/JsonParser.cs#L49) don't forget to include, accordingly :

            using System.Text.Json.JsonPath;

**Step \#3**, have some input JSON to parse :

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

(Optional) **step \#4**, have an evaluator delegate ready to compile [JSONPath member selectors or filter predicates](http://goessner.net/articles/JsonPath/#e2) that the [JsonPathSelection](https://github.com/ysharplanguage/FastJsonParser/blob/master/JsonTest/System.Text.Json/JsonParser.cs#L49)'s [SelectNodes(...)](https://github.com/ysharplanguage/FastJsonParser/blob/master/JsonTest/System.Text.Json/JsonParser.cs#L57) method may come across :

            JsonPathScriptEvaluator evaluator =
               (script, value, context) =>
                  (value is Type) ?
                  // This holds: (value as Type) == typeof(Func<string, T, IJsonPathContext, object>),
                  // with T inferred by JsonPathSelection::SelectNodes(...)
                  ExpressionParser.Parse
                  (
                     (Type)value, script, true, typeof(Data).Namespace
                  ).
                  Compile()
                  :
                  null;

where the delegate type [JsonPathScriptEvaluator](https://code.google.com/p/jsonpath/source/browse/trunk/src/cs/JsonPath.cs?spec=svn56&r=53#57) has been [redefined here](https://github.com/ysharplanguage/FastJsonParser/blob/master/JsonTest/System.Text.Json/JsonParser.cs#L47), as :

            delegate object JsonPathScriptEvaluator(string script, object value, IJsonPathContext context)

Note there is a **basic** ( * ) lambda expression parser & compiler - [ExpressionParser](https://github.com/ysharplanguage/FastJsonParser/blob/master/JsonTest/System.Text.Json/LambdaCompilation.cs#L608) (adapted from Zhucai's "lambda-parser", at [http://code.google.com/p/lambda-parser](http://code.google.com/p/lambda-parser)) defined in the namespace "[System.Text.Json.JsonPath.LambdaCompilation](https://github.com/ysharplanguage/FastJsonParser/blob/master/JsonTest/System.Text.Json/LambdaCompilation.cs#L606)" - used here as a helper to implement the above "evaluator".

(* N.B. : **not** all of the C\# 3.0+ syntax is supported by [ExpressionParser](https://github.com/ysharplanguage/FastJsonParser/blob/master/JsonTest/System.Text.Json/LambdaCompilation.cs#L608) (e.g., the [Linq Query Comprehension Syntax](http://msdn.microsoft.com/en-us/library/bb397947(v=vs.90).aspx) isn't) - only the most common expression forms, including unary / binary / ternary operators, array & dictionary indexers "[ ]", instance and static method calls, "is", "as", "typeof" type system operators, ... etc.)

**Step \#4 or \#5**, (a) parse and deserialize the input JSON into the target object model, (b) wrap a [JsonPathSelection](https://github.com/ysharplanguage/FastJsonParser/blob/master/JsonTest/System.Text.Json/JsonParser.cs#L49) instance around the latter, and (c) invoke the [JsonPathSelection](https://github.com/ysharplanguage/FastJsonParser/blob/master/JsonTest/System.Text.Json/JsonParser.cs#L49)'s [SelectNodes(...)](https://github.com/ysharplanguage/FastJsonParser/blob/master/JsonTest/System.Text.Json/JsonParser.cs#L57) method with the [JSONPath](http://goessner.net/articles/JsonPath) expression of interest to query the object model :

            // Step #5 (a)... parse and deserialize the input JSON into the target object model :
            var typed = new JsonParser().Parse<Data>(input);

            // Step #5 (b)... cache the JsonPathSelection and its lambdas compiled (on-demand)
            // by the evaluator :
            var scope = new JsonPathSelection(typed, evaluator);
            
            // Step #5 (c)... invoke the SelectNodes method with the JSONPath expression
            // to query the object model :
            var nodes = scope.SelectNodes("$.store.book[?(@.title == \"The Lord of the Rings\")].price");
            
            System.Diagnostics.Debug.Assert
            (
                nodes != null &&
                nodes.Length == 1 &&
                nodes[0].Value is decimal &&
                nodes[0].As<decimal>() == 22.99m
            );

Thus, the purpose of this "evaluator", passed here as an optional argument to the [JsonPathSelection constructor](https://github.com/ysharplanguage/FastJsonParser/blob/master/JsonTest/System.Text.Json/JsonParser.cs#L55), is for the [SelectNodes(...)](https://github.com/ysharplanguage/FastJsonParser/blob/master/JsonTest/System.Text.Json/JsonParser.cs#L57) method to be able to compile on-demand whatever lambda expression delegates are required to implement [JSONPath expressions for member selectors or filter predicates](http://goessner.net/articles/JsonPath/#e2), such as

    ?(@.title == \"The Lord of the Rings\")

above.

In this same example, the lambda expression delegate compiled by the evaluator (and then cached into the "scope" [JsonPathSelection](https://github.com/ysharplanguage/FastJsonParser/blob/master/JsonTest/System.Text.Json/JsonParser.cs#L49) instance) is of type

    Func<string, Book, IJsonPathContext, object>
    
corresponding to the actual lambda expression script prepared behind the scene :

    (string script, Book value, IJsonPathContext context) => (object)(value.title == "The Lord of the Rings")
    
There is therefore type inference - performed at run-time by the [JsonPathSelection](https://github.com/ysharplanguage/FastJsonParser/blob/master/JsonTest/System.Text.Json/JsonParser.cs#L49)'s [SelectNodes(...)](https://github.com/ysharplanguage/FastJsonParser/blob/master/JsonTest/System.Text.Json/JsonParser.cs#L57) method - regarding the second argument (and only that one, named "value") of the evaluator-produced, cached delegates.

Finally, notice how those delegates' static return type is in fact [System.Object](http://msdn.microsoft.com/en-us/library/System.Object.aspx) (and not [System.Boolean](http://msdn.microsoft.com/en-us/library/System.Boolean.aspx)), for uniformity with the more general member selector expression, as used as an [alternative to explicit names or indices](http://goessner.net/articles/JsonPath/#e2).

More [JSONPath](http://goessner.net/articles/JsonPath) usage examples (after JSON deserialization by [System.Text.Json.JsonParser](https://raw.githubusercontent.com/ysharplanguage/FastJsonParser/master/JsonTest/System.Text.Json/JsonParser.cs)) can be found [here](https://github.com/ysharplanguage/FastJsonParser/blob/master/JsonTest/ParserTests.cs#L333).

E.g., the following [JSONPath](http://goessner.net/articles/JsonPath) expressions work as expected, in [version 1.9.9.7](https://www.nuget.org/packages/System.Text.Json) and up :

    $.store // The store
    $['store'] // (Idem) The store
    
    // (Involves an object member selector lambda)
    $.[((@ is Data) ? \"store\" : (string)null)] // (Idem) The store
    
    $.store.book[3].title // Title of the fourth book
    
    // (Involves an object filter predicate lambda)
    $.store.book[?(@.author == \"Herman Melville\")].price // Price of Herman Melville's book
    
    $.store.book[*].author // Authors of all books in the store
    $.store..price // Price of everything in the store
    $..book[2] // Third book
    
    // (Involves an array member (index) selector lambda)
    $..book[(@.Length - 1)] // Last book in order
    
    $..book[-1:] // (Idem) Last book in order
    
    $..book[0,1] // First two books
    $..book[:2] // (Idem) First two books
    
    // (Involves an object filter predicate lambda)
    $..book[?(@.isbn)] // All books with an ISBN
    
    // (Idem)
    $..book[?(@.price < 10m)] // All books cheaper than 10

***References***

* [JSONPath - XPath for JSON](http://goessner.net/articles/JsonPath/#e1) (by Stefan Gössner)
* [JSONPath expressions](http://goessner.net/articles/JsonPath/#e2)
* [JSONPath examples](http://goessner.net/articles/JsonPath/#e3)

***Online utilities***

* [JSONPath Expression Tester](http://jsonpath.curiousconcept.com/) (by Curious Concept)
* [JSONPath Online Evaluator](http://ashphy.com/JSONPathOnlineEvaluator) (by Kazuki Hamasaki)

<a name="AnonymousTypes"></a>

Anonymous types support
-----------------------

Starting with [version 1.9.9.8](https://www.nuget.org/packages/System.Text.Json), the deserialization into anonymous types instances is also supported. [Here is an example](https://github.com/ysharplanguage/FastJsonParser/blob/master/JsonTest/ParserTests.cs#L510) to get started :

            // Anonymous type instance prototype of the target object model,
            // used for static type inference by the C# compiler (see below)
            var OBJECT_MODEL = new
            {
                country = new // (Anonymous) country
                {
                    name = default(string),
                    people = new[] // (Array of...)
                    {
                        new // (Anonymous) person
                        {
                            initials = default(string),
                            DOB = default(DateTime),
                            citizen = default(bool),
                            status = default(Status) // (Marital "Status" enumeration type)
                        }
                    }
                }
            };
            
            var anonymous = new JsonParser().Parse
            (
                // Anonymous type instance prototype, passed in
                // solely for type inference by the C# compiler
                OBJECT_MODEL,
                
                // Input
                @"{
                    ""country"": {
                        ""name"": ""USA"",
                        ""people"": [
                            {
                                ""initials"": ""VV"",
                                ""citizen"": true,
                                ""DOB"": ""1970-03-28"",
                                ""status"": ""Married""
                            },
                            {
                                ""DOB"": ""1970-05-10"",
                                ""initials"": ""CJ""
                            },
                            {
                                ""initials"": ""RP"",
                                ""DOB"": ""1935-08-20"",
                                ""status"": ""Married"",
                                ""citizen"": true
                            }
                        ]
                    }
                }"
            );
            
            System.Diagnostics.Debug.Assert(anonymous.country.people.Length == 3);
            
            foreach (var person in anonymous.country.people)
                System.Diagnostics.Debug.Assert
                (
                    person.initials.Length == 2 &&
                    person.DOB > new DateTime(1901, 1, 1)
                );
                
            scope = new JsonPathSelection(anonymous, evaluator);
            System.Diagnostics.Debug.Assert
            (
                (nodes = scope.SelectNodes(@"$..people[?(!@.citizen)]")).Length == 1 &&
                nodes.As(OBJECT_MODEL.country.people[0])[0].initials == "CJ" &&
                nodes.As(OBJECT_MODEL.country.people[0])[0].DOB == new DateTime(1970, 5, 10) &&
                nodes.As(OBJECT_MODEL.country.people[0])[0].status == Status.Single
            );

where the "evaluator" is the same as the one defined in the [JSONPath section](#JSONPath) of this document.

<a name="IntegralTypes"></a>

Integral types support
----------------------

Starting with [version 2.0.0.0](https://www.nuget.org/packages/System.Text.Json), the following integral types are supported (including as possible [underlying types](http://msdn.microsoft.com/en-us/library/system.enum.getunderlyingtype(v=vs.110).aspx) of programmer-defined [enumeration types](http://msdn.microsoft.com/en-us/library/system.enum(v=vs.110).aspx)) :

([example](https://github.com/ysharplanguage/FastJsonParser/blob/master/JsonTest/ParserTests.cs#L647))

* [System.SByte (aka "sbyte" in C#)](http://msdn.microsoft.com/en-us/library/System.SByte.aspx) (&gt;= 2.0.0.0)
* [System.Byte (aka "byte")](http://msdn.microsoft.com/en-us/library/System.Byte.aspx)
* [System.Int16 (aka "short")](http://msdn.microsoft.com/en-us/library/System.Int16.aspx)
* [System.UInt16 (aka "ushort")](http://msdn.microsoft.com/en-us/library/System.UInt16.aspx) (&gt;= 2.0.0.0)
* [System.Int32 (aka "int")](http://msdn.microsoft.com/en-us/library/System.Int32.aspx)
* [System.UInt32 (aka "uint")](http://msdn.microsoft.com/en-us/library/System.UInt32.aspx) (&gt;= 2.0.0.0)
* [System.Int64 (aka "long")](http://msdn.microsoft.com/en-us/library/System.Int64.aspx)
* [System.UInt64 (aka "ulong")](http://msdn.microsoft.com/en-us/library/System.UInt64.aspx) (&gt;= 2.0.0.0)

<a name="MiscellaneousTypes"></a>

Miscellaneous types support
---------------------------

* Starting with [version 2.0.0.1](https://www.nuget.org/packages/System.Text.Json), [System.Guid](http://msdn.microsoft.com/en-us/library/System.Guid.aspx) is supported ([example](https://github.com/ysharplanguage/FastJsonParser/blob/master/JsonTest/ParserTests.cs#L647))
* Starting with [version 2.0.0.2](https://www.nuget.org/packages/System.Text.Json), some well-known [nullable types](http://msdn.microsoft.com/en-us/library/b3h38hb0.aspx) are supported ([example](https://github.com/ysharplanguage/FastJsonParser/blob/master/JsonTest/ParserTests.cs#L647)) - these are, in C# syntax :
    * bool?
    * char?
    * sbyte?
    * byte?
    * short?
    * ushort?
    * int?
    * uint?
    * long?
    * ulong?
    * float?
    * double?
    * decimal?
    * Guid?
    * DateTime?
    * DateTimeOffset?
    * ... as well as any programmer-defined nullable [enumeration type](http://msdn.microsoft.com/en-us/library/system.enum(v=vs.110).aspx)

<a name="Performance"></a>
<a name="Performances"></a>

Performance
-----------

Following in the table below: a few figures, the outcome *average numbers* (only) that I obtain from the tests provided here.

Consistently enough, I also obtain similar performance ratios for the same 4 parsers / deserializers when compared one-to-one, after I adapt (for this [JsonParser](https://raw.githubusercontent.com/ysharplanguage/FastJsonParser/master/JsonTest/System.Text.Json/JsonParser.cs) doesn't provide object-to-JSON text *serialization*) and I run "the burning monk's" simple speed tester for JSON, which can be found at :

http://theburningmonk.com/2015/04/binary-and-json-benchmarks-updated-2

<a name="PerfOverview"></a>
***Speed Tests Results : Overview***

<table border="1" width="100%">
<tr>
<th><a href="https://raw.githubusercontent.com/ysharplanguage/FastJsonParser/master/JsonTest/System.Text.Json/JsonParser.cs">This<br/>JsonParser</a><br/>versus...</th>
<th>Microsoft's<br/><a href="http://msdn.microsoft.com/en-us/library/system.web.script.serialization.javascriptserializer%28v=vs.100%29.aspx">JavaScript Serializer</a></th>
<th>James Newton-King's<br/><a href="http://james.newtonking.com/json">Json.NET</a></th>
<th>Demis<br/>Bellot's<br/><a href="https://github.com/ServiceStack/ServiceStack">ServiceStack</a></th>
<th><a href="https://raw.githubusercontent.com/ysharplanguage/FastJsonParser/master/JsonTest/System.Text.Json/JsonParser.cs">This<br/>JsonParser</a></th>
<th>Peter Ohler's<br/><a href="http://www.ohler.com/oj/">"Oj"</a><br/>C extension<br/>to Ruby</th>
</tr>
<tr>
<td align="center"><strong>Performance<br/>+/- %</strong></td>
<td align="center"><strong>+ 425 %</strong><br/>(faster)</td>
<td align="center"><strong>+ 127 %</strong><br/>(faster)</td>
<td align="center"><strong>+ 33 %</strong><br/>(faster)</td>
<td align="center"><strong>=</strong></td>
<td align="center"><strong>- 108 %</strong><br/>(slower)</td>
</tr>
</table>

***Disclaimer***

Note such figures (either the "burning monk's", or the following) can always - potentially - be much dependent on the test data at hand, and/or the way testing is performed.

Of course, YMMV, so it's always a good idea to make *your own* benchmarks, using *your own test data*, especially in the data "shape" you're interested in, and that you expect to encounter with *a good probability* in your application domain.

***Other libraries, versions used ("the competition")***

* Out-of-the-box .NET 4.0's [JavaScriptSerializer](http://msdn.microsoft.com/en-us/library/system.web.script.serialization.javascriptserializer%28v=vs.100%29.aspx)
* [Json.NET](http://james.newtonking.com/json) **v5.0 r8**, and
* [ServiceStack](https://github.com/ServiceStack/ServiceStack) **v3.9.59**

Before you try to run the speed tests against the test data provided, please note this repository does **not** include the binaries for the versions of Json.NET and ServiceStack mentioned (you can obtain those from their respective links above).

***Executable target, and H/W used***

.NET 4.0 target, on a humble Ideapad Intel Core i5 CPU @ 2.50GHz, 6 GB RAM, running Win7 64bit, with a ~ 98%..99% idle CPU (a nice enough personal laptop, but not exactly a beast of speed nowadays).

Just for comparison out of curiosity, on the third row of the table below I also give (this one measure only, for a glimpse) the throughput achieved, in the native code realm, by <a href="http://www.ohler.com/oj/">Peter Ohler's "Oj"</a> ("Oj" - *Optimized JSON* : a C extension to Ruby) for 100,000 parses over his own JSON sample that I've reused to prepare this benchmark.

(Refer to [_oj-highly-nested.json.txt](https://raw.github.com/ysharplanguage/FastJsonParser/master/JsonTest/TestData/_oj-highly-nested.json.txt), copied from Peter's: [http://www.ohler.com/dev/oj_misc/performance_strict.html](http://www.ohler.com/dev/oj_misc/performance_strict.html))

<a name="PerfDetailed"></a>
***Speed Tests Results : Detailed***

So, without further ado... (larger figure - # parses per second - means faster)

<table border="1" width="100%">
<tr>
<th>Test /<br/>JSON size /<br/># Iterations /<br/>POCO or<br/>loosely-typed?</th>
<th>Microsoft's<br/><a href="http://msdn.microsoft.com/en-us/library/system.web.script.serialization.javascriptserializer%28v=vs.100%29.aspx">JavaScript<br/>Serializer</a></th>
<th>James<br/>Newton-<br/>King's<br/><a href="http://james.newtonking.com/json">Json.NET</a></th>
<th>Demis<br/>Bellot's<br/><a href="https://github.com/ServiceStack/ServiceStack">ServiceStack</a></th>
<th><a href="https://raw.githubusercontent.com/ysharplanguage/FastJsonParser/master/JsonTest/System.Text.Json/JsonParser.cs">This<br/>JsonParser</a></th>
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

The same, with the test files and timings details :

(smaller time means faster)

<a name="Peters"></a>

***Peter's "Oj Strict Mode Performance" test***

* "Loop" Test over Peter's "Oj Strict Mode Performance" sample (deserializing x times the JSON contained in the [_oj-highly-nested.json.txt](https://raw.github.com/ysharplanguage/FastJsonParser/master/JsonTest/TestData/_oj-highly-nested.json.txt) file = 257 bytes) - "loosely-typed" deserialization :
    * Performed 10,000 iterations: in ~ 235 milliseconds ( * )
        * vs. JavaScriptSerializer in ~ 875 milliseconds (... **272 %** slower)
        * vs. Json.NET in ~ 805 milliseconds (... **242 %** slower)
        * vs. ServiceStack... N / A
        * ( * Which yields System.Text.Json.JsonParser's throughput : 10,982,905 bytes / second)
    * Performed 100,000 iterations: in ~ 2.71 seconds ( * )
        * vs. JavaScriptSerializer in ~ 9.05 seconds (... **239 %** slower)
        * vs. Json.NET in ~ 7.95 seconds (... **193 %** slower)
        * vs. ServiceStack... N / A
        * ( * Which yields System.Text.Json.JsonParser's throughput : 9,486,895 bytes / second)
    * [_oj-highly-nested.json.txt](https://raw.github.com/ysharplanguage/FastJsonParser/master/JsonTest/TestData/_oj-highly-nested.json.txt) comes from Peter's sample and tests, at:
        * http://www.ohler.com/dev/oj_misc/performance_strict.html

I find the JSON data sample from Peter interesting for its non-trivial "shape", and the presence of these "highly nested" arrays (so to speak) at end of the payload :

    {"a":"Alpha","b":true,"c":12345,"d":[true,[false,[-123456789,null],3.9676,
    ["Something else.",false],null]],"e":{"zero":null,"one":1,"two":2,"three":[3],"four":[0,1,2,3,4]},
    "f":null,"h":{"a":{"b":{"c":{"d":{"e":{"f":{"g":null}}}}}}},
    "i":[[[[[[[null]]]]]]]}

As for that "vs. ServiceStack in... N / A" :

unfortunately, quite unfamiliar with ServiceStack, I'm still trying to understand how, in absence of POCOs, to have it deserialize into merely trees of dictionaries + lists or arrays (just as we can do very easily using Json.NET, or Microsoft's JavaScriptSerializer, or this parser here).

***Rick's "Boon" small test***

* Rick's "Boon" small test, slightly modified (deserializing x times the JSON contained in the [boon-small.json.txt](https://raw.github.com/ysharplanguage/FastJsonParser/master/JsonTest/TestData/boon-small.json.txt) file = 79 bytes) - with POCO target (1 class) :
    * Performed 1,000,000 iterations: in ~ 3.83 seconds ( * )
        * vs. JavaScriptSerializer in ~ 31.5 seconds (... **722 %** slower)
        * vs. Json.NET in ~ 7.15 seconds (... **86 %** slower)
        * vs. ServiceStack in ~ 5.55 seconds (... **45 %** slower)
        * ( * Which yields System.Text.Json.JsonParser's throughput : 20,632,018 bytes / second)
    * Performed 10,000,000 iterations: in ~ 36.9 seconds ( * )
        * vs. JavaScriptSerializer in ~ 302.3 seconds (... **719 %** slower)
        * vs. Json.NET in ~ 69.8 seconds (... **89 %** slower)
        * vs. ServiceStack in ~ 54.8 seconds (... **48 %** slower)
        * ( * Which yields System.Text.Json.JsonParser's throughput : 21,379,664 bytes / second)

Rick's original test can be found at :

http://rick-hightower.blogspot.com/2013/11/benchmark-for-json-parsing-boon-scores.html

Note Rick is one of our fellows from the Java realm - and from [his own comparative figures](http://rick-hightower.blogspot.com/2013/12/boon-json-parser-seems-to-be-fastest.html) that I eventually noticed, it does seem that [Rick's "Boon"](https://github.com/RichardHightower/json-parsers-benchmark/blob/master/README.md) is also, indeed, *pretty fast*, among the number of Java toolboxes for JSON.

***"Tiny JSON" test***

* "Loop" Test over tiny JSON (deserializing x times the JSON contained in the [tiny.json.txt](https://raw.github.com/ysharplanguage/FastJsonParser/master/JsonTest/TestData/tiny.json.txt) file = 127 bytes) - with POCO target (1 class) :
    * Performed 10,000 iterations: in ~ 56 milliseconds (pretty good) ( * )
        * vs. JavaScriptSerializer in ~ 550 milliseconds (... **882 %** slower)
        * vs. Json.NET in ~ 250 milliseconds (... **346 %** slower)
        * vs. ServiceStack in ~ 125 milliseconds (... **123 %** slower)
        * ( * Which yields System.Text.Json.JsonParser's throughput : 22,321,428 bytes / second)
    * Performed 100,000 iterations: in ~ 575 milliseconds (not bad) ( * )
        * vs. JavaScriptSerializer in ~ 5.4 seconds (... **839 %** slower)
        * vs. Json.NET in ~ 1.1 seconds (... **91 %** slower)
        * vs. ServiceStack in ~ 750 milliseconds (... **30 %** slower)
        * ( * Which yields System.Text.Json.JsonParser's throughput : 21,865,184 bytes / second)
    * Performed 1,000,000 iterations: in ~ 5.9 seconds (not bad either) ( * )
        * vs. JavaScriptSerializer in 54.5 seconds (... **823 %** slower)
        * vs. Json.NET in ~ 9.9 seconds (... **67 %** slower)
        * vs. ServiceStack in ~ 6.8 seconds (... **15 %** slower)
        * ( * Which yields System.Text.Json.JsonParser's throughput : 21,594,966 bytes / second)
    * [tiny.json.txt](https://raw.github.com/ysharplanguage/FastJsonParser/master/JsonTest/TestData/tiny.json.txt)

<a name="Dicos"></a>

***"Dicos JSON" test***

* "Loop" Test over JSON "dictionaries" (deserializing x times the JSON contained in the [dicos.json.txt](https://raw.github.com/ysharplanguage/FastJsonParser/master/JsonTest/TestData/dicos.json.txt) file = 922 bytes) - with POCO target (1 class) :
    * Performed 10,000 iterations: in ~ 239 milliseconds (pretty good) ( * )
        * vs. JavaScriptSerializer... N / A
        * vs. Json.NET in ~ 1,525 milliseconds (... **538 %** slower)
        * vs. ServiceStack in ~ 600 milliseconds (... **108 %** slower)
        * ( * Which yields System.Text.Json.JsonParser's throughput : 38,661,087 bytes / second)
    * Performed 100,000 iterations: in ~ 2.52 seconds (not bad) ( * )
        * vs. JavaScriptSerializer... N / A
        * vs. Json.NET in ~ 14.2 seconds (... **463 %** slower)
        * vs. ServiceStack in ~ 5.25 seconds (... **108 %** slower)
        * ( * Which yields System.Text.Json.JsonParser's throughput : 36,623,067 bytes / second)
    * Performed 1,000,000 iterations: in ~ 25.8 seconds (not bad either) ( * )
        * vs. JavaScriptSerializer... N / A
        * vs. Json.NET in ~ 2 minutes 19 seconds (... **439 %** slower)
        * vs. ServiceStack in ~ 53.4 seconds (... **107 %** slower)
        * ( * Which yields System.Text.Json.JsonParser's throughput : 35,788,984 bytes / second)
    * [dicos.json.txt](https://raw.github.com/ysharplanguage/FastJsonParser/master/JsonTest/TestData/dicos.json.txt)

Note this reads "JavaScriptSerializer... N / A" for this test because I couldn't get Microsoft's JavaScriptSerializer to deserialize [dicos.json.txt](https://raw.github.com/ysharplanguage/FastJsonParser/master/JsonTest/TestData/dicos.json.txt)'s data properly... and easily.

***"Small JSON" test***

* "Loop" Test over small JSON (deserializing x times the JSON contained in the [small.json.txt](https://raw.github.com/ysharplanguage/FastJsonParser/master/JsonTest/TestData/small.json.txt) file ~ 3.5 KB) - "loosely-typed" deserialization :
    * Performed 10,000 iterations: in ~ 1.14 second (pretty good) ( * )
        * vs. JavaScriptSerializer in ~ 6.7 seconds (... **488 %** slower)
        * vs. Json.NET in ~ 2.2 seconds (... **93 %** slower)
        * vs. ServiceStack... N / A
        * ( * Which yields System.Text.Json.JsonParser's throughput : 31,174,408 bytes / second)
    * Performed 100,000 iterations: in ~ 11.7 seconds (not bad) ( * )
        * vs. JavaScriptSerializer in ~ 66.5 seconds (... **468 %** slower)
        * vs. Json.NET... OutOfMemoryException
        * vs. ServiceStack... N / A
        * ( * Which yields System.Text.Json.JsonParser's throughput : 30,318,786 bytes / second)
    * [small.json.txt](https://raw.github.com/ysharplanguage/FastJsonParser/master/JsonTest/TestData/small.json.txt) being just a copy of the "{ "web-app": { "servlet": [ ... ] ... } }" sample, at:
        * http://www.json.org/example.html

***"Fathers JSON" test***

* "Fathers" Test (12 MB JSON file) - with POCO targets (4 distinct classes) :
    * Parsed in ~ 285 milliseconds ( * )
        * vs. JavaScriptSerializer in ~ 2.6 seconds (... **812 %** slower)
        * vs. Json.NET in ~ 500 milliseconds (... **75 %** slower)
        * vs. ServiceStack in ~ 575 milliseconds (... **101 %** slower)
        * ( * Which yields System.Text.Json.JsonParser's throughput : 45,494,340 bytes / second)
    * Note: [fathers.json.txt](https://github.com/ysharplanguage/FastJsonParser/blob/master/JsonTest/TestData/fathers.json.txt) was generated using this nifty online helper :
        * http://experiments.mennovanslooten.nl/2010/mockjson/tryit.html

The latter, "fathers" test, is the one with the results that intrigued me the most the very first few times I ran it - and it still does. However, I haven't taken the time yet to do more serious profiling to fully explain these timing differences that I didn't expect to be *that significant*.

They are also interesting to notice, if only when comparing Json.NET vs. ServiceStack.

***"Huge JSON" test***

* "Huge" Test (180 MB JSON file) - "loosely-typed" deserialization :
    * Parsed in ~ 8.3 seconds ( * )
        * vs. JavaScriptSerializer in ~ 62 seconds (... **646 %** slower)
        * vs. Json.NET... OutOfMemoryException
        * vs. ServiceStack... N / A
        * ( * Which yields System.Text.Json.JsonParser's throughput : 22,798,921 bytes / second)
    * As for huge.json.txt, it is just a copy of this file:
        * https://github.com/zeMirco/sf-city-lots-json

<a name="POCOs"></a>

Test target POCOs
-----------------

These are used by some of the above tests :

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
        // (adapted for Json.NET and ServiceStack to deserialize OK)
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

<a name="Limitations"></a>

Known limitations / caveats
---------------------------

* The current [JsonParser](https://raw.githubusercontent.com/ysharplanguage/FastJsonParser/master/JsonTest/System.Text.Json/JsonParser.cs)'s instance methods implementation is **neither** [thread-safe or reentrant](http://en.wikipedia.org/wiki/Thread_safety#Implementation_approaches).
    * (Work is underway to make [the "Parse" methods of the public interface](#Interface) *at least* [reentrant](http://en.wikipedia.org/wiki/Reentrancy_(computing)#Rules_for_reentrancy) for any given [JsonParser](https://raw.githubusercontent.com/ysharplanguage/FastJsonParser/master/JsonTest/System.Text.Json/JsonParser.cs) instance.)
* <s>Incoming "null"s are not yet recognized as a valid "value" (or rather, absence thereof) for nullable types members of the target POCO(s).</s>
    * <s>(Support for such explicited "null"s will be added asap.)</s> (fixed in [version 2.0.0.5](https://www.nuget.org/packages/System.Text.Json))

<a name="Roadmap"></a>

Roadmap
-------

None really worth of the name for now (beyond what is mentioned in "[known limitations](#Limitations)" above).

<s>However, one thing I would like to support as soon as I can, is the ability to deserialize into [C#'s anonymous types](http://en.wikipedia.org/wiki/Anonymous_type). I've done it before, but I need to put more thinking into it (vs. [my first, other attempt at it](https://code.google.com/p/ysharp/source/browse/trunk/TestJSONParser/%28System.Text.Json%29Parser.cs)), in order to avoid the potential significant loss in performance I'm aware of.</s>

<s>Another, quite obvious, item on the wish list is to provide some support for custom deserialization. Design-wise, I do have a preference for a [functional approach](http://en.wikipedia.org/wiki/First-class_function#Language_support) which would be based on (more or less) arbitrary "reviver" [delegate types](http://en.wikipedia.org/wiki/Delegate_%28CLI%29#Technical_Implementation_Details), for use by the parser's methods (for typical IoC / callback use cases).</s>

<s>Again, the main implementation challenge will be not drifting too much from the current parsing speed ballpark.</s>

In any case, I don't plan to make this small JSON deserializer as general-purpose and extensible as Json.NET or ServiceStack's, I just want to keep it as simple, short, and fast as possible for my present and future needs (read on).

<a name="Background"></a>

Background
----------

Pure parsing + deserialization speed isn't in fact my *long term* goal, or not for any *arbitrary* JSON input, anyway. For another, and broader project - still in design stage - that I have, I plan to use JSON as a "malleable" IR (intermediate representation) for code and meta data transformations that I'll have to make happen in-between a high level source language (e.g., C#, or F#,...) and the target CIL (or some other lower target).

[Json.NET](http://james.newtonking.com/json)'s deserialization performance is great, and so is [ServiceStack](https://github.com/ServiceStack/ServiceStack)'s - [really, they are, already](http://theburningmonk.com/benchmarks/) - but I would like to have something of my own in my toolbox much smaller / more manageable (in terms of # of SLOC), and simpler to extend, for whatever unforeseen requirements of the deserialization process (from JSON text into CLR types and values) I may have to tackle.

This parser / deserializer is / was also a nice learning opportunity in regards to parsing JSON, and to verify by myself once again [what I had read about](http://msdn.microsoft.com/en-us/magazine/cc507639.aspx) and experienced many times before. That is: never try to merely guess about performance, but instead always do your best to measure and to find out *where exactly* the parsing and deserialization slowdowns (and memory consumption costs) *actually* come from.

<a name="FAQ"></a>

CFAQ
----

(Could-be Frequently Asked Questions)

* Q : Isn't it a bit confusing, somehow, that [the "Parse" methods of the public interface](#Interface) do actually more than just parse the input against [the JSON syntax](http://www.json.org/), but also perform the work that most other JSON implementations call "Deserialize"?
    * A : **Yes** and **no**. It is indeed true that [these "Parse" methods](#Interface) do more than just parse the input, but they have been named that way because this [JsonParser](https://raw.githubusercontent.com/ysharplanguage/FastJsonParser/master/JsonTest/System.Text.Json/JsonParser.cs) is designed to remain only that : *merely* a JSON **parser** and **deserializer**, thus *without* any JSON *serialization*-related feature. By not naming them "Deserialize", this helps to avoid another otherwise possible confusion as to why there are no "Serialize" methods to be found anywhere, w.r.t. the dual operation (*serialization* vs. *deserialization*).
* Q : Do you foresee that you'll make any breaking changes to [the public interface](#Interface) in the near-, mid-, or long-term?
    * A : For [most of it](#Interface), **no**, I should not and I won't. The only [JsonParser](https://raw.githubusercontent.com/ysharplanguage/FastJsonParser/master/JsonTest/System.Text.Json/JsonParser.cs)'s instance methods that may be subject to change / to some refactoring (or disappear altogether) in the future, are those taking that last "IDictionary&lt;Type, Func&lt;...&gt;&gt; mappers" parameter (for now a rudimentary provision to support custom filtered deserialization use cases). So, all of the following are definitely going to stay around for as long as [JsonParser](https://raw.githubusercontent.com/ysharplanguage/FastJsonParser/master/JsonTest/System.Text.Json/JsonParser.cs) is developed and maintained here :

[JsonParser](https://raw.githubusercontent.com/ysharplanguage/FastJsonParser/master/JsonTest/System.Text.Json/JsonParser.cs)'s (*stable* public interface)

        object Parse(string input)
        object Parse(TextReader input)
        object Parse(Stream input)
        object Parse(Stream input, Encoding encoding)
        T Parse<T>(string input)
        T Parse<T>(T prototype, string input)
        T Parse<T>(TextReader input)
        T Parse<T>(T prototype, TextReader input)
        T Parse<T>(Stream input)
        T Parse<T>(T prototype, Stream input)
        T Parse<T>(Stream input, Encoding encoding)
        T Parse<T>(T prototype, Stream input, Encoding encoding)

Other questions?
----------------

Feel free to send them to :

ysharp {dot} design {at} gmail {dot} com
