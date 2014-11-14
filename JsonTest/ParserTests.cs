// On GitHub: https://github.com/ysharplanguage/FastJsonParser
//#define THIS_JSON_PARSER_ONLY
#define RUN_UNIT_TESTS
#define RUN_BASIC_JSONPATH_TESTS
#define RUN_ADVANCED_JSONPATH_TESTS
//#define RUN_SERVICESTACK_TESTS

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

// For the JavaScriptSerializer
using System.Web.Script.Serialization;

#if !THIS_JSON_PARSER_ONLY
// For Json.NET
using Newtonsoft.Json;

#if RUN_SERVICESTACK_TESTS
// For ServiceStack
using ServiceStack.Text;
#endif
#endif

// Our stuff
using System.Text.Json;

namespace Test
{
#if RUN_UNIT_TESTS && (RUN_BASIC_JSONPATH_TESTS || RUN_ADVANCED_JSONPATH_TESTS)
    using System.Text.Json.JsonPath;
    using System.Text.Json.LambdaCompilation;
#endif

    public class E
    {
        public object zero { get; set; }
        public int one { get; set; }
        public int two { get; set; }
        public List<int> three { get; set; }
        public List<int> four { get; set; }
    }

    public class F
    {
        public object g { get; set; }
    }

    public class E2
    {
        public F f { get; set; }
    }

    public class D
    {
        public E2 e { get; set; }
    }

    public class C
    {
        public D d { get; set; }
    }

    public class B
    {
        public C c { get; set; }
    }

    public class A
    {
        public B b { get; set; }
    }

    public class H
    {
        public A a { get; set; }
    }

    public class HighlyNested
    {
        public string a { get; set; }
        public bool b { get; set; }
        public int c { get; set; }
        public List<object> d { get; set; }
        public E e { get; set; }
        public object f { get; set; }
        public H h { get; set; }
        public List<List<List<List<List<List<List<object>>>>>>> i { get; set; }
    }

    public class BoonSmall
    {
        public string debug { get; set; }
        public IList<int> nums { get; set; }
    }

    public enum Status
    {
        Single,
        Married,
        Divorced
    }

    public interface ISomething
    {
        int Id { get; set; }
        // Notice how "Name" isn't introduced here yet, but
        // instead, only in the implementation class "Stuff"
        // below:
    }

    public class Stuff : ISomething
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class StuffHolder
    {
        public IList<ISomething> Items { get; set; }
    }

