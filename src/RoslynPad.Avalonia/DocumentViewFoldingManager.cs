﻿using Avalonia.Threading;
using AvaloniaEdit.Folding;
using RoslynPad.Folding;

namespace RoslynPad;

partial class DocumentView
{
    DispatcherTimer _foldingUpdateTimer;
    FoldingStrategy _foldingStrategy;

    public FoldingManager FoldingManager { get; private set; }

    public FoldingStrategy FoldingStrategy
    {
        get => _foldingStrategy;
        set
        {
            if (_foldingStrategy == value)
                return;

            _foldingStrategy = value;
        }
    }

    public TimeSpan FoldingUpdateInterval
    {
        get => _foldingUpdateTimer.Interval;
        set => _foldingUpdateTimer.Interval = value;
    }

    private void UpdateFoldingManager()
    {
        if (FoldingManager != null)
            UninstallFoldingManager();

        if (Editor.Document != null)
            InstallFoldingManager();
    }

    private void InstallFoldingManager()
    {
        FoldingManager = FoldingManager.Install(Editor.TextArea);

        _foldingUpdateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _foldingUpdateTimer.Tick += OnFoldingTimerTick;
        _foldingUpdateTimer.Start();
    }

    private void UninstallFoldingManager()
    {
        _foldingUpdateTimer.Stop();
        _foldingUpdateTimer.Tick -= OnFoldingTimerTick;

        FoldingManager.Uninstall(FoldingManager);
    }

    private void OnFoldingTimerTick(object? sender, EventArgs e)
    {
        UpdateFoldings();
    }

    public void FoldAllFoldings()
    {
        if (FoldingManager == null)
            return;

        foreach (var foldingSection in FoldingManager.AllFoldings)
            foldingSection.IsFolded = true;
    }

    public void UnfoldAllFoldings()
    {
        if (FoldingManager == null)
            return;

        foreach (var foldingSection in FoldingManager.AllFoldings)
            foldingSection.IsFolded = false;
    }

    public void ToggleAllFoldings()
    {
        if (FoldingManager == null)
            return;

        var fold = FoldingManager.AllFoldings.All(folding => !folding.IsFolded);

        foreach (var foldingSection in FoldingManager.AllFoldings)
            foldingSection.IsFolded = fold;
    }

    public void ToggleCurrentFolding()
    {
        if (FoldingManager == null)
            return;

        var folding = FoldingManager.GetNextFolding(Editor.CaretOffset);
        if (folding == null || Editor.Document.GetLocation(folding.StartOffset).Line != Editor.Document.GetLocation(Editor.CaretOffset).Line)
        {
            folding = FoldingManager.GetFoldingsContaining(Editor.CaretOffset).LastOrDefault();
        }

        if (folding != null)
            folding.IsFolded = !folding.IsFolded;
    }

    public void UpdateFoldings()
    {
        if (FoldingManager == null)
            return;

        if (FoldingStrategy != null)
            FoldingStrategy.UpdateFoldings(FoldingManager, Editor.Document);

        else if (FoldingManager.AllFoldings.Any())
            FoldingManager.Clear();
    }

    public object? SaveFoldings()
    {
        return FoldingManager?.AllFoldings
                              .Select(folding => new NewFolding
                              {
                                  StartOffset = folding.StartOffset,
                                  EndOffset = folding.EndOffset,
                                  Name = folding.Title,
                                  DefaultClosed = folding.IsFolded
                              })
                              .ToList();
    }

    public void RestoreFoldings(object foldings)
    {
        var list = foldings as IEnumerable<NewFolding>;
        if (list == null)
            return;

        FoldingManager.Clear();
        FoldingManager.UpdateFoldings(list, -1);
    }

}
