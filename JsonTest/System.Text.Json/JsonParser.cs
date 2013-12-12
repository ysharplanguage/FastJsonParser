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
		private const int OBJECT = 0;
		private const int ARRAY = 1;
		private const int STRING = 2;

		private IDictionary<Type, int> thash = new Dictionary<Type, int>();
		private TypeInfo[] types = new TypeInfo[TDS];

		private Func<int, object>[] parse = new Func<int, object>[128];
		private StringBuilder lsb = new StringBuilder();
		private char[] lbf = new char[LBS];
		private char[] stc = new char[1];
		private System.IO.StreamReader str;
		private Func<int, int> Char;
		private Action<int> Next;
		private Func<int> Read;
		private Func<int> Space;
		private char[] txt;
		private int len;
		private int lln;
		private int chr;
		private int at;

		internal class PropInfo
		{
			internal Action<JsonParser, int, object> Set;
			internal Type Type;
			internal int Outer;
			internal string Name;
		}

		internal class TypeInfo
		{
			private static readonly HashSet<Type> WellKnown = new HashSet<Type>();

			internal Func<string, object> Convert;
			internal Func<object> Ctor;
			internal PropInfo[] Props;
			internal PropInfo Items;
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
			}

			private object DefaultConvert(string s)
			{
				return System.Convert.ChangeType(s, Type);
			}

			private Func<string, object> GetConvert(Type clr, int outer)
			{
				if (outer > STRING)
				{
					var type = typeof(Func<,>).MakeGenericType(typeof(string), clr);
					var conv = clr.GetMethod("Parse", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public, null, new Type[] { typeof(string) }, null);
					if (clr.IsValueType && (conv != null))
					{
						var dyn = new System.Reflection.Emit.DynamicMethod("", typeof(object), new Type[] { typeof(string) }, type, true);
						var il = dyn.GetILGenerator();
						il.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
						il.Emit(System.Reflection.Emit.OpCodes.Call, conv);
						il.Emit(System.Reflection.Emit.OpCodes.Box, clr);
						il.Emit(System.Reflection.Emit.OpCodes.Ret);
						return (Func<string, object>)dyn.CreateDelegate(typeof(Func<string, object>));
					}
					return DefaultConvert;
				}
				return null;
			}

			private Func<object> GetCtor(Type clr, int outer, bool list)
			{
				var type = (list ? typeof(List<>).MakeGenericType(clr) : clr);
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

			private static System.Reflection.MethodInfo GetParseMethod(Type type)
			{
				var val = typeof(JsonParser).GetMethod("Val", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
				if (type.IsEnum)
					return typeof(JsonParser).GetMethod("Enum", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).MakeGenericMethod(type);
				else if (WellKnown.Contains(type))
					return typeof(JsonParser).GetMethod(type.Name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
				else
					return val;
			}

			private static PropInfo GetPropInfo(Type type, string name, System.Reflection.MethodInfo smethod, System.Reflection.MethodInfo pmethod)
			{
				var dyn = new System.Reflection.Emit.DynamicMethod("", null, new Type[] { typeof(JsonParser), typeof(int), typeof(object) }, typeof(PropInfo));
				var il = dyn.GetILGenerator();
				il.DeclareLocal(pmethod.ReturnType);
				il.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
				il.Emit(System.Reflection.Emit.OpCodes.Ldarg_1);
				il.Emit(System.Reflection.Emit.OpCodes.Callvirt, pmethod);
				if (type.IsValueType && (pmethod.ReturnType == typeof(object)))
					il.Emit(System.Reflection.Emit.OpCodes.Unbox_Any, type);
				il.Emit(System.Reflection.Emit.OpCodes.Stloc_0);
				il.Emit(System.Reflection.Emit.OpCodes.Ldarg_2);
				il.Emit(System.Reflection.Emit.OpCodes.Ldloc_0);
				il.Emit(System.Reflection.Emit.OpCodes.Callvirt, smethod);
				il.Emit(System.Reflection.Emit.OpCodes.Ret);
				return new PropInfo { Type = type, Name = name, Set = (Action<JsonParser, int, object>)dyn.CreateDelegate(typeof(Action<JsonParser, int, object>)) };
			}

			internal TypeInfo(Type type, int outer, Type elem)
			{
				var infos = type.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
				var props = new SortedList<string, PropInfo>();
				Type = type;
				Outer = outer;
				ElementType = elem;
				Convert = GetConvert(Type, outer);
				Ctor = GetCtor((ElementType ?? Type), outer, (ElementType != null));
				for (var i = 0; i < infos.Length; i++)
					if (infos[i].CanWrite)
						props.Add(infos[i].Name, GetPropInfo(infos[i].PropertyType, infos[i].Name, infos[i].GetSetMethod(), GetParseMethod(infos[i].PropertyType)));
				Props = props.Values.ToArray();
				Items = ((ElementType != null) ? GetPropInfo(ElementType, "", typeof(List<>).MakeGenericType(ElementType).GetMethod("Add", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public), GetParseMethod(ElementType)) : null);
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

		private Exception Error(string message) { return new Exception(String.Format("{0} at {1} (found: '{2}')", message, at, ((chr < EOF) ? ("\\" + chr) : "EOF"))); }
		private void Reset(Func<int> read, Action<int> next, Func<int, int> achar, Func<int> space) { at = -1; chr = ANY; Read = read; Next = next; Char = achar; Space = space; }

		private object Error(int outer) { throw Error("Bad value"); }
		private object Null(int outer) { Next('n'); Next('u'); Next('l'); Next('l'); return null; }
		private bool F(int outer) { Next('f'); Next('a'); Next('l'); Next('s'); Next('e'); return false; }
		private bool T(int outer) { Next('t'); Next('r'); Next('u'); Next('e'); return true; }
		private object False(int outer) { return F(0); }
		private object True(int outer) { return T(0); }

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
				case 'f': return F(0);
				case 't': return T(0);
				default: throw Error("Bad boolean");
			}
		}

		private byte Byte(int outer)
		{
			var ch = Space();
			byte n = 0;
			while ((ch >= '0') && (ch <= '9')) { n *= 10; n += (byte)(ch - 48); ch = Read(); }
			return n;
		}

		private short Int16(int outer)
		{
			short it = 1, n = 0;
			var ch = Space();
			if (ch == '-') { ch = Read(); it = (short)-it; }
			while ((ch >= '0') && (ch <= '9')) { n *= 10; n += (short)(ch - 48); ch = Read(); }
			return (short)(it * n);
		}

		private int Int32(int outer)
		{
			int it = 1, n = 0;
			var ch = Space();
			if (ch == '-') { ch = Read(); it = -it; }
			while ((ch >= '0') && (ch <= '9')) { n *= 10; n += (ch - 48); ch = Read(); }
			return (it * n);
		}

		private long Int64(int outer)
		{
			long it = 1, n = 0;
			var ch = Space();
			if (ch == '-') { ch = Read(); it = -it; }
			while ((ch >= '0') && (ch <= '9')) { n *= 10; n += (ch - 48); ch = Read(); }
			return (it * n);
		}

		private float Single(int outer)
		{
			var ch = Space();
			string s;
			lsb.Length = 0; lln = 0;
			if (ch == '-') ch = Char(ch);
			while ((ch >= '0') && (ch <= '9')) ch = Char(ch);
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
			s = ((lsb.Length > 0) ? lsb.ToString() : new string(lbf, 0, lln));
			return float.Parse(s);
		}

		private double Double(int outer)
		{
			var ch = Space();
			string s;
			lsb.Length = 0; lln = 0;
			if (ch == '-') ch = Char(ch);
			while ((ch >= '0') && (ch <= '9')) ch = Char(ch);
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
			s = ((lsb.Length > 0) ? lsb.ToString() : new string(lbf, 0, lln));
			return double.Parse(s);
		}

		private decimal Decimal(int outer)
		{
			var ch = Space();
			string s;
			lsb.Length = 0; lln = 0;
			if (ch == '-') ch = Char(ch);
			while ((ch >= '0') && (ch <= '9')) ch = Char(ch);
			if (ch == '.')
			{
				ch = Char(ch);
				while ((ch >= '0') && (ch <= '9')) ch = Char(ch);
			}
			s = ((lsb.Length > 0) ? lsb.ToString() : new string(lbf, 0, lln));
			return decimal.Parse(s);
		}

		private object Num(int outer)
		{
			long it = 1, n = 0;
			var ch = chr;
			string s;
			lsb.Length = 0; lln = 0;
			if (ch == '-') { ch = Char(ch); it = -it; }
			while ((ch >= '0') && (ch <= '9')) { n *= 10; n += (ch - 48); ch = Char(ch); }
			if (ch == '.')
			{
				it = 0;
				ch = Char(ch);
				while ((ch >= '0') && (ch <= '9')) ch = Char(ch);
			}
			if ((ch == 'e') || (ch == 'E'))
			{
				it = 0;
				ch = Char(ch);
				if ((ch == '-') || (ch == '+')) ch = Char(ch);
				while ((ch >= '0') && (ch <= '9')) ch = Char(ch);
			}
			if (it != 0) { n *= it; if ((int.MinValue <= n) && (n <= int.MaxValue)) return (int)n; else return n; }
			s = ((lsb.Length > 0) ? lsb.ToString() : new string(lbf, 0, lln));
			return ((outer > STRING) ? types[outer].Convert(s) : s);
		}

		private DateTime DateTime(int outer)
		{
			Space();
			return System.DateTime.Parse((string)Str(OBJECT));
		}

		private TEnum Enum<TEnum>(int outer) where TEnum : struct
		{
			var ch = Space();
			return ((ch == '"') ? (TEnum)System.Enum.Parse(typeof(TEnum), (string)Str(OBJECT)) : (TEnum)Num(OBJECT));
		}

		private object Str(int outer)
		{
			var a = ((outer < OBJECT) ? types[-outer].Props : null);
			int n = ((a != null) ? a.Length : 0), c = 0, i = 0, cc = 0, nc = 0;
			PropInfo p = null; string s = null; var ec = false; var ch = chr;
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
							if (i >= n)
							{
								s = ((lsb.Length > 0) ? lsb.ToString() : new string(lbf, 0, lln));
								return ((outer > STRING) ? types[outer].Convert(s) : s);
							}
							return p;
						default:
							break;
					}
					if (ch < EOF) { if (!ec || (ch >= 128)) Char(nc = ch); else { nc = Esc(ch); ec = false; } } else break;
					if (i < n)
					{
						while ((i < n) && ((c >= (s = (p = a[i]).Name).Length) || (s[c] != nc))) i++;
						if ((i >= n) && ((c == 0) || (s[c - 1] != cc))) i = n;
						c++;
					}
					cc = nc;
				}
			}
			throw Error((outer > OBJECT) ? "Bad literal" : "Bad key");
		}

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
					var key = Str(-outer);
					Space();
					Next(':');
					if (outer > OBJECT)
					{
						if (key is PropInfo)
						{
							var prop = (PropInfo)key;
							prop.Set(this, prop.Outer, obj);
						}
						else
							Val(OBJECT);
					}
					else
						((IDictionary)obj).Add(key, Val(OBJECT));
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
			var cached = types[outer = (outer >= ARRAY) ? outer : ARRAY];
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
					if (cached.Type.IsArray)
					{
						var array = Array.CreateInstance(cached.ElementType, list.Count);
						list.CopyTo(array, 0);
						return array;
					}
					return list;
				}
				while (ch < EOF)
				{
					var items = cached.Items;
					items.Set(this, cached.Inner, list);
					ch = Space();
					if (ch == ']')
					{
						Read();
						if (cached.Type.IsArray)
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

		private object NewObj()
		{
			return new Dictionary<string, object>();
		}

		private int Closure(int outer)
		{
			var props = types[outer].Props;
			for (var i = 0; i < props.Length; i++) props[i].Outer = Entry(props[i].Type);
			return outer;
		}

		private int Entry(Type type)
		{
			return Entry(type, null);
		}

		private int Entry(Type type, Type elem)
		{
			int outer;
			if (!thash.TryGetValue(type, out outer))
			{
				bool b = (elem != null);
				outer = thash.Count;
				elem = (elem ?? GetElementType(type));
				types[outer] = new TypeInfo(type, outer, elem);
				types[outer].Ctor = ((outer > OBJECT) ? types[outer].Ctor : NewObj);
				thash.Add(type, outer);
				if ((elem != null) && !b) types[outer].Inner = Entry(elem);
			}
			return Closure(outer);
		}

		private T DoParse<T>(System.IO.Stream input)
		{
			Reset(StreamRead, StreamNext, StreamChar, StreamSpace);
			using (str = new System.IO.StreamReader(input))
			{
				return (T)Val(Entry(typeof(T)));
			}
		}

		private T DoParse<T>(string input)
		{
			len = input.Length;
			txt = new char[len];
			input.CopyTo(0, txt, 0, len);
			Reset(StringRead, StringNext, StringChar, StringSpace);
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
			Entry(typeof(object[]), typeof(object));
			Entry(typeof(string), typeof(char));
		}

		public T Parse<T>(System.IO.Stream input)
		{
			if (input == null) throw new ArgumentNullException("input", "cannot be null");
			return DoParse<T>(input);
		}

		public T Parse<T>(string input)
		{
			if (input == null) throw new ArgumentNullException("input", "cannot be null");
			return DoParse<T>(input);
		}
	}
}
