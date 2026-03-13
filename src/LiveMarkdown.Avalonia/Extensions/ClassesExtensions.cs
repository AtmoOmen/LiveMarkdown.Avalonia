using Avalonia.Controls;

namespace LiveMarkdown.Avalonia;

internal static class ClassesExtensions
{
    extension(Classes classes)
    {
        public bool Equals(params ReadOnlySpan<string> other)
        {
            if (classes.Count != other.Length) return false;

            foreach (var @class in other)
            {
                if (!classes.Contains(@class)) return false;
            }

            return true;
        }

        public void Reset(params ReadOnlySpan<string> newClasses)
        {
            if (classes.Equals(newClasses)) return;

            classes.Clear();
            foreach (var @class in newClasses)
            {
                classes.Add(@class);
            }
        }
    }
}