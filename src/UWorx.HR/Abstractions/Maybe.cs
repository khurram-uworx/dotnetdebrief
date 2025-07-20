using System;
using System.Collections.Generic;

namespace UWorx.HR.Abstractions;

abstract class Maybe<T> : IEquatable<Maybe<T>>
{
    public sealed class Some : Maybe<T>
    {
        public Some(T v) => Value = v;
        public T Value { get; }

        public override bool Equals(object? obj) =>
            obj is Some other && EqualityComparer<T>.Default.Equals(Value, other.Value);

        public override int GetHashCode() =>
            EqualityComparer<T>.Default.GetHashCode(Value);

        public override bool Equals(Maybe<T>? other) =>
            other is Some s && EqualityComparer<T>.Default.Equals(Value, s.Value);
    }

    public sealed class None : Maybe<T>
    {
        internal None() { }

        public override bool Equals(object? obj) => obj is None;
        public override int GetHashCode() => 0;
        public override bool Equals(Maybe<T>? other) => other is None;
    }

    private static readonly None noneInstance = new None();

    public static Maybe<T> Something(T value) => new Some(value);
    public static None Nothing() => noneInstance;

    // Bind: Chain operations that return Maybe
    public Maybe<TResult> Bind<TResult>(Func<T, Maybe<TResult>> binder) =>
        this is Some some ? binder(some.Value) : Maybe<TResult>.Nothing();

    // Map: Transform the value if present
    public Maybe<TResult> Map<TResult>(Func<T, TResult> transform) =>
        this is Some some
        ? Maybe<TResult>.Something(transform(some.Value))
        : Maybe<TResult>.Nothing();

    // Pattern matching: Perform different actions based on the type
    public TResult Match<TResult>(Func<T, TResult> onSome, Func<TResult> onNone) =>
        this is Some some ? onSome(some.Value) : onNone();

    public abstract bool Equals(Maybe<T>? other);

    public override bool Equals(object? obj) =>
        obj is Maybe<T> other && Equals(other);

    public abstract override int GetHashCode();
}