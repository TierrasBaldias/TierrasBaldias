﻿#region Header
//   Vorspire    _,-'/-'/  EnumerableExt.cs
//   .      __,-; ,'( '/
//    \.    `-.__`-._`:_,-._       _ , . ``
//     `:-._,------' ` _,`--` -: `_ , ` ,' :
//        `---..__,,--'  (C) 2016  ` -'. -'
//        #  Vita-Nex [http://core.vita-nex.com]  #
//  {o)xxx|===============-   #   -===============|xxx(o}
//        #        The MIT License (MIT)          #
#endregion

#region References
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Server;
#endregion

namespace System
{
	public static class EnumerableExtUtility
	{
		public static IEnumerable Ensure(this IEnumerable source)
		{
			return source ?? Enumerable.Empty<object>();
		}

		public static IEnumerable<T> Ensure<T>(this IEnumerable<T> source)
		{
			return source ?? Enumerable.Empty<T>();
		}

		public static IEnumerator GetEnumerator(this IEnumerable source)
		{
			return Ensure(source).GetEnumerator();
		}

		public static IEnumerator<T> GetEnumerator<T>(this T[] source)
		{
			return Ensure(source).GetEnumerator();
		}

		public static IEnumerable<T> ToEnumerable<T>(this T value)
		{
			if (value != null)
			{
				yield return value;
			}
		}

		public static Queue<T> ToQueue<T>(this IEnumerable<T> source)
		{
			return new Queue<T>(Ensure(source));
		}

		public static IEnumerable<T> Clear<T>(this IEnumerable<T> source)
		{
			return Ensure(source).Take(0);
		}

		public static void Clear<T>(this T[] source)
		{
			for (var i = 0; i < source.Length; i++)
			{
				source[i] = default(T);
			}
		}

		public static IEnumerable<T> With<T>(this IEnumerable<T> source, T include)
		{
			foreach (var o in Ensure(source))
			{
				yield return o;
			}

			yield return include;
		}

		public static IEnumerable<T> With<T>(this IEnumerable<T> source, params T[] include)
		{
			return Ensure(source).Union(Ensure(include));
		}

		public static IEnumerable<T> With<T>(this IEnumerable<T> source, IEnumerable<T> include)
		{
			return Ensure(source).Union(include);
		}

		public static IEnumerable<T> Without<T>(this IEnumerable<T> source, params T[] exclude)
		{
			return Ensure(source).Except(Ensure(exclude));
		}

		public static IEnumerable<T> Without<T>(this IEnumerable<T> source, IEnumerable<T> exclude)
		{
			return Ensure(source).Except(exclude);
		}

		/// <summary>
		///     Trim the given number of entries from the start of a collection.
		/// </summary>
		public static void TrimStart<T>(this List<T> source, int count)
		{
			if (source == null)
			{
				return;
			}

			while (source.Count > 0 && --count >= 0)
			{
				source.RemoveAt(0);
			}
		}

		/// <summary>
		///     Trim the given number of entries from the end of a collection.
		/// </summary>
		public static void TrimEnd<T>(this List<T> source, int count)
		{
			if (source == null)
			{
				return;
			}

			while (source.Count > 0 && --count >= 0)
			{
				source.RemoveAt(source.Count - 1);
			}
		}

		/// <summary>
		///     Trim the entries from the start of a collection until the given count is reached.
		/// </summary>
		public static void TrimStartTo<T>(this List<T> source, int count)
		{
			if (source == null)
			{
				return;
			}

			while (source.Count > count)
			{
				source.RemoveAt(0);
			}
		}

		/// <summary>
		///     Trim the entries from the end of a collection until the given count is reached.
		/// </summary>
		public static void TrimEndTo<T>(this List<T> source, int count)
		{
			if (source == null)
			{
				return;
			}

			while (source.Count > count)
			{
				source.RemoveAt(source.Count - 1);
			}
		}

		public static void AddRange<T>(this List<T> source, params T[] entries)
		{
			if (source != null && entries != null)
			{
				source.AddRange(entries);
			}
		}

		public static bool AddOrReplace<T>(this List<T> source, T entry)
		{
			return AddOrReplace(source, entry, entry);
		}

		public static bool AddOrReplace<T>(this List<T> source, T search, T replace)
		{
			if (source == null)
			{
				return false;
			}

			var index = source.IndexOf(search);

			if (!InBounds(source, index))
			{
				source.Add(replace);
			}
			else
			{
				source[index] = replace;
			}

			return true;
		}

		public static bool AddOrReplace<TKey, TVal>(this IDictionary<TKey, TVal> source, TKey key, TVal val)
		{
			if (source == null || key == null)
			{
				return false;
			}

			if (!source.ContainsKey(key))
			{
				source.Add(key, val);
			}
			else
			{
				source[key] = val;
			}

			return true;
		}

		public static bool AddOrReplace<TKey, TVal>(this IDictionary<TKey, TVal> source, TKey key, Func<TVal, TVal> resolver)
		{
			if (source == null || key == null || resolver == null)
			{
				return false;
			}

			if (!source.ContainsKey(key))
			{
				source.Add(key, resolver(default(TVal)));
			}
			else
			{
				source[key] = resolver(source[key]);
			}

			return true;
		}

		/// <summary>
		///     Creates a multi-dimensional array from a multi-dimensional collection.
		/// </summary>
		public static T[][] ToMultiArray<T>(this IEnumerable<IEnumerable<T>> source)
		{
			return Ensure(source).Select(e => e.ToArray()).ToArray();
		}

		public static T[] FreeToArray<T>(this List<T> source, bool clear)
		{
			if (source == null)
			{
				return new T[0];
			}

			var buffer = source.ToArray();

			Free(source, clear);

			return buffer;
		}

		public static T[] FreeToArray<T>(this Stack<T> source, bool clear)
		{
			if (source == null)
			{
				return new T[0];
			}

			var buffer = source.ToArray();

			Free(source, clear);

			return buffer;
		}

