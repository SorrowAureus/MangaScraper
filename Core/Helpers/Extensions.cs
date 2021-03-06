﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MangaScraper.Core.Helpers {
    public static class Extensions {
        public static ConcurrentDictionary<TKey, TValue> ToConcurrentDictionary<TKey, TValue>(this IEnumerable<TValue> source, Func<TValue, TKey> f) =>
            new ConcurrentDictionary<TKey, TValue>(source.Select(i => new KeyValuePair<TKey, TValue>(f(i), i)));

        public static async Task Transform<TGrouping>(this IEnumerable<IGrouping<int, TGrouping>> source,
                                                          Func<TGrouping, Task> action,
                                                          IProgress<double> progress = null) {
            var percent = 100.0 / source.Count();
            foreach (var group in source) {
                await Task.WhenAll(@group.Select(action));
                progress?.Report(percent * (@group.Key + 1) / 100.0);
                await Task.Delay(100);
            }
        }

        public static async Task<IEnumerable<T>> Transform<T, TGrouping>(this IEnumerable<IGrouping<int, TGrouping>> source,
                                                              Func<TGrouping, Task<IEnumerable<T>>> action,
                                                              IProgress<double> progress = null) {
            var list = new List<T>();
            var percent = 100.0 / source.Count();
            foreach (var group in source) {
                var e = await Task.WhenAll(@group.Select(action));
                list.AddRange(e.SelectMany(x => x));
                progress?.Report(percent * (@group.Key + 1) / 100.0);
                await Task.Delay(10);
            }

            return list;
        }

        public static Task<IEnumerable<T>> Transform<T, TEnum>(this IEnumerable<IGrouping<int, TEnum>> source,
                                              Func<TEnum, Task<T>> action,
                                              IProgress<double> progress = null,
                                              int delay = 100) =>
            Transform(source, action, CancellationToken.None, progress, delay);

        public static async Task<IEnumerable<T>> Transform<T, TEnum>(this IEnumerable<IGrouping<int, TEnum>> source,
                                                      Func<TEnum, Task<T>> action,
                                                      CancellationToken token,
                                                      IProgress<double> progress = null,
                                                      int delay = 100) {
            var list = new List<T>();
            var percent = 1.0 / source.Count();
            foreach (var group in source) {
                token.ThrowIfCancellationRequested();
                var e = await Task.WhenAll(@group.Select(action));
                list.AddRange(e);
                progress?.Report(percent * (@group.Key + 1));
                await Task.Delay(delay, token);
            }

            return list;
        }

        public static IEnumerable<IGrouping<int, T>> Batch<T>(this IEnumerable<T> source, int size) =>
            source.Select((t, i) => (index: i / size, item: t)).GroupBy(t => t.index, t => t.item);

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> a) {
            foreach (var x1 in source) a(x1);
        }

        public static void ForEach<T, X>(this IEnumerable<T> source, Func<T, X> a) {
            foreach (var x1 in source) a(x1);
        }

        public static async Task<IEnumerable<T>> SelectMany<X, T>(this IEnumerable<X> source, Func<X, Task<IEnumerable<T>>> func) {
            var all = await Task.WhenAll(source.Select(x => func(x)));
            return all.SelectMany(t => t);
        }

        public static async Task<IEnumerable<T>> SelectMany<X, T>(this IEnumerable<X> source, Func<X, Task<List<T>>> func) {
            var all = await Task.WhenAll(source.Select(x => func(x)));
            return all.SelectMany(t => t);
        }

        public static async Task<List<T>> ToListAsync<T>(this Task<IEnumerable<T>> source) {
            var result = await source.ConfigureAwait(false);
            return result is List<T> l ? l : result.ToList();
        }

        public static async Task<T[]> ToArrayAsync<T>(this Task<IEnumerable<T>> source) =>
          (await source.ConfigureAwait(false)).ToArray();

        public static IEnumerable<Task<T2>> AndThen<T, T2>(this IEnumerable<Task<T>> source, Func<T, Task<T2>> continuation) => source.Select(t => t.ContinueWith(tt => TryDo(tt, continuation)).Unwrap());

        public static IEnumerable<Task<T2>> AndThen<T, T2>(this IEnumerable<Task<T>> source, Func<T, T2> continuation) => source.Select(t => t.ContinueWith(tt => TryDo(tt, continuation)));

        public static Task<IEnumerable<T2>> Select<T, T2>(this Task<List<T>> source, Func<T, T2> continuation) => source.ContinueWith(t => TryDo(t, x => x.Select(continuation)));

        public static Task<IEnumerable<T2>> Select<T, T2>(this Task<T[]> source, Func<T, T2> continuation) => source.ContinueWith(t => TryDo(t, x => x.Select(continuation)));

        public static async Task<T2[]> ContinueWith<T, T2>(this IEnumerable<Task<T>> source, Func<T, Task<T2>> continuation) => await Task.WhenAll(source.Select(t => t.ContinueWith(tt => TryDo(tt, continuation)).Unwrap())).ConfigureAwait(false);

        /// <summary>
        /// Adds the continuation to each task in the source, and merges them to a single task, which completes when all tasks and continutations are done
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="source"></param>
        /// <param name="continuation"></param>
        /// <returns></returns>
        public static async Task<T2[]> ContinueWith<T, T2>(this IEnumerable<Task<T>> source, Func<T, T2> continuation) {
            var asdf = source.Select(t => t.ContinueWith(tt => TryDo(tt, continuation)));
            return await Task.WhenAll(asdf).ConfigureAwait(false);
        }

        public static async Task ContinueWith(this IEnumerable<Task> source, Action continuation) => await Task.WhenAll(source.Select(t => t.ContinueWith(tt => {
            if (tt.IsFaulted && tt.Exception != null)
                throw tt.Exception;
            continuation();
        }))).ConfigureAwait(false);

        private static T2 TryDo<T1, T2>(Task<T1> tt, Func<T1, T2> action) =>
      tt.IsFaulted && tt.Exception != null
      ? throw tt.Exception
      : action(tt.Result);

        /// <summary>
        /// Flattens collections inside a task
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<T>> Flatten<T>(this Task<IEnumerable<T>[]> task) {
            var result = await task.ConfigureAwait(false);
            return result.SelectMany(e => e);
        }

        public static Task WhenAll(this IEnumerable<Task> values) => Task.WhenAll(values);

        public static async Task WhenAll(this IEnumerable<Task> values, IProgress<double> progress) {
            var arr = values.ToArray();
            var tempCount = 0;
            void Update() {
                progress?.Report(Interlocked.Increment(ref tempCount) / (double)arr.Length);
            }
            await arr.ContinueWith(Update).ConfigureAwait(false);
        }
        public static Task<T[]> WhenAll<T>(this IEnumerable<Task<T>> values) => Task.WhenAll(values);

        public static async Task<T[]> WhenAll<T>(this IEnumerable<Task<T>> values, IProgress<double> progress) {
            var arr = values.ToArray();
            var tempCount = 0;
            T Update(T value) {
                progress?.Report(Interlocked.Increment(ref tempCount) / (double)arr.Length);
                return value;
            }
            return await arr.ContinueWith(Update).ConfigureAwait(false);
        }
    }
}