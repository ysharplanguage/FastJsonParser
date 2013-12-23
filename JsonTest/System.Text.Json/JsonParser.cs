/*
 * Copyright (c) 2013 Cyril Jandia
 *
 * http://www.cjandia.com/
 *
Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
``Software''), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be included
in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED ``AS IS'', WITHOUT WARRANTY OF ANY KIND, EXPRESS
OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL CYRIL JANDIA BE LIABLE FOR ANY CLAIM, DAMAGES OR
OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.

Except as contained in this notice, the name of Cyril Jandia shall
not be used in advertising or otherwise to promote the sale, use or
other dealings in this Software without prior written authorization
from Cyril Jandia.

Inquiries : ysharp {dot} design {at} gmail {dot} com
 *
 */

// On GitHub:
// https://github.com/ysharplanguage/FastJsonParser

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Text.Json
{
    public class JsonParser
    {
        private static readonly short[] HEX = new short[128];
        private static readonly bool[] HXD = new bool[128];
        private static readonly char[] ESC = new char[128];
        private static readonly bool[] IDF = new bool[128];
        private static readonly bool[] IDN = new bool[128];
        private const int EOF = (char.MaxValue + 1);
        private const int ANY = 0;
        private const int LBS = 128;
        private const int TDS = 128;

        private IDictionary<Type, int> thash = new Dictionary<Type, int>();
        private TypeInfo[] types = new TypeInfo[TDS];

        private Func<int, object>[] parse = new Func<int, object>[128];
        private StringBuilder lsb = new StringBuilder();
        private char[] lbf = new char[LBS];
        private char[] stc = new char[1];
        private System.IO.TextReader str;
        private Func<int, int> Char;
        private Action<int> Next;
        private Func<int> Read;
        private Func<int> Space;
        private string txt;
        private int len;
        private int lln;
        private int chr;
        private int at;

        internal class PropInfo
        {
            internal string Name;
            internal Action<object, JsonParser, int> Set;
            internal Type Type;
            internal int Outer;
        }

        internal class TypeInfo
        {
            private static readonly HashSet<Type> WellKnown = new HashSet<Type>();

            internal Func<object> Ctor;
            internal PropInfo[] Prop;
            internal PropInfo Item;
            internal Type ElementType;
            internal Type Type;
            internal int Outer;
            internal int Inner;

            static TypeInfo()
            {
                WellKnown.Add(typeof(bool));
                WellKnown.Add(typeof(byte));
                WellKnown.Add(typeof(short));
                WellKnown.Add(typeof(int));
                WellKnown.Add(typeof(long));
                WellKnown.Add(typeof(float));
                WellKnown.Add(typeof(double));
                WellKnown.Add(typeof(decimal));
                WellKnown.Add(typeof(DateTime));
                WellKnown.Add(typeof(string));
            }

            private Func<object> GetCtor(Type clr, bool list)
            {
                var type = (!list ? ((clr == typeof(object)) ? typeof(Dictionary<string, object>) : clr) : typeof(List<>).MakeGenericType(clr));
                var ctor = type.GetConstructor(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.CreateInstance, null, System.Type.EmptyTypes, null);
                if (ctor != null)
                {
                    var dyn = new System.Reflection.Emit.DynamicMethod("", typeof(object), null, type, true);
                    var il = dyn.GetILGenerator();
                    il.Emit(System.Reflection.Emit.OpCodes.Newobj, ctor);
                    il.Emit(System.Reflection.Emit.OpCodes.Ret);
                    return (Func<object>)dyn.CreateDelegate(typeof(Func<object>));
                }
                return null;
            }

            private string GetParseName(Type type)
            {
                return (!WellKnown.Contains(type) ? ((type.IsEnum && WellKnown.Contains(type.GetEnumUnderlyingType())) ? type.GetEnumUnderlyingType().Name : null) : type.Name);
            }

            private System.Reflection.MethodInfo GetParseMethod(string parseName)
            {
                return typeof(JsonParser).GetMethod((parseName ?? "Val"), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            }

            private PropInfo GetPropInfo(Type type, string name, System.Reflection.MethodInfo set, System.Reflection.MethodInfo parse)
            {
                var dyn = new System.Reflection.Emit.DynamicMethod("Set" + name, null, new Type[] { typeof(object), typeof(JsonParser), typeof(int) }, typeof(string), true);
                var il = dyn.GetILGenerator();
                il.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
                il.Emit(System.Reflection.Emit.OpCodes.Ldarg_1);
                il.Emit(System.Reflection.Emit.OpCodes.Ldarg_2);
                il.Emit(System.Reflection.Emit.OpCodes.Callvirt, parse);
                il.Emit(System.Reflection.Emit.OpCodes.Callvirt, set);
                il.Emit(System.Reflection.Emit.OpCodes.Ret);
                return new PropInfo { Type = type, Name = name, Set = (Action<object, JsonParser, int>)dyn.CreateDelegate(typeof(Action<object, JsonParser, int>)) };
            }

            internal TypeInfo(Type type, int outer, Type elem)
            {
                var info = type.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                var prop = new SortedList<string, PropInfo>();
                Type = type;
                Outer = outer;
                ElementType = elem;
                Ctor = GetCtor((ElementType ?? Type), (ElementType != null));
                for (var i = 0; i < info.Length; i++)
                    if (info[i].CanWrite)
                        prop.Add(info[i].Name, GetPropInfo(info[i].PropertyType, info[i].Name, info[i].GetSetMethod(), GetParseMethod(GetParseName(info[i].PropertyType))));
                Prop = prop.Values.ToArray();
                Item = ((ElementType != null) ? GetPropInfo(ElementType, "", typeof(List<>).MakeGenericType(ElementType).GetMethod("Add", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public), GetParseMethod(GetParseName(ElementType))) : null);
            }
        }

        static JsonParser()
        {
            for (char c = '0'; c <= '9'; c++) { HXD[c] = true; HEX[c] = (short)(c - 48); }
            for (char c = 'A'; c <= 'F'; c++) { HXD[c] = HXD[c + 32] = true; HEX[c] = HEX[c + 32] = (short)(c - 55); }
            ESC['/'] = '/'; ESC['\\'] = '\\';
            ESC['b'] = '\b'; ESC['f'] = '\f'; ESC['n'] = '\n'; ESC['r'] = '\r'; ESC['t'] = '\t'; ESC['u'] = 'u';
            for (int c = ANY; c < 128; c++) if (ESC[c] == ANY) ESC[c] = (char)c;
            for (int c = '0'; c <= '9'; c++) IDN[c] = true;
            IDF['_'] = IDN['_'] = true;
            for (int c = 'A'; c <= 'Z'; c++) IDF[c] = IDN[c] = IDF[c + 32] = IDN[c + 32] = true;
        }

        private Exception Error(string message) { return new Exception(System.String.Format("{0} at {1} (found: '{2}')", message, at, ((chr < EOF) ? ("\\" + chr) : "EOF"))); }
        private void Reset(Func<int> read, Action<int> next, Func<int, int> achar, Func<int> space) { at = -1; chr = ANY; Read = read; Next = next; Char = achar; Space = space; }

        private int StreamSpace() { if (chr <= ' ') while ((chr = (str.Read(stc, 0, 1) > 0) ? stc[0] : EOF) <= ' ') ; return chr; }
        private int StreamRead() { return (chr = (str.Read(stc, 0, 1) > 0) ? stc[0] : EOF); }
        private void StreamNext(int ch) { if (chr != ch) throw Error("Unexpected character"); chr = ((str.Read(stc, 0, 1) > 0) ? stc[0] : EOF); }
        private int StreamChar(int ch)
        {
            if (lln >= LBS)
            {
                if (lsb.Length == 0)
                    lsb.Append(new string(lbf, 0, lln));
                lsb.Append((char)ch);
            }
            else
                lbf[lln++] = (char)ch;
            return (chr = (str.Read(stc, 0, 1) > 0) ? stc[0] : EOF);
        }

        private int StringSpace() { if (chr <= ' ') while ((++at < len) && ((chr = txt[at]) <= ' ')) ; return chr; }
        private int StringRead() { return (chr = (++at < len) ? txt[at] : EOF); }
        private void StringNext(int ch) { if (chr != ch) throw Error("Unexpected character"); chr = ((++at < len) ? txt[at] : EOF); }
        private int StringChar(int ch)
        {
            if (lln >= LBS)
            {
                if (lsb.Length == 0)
                    lsb.Append(new string(lbf, 0, lln));
                lsb.Append((char)ch);
            }
            else
                lbf[lln++] = (char)ch;
            return (chr = (++at < len) ? txt[at] : EOF);
        }

        private int Esc(int ec)
        {
            int ch;
            if (ec == 'u')
            {
                short cp = 0, ic = -1;
                while ((++ic < 4) && ((ch = Read()) <= 'f') && HXD[ch]) { cp *= 16; cp += HEX[ch]; }
                if (ic < 4) throw Error("Invalid Unicode character");
                ch = Convert.ToChar(cp);
            }
            else
                ch = ESC[ec];
            Char(ch);
            return ch;
        }

        private bool Boolean(int outer)
        {
            var ch = Space();
            switch (ch)
            {
                case 'f': Next('f'); Next('a'); Next('l'); Next('s'); Next('e'); return false;
                case 't': Next('t'); Next('r'); Next('u'); Next('e'); return true;
                default: throw Error("Bad boolean");
            }
        }

        private byte Byte(int outer)
        {
            var ch = Space();
            bool b = false;
            byte n = 0;
            while ((ch >= '0') && (ch <= '9') && (b = true)) { n *= 10; n += (byte)(ch - 48); ch = Read(); }
            if (!b) throw Error("Bad number");
            return n;
        }

        private short Int16(int outer)
        {
            short it = 1, n = 0;
            var ch = Space();
            bool b = false;
            if (ch == '-') { ch = Read(); it = (short)-it; }
            while ((ch >= '0') && (ch <= '9') && (b = true)) { n *= 10; n += (short)(ch - 48); ch = Read(); }
            if (!b) throw Error("Bad number");
            return (short)(it * n);
        }

        private int Int32(int outer)
        {
            int it = 1, n = 0;
            var ch = Space();
            bool b = false;
            if (ch == '-') { ch = Read(); it = -it; }
            while ((ch >= '0') && (ch <= '9') && (b = true)) { n *= 10; n += (ch - 48); ch = Read(); }
            if (!b) throw Error("Bad number");
            return (it * n);
        }

        private long Int64(int outer)
        {
            long it = 1, n = 0;
            var ch = Space();
            bool b = false;
            if (ch == '-') { ch = Read(); it = -it; }
            while ((ch >= '0') && (ch <= '9') && (b = true)) { n *= 10; n += (ch - 48); ch = Read(); }
            if (!b) throw Error("Bad number");
            return (it * n);
        }

        private float Single(int outer)
        {
            var ch = Space();
            bool b = false;
            string s;
            lsb.Length = 0; lln = 0;
            if (ch == '-') ch = Char(ch);
            while ((ch >= '0') && (ch <= '9') && (b = true)) ch = Char(ch);
            if (ch == '.')
            {
                ch = Char(ch);
                while ((ch >= '0') && (ch <= '9')) ch = Char(ch);
            }
            if ((ch == 'e') || (ch == 'E'))
            {
                ch = Char(ch);
                if ((ch == '-') || (ch == '+')) ch = Char(ch);
                while ((ch >= '0') && (ch <= '9')) ch = Char(ch);
            }
            if (!b) throw Error("Bad number");
            s = ((lsb.Length > 0) ? lsb.ToString() : new string(lbf, 0, lln));
            return float.Parse(s);
        }

        private double Double(int outer)
        {
            var ch = Space();
            bool b = false;
            string s;
            lsb.Length = 0; lln = 0;
            if (ch == '-') ch = Char(ch);
            while ((ch >= '0') && (ch <= '9') && (b = true)) ch = Char(ch);
            if (ch == '.')
            {
                ch = Char(ch);
                while ((ch >= '0') && (ch <= '9')) ch = Char(ch);
            }
            if ((ch == 'e') || (ch == 'E'))
            {
                ch = Char(ch);
                if ((ch == '-') || (ch == '+')) ch = Char(ch);
                while ((ch >= '0') && (ch <= '9')) ch = Char(ch);
            }
            if (!b) throw Error("Bad number");
            s = ((lsb.Length > 0) ? lsb.ToString() : new string(lbf, 0, lln));
            return double.Parse(s);
        }

        private decimal Decimal(int outer)
        {
            var ch = Space();
            bool b = false;
            string s;
            lsb.Length = 0; lln = 0;
            if (ch == '-') ch = Char(ch);
            while ((ch >= '0') && (ch <= '9') && (b = true)) ch = Char(ch);
            if (ch == '.')
            {
                ch = Char(ch);
                while ((ch >= '0') && (ch <= '9')) ch = Char(ch);
            }
            if (!b) throw Error("Bad number");
            s = ((lsb.Length > 0) ? lsb.ToString() : new string(lbf, 0, lln));
            return decimal.Parse(s);
        }

        private string String(int outer)
        {
            var ch = Space();
            var ec = false;
            if (ch == '"')
            {
                Read();
                lsb.Length = 0; lln = 0;
                while (true)
                {
                    switch (ch = chr)
                    {
                        case '\\':
                            ch = Read(); ec = true;
                            break;
                        case '"':
                            Read();
                            return ((lsb.Length > 0) ? lsb.ToString() : new string(lbf, 0, lln));
                        default:
                            break;
                    }
                    if (ch < EOF) { if (!ec || (ch >= 128)) Char(ch); else { Esc(ch); ec = false; } } else break;
                }
            }
            throw Error((outer > 0) ? "Bad string" : "Bad key");
        }

        private DateTime DateTime(int outer)
        {
            return System.DateTime.Parse(String(0));
        }

        private PropInfo Key(int outer)
        {
            var a = types[outer].Prop; int ch = Space(), n = a.Length, c = 0, i = 0, nc = 0;
            bool k = (n > 0), ec = false;
            PropInfo p = null;
            string s = null;
            if (ch == '"')
            {
                Read();
                lsb.Length = 0; lln = 0;
                while (true)
                {
                    switch (ch = chr)
                    {
                        case '\\':
                            ch = Read(); ec = true;
                            break;
                        case '"':
                            Read();
                            return ((i < n) ? p : null);
                        default:
                            break;
                    }
                    if (ch < EOF) { if (!ec || (ch >= 128)) Char(nc = ch); else { nc = Esc(ch); ec = false; } } else break;
                    if (k)
                    {
                        while ((i < n) && ((c >= (s = (p = a[i]).Name).Length) || (s[c] != nc))) i++;
                        c++;
                    }
                }
            }
            throw Error("Bad key");
        }

        private object Error(int outer) { throw Error("Bad value"); }
        private object Null(int outer) { Next('n'); Next('u'); Next('l'); Next('l'); return null; }
        private object False(int outer) { return Boolean(0); }
        private object True(int outer) { return Boolean(0); }

        private object Num(int outer)
        {
            var ch = Space();
            bool b = false;
            lsb.Length = 0; lln = 0;
            if (ch == '-') ch = Char(ch);
            while ((ch >= '0') && (ch <= '9') && (b = true)) ch = Char(ch);
            if (ch == '.')
            {
                ch = Char(ch);
                while ((ch >= '0') && (ch <= '9')) ch = Char(ch);
            }
            if ((ch == 'e') || (ch == 'E'))
            {
                ch = Char(ch);
                if ((ch == '-') || (ch == '+')) ch = Char(ch);
                while ((ch >= '0') && (ch <= '9')) ch = Char(ch);
            }
            if (!b) throw Error("Bad number");
            return ((lsb.Length > 0) ? lsb.ToString() : new string(lbf, 0, lln));
        }

        private object Str(int outer) { return String(0); }

        private object Obj(int outer)
        {
            var cached = types[outer];
            var obj = cached.Ctor();
            var ch = chr;
            if (ch == '{')
            {
                Read();
                ch = Space();
                if (ch == '}')
                {
                    Read();
                    return obj;
                }
                while (ch < EOF)
                {
                    var prop = ((outer > 0) ? Key(outer) : null);
                    var hash = ((outer == 0) ? String(0) : null);
                    Space();
                    Next(':');
                    if (prop != null)
                        prop.Set(obj, this, prop.Outer);
                    else if (hash != null)
                        ((IDictionary)obj).Add(hash, Val(0));
                    else
                        Val(0);
                    ch = Space();
                    if (ch == '}')
                    {
                        Read();
                        return obj;
                    }
                    Next(',');
                    ch = Space();
                }
            }
            throw Error("Bad object");
        }

        private object Arr(int outer)
        {
            var cached = types[(outer > 0) ? outer : 1];
            var item = cached.Item;
            var ch = chr;
            if (ch == '[')
            {
                IList list;
                Read();
                ch = Space();
                list = (IList)cached.Ctor();
                if (ch == ']')
                {
                    Read();
                    if ((outer > 0) && cached.Type.IsArray)
                    {
                        var array = Array.CreateInstance(cached.ElementType, list.Count);
                        list.CopyTo(array, 0);
                        return array;
                    }
                    return list;
                }
                while (ch < EOF)
                {
                    item.Set(list, this, cached.Inner);
                    ch = Space();
                    if (ch == ']')
                    {
                        Read();
                        if ((outer > 0) && cached.Type.IsArray)
                        {
                            var array = Array.CreateInstance(cached.ElementType, list.Count);
                            list.CopyTo(array, 0);
                            return array;
                        }
                        return list;
                    }
                    Next(',');
                    ch = Space();
                }
            }
            throw Error("Bad array");
        }

        private object Val(int outer)
        {
            return parse[Space() & 0x7f](outer);
        }

        private Type GetElementType(Type type)
        {
            if (type.IsArray)
                return type.GetElementType();
            else if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
                return (type.IsGenericType ? type.GetGenericArguments()[0] : typeof(object));
            else
                return null;
        }

        private int Closure(int outer)
        {
            var prop = types[outer].Prop;
            for (var i = 0; i < prop.Length; i++) prop[i].Outer = Entry(prop[i].Type);
            return outer;
        }

        private int Entry(Type type)
        {
            return Entry(type, null);
        }

        private int Entry(Type type, Type etype)
        {
            int outer;
            if (!thash.TryGetValue(type, out outer))
            {
                bool b = (etype != null);
                outer = thash.Count;
                etype = (etype ?? GetElementType(type));
                types[outer] = new TypeInfo(type, outer, etype);
                types[outer].Ctor = types[outer].Ctor;
                thash.Add(type, outer);
                if ((etype != null) && !b) types[outer].Inner = Entry(etype);
            }
            return Closure(outer);
        }

        private T DoParse<T>(string input)
        {
            len = input.Length;
            txt = input;
            Reset(StringRead, StringNext, StringChar, StringSpace);
            return (T)Val(Entry(typeof(T)));
        }

        private T DoParse<T>(System.IO.TextReader input)
        {
            str = input;
            Reset(StreamRead, StreamNext, StreamChar, StreamSpace);
            return (T)Val(Entry(typeof(T)));
        }

        public JsonParser()
        {
            parse['n'] = Null; parse['f'] = False; parse['t'] = True;
            parse['0'] = parse['1'] = parse['2'] = parse['3'] = parse['4'] =
            parse['5'] = parse['6'] = parse['7'] = parse['8'] = parse['9'] =
            parse['-'] = Num; parse['"'] = Str; parse['{'] = Obj; parse['['] = Arr;
            for (var input = 0; input < 128; input++) parse[input] = (parse[input] ?? Error);
            Entry(typeof(object));
            Entry(typeof(object[]));
        }

        public T Parse<T>(string input)
        {
            if (input == null) throw new ArgumentNullException("input", "cannot be null");
            return DoParse<T>(input);
        }

        public T Parse<T>(System.IO.TextReader input)
        {
            if (input == null) throw new ArgumentNullException("input", "cannot be null");
            return DoParse<T>(input);
        }

        public T Parse<T>(System.IO.Stream input)
        {
            if (input == null) throw new ArgumentNullException("input", "cannot be null");
            return DoParse<T>(new System.IO.StreamReader(input));
        }
    }
}