		public static T[][] FreeToMultiArray<T>(this IEnumerable<List<T>> source, bool clear)
		{
			return Ensure(source).Select(o => FreeToArray(o, clear)).ToArray();
		}

		public static T[][] FreeToMultiArray<T>(this IEnumerable<Stack<T>> source, bool clear)
		{
			return Ensure(source).Select(o => FreeToArray(o, clear)).ToArray();
		}

		public static void Free<T>(this Stack<T> source, bool clear)
		{
			if (source == null)
			{
				return;
			}

			if (clear)
			{
				source.Clear();
			}

			source.TrimExcess();
		}

		public static void Free<T>(this IEnumerable<Stack<T>> source, bool clear)
		{
			foreach (var o in source)
			{
				Free(o, clear);
			}
		}

		public static void Free<T>(this List<T> source, bool clear)
		{
			if (source == null)
			{
				return;
			}

			if (clear)
			{
				source.Clear();
			}

			source.TrimExcess();
		}

		public static void Free<T>(this IEnumerable<List<T>> source, bool clear)
		{
			foreach (var o in source)
			{
				Free(o, clear);
			}
		}

		public static void Free<T>(this HashSet<T> source, bool clear)
		{
			if (source == null)
			{
				return;
			}

			if (clear)
			{
				source.Clear();
			}

			source.TrimExcess();
		}

		public static void Free<T>(this IEnumerable<HashSet<T>> source, bool clear)
		{
			foreach (var o in source)
			{
				Free(o, clear);
			}
		}

		public static void Free<T>(this Queue<T> source, bool clear)
		{
			if (source == null)
			{
				return;
			}

			if (clear)
			{
				source.Clear();
			}

			source.TrimExcess();
		}

		public static void Free<T>(this IEnumerable<Queue<T>> source, bool clear)
		{
			foreach (var o in source)
			{
				Free(o, clear);
			}
		}

		public static bool InBounds<T>(this T[][][] source, int x, int y, int z)
		{
			return InBounds(source, x, y) && InBounds(source[x][y], z);
		}

		public static bool InBounds<T>(this T[][] source, int x, int y)
		{
			return InBounds(source, x) && InBounds(source[x], y);
		}

		public static bool InBounds<T>(this T[] source, int index)
		{
			return source != null && index >= 0 && index < source.Length;
		}

		public static bool InBounds<T>(this ICollection<T> source, int index)
		{
			return source != null && index >= 0 && index < source.Count;
		}

		public static bool InBounds<T>(this IEnumerable<T> source, int index)
		{
			return source != null && index >= 0 && index < source.Count();
		}

		public static K Intern<T, K>(this List<T> source, int index, Func<T, K> ctor)
		{
			if (InBounds(source, index))
			{
				return ctor(source[index]);
			}

			return ctor(default(T));
		}

		public static K Intern<TKey, TValue, K>(this Dictionary<TKey, TValue> source, TKey key, Func<TValue, K> ctor)
		{
			if (source.ContainsKey(key))
			{
				return ctor(source[key]);
			}

			return ctor(default(TValue));
		}

		public static int Prune<T>(this List<T> source)
		{
			return Prune(source, false);
		}

		public static int Prune<T>(this List<T> source, bool reverse)
		{
			return Prune(source, reverse, o => o);
		}

		public static int Prune<T>(this List<T> source, Func<T, object> equalitySelector)
		{
			return Prune(source, false, equalitySelector);
		}

		public static int Prune<T>(this List<T> source, bool reverse, Func<T, object> equalitySelector)
		{
			if (source == null || source.Count < 2)
			{
				return 0;
			}

			var count = 0;
			var total = source.Count;

			while (--total > 0)
			{
				var obj = equalitySelector(source[total]);
				var idx =
					source.FindIndex(
						o => ReferenceEquals(obj, null) ? ReferenceEquals(equalitySelector(o), null) : obj.Equals(equalitySelector(o)));

				if (idx == total || idx < 0 || idx >= source.Count)
				{
					continue;
				}

				source.RemoveAt(reverse ? idx : total);
				++count;
			}

			return count;
		}

		public static int PopRange<T>(this List<T> source, T[] buffer)
		{
			var count = -1;

			if (source != null && buffer != null && buffer.Length != 0)
			{
				count = 0;

				while (source.Count > 0 && count < buffer.Length)
				{
					buffer[count++] = Pop(source);
				}
			}

			return count;
		}

		public static int RemoveRange<T>(this List<T> source, params T[] entries)
		{
			var count = -1;

			if (source != null && entries != null)
			{
				count = entries.Count(source.Remove);
			}

			return count;
		}

		public static int RemoveRange<T>(this List<T> source, IEnumerable<T> entries)
		{
			var count = -1;

			if (source != null && entries != null)
			{
				var buffer = entries.ToList();

				count = buffer.Count(source.Remove);

				Free(buffer, true);
			}

			return count;
		}

		public static int RemoveRange<TKey, TVal>(this Dictionary<TKey, TVal> source, Func<TKey, TVal, bool> predicate)
		{
			var count = -1;

			if (source != null && predicate != null)
			{
				var buffer = source.Where(kv => predicate(kv.Key, kv.Value)).Select(kv => kv.Key).ToList();

				count = buffer.Count(source.Remove);

				Free(buffer, true);
			}

			return count;
		}

		public static int RemoveRange<TKey, TVal>(
			this Dictionary<TKey, TVal> source,
			Func<KeyValuePair<TKey, TVal>, bool> predicate)
		{
			var count = -1;

			if (source != null && predicate != null)
			{
				var buffer = source.Where(predicate).Select(kv => kv.Key).ToList();

				count = buffer.Count(source.Remove);

				Free(buffer, true);
			}

			return count;
		}

		public static int RemoveRange<TKey, TVal>(
			this Dictionary<TKey, TVal> source,
			params KeyValuePair<TKey, TVal>[] entries)
		{
			var count = -1;

			if (source != null && entries != null)
			{
				count = entries.Count(kv => source.Remove(kv.Key));
			}

			return count;
		}