    public class Asset
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
    }

    public class Owner : Person
    {
        public IList<Asset> Assets { get; set; }
    }

    public class Owners
    {
        public IDictionary<decimal, Owner> OwnerByWealth { get; set; }
        public IDictionary<Owner, decimal> WealthByOwner { get; set; }
    }

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

    public enum SomeKey
    {
        Key0, Key1, Key2, Key3, Key4,
        Key5, Key6, Key7, Key8, Key9
    }

    public class DictionaryData
    {
        public IList<IDictionary<SomeKey, string>> Dictionaries { get; set; }
    }

    public class DictionaryDataAdaptJsonNetServiceStack
    {
        public IList<
            IList<KeyValuePair<SomeKey, string>>
        > Dictionaries { get; set; }
    }

    public class FathersData
    {
        public Father[] fathers { get; set; }
    }

    public class Someone
    {
        public string name { get; set; }
    }

    public class Father : Someone
    {
        public int id { get; set; }
        public bool married { get; set; }
        // Lists...
        public List<Son> sons { get; set; }
        // ... or arrays for collections, that's fine:
        public Daughter[] daughters { get; set; }
    }

    public class Child : Someone
    {
        public int age { get; set; }
    }

    public class Son : Child
    {
    }

    public class Daughter : Child
    {
        public string maidenName { get; set; }
    }

    public enum VendorID
    {
        Vendor0,
        Vendor1,
        Vendor2,
        Vendor3,
        Vendor4,
        Vendor5
    }

    public class SampleConfigItem
    {
        public int Id { get; set; }
        public string Content { get; set; }
    }

    public class SampleConfigData<TKey>
    {
        public Dictionary<TKey, object> ConfigItems { get; set; }
    }

    #region POCO model for SO question "Json deserializing issue c#" ( http://stackoverflow.com/questions/26426594/json-deserializing-issue-c-sharp )
    public class From
    {
        public string id { get; set; }
        public string name { get; set; }
        public string category { get; set; }
    }

    public class Post
    {
        public string id { get; set; }
        public From from { get; set; }
        public string message { get; set; }
        public string picture { get; set; }
        public Dictionary<string, Like[]> likes { get; set; }
    }

    public class Like
    {
        public string id { get; set; }
        public string name { get; set; }
    }
    #endregion

    #region POCO model for JSONPath Tests (POCO)
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
        public string isbn { get; set; }
        public decimal price { get; set; }
    }

    public class Bicycle
    {
        public string color { get; set; }
        public decimal price { get; set; }
    }
    #endregion

    class ParserTests
    {
        private static readonly string OJ_TEST_FILE_PATH = string.Format(@"..{0}..{0}TestData{0}_oj-highly-nested.json.txt", Path.DirectorySeparatorChar);
        private static readonly string BOON_SMALL_TEST_FILE_PATH = string.Format(@"..{0}..{0}TestData{0}boon-small.json.txt", Path.DirectorySeparatorChar);
        private static readonly string TINY_TEST_FILE_PATH = string.Format(@"..{0}..{0}TestData{0}tiny.json.txt", Path.DirectorySeparatorChar);
        private static readonly string DICOS_TEST_FILE_PATH = string.Format(@"..{0}..{0}TestData{0}dicos.json.txt", Path.DirectorySeparatorChar);
        private static readonly string SMALL_TEST_FILE_PATH = string.Format(@"..{0}..{0}TestData{0}small.json.txt", Path.DirectorySeparatorChar);
        private static readonly string FATHERS_TEST_FILE_PATH = string.Format(@"..{0}..{0}TestData{0}fathers.json.txt", Path.DirectorySeparatorChar);
        private static readonly string HUGE_TEST_FILE_PATH = string.Format(@"..{0}..{0}TestData{0}huge.json.txt", Path.DirectorySeparatorChar);

#if RUN_UNIT_TESTS
        static object UnitTest<T>(string input, Func<string, T> parse) { return UnitTest(input, parse, false); }

        static object UnitTest<T>(string input, Func<string, T> parse, bool errorCase)
        {
            object obj;
            Console.WriteLine();
            Console.WriteLine(errorCase ? "(Error case)" : "(Nominal case)");
            Console.WriteLine("\tTry parse: {0} ... as: {1} ...", input, typeof(T).FullName);
            try { obj = parse(input); }
            catch (Exception ex) { obj = ex; }
            Console.WriteLine("\t... result: {0}{1}", (obj != null) ? obj.GetType().FullName : "(null)", (obj is Exception) ? " (" + ((Exception)obj).Message + ")" : String.Empty);
            Console.WriteLine();
            Console.WriteLine("Press a key...");
            Console.WriteLine();
            Console.ReadKey();
            return obj;
        }

        static void UnitTests()
        {
            object obj;
            Console.Clear();
            Console.WriteLine("Press ESC to skip the unit tests or any other key to start...");
            Console.WriteLine();
            if (Console.ReadKey().KeyChar == 27)
                return;
#if RUN_BASIC_JSONPATH_TESTS || RUN_ADVANCED_JSONPATH_TESTS
            #region JSONPath Tests ( http://goessner.net/articles/JsonPath/ )
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
            JsonPathScriptEvaluator evaluator =
                (script, value, context) =>
                    ((value is Type) && (context == script))
                    ?
                    ExpressionParser.Parse((Type)value, script, true, typeof(Data).Namespace).Compile()
                    :
                    null;
            JsonPathSelection scope;
            JsonPathNode[] nodes;

#if RUN_BASIC_JSONPATH_TESTS
            var untyped = new JsonParser().Parse(input); // (object untyped = ...)

            scope = untyped.ToJsonPath(); // Extension method
            nodes = scope.SelectNodes("$.store.book[3].title"); // Normalized in bracket-notation: $['store']['book'][3]['title']
            System.Diagnostics.Debug.Assert
            (
                nodes != null &&
                nodes.Length == 1 &&
                nodes[0].Value is string &&
                (string)nodes[0].Value == "The Lord of the Rings"
            );

            scope = untyped.ToJsonPath(evaluator); // Cache the JsonPathSelection and its lambdas compiled on-demand at run-time by the evaluator.
            nodes = scope.SelectNodes("$.store.book[?(@.ContainsKey(\"isbn\") && (string)@[\"isbn\"] == \"0-395-19395-8\")].title");
            System.Diagnostics.Debug.Assert
            (
                nodes != null &&
                nodes.Length == 1 &&
                nodes[0].Value is string &&
                (string)nodes[0].Value == "The Lord of the Rings"
            );
#endif

#if RUN_ADVANCED_JSONPATH_TESTS
            var typed = new JsonParser().Parse<Data>(input); // (Data typed = ...)
            scope = typed.ToJsonPath(evaluator);
            nodes = scope.SelectNodes("$.store.book[?(@.author == \"Herman Melville\")].price");
            System.Diagnostics.Debug.Assert
            (
                nodes != null &&
                nodes.Length == 1 &&
                nodes[0].Value is decimal &&
                (decimal)nodes[0].Value == 8.99m
            );

            // Yup. This works too.
            nodes = scope.SelectNodes("$.[((@ is Data) ? \"store\" : (string)null)]"); // Dynamic member (property) selection
            System.Diagnostics.Debug.Assert
            (
                nodes != null &&
                nodes.Length == 1 &&
                nodes[0].Value is Store &&
                nodes[0].Value == scope.SelectNodes("$['store']")[0].Value && // Normalized in bracket-notation
                nodes[0].Value == scope.SelectNodes("$.store")[0].Value // Common dot-notation
            );

            // And this, as well. To compare with the above '... nodes = scope.SelectNodes("$.store.book[3].title")'
            nodes = scope.
                SelectNodes
                (
                    // JSONPath expression template...
                    "$.[{0}].[{1}][{2}].[{3}]",
                    // ... interpolated with these compile-time lambdas:
                    (script, value, context) => "store", // Member selector (by name)
                    (script, value, context) => "book", // Member selector (by name)
                    (script, value, context) => 1, // Member selector (by index)
                    (script, value, context) => "title" // Member selector (by name)
                );
            System.Diagnostics.Debug.Assert
            (
                nodes != null &&
                nodes.Length == 1 &&
                nodes[0].Value is string &&
                (string)nodes[0].Value == "Sword of Honour"
            );

            // Some JSONPath expressions from Stefan Gössner's JSONPath examples ( http://goessner.net/articles/JsonPath/#e3 )...

            // Authors of all books in the store
            System.Diagnostics.Debug.Assert
            (
                (nodes = scope.SelectNodes("$.store.book[*].author")).Length == 4
            );

            // Price of everything in the store
            System.Diagnostics.Debug.Assert
            (
                (nodes = scope.SelectNodes("$.store..price")).Length == 5
            );

            // Third book
            System.Diagnostics.Debug.Assert
            (
                (nodes = scope.SelectNodes("$..book[2]"))[0].Value is Book && ((Book)nodes[0].Value).isbn == "0-553-21311-3"
            );

            // Last book in order
            System.Diagnostics.Debug.Assert
            (
                (nodes = scope.SelectNodes("$..book[(@.Length - 1)]"))[0].Value == scope.SelectNodes("$..book[-1:]")[0].Value
            );

            // First two books
            System.Diagnostics.Debug.Assert
            (
                (nodes = scope.SelectNodes("$..book[0,1]")).Length == scope.SelectNodes("$..book[:2]").Length && nodes.Length == 2
            );

            // All books with an ISBN
            System.Diagnostics.Debug.Assert
            (
                (nodes = scope.SelectNodes("$..book[?(@.isbn)]")).Length == 2
            );

            // All books cheapier than 10
            System.Diagnostics.Debug.Assert
            (
                (nodes = scope.SelectNodes("$..book[?(@.price < 10m)]")).Length == 2
            );
#endif
            #endregion
#endif

            // A few nominal cases
            obj = UnitTest("null", s => new JsonParser().Parse(s));
            System.Diagnostics.Debug.Assert(obj == null);

            obj = UnitTest("true", s => new JsonParser().Parse(s));
            System.Diagnostics.Debug.Assert(obj is bool && (bool)obj);

            obj = UnitTest(@"""\z""", s => new JsonParser().Parse<char>(s));
            System.Diagnostics.Debug.Assert(obj is char && (char)obj == 'z');

            obj = UnitTest(@"""\t""", s => new JsonParser().Parse<char>(s));
            System.Diagnostics.Debug.Assert(obj is char && (char)obj == '\t');

            obj = UnitTest(@"""\u0021""", s => new JsonParser().Parse<char>(s));
            System.Diagnostics.Debug.Assert(obj is char && (char)obj == '!');

            obj = UnitTest(@"""\u007D""", s => new JsonParser().Parse<char>(s));
            System.Diagnostics.Debug.Assert(obj is char && (char)obj == '}');

            obj = UnitTest(@" ""\u007e"" ", s => new JsonParser().Parse<char>(s));
            System.Diagnostics.Debug.Assert(obj is char && (char)obj == '~');

            obj = UnitTest(@" ""\u202A"" ", s => new JsonParser().Parse<char>(s));
            System.Diagnostics.Debug.Assert(obj is char && (int)(char)obj == 0x202a);

            obj = UnitTest("123", s => new JsonParser().Parse<int>(s));
            System.Diagnostics.Debug.Assert(obj is int && (int)obj == 123);

            obj = UnitTest("\"\"", s => new JsonParser().Parse<string>(s));
            System.Diagnostics.Debug.Assert(obj is string && (string)obj == String.Empty);

            obj = UnitTest("\"Abc\\tdef\"", s => new JsonParser().Parse<string>(s));
            System.Diagnostics.Debug.Assert(obj is string && (string)obj == "Abc\tdef");

            obj = UnitTest("[null]", s => new JsonParser().Parse<object[]>(s));
            System.Diagnostics.Debug.Assert(obj is object[] && ((object[])obj).Length == 1 && ((object[])obj)[0] == null);

            obj = UnitTest("[true]", s => new JsonParser().Parse<IList<bool>>(s));
            System.Diagnostics.Debug.Assert(obj is IList<bool> && ((IList<bool>)obj).Count == 1 && ((IList<bool>)obj)[0]);

            obj = UnitTest("[1,2,3,4,5]", s => new JsonParser().Parse<int[]>(s));
            System.Diagnostics.Debug.Assert(obj is int[] && ((int[])obj).Length == 5 && ((int[])obj)[4] == 5);

            obj = UnitTest("123.456", s => new JsonParser().Parse<decimal>(s));
            System.Diagnostics.Debug.Assert(obj is decimal && (decimal)obj == 123.456m);

            obj = UnitTest("{\"a\":123,\"b\":true}", s => new JsonParser().Parse(s));
            System.Diagnostics.Debug.Assert(obj is IDictionary<string, object> && (((IDictionary<string, object>)obj)["a"] as string) == "123" && ((obj = ((IDictionary<string, object>)obj)["b"]) is bool) && (bool)obj);

            obj = UnitTest("1", s => new JsonParser().Parse<Status>(s));
            System.Diagnostics.Debug.Assert(obj is Status && (Status)obj == Status.Married);

            obj = UnitTest("\"Divorced\"", s => new JsonParser().Parse<Status>(s));
            System.Diagnostics.Debug.Assert(obj is Status && (Status)obj == Status.Divorced);

            obj = UnitTest("{\"Name\":\"Peter\",\"Status\":0}", s => new JsonParser().Parse<Person>(s));
            System.Diagnostics.Debug.Assert(obj is Person && ((Person)obj).Name == "Peter" && ((Person)obj).Status == Status.Single);

            obj = UnitTest("{\"Name\":\"Paul\",\"Status\":\"Married\"}", s => new JsonParser().Parse<Person>(s));
            System.Diagnostics.Debug.Assert(obj is Person && ((Person)obj).Name == "Paul" && ((Person)obj).Status == Status.Married);

            obj = UnitTest("{\"History\":[{\"key\":\"1801-06-30T00:00:00Z\",\"value\":\"Birth date\"}]}", s => new JsonParser().Parse<Person>(s));
            System.Diagnostics.Debug.Assert(obj is Person && ((Person)obj).History[new DateTime(1801, 6, 30, 0, 0, 0, DateTimeKind.Utc)] == "Birth date");

            obj = UnitTest(@"{""Items"":[
                {
                    ""__type"": ""Test.Stuff, Test, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"",
                    ""Id"": 123, ""Name"": ""Foo""
                },
                {
                    ""__type"": ""Test.Stuff, Test, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"",
                    ""Id"": 456, ""Name"": ""Bar""
                }]}", s => new JsonParser().Parse<StuffHolder>(s));
            System.Diagnostics.Debug.Assert
            (
                obj is StuffHolder && ((StuffHolder)obj).Items.Count == 2 &&
                ((Stuff)((StuffHolder)obj).Items[1]).Name == "Bar"
            );

            string configTestInputVendors = @"{
                ""ConfigItems"": {
                    ""Vendor1"": {
                        ""__type"": ""Test.SampleConfigItem, Test, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"",
                        ""Id"": 100,
                        ""Content"": ""config content for vendor 1""
                    },
                    ""Vendor3"": {
                        ""__type"": ""Test.SampleConfigItem, Test, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"",
                        ""Id"": 300,
                        ""Content"": ""config content for vendor 3""
                    }
                }
            }";

            string configTestInputIntegers = @"{
                ""ConfigItems"": {
                    ""123"": {
                        ""__type"": ""Test.SampleConfigItem, Test, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"",
                        ""Id"": 123000,
                        ""Content"": ""config content for key 123""
                    },
                    ""456"": {
                        ""__type"": ""Test.SampleConfigItem, Test, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"",
                        ""Id"": 456000,
                        ""Content"": ""config content for key 456""
                    }
                }
            }";

            obj = UnitTest(configTestInputVendors, s => new JsonParser().Parse<SampleConfigData<VendorID>>(s));
            System.Diagnostics.Debug.Assert
            (
                obj is SampleConfigData<VendorID> &&
                ((SampleConfigData<VendorID>)obj).ConfigItems.ContainsKey(VendorID.Vendor3) &&
                ((SampleConfigData<VendorID>)obj).ConfigItems[VendorID.Vendor3] is SampleConfigItem &&
                ((SampleConfigItem)((SampleConfigData<VendorID>)obj).ConfigItems[VendorID.Vendor3]).Id == 300
            );

            obj = UnitTest(configTestInputIntegers, s => new JsonParser().Parse<SampleConfigData<int>>(s));
            System.Diagnostics.Debug.Assert
            (
                obj is SampleConfigData<int> &&
                ((SampleConfigData<int>)obj).ConfigItems.ContainsKey(456) &&
                ((SampleConfigData<int>)obj).ConfigItems[456] is SampleConfigItem &&
                ((SampleConfigItem)((SampleConfigData<int>)obj).ConfigItems[456]).Id == 456000
            );

            obj = UnitTest(@"{
                ""OwnerByWealth"": {
                    ""15999.99"":
                        { ""Id"": 1,
                          ""Name"": ""Peter"",
                          ""Assets"": [
                            { ""Name"": ""Car"",
                              ""Price"": 15999.99 } ]
                        },
                    ""250000.05"":
                        { ""Id"": 2,
                          ""Name"": ""Paul"",
                          ""Assets"": [
                            { ""Name"": ""House"",
                              ""Price"": 250000.05 } ]
                        }
                },
                ""WealthByOwner"": [
                    { ""key"": { ""Id"": 1, ""Name"": ""Peter"" }, ""value"": 15999.99 },
                    { ""key"": { ""Id"": 2, ""Name"": ""Paul"" }, ""value"": 250000.05 }
                ]
            }", s => new JsonParser().Parse<Owners>(s));
            Owner peter, owner;
            System.Diagnostics.Debug.Assert
            (
                (obj is Owners) &&
                (peter = ((Owners)obj).WealthByOwner.Keys.
                    Where(person => person.Name == "Peter").FirstOrDefault()
                ) != null &&
                (owner = ((Owners)obj).OwnerByWealth[15999.99m]) != null &&
                (owner.Name == peter.Name) &&
                (owner.Assets.Count == 1) &&
                (owner.Assets[0].Name == "Car")
            );

