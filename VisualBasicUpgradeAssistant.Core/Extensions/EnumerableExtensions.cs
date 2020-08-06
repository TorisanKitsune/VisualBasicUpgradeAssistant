using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace VisualBasicUpgradeAssistant.Core.Extensions
{
    public static class EnumerableExtensions
    {
        private static readonly Random Rnd = RandomUtility.GetUniqueRandom();

        public static String Aggregate(this IEnumerable<String> enumeration, String separator)
        {
            if (enumeration == null)
            {
                throw new ArgumentNullException("enumeration");
            }
            if (separator == null)
            {
                throw new ArgumentNullException("separator");
            }
            String text = String.Join(separator, enumeration.ToArray());
            if (text.Length > separator.Length)
            {
                return text.Substring(separator.Length);
            }
            return text;
        }

        public static String Aggregate<T>(this IEnumerable<T> enumeration, Func<T, String> toString, String separator)
        {
            if (toString == null)
            {
                throw new ArgumentNullException("toString");
            }
            return Aggregate(Select(enumeration, toString), separator);
        }

        public static Boolean AnyOrNotNull(this IEnumerable<String> source)
        {
            Boolean flag = source.Aggregate((String a, String b) => a + b).Any();
            if ((source?.Any() ?? false) && flag)
            {
                return true;
            }
            return false;
        }

        public static IEnumerable<T> Append<T>(this IEnumerable<T> source, T item)
        {
            foreach (T item2 in source)
            {
                yield return item2;
            }
            yield return item;
        }

        public static Boolean AreDistinct<T>(this IEnumerable<T> items)
        {
            return items.Count() == items.Distinct().Count();
        }

        public static IEnumerable<T?> AsNullable<T>(this IEnumerable<T> enumeration) where T : struct
        {
            return Select(enumeration, (Func<T, T?>)((T item) => item), allowNull: true);
        }

        public static ReadOnlyCollection<T> AsReadOnlyCollection<T>(this IEnumerable<T> @this)
        {
            if (@this != null)
            {
                return new ReadOnlyCollection<T>(new List<T>(@this));
            }
            throw new ArgumentNullException("this");
        }

        public static T At<T>(this IEnumerable<T> enumeration, Int32 index)
        {
            if (enumeration == null)
            {
                throw new ArgumentNullException("enumeration");
            }
            return enumeration.Skip(index).First();
        }

        public static IEnumerable<T> At<T>(this IEnumerable<T> enumeration, params Int32[] indices)
        {
            return At(enumeration, (IEnumerable<Int32>)indices);
        }

        public static IEnumerable<T> At<T>(this IEnumerable<T> enumeration, IEnumerable<Int32> indices)
        {
            if (enumeration == null)
            {
                throw new ArgumentNullException("enumeration");
            }
            if (indices == null)
            {
                throw new ArgumentNullException("indices");
            }
            Int32 currentIndex = 0;
            foreach (Int32 item in indices.OrderBy((Int32 i) => i))
            {
                for (; currentIndex != item; currentIndex++)
                {
                    enumeration = enumeration.Skip(1);
                }
                yield return enumeration.First();
            }
        }

        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> enumerable, Int32 chunks)
        {
            Int32 num = enumerable.Count();
            Int32 ceiling = (Int32)Math.Ceiling((Double)num / (Double)chunks);
            return Select(from x in enumerable.Select((T x, Int32 i) => new
            {
                value = x,
                index = i
            })
                          group x by x.index / ceiling, x => Select(x, z => z.value));
        }

        public static IEnumerable<IEnumerable<T>> Combinations<T>(this IEnumerable<T> source, Int32 select, Boolean repetition = false)
        {
            if (source == null || select < 0)
            {
                throw new ArgumentNullException();
            }
            if (select != 0)
            {
                return source.SelectMany((T element, Int32 index) => Select(Combinations(source.Skip(repetition ? index : (index + 1)), select - 1, repetition), (IEnumerable<T> c) => new T[1]
                {
                element
                }.Concat(c)));
            }
            return new T[1][]
            {
            new T[0]
            };
        }

        public static String Concatenate(this IEnumerable<String> @this)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (String item in @this)
            {
                stringBuilder.Append(item);
            }
            return stringBuilder.ToString();
        }

        public static String Concatenate<T>(this IEnumerable<T> source, Func<T, String> func)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (T item in source)
            {
                stringBuilder.Append(func(item));
            }
            return stringBuilder.ToString();
        }

        public static String ConcatWith<T>(this IEnumerable<T> items, String separator = ",", String formatString = "")
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }
            if (separator == null)
            {
                throw new ArgumentNullException("separator");
            }
            if (typeof(T) == typeof(String))
            {
                return String.Join(separator, ((IEnumerable<String>)items).ToArray());
            }
            if (String.IsNullOrEmpty(formatString))
            {
                formatString = "{0}";
            }
            else
            {
                formatString = $"{{0:{formatString}}}";
            }
            return String.Join(separator, Select(items, (T x) => String.Format(formatString, x)).ToArray());
        }

        public static Boolean Contains<T>(this IEnumerable<T> source, Func<T, Boolean> selector)
        {
            foreach (T item in source)
            {
                if (selector(item))
                {
                    return true;
                }
            }
            return false;
        }

        public static Boolean ContainsAll<T>(this IEnumerable<T> @this, params T[] values)
        {
            T[] source = @this.ToArray();
            foreach (T value in values)
            {
                if (!source.Contains(value))
                {
                    return false;
                }
            }
            return true;
        }

        public static Boolean ContainsAny<T>(this IEnumerable<T> @this, params T[] values)
        {
            T[] source = @this.ToArray();
            foreach (T value in values)
            {
                if (source.Contains(value))
                {
                    return true;
                }
            }
            return false;
        }

        public static Boolean ContainsAtLeast<T>(this IEnumerable<T> enumeration, Int32 count)
        {
            if (enumeration == null)
            {
                throw new ArgumentNullException("enumeration");
            }
            return Select(enumeration.Take(count), (T t) => t).Count() <= count;
        }

        public static Boolean ContainsAtMost<T>(this IEnumerable<T> enumeration, Int32 count)
        {
            if (enumeration == null)
            {
                throw new ArgumentNullException("enumeration");
            }
            return Select(enumeration.Take(count), (T t) => t).Count() >= count;
        }

        public static IEnumerable<T> Cycle<T>(this IEnumerable<T> source)
        {
            while (true)
            {
                foreach (T item in source)
                {
                    yield return item;
                }
            }
        }

        public static void Delete(this IEnumerable<DirectoryInfo> @this)
        {
            foreach (DirectoryInfo item in @this)
            {
                item.Delete();
            }
        }

        public static void Delete(this IEnumerable<FileInfo> @this)
        {
            foreach (FileInfo item in @this)
            {
                item.Delete();
            }
        }

        public static IEnumerable<T> Distinct<T>(this IEnumerable<T> source, Func<T, T, Boolean> equalityComparer)
        {
            Int32 sourceCount = source.Count();
            for (Int32 i = 0; i < sourceCount; i++)
            {
                Boolean flag = false;
                for (Int32 j = 0; j < i; j++)
                {
                    if (equalityComparer(source.ElementAt(i), source.ElementAt(j)))
                    {
                        flag = true;
                        break;
                    }
                }
                if (!flag)
                {
                    yield return source.ElementAt(i);
                }
            }
        }

        public static IEnumerable<TKey> Distinct<T, TKey>(this IEnumerable<T> source, Func<T, TKey> selector)
        {
            return Select(source.GroupBy(selector), (IGrouping<TKey, T> x) => x.Key);
        }

        public static IEnumerable<T> DistinctBy<T>(this IEnumerable<T> list, Func<T, Object> propertySelector)
        {
            return Select(list.GroupBy(propertySelector), (IGrouping<Object, T> x) => x.First());
        }

        private static IEnumerable<T> ElementsNotNullFrom<T>(IEnumerable<T> source)
        {
            return source.Where((T x) => x != null);
        }

        public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> items)
        {
            return items ?? Enumerable.Empty<T>();
        }

        public static IEnumerable<String> EnumNamesToList<T>(this IEnumerable<T> collection)
        {
            return Select(from objType in typeof(T).GetInterfaces()
                          where objType.IsEnum
                          select objType, (Type objType) => objType.Name).ToList();
        }

        public static IEnumerable<T> EnumValuesToList<T>(this IEnumerable<T> collection)
        {
            Type enumType = typeof(T);
            if (enumType.BaseType != typeof(Enum))
            {
                throw new ArgumentException("T must be of type System.Enum");
            }
            Array values = Enum.GetValues(enumType);
            List<T> list = new List<T>(values.Length);
            list.AddRange(Select(values.Cast<Int32>(), (Int32 val) => (T)Enum.Parse(enumType, val.ToString())));
            return list;
        }

        public static Boolean Exists<T>(this IEnumerable<T> list, Func<T, Boolean> predicate)
        {
            return Index(list, predicate) > -1;
        }

        public static List<T> FindAll<T>(this IEnumerable<T> list, Func<T, Boolean> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            List<T> list2 = new List<T>();
            IEnumerator<T> enumerator = list.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (predicate(enumerator.Current))
                {
                    list2.Add(enumerator.Current);
                }
            }
            return list2;
        }

        public static T FirstOr<T>(this IEnumerable<T> @this, Func<T, Boolean> predicate, Func<T> onOr)
        {
            T result = @this.FirstOrDefault(predicate);
            if (result.Equals(default(T)))
            {
                result = onOr();
            }
            return result;
        }

        public static T FirstOrDefault<T>(this IEnumerable<T> source, T defaultValue)
        {
            if (!IsNotEmpty(source))
            {
                return defaultValue;
            }
            return source.First();
        }

        public static void ForEach<T>(this IEnumerable<T> items, Action<Int32, T> action)
        {
            if (items == null)
            {
                return;
            }
            Int32 num = 0;
            foreach (T item in items)
            {
                action(num++, item);
            }
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T item in source)
            {
                action(item);
            }
        }

        public static void ForEach(this IEnumerable<DirectoryInfo> @this, Action<DirectoryInfo> action)
        {
            foreach (DirectoryInfo item in @this)
            {
                action(item);
            }
        }

        public static void ForEach<T>(this IEnumerable<T> @this, Action<T, Int32> action)
        {
            T[] array = @this.ToArray();
            for (Int32 i = 0; i < array.Length; i++)
            {
                action(array[i], i);
            }
        }

        public static void ForEachReverse<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T item in source.Reverse())
            {
                action(item);
            }
        }

        public static void ForEachReverse<T>(this IEnumerable<T> @this, Action<T, Int32> action)
        {
            T[] array = @this.ToArray();
            for (Int32 num = array.Length - 1; num >= 0; num--)
            {
                action(array[num], num);
            }
        }

        public static IEnumerable<T> GetDuplicateItems<T>(this IEnumerable<T> list)
        {
            return Select(from x in list
                          group x by x into @group
                          where @group.Count() > 1
                          select @group, (IGrouping<T, T> group) => group.Key);
        }

        public static IEnumerable<T[]> GroupEvery<T>(this IEnumerable<T> enumeration, Int32 count)
        {
            if (enumeration == null)
            {
                throw new ArgumentNullException("enumeration");
            }
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            Int32 num = 0;
            T[] array = new T[count];
            foreach (T item in enumeration)
            {
                array[num++] = item;
                if (num == count)
                {
                    yield return array;
                    num = 0;
                    array = new T[count];
                }
            }
            if (num != 0)
            {
                yield return array;
            }
        }

        public static Boolean HasCountOf<T>(this IEnumerable<T> source, Int32 count)
        {
            return source.Take(count + 1).Count() == count;
        }

        public static IEnumerable<T> IgnoreNulls<T>(this IEnumerable<T> target)
        {
            if (target == null)
            {
                yield break;
            }
            foreach (T item in target.Where((T item) => item != null))
            {
                yield return item;
            }
        }

        public static Int32 Index<T>(this IEnumerable<T> list, Func<T, Boolean> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            IEnumerator<T> enumerator = list.GetEnumerator();
            Int32 num = 0;
            while (enumerator.MoveNext())
            {
                if (predicate(enumerator.Current))
                {
                    return num;
                }
                num++;
            }
            return -1;
        }

        public static Int32 IndexOf<T>(this IEnumerable<T> items, T item, IEqualityComparer<T> comparer)
        {
            return IndexOf(items, item, comparer.Equals);
        }

        public static Int32 IndexOf<T>(this IEnumerable<T> items, T item, Func<T, T, Boolean> predicate)
        {
            Int32 num = 0;
            foreach (T item2 in items)
            {
                if (predicate(item, item2))
                {
                    return num;
                }
                num++;
            }
            return -1;
        }

        public static IEnumerable<Int32> IndicesWhere<T>(this IEnumerable<T> enumeration, Func<T, Boolean> predicate)
        {
            if (enumeration == null)
            {
                throw new ArgumentNullException("enumeration");
            }
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            Int32 i = 0;
            foreach (T item in enumeration)
            {
                if (predicate(item))
                {
                    yield return i;
                }
                i++;
            }
        }

        public static Boolean IsEmpty<T>(this IEnumerable<T> @this)
        {
            return !@this.Any();
        }

        public static Boolean IsNotEmpty<T>(this IEnumerable<T> @this)
        {
            return @this.Any();
        }

        public static Boolean IsNotNullOrEmpty<T>(this IEnumerable<T> @this)
        {
            return @this?.Any() ?? false;
        }

        public static Boolean IsNullOrEmpty(this IEnumerable sequence)
        {
            if (sequence != null)
            {
                return !sequence.Cast<Object>().Any();
            }
            return true;
        }

        public static Boolean IsNullOrEmpty<T>(this IEnumerable<T> @this)
        {
            if (@this != null)
            {
                return !@this.Any();
            }
            return true;
        }

        public static Boolean IsSingle<T>(this IEnumerable<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            using (IEnumerator<T> enumerator = source.GetEnumerator())
            {
                return enumerator.MoveNext() && !enumerator.MoveNext();
            }
        }

        public static String Join<T>(this IEnumerable<T> collection, Func<T, String> func, String separator)
        {
            return String.Join(separator, Select(collection, func).ToArray());
        }

        public static Boolean Many<T>(this IEnumerable<T> source)
        {
            return source.Count() > 1;
        }

        public static Boolean Many<T>(this IEnumerable<T> source, Func<T, Boolean> query)
        {
            return source.Count(query) > 1;
        }

        public static TItem MaxItem<TItem, TValue>(this IEnumerable<TItem> items, Func<TItem, TValue> selector, out TValue maxValue) where TItem : class where TValue : IComparable
        {
            TItem val = null;
            maxValue = default(TValue);
            foreach (TItem item in items)
            {
                if (item != null)
                {
                    TValue val2 = selector(item);
                    if (val == null || val2.CompareTo(maxValue) > 0)
                    {
                        maxValue = val2;
                        val = item;
                    }
                }
            }
            return val;
        }

        public static TItem MaxItem<TItem, TValue>(this IEnumerable<TItem> items, Func<TItem, TValue> selector) where TItem : class where TValue : IComparable
        {
            return MaxItem(items, selector, out TValue maxValue);
        }

        public static IEnumerable<T> MergeDistinctInnerEnumerable<T>(this IEnumerable<IEnumerable<T>> @this)
        {
            List<IEnumerable<T>> list = @this.ToList();
            List<T> list2 = new List<T>();
            foreach (IEnumerable<T> item in list)
            {
                list2 = list2.Union(item).ToList();
            }
            return list2;
        }

        public static IEnumerable<T> MergeInnerEnumerable<T>(this IEnumerable<IEnumerable<T>> @this)
        {
            List<IEnumerable<T>> list = @this.ToList();
            List<T> list2 = new List<T>();
            foreach (IEnumerable<T> item in list)
            {
                list2.AddRange(item);
            }
            return list2;
        }

        public static TItem MinItem<TItem, TValue>(this IEnumerable<TItem> items, Func<TItem, TValue> selector, out TValue minValue) where TItem : class where TValue : IComparable
        {
            TItem val = null;
            minValue = default(TValue);
            foreach (TItem item in items)
            {
                if (item != null)
                {
                    TValue val2 = selector(item);
                    if (val == null || val2.CompareTo(minValue) < 0)
                    {
                        minValue = val2;
                        val = item;
                    }
                }
            }
            return val;
        }

        public static TItem MinItem<TItem, TValue>(this IEnumerable<TItem> items, Func<TItem, TValue> selector) where TItem : class where TValue : IComparable
        {
            return MinItem(items, selector, out TValue minValue);
        }

        public static Boolean None<T>(this IEnumerable<T> source)
        {
            return !source.Any();
        }

        public static Boolean None<T>(this IEnumerable<T> source, Func<T, Boolean> query)
        {
            return !source.Any(query);
        }

        public static Boolean OneOf<T>(this IEnumerable<T> source)
        {
            return source.Count() == 1;
        }

        public static Boolean OneOf<T>(this IEnumerable<T> source, Func<T, Boolean> query)
        {
            return source.Count(query) == 1;
        }

        public static Boolean OnlyOne<T>(this IEnumerable<T> source, Func<T, Boolean> condition = null)
        {
            return source.Count(condition ?? ((Func<T, Boolean>)((T t) => true))) == 1;
        }

        public static String PathCombine(this IEnumerable<String> @this)
        {
            return Path.Combine(@this.ToArray());
        }

        public static IEnumerable<T> RandomSubset<T>(this IEnumerable<T> sequence, Int32 subsetSize)
        {
            return RandomSubset(sequence, subsetSize, new Random());
        }

        public static IEnumerable<T> RandomSubset<T>(this IEnumerable<T> sequence, Int32 subsetSize, Random rand)
        {
            if (rand == null)
            {
                throw new ArgumentNullException("rand");
            }
            if (sequence == null)
            {
                throw new ArgumentNullException("sequence");
            }
            if (subsetSize < 0)
            {
                throw new ArgumentOutOfRangeException("subsetSize");
            }
            return RandomSubsetImpl(sequence, subsetSize, rand);
        }

        private static IEnumerable<T> RandomSubsetImpl<T>(IEnumerable<T> sequence, Int32 subsetSize, Random rand)
        {
            T[] seqArray = sequence.ToArray();
            if (seqArray.Length < subsetSize)
            {
                throw new ArgumentOutOfRangeException("subsetSize", "Subset size must be <= sequence.Count()");
            }
            Int32 num = 0;
            Int32 num2 = seqArray.Length;
            Int32 num3 = num2 - 1;
            while (num < subsetSize)
            {
                Int32 num4 = num3 - rand.Next(num2);
                T val = seqArray[num4];
                seqArray[num4] = seqArray[num];
                seqArray[num] = val;
                num++;
                num2--;
            }
            for (Int32 i = 0; i < subsetSize; i++)
            {
                yield return seqArray[i];
            }
        }

        public static IEnumerable<String> RemoveEmptyElements(this IEnumerable<String> strings)
        {
            foreach (String @string in strings)
            {
                if (!String.IsNullOrEmpty(@string))
                {
                    yield return @string;
                }
            }
        }

        public static IEnumerable<T> RemoveWhere<T>(this IEnumerable<T> source, Predicate<T> predicate)
        {
            if (source == null)
            {
                yield break;
            }
            foreach (T item in source)
            {
                if (!predicate(item))
                {
                    yield return item;
                }
            }
        }

        public static IEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector, Boolean allowNull = true)
        {
            foreach (TSource item in source)
            {
                TResult val = selector(item);
                if (allowNull || !Object.Equals(val, default(TSource)))
                {
                    yield return val;
                }
            }
        }

        public static IEnumerable<T> SelectMany<T>(this IEnumerable<IEnumerable<T>> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            foreach (IEnumerable<T> item in source)
            {
                foreach (T item2 in item)
                {
                    yield return item2;
                }
            }
        }

        public static IEnumerable<T> SelectMany<T>(this IEnumerable<T[]> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            foreach (T[] item in source)
            {
                T[] array = item;
                for (Int32 i = 0; i < array.Length; i++)
                {
                    yield return array[i];
                }
            }
        }

        public static IEnumerable<T> SelectManyAllInclusive<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> selector)
        {
            return source.Concat(SelectManyRecursive(source, selector));
        }

        public static IEnumerable<T> SelectManyRecursive<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> selector)
        {
            List<T> list = source.SelectMany(selector).ToList();
            if (list.Count == 0)
            {
                return list;
            }
            return list.Concat(SelectManyRecursive(list, selector));
        }

        public static Boolean SequenceEqual<T1, T2>(this IEnumerable<T1> left, IEnumerable<T2> right, Func<T1, T2, Boolean> comparer)
        {
            using (IEnumerator<T1> enumerator = left.GetEnumerator())
            {
                using (IEnumerator<T2> enumerator2 = right.GetEnumerator())
                {
                    Boolean flag = enumerator.MoveNext();
                    Boolean flag2 = enumerator2.MoveNext();
                    while (flag && flag2)
                    {
                        if (!comparer(enumerator.Current, enumerator2.Current))
                        {
                            return false;
                        }
                        flag = enumerator.MoveNext();
                        flag2 = enumerator2.MoveNext();
                    }
                    if (flag || flag2)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public static Boolean SequenceSuperset<T>(this IEnumerable<T> enumeration, IEnumerable<T> subset)
        {
            return SequenceSuperset(enumeration, subset, EqualityComparer<T>.Default.Equals);
        }

        public static Boolean SequenceSuperset<T>(this IEnumerable<T> enumeration, IEnumerable<T> subset, Func<T, T, Boolean> equalityComparer)
        {
            if (enumeration == null)
            {
                throw new ArgumentNullException("enumeration");
            }
            if (subset == null)
            {
                throw new ArgumentNullException("subset");
            }
            if (equalityComparer == null)
            {
                throw new ArgumentNullException("comparer");
            }
            using (IEnumerator<T> enumerator = enumeration.GetEnumerator())
            {
                using (IEnumerator<T> enumerator2 = subset.GetEnumerator())
                {
                    enumerator.Reset();
                    enumerator2.Reset();
                    while (enumerator.MoveNext())
                    {
                        if (!enumerator2.MoveNext())
                        {
                            return true;
                        }
                        if (!equalityComparer(enumerator.Current, enumerator2.Current))
                        {
                            enumerator2.Reset();
                            enumerator2.MoveNext();
                            if (!equalityComparer(enumerator.Current, enumerator2.Current))
                            {
                                enumerator2.Reset();
                            }
                        }
                    }
                    if (!enumerator2.MoveNext())
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static Boolean SetEqual<T>(this IEnumerable<T> source, IEnumerable<T> toCompareWith)
        {
            if (source == null || toCompareWith == null)
            {
                return false;
            }
            return SetEqual(source, toCompareWith, null);
        }

        public static Boolean SetEqual<T>(this IEnumerable<T> source, IEnumerable<T> toCompareWith, IEqualityComparer<T> comparer)
        {
            if (source == null || toCompareWith == null)
            {
                return false;
            }
            Int32 num = source.Count();
            Int32 num2 = toCompareWith.Count();
            if (num != num2)
            {
                return false;
            }
            if (num == 0)
            {
                return true;
            }
            IEqualityComparer<T> comparer2 = comparer ?? EqualityComparer<T>.Default;
            return source.Intersect(toCompareWith, comparer2).Count() == num;
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }
            return ShuffleIterator(items);
        }

        private static IEnumerable<T> ShuffleIterator<T>(this IEnumerable<T> items)
        {
            List<T> buffer = items.ToList();
            for (Int32 i = 0; i < buffer.Count; i++)
            {
                Int32 j = Rnd.Next(i, buffer.Count);
                yield return buffer[j];
                buffer[j] = buffer[i];
            }
        }

        public static IEnumerable<T> Slice<T>(this IEnumerable<T> items, Int32 start, Int32 end)
        {
            Int32 index = 0;
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }
            Int32 num = (items is ICollection<T>) ? ((ICollection<T>)items).Count : ((!(items is ICollection)) ? items.Count() : ((ICollection)items).Count);
            if (start < 0)
            {
                start += num;
            }
            if (end < 0)
            {
                end += num;
            }
            foreach (T item in items)
            {
                if (index < end)
                {
                    if (index >= start)
                    {
                        yield return item;
                    }
                    Int32 num2 = index + 1;
                    index = num2;
                    continue;
                }
                yield break;
            }
        }

        public static String StringJoin<T>(this IEnumerable<T> @this, String separator)
        {
            return String.Join(separator, @this);
        }

        public static String StringJoin<T>(this IEnumerable<T> @this, Char separator)
        {
            return String.Join(separator.ToString(), @this);
        }

        public static UInt32 Sum(this IEnumerable<UInt32> source)
        {
            return source.Aggregate(0u, (UInt32 current, UInt32 number) => current + number);
        }

        public static UInt64 Sum(this IEnumerable<UInt64> source)
        {
            return source.Aggregate(0uL, (UInt64 current, UInt64 number) => current + number);
        }

        public static UInt32? Sum(this IEnumerable<UInt32?> source)
        {
            return source.Where((UInt32? nullable) => nullable.HasValue).Aggregate(0u, (UInt32 current, UInt32? nullable) => current + nullable.GetValueOrDefault());
        }

        public static UInt64? Sum(this IEnumerable<UInt64?> source)
        {
            return source.Where((UInt64? nullable) => nullable.HasValue).Aggregate(0uL, (UInt64 current, UInt64? nullable) => current + nullable.GetValueOrDefault());
        }

        public static UInt32 Sum<T>(this IEnumerable<T> source, Func<T, UInt32> selection)
        {
            return Sum(Select(ElementsNotNullFrom(source), selection));
        }

        public static UInt32? Sum<T>(this IEnumerable<T> source, Func<T, UInt32?> selection)
        {
            return Sum(Select(ElementsNotNullFrom(source), selection));
        }

        public static UInt64 Sum<T>(this IEnumerable<T> source, Func<T, UInt64> selector)
        {
            return Sum(Select(ElementsNotNullFrom(source), selector));
        }

        public static UInt64? Sum<T>(this IEnumerable<T> source, Func<T, UInt64?> selector)
        {
            return Sum(Select(ElementsNotNullFrom(source), selector));
        }

        public static IEnumerable<T> TakeEvery<T>(this IEnumerable<T> enumeration, Int32 startAt, Int32 hopLength)
        {
            if (enumeration == null)
            {
                throw new ArgumentNullException("enumeration");
            }
            Int32 first = 0;
            Int32 count = 0;
            foreach (T item in enumeration)
            {
                if (first < startAt)
                {
                    first++;
                    continue;
                }
                if (first == startAt)
                {
                    yield return item;
                    first++;
                    continue;
                }
                count++;
                if (count == hopLength)
                {
                    yield return item;
                    count = 0;
                }
            }
        }

        public static IEnumerable<T> TakeUntil<T>(this IEnumerable<T> collection, Predicate<T> endCondition)
        {
            return collection.TakeWhile((T item) => !endCondition(item));
        }

        public static TResult[] ToArray<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            return Select(source, selector).ToArray();
        }

        public static Collection<T> ToCollection<T>(this IEnumerable<T> enumerable)
        {
            Collection<T> collection = new Collection<T>();
            foreach (T item in enumerable)
            {
                collection.Add(item);
            }
            return collection;
        }

        public static DataTable ToDataTable<T>(this IEnumerable<T> varlist)
        {
            DataTable dataTable = new DataTable();
            PropertyInfo[] array = null;
            if (varlist == null)
            {
                return dataTable;
            }
            foreach (T item in varlist)
            {
                PropertyInfo[] array2;
                if (array == null)
                {
                    array = item.GetType().GetProperties();
                    array2 = array;
                    foreach (PropertyInfo propertyInfo in array2)
                    {
                        Type type = propertyInfo.PropertyType;
                        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            type = type.GetGenericArguments()[0];
                        }
                        dataTable.Columns.Add(new DataColumn(propertyInfo.Name, type));
                    }
                }
                DataRow dataRow = dataTable.NewRow();
                array2 = array;
                foreach (PropertyInfo propertyInfo2 in array2)
                {
                    dataRow[propertyInfo2.Name] = (propertyInfo2.GetValue(item, null) ?? DBNull.Value);
                }
                dataTable.Rows.Add(dataRow);
            }
            return dataTable;
        }

        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> enumeration)
        {
            if (enumeration == null)
            {
                throw new ArgumentNullException("enumeration");
            }
            return enumeration.ToDictionary((KeyValuePair<TKey, TValue> item) => item.Key, (KeyValuePair<TKey, TValue> item) => item.Value);
        }

        public static Dictionary<TKey, IEnumerable<TElement>> ToDictionary<TKey, TElement>(this IEnumerable<IGrouping<TKey, TElement>> enumeration)
        {
            if (enumeration == null)
            {
                throw new ArgumentNullException("enumeration");
            }
            return enumeration.ToDictionary((IGrouping<TKey, TElement> item) => item.Key, (IGrouping<TKey, TElement> item) => item.Cast<TElement>());
        }

        public static HashSet<TDestination> ToHashSet<TDestination>(this IEnumerable<TDestination> source)
        {
            return new HashSet<TDestination>(source);
        }

        public static List<TResult> ToList<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            return Select(source, selector).ToList();
        }

        public static String ToString(this IEnumerable<String> strs)
        {
            return ToStringBuilder(strs).ToString();
        }

        public static StringBuilder ToStringBuilder(this IEnumerable<String> strs)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (String str in strs)
            {
                stringBuilder.AppendLine(str);
            }
            return stringBuilder;
        }

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> source) where T : class
        {
            return source.Where((T x) => x != null);
        }

        public static Boolean XOf<T>(this IEnumerable<T> source, Int32 count)
        {
            return source.Count() == count;
        }

        public static Boolean XOf<T>(this IEnumerable<T> source, Func<T, Boolean> query, Int32 count)
        {
            return source.Count(query) == count;
        }
    }
}
