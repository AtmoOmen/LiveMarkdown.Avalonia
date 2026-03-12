using System.ComponentModel;
using Avalonia.Media;

namespace LiveMarkdown.Avalonia;

[TypeConverter(typeof(AsyncImageLoaderCacheTypeConverter))]
public abstract class AsyncImageLoaderCache
{
    public abstract IImage? GetImage(Uri uri);

    public abstract void SetImage(Uri uri, IImage image);
}