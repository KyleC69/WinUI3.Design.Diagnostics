// Project Name: WinUI3.Diagnostics
// File Name: VisualTreeExplorer.cs
// Author: Kyle Crowder
// Github:  OldSkoolzRoolz
// Distributed under Open Source License
// Do not remove file headers


using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;


namespace WinUI3.Diagnostics;


public sealed class TreeNode
{
    public string Header { get; set; } = "";
    public DependencyObject Target { get; set; } = default!;
    public ObservableCollection<TreeNode> Children { get; } = new();
}

public sealed class VisualTreeExplorer
{
    private readonly FrameworkElement _root;

    private CancellationTokenSource? _cts;





    public VisualTreeExplorer(FrameworkElement root)
    {
        _root = root;
        Rebuild();
        _root.LayoutUpdated += (_, __) => DebouncedRebuild();
        _root.Loaded += (_, __) => DebouncedRebuild();
        _root.Unloaded += (_, __) => DebouncedRebuild();
    }





    public ObservableCollection<TreeNode> Nodes { get; } = new();





    private void DebouncedRebuild()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        CancellationToken token = _cts.Token;
        _ = Task.Run(async () =>
        {
            await Task.Delay(150, token);
            if (token.IsCancellationRequested) return;
            _root.DispatcherQueue.TryEnqueue(Rebuild);
        }, token);
    }





    public void Rebuild()
    {
        Nodes.Clear();
        Nodes.Add(BuildNode(_root));
    }





    private TreeNode BuildNode(DependencyObject d)
    {
        var name = (d as FrameworkElement)?.Name;
        var childCount = VisualTreeHelper.GetChildrenCount(d);
        var header = $"{d.GetType().Name}{(string.IsNullOrEmpty(name) ? "" : $" [{name}]")} ({childCount})";
        var node = new TreeNode { Target = d, Header = header };

        for (var i = 0; i < childCount; i++)
            node.Children.Add(BuildNode(VisualTreeHelper.GetChild(d, i)));

        return node;
    }
}