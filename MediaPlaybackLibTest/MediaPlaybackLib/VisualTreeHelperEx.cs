using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Media;

namespace MediaPlaybackLib
{
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    [ExcludeFromCodeCoverage]  // Gui control - not unit testable; automated tests cover this
    public static class VisualTreeHelperEx
    {
        public static T FindParent<T>(DependencyObject reference) where T : class
        {
            DependencyObject d = reference;
            while ((d = VisualTreeHelper.GetParent(d)) != null)
            {
                var p = d as T;
                if (p != null)
                    return p;
            }
            return null;
        }

        public static FrameworkElement FindChildByName(DependencyObject reference, string name)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(reference); i++)
            {
                var child = VisualTreeHelper.GetChild(reference, i) as FrameworkElement;
                if (child != null)
                {
                    if (child.Name == name)
                    {
                        return child;
                    }

                    FrameworkElement childOfChild = FindChildByName(child, name);
                    if (childOfChild != null)
                    {
                        return childOfChild;
                    }
                }
            }
            return null;
        }

        public static IEnumerable<T> FindVisualChild<T>(DependencyObject d) where T : class
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(d); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(d, i);
                var result = child as T;
                if (result != null)
                {
                    yield return result;
                }

                foreach (var c in FindVisualChild<T>(child))
                {
                    yield return c;
                }
            }
        }
    }
}
