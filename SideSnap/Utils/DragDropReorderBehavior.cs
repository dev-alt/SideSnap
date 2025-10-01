using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SideSnap.Utils;

public static class DragDropReorderBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(DragDropReorderBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj)
        => (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value)
        => obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ItemsControl itemsControl)
        {
            if ((bool)e.NewValue)
            {
                itemsControl.PreviewMouseLeftButtonDown += ItemsControl_PreviewMouseLeftButtonDown;
                itemsControl.PreviewMouseMove += ItemsControl_PreviewMouseMove;
                itemsControl.Drop += ItemsControl_Drop;
                itemsControl.AllowDrop = true;
            }
            else
            {
                itemsControl.PreviewMouseLeftButtonDown -= ItemsControl_PreviewMouseLeftButtonDown;
                itemsControl.PreviewMouseMove -= ItemsControl_PreviewMouseMove;
                itemsControl.Drop -= ItemsControl_Drop;
                itemsControl.AllowDrop = false;
            }
        }
    }

    private static Point? _startPoint;
    private static object? _draggedItem;

    private static void ItemsControl_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(null);

        // Find the data context of the clicked item
        var element = e.OriginalSource as DependencyObject;
        while (element != null && !(element is ContentPresenter))
        {
            element = VisualTreeHelper.GetParent(element);
        }

        if (element is ContentPresenter presenter)
        {
            _draggedItem = presenter.Content;
        }
    }

    private static void ItemsControl_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && _startPoint.HasValue && _draggedItem != null)
        {
            Point currentPosition = e.GetPosition(null);
            Vector diff = _startPoint.Value - currentPosition;

            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                DragDrop.DoDragDrop((DependencyObject)sender, _draggedItem, DragDropEffects.Move);
                _draggedItem = null;
                _startPoint = null;
            }
        }
    }

    private static void ItemsControl_Drop(object sender, System.Windows.DragEventArgs e)
    {
        if (sender is ItemsControl itemsControl && e.Data.GetDataPresent(e.Data.GetFormats()[0]))
        {
            var droppedData = e.Data.GetData(e.Data.GetFormats()[0]);

            // Find the target item
            var element = e.OriginalSource as DependencyObject;
            while (element != null && !(element is ContentPresenter))
            {
                element = VisualTreeHelper.GetParent(element);
            }

            if (element is ContentPresenter presenter && presenter.Content != droppedData)
            {
                var targetItem = presenter.Content;

                // Get the source collection
                var source = itemsControl.ItemsSource as System.Collections.IList;
                if (source != null && source.Contains(droppedData) && source.Contains(targetItem))
                {
                    int oldIndex = source.IndexOf(droppedData);
                    int newIndex = source.IndexOf(targetItem);

                    if (oldIndex != newIndex)
                    {
                        source.Remove(droppedData);
                        source.Insert(newIndex, droppedData);
                    }
                }
            }
        }
    }
}