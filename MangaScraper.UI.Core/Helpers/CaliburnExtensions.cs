﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Caliburn.Micro;
using Reactive.Bindings;

namespace MangaScraper.UI.Core.Helpers {
  public static class CaliburnExtensions {
    public static IObservableCollection<T> ToReactiveCollection<T>(this IObservable<List<T>> source) =>
        new ReactiveCollection<T>(source);


    public static IObservable<B> SelectTask<A, B>(this IObservable<A> source, Func<A, Task<B>> transform, IScheduler s) =>
    source.Select(x => Observable.FromAsync(_ => transform(x), s)).SelectMany(x => x);

    public static IObservable<B> SelectTask<A, B>(this IObservable<A> source, Func<A, Task<B>> transform) =>
        source.Select(x => Observable.FromAsync(_ => transform(x))).SelectMany(x => x);

    public static ReactiveProperty<T> AsReactiveProperty<T>(this T t) => new ReactiveProperty<T>(t);


    public static BindableCollection<T> ToBindableCollection<T>(this IEnumerable<T> source) {
      if (source == null)
        throw new ArgumentException(nameof(source));

      return new BindableCollection<T>(source);
    }

    public static void RemoveWhere<T>(this IObservableCollection<T> source, Predicate<T> p) {
      var itemsToRemove = source.Where(t => p(t)).ToList();
      source.RemoveRange(itemsToRemove);
    }


    /// <summary>
    /// Gets property information for the specified <paramref name="property"/> expression.
    /// </summary>
    /// <typeparam name="TSource">Type of the parameter in the <paramref name="property"/> expression.</typeparam>
    /// <typeparam name="TValue">Type of the property's value.</typeparam>
    /// <param name="property">The expression from which to retrieve the property information.</param>
    /// <returns>Property information for the specified expression.</returns>
    /// <exception cref="ArgumentException">The expression is not understood.</exception>
    public static PropertyInfo GetPropertyInfo<TSource, TValue>(this Expression<Func<TSource, TValue>> property) {
      if (property == null) {
        throw new ArgumentNullException(nameof(property));
      }

      if (property.Body is MemberExpression body && body.Member is PropertyInfo propertyInfo)
        return propertyInfo;

      throw new ArgumentException("Expression is not a property", nameof(property));
    }


    /// <summary>
    /// Returns an observable sequence of the value of a property when <paramref name="source"/> raises <seealso cref="INotifyPropertyChanged.PropertyChanged"/> for the given property.
    /// </summary>
    /// <typeparam name="T">The type of the source object. Type must implement <seealso cref="INotifyPropertyChanged"/>.</typeparam>
    /// <typeparam name="TProperty">The type of the property that is being observed.</typeparam>
    /// <param name="source">The object to observe property changes on.</param>
    /// <param name="property">An expression that describes which property to observe.</param>
    /// <returns>Returns an observable sequence of the property values as they change.</returns>
    public static IObservable<TProperty> OnPropertyChanges<T, TProperty>(
        this T source,
        Expression<Func<T, TProperty>> property)
        where T : INotifyPropertyChanged {
      //todo better perf
      return Observable.Create<TProperty>(o => {
        var propertyName = property.GetPropertyInfo().Name;
        var propertySelector = property.Compile();

        return Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                handler => handler.Invoke,
                h => source.PropertyChanged += h,
                h => source.PropertyChanged -= h)
            .Where(e => e.EventArgs.PropertyName == propertyName)
            .Select(e => propertySelector(source))
            .Subscribe(o);
      });
    }
  }
}