#if !THIS_JSON_PARSER_ONLY
            // Support for Json.NET's "$type" pseudo-key (in addition to ServiceStack's "__type"):
            Person jsonNetPerson = new Person { Id = 123, Abc = '#', Name = "Foo", Scores = new[] { 100, 200, 300 } };

            // (Expected serialized form shown in next comment)
            string jsonNetString = JsonConvert.SerializeObject(jsonNetPerson, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects });
            // => '{"$type":"Test.ParserTests+Person, Test","Id":123,"Name":"Foo","Status":0,"Address":null,"Scores":[100,200,300],"Data":null,"History":null,"Abc":"#"}'

            // (Note the Parse<object>(...))
            object restoredObject = UnitTest(jsonNetString, s => new JsonParser().Parse<object>(jsonNetString));
            System.Diagnostics.Debug.Assert
            (
                restoredObject is Person &&
                ((Person)restoredObject).Name == "Foo" &&
                ((Person)restoredObject).Abc == '#' &&
                ((IList<int>)((Person)restoredObject).Scores).Count == 3 &&
                ((IList<int>)((Person)restoredObject).Scores)[2] == 300
            );
#endif

            var SO_26426594_input = @"{ ""data"": [
    {
      ""id"": ""post 1"", 
      ""from"": {
        ""category"": ""Local business"", 
        ""name"": ""..."", 
        ""id"": ""...""
      }, 
      ""message"": ""..."", 
      ""picture"": ""..."", 
      ""likes"": {
        ""data"": [
          {
            ""id"": ""like 1"", 
            ""name"": ""text 1...""
          }, 
          {
            ""id"": ""like 2"", 
            ""name"": ""text 2...""
          }
        ]
      }
    }
] }";
            var posts =
                (
                    UnitTest(
                        SO_26426594_input,
                        FacebookPostDeserialization_SO_26426594
                    ) as
                    Dictionary<string, Post[]>
                );
            System.Diagnostics.Debug.Assert(posts != null && posts["data"][0].id == "post 1");
            System.Diagnostics.Debug.Assert(posts != null && posts["data"][0].from.category == "Local business");
            System.Diagnostics.Debug.Assert(posts != null && posts["data"][0].likes["data"][0].id == "like 1");
            System.Diagnostics.Debug.Assert(posts != null && posts["data"][0].likes["data"][1].id == "like 2");

            // A few error cases
            obj = UnitTest("\"unfinished", s => new JsonParser().Parse<string>(s), true);
            System.Diagnostics.Debug.Assert(obj is Exception && ((Exception)obj).Message.StartsWith("Bad string"));

            obj = UnitTest("[123]", s => new JsonParser().Parse<string[]>(s), true);
            System.Diagnostics.Debug.Assert(obj is Exception && ((Exception)obj).Message.StartsWith("Bad string"));

            obj = UnitTest("[null]", s => new JsonParser().Parse<short[]>(s), true);
            System.Diagnostics.Debug.Assert(obj is Exception && ((Exception)obj).Message.StartsWith("Bad number (short)"));

            obj = UnitTest("[123.456]", s => new JsonParser().Parse<int[]>(s), true);
            System.Diagnostics.Debug.Assert(obj is Exception && ((Exception)obj).Message.StartsWith("Unexpected character at 4"));

            obj = UnitTest("\"Unknown\"", s => new JsonParser().Parse<Status>(s), true);
            System.Diagnostics.Debug.Assert(obj is Exception && ((Exception)obj).Message.StartsWith("Bad enum value"));

            Console.Clear();
            Console.WriteLine("... Unit tests done.");
            Console.WriteLine();
            Console.WriteLine("Press a key to start the speed tests...");
            Console.ReadKey();
        }
