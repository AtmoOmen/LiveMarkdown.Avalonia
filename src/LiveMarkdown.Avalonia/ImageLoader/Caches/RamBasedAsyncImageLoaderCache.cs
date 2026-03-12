using Avalonia.Media;

namespace LiveMarkdown.Avalonia;

public class RamBasedAsyncImageLoaderCache : AsyncImageLoaderCache
{
    public static RamBasedAsyncImageLoaderCache Shared { get; } = new();

    private readonly Dictionary<Uri, WeakReference<IImage>> _cache = new();

    private int _checkThreshold = 16;

    public override IImage? GetImage(Uri uri)
    {
        lock (_cache)
        {
            if (_cache.TryGetValue(uri, out var weakRef) && weakRef.TryGetTarget(out var image))
            {
                return image;
            }

            return null;
        }
    }

    public override void SetImage(Uri uri, IImage image)
    {
        lock (_cache)
        {
            _cache[uri] = new WeakReference<IImage>(image);

            if (_cache.Count <= _checkThreshold) return;

            // Clean up weak references that are no longer alive
            var keysToRemove = _cache.Where(kvp => !kvp.Value.TryGetTarget(out _)).Select(kvp => kvp.Key).ToList();
            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
            }

            if (_cache.Count > _checkThreshold) _checkThreshold *= 2;
            else if (_cache.Count < _checkThreshold / 4) _checkThreshold /= 2;
        }
    }
}