		public static int RemoveRange<TKey, TVal>(
			this Dictionary<TKey, TVal> source,
			IEnumerable<KeyValuePair<TKey, TVal>> entries)
		{
			var count = -1;

			if (source != null && entries != null)
			{
				var buffer = entries.Select(kv => kv.Key).ToList();

				count = buffer.Count(source.Remove);

				Free(buffer, true);
			}

			return count;
		}

		public static int RemoveKeyRange<TKey, TVal>(this Dictionary<TKey, TVal> source, Func<TKey, bool> predicate)
		{
			var count = -1;

			if (source != null && predicate != null)
			{
				var buffer = source.Keys.Where(predicate).ToList();

				count = buffer.Count(source.Remove);

				Free(buffer, true);
			}

			return count;
		}

		public static int RemoveKeyRange<TKey, TVal>(this Dictionary<TKey, TVal> source, params TKey[] keys)
		{
			var count = -1;

			if (source != null && keys != null)
			{
				count = keys.Count(source.Remove);
			}

			return count;
		}

		public static int RemoveKeyRange<TKey, TVal>(this Dictionary<TKey, TVal> source, IEnumerable<TKey> keys)
		{
			var count = -1;

			if (source != null && keys != null)
			{
				var buffer = keys.ToList();

				count = buffer.Count(source.Remove);

				Free(buffer, true);
			}

			return count;
		}

		public static int RemoveValueRange<TKey, TVal>(this Dictionary<TKey, TVal> source, params TVal[] values)
		{
			var count = -1;

			if (source != null && values != null)
			{
				var buffer = values.SelectMany(o => source.Where(kv => Equals(kv.Value, o))).Select(kv => kv.Key);

				count = buffer.Count(source.Remove);
			}

			return count;
		}

		public static int RemoveValueRange<TKey, TVal>(this Dictionary<TKey, TVal> source, IEnumerable<TVal> values)
		{
			var count = -1;

			if (source != null && values != null)
			{
				var buffer = values.SelectMany(o => source.Where(kv => Equals(kv.Value, o))).Select(kv => kv.Key).ToList();

				count = buffer.Count(source.Remove);

				Free(buffer, true);
			}

			return count;
		}

		public static int RemoveValueRange<TKey, TVal>(this Dictionary<TKey, TVal> source, Func<TVal, bool> predicate)
		{
			var count = -1;

			if (source != null && predicate != null)
			{
				var buffer = source.Where(kv => predicate(kv.Value)).Select(kv => kv.Key).ToList();

				count = buffer.Count(source.Remove);

				Free(buffer, true);
			}

			return count;
		}

		/// <summary>
		///     Combines the elements of each sequence in the source sequence in to a standard array.
		/// </summary>
		public static T[] Combine<T>(this IEnumerable<IEnumerable<T>> source)
		{
			return Ensure(source).SelectMany(Ensure).ToArray();
		}

		public static void SetAll<T>(this T[] source, T entry)
		{
			SetAll(source, i => entry);
		}

		public static void SetAll<T>(this T[] source, Func<T> instantiate)
		{
			if (source == null || source.Length == 0)
			{
				return;
			}

			for (var i = 0; i < source.Length; i++)
			{
				source[i] = instantiate != null ? instantiate() : source[i];
			}
		}

		public static void SetAll<T>(this T[] source, Func<int, T> instantiate)
		{
			if (source == null || source.Length == 0)
			{
				return;
			}

			for (var i = 0; i < source.Length; i++)
			{
				source[i] = instantiate != null ? instantiate(i) : source[i];
			}
		}

		public static void SetAll<T>(this T[] source, Func<int, T, T> instantiate)
		{
			if (source == null || source.Length == 0)
			{
				return;
			}

			for (var i = 0; i < source.Length; i++)
			{
				source[i] = instantiate != null ? instantiate(i, source[i]) : source[i];
			}
		}

		public static void SetAll<T>(this List<T> source, T entry)
		{
			SetAll(source, i => entry);
		}

		public static void SetAll<T>(this List<T> source, Func<T> instantiate)
		{
			if (source == null)
			{
				return;
			}

			while (source.Count < source.Capacity)
			{
				source.Add(default(T));
			}

			for (var i = 0; i < source.Count; i++)
			{
				source[i] = instantiate != null ? instantiate() : source[i];
			}
		}

		public static void SetAll<T>(this List<T> source, Func<int, T> instantiate)
		{
			if (source == null)
			{
				return;
			}

			while (source.Count < source.Capacity)
			{
				source.Add(default(T));
			}

			for (var i = 0; i < source.Count; i++)
			{
				source[i] = instantiate != null ? instantiate(i) : source[i];
			}
		}

		public static void SetAll<T>(this List<T> source, Func<int, T, T> instantiate)
		{
			if (source == null)
			{
				return;
			}

			while (source.Count < source.Capacity)
			{
				source.Add(default(T));
			}

			for (var i = 0; i < source.Count; i++)
			{
				source[i] = instantiate != null ? instantiate(i, source[i]) : source[i];
			}
		}

		/// <summary>
		///     Perform an action for each element in a clone* of the source sequence.
		///     The action incorporates the index of each element in the source sequence.
		///     The action may modify the source list in a safe context.
		///     *Standard Arrays are not cloned.
		/// </summary>
		public static void For<T>(this IEnumerable<T> source, Action<int, T> action)
		{
			if (source == null || action == null)
			{
				return;
			}

			var i = 0;

			if (source is T[])
			{
				foreach (var o in source)
				{
					action(i++, o);
				}

				return;
			}

			var buffer = source.ToList();

			foreach (var o in buffer)
			{
				action(i++, o);
			}

			Free(buffer, true);
		}

