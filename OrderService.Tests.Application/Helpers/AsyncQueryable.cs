using System.Collections;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace OrderService.Tests.Application.Helpers;

public static class AsyncQueryable
{
  public static IQueryable<T> From<T>(IEnumerable<T> source) => new TestAsyncEnumerable<T>(source);
}

internal sealed class TestAsyncQueryProvider<TEntity>(IQueryProvider inner) : IAsyncQueryProvider
{
  public IQueryable CreateQuery(Expression expression) => new TestAsyncEnumerable<TEntity>(expression);

  public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new TestAsyncEnumerable<TElement>(expression);

  public object? Execute(Expression expression) => inner.Execute(expression);

  public TResult Execute<TResult>(Expression expression) => inner.Execute<TResult>(expression);

  public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
  {
    var resultType = typeof(TResult).GetGenericArguments().First();
    var executionResult = typeof(IQueryProvider)
        .GetMethod(
            name: nameof(IQueryProvider.Execute),
            genericParameterCount: 1,
            types: [typeof(Expression)])!
        .MakeGenericMethod(resultType)
        .Invoke(inner, [expression]);

    return (TResult)typeof(Task)
        .GetMethod(nameof(Task.FromResult))!
        .MakeGenericMethod(resultType)
        .Invoke(null, [executionResult])!;
  }
}

internal sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
  public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable)
  {
  }

  public TestAsyncEnumerable(Expression expression) : base(expression)
  {
  }

  IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken) =>
      new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

  IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

internal sealed class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
{
  public T Current => inner.Current;

  public ValueTask<bool> MoveNextAsync() => new(inner.MoveNext());

  public ValueTask DisposeAsync()
  {
    inner.Dispose();
    return ValueTask.CompletedTask;
  }
}
