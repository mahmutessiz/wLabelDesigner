namespace wLabelDesigner.Services;

public sealed class UndoHistory
{
    private const int MaximumUndoStates = 100;
    private static readonly TimeSpan MergeWindow = TimeSpan.FromMilliseconds(650);
    private readonly Stack<DesignerSnapshot> undoStates = new();
    private readonly Stack<DesignerSnapshot> redoStates = new();
    private DesignerSnapshot? currentState;
    private string? lastMergeKey;
    private DateTime lastRecordedAt;

    public bool CanUndo => undoStates.Count > 0;

    public bool CanRedo => redoStates.Count > 0;

    public void Reset(DesignerSnapshot state)
    {
        ArgumentNullException.ThrowIfNull(state);
        undoStates.Clear();
        redoStates.Clear();
        currentState = state;
        lastMergeKey = null;
        lastRecordedAt = default;
    }

    public void Record(DesignerSnapshot state, string? mergeKey = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (currentState is null)
        {
            Reset(state);
            return;
        }

        var now = DateTime.UtcNow;
        var canMerge = mergeKey is not null &&
            string.Equals(mergeKey, lastMergeKey, StringComparison.Ordinal) &&
            now - lastRecordedAt <= MergeWindow;

        if (!canMerge)
        {
            undoStates.Push(currentState);
            TrimUndoStates();
        }

        currentState = state;
        redoStates.Clear();
        lastMergeKey = mergeKey;
        lastRecordedAt = now;
    }

    public void UpdateSelection(Guid? selectedElementId)
    {
        if (currentState is not null)
        {
            currentState = currentState with { SelectedElementId = selectedElementId };
        }
    }

    public bool TryUndo(out DesignerSnapshot? state)
    {
        if (currentState is null || undoStates.Count == 0)
        {
            state = null;
            return false;
        }

        redoStates.Push(currentState);
        currentState = undoStates.Pop();
        ResetMergeTracking();
        state = currentState;
        return true;
    }

    public bool TryRedo(out DesignerSnapshot? state)
    {
        if (currentState is null || redoStates.Count == 0)
        {
            state = null;
            return false;
        }

        undoStates.Push(currentState);
        currentState = redoStates.Pop();
        ResetMergeTracking();
        state = currentState;
        return true;
    }

    private void ResetMergeTracking()
    {
        lastMergeKey = null;
        lastRecordedAt = default;
    }

    private void TrimUndoStates()
    {
        if (undoStates.Count <= MaximumUndoStates)
        {
            return;
        }

        var retainedStates = undoStates.Take(MaximumUndoStates).Reverse().ToArray();
        undoStates.Clear();
        foreach (var state in retainedStates)
        {
            undoStates.Push(state);
        }
    }
}
