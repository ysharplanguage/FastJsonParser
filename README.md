System.Text.Json
================

A minimalistic, and pretty fast JSON parser.

The complete source code (Visual Studio 2010 solution), tests, and test data, are in the "JsonTest" folder.

Performances
------------

Here are some figures, from the tests that are provided (note: YMMV)

(.NET 4.0 target, on an Ideapad w/ Intel Core i5 CPU @ 2.50GHz, 6GB RAM, running Win7 64bit, 98% idle CPU)

* "Loop" Test of tiny JSON (deserializing x times the JSON contained in the tiny.json.txt file = 91 bytes):
    * 10,000 iterations: in ~ 65 milliseconds vs. JSON.NET 5.0 r8 in ~ 250 milliseconds vs. ServiceStack in ~ 125 milliseconds
    * 100,000 iterations: in ~ 600 milliseconds vs. JSON.NET 5.0 r8 in ~ 900 milliseconds vs. ServiceStack in ~ 650 milliseconds
    * 1,000,000 iterations: in ~ 5.9 seconds vs. JSON.NET 5.0 r8 in ~ 8.3 seconds vs. ServiceStack in ~ 6.1 seconds

* "Loop" Test of small JSON (deserializing x times the JSON contained in the small.json.txt file ~ 3.5 KB):
    * 10,000 iterations: in ~ 1.2 second vs. JSON.NET 5.0 r8 in ~ 2.2 seconds vs. ServiceStack... N/A
    * 100,000 iterations: in ~ 12.4 seconds vs. JSON.NET 5.0 r8... OutOfMemoryException vs. ServiceStack... N/A

* Note: fathers.json.txt was generated using:
    * http://experiments.mennovanslooten.nl/2010/mockjson/tryit.html

* "Fathers" Test (12 MB JSON file):
    * Parsed in ~ 275 milliseconds vs. JSON.NET 5.0 r8 in ~ 500 milliseconds vs. ServiceStack in ~ 575 milliseconds

* "Huge" Test (180 MB JSON file):
    * Parsed in ~ 9.75 seconds vs. JSON.NET 5.0 r8... OutOfMemoryException vs. ServiceStack... N/A

As for huge.json.txt, it is just a copy of this file:

https://github.com/zeMirco/sf-city-lots-json
