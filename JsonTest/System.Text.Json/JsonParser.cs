// On GitHub: https://github.com/ysharplanguage/FastJsonParser
/*
 * Copyright (c) 2013, 2014 Cyril Jandia
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace System.Text.Json
{
    using JsonPath; // ( See http://goessner.net/articles/JsonPath/ and http://code.google.com/p/jsonpath/ )

    public sealed class JsonPathSelection
    {
        public readonly JsonPathContext Context;
        public readonly object Data;
        internal JsonPathSelection(JsonPathContext context, object data) { Context = context; Data = data; }
        public JsonPathNode[] SelectNodes(string expr, params JsonPathScriptEvaluator[] lambdas) { return Context.SelectNodes(Data, expr, lambdas); }
    }

    public static class JsonParserExtensions
    {
        public static JsonPathSelection ToJsonPath(this object data) { return JsonParser.ToJsonPath(data); }

        public static JsonPathSelection ToJsonPath(this object data, JsonPathScriptEvaluator eval) { return JsonParser.ToJsonPath(data, eval); }
    }

    public sealed class JsonParserValueSystem : IJsonPathValueSystem
    {
        public bool HasMember(object value, string member)
        {
            if (value != null)
            {
                if (IsArray(value))
                {
                    int index = ParseInt(member, -1);
                    return ((index >= 0) && (index < ((IList)value).Count));
                }
                else if (IsObject(value))
                    return ((value is IDictionary) ? ((IDictionary)value).Contains(member) : (value.GetType().GetProperty(member, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public) != null));
                else
                    return false;
            }
            return false;
        }

        public object GetMemberValue(object value, string member)
        {
            if (value != null)
            {
                if (IsArray(value))
                {
                    int index = ParseInt(member, -1);
                    return (((index >= 0) && (index < ((IList)value).Count)) ? ((IList)value)[index] : null);
                }
                else if (IsObject(value))
                    return ((value is IDictionary) ? ((IDictionary)value)[member] : value.GetType().GetProperty(member, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).GetValue(value, null));
                else
                    return null;
            }
            return null;
        }

        public IEnumerable GetMembers(object value)
        {
            return ((value is IDictionary) ? ((IDictionary)value).Keys : value.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).Select(prop => prop.Name).ToArray());
        }

        public bool IsObject(object value)
        {
            return ((value is IDictionary) || (!IsArray(value) && !IsPrimitive(value)));
        }

        public bool IsArray(object value)
        {
            return (value is IList);
        }

        public bool IsPrimitive(object value)
        {
            return ((value is string) || ((value != null) && value.GetType().IsValueType));
        }

        private int ParseInt(string s, int defaultValue)
        {
            int result;
            return int.TryParse(s, out result) ? result : defaultValue;
        }
    }

    public class JsonParser
    {
        private const string TypeTag1 = "__type";
        private const string TypeTag2 = "$type";

        private static readonly byte[] HEX = new byte[128];
        private static readonly bool[] HXD = new bool[128];
        private static readonly char[] ESC = new char[128];
        private static readonly bool[] IDF = new bool[128];
        private static readonly bool[] IDN = new bool[128];
        private const int EOF = (char.MaxValue + 1);
        private const int ANY = 0;
        private const int LBS = 128;
        private const int TDS = 128;

        private IDictionary<Type, int> rtti = new Dictionary<Type, int>();
        private TypeInfo[] types = new TypeInfo[TDS];

        private Func<int, object>[] parse = new Func<int, object>[128];
        private StringBuilder lsb = new StringBuilder();
        private char[] lbf = new char[LBS];
        private char[] stc = new char[1];
        private TextReader str;
        private Func<int, int> Char;
        private Action<int> Next;
        private Func<int> Read;
        private Func<int> Space;
        private string txt;
        private int len;
        private int lln;
        private int chr;
        private int at;

        internal class EnumInfo
        {
            internal string Name;
            internal long Value;
            internal int Len;
        }

        internal class ItemInfo
        {
            internal string Name;
            internal Action<object, JsonParser, int, int> Set;
            internal Type Type;
            internal int Outer;
            internal int Len;
        }

        internal class TypeInfo
        {
            private static readonly HashSet<Type> WellKnown = new HashSet<Type>();

            internal Func<Type, object, object, int, Func<object, object>> Select;
            internal Func<JsonParser, int, object> Parse;
            internal Func<object> Ctor;
            internal EnumInfo[] Enums;
            internal ItemInfo[] Props;
            internal ItemInfo Dico;
            internal ItemInfo List;
            internal bool IsStruct;
            internal bool IsEnum;
            internal Type EType;
            internal Type Type;
            internal int Inner;
            internal int Key;
            internal int T;

            static TypeInfo()
            {
                WellKnown.Add(typeof(bool));
                WellKnown.Add(typeof(char));
                WellKnown.Add(typeof(byte));
                WellKnown.Add(typeof(short));
                WellKnown.Add(typeof(int));
                WellKnown.Add(typeof(long));
                WellKnown.Add(typeof(float));
                WellKnown.Add(typeof(double));
                WellKnown.Add(typeof(decimal));
                WellKnown.Add(typeof(DateTime));
                WellKnown.Add(typeof(DateTimeOffset));
                WellKnown.Add(typeof(string));
            }

            private Func<object> GetCtor(Type clr, bool list)
            {
                var type = (!list ? ((clr == typeof(object)) ? typeof(Dictionary<string, object>) : clr) : typeof(List<>).MakeGenericType(clr));
                var ctor = type.GetConstructor(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.CreateInstance, null, System.Type.EmptyTypes, null);
                if (ctor != null)
                {
                    var dyn = new System.Reflection.Emit.DynamicMethod("", typeof(object), null, typeof(string), true);
                    var il = dyn.GetILGenerator();
                    il.Emit(System.Reflection.Emit.OpCodes.Newobj, ctor);
                    il.Emit(System.Reflection.Emit.OpCodes.Ret);
                    return (Func<object>)dyn.CreateDelegate(typeof(Func<object>));
                }
                return null;
            }

            private Func<object> GetCtor(Type clr, Type key, Type value)
            {
                var type = typeof(Dictionary<,>).MakeGenericType(key, value);
                var ctor = (type = (((type != clr) && clr.IsClass) ? clr : type)).GetConstructor(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.CreateInstance, null, System.Type.EmptyTypes, null);
                var dyn = new System.Reflection.Emit.DynamicMethod("", typeof(object), null, typeof(string), true);
                var il = dyn.GetILGenerator();
                il.Emit(System.Reflection.Emit.OpCodes.Newobj, ctor);
                il.Emit(System.Reflection.Emit.OpCodes.Ret);
                return (Func<object>)dyn.CreateDelegate(typeof(Func<object>));
            }

            private EnumInfo[] GetEnumInfos(Type type)
            {
                var einfo = new Dictionary<string, EnumInfo>();
                foreach (var name in System.Enum.GetNames(type))
                    einfo.Add(name, new EnumInfo { Name = name, Value = Convert.ToInt64(System.Enum.Parse(type, name)), Len = name.Length });
                return einfo.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToArray();
            }

            private ItemInfo GetItemInfo(Type type, string name, System.Reflection.MethodInfo setter)
            {
                var method = new System.Reflection.Emit.DynamicMethod("Set" + name, null, new Type[] { typeof(object), typeof(JsonParser), typeof(int), typeof(int) }, typeof(string), true);
                var parse = GetParserParse(GetParseName(type));
                var il = method.GetILGenerator();
                il.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
                il.Emit(System.Reflection.Emit.OpCodes.Ldarg_1);
                il.Emit(System.Reflection.Emit.OpCodes.Ldarg_2);
                il.Emit(System.Reflection.Emit.OpCodes.Callvirt, parse);
                if (type.IsValueType && (parse.ReturnType == typeof(object)))
                    il.Emit(System.Reflection.Emit.OpCodes.Unbox_Any, type);
                if (parse.ReturnType.IsValueType && (type == typeof(object)))
                    il.Emit(System.Reflection.Emit.OpCodes.Box, parse.ReturnType);
                il.Emit(System.Reflection.Emit.OpCodes.Callvirt, setter);
                il.Emit(System.Reflection.Emit.OpCodes.Ret);
                return new ItemInfo { Type = type, Name = name, Set = (Action<object, JsonParser, int, int>)method.CreateDelegate(typeof(Action<object, JsonParser, int, int>)), Len = name.Length };
            }

            private ItemInfo GetItemInfo(Type type, Type key, Type value, System.Reflection.MethodInfo setter)
            {
                var method = new System.Reflection.Emit.DynamicMethod("Add", null, new Type[] { typeof(object), typeof(JsonParser), typeof(int), typeof(int) }, typeof(string), true);
                var sBrace = typeof(JsonParser).GetMethod("SBrace", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var eBrace = typeof(JsonParser).GetMethod("EBrace", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var kColon = typeof(JsonParser).GetMethod("KColon", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var sComma = typeof(JsonParser).GetMethod("SComma", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var vParse = GetParserParse(GetParseName(value));
                var kParse = GetParserParse(GetParseName(key));
                var il = method.GetILGenerator();
                il.DeclareLocal(key);
                il.DeclareLocal(value);

                il.Emit(System.Reflection.Emit.OpCodes.Ldarg_1);
                il.Emit(System.Reflection.Emit.OpCodes.Callvirt, sBrace);

                il.Emit(System.Reflection.Emit.OpCodes.Ldarg_1);
                il.Emit(System.Reflection.Emit.OpCodes.Callvirt, kColon);

                il.Emit(System.Reflection.Emit.OpCodes.Ldarg_1);
                il.Emit(System.Reflection.Emit.OpCodes.Ldarg_3);
                il.Emit(System.Reflection.Emit.OpCodes.Callvirt, kParse);
                if (key.IsValueType && (kParse.ReturnType == typeof(object)))
                    il.Emit(System.Reflection.Emit.OpCodes.Unbox_Any, key);
                if (kParse.ReturnType.IsValueType && (key == typeof(object)))
                    il.Emit(System.Reflection.Emit.OpCodes.Box, kParse.ReturnType);
                il.Emit(System.Reflection.Emit.OpCodes.Stloc_0);

                il.Emit(System.Reflection.Emit.OpCodes.Ldarg_1);
                il.Emit(System.Reflection.Emit.OpCodes.Callvirt, sComma);

                il.Emit(System.Reflection.Emit.OpCodes.Ldarg_1);
                il.Emit(System.Reflection.Emit.OpCodes.Callvirt, kColon);

                il.Emit(System.Reflection.Emit.OpCodes.Ldarg_1);
                il.Emit(System.Reflection.Emit.OpCodes.Ldarg_2);
                il.Emit(System.Reflection.Emit.OpCodes.Callvirt, vParse);
                if (value.IsValueType && (vParse.ReturnType == typeof(object)))
                    il.Emit(System.Reflection.Emit.OpCodes.Unbox_Any, value);
                if (vParse.ReturnType.IsValueType && (value == typeof(object)))
                    il.Emit(System.Reflection.Emit.OpCodes.Box, vParse.ReturnType);
                il.Emit(System.Reflection.Emit.OpCodes.Stloc_1);

                il.Emit(System.Reflection.Emit.OpCodes.Ldarg_1);
                il.Emit(System.Reflection.Emit.OpCodes.Callvirt, eBrace);

                il.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
                il.Emit(System.Reflection.Emit.OpCodes.Ldloc_0);
                il.Emit(System.Reflection.Emit.OpCodes.Ldloc_1);
                il.Emit(System.Reflection.Emit.OpCodes.Callvirt, setter);
                il.Emit(System.Reflection.Emit.OpCodes.Ret);
                return new ItemInfo { Type = type, Name = String.Empty, Set = (Action<object, JsonParser, int, int>)method.CreateDelegate(typeof(Action<object, JsonParser, int, int>)) };
            }

            private Type GetEnumUnderlyingType(Type enumType)
            {
                return enumType.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)[0].FieldType;
            }

            protected string GetParseName(Type type)
            {
                var typeName = (!WellKnown.Contains(type) ? ((type.IsEnum && WellKnown.Contains(GetEnumUnderlyingType(type))) ? GetEnumUnderlyingType(type).Name : null) : type.Name);
                return ((typeName != null) ? String.Concat("Parse", typeName) : null);
            }

            protected System.Reflection.MethodInfo GetParserParse(string pName)
            {
                return typeof(JsonParser).GetMethod((pName ?? "Val"), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            }

            protected TypeInfo(Type type, int self, Type eType, Type kType, Type vType)
            {
                var props = ((self > 2) ? type.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public) : new System.Reflection.PropertyInfo[] { });
                var infos = new Dictionary<string, ItemInfo>();
                IsStruct = type.IsValueType;
                IsEnum = type.IsEnum;
                EType = eType;
                Type = type;
                T = self;
                Ctor = (((kType != null) && (vType != null)) ? GetCtor(Type, kType, vType) : GetCtor((EType ?? Type), (EType != null)));
                for (var i = 0; i < props.Length; i++)
                {
                    System.Reflection.PropertyInfo pi;
                    System.Reflection.MethodInfo set;
                    if ((pi = props[i]).CanWrite && ((set = pi.GetSetMethod()).GetParameters().Length == 1))
                        infos.Add(pi.Name, GetItemInfo(pi.PropertyType, pi.Name, set));
                }
                Dico = (((kType != null) && (vType != null)) ? GetItemInfo(Type, kType, vType, typeof(Dictionary<,>).MakeGenericType(kType, vType).GetMethod("Add", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)) : null);
                List = ((EType != null) ? GetItemInfo(EType, String.Empty, typeof(List<>).MakeGenericType(EType).GetMethod("Add", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)) : null);
                Enums = (IsEnum ? GetEnumInfos(Type) : null);
                Props = infos.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToArray();
            }
        }

        internal class TypeInfo<T> : TypeInfo
        {
            internal Func<JsonParser, int, T> Value;

            private Func<JsonParser, int, R> GetParseFunc<R>(string pName)
            {
                var parse = GetParserParse(pName ?? "Key");
                if (parse != null)
                {
                    var method = new System.Reflection.Emit.DynamicMethod(parse.Name, typeof(R), new Type[] { typeof(JsonParser), typeof(int) }, typeof(string), true);
                    var il = method.GetILGenerator();
                    il.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
                    il.Emit(System.Reflection.Emit.OpCodes.Ldarg_1);
                    il.Emit(System.Reflection.Emit.OpCodes.Callvirt, parse);
                    il.Emit(System.Reflection.Emit.OpCodes.Ret);
                    return (Func<JsonParser, int, R>)method.CreateDelegate(typeof(Func<JsonParser, int, R>));
                }
                return null;
            }

            internal TypeInfo(int self, Type eType, Type kType, Type vType)
                : base(typeof(T), self, eType, kType, vType)
            {
                var value = (Value = GetParseFunc<T>(GetParseName(typeof(T))));
                Parse = (Func<JsonParser, int, object>)((parser, outer) => value(parser, outer));
            }
        }

        static JsonParser()
        {
            for (char c = '0'; c <= '9'; c++) { HXD[c] = true; HEX[c] = (byte)(c - 48); }
            for (char c = 'A'; c <= 'F'; c++) { HXD[c] = HXD[c + 32] = true; HEX[c] = HEX[c + 32] = (byte)(c - 55); }
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
            int cp = 0, ic = -1, ch;
            if (ec == 'u')
            {
                while ((++ic < 4) && ((ch = Read()) <= 'f') && HXD[ch]) { cp *= 16; cp += HEX[ch]; }
                if (ic < 4) throw Error("Invalid Unicode character");
                ch = cp;
            }
            else
                ch = ESC[ec];
            Read();
            return ch;
        }

        private int CharEsc(int ec)
        {
            int cp = 0, ic = -1, ch;
            if (ec == 'u')
            {
                while ((++ic < 4) && ((ch = Read()) <= 'f') && HXD[ch]) { cp *= 16; cp += HEX[ch]; }
                if (ic < 4) throw Error("Invalid Unicode character");
                ch = cp;
            }
            else
                ch = ESC[ec];
            Char(ch);
            return ch;
        }

        private EnumInfo GetEnumInfo(TypeInfo type)
        {
            var a = type.Enums; int n = a.Length, c = 0, i = 0, ch;
            var e = false;
            EnumInfo ei;
            if (n > 0)
            {
                while (true)
                {
                    if ((ch = chr) == '"') { Read(); return (((i < n) && (c > 0)) ? a[i] : null); }
                    if (e = (ch == '\\')) ch = Read();
                    if (ch < EOF) { if (!e || (ch >= 128)) Read(); else { ch = Esc(ch); e = false; } } else break;
                    while ((i < n) && ((c >= (ei = a[i]).Len) || (ei.Name[c] != ch))) i++; c++;
                }
            }
            return null;
        }

        private bool ParseBoolean(int outer)
        {
            int ch = Space();
            bool k;
            if (k = ((outer > 0) && (ch == '"'))) ch = Read();
            switch (ch)
            {
                case 'f': Read(); Next('a'); Next('l'); Next('s'); Next('e'); if (k) Next('"'); return false;
                case 't': Read(); Next('r'); Next('u'); Next('e'); if (k) Next('"'); return true;
                default: throw Error("Bad boolean");
            }
        }

        private char ParseChar(int outer)
        {
            int ch = Space();
            if (ch == '"')
            {
                ch = Read();
                lln = 0;
                switch (ch) { case '\\': ch = Read(); CharEsc(ch); Next('"'); break; default: Char(ch); Next('"'); break; }
                return lbf[0];
            }
            throw Error("Bad character");
        }

        private byte ParseByte(int outer)
        {
            bool b = false, k;
            int ch = Space();
            byte n = 0;
            TypeInfo t;
            if (k = ((outer > 0) && (ch == '"')))
            {
                ch = Read();
                if ((t = types[outer]).IsEnum && ((ch < '0') || (ch > '9')))
                {
                    var e = GetEnumInfo(t);
                    if (e == null) throw Error(System.String.Format("Bad enum value ({0})", t.Type.FullName));
                    return (byte)e.Value;
                }
            }
            while ((ch >= '0') && (ch <= '9') && (b = true)) { checked { n *= 10; n += (byte)(ch - 48); } ch = Read(); }
            if (!b) throw Error("Bad number (byte)"); if (k) Next('"');
            return n;
        }

        private short ParseInt16(int outer)
        {
            short it = 1, n = 0;
            bool b = false, k;
            int ch = Space();
            TypeInfo t;
            if (k = ((outer > 0) && (ch == '"')))
            {
                ch = Read();
                if ((t = types[outer]).IsEnum && (ch != '-') && ((ch < '0') || (ch > '9')))
                {
                    var e = GetEnumInfo(t);
                    if (e == null) throw Error(System.String.Format("Bad enum value ({0})", t.Type.FullName));
                    return (short)e.Value;
                }
            }
            if (ch == '-') { ch = Read(); it = (short)-it; }
            while ((ch >= '0') && (ch <= '9') && (b = true)) { checked { n *= 10; n += (short)(ch - 48); } ch = Read(); }
            if (!b) throw Error("Bad number (short)"); if (k) Next('"');
            return (short)checked(it * n);
        }

        private int ParseInt32(int outer)
        {
            int it = 1, n = 0;
            bool b = false, k;
            int ch = Space();
            TypeInfo t;
            if (k = ((outer > 0) && (ch == '"')))
            {
                ch = Read();
                if ((t = types[outer]).IsEnum && (ch != '-') && ((ch < '0') || (ch > '9')))
                {
                    var e = GetEnumInfo(t);
                    if (e == null) throw Error(System.String.Format("Bad enum value ({0})", t.Type.FullName));
                    return (int)e.Value;
                }
            }
            if (ch == '-') { ch = Read(); it = -it; }
            while ((ch >= '0') && (ch <= '9') && (b = true)) { checked { n *= 10; n += (ch - 48); } ch = Read(); }
            if (!b) throw Error("Bad number (int)"); if (k) Next('"');
            return checked(it * n);
        }

        private long ParseInt64(int outer)
        {
            long it = 1, n = 0;
            bool b = false, k;
            int ch = Space();
            TypeInfo t;
            if (k = ((outer > 0) && (ch == '"')))
            {
                ch = Read();
                if ((t = types[outer]).IsEnum && (ch != '-') && ((ch < '0') || (ch > '9')))
                {
                    var e = GetEnumInfo(t);
                    if (e == null) throw Error(System.String.Format("Bad enum value ({0})", t.Type.FullName));
                    return e.Value;
                }
            }
            if (ch == '-') { ch = Read(); it = -it; }
            while ((ch >= '0') && (ch <= '9') && (b = true)) { checked { n *= 10; n += (ch - 48); } ch = Read(); }
            if (!b) throw Error("Bad number (long)"); if (k) Next('"');
            return checked(it * n);
        }

        private float ParseSingle(int outer)
        {
            bool b = false, k;
            int ch = Space();
            string s;
            lsb.Length = 0; lln = 0;
            if (k = ((outer > 0) && (ch == '"'))) ch = Read();
            if (ch == '-') ch = Char(ch);
            while ((ch >= '0') && (ch <= '9') && (b = true)) ch = Char(ch);
            if (ch == '.') { ch = Char(ch); while ((ch >= '0') && (ch <= '9')) ch = Char(ch); }
            if ((ch == 'e') || (ch == 'E')) { ch = Char(ch); if ((ch == '-') || (ch == '+')) ch = Char(ch); while ((ch >= '0') && (ch <= '9')) ch = Char(ch); }
            if (!b) throw Error("Bad number (float)"); if (k) Next('"');
            s = ((lsb.Length > 0) ? lsb.ToString() : new string(lbf, 0, lln));
            return float.Parse(s);
        }

        private double ParseDouble(int outer)
        {
            bool b = false, k;
            int ch = Space();
            string s;
            lsb.Length = 0; lln = 0;
            if (k = ((outer > 0) && (ch == '"'))) ch = Read();
            if (ch == '-') ch = Char(ch);
            while ((ch >= '0') && (ch <= '9') && (b = true)) ch = Char(ch);
            if (ch == '.') { ch = Char(ch); while ((ch >= '0') && (ch <= '9')) ch = Char(ch); }
            if ((ch == 'e') || (ch == 'E')) { ch = Char(ch); if ((ch == '-') || (ch == '+')) ch = Char(ch); while ((ch >= '0') && (ch <= '9')) ch = Char(ch); }
            if (!b) throw Error("Bad number (double)"); if (k) Next('"');
            s = ((lsb.Length > 0) ? lsb.ToString() : new string(lbf, 0, lln));
            return double.Parse(s);
        }

        private decimal ParseDecimal(int outer)
        {
            bool b = false, k;
            int ch = Space();
            string s;
            lsb.Length = 0; lln = 0;
            if (k = ((outer > 0) && (ch == '"'))) ch = Read();
            if (ch == '-') ch = Char(ch);
            while ((ch >= '0') && (ch <= '9') && (b = true)) ch = Char(ch);
            if (ch == '.') { ch = Char(ch); while ((ch >= '0') && (ch <= '9')) ch = Char(ch); }
            if (!b) throw Error("Bad number (decimal)"); if (k) Next('"');
            s = ((lsb.Length > 0) ? lsb.ToString() : new string(lbf, 0, lln));
            return decimal.Parse(s);
        }

        private void PastKey()
        {
            var ch = Space();
            var e = false;
            if (ch == '"')
            {
                Read();
                while (true)
                {
                    if ((ch = chr) == '"') { Read(); return; }
                    if (e = (ch == '\\')) ch = Read();
                    if (ch < EOF) { if (!e || (ch >= 128)) Read(); else { Esc(ch); e = false; } } else break;
                }
            }
            throw Error("Bad key");
        }

        private string ParseString(int outer)
        {
            var ch = Space();
            var e = false;
            if (ch == '"')
            {
                Read();
                lsb.Length = 0; lln = 0;
                while (true)
                {
                    if ((ch = chr) == '"') { Read(); return ((lsb.Length > 0) ? lsb.ToString() : new string(lbf, 0, lln)); }
                    if (e = (ch == '\\')) ch = Read();
                    if (ch < EOF) { if (!e || (ch >= 128)) Char(ch); else { CharEsc(ch); e = false; } } else break;
                }
            }
            if (ch == 'n')
                return (string)Null(0);
            throw Error((outer >= 0) ? "Bad string" : "Bad key");
        }

        private DateTimeOffset ParseDateTimeOffset(int outer)
        {
            DateTimeOffset dateTimeOffset;
            if (!DateTimeOffset.TryParse(ParseString(0), System.Globalization.CultureInfo.CurrentCulture, System.Globalization.DateTimeStyles.RoundtripKind, out dateTimeOffset))
                throw Error("Bad date/time offset");
            return dateTimeOffset;
        }

        private DateTime ParseDateTime(int outer)
        {
            DateTime dateTime;
            if (!DateTime.TryParse(ParseString(0), System.Globalization.CultureInfo.CurrentCulture, System.Globalization.DateTimeStyles.RoundtripKind, out dateTime))
                throw Error("Bad date/time");
            return dateTime;
        }

        private ItemInfo GetPropInfo(TypeInfo type)
        {
            var a = type.Props; int ch = Space(), n = a.Length, c = 0, i = 0;
            var e = false;
            ItemInfo pi;
            if (ch == '"')
            {
                Read();
                while (true)
                {
                    if ((ch = chr) == '"') { Read(); return (((i < n) && (c > 0)) ? a[i] : null); }
                    if (e = (ch == '\\')) ch = Read();
                    if (ch < EOF) { if (!e || (ch >= 128)) Read(); else { ch = Esc(ch); e = false; } } else break;
                    while ((i < n) && ((c >= (pi = a[i]).Len) || (pi.Name[c] != ch))) i++; c++;
                }
            }
            throw Error("Bad key");
        }

        private object Error(int outer) { throw Error("Bad value"); }
        private object Null(int outer) { Read(); Next('u'); Next('l'); Next('l'); return null; }
        private object False(int outer) { Read(); Next('a'); Next('l'); Next('s'); Next('e'); return false; }
        private object True(int outer) { Read(); Next('r'); Next('u'); Next('e'); return true; }

        private object Num(int outer)
        {
            var ch = chr;
            var b = false;
            lsb.Length = 0; lln = 0;
            if (ch == '-') ch = Char(ch);
            while ((ch >= '0') && (ch <= '9') && (b = true)) ch = Char(ch);
            if (ch == '.') { ch = Char(ch); while ((ch >= '0') && (ch <= '9')) ch = Char(ch); }
            if ((ch == 'e') || (ch == 'E')) { ch = Char(ch); if ((ch == '-') || (ch == '+')) ch = Char(ch); while ((ch >= '0') && (ch <= '9')) ch = Char(ch); }
            if (!b) throw Error("Bad number");
            return ((lsb.Length > 0) ? lsb.ToString() : new string(lbf, 0, lln));
        }

        private object Key(int outer) { return ParseString(-1); }

        private object Str(int outer) { var s = ParseString(0); if ((outer != 2) || ((s != null) && (s.Length == 1))) return ((outer == 2) ? (object)s[0] : s); else throw Error("Bad character"); }

        private object Obj(int outer)
        {
            var cached = types[outer]; var hash = types[cached.Key]; var select = cached.Select; var ctor = cached.Ctor;
            var mapper = (null as Func<object, object>);
            var typed = ((outer > 0) && (cached.Dico == null) && (ctor != null));
            var parse = hash.Parse;
            var keyed = hash.T;
            var ch = chr;
            if (ch == '{')
            {
                object obj;
                Read();
                ch = Space();
                if (ch == '}')
                {
                    Read();
                    return ctor();
                }
                obj = null;
                while (ch < EOF)
                {
                    var prop = (typed ? GetPropInfo(cached) : null);
                    var slot = (!typed ? parse(this, keyed) : null);
                    Func<object, object> read = null;
                    Space();
                    Next(':');
                    if (slot != null)
                    {
                        if ((select == null) || ((read = select(cached.Type, obj, slot, -1)) != null))
                        {
                            var val = Val(cached.Inner);
                            var key = (slot as string);
                            if (obj == null)
                            {
                                if ((key != null) && ((String.Compare(key, TypeTag1) == 0) || (String.Compare(key, TypeTag2) == 0)))
                                {
                                    obj = (((key = (val as string)) != null) ? (cached = types[Entry(Type.GetType(key, true), null)]).Ctor() : ctor());
                                    typed = !(obj is IDictionary);
                                }
                                else
                                    obj = ctor();
                            }
                            if (!typed)
                                ((IDictionary)obj).Add(slot, val);
                        }
                        else
                            Val(0);
                    }
                    else if (prop != null)
                    {
                        if ((select == null) || ((read = select(cached.Type, obj, prop.Name, -1)) != null))
                        {
                            obj = (obj ?? ctor());
                            prop.Set(obj, this, prop.Outer, 0);
                        }
                        else
                            Val(0);
                    }
                    else
                        Val(0);
                    mapper = (mapper ?? read);
                    ch = Space();
                    if (ch == '}')
                    {
                        mapper = (mapper ?? Identity);
                        Read();
                        return mapper(obj ?? ctor());
                    }
                    Next(',');
                    ch = Space();
                }
            }
            throw Error("Bad object");
        }

        private void SBrace() { Next('{'); }
        private void EBrace() { Space(); Next('}'); }
        private void KColon() { PastKey(); Space(); Next(':'); }
        private void SComma() { Space(); Next(','); }

        private object Arr(int outer)
        {
            var cached = types[(outer != 0) ? outer : 1]; var select = cached.Select; var dico = (cached.Dico != null);
            var mapper = (null as Func<object, object>);
            var items = (dico ? cached.Dico : cached.List);
            var val = cached.Inner;
            var key = cached.Key;
            var ch = chr;
            var i = -1;
            if (ch == '[')
            {
                object obj;
                Read();
                ch = Space();
                obj = cached.Ctor();
                if (ch == ']')
                {
                    Read();
                    if (cached.Type.IsArray)
                    {
                        IList list = (IList)obj;
                        var array = Array.CreateInstance(cached.EType, list.Count);
                        list.CopyTo(array, 0);
                        return array;
                    }
                    return obj;
                }
                while (ch < EOF)
                {
                    Func<object, object> read = null;
                    i++;
                    if (dico || (select == null) || ((read = select(cached.Type, obj, null, i)) != null))
                        items.Set(obj, this, val, key);
                    else
                        Val(0);
                    mapper = (mapper ?? read);
                    ch = Space();
                    if (ch == ']')
                    {
                        mapper = (mapper ?? Identity);
                        Read();
                        if (cached.Type.IsArray)
                        {
                            IList list = (IList)obj;
                            var array = Array.CreateInstance(cached.EType, list.Count);
                            list.CopyTo(array, 0);
                            return mapper(array);
                        }
                        return mapper(obj);
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
            else if ((type != typeof(string)) && typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
                return (type.IsGenericType ? type.GetGenericArguments()[0] : typeof(object));
            else
                return null;
        }

        private Type Realizes(Type type, Type generic)
        {
            var itfs = type.GetInterfaces();
            foreach (var it in itfs)
                if (it.IsGenericType && it.GetGenericTypeDefinition() == generic)
                    return type;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == generic)
                return type;
            if (type.BaseType == null)
                return null;
            return Realizes(type.BaseType, generic);
        }

        private bool GetKeyValueTypes(Type type, out Type key, out Type value)
        {
            var generic = (Realizes(type, typeof(Dictionary<,>)) ?? Realizes(type, typeof(IDictionary<,>)));
            var kvPair = ((generic != null) && (generic.GetGenericArguments().Length == 2));
            value = (kvPair ? generic.GetGenericArguments()[1] : null);
            key = (kvPair ? generic.GetGenericArguments()[0] : null);
            return kvPair;
        }

        private int Closure(int outer, IDictionary<Type, Func<Type, object, object, int, Func<object, object>>> filter)
        {
            var prop = types[outer].Props;
            for (var i = 0; i < prop.Length; i++) prop[i].Outer = Entry(prop[i].Type, filter);
            return outer;
        }

        private int Entry(Type type, IDictionary<Type, Func<Type, object, object, int, Func<object, object>>> filter)
        {
            Func<Type, object, object, int, Func<object, object>> select = null;
            int outer;
            if (!rtti.TryGetValue(type, out outer))
            {
                Type et, kt, vt;
                bool dico = GetKeyValueTypes(type, out kt, out vt);
                et = (!dico ? GetElementType(type) : null);
                outer = rtti.Count;
                types[outer] = (TypeInfo)Activator.CreateInstance(typeof(TypeInfo<>).MakeGenericType(type), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null, new object[] { outer, et, kt, vt }, null);
                rtti.Add(type, outer);
                types[outer].Inner = ((et != null) ? Entry(et, filter) : (dico ? Entry(vt, filter) : 0));
                if (dico) types[outer].Key = Entry(kt, filter);
            }
            if ((filter != null) && filter.TryGetValue(type, out select))
                types[outer].Select = select;
            return Closure(outer, filter);
        }

        private T DoParse<T>(string input, IDictionary<Type, Func<Type, object, object, int, Func<object, object>>> filter)
        {
            var outer = Entry(typeof(T), filter);
            len = input.Length;
            txt = input;
            Reset(StringRead, StringNext, StringChar, StringSpace);
            return (typeof(T).IsValueType ? ((TypeInfo<T>)types[outer]).Value(this, outer) : (T)Val(outer));
        }

        private T DoParse<T>(TextReader input, IDictionary<Type, Func<Type, object, object, int, Func<object, object>>> filter)
        {
            var outer = Entry(typeof(T), filter);
            str = input;
            Reset(StreamRead, StreamNext, StreamChar, StreamSpace);
            return (typeof(T).IsValueType ? ((TypeInfo<T>)types[outer]).Value(this, outer) : (T)Val(outer));
        }

        public static object Identity(object obj) { return obj; }

        public static readonly Func<object, object> Skip = null;

        public static JsonPathSelection ToJsonPath(object data) { return ToJsonPath(data, null); }

        public static JsonPathSelection ToJsonPath(object data, JsonPathScriptEvaluator eval)
        {
            return new JsonPathSelection(new JsonPathContext { ValueSystem = new JsonParserValueSystem(), ScriptEvaluator = eval }, data);
        }

        public JsonParser()
        {
            parse['n'] = Null; parse['f'] = False; parse['t'] = True;
            parse['0'] = parse['1'] = parse['2'] = parse['3'] = parse['4'] =
            parse['5'] = parse['6'] = parse['7'] = parse['8'] = parse['9'] =
            parse['-'] = Num; parse['"'] = Str; parse['{'] = Obj; parse['['] = Arr;
            for (var input = 0; input < 128; input++) parse[input] = (parse[input] ?? Error);
            Entry(typeof(object), null);
            Entry(typeof(List<object>), null);
            Entry(typeof(char), null);
        }

        public object Parse(string input) { return Parse<object>(input); }

        public object Parse(string input, IDictionary<Type, Func<Type, object, object, int, Func<object, object>>> mappers) { return Parse<object>(input, mappers); }

        public object Parse(TextReader input) { return Parse<object>(input); }

        public object Parse(TextReader input, IDictionary<Type, Func<Type, object, object, int, Func<object, object>>> mappers) { return Parse<object>(input, mappers); }

        public object Parse(Stream input) { return Parse<object>(input); }

        public object Parse(Stream input, Encoding encoding) { return Parse<object>(input, encoding); }

        public object Parse(Stream input, IDictionary<Type, Func<Type, object, object, int, Func<object, object>>> mappers) { return Parse<object>(input, mappers); }

        public object Parse(Stream input, Encoding encoding, IDictionary<Type, Func<Type, object, object, int, Func<object, object>>> mappers) { return Parse<object>(input, encoding, mappers); }

        public T Parse<T>(string input) { return Parse<T>(input, null); }

        public T Parse<T>(string input, IDictionary<Type, Func<Type, object, object, int, Func<object, object>>> mappers)
        {
            if (input == null) throw new ArgumentNullException("input", "cannot be null");
            return DoParse<T>(input, mappers);
        }

        public T Parse<T>(TextReader input) { return Parse<T>(input, null); }

        public T Parse<T>(TextReader input, IDictionary<Type, Func<Type, object, object, int, Func<object, object>>> mappers)
        {
            if (input == null) throw new ArgumentNullException("input", "cannot be null");
            return DoParse<T>(input, mappers);
        }

        public T Parse<T>(Stream input) { return Parse<T>(input, null as Encoding); }

        public T Parse<T>(Stream input, Encoding encoding) { return Parse<T>(input, encoding, null); }

        public T Parse<T>(Stream input, IDictionary<Type, Func<Type, object, object, int, Func<object, object>>> mappers) { return Parse<T>(input, null, mappers); }

        public T Parse<T>(Stream input, Encoding encoding, IDictionary<Type, Func<Type, object, object, int, Func<object, object>>> mappers)
        {
            if (input == null) throw new ArgumentNullException("input", "cannot be null");
            return DoParse<T>((encoding != null) ? new StreamReader(input, encoding) : new StreamReader(input), mappers);
        }
    }
}

//
// C# implementation of JSONPath[1]
//
// Copyright (c) 2007 Atif Aziz (http://www.raboof.com/)
// Licensed under The MIT License
//
// Supported targets:
//
//  - Mono 1.1 or later
//  - Microsoft .NET Framework 1.0 or later
//
// [1]  JSONPath - XPath for JSON
//      http://code.google.com/p/jsonpath/
//      Copyright (c) 2007 Stefan Goessner (goessner.net)
//      Licensed under The MIT License
//

#region The MIT License
//
// The MIT License
//
// Copyright (c) 2007 Atif Aziz (http://www.raboof.com/)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
#endregion

namespace JsonPath
{
    #region Imports

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;
    using System.Text.RegularExpressions;

    #endregion

    public delegate object JsonPathScriptEvaluator(string script, object value, string context);
    public delegate void JsonPathResultAccumulator(object value, string[] indicies);

    public interface IJsonPathValueSystem
    {
        bool HasMember(object value, string member);
        object GetMemberValue(object value, string member);
        IEnumerable GetMembers(object value);
        bool IsObject(object value);
        bool IsArray(object value);
        bool IsPrimitive(object value);
    }

    [Serializable]
    public sealed class JsonPathNode
    {
        private readonly object value;
        private readonly string path;

        public JsonPathNode(object value, string path)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            if (path.Length == 0)
                throw new ArgumentException("path");

            this.value = value;
            this.path = path;
        }

        public object Value
        {
            get { return value; }
        }

        public string Path
        {
            get { return path; }
        }

        public override string ToString()
        {
            return Path + " = " + Value;
        }

        public static object[] ValuesFrom(ICollection nodes)
        {
            object[] values = new object[nodes != null ? nodes.Count : 0];

            if (values.Length > 0)
            {
                Debug.Assert(nodes != null);

                int i = 0;
                foreach (JsonPathNode node in nodes)
                    values[i++] = node.Value;
            }

            return values;
        }

        public static string[] PathsFrom(ICollection nodes)
        {
            string[] paths = new string[nodes != null ? nodes.Count : 0];

            if (paths.Length > 0)
            {
                Debug.Assert(nodes != null);

                int i = 0;
                foreach (JsonPathNode node in nodes)
                    paths[i++] = node.Path;
            }

            return paths;
        }
    }

    public sealed class JsonPathContext
    {
        public static readonly JsonPathContext Default = new JsonPathContext();
        public readonly IDictionary<string, JsonPathScriptEvaluator> Lambdas = new Dictionary<string, JsonPathScriptEvaluator>();

        private JsonPathScriptEvaluator eval;
        private IJsonPathValueSystem system;

        public JsonPathScriptEvaluator ScriptEvaluator
        {
            get { return eval; }
            set { eval = value; }
        }

        public IJsonPathValueSystem ValueSystem
        {
            get { return system; }
            set { system = value; }
        }

        public void SelectTo(object obj, string expr, JsonPathResultAccumulator output, params JsonPathScriptEvaluator[] lambdas)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");

            if (output == null)
                throw new ArgumentNullException("output");

            Interpreter i = new Interpreter(this, output, ValueSystem, ScriptEvaluator);

            expr = Normalize(expr);

            if (expr.Length >= 1 && expr[0] == '$') // ^\$:?
                expr = expr.Substring(expr.Length >= 2 && expr[1] == ';' ? 2 : 1);

            i.Trace(expr, obj, "$", lambdas, -1);
        }

        public JsonPathNode[] SelectNodes(object obj, string expr, params JsonPathScriptEvaluator[] lambdas)
        {
            ArrayList list = new ArrayList();
            SelectNodesTo(obj, expr, list, lambdas);
            return (JsonPathNode[])list.ToArray(typeof(JsonPathNode));
        }

        public IList SelectNodesTo(object obj, string expr, IList output, params JsonPathScriptEvaluator[] lambdas)
        {
            ListAccumulator accumulator = new ListAccumulator(output != null ? output : new ArrayList());
            SelectTo(obj, expr, new JsonPathResultAccumulator(accumulator.Put), lambdas);
            return output;
        }

        private static Regex RegExp(string pattern)
        {
            return new Regex(pattern, RegexOptions.ECMAScript);
        }

        private static string Normalize(string expr)
        {
            NormalizationSwap swap = new NormalizationSwap();
            expr = RegExp(@"[\['](\??\(.*?\))[\]']").Replace(expr, new MatchEvaluator(swap.Capture));
            expr = RegExp(@"'?\.'?|\['?").Replace(expr, ";");
            expr = RegExp(@";;;|;;").Replace(expr, ";..;");
            expr = RegExp(@";$|'?\]|'$").Replace(expr, string.Empty);
            expr = RegExp(@"#([0-9]+)").Replace(expr, new MatchEvaluator(swap.Yield));
            return expr;
        }

        private sealed class NormalizationSwap
        {
            private readonly ArrayList subx = new ArrayList(4);

            public string Capture(Match match)
            {
                Debug.Assert(match != null);

                int index = subx.Add(match.Groups[1].Value);
                return "[#" + index.ToString(CultureInfo.InvariantCulture) + "]";
            }

            public string Yield(Match match)
            {
                Debug.Assert(match != null);

                int index = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                return (string)subx[index];
            }
        }

        public static string AsBracketNotation(string[] indicies)
        {
            if (indicies == null)
                throw new ArgumentNullException("indicies");

            StringBuilder sb = new StringBuilder();

            foreach (string index in indicies)
            {
                if (sb.Length == 0)
                {
                    sb.Append('$');
                }
                else
                {
                    sb.Append('[');
                    if (RegExp(@"^[0-9*]+$").IsMatch(index))
                        sb.Append(index);
                    else
                        sb.Append('\'').Append(index).Append('\'');
                    sb.Append(']');
                }
            }

            return sb.ToString();
        }

        private static int ParseInt(string s)
        {
            return ParseInt(s, 0);
        }

        private static int ParseInt(string str, int defaultValue)
        {
            if (str == null || str.Length == 0)
                return defaultValue;

            try
            {
                return int.Parse(str, NumberStyles.None, CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                return defaultValue;
            }
        }

        private sealed class Interpreter
        {
            public readonly JsonPathContext Bindings;
            private readonly JsonPathResultAccumulator output;
            private readonly JsonPathScriptEvaluator eval;
            private readonly IJsonPathValueSystem system;

            private static readonly IJsonPathValueSystem defaultValueSystem = new BasicValueSystem();

            private static readonly char[] colon = new char[] { ':' };
            private static readonly char[] semicolon = new char[] { ';' };

            private delegate void WalkCallback(object member, string loc, string expr, object value, string path, JsonPathScriptEvaluator[] lambdas, int clambda);

            public Interpreter(JsonPathContext bindings, JsonPathResultAccumulator output, IJsonPathValueSystem valueSystem, JsonPathScriptEvaluator eval)
            {
                Debug.Assert(output != null);
                Bindings = bindings;
                this.output = output;
                this.eval = eval != null ? eval : new JsonPathScriptEvaluator(NullEval);
                this.system = valueSystem != null ? valueSystem : defaultValueSystem;
            }

            public void Trace(string expr, object value, string path, JsonPathScriptEvaluator[] lambdas, int clambda)
            {
                if (expr == null || expr.Length == 0)
                {
                    Store(path, value);
                    return;
                }

                int i = expr.IndexOf(';');
                string atom = i >= 0 ? expr.Substring(0, i) : expr;
                string tail = i >= 0 ? expr.Substring(i + 1) : string.Empty;

                if (value != null && system.HasMember(value, atom))
                {
                    Trace(tail, Index(value, atom), path + ";" + atom, lambdas, -1);
                }
                else if (atom == "*")
                {
                    Walk(atom, tail, value, path, new WalkCallback(WalkWild), lambdas, -1);
                }
                else if (atom == "..")
                {
                    Trace(tail, value, path, lambdas, -1);
                    Walk(atom, tail, value, path, new WalkCallback(WalkTree), lambdas, -1);
                }
                else if (atom.Length > 2 && atom[0] == '(' && atom[atom.Length - 1] == ')') // [(exp)]
                {
                    Trace(Eval(atom, value, path.Substring(path.LastIndexOf(';') + 1)) + ";" + tail, value, path, lambdas, -1);
                }
                else if (atom.Length > 2 && atom[0] == '{' && atom[atom.Length - 1] == '}') // [{N}]
                {
                    var lambda = lambdas[int.Parse(atom.Substring(1, atom.Length - 2))];
                    Trace(lambda(atom, value, path.Substring(path.LastIndexOf(';') + 1)) + ";" + tail, value, path, lambdas, -1);
                }
                else if (atom.Length > 3 && atom[0] == '?' && atom[1] == '(' && atom[atom.Length - 1] == ')') // [?(exp)]
                {
                    Walk(atom, tail, value, path, new WalkCallback(WalkFiltered), lambdas, -1);
                }
                else if (atom.Length > 3 && atom[0] == '?' && atom[1] == '{' && atom[atom.Length - 1] == '}') // [?{N}]
                {
                    Walk(atom, tail, value, path, new WalkCallback(WalkFiltered), lambdas, int.Parse(atom.Substring(2, atom.Length - 3)));
                }
                else if (RegExp(@"^(-?[0-9]*):(-?[0-9]*):?([0-9]*)$").IsMatch(atom)) // [start:end:step] Phyton slice syntax
                {
                    Slice(atom, tail, value, path, lambdas, -1);
                }
                else if (atom.IndexOf(',') >= 0) // [name1,name2,...]
                {
                    foreach (string part in RegExp(@"'?,'?").Split(atom))
                        Trace(part + ";" + tail, value, path, lambdas, -1);
                }
            }

            private void Store(string path, object value)
            {
                if (path != null)
                    output(value, path.Split(semicolon));
            }

            private void Walk(string loc, string expr, object value, string path, WalkCallback callback, JsonPathScriptEvaluator[] lambdas, int clambda)
            {
                if (system.IsPrimitive(value))
                    return;

                if (system.IsArray(value))
                {
                    IList list = (IList)value;
                    for (int i = 0; i < list.Count; i++)
                        callback(i, loc, expr, value, path, lambdas, clambda);
                }
                else if (system.IsObject(value))
                {
                    foreach (string key in system.GetMembers(value))
                        callback(key, loc, expr, value, path, lambdas, clambda);
                }
            }

            private void WalkWild(object member, string loc, string expr, object value, string path, JsonPathScriptEvaluator[] lambdas, int clambda)
            {
                Trace(member + ";" + expr, value, path, lambdas, -1);
            }

            private void WalkTree(object member, string loc, string expr, object value, string path, JsonPathScriptEvaluator[] lambdas, int clambda)
            {
                object result = Index(value, member.ToString());
                if (result != null && !system.IsPrimitive(result))
                    Trace("..;" + expr, result, path + ";" + member, lambdas, -1);
            }

            private void WalkFiltered(object member, string loc, string expr, object value, string path, JsonPathScriptEvaluator[] lambdas, int clambda)
            {
                string script = RegExp(@"^\?\((.*?)\)$").Replace(loc, "$1");
                string context = member.ToString();
                object result = ((clambda < 0) ? Eval(script, value, context, true) : lambdas[clambda](script, value, context));
                if ((result != null) && Convert.ToBoolean(result.ToString(), CultureInfo.InvariantCulture))
                    Trace(member + ";" + expr, value, path, lambdas, -1);
            }

            private void Slice(string loc, string expr, object value, string path, JsonPathScriptEvaluator[] lambdas, int clambda)
            {
                IList list = value as IList;

                if (list == null)
                    return;

                int length = list.Count;
                string[] parts = loc.Split(colon);
                int start = ParseInt(parts[0]);
                int end = ParseInt(parts[1], list.Count);
                int step = parts.Length > 2 ? ParseInt(parts[2], 1) : 1;
                start = (start < 0) ? Math.Max(0, start + length) : Math.Min(length, start);
                end = (end < 0) ? Math.Max(0, end + length) : Math.Min(length, end);
                for (int i = start; i < end; i += step)
                    Trace(i + ";" + expr, value, path, lambdas, -1);
            }

            private object Index(object obj, string member)
            {
                return system.GetMemberValue(obj, member);
            }

            private object Eval(string script, object value, string context) { return Eval(script, value, context, false); }

            private object Eval(string script, object value, string context, bool forFilter)
            {
                object target = (forFilter ? Index(value, context) : value);
                Type type = ((target != null) ? target.GetType() : typeof(void));
                JsonPathScriptEvaluator func;
                if (!Bindings.Lambdas.TryGetValue(script, out func))
                {
                    string lambda = String.Format("(string script, object value, string context) => (object)({0})", script.Replace("@", "value"));
                    func = (eval(lambda, type, lambda) as JsonPathScriptEvaluator);
                    if (func != null) Bindings.Lambdas.Add(script, func);
                }
                return ((func != null) ? func(script, target, context) : eval(script, target, context));
            }

            private static object NullEval(string expr, object value, string context)
            {
                //
                // @ symbol in expr must be interpreted specially to resolve
                // to value. In JavaScript, the implementation would look 
                // like:
                //
                // return obj && value && eval(expr.replace(/@/g, "value"));
                //

                return null;
            }
        }

        private sealed class BasicValueSystem : IJsonPathValueSystem
        {
            public bool HasMember(object value, string member)
            {
                if (IsPrimitive(value))
                    return false;

                IDictionary dict = value as IDictionary;
                if (dict != null)
                    return dict.Contains(member);

                IList list = value as IList;
                if (list != null)
                {
                    int index = ParseInt(member, -1);
                    return index >= 0 && index < list.Count;
                }

                return false;
            }

            public object GetMemberValue(object value, string member)
            {
                if (IsPrimitive(value))
                    throw new ArgumentException("value");

                IDictionary dict = value as IDictionary;
                if (dict != null)
                    return dict[member];

                IList list = (IList)value;
                int index = ParseInt(member, -1);
                if (index >= 0 && index < list.Count)
                    return list[index];

                return null;
            }

            public IEnumerable GetMembers(object value)
            {
                return ((IDictionary)value).Keys;
            }

            public bool IsObject(object value)
            {
                return value is IDictionary;
            }

            public bool IsArray(object value)
            {
                return value is IList;
            }

            public bool IsPrimitive(object value)
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                return Type.GetTypeCode(value.GetType()) != TypeCode.Object;
            }
        }

        private sealed class ListAccumulator
        {
            private readonly IList list;

            public ListAccumulator(IList list)
            {
                Debug.Assert(list != null);

                this.list = list;
            }

            public void Put(object value, string[] indicies)
            {
                list.Add(new JsonPathNode(value, JsonPathContext.AsBracketNotation(indicies)));
            }
        }
    }
}
