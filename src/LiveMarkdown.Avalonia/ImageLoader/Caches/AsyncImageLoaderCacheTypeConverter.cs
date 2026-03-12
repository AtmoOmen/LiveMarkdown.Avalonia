using System.ComponentModel;
using System.Globalization;

namespace LiveMarkdown.Avalonia;

#if NETSTANDARD2_0
public class AsyncImageLoaderCacheTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return sourceType == typeof(string);
    }

    public override object? ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object? value)
    {
        if (value is string str)
        {
            return str switch
            {
                "None" => null,
                "Ram" => RamBasedAsyncImageLoaderCache.Shared,
                _ => throw new NotSupportedException($"Cache type '{str}' is not supported.")
            };
        }

        return base.ConvertFrom(context, culture, value);
    }
}
#else
public class AsyncImageLoaderCacheTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string str)
        {
            return str switch
            {
                "None" => null,
                "Ram" => RamBasedAsyncImageLoaderCache.Shared,
                _ => throw new NotSupportedException($"Cache type '{str}' is not supported.")
            };
        }

        return base.ConvertFrom(context, culture, value);
    }
}
#endif