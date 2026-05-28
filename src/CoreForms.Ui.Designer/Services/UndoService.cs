using System;
using System.Collections.Generic;
using CoreForms.Ui.Core;

namespace CoreForms.Ui.Designer.Services;

/// <summary>
/// Represents a snapshot of control bounds for undo/redo operations.
/// </summary>
public class UndoSnapshot
{
    /// <summary>
    /// Gets the control whose state was captured.
    /// </summary>
    public Control Control { get; }

    /// <summary>
    /// Gets the captured bounds.
    /// </summary>
    public Rectangle Bounds { get; }

    /// <summary>
    /// Gets the captured text value.
    /// </summary>
    public string? Text { get; }

    /// <summary>
    /// Initializes a new snapshot for the given control.
    /// </summary>
    public UndoSnapshot(Control control)
    {
        Control = control;
        Bounds = control.Bounds;
        Text = control.Text;
    }

    /// <summary>
    /// Restores the control to the captured state.
    /// </summary>
    public void Restore()
    {
        Control.Bounds = Bounds;
        if (Text != null)
            Control.Text = Text;
    }
}

/// <summary>
/// Provides undo/redo support for designer operations.
/// Stores snapshots of control state before each mutation.
/// </summary>
public class UndoService
{
    private readonly Stack<UndoSnapshot[]> _undoStack = new();
    private readonly Stack<UndoSnapshot[]> _redoStack = new();
    private const int MaxUndoDepth = 100;

    /// <summary>
    /// Raised when the undo/redo state changes (stacks modified).
    /// </summary>
    public event EventHandler? StateChanged;

    /// <summary>
    /// Gets whether undo operations are available.
    /// </summary>
    public bool CanUndo => _undoStack.Count > 0;

    /// <summary>
    /// Gets whether redo operations are available.
    /// </summary>
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>
    /// Captures a snapshot of the given controls and pushes it onto the undo stack.
    /// Clears the redo stack, since a new action invalidates redo history.
    /// </summary>
    /// <param name="controls">The controls to capture.</param>
    public void PushSnapshot(params Control[] controls)
    {
        if (controls == null || controls.Length == 0) return;

        var snapshots = new UndoSnapshot[controls.Length];
        for (int i = 0; i < controls.Length; i++)
            snapshots[i] = new UndoSnapshot(controls[i]);

        _undoStack.Push(snapshots);
        _redoStack.Clear();

        if (_undoStack.Count > MaxUndoDepth)
        {
            var temp = new Stack<UndoSnapshot[]>(_undoStack.ToArray()[..MaxUndoDepth]);
            _undoStack.Clear();
            foreach (var item in temp)
                _undoStack.Push(item);
        }

        OnStateChanged();
    }

    /// <summary>
    /// Performs an undo, restoring the most recent snapshot.
    /// The current state is pushed onto the redo stack.
    /// </summary>
    public void Undo()
    {
        if (_undoStack.Count == 0) return;

        var snapshots = _undoStack.Pop();

        // Save current state for redo
        var redoSnapshots = new UndoSnapshot[snapshots.Length];
        for (int i = 0; i < snapshots.Length; i++)
            redoSnapshots[i] = new UndoSnapshot(snapshots[i].Control);
        _redoStack.Push(redoSnapshots);

        foreach (var snapshot in snapshots)
            snapshot.Restore();

        OnStateChanged();
    }

    /// <summary>
    /// Performs a redo, restoring the state before the last undo.
    /// </summary>
    public void Redo()
    {
        if (_redoStack.Count == 0) return;

        var snapshots = _redoStack.Pop();

        // Save current state for undo
        var undoSnapshots = new UndoSnapshot[snapshots.Length];
        for (int i = 0; i < snapshots.Length; i++)
            undoSnapshots[i] = new UndoSnapshot(snapshots[i].Control);
        _undoStack.Push(undoSnapshots);

        foreach (var snapshot in snapshots)
            snapshot.Restore();

        OnStateChanged();
    }

    /// <summary>
    /// Clears all undo/redo history.
    /// </summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        OnStateChanged();
    }

    private void OnStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
}
