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
    // Static API
    private static readonly DependencyProperty ServiceProperty =
        DependencyProperty.RegisterAttached("Service", typeof(InspectorService), typeof(InspectorService),
            new PropertyMetadata(null));

    private readonly OverlayHost _overlay;
    private readonly SelectionController _selector;
    private readonly VisualTreeExplorer _tree;
    private readonly Window _window;





    private InspectorService(Window window)
    {
        _window = window;
        var root = (FrameworkElement)window.Content;
        _overlay = new OverlayHost();
        _tree = new VisualTreeExplorer(root);
        _selector = new SelectionController(root, _overlay.HighlightCanvasField);

        _overlay.Initialize(_tree, _selector);

        EnsureOverlayAttached(root);
        HookKeys(root);
        _selector.Selected += (_, fe) => _overlay.OnSelectionChanged(fe);
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





    private void EnsureOverlayAttached(FrameworkElement root)
    {
        if (root is Panel p)
        {
            if (!p.Children.Contains(_overlay))
                p.Children.Add(_overlay);
            Canvas.SetZIndex(_overlay, int.MaxValue);
        }
        else
        {
            var grid = new Grid();
            FrameworkElement original = root;
            _window.Content = null;
            grid.Children.Add(original);
            grid.Children.Add(_overlay);
            _window.Content = grid;
        }
    }





    private void HookKeys(FrameworkElement root)
    {
        root.KeyDown += (s, e) =>
        {
            var ctrl = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control)
                .HasFlag(CoreVirtualKeyStates.Down);
            var alt = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu)
                .HasFlag(CoreVirtualKeyStates.Down);
            if (ctrl && alt && e.Key == VirtualKey.I)
            {
                IsOpen = !IsOpen;
                e.Handled = true;
            }
            else if (ctrl && alt && e.Key == VirtualKey.P)
            {
                IsPickerEnabled = !IsPickerEnabled;
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Escape && IsPickerEnabled)
            {
                IsPickerEnabled = false;
                e.Handled = true;
            }
        };
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