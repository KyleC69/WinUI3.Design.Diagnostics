// Project Name: WinUI3.Diagnostics
// File Name: SelectionController.cs
// Author: Kyle Crowder
// Github:  OldSkoolzRoolz
// Distributed under Open Source License
// Do not remove file headers


using System;

using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;

using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;


namespace WinUI3.Diagnostics;


public sealed class SelectionController
{
    private readonly Canvas _overlay;
    private readonly FrameworkElement _root;
    private Border? _rect;





    public SelectionController(FrameworkElement root, Canvas overlay)
    {
        _root = root;
        _overlay = overlay;
        _overlay.IsHitTestVisible = false;
        _overlay.PointerPressed += OnPointerPressed;
    }





    public bool IsPickerEnabled { get; set; }
    public FrameworkElement? SelectedElement { get; private set; }

    public event EventHandler<FrameworkElement?>? Selected;





    public void Select(FrameworkElement? fe)
    {
        SelectedElement = fe;
        DrawHighlight();
        Selected?.Invoke(this, fe);
    }





    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (!IsPickerEnabled) return;

        var alt = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down);
        var shift = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift)
            .HasFlag(CoreVirtualKeyStates.Down);
        if (!(alt && shift)) return;

        var d = e.OriginalSource as DependencyObject;
        FrameworkElement? target = null;
        while (d is not null)
        {
            if (d is FrameworkElement fe)
            {
                target = fe;
                break;
            }

            d = VisualTreeHelper.GetParent(d);
        }

        Select(target);
        e.Handled = true;
    }





    private void DrawHighlight()
    {
        _overlay.Children.Clear();
        if (SelectedElement is null) return;

        var rect = new Border
        {
            BorderBrush = new SolidColorBrush(Colors.DeepSkyBlue),
            BorderThickness = new Thickness(2),
            Background = new SolidColorBrush(
                Color.FromArgb(40, 30, 144, 255)),
            IsHitTestVisible = false
        };

        GeneralTransform ttv = SelectedElement.TransformToVisual(_overlay);
        Rect bounds = ttv.TransformBounds(new Rect(0, 0, SelectedElement.ActualWidth, SelectedElement.ActualHeight));

        Canvas.SetLeft(rect, bounds.X);
        Canvas.SetTop(rect, bounds.Y);
        rect.Width = bounds.Width;
        rect.Height = bounds.Height;

        _overlay.Children.Add(rect);
        _rect = rect;
    }
}