		/// <summary>
		///     Perform an action for each key-value pair in a clone of the source dictionary.
		///     The action incorporates the index of each pair in the source dictionary.
		///     The action may modify the source dictionary in a safe context.
		/// </summary>
		public static void For<TKey, TVal>(this IDictionary<TKey, TVal> source, Action<int, TKey, TVal> action)
		{
			if (source == null || action == null)
			{
				return;
			}

			var i = 0;
			var buffer = source.ToList();

			foreach (var kv in source)
			{
				action(i++, kv.Key, kv.Value);
			}

			Free(buffer, true);
		}

		/// <summary>
		///     Perform an action for each element in the source array, in reverse order.
		///     The action incorporates the index of each element in the source array.
		///     The action may modify the source array in a safe context.
		/// </summary>
		public static void ForReverse<T>(this T[] source, Action<int, T> action)
		{
			if (source == null || action == null)
			{
				return;
			}

			var i = source.Length;

			while (--i >= 0)
			{
				if (i < source.Length)
				{
					action(i, source[i]);
				}
			}
		}

		/// <summary>
		///     Perform an action for each element in the source list, in reverse order.
		///     The action incorporates the index of each element in the source list.
		///     The action may modify the source list in a safe context.
		/// </summary>
		public static void ForReverse<T>(this List<T> source, Action<int, T> action)
		{
			if (source == null || action == null)
			{
				return;
			}

			var i = source.Count;

			while (--i >= 0)
			{
				if (i < source.Count)
				{
					action(i, source[i]);
				}
			}
		}

		/// <summary>
		///     Perform an action for each element in the source array, in reverse order.
		///     The action may modify the source array in a safe context.
		/// </summary>
		public static void ForEachReverse<T>(this T[] source, Action<T> action)
		{
			if (source == null || action == null)
			{
				return;
			}

			var i = source.Length;

			while (--i >= 0)
			{
				if (i < source.Length)
				{
					action(source[i]);
				}
			}
		}

		/// <summary>
		///     Perform an action for each element in the source list, in reverse order.
		///     The action may modify the source list in a safe context.
		/// </summary>
		public static void ForEachReverse<T>(this List<T> source, Action<T> action)
		{
			if (source == null || action == null)
			{
				return;
			}

			var i = source.Count;

			while (--i >= 0)
			{
				if (i < source.Count)
				{
					action(source[i]);
				}
			}
		}

		/// <summary>
		///     Perform an action for each element in a clone* of the source sequence.
		///     The action may modify the source sequence in a safe context.
		///     *Standard Arrays are not cloned.
		/// </summary>
		public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
		{
			if (source == null || action == null)
			{
				return;
			}

			if (source is T[])
			{
				foreach (var o in source)
				{
					action(o);
				}

				return;
			}

			var buffer = source.ToList();

			foreach (var o in buffer)
			{
				action(o);
			}

			Free(buffer, true);
		}

		/// <summary>
		///     Perform an action for each element within the given range, in a clone* of the source sequence.
		///     The action incorporates the index of each element in the source sequence.
		///     The action may modify the source sequence in a safe context.
		///     *Standard Arrays are not cloned.
		/// </summary>
		public static void ForRange<T>(this IEnumerable<T> source, int offset, Action<int, T> action)
		{
			if (source == null || offset < 0 || action == null)
			{
				return;
			}

			if (source is T[])
			{
				foreach (var o in source.Skip(offset))
				{
					action(offset++, o);
				}

				return;
			}

			var buffer = source.Skip(offset).ToList();

			foreach (var o in buffer)
			{
				action(offset++, o);
			}

			Free(buffer, true);
		}

		/// <summary>
		///     Perform an action for each element within the given range, in a clone* of the source sequence.
		///     The action incorporates the index of each element in the source sequence.
		///     The action may modify the source sequence in a safe context.
		///     *Standard Arrays are not cloned.
		/// </summary>
		public static void ForRange<T>(this IEnumerable<T> source, int offset, int count, Action<int, T> action)
		{
			if (source == null || offset < 0 || count <= 0 || action == null)
			{
				return;
			}

			if (source is T[])
			{
				foreach (var o in source.Skip(offset).Take(count))
				{
					action(offset++, o);
				}

				return;
			}

			var buffer = source.Skip(offset).Take(count).ToList();

			foreach (var o in buffer)
			{
				action(offset++, o);
			}

			Free(buffer, true);
		}

		/// <summary>
		///     Perform an action for each key-value pair in a clone of the source dictionary.
		///     The action may modify the source dictionary in a safe context.
		/// </summary>
		public static void ForEach<TKey, TVal>(this IDictionary<TKey, TVal> source, Action<TKey, TVal> action)
		{
			if (source == null || source.Count == 0 || action == null)
			{
				return;
			}

			var buffer = source.ToList();

			foreach (var kv in buffer)
			{
				action(kv.Key, kv.Value);
			}

			Free(buffer, true);
		}

		/// <summary>
		///     Perform an action for each key-value pair in a clone of the source dictionary.
		///     The action may modify the source dictionary in a safe context.
		/// </summary>
		public static void ForEach<TKey, TVal>(this IDictionary<TKey, TVal> source, Action<KeyValuePair<TKey, TVal>> action)
		{
			if (source == null || source.Count == 0 || action == null)
			{
				return;
			}

			var buffer = source.ToList();

			foreach (var kv in buffer)
			{
				action(kv);
			}

			Free(buffer, true);
		}

		/// <summary>
		///     Perform an action for each key-value pair within the given range, in a clone of the source dictionary.
		///     The action incorporates the index of each pair in the source dictionary.
		///     The action may modify the source dictionary in a safe context.
		/// </summary>
		public static void ForRange<TKey, TVal>(
			this IDictionary<TKey, TVal> source,
			int offset,
			Action<int, KeyValuePair<TKey, TVal>> action)
		{
			if (source == null || source.Count == 0 || offset < 0 || action == null)
			{
				return;
			}

			var buffer = source.Skip(offset).ToList();

			foreach (var kv in buffer)
			{
				action(offset++, kv);
			}

			Free(buffer, true);
		}

