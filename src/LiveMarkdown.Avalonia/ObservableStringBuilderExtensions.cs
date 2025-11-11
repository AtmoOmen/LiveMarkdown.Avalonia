namespace LiveMarkdown.Avalonia;

public static class ObservableStringBuilderExtensions
{
    public static IDisposable SubscribeAppend(
        this ObservableStringBuilder builder,
        IObservable<string> source)
    {
        if (builder is null) throw new ArgumentNullException(nameof(builder));
        if (source is null) throw new ArgumentNullException(nameof(source));

        var observer = new AnonymousObserver<string>(
            onNext: value =>
            {
                builder.Append(value);
            },
            onError: ex =>
            {
                throw ex;
            },
            onCompleted: () =>
            {

            });

        return source.Subscribe(observer);
    }

    public static IDisposable SubscribeAppendLine(
        this ObservableStringBuilder builder,
        IObservable<string> source)
    {
        if (builder is null) throw new ArgumentNullException(nameof(builder));
        if (source is null) throw new ArgumentNullException(nameof(source));

        var observer = new AnonymousObserver<string>(
            onNext: value =>
            {
                builder.AppendLine(value);
            },
            onError: ex =>
            {
                throw ex;
            },
            onCompleted: () =>
            {
                
            });

        return source.Subscribe(observer);
    }

    public static async Task EnumerateAppendAsync(
        this ObservableStringBuilder builder,
        IAsyncEnumerable<string> asyncEnumerable,
        TimeSpan? timeSpan = null,
        CancellationToken cancellationToken = default)
    {
        if (builder is null) throw new ArgumentNullException(nameof(builder));
        if (asyncEnumerable is null) throw new ArgumentNullException(nameof(asyncEnumerable));

        await foreach (var line in asyncEnumerable.WithCancellation(cancellationToken))
        {
            builder.Append(line);
            if (timeSpan.HasValue)
            {
                await Task.Delay(timeSpan.Value, cancellationToken);
            }
        }
    }


    public static async Task EnumerateAppendLineAsync(
        this ObservableStringBuilder builder,
        IAsyncEnumerable<string> asyncEnumerable,
        TimeSpan? timeSpan = null,
        CancellationToken cancellationToken = default)
    {
        if (builder is null) throw new ArgumentNullException(nameof(builder));
        if (asyncEnumerable is null) throw new ArgumentNullException(nameof(asyncEnumerable));

        await foreach (var line in asyncEnumerable.WithCancellation(cancellationToken))
        {
            builder.AppendLine(line);
            if (timeSpan.HasValue)
            {
                await Task.Delay(timeSpan.Value, cancellationToken);
            }
        }
    }

    private sealed class AnonymousObserver<T> : IObserver<T>
    {
        private readonly Action<T>? onNext;
        private readonly Action<Exception>? onError;
        private readonly Action? onCompleted;
        private bool isStopped;

        public AnonymousObserver(Action<T>? onNext, Action<Exception>? onError, Action? onCompleted)
        {
            this.onNext = onNext;
            this.onError = onError;
            this.onCompleted = onCompleted;
        }

        public void OnNext(T value)
        {
            if (!isStopped)
                onNext?.Invoke(value);
        }

        public void OnError(Exception error)
        {
            if (isStopped) return;
            isStopped = true;
            onError?.Invoke(error);
        }

        public void OnCompleted()
        {
            if (isStopped) return;
            isStopped = true;
            onCompleted?.Invoke();
        }
    }
}
