// Project Name: WinUI3.Diagnostics
// File Name: ResourceProbe.cs
// Author: Kyle Crowder
// Github:  OldSkoolzRoolz
// Distributed under Open Source License
// Do not remove file headers


using System.Collections.Generic;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;


namespace WinUI3.Diagnostics;


public sealed record ResourceLookupStep(string Scope, string DictionaryKind, bool Found, object? Value);

public sealed record ResourceProbeResult(string Key, IReadOnlyList<ResourceLookupStep> Steps, object? Winner);

public static class ResourceProbe
{
    public static ResourceProbeResult Trace(FrameworkElement start, object key)
    {
        ElementTheme theme = start.ActualTheme;
        var steps = new List<ResourceLookupStep>();
        object? winner = null;

        foreach ((var name, ResourceDictionary dict) in EnumerateScopes(start))
        {
            // merged dictionaries last-to-first
            for (var i = dict.MergedDictionaries.Count - 1; i >= 0; i--)
            {
                ResourceDictionary md = dict.MergedDictionaries[i];
                if (TryGet(md, key, theme, out var v, out var kind))
                {
                    steps.Add(new ResourceLookupStep($"{name} > Merged #{i}", kind, true, v));
                    winner = v;
                    goto Done;
                }

                steps.Add(new ResourceLookupStep($"{name} > Merged #{i}", "Miss", false, null));
            }

            if (TryGet(dict, key, theme, out var val, out var k))
            {
                steps.Add(new ResourceLookupStep($"{name}", k, true, val));
                winner = val;
                goto Done;
            }

            steps.Add(new ResourceLookupStep($"{name}", "Miss", false, null));
        }

        Done:
        return new ResourceProbeResult(key.ToString()!, steps, winner);
    }





    private static bool TryGet(ResourceDictionary dict, object key, ElementTheme theme, out object? value,
        out string kind)
    {
        value = null;
        kind = "Default";
        if (dict.ThemeDictionaries is not null)
        {
            var t = theme switch { ElementTheme.Light => "Light", ElementTheme.Dark => "Dark", _ => "Default" };
            if (dict.ThemeDictionaries.TryGetValue(t, out var themed) && themed is ResourceDictionary td &&
                td.TryGetValue(key, out var tv))
            {
                value = tv;
                kind = $"Theme:{t}";
                return true;
            }

            if (dict.ThemeDictionaries.TryGetValue("Default", out var def) && def is ResourceDictionary dd &&
                dd.TryGetValue(key, out var dv))
            {
                value = dv;
                kind = "Theme:Default";
                return true;
            }
        }

        if (dict.TryGetValue(key, out var v))
        {
            value = v;
            kind = "Default";
            return true;
        }

        return false;
    }





    private static IEnumerable<(string name, ResourceDictionary dict)> EnumerateScopes(FrameworkElement fe)
    {
        for (DependencyObject? d = fe; d is not null; d = VisualTreeHelper.GetParent(d))
            if (d is FrameworkElement e)
                yield return ($"{e.GetType().Name}[{e.Name}].Resources", e.Resources);

        if (fe.XamlRoot?.Content is FrameworkElement root) yield return ("Window.Resources", root.Resources);
        yield return ("Application.Resources", Application.Current.Resources);
    }
}