		/// <summary>
		///     Perform an action for each key-value pair within the given range, in a clone of the source dictionary.
		///     The action incorporates the index of each pair in the source dictionary.
		///     The action may modify the source dictionary in a safe context.
		/// </summary>
		public static void ForRange<TKey, TVal>(
			this IDictionary<TKey, TVal> source,
			int offset,
			int count,
			Action<int, KeyValuePair<TKey, TVal>> action)
		{
			if (source == null || source.Count == 0 || offset < 0 || count <= 0 || action == null)
			{
				return;
			}

			var buffer = source.Skip(offset).Take(count).ToList();

			foreach (var kv in buffer)
			{
				action(offset++, kv);
			}

			Free(buffer, true);
		}

		/// <summary>
		///     Perform an action for each key-value pair within the given range, in a clone of the source dictionary.
		///     The action may modify the source dictionary in a safe context.
		/// </summary>
		public static void ForRange<TKey, TVal>(this IDictionary<TKey, TVal> source, int offset, Action<TKey, TVal> action)
		{
			if (source != null && source.Count > 0 && offset >= 0 && action != null)
			{
				ForRange(source, offset, (i, kv) => action(kv.Key, kv.Value));
			}
		}

		/// <summary>
		///     Perform an action for each key-value pair within the given range, in a clone of the source dictionary.
		///     The action may modify the source dictionary in a safe context.
		/// </summary>
		public static void ForRange<TKey, TVal>(
			this IDictionary<TKey, TVal> source,
			int offset,
			int count,
			Action<TKey, TVal> action)
		{
			if (source != null && source.Count > 0 && offset >= 0 && count > 0 && action != null)
			{
				ForRange(source, offset, count, (i, kv) => action(kv.Key, kv.Value));
			}
		}

		/// <summary>
		///     Perform an action for each key-value pair within the given range, in a clone of the source dictionary.
		///     The action incorporates the index of each pair in the source dictionary.
		///     The action may modify the source dictionary in a safe context.
		/// </summary>
		public static void ForRange<TKey, TVal>(
			this IDictionary<TKey, TVal> source,
			int offset,
			Action<int, TKey, TVal> action)
		{
			if (source != null && source.Count > 0 && offset >= 0 && action != null)
			{
				ForRange(source, offset, (i, kv) => action(i, kv.Key, kv.Value));
			}
		}

		/// <summary>
		///     Perform an action for each key-value pair within the given range, in a clone of the source dictionary.
		///     The action incorporates the index of each pair in the source dictionary.
		///     The action may modify the source dictionary in a safe context.
		/// </summary>
		public static void ForRange<TKey, TVal>(
			this IDictionary<TKey, TVal> source,
			int offset,
			int count,
			Action<int, TKey, TVal> action)
		{
			if (source != null && source.Count > 0 && offset >= 0 && count > 0 && action != null)
			{
				ForRange(source, offset, count, (i, kv) => action(i, kv.Key, kv.Value));
			}
		}

		public static int IndexOf<T>(this IEnumerable<T> source, T obj)
		{
			return IndexOf(source, obj, null);
		}

		public static int IndexOf<T>(this IEnumerable<T> source, T obj, Func<T, bool> predicate)
		{
			var index = -1;

			foreach (var i in IndexOfAll(source, obj, predicate))
			{
				index = i;
				break;
			}

			return index;
		}

		public static int LastIndexOf<T>(this IEnumerable<T> source, T obj)
		{
			return LastIndexOf(source, obj, null);
		}

		public static int LastIndexOf<T>(this IEnumerable<T> source, T obj, Func<T, bool> predicate)
		{
			var index = -1;

			foreach (var i in IndexOfAll(source, obj, predicate))
			{
				index = i;
			}

			return index;
		}

		public static IEnumerable<int> IndexOfAll<T>(this IEnumerable<T> source, T obj, Func<T, bool> predicate)
		{
			return
				Ensure(source)
					.Select((o, i) => (predicate != null && predicate(o)) || (ReferenceEquals(o, obj) || Equals(o, obj)) ? i : -1)
					.Where(i => i >= 0);
		}

		public static IEnumerable<T> TakeWhile<T>(this IEnumerable<T> source, Func<T, int, bool> predicate, int count)
		{
			return (predicate == null ? Ensure(source) : Ensure(source).TakeWhile(predicate)).Take(count);
		}

		public static IEnumerable<T> TakeUntil<T>(this IEnumerable<T> source, Func<T, bool> predicate)
		{
			return predicate == null ? Ensure(source) : Ensure(source).TakeWhile(o => !predicate(o));
		}

		public static IEnumerable<T> TakeUntil<T>(this IEnumerable<T> source, Func<T, bool> predicate, int count)
		{
			return TakeUntil(source, predicate).Take(count);
		}

		public static IEnumerable<T> TakeUntil<T>(this IEnumerable<T> source, Func<T, int, bool> predicate)
		{
			return predicate == null ? Ensure(source) : Ensure(source).TakeWhile((o, i) => !predicate(o, i));
		}

		public static IEnumerable<T> TakeUntil<T>(this IEnumerable<T> source, Func<T, int, bool> predicate, int count)
		{
			return TakeUntil(source, predicate).Take(count);
		}

		public static IEnumerable<T> TakeRandom<T>(this IEnumerable<T> source, int count)
		{
			return Randomize(source).Take(count);
		}

		public static IEnumerable<T> Randomize<T>(this IEnumerable<T> source)
		{
			return Ensure(source).OrderByDescending(e => Utility.RandomDouble());
		}

		public static void Shuffle<T>(this List<T> source)
		{
			if (source == null || source.Count < 2)
			{
				return;
			}

			T value;
			int index, count = source.Count;

			while (--count >= 0)
			{
				index = Utility.Random(count + 1);
				value = source[index];
				source[index] = source[count];
				source[count] = value;
			}
		}

