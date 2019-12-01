using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace TSKT
{
    public static class ArrayUtil
    {
        static public void SortKeyValue<K, V>(K[] keys, V[] values)
        {
            var a = new SortedDictionary<K, V>();
            for(int i=0; i<keys.Length; ++i)
            {
                a.Add(keys[i], values[i]);
            }

            var sortedKeys = a.Keys.ToArray();
            for (int i = 0; i < keys.Length; ++i)
            {
                keys[i] = sortedKeys[i];
                values[i] = a[sortedKeys[i]];
            }
        }

        static public T MaxBy<T, S, E>(S list, System.Func<T, E> func)
            where S : IEnumerable<T>
            where E : System.IComparable<E>
        {
            var result = default(T);
            var max = default(E);
            var first = true;
            foreach (var item in list)
            {
                var v = func(item);
                if (first)
                {
                    first = false;
                    max = v;
                    result = item;
                }
                else if (max.CompareTo(v) < 0)
                {
                    max = v;
                    result = item;
                }
            }
            return result;
        }

        static public T MinBy<T, S, E>(S list, System.Func<T, E> func)
            where S : IEnumerable<T>
            where E : System.IComparable<E>
        {
            var result = default(T);
            var min = default(E);
            var first = true;
            foreach (var item in list)
            {
                var v = func(item);
                if (first)
                {
                    first = false;
                    min = v;
                    result = item;
                }
                else if (min.CompareTo(v) > 0)
                {
                    min = v;
                    result = item;
                }
            }
            return result;
        }

        static public T[] UnionSets<T>(T[] leftSet, T[] rightSet)
        {
            if (leftSet == null || leftSet.Length == 0)
            {
                return rightSet ?? System.Array.Empty<T>();
            }
            if (rightSet == null || rightSet.Length == 0)
            {
                return leftSet ?? System.Array.Empty<T>();
            }

            var count = leftSet.Length;
            foreach (var it in rightSet)
            {
                if (System.Array.IndexOf(leftSet, it) < 0)
                {
                    ++count;
                }
            }

            var result = new ArrayBuilder<T>(count);
            foreach (var it in leftSet)
            {
                result.Add(it);
            }
            foreach (var it in rightSet)
            {
                if (System.Array.IndexOf(leftSet, it) < 0)
                {
                    result.Add(it);
                }
            }
            return result.Array;
        }

        public static T[] Concat<T>(T[] a, T[] b)
        {
            if (a == null || a.Length == 0)
            {
                return b ?? System.Array.Empty<T>();
            }
            if (b == null || b.Length == 0)
            {
                return a ?? System.Array.Empty<T>();
            }

            var builder = new ArrayBuilder<T>(a.Length + b.Length);
            foreach(var it in a)
            {
                builder.Add(it);
            }
            foreach (var it in b)
            {
                builder.Add(it);
            }
            return builder.Array;
        }
    }
}
