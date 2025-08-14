// Project Name: WinUI3.Diagnostics
// File Name: StyleInspector.cs
// Author: Kyle Crowder
// Github:  OldSkoolzRoolz
// Distributed under Open Source License
// Do not remove file headers


using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using System;
using System.Collections.Generic;
using System.Linq;


namespace WinUI3.Diagnostics;


public sealed record StyleReport(
    bool IsExplicit,
    Style? ExplicitStyle,
    Style? ImplicitStyle,
    IReadOnlyList<Style> BasedOnChain,
    IReadOnlyList<Setter> EffectiveSetters,
    bool TemplateApplied,
    string? TemplateRootSummary);

public static class StyleInspector
{
    public static StyleReport Inspect(FrameworkElement fe)
    {
        Style explicitStyle = fe.Style;
        Style? implicitStyle = explicitStyle is null ? FindImplicitStyle(fe) : null;
        Style? effective = explicitStyle ?? implicitStyle;
        var chain = AggregateBasedOn(effective);
        var setters = AggregateSetters(chain);
        var isControl = fe is Control;
        var templateApplied = isControl && ((Control)fe).Template is not null;
        string? rootSummary = null;

        return new StyleReport(
            explicitStyle is not null,
            explicitStyle,
            implicitStyle,
            chain,
            setters,
            templateApplied,
            rootSummary);
    }





    private static Style? FindImplicitStyle(FrameworkElement fe)
    {
        Type key = fe.GetType();
        foreach (ResourceDictionary dict in EnumerateScopes(fe))
        {
            // Theme dictionaries then base
            var themed = TryGetFromThemes(dict, key, fe.ActualTheme);
            if (themed is Style s1) return s1;
            if (dict.TryGetValue(key, out var v) && v is Style s2) return s2;
        }

        return null;
    }





    private static IEnumerable<ResourceDictionary> EnumerateScopes(FrameworkElement fe)
    {
        for (DependencyObject? d = fe; d is not null; d = VisualTreeHelper.GetParent(d))
            if (d is FrameworkElement e)
                yield return e.Resources;
        if (fe.XamlRoot?.Content is FrameworkElement root) yield return root.Resources;
        yield return Application.Current.Resources;
    }





    private static object? TryGetFromThemes(ResourceDictionary dict, object key, ElementTheme theme)
    {
        if (dict.ThemeDictionaries is null) return null;
        var themeKey = theme switch { ElementTheme.Light => "Light", ElementTheme.Dark => "Dark", _ => "Default" };

        return dict.ThemeDictionaries.TryGetValue(themeKey, out var themed) && themed is ResourceDictionary td &&
            td.TryGetValue(key, out var v)
            ? v
            : dict.ThemeDictionaries.TryGetValue("Default", out var def) && def is ResourceDictionary dd &&
               dd.TryGetValue(key, out var dv)
            ? dv
            : null;
    }





    private static IReadOnlyList<Style> AggregateBasedOn(Style? style)
    {
        var result = new List<Style>();
        for (Style? s = style; s is not null; s = s.BasedOn) result.Add(s);
        return result;
    }





    private static IReadOnlyList<Setter> AggregateSetters(IReadOnlyList<Style> chain)
    {
        var stack = chain.Reverse(); // base first, derived last
        var list = new List<Setter>();
        foreach (Style s in stack)
            foreach (Setter setter in s.Setters.OfType<Setter>())
                list.Add(setter);
        return list;
    }
}