		public static void Shuffle<T>(this T[] source)
		{
			if (source == null || source.Length < 2)
			{
				return;
			}

			T value;
			int index, count = source.Length;

			while (--count >= 0)
			{
				index = Utility.Random(count + 1);
				value = source[index];
				source[index] = source[count];
				source[count] = value;
			}
		}

		public static T GetRandom<T>(this IEnumerable<T> source)
		{
			return GetRandom(source, default(T));
		}

		public static T GetRandom<T>(this IEnumerable<T> source, T def)
		{
			if (source == null)
			{
				return def;
			}

			if (source is T[])
			{
				return GetRandom((T[])source, def);
			}

			if (source is IList<T>)
			{
				return GetRandom((IList<T>)source, def);
			}

			if (source is ISet<T>)
			{
				return GetRandom((ISet<T>)source, def);
			}

			if (source is Queue<T>)
			{
				return GetRandom((Queue<T>)source, def);
			}

			foreach (var o in Randomize(source))
			{
				return o;
			}

			return def;
		}

		public static T GetRandom<T>(this T[] source)
		{
			return GetRandom(source, default(T));
		}

		public static T GetRandom<T>(this T[] source, T def)
		{
			return source == null || source.Length == 0 ? def : source[Utility.Random(source.Length)];
		}

		public static T GetRandom<T>(this IList<T> source)
		{
			return GetRandom(source, default(T));
		}

		public static T GetRandom<T>(this IList<T> source, T def)
		{
			return source == null || source.Count == 0 ? def : source[Utility.Random(source.Count)];
		}

		public static T GetRandom<T>(this ISet<T> source)
		{
			return GetRandom(source, default(T));
		}

		public static T GetRandom<T>(this ISet<T> source, T def)
		{
			return source == null || source.Count == 0 ? def : source.ElementAt(Utility.Random(source.Count));
		}

		public static T GetRandom<T>(this Queue<T> source)
		{
			return GetRandom(source, default(T));
		}

		public static T GetRandom<T>(this Queue<T> source, T def)
		{
			return source == null || source.Count == 0 ? def : source.ElementAt(Utility.Random(source.Count));
		}

		public static T Pop<T>(this List<T> list)
		{
			return Pop(list, default(T));
		}

		public static T Pop<T>(this List<T> source, T def)
		{
			if (source == null || source.Count == 0)
			{
				return def;
			}

			var o = source[0];

			source.Remove(o);

			return o;
		}

		public static bool Push<T>(this List<T> source, T o)
		{
			if (source != null)
			{
				source.Insert(0, o);
				return true;
			}

			return false;
		}

		public static List<T> RemoveAllGet<T>(this List<T> source, Predicate<T> match)
		{
			return RemoveAllFind(source, match).ToList();
		}

		public static IEnumerable<T> RemoveAllFind<T>(this List<T> source, Predicate<T> match)
		{
			if (source == null)
			{
				yield break;
			}

			var index = source.Count;

			while (--index >= 0)
			{
				var obj = source[index];

				if (match == null || match(obj))
				{
					source.RemoveAt(index);
					yield return obj;
				}
			}

			Free(source, false);
		}

		public static IEnumerable<T> Not<T>(this IEnumerable<T> source, Func<T, bool> predicate)
		{
			return predicate == null ? Ensure(source) : Ensure(source).Where(o => !predicate(o));
		}

		public static bool Contains<T>(this T[] source, T obj)
		{
			return IndexOf(source, obj) != -1;
		}

		public static bool ContainsAny<T>(this T[] source, params T[] obj)
		{
			return Ensure(obj).Any(o => IndexOf(source, o) != -1);
		}

		public static void Reverse<T>(this T[] source)
		{
			if (source == null || source.Length < 2)
			{
				return;
			}

			var i = 0;

			foreach (var o in Ensure(source).Reverse())
			{
				source[i++] = o;
			}
		}

		public static void Reverse<T>(this T[] source, int offset, int count)
		{
			if (source == null || source.Length < 2)
			{
				return;
			}

			var i = offset;

			foreach (var o in Ensure(source).Skip(offset).Take(count).Reverse())
			{
				source[i++] = o;
			}
		}

		public static T[] Dupe<T>(this T[] source)
		{
			return Ensure(source).ToArray();
		}

		public static T[] Dupe<T>(this T[] source, int offset)
		{
			return Ensure(source).Skip(offset).ToArray();
		}

		public static T[] Dupe<T>(this T[] source, int offset, int count)
		{
			return Ensure(source).Skip(offset).Take(count).ToArray();
		}

		public static T[] Dupe<T>(this T[] source, Func<T, T> selector)
		{
			return (selector == null ? Ensure(source) : Ensure(source).Select(selector)).ToArray();
		}

		public static T[] Dupe<T>(this T[] source, Func<T, int, T> selector)
		{
			return (selector == null ? Ensure(source) : Ensure(source).Select(selector)).ToArray();
		}

		public static List<T> Merge<T>(this List<T> source, params List<T>[] sources)
		{
			return With(source, Ensure(sources).SelectMany(l => l)).ToList();
		}

		public static T[] Merge<T>(this T[] source, params T[][] sources)
		{
			return With(source, Ensure(sources).SelectMany(l => l)).ToArray();
		}

		public static List<T> ChainSort<T>(this List<T> source)
		{
			if (source != null)
			{
				source.Sort();
			}

			return source;
		}

		public static List<T> ChainSort<T>(this List<T> source, Comparison<T> compare)
		{
			if (compare == null)
			{
				return ChainSort(source);
			}

			if (source != null)
			{
				source.Sort(compare);
			}

			return source;
		}

		public static KeyValuePair<TKey, TVal> Pop<TKey, TVal>(this IDictionary<TKey, TVal> source)
		{
			var kvp = default(KeyValuePair<TKey, TVal>);

			if (source != null && source.Count > 0)
			{
				kvp = source.FirstOrDefault();

				if (kvp.Key != null)
				{
					source.Remove(kvp.Key);
				}
			}

			return kvp;
		}

