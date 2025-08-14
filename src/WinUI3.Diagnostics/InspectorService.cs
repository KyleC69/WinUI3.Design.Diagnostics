// Project Name: WinUI3.Diagnostics
// File Name: InspectorService.cs
// Author: Kyle Crowder
// Github:  OldSkoolzRoolz
// Distributed under Open Source License
// Do not remove file headers

#nullable enable

using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Windows.System;
using Windows.UI.Core;

using WinUI3.Diagnostics.Controls;


namespace WinUI3.Diagnostics;


public sealed class InspectorService
{
    // Attached property used to associate a single InspectorService with a Window's root element
    private static readonly DependencyProperty ServiceProperty =
        DependencyProperty.RegisterAttached(
            "Service",
            typeof(InspectorService),
            typeof(InspectorService),
            new PropertyMetadata(null));

    private readonly OverlayHost _overlay;
    private readonly SelectionController _selector;
    private readonly VisualTreeExplorer _tree;
    private readonly Window _window;

    private InspectorService(Window window)
    {
        _window = window;
        var windowroot = (FrameworkElement)window.Content;
        var wrapper = new Canvas();
        wrapper.Name = "InspectorServiceWrapper";
        wrapper.Children.Add(windowroot);

        _overlay = new OverlayHost();
        _tree = new VisualTreeExplorer(wrapper);
        _selector = new SelectionController(wrapper, _overlay.HighlightCanvasField);

        _overlay.Initialize(_tree, _selector);

        EnsureOverlayAttached(wrapper);
        HookKeys(wrapper);

        // Avoids capturing allocations by using a static handler that calls instance method
        _selector.Selected += OnSelectorSelected;
    }

    public bool IsOpen
    {
        get => _overlay.IsOpen;
        set => _overlay.IsOpen = value;
    }

    public bool IsPickerEnabled
    {
        get => _selector.IsPickerEnabled;
        set => _selector.IsPickerEnabled = value;
    }

    public FrameworkElement? SelectedElement
    {
        get => _selector.SelectedElement;
        set => _selector.Select(value);
    }

    private void OnSelectorSelected(object? _, FrameworkElement? fe)
    {
        _overlay.OnSelectionChanged(fe);
    }

    private void EnsureOverlayAttached(FrameworkElement root)
    {
        // Fast-path: if root already a Canvas
        if (root is Canvas canvas)
        {
            AttachOverlayToCanvas(canvas);
            return;
        }

        // If root is any Panel (Grid, StackPanel, etc.), just add overlay instead of wrapping
        if (root is Panel panel)
        {
            AttachOverlayToPanel(panel);
            return;
        }

        // Fallback: wrap non-Panel root (e.g., a Control) in a Grid
        // WrapRootWithGrid(root);
    }

    private void AttachOverlayToCanvas(Canvas canvas)
    {
        if (!canvas.Children.Contains(_overlay))
        {
            canvas.Children.Add(_overlay);
        }
        // Highest possible z-order inside the canvas
        Canvas.SetZIndex(_overlay, 32767);
    }

    private void AttachOverlayToPanel(Panel panel)
    {
        if (!panel.Children.Contains(_overlay))
        {
            panel.Children.Add(_overlay);
        }

        // Try to put overlay visually on top when supported
        //Panel.SetZIndex(_overlay, int.MaxValue);
    }

    private void WrapRootWithGrid(FrameworkElement originalContent)
    {
        // Replace once with a lightweight Grid to host overlay
        var grid = new Grid();
        _window.Content = null;         // Help XAML tree detach cleanly before reparent
        grid.Children.Add(originalContent);
        grid.Children.Add(_overlay);
        _window.Content = grid;
    }

    private void HookKeys(FrameworkElement root)
    {
        // Use a named handler to avoid per-subscription lambda allocation
        root.KeyDown += OnRootKeyDown;
    }

    private void OnRootKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        // Use bitwise check instead of HasFlag (smaller/faster)
        bool ctrl = (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
        bool alt = (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;

        // Fast reject if no modifiers for the first two commands
        if (ctrl && alt)
        {
            if (e.Key == VirtualKey.I)
            {
                IsOpen = !IsOpen;
                e.Handled = true;
                return;
            }
            if (e.Key == VirtualKey.P)
            {
                IsPickerEnabled = !IsPickerEnabled;
                e.Handled = true;
                return;
            }
        }

        if (e.Key == VirtualKey.Escape && IsPickerEnabled)
        {
            IsPickerEnabled = false;
            e.Handled = true;
        }
    }

    public static InspectorService Attach(Window window)
    {
        var root = (FrameworkElement)window.Content;
        var existing = (InspectorService?)root.GetValue(ServiceProperty);
        if (existing is not null) return existing;

        var svc = new InspectorService(window);
        root.SetValue(ServiceProperty, svc);
        return svc;
    }

    public static InspectorService? Get(Window window)
    {
        var root = (FrameworkElement)window.Content;
        return (InspectorService?)root.GetValue(ServiceProperty);
    }
}
