using CoreForms.Ui.Core;

namespace CoreForms.Ui.Controls.Advanced;

/// <summary>
/// Provides data for Kanban board card events that reference a card.
/// </summary>
public class KanbanBoardCardEventArgs : EventArgs
{
    /// <summary>
    /// Gets the card associated with the event.
    /// </summary>
    public KanbanBoardCard Card { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="KanbanBoardCardEventArgs"/>.
    /// </summary>
    /// <param name="card">The card associated with the event.</param>
    /// <exception cref="ArgumentNullException">Thrown when card is null.</exception>
    public KanbanBoardCardEventArgs(KanbanBoardCard card)
    {
        Card = card ?? throw new ArgumentNullException(nameof(card));
    }
}

/// <summary>
/// Provides data for Kanban board card events that support cancellation.
/// </summary>
public class KanbanBoardCardCancelEventArgs : CancelEventArgs
{
    /// <summary>
    /// Gets the card associated with the event.
    /// </summary>
    public KanbanBoardCard Card { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="KanbanBoardCardCancelEventArgs"/>.
    /// </summary>
    /// <param name="card">The card associated with the event.</param>
    /// <exception cref="ArgumentNullException">Thrown when card is null.</exception>
    public KanbanBoardCardCancelEventArgs(KanbanBoardCard card)
        : base(false)
    {
        Card = card ?? throw new ArgumentNullException(nameof(card));
    }
}

/// <summary>
/// Provides data for the CardMoving event that occurs before a card is moved to a different column.
/// Supports cancellation to prevent the move.
/// </summary>
public class KanbanBoardCardMovingEventArgs : CancelEventArgs
{
    /// <summary>
    /// Gets the card being moved.
    /// </summary>
    public KanbanBoardCard Card { get; }

    /// <summary>
    /// Gets the column the card is being moved from.
    /// </summary>
    public KanbanBoardColumn FromColumn { get; }

    /// <summary>
    /// Gets the column the card is being moved to.
    /// </summary>
    public KanbanBoardColumn ToColumn { get; }

    /// <summary>
    /// Gets or sets the target index within the destination column.
    /// </summary>
    public int NewIndex { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="KanbanBoardCardMovingEventArgs"/>.
    /// </summary>
    /// <param name="card">The card being moved.</param>
    /// <param name="fromColumn">The source column.</param>
    /// <param name="toColumn">The destination column.</param>
    /// <param name="newIndex">The target index in the destination column.</param>
    /// <exception cref="ArgumentNullException">Thrown when card, fromColumn, or toColumn is null.</exception>
    public KanbanBoardCardMovingEventArgs(KanbanBoardCard card, KanbanBoardColumn fromColumn, KanbanBoardColumn toColumn, int newIndex)
        : base(false)
    {
        Card = card ?? throw new ArgumentNullException(nameof(card));
        FromColumn = fromColumn ?? throw new ArgumentNullException(nameof(fromColumn));
        ToColumn = toColumn ?? throw new ArgumentNullException(nameof(toColumn));
        NewIndex = newIndex;
    }
}

/// <summary>
/// Provides data for the CardReordering event that occurs before a card's position changes within a column.
/// Supports cancellation to prevent the reorder.
/// </summary>
public class KanbanBoardCardReorderingEventArgs : CancelEventArgs
{
    /// <summary>
    /// Gets the card being reordered.
    /// </summary>
    public KanbanBoardCard Card { get; }

    /// <summary>
    /// Gets the column the card belongs to.
    /// </summary>
    public KanbanBoardColumn Column { get; }

    /// <summary>
    /// Gets the original index of the card.
    /// </summary>
    public int OldIndex { get; }

    /// <summary>
    /// Gets or sets the new index for the card.
    /// </summary>
    public int NewIndex { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="KanbanBoardCardReorderingEventArgs"/>.
    /// </summary>
    /// <param name="card">The card being reordered.</param>
    /// <param name="column">The column the card belongs to.</param>
    /// <param name="oldIndex">The original index.</param>
    /// <param name="newIndex">The new index.</param>
    /// <exception cref="ArgumentNullException">Thrown when card or column is null.</exception>
    public KanbanBoardCardReorderingEventArgs(KanbanBoardCard card, KanbanBoardColumn column, int oldIndex, int newIndex)
        : base(false)
    {
        Card = card ?? throw new ArgumentNullException(nameof(card));
        Column = column ?? throw new ArgumentNullException(nameof(column));
        OldIndex = oldIndex;
        NewIndex = newIndex;
    }
}