		public static bool Push<TKey, TVal>(this IDictionary<TKey, TVal> source, TKey key, TVal value)
		{
			if (source == null || key == null)
			{
				return false;
			}

			var swap = new Dictionary<TKey, TVal>(source);

			source.Clear();
			source.Add(key, value);

			swap.Remove(key);

			foreach (var kv in swap)
			{
				source.Add(kv.Key, kv.Value);
			}

			swap.Clear();
			return true;
		}

		public static TKey GetKey<TKey, TVal>(this IDictionary<TKey, TVal> source, TVal value)
		{
			return Ensure(source).FirstOrDefault(kv => ReferenceEquals(kv.Value, value) || Equals(value, kv.Value)).Key;
		}

		public static TKey GetKeyAt<TKey, TVal>(this IDictionary<TKey, TVal> source, int index)
		{
			return ElementAtOrDefault(source, index).Key;
		}

		public static TVal GetValue<TKey, TVal>(this IDictionary<TKey, TVal> source, TKey key)
		{
			if (source == null || key == null)
			{
				return default(TVal);
			}

			TVal value;
			source.TryGetValue(key, out value);
			return value;
		}

		public static TVal GetValueAt<TKey, TVal>(this IDictionary<TKey, TVal> source, int index)
		{
			return ElementAtOrDefault(source, index).Value;
		}

		public static IEnumerable<KeyValuePair<TKey, TVal>> GetRange<TKey, TVal>(
			this IDictionary<TKey, TVal> source,
			int index,
			int count)
		{
			return Ensure(source).Skip(index).Take(count);
		}

		public static Dictionary<TKey, TVal> Merge<TKey, TVal>(
			this IDictionary<TKey, TVal> source,
			params IDictionary<TKey, TVal>[] sources)
		{
			return
				With(source, Ensure(sources).SelectMany(o => o))
					.ToLookup(kv => kv.Key, kv => kv.Value)
					.ToDictionary(g => g.Key, g => g.FirstOrDefault());
		}

		public static Dictionary<TKey, TVal> Merge<TKey, TVal>(
			this IEnumerable<IDictionary<TKey, TVal>> sources,
			Func<IGrouping<TKey, TVal>, TKey> keySelector = null,
			Func<IGrouping<TKey, TVal>, TVal> elementSelector = null)
		{
			return
				Ensure(sources)
					.SelectMany(d => d)
					.ToLookup(kv => kv.Key, kv => kv.Value)
					.ToDictionary(keySelector ?? (g => g.Key), elementSelector ?? (g => g.FirstOrDefault()));
		}

		public static int Enqueue<T>(this Queue<T> source, params T[] buffer)
		{
			return EnqueueRange(source, buffer);
		}

		public static int EnqueueRange<T>(this Queue<T> source, T[] buffer)
		{
			var count = 0;

			foreach (var o in Ensure(buffer))
			{
				++count;
				source.Enqueue(o);
			}

			return count;
		}

		public static int EnqueueRange<T>(this Queue<T> source, IEnumerable<T> range)
		{
			var count = 0;

			foreach (var o in Ensure(range))
			{
				++count;
				source.Enqueue(o);
			}

			return count;
		}

		public static int DequeueRange<T>(this Queue<T> source, T[] buffer)
		{
			var count = 0;

			if (buffer != null)
			{
				foreach (var o in DequeueRange(source, buffer.Length))
				{
					buffer[count++] = o;
				}
			}

			return count;
		}

		public static IEnumerable<T> DequeueRange<T>(this Queue<T> source, int count)
		{
			return DequeueRange(source, ref count);
		}

		public static IEnumerable<T> DequeueRange<T>(this Queue<T> source, ref int count)
		{
			count = Math.Max(0, Math.Min(source.Count, count));

			return Pad(count, source.Dequeue);
		}

		public static IEnumerable<T> PadStart<T>(this IEnumerable<T> source, int count, Func<T> selector = null)
		{
			return With(Pad(count, selector), source);
		}

		public static IEnumerable<T> PadStart<T>(this IEnumerable<T> source, int count, Func<int, T> selector = null)
		{
			return With(Pad(count, selector), source);
		}

		public static IEnumerable<T> PadEnd<T>(this IEnumerable<T> source, int count, Func<T> selector = null)
		{
			return With(source, Pad(count, selector));
		}

		public static IEnumerable<T> PadEnd<T>(this IEnumerable<T> source, int count, Func<int, T> selector = null)
		{
			return With(source, Pad(count, selector));
		}

		private static IEnumerable<T> Pad<T>(int count, Func<T> selector = null)
		{
			while (--count >= 0)
			{
				yield return (selector == null ? default(T) : selector());
			}
		}

		private static IEnumerable<T> Pad<T>(int count, Func<int, T> selector = null)
		{
			for (var i = 0; i < count; i++)
			{
				yield return (selector == null ? default(T) : selector(i));
			}
		}

		private static readonly Regex _NaturalOrderExpr = new Regex(@"\d+", RegexOptions.Compiled);

		public static IOrderedEnumerable<T> OrderByNatural<T>(this IEnumerable<T> source)
		{
			return OrderByNatural(source, o => o != null ? o.ToString() : String.Empty);
		}

		public static IOrderedEnumerable<T> OrderByNatural<T, TKey>(this IEnumerable<T> source, Func<T, TKey> selector)
		{
			var max = 0;

			var buffer = Ensure(source).Select(
				o =>
				{
					var v = selector(o);
					var s = v != null ? v.ToString() : String.Empty;

					if (!String.IsNullOrWhiteSpace(s))
					{
						var mc = _NaturalOrderExpr.Matches(s);

						if (mc.Count > 0)
						{
							max = Math.Max(max, mc.Cast<Match>().Max(m => m.Value.Length));
						}
					}

					return new
					{
						Key = o,
						Value = s
					};
				});

			return
				buffer.OrderBy(o => _NaturalOrderExpr.Replace(o.Value, m => m.Value.PadLeft(max, '0')))
					  .Select(o => o.Key)
					  .OrderBy(k => 0);
		}

