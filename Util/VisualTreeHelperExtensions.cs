﻿using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Dash
{
    public static class VisualTreeHelperExtensions
    {
        public static T GetFirstDescendantOfType<T>(this DependencyObject start)
        {
            return start.GetDescendantsOfType<T>().FirstOrDefault();
        }

        public static IEnumerable<T> GetDescendantsOfType<T>(this DependencyObject start)
        {
            return start.GetDescendants().OfType<T>();
        }

        public static IEnumerable<T> GetDescentantsOfNextGenerationOfType<T>(this DependencyObject start)
        {
            return start.GetDescendantsOfNextGeneration().OfType<T>();
        }

        public static IEnumerable<DependencyObject> GetDescendantsOfNextGeneration(this DependencyObject start)
        {
            var queue = new Queue<DependencyObject>();
            var count = VisualTreeHelper.GetChildrenCount(start);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(start, i);
                yield return child;
            }
        }
        public static IEnumerable<T> GetImmediateDescendantsOfType<T>(this DependencyObject start) where T : DependencyObject
        {
            var queue = new Queue<DependencyObject>();
            var count = VisualTreeHelper.GetChildrenCount(start);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(start, i);
                if (child is T)
                    yield return child as T;
                else
                    queue.Enqueue(child);
            }
            while (queue.Count > 0)
            {
                var parent = queue.Dequeue();
                var count2 = VisualTreeHelper.GetChildrenCount(parent);

                for (int i = 0; i < count2; i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);
                    if (child is T)
                        yield return child as T;
                    else
                        queue.Enqueue(child);
                }
            }
        }

        public static IEnumerable<DependencyObject> GetDescendants(this DependencyObject start)
        {
            var queue = new Queue<DependencyObject>();
            var count = VisualTreeHelper.GetChildrenCount(start);

            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(start, i);
                yield return child;
                queue.Enqueue(child);
            }

            while (queue.Count > 0)
            {
                var parent = queue.Dequeue();
                var count2 = VisualTreeHelper.GetChildrenCount(parent);

                for (int i = 0; i < count2; i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);
                    yield return child;
                    queue.Enqueue(child);
                }
            }
        }

        public static T GetFirstAncestorOfType<T>(this DependencyObject start)
        {
            return start.GetAncestorsOfType<T>().FirstOrDefault();
        }

        public static IEnumerable<T> GetAncestorsOfType<T>(this DependencyObject start)
        {
            return start.GetAncestors().OfType<T>();
        }

        public static IEnumerable<DependencyObject> GetAncestors(this DependencyObject start)
        {
            var parent = VisualTreeHelper.GetParent(start);

            while (parent != null)
            {
                yield return parent;
                parent = VisualTreeHelper.GetParent(parent);
            }
        }

        public static bool IsInVisualTree(this DependencyObject dob)
        {
            return Window.Current.Content != null && dob.GetAncestors().Contains(Window.Current.Content);
        }

        public static Point RootPointerPos(this UIElement dob)
        {
            var pointerPosition = Windows.UI.Core.CoreWindow.GetForCurrentThread().PointerPosition;
            var x = pointerPosition.X - Window.Current.Bounds.X;
            var y = pointerPosition.Y - Window.Current.Bounds.Y;
            var pos = new Point(x, y);
            return pos;
        }

        public static bool IsPointerOver(this UIElement dob)
        {
            var overlappedViews = VisualTreeHelper.FindElementsInHostCoordinates(dob.RootPointerPos(), dob).ToList();
            return overlappedViews.Count > 0;
        }
        public static bool IsTopmost(this UIElement dob)
        {
            var overlappedViews = VisualTreeHelper.FindElementsInHostCoordinates(dob.RootPointerPos(), MainPage.Instance).ToList();
            return dob == overlappedViews.FirstOrDefault();
        }

        public static Rect GetBoundingRect(this FrameworkElement dob, FrameworkElement relativeTo = null)
        {
            if (relativeTo == null)
            {
                relativeTo = Window.Current.Content as FrameworkElement;
            }

            if (relativeTo == null)
            {
                throw new InvalidOperationException("Element not in visual tree.");
            }

            if (dob == relativeTo)
                return new Rect(0, 0, relativeTo.ActualWidth, relativeTo.ActualHeight);

            var ancestors = dob.GetAncestors().ToArray();

            if (!ancestors.Contains(relativeTo))
            {
                throw new InvalidOperationException("Element not in visual tree.");
            }

            var pos =
                dob
                    .TransformToVisual(relativeTo)
                    .TransformPoint(new Point());
            var pos2 =
                dob
                    .TransformToVisual(relativeTo)
                    .TransformPoint(
                        new Point(
                            dob.ActualWidth,
                            dob.ActualHeight));

            return new Rect(pos, pos2);
        }

        public static bool IsRightPressed(this PointerRoutedEventArgs e)
        {
            return e.GetCurrentPoint(null).Properties.IsRightButtonPressed;
        }
        public static bool IsCtrlPressed(this FrameworkElement f)
        {
            return Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
        }
        public static bool IsF1Pressed(this FrameworkElement f)
        {
            return Window.Current.CoreWindow.GetKeyState(VirtualKey.F1).HasFlag(CoreVirtualKeyStates.Down);
        }
        public static bool IsF2Pressed(this FrameworkElement f)
        {
            return Window.Current.CoreWindow.GetKeyState(VirtualKey.F2).HasFlag(CoreVirtualKeyStates.Down);
        }
        public static bool IsShiftPressed(this FrameworkElement f)
        {
            return Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
        }
        public static bool IsAltPressed(this FrameworkElement f)
        {
            return Window.Current.CoreWindow.GetKeyState(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down);
        }
        public static bool IsTabPressed(this FrameworkElement f)
        {
            return Window.Current.CoreWindow.GetKeyState(VirtualKey.Tab).HasFlag(CoreVirtualKeyStates.Down);
        }
        public static bool IsRightBtnPressed(this FrameworkElement f)
        {
            return Window.Current.CoreWindow.GetKeyState(VirtualKey.RightButton).HasFlag(CoreVirtualKeyStates.Down);
        }
        public static bool IsLeftBtnPressed(this FrameworkElement f)
        {
            return Window.Current.CoreWindow.GetKeyState(VirtualKey.LeftButton).HasFlag(CoreVirtualKeyStates.Down);
        }
    }
}