#endif

        static void LoopTest<T>(string parserName, Func<string, T> parseFunc, string testFile, int count)
        {
            Console.Clear();
            Console.WriteLine("Parser: {0}", parserName);
            Console.WriteLine();
            Console.WriteLine("Loop Test File: {0}", testFile);
            Console.WriteLine();
            Console.WriteLine("Iterations: {0}", count.ToString("0,0"));
            Console.WriteLine();
            Console.WriteLine("Deserialization: {0}", (typeof(T) != typeof(object)) ? "POCO(s)" : "loosely-typed");
            Console.WriteLine();
            Console.WriteLine("Press ESC to skip this test or any other key to start...");
            Console.WriteLine();
            if (Console.ReadKey().KeyChar == 27)
                return;

            System.Threading.Thread.MemoryBarrier();
            var initialMemory = System.GC.GetTotalMemory(true);

            var json = System.IO.File.ReadAllText(testFile);
            var st = DateTime.Now;
            var l = new List<object>();
            for (var i = 0; i < count; i++)
                l.Add(parseFunc(json));
            var tm = (int)DateTime.Now.Subtract(st).TotalMilliseconds;

            System.Threading.Thread.MemoryBarrier();
            var finalMemory = System.GC.GetTotalMemory(true);
            var consumption = finalMemory - initialMemory;

            System.Diagnostics.Debug.Assert(l.Count == count);

            Console.WriteLine();
            Console.WriteLine("... Done, in {0} ms. Throughput: {1} characters / second.", tm.ToString("0,0"), (1000 * (decimal)(count * json.Length) / (tm > 0 ? tm : 1)).ToString("0,0.00"));
            Console.WriteLine();
            Console.WriteLine("\tMemory used : {0}", ((decimal)finalMemory).ToString("0,0"));
            Console.WriteLine();

            GC.Collect();
            Console.WriteLine("Press a key...");
            Console.WriteLine();
            Console.ReadKey();
        }

        static void Test<T>(string parserName, Func<string, T> parseFunc, string testFile)
        {
            Test<T>(parserName, parseFunc, testFile, null, null, null);
        }

        static void Test<T>(string parserName, Func<string, T> parseFunc, string testFile, string jsonPathExpression, Func<object, object> jsonPathSelect, Func<object, bool> jsonPathAssert)
        {
            var jsonPathTest = !String.IsNullOrWhiteSpace(jsonPathExpression) && (jsonPathSelect != null) && (jsonPathAssert != null);
            Console.Clear();
            Console.WriteLine("{0}Parser: {1}", (jsonPathTest ? "(With JSONPath selection test) " : String.Empty), parserName);
            Console.WriteLine();
            Console.WriteLine("Test File: {0}", testFile);
            Console.WriteLine();
            Console.WriteLine("Deserialization: {0}", (typeof(T) != typeof(object)) ? "POCO(s)" : "loosely-typed");
            Console.WriteLine();
            if (jsonPathTest)
            {
                Console.WriteLine("JSONPath expression: {0}", jsonPathExpression);
                Console.WriteLine();
            }
            Console.WriteLine("Press ESC to skip this test or any other key to start...");
            Console.WriteLine();
            if (Console.ReadKey().KeyChar == 27)
                return;

            System.Threading.Thread.MemoryBarrier();
            var initialMemory = System.GC.GetTotalMemory(true);

            var json = System.IO.File.ReadAllText(testFile);
            var st = DateTime.Now;
            var o = parseFunc(json);
            if (jsonPathTest)
            {
                var selection = jsonPathSelect(o);
                var assertion = jsonPathAssert(selection);
                System.Diagnostics.Debug.Assert(assertion);
            }
            var tm = (int)DateTime.Now.Subtract(st).TotalMilliseconds;

            System.Threading.Thread.MemoryBarrier();
            var finalMemory = System.GC.GetTotalMemory(true);
            var consumption = finalMemory - initialMemory;

            Console.WriteLine();
            Console.WriteLine("... Done, in {0} ms. Throughput: {1} characters / second.", tm.ToString("0,0"), (1000 * (decimal)json.Length / (tm > 0 ? tm : 1)).ToString("0,0.00"));
            Console.WriteLine();
            Console.WriteLine("\tMemory used : {0}", ((decimal)finalMemory).ToString("0,0"));
            Console.WriteLine();

            if (o is FathersData)
            {
                Console.WriteLine("Fathers : {0}", ((FathersData)(object)o).fathers.Length.ToString("0,0"));
                Console.WriteLine();
            }
            GC.Collect();
            Console.WriteLine("Press a key...");
            Console.WriteLine();
            Console.ReadKey();
        }

        static void SpeedTests()
        {
#if !THIS_JSON_PARSER_ONLY
            LoopTest(typeof(JavaScriptSerializer).FullName, new JavaScriptSerializer().DeserializeObject, OJ_TEST_FILE_PATH, 10000);
            LoopTest(GetVersionString(typeof(JsonConvert).Assembly.GetName()), JsonConvert.DeserializeObject, OJ_TEST_FILE_PATH, 10000);
#if RUN_SERVICESTACK_TESTS
            //LoopTest("ServiceStack", new JsonSerializer<object>().DeserializeFromString, OJ_TEST_FILE_PATH, 10000);
#endif
#endif
            LoopTest(typeof(JsonParser).FullName, new JsonParser().Parse, OJ_TEST_FILE_PATH, 10000);

#if !THIS_JSON_PARSER_ONLY
            LoopTest(typeof(JavaScriptSerializer).FullName, new JavaScriptSerializer().Deserialize<HighlyNested>, OJ_TEST_FILE_PATH, 10000);
            LoopTest(GetVersionString(typeof(JsonConvert).Assembly.GetName()), JsonConvert.DeserializeObject<HighlyNested>, OJ_TEST_FILE_PATH, 10000);
#if RUN_SERVICESTACK_TESTS
            LoopTest("ServiceStack", new JsonSerializer<HighlyNested>().DeserializeFromString, OJ_TEST_FILE_PATH, 10000);
#endif
#endif
            LoopTest(typeof(JsonParser).FullName, new JsonParser().Parse<HighlyNested>, OJ_TEST_FILE_PATH, 10000);

#if !THIS_JSON_PARSER_ONLY
            LoopTest(typeof(JavaScriptSerializer).FullName, new JavaScriptSerializer().DeserializeObject, OJ_TEST_FILE_PATH, 100000);
            LoopTest(GetVersionString(typeof(JsonConvert).Assembly.GetName()), JsonConvert.DeserializeObject, OJ_TEST_FILE_PATH, 100000);
#if RUN_SERVICESTACK_TESTS
            //LoopTest("ServiceStack", new JsonSerializer<object>().DeserializeFromString, OJ_TEST_FILE_PATH, 100000);
#endif
#endif
            LoopTest(typeof(JsonParser).FullName, new JsonParser().Parse, OJ_TEST_FILE_PATH, 100000);

#if !THIS_JSON_PARSER_ONLY
            LoopTest(typeof(JavaScriptSerializer).FullName, new JavaScriptSerializer().Deserialize<HighlyNested>, OJ_TEST_FILE_PATH, 100000);
            LoopTest(GetVersionString(typeof(JsonConvert).Assembly.GetName()), JsonConvert.DeserializeObject<HighlyNested>, OJ_TEST_FILE_PATH, 100000);
#if RUN_SERVICESTACK_TESTS
            LoopTest("ServiceStack", new JsonSerializer<HighlyNested>().DeserializeFromString, OJ_TEST_FILE_PATH, 100000);
#endif
#endif
            LoopTest(typeof(JsonParser).FullName, new JsonParser().Parse<HighlyNested>, OJ_TEST_FILE_PATH, 100000);

#if !THIS_JSON_PARSER_ONLY
            LoopTest(typeof(JavaScriptSerializer).FullName, new JavaScriptSerializer().Deserialize<BoonSmall>, BOON_SMALL_TEST_FILE_PATH, 1000000);
            LoopTest(GetVersionString(typeof(JsonConvert).Assembly.GetName()), JsonConvert.DeserializeObject<BoonSmall>, BOON_SMALL_TEST_FILE_PATH, 1000000);
#if RUN_SERVICESTACK_TESTS
            LoopTest("ServiceStack", new JsonSerializer<BoonSmall>().DeserializeFromString, BOON_SMALL_TEST_FILE_PATH, 1000000);
#endif
#endif
            LoopTest(typeof(JsonParser).FullName, new JsonParser().Parse<BoonSmall>, BOON_SMALL_TEST_FILE_PATH, 1000000);

#if !THIS_JSON_PARSER_ONLY
            LoopTest(typeof(JavaScriptSerializer).FullName, new JavaScriptSerializer().Deserialize<BoonSmall>, BOON_SMALL_TEST_FILE_PATH, 10000000);
            LoopTest(GetVersionString(typeof(JsonConvert).Assembly.GetName()), JsonConvert.DeserializeObject<BoonSmall>, BOON_SMALL_TEST_FILE_PATH, 10000000);
#if RUN_SERVICESTACK_TESTS
            LoopTest("ServiceStack", new JsonSerializer<BoonSmall>().DeserializeFromString, BOON_SMALL_TEST_FILE_PATH, 10000000);
#endif
#endif
            LoopTest(typeof(JsonParser).FullName, new JsonParser().Parse<BoonSmall>, BOON_SMALL_TEST_FILE_PATH, 10000000);

#if !THIS_JSON_PARSER_ONLY
            LoopTest(typeof(JavaScriptSerializer).FullName, new JavaScriptSerializer().Deserialize<Person>, TINY_TEST_FILE_PATH, 10000);
            LoopTest(GetVersionString(typeof(JsonConvert).Assembly.GetName()), JsonConvert.DeserializeObject<Person>, TINY_TEST_FILE_PATH, 10000);
#if RUN_SERVICESTACK_TESTS
            LoopTest("ServiceStack", new JsonSerializer<Person>().DeserializeFromString, TINY_TEST_FILE_PATH, 10000);
#endif
#endif
            LoopTest(typeof(JsonParser).FullName, new JsonParser().Parse<Person>, TINY_TEST_FILE_PATH, 10000);

#if !THIS_JSON_PARSER_ONLY
            LoopTest(typeof(JavaScriptSerializer).FullName, new JavaScriptSerializer().Deserialize<Person>, TINY_TEST_FILE_PATH, 100000);
            LoopTest(GetVersionString(typeof(JsonConvert).Assembly.GetName()), JsonConvert.DeserializeObject<Person>, TINY_TEST_FILE_PATH, 100000);
#if RUN_SERVICESTACK_TESTS
            LoopTest("ServiceStack", new JsonSerializer<Person>().DeserializeFromString, TINY_TEST_FILE_PATH, 100000);
#endif
#endif
            LoopTest(typeof(JsonParser).FullName, new JsonParser().Parse<Person>, TINY_TEST_FILE_PATH, 100000);

#if !THIS_JSON_PARSER_ONLY
            LoopTest(typeof(JavaScriptSerializer).FullName, new JavaScriptSerializer().Deserialize<Person>, TINY_TEST_FILE_PATH, 1000000);
            LoopTest(GetVersionString(typeof(JsonConvert).Assembly.GetName()), JsonConvert.DeserializeObject<Person>, TINY_TEST_FILE_PATH, 1000000);
#if RUN_SERVICESTACK_TESTS
            LoopTest("ServiceStack", new JsonSerializer<Person>().DeserializeFromString, TINY_TEST_FILE_PATH, 1000000);
#endif
#endif
            LoopTest(typeof(JsonParser).FullName, new JsonParser().Parse<Person>, TINY_TEST_FILE_PATH, 1000000);

#if !THIS_JSON_PARSER_ONLY
            //LoopTest(typeof(JavaScriptSerializer).FullName, new JavaScriptSerializer().Deserialize<DictionaryDataAdaptJsonNetServiceStack>, DICOS_TEST_FILE_PATH, 10000);//(Can't deserialize properly)
            LoopTest(GetVersionString(typeof(JsonConvert).Assembly.GetName()), JsonConvert.DeserializeObject<DictionaryDataAdaptJsonNetServiceStack>, DICOS_TEST_FILE_PATH, 10000);
#if RUN_SERVICESTACK_TESTS
            LoopTest("ServiceStack", new JsonSerializer<DictionaryDataAdaptJsonNetServiceStack>().DeserializeFromString, DICOS_TEST_FILE_PATH, 10000);
#endif
#endif
            LoopTest(typeof(JsonParser).FullName, new JsonParser().Parse<DictionaryData>, DICOS_TEST_FILE_PATH, 10000);

#if !THIS_JSON_PARSER_ONLY
            //LoopTest(typeof(JavaScriptSerializer).FullName, new JavaScriptSerializer().Deserialize<DictionaryDataAdaptJsonNetServiceStack>, DICOS_TEST_FILE_PATH, 100000);//(Can't deserialize properly)
            LoopTest(GetVersionString(typeof(JsonConvert).Assembly.GetName()), JsonConvert.DeserializeObject<DictionaryDataAdaptJsonNetServiceStack>, DICOS_TEST_FILE_PATH, 100000);
#if RUN_SERVICESTACK_TESTS
            LoopTest("ServiceStack", new JsonSerializer<DictionaryDataAdaptJsonNetServiceStack>().DeserializeFromString, DICOS_TEST_FILE_PATH, 100000);
#endif
#endif
            LoopTest(typeof(JsonParser).FullName, new JsonParser().Parse<DictionaryData>, DICOS_TEST_FILE_PATH, 100000);

#if !THIS_JSON_PARSER_ONLY
            //LoopTest(typeof(JavaScriptSerializer).FullName, new JavaScriptSerializer().Deserialize<DictionaryDataAdaptJsonNetServiceStack>, DICOS_TEST_FILE_PATH, 1000000);//(Can't deserialize properly)
            LoopTest(GetVersionString(typeof(JsonConvert).Assembly.GetName()), JsonConvert.DeserializeObject<DictionaryDataAdaptJsonNetServiceStack>, DICOS_TEST_FILE_PATH, 1000000);
#if RUN_SERVICESTACK_TESTS
            LoopTest("ServiceStack", new JsonSerializer<DictionaryDataAdaptJsonNetServiceStack>().DeserializeFromString, DICOS_TEST_FILE_PATH, 1000000);
#endif
#endif
            LoopTest(typeof(JsonParser).FullName, new JsonParser().Parse<DictionaryData>, DICOS_TEST_FILE_PATH, 1000000);

#if !THIS_JSON_PARSER_ONLY
            LoopTest(typeof(JavaScriptSerializer).FullName, new JavaScriptSerializer().DeserializeObject, SMALL_TEST_FILE_PATH, 10000);
            LoopTest(GetVersionString(typeof(JsonConvert).Assembly.GetName()), JsonConvert.DeserializeObject, SMALL_TEST_FILE_PATH, 10000);
#if RUN_SERVICESTACK_TESTS
            //LoopTest("ServiceStack", new JsonSerializer<object>().DeserializeFromString, SMALL_TEST_FILE_PATH, 10000);
#endif
#endif
            LoopTest(typeof(JsonParser).FullName, new JsonParser().Parse, SMALL_TEST_FILE_PATH, 10000);

#if !THIS_JSON_PARSER_ONLY
            LoopTest(typeof(JavaScriptSerializer).FullName, new JavaScriptSerializer().DeserializeObject, SMALL_TEST_FILE_PATH, 100000);
            LoopTest(GetVersionString(typeof(JsonConvert).Assembly.GetName()), JsonConvert.DeserializeObject, SMALL_TEST_FILE_PATH, 100000);//(Json.NET: OutOfMemoryException)
#if RUN_SERVICESTACK_TESTS
            //LoopTest("ServiceStack", new JsonSerializer<object>().DeserializeFromString, SMALL_TEST_FILE_PATH, 100000);
#endif
#endif
            LoopTest(typeof(JsonParser).FullName, new JsonParser().Parse, SMALL_TEST_FILE_PATH, 100000);

#if !THIS_JSON_PARSER_ONLY
            var msJss = new JavaScriptSerializer() { MaxJsonLength = int.MaxValue };
            Test(typeof(JavaScriptSerializer).FullName, msJss.Deserialize<FathersData>, FATHERS_TEST_FILE_PATH);
            Test(GetVersionString(typeof(JsonConvert).Assembly.GetName()), JsonConvert.DeserializeObject<FathersData>, FATHERS_TEST_FILE_PATH);
#if RUN_SERVICESTACK_TESTS
            Test("ServiceStack", new JsonSerializer<FathersData>().DeserializeFromString, FATHERS_TEST_FILE_PATH);
#endif
#endif
            Test(typeof(JsonParser).FullName, new JsonParser().Parse<FathersData>, FATHERS_TEST_FILE_PATH);

#if RUN_UNIT_TESTS && RUN_ADVANCED_JSONPATH_TESTS
            JsonPathScriptEvaluator evaluator =
                (script, value, context) =>
                    ((value is Type) && (context == script))
                    ?
                    ExpressionParser.Parse((Type)value, script, true, typeof(Data).Namespace).Compile()
                    :
                    null;
            var JSONPATH_SAMPLE_QUERY = "$.fathers[?(@.id == 28149)].daughters[?(@.age == 12)]";
#if !THIS_JSON_PARSER_ONLY
            Test // Note: requires Json.NET 6.0+
            (
                GetVersionString(typeof(JsonConvert).Assembly.GetName()), Newtonsoft.Json.Linq.JObject.Parse, FATHERS_TEST_FILE_PATH,
                JSONPATH_SAMPLE_QUERY,
                (parsed) => ((Newtonsoft.Json.Linq.JObject)parsed).SelectToken(JSONPATH_SAMPLE_QUERY),
                (selected) => ((Newtonsoft.Json.Linq.JToken)selected).Value<string>("name") == "Susan"
            );
#endif
            Test
            (
                typeof(JsonParser).FullName, new JsonParser().Parse<FathersData>, FATHERS_TEST_FILE_PATH,
                JSONPATH_SAMPLE_QUERY,
                (parsed) => parsed.ToJsonPath(evaluator).SelectNodes(JSONPATH_SAMPLE_QUERY),
                (selected) =>
                    (selected as JsonPathNode[]).Length > 0 &&
                    (selected as JsonPathNode[])[0].Value is Daughter &&
                    ((selected as JsonPathNode[])[0].Value as Daughter).name == "Susan"
            );
#endif

            if (File.Exists(HUGE_TEST_FILE_PATH))
            {
#if !THIS_JSON_PARSER_ONLY
                Test(typeof(JavaScriptSerializer).FullName, msJss.DeserializeObject, HUGE_TEST_FILE_PATH);
                Test(GetVersionString(typeof(JsonConvert).Assembly.GetName()), JsonConvert.DeserializeObject, HUGE_TEST_FILE_PATH);//(Json.NET: OutOfMemoryException)
#if RUN_SERVICESTACK_TESTS
                //Test("ServiceStack", new JsonSerializer<object>().DeserializeFromString, HUGE_TEST_FILE_PATH);
#endif
#endif
                Test(typeof(JsonParser).FullName, new JsonParser().Parse, HUGE_TEST_FILE_PATH);
            }

            StreamTest(null);

            // Note this will be invoked thru the filters dictionary passed to this 2nd "StreamTest" below, in order to:
            // 1) for each "Father", skip the parsing of the properties to be ignored (i.e., all but "id" and "name")
            // 2) while populating the resulting "Father[]" array, skip the deserialization of the first 29,995 fathers
            Func<object, object> mapperCallback = obj =>
            {
                Father father = (obj as Father);
                // Output only the individual fathers that the filters decided to keep
                // (i.e., when obj.Type equals typeof(Father)),
                // but don't output (even once) the resulting array
                // (i.e., when obj.Type equals typeof(Father[])):
                if (father != null)
                {
                    Console.WriteLine("\t\tId : {0}\t\tName : {1}", father.id, father.name);
                }
                // Do not project the filtered data in any specific way otherwise, just return it deserialized as-is:
                return obj;
            };

            StreamTest
            (
                new Dictionary<Type, Func<Type, object, object, int, Func<object, object>>>
                {
                    // We don't care about anything but these 2 properties:
                    {
                        typeof(Father),
                        (type, obj, key, index) =>
                            ((key as string) == "id" || (key as string) == "name") ?
                            mapperCallback :
                            JsonParser.Skip
                    },
                    // We want to pick only the last 5 fathers from the source:
                    {
                        typeof(Father[]),
                        (type, obj, key, index) =>
                            (index >= 29995) ?
                            mapperCallback :
                            JsonParser.Skip
                    }
                }
            );

            FilteredFatherStreamTestDaughterMaidenNamesFixup();
        }

        static void StreamTest(IDictionary<Type, Func<Type, object, object, int, Func<object, object>>> filter)
        {
            Console.Clear();
            System.Threading.Thread.MemoryBarrier();
            var initialMemory = System.GC.GetTotalMemory(true);
            using (var reader = new System.IO.StreamReader(FATHERS_TEST_FILE_PATH))
            {
                Console.WriteLine("\"Fathers\" Test... streamed{0} (press a key)", (filter != null) ? " AND filtered" : String.Empty);
                Console.WriteLine();
                Console.ReadKey();
                var st = DateTime.Now;
                var o = new JsonParser().Parse<FathersData>(reader, filter);
                var tm = (int)DateTime.Now.Subtract(st).TotalMilliseconds;

                System.Threading.Thread.MemoryBarrier();
                var finalMemory = System.GC.GetTotalMemory(true);
                var consumption = finalMemory - initialMemory;

                Console.WriteLine();
                if (filter == null)
                {
                    System.Diagnostics.Debug.Assert(o.fathers.Length == 30000);
                }
                Console.WriteLine();
                Console.WriteLine("... {0} ms", tm);
                Console.WriteLine();
                Console.WriteLine("\tMemory used : {0}", ((decimal)finalMemory).ToString("0,0"));
                Console.WriteLine();
            }
            Console.ReadKey();
        }

        static Dictionary<string, Post[]> FacebookPostDeserialization_SO_26426594(string input)
        {
            return new JsonParser().Parse<Dictionary<string, Post[]>>(input);
        }

        // Existing test (above) simplified for SO question "Deserialize json array stream one item at a time":
        // ( http://stackoverflow.com/questions/20374083/deserialize-json-array-stream-one-item-at-a-time )
        static void FilteredFatherStreamTestSimplified()
        {
            // Get our parser:
            var parser = new JsonParser();

            // (Note this will be invoked thanks to the "filters" dictionary below)
            Func<object, object> filteredFatherStreamCallback = obj =>
            {
                Father father = (obj as Father);
                // Output only the individual fathers that the filters decided to keep (i.e., when obj.Type equals typeof(Father)),
                // but don't output (even once) the resulting array (i.e., when obj.Type equals typeof(Father[])):
                if (father != null)
                {
                    Console.WriteLine("\t\tId : {0}\t\tName : {1}", father.id, father.name);
                }
                // Do not project the filtered data in any specific way otherwise,
                // just return it deserialized as-is:
                return obj;
            };

            // Prepare our filter, and thus:
            // 1) we want only the last five (5) fathers (array index in the resulting "Father[]" >= 29,995),
            // (assuming we somehow have prior knowledge that the total count is 30,000)
            // and for each of them,
            // 2) we're interested in deserializing them with only their "id" and "name" properties
            var filters =
                new Dictionary<Type, Func<Type, object, object, int, Func<object, object>>>
                {
                    // We don't care about anything but these 2 properties:
                    {
                        typeof(Father), // Note the type
                        (type, obj, key, index) =>
                            ((key as string) == "id" || (key as string) == "name") ?
                            filteredFatherStreamCallback :
                            JsonParser.Skip
                    },
                    // We want to pick only the last 5 fathers from the source:
                    {
                        typeof(Father[]), // Note the type
                        (type, obj, key, index) =>
                            (index >= 29995) ?
                            filteredFatherStreamCallback :
                            JsonParser.Skip
                    }
                };

            // Read, parse, and deserialize fathers.json.txt in a streamed fashion,
            // and using the above filters, along with the callback we've set up:
            using (var reader = new System.IO.StreamReader(FATHERS_TEST_FILE_PATH))
            {
                FathersData data = parser.Parse<FathersData>(reader, filters);

                System.Diagnostics.Debug.Assert
                (
                    (data != null) &&
                    (data.fathers != null) &&
                    (data.fathers.Length == 5)
                );
                foreach (var i in Enumerable.Range(29995, 5))
                    System.Diagnostics.Debug.Assert
                    (
                        (data.fathers[i - 29995].id == i) &&
                        !String.IsNullOrEmpty(data.fathers[i - 29995].name)
                    );
            }
            Console.ReadKey();
        }

        // This test deserializes the last ten (10) fathers found in fathers.json.txt,
        // and performs a fixup of the maiden names (all absent from fathers.json.txt)
        // of their daughters (if any):
        static void FilteredFatherStreamTestDaughterMaidenNamesFixup()
        {
            Console.Clear();
            Console.WriteLine("\"Fathers\" Test... streamed AND filtered");
            Console.WriteLine();
            Console.WriteLine("(static void FilteredFatherStreamTestDaughterMaidenNamesFixup())");
            Console.WriteLine();
            Console.WriteLine("(press a key)");
            Console.WriteLine();
            Console.ReadKey();

            // Get our parser:
            var parser = new JsonParser();

            // (Note this will be invoked thanks to the "filters" dictionary below)
            Func<object, object> filteredFatherStreamCallback = obj =>
            {
                Father father = (obj as Father);
                // Processes only the individual fathers that the filters decided to keep
                // (i.e., iff obj.Type equals typeof(Father))
                if (father != null)
                {
                    if ((father.daughters != null) && (father.daughters.Length > 0))
                        // The fixup of the maiden names is done in-place, on
                        // by-then freshly deserialized father's daughters:
                        foreach (var daughter in father.daughters)
                            daughter.maidenName = father.name.Substring(father.name.IndexOf(' ') + 1);
                }
                // Do not project the filtered data in any specific way otherwise,
                // just return it deserialized as-is:
                return obj;
            };

            // Prepare our filters, i.e., we want only the last ten (10) fathers
            // (array index in the resulting "Father[]" >= 29990)
            var filters =
                new Dictionary<Type, Func<Type, object, object, int, Func<object, object>>>
                {
                    // Necessary to perform post-processing on the daughters (if any)
                    // of each father we kept in "Father[]" via the 2nd filter below:
                    {
                        typeof(Father), // Note the type
                        (type, obj, key, index) => filteredFatherStreamCallback
                    },
                    // We want to pick only the last 10 fathers from the source:
                    {
                        typeof(Father[]), // Note the type
                        (type, obj, key, index) =>
                            (index >= 29990) ?
                            filteredFatherStreamCallback :
                            JsonParser.Skip
                    }
                };

            // Read, parse, and deserialize fathers.json.txt in a streamed fashion,
            // and using the above filters, along with the callback we've set up:
            using (var reader = new System.IO.StreamReader(FATHERS_TEST_FILE_PATH))
            {
                FathersData data = parser.Parse<FathersData>(reader, filters);

                System.Diagnostics.Debug.Assert
                (
                    (data != null) &&
                    (data.fathers != null) &&
                    (data.fathers.Length == 10)
                );
                foreach (var father in data.fathers)
                {
                    Console.WriteLine();
                    Console.WriteLine("\t\t{0}'s daughters:", father.name);
                    if ((father.daughters != null) && (father.daughters.Length > 0))
                        foreach (var daughter in father.daughters)
                        {
                            System.Diagnostics.Debug.Assert
                            (
                                !String.IsNullOrEmpty(daughter.maidenName)
                            );
                            Console.WriteLine("\t\t\t\t{0} {1}", daughter.name, daughter.maidenName);
                        }
                    else
                        Console.WriteLine("\t\t\t\t(None)");
                }
            }
            Console.WriteLine("Press a key...");
            Console.ReadKey();
        }

        static string GetVersionString(System.Reflection.AssemblyName assemblyName)
        {
            return String.Format("{0} {1}.{2}", assemblyName.Name, assemblyName.Version.Major, assemblyName.Version.MajorRevision);
        }

        public static void Run()
        {
#if RUN_UNIT_TESTS
            UnitTests();
#endif
            SpeedTests();
        }
    }
}
