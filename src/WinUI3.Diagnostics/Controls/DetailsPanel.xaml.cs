// Project Name: WinUI3.Diagnostics
// File Name: DetailsPanel.xaml.cs
// Author: Kyle Crowder
// Github:  OldSkoolzRoolz
// Distributed under Open Source License
// Do not remove file headers


using System.Linq;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;


namespace WinUI3.Diagnostics.Controls;


public sealed partial class DetailsPanel : UserControl
{
    private FrameworkElement? _element;





    public DetailsPanel()
    {
        InitializeComponent();
    }





    public void SetElement(FrameworkElement? fe)
    {
        _element = fe;
        ElementHeaderUpdate();
        StyleSummaryUpdate();
        TraceList.ItemsSource = null;
        WinnerText.Text = "";
    }





    private void ElementHeaderUpdate()
    {
        if (_element is null)
            // No selection
            return;
        var dt = _element.DataContext;
        var dcType = dt is null ? "(null)" : dt.GetType().FullName;
        // quick display
        // (Bound TextBlocks in XAML are illustrative; set content directly here)
    }





    private void StyleSummaryUpdate()
    {
        if (_element is null)
        {
            StyleSummary.Text = "No selection";
            return;
        }

        StyleReport report = StyleInspector.Inspect(_element);
        StyleSummary.Text =
            $"Explicit: {report.IsExplicit}\n" +
            $"Implicit: {report.ImplicitStyle is not null}\n" +
            $"BasedOnChain: {report.BasedOnChain.Count}\n" +
            $"TemplateApplied: {report.TemplateApplied}\n" +
            $"Setters: {report.EffectiveSetters.Count}";
    }





    private void OnTraceClick(object sender, RoutedEventArgs e)
    {
        if (_element is null) return;
        var keyText = ResourceKeyBox.Text;
        if (string.IsNullOrWhiteSpace(keyText)) return;

        ResourceProbeResult result = ResourceProbe.Trace(_element, keyText);
        TraceList.ItemsSource =
            result.Steps.Select(s => $"{s.Scope} [{s.DictionaryKind}] {(s.Found ? "FOUND" : "miss")}");
        WinnerText.Text = $"Winner: {result.Winner ?? "(none)"}";
    }





    private void OnHighlightClick(object sender, RoutedEventArgs e)
    {
        // No-op here; highlight is handled by SelectionController drawing the overlay.
    }
}