		public static IOrderedEnumerable<T> OrderByDescendingNatural<T>(this IEnumerable<T> source)
		{
			return OrderByDescendingNatural(source, o => o != null ? o.ToString() : String.Empty);
		}

		public static IOrderedEnumerable<T> OrderByDescendingNatural<T, TKey>(
			this IEnumerable<T> source,
			Func<T, TKey> selector)
		{
			var max = 0;

			var buffer = Ensure(source).Select(
				o =>
				{
					var v = selector != null ? selector(o) : default(TKey);
					var s = v != null ? v.ToString() : String.Empty;

					if (!String.IsNullOrWhiteSpace(s))
					{
						var mc = _NaturalOrderExpr.Matches(s);

						if (mc.Count > 0)
						{
							max = Math.Max(max, mc.Cast<Match>().Max(m => m.Value.Length));
						}
					}

					return new
					{
						Key = o,
						Value = s
					};
				});

			return
				buffer.OrderByDescending(o => _NaturalOrderExpr.Replace(o.Value, m => m.Value.PadLeft(max, '0')))
					  .Select(o => o.Key)
					  .OrderByDescending(k => 0);
		}

		public static T Highest<T>(this IEnumerable<T> source)
		{
			return Highest(source, o => o);
		}

		public static T Highest<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
		{
			return Ensure(source).OrderByDescending(keySelector).FirstOrDefault();
		}

		public static T Lowest<T>(this IEnumerable<T> source)
		{
			return Lowest(source, o => o);
		}

		public static T Lowest<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
		{
			return Ensure(source).OrderBy(keySelector).FirstOrDefault();
		}

		public static T ElementAtOrDefault<T>(this T[] source, int index)
		{
			return InBounds(source, index) ? source[index] : default(T);
		}

		public static T ElementAtOrDefault<T>(this List<T> source, int index)
		{
			return InBounds(source, index) ? source[index] : default(T);
		}

		public static T ElementAtOrDefault<T>(this IEnumerable<T> source, int index)
		{
			var i = 0;

			foreach (var o in Ensure(source))
			{
				if (i == index)
				{
					return o;
				}

				if (++i > index)
				{
					break;
				}
			}

			return default(T);
		}

		public static void ForEachRow<T>(this T[,] source, Action<IEnumerable<T>> action)
		{
			ForEachRow(source, (i, o) => action(o));
		}

		public static void ForEachRow<T>(this T[,] source, Action<int, IEnumerable<T>> action)
		{
			if (source == null)
			{
				return;
			}

			var len = source.GetLength(0);
			var sub = source.GetLength(1);

			if (len * sub == 0)
			{
				return;
			}

			for (var i = 0; i < len; i++)
			{
				action(i, Pad(sub, j => source[i, j]));
			}
		}

		public static double Percent<T>(this IEnumerable<T> source, Func<T, bool> predicate)
		{
			if (source == null || predicate == null)
			{
				return 0;
			}

			var min = 0;
			var max = 0;

			foreach (var o in source)
			{
				++max;

				if (predicate(o))
				{
					++min;
				}
			}

			return max == 0 ? 0 : min / (double)max;
		}

		public static IEnumerable<T> Invert<T>(this IEnumerable<T> source, int offset)
		{
			return Ensure(source).Select(
				(o, i) => new
				{
					Index = i < offset ? Int32.MaxValue : i,
					Object = o
				}).OrderByDescending(o => o.Index).Select(o => o.Object);
		}

		public static IEnumerable<T> Invert<T>(this IEnumerable<T> source, int offset, int count)
		{
			return Ensure(source).Select(
				(o, i) => new
				{
					Index = i < offset ? Int32.MaxValue : i >= offset + count ? Int32.MinValue : i,
					Object = o
				}).OrderByDescending(o => o.Index).Select(o => o.Object);
		}

		public static bool ContentsEqual<T>(this IEnumerable<T> source, IEnumerable<T> target)
		{
			return ContentsEqual(source, target, true);
		}

		public static bool ContentsEqual<T>(this IEnumerable<T> source, IEnumerable<T> target, bool ignoreOrder)
		{
			if (source == target)
			{
				return true;
			}

			return GetContentsHashCode(source, ignoreOrder) == GetContentsHashCode(target, ignoreOrder);
		}

		public static int GetContentsHashCode<T>(this IEnumerable<T> source)
		{
			return GetContentsHashCode(source, true);
		}

		public static int GetContentsHashCode<T>(this IEnumerable<T> source, bool ignoreOrder)
		{
			var hashCodes = Ensure(source).Select(o => o == null ? 0 : o.GetHashCode());

			if (ignoreOrder)
			{
				hashCodes = hashCodes.OrderByDescending(h => h);
			}

			return hashCodes.Aggregate(0, (h, c) => unchecked((h * 397) ^ c));
		}

		public static T[] CastToArray<T>(this IEnumerable source)
		{
			return Ensure(source).Cast<T>().ToArray();
		}

		public static List<T> CastToList<T>(this IEnumerable source)
		{
			return Ensure(source).Cast<T>().ToList();
		}

		public static bool IsNullOrEmpty<T>(this T[] source)
		{
			return source == null || source.Length == 0;
		}

		public static bool IsNullOrEmpty<T>(this List<T> source)
		{
			return source == null || source.Count == 0;
		}

		public static bool IsNullOrEmpty<T>(this Stack<T> source)
		{
			return source == null || source.Count == 0;
		}

		public static bool IsNullOrEmpty<T>(this Queue<T> source)
		{
			return source == null || source.Count == 0;
		}

		public static bool IsNullOrEmpty<T>(this HashSet<T> source)
		{
			return source == null || source.Count == 0;
		}

		public static bool IsNullOrEmpty<TKey, TVal>(this IDictionary<TKey, TVal> source)
		{
			return source == null || source.Count == 0;
		}
	}
}