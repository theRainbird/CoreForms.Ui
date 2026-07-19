using OldSchoolForms.Ui.Core;

namespace OldSchoolForms.Ui.Html;

/// <summary>
/// Defines the type of a rich text block.
/// </summary>
public enum RichTextBlockType
{
    /// <summary>A standard paragraph.</summary>
    Paragraph,
    /// <summary>Heading level 1.</summary>
    Heading1,
    /// <summary>Heading level 2.</summary>
    Heading2,
    /// <summary>Heading level 3.</summary>
    Heading3,
    /// <summary>Heading level 4.</summary>
    Heading4,
    /// <summary>Heading level 5.</summary>
    Heading5,
    /// <summary>Heading level 6.</summary>
    Heading6,
    /// <summary>An item in an unordered (bulleted) list.</summary>
    BulletItem,
    /// <summary>An item in an ordered (numbered) list.</summary>
    NumberItem,
    /// <summary>A table block containing rows and cells.</summary>
    Table
}

/// <summary>
/// Base class for all inline content elements within a rich text block.
/// </summary>
public abstract class InlineContent
{
    /// <summary>
    /// Gets the length of this content in terms of flat character positions.
    /// </summary>
    /// <remarks>
    /// TextRuns return the text length. LineBreakRuns and ImageRuns return 1.
    /// HyperlinkRuns return the sum of their inner content lengths.
    /// </remarks>
    public abstract int Length { get; }
}

/// <summary>
/// A formatted text segment.
/// </summary>
public class TextRun : InlineContent
{
    /// <summary>The text content.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>The font style flags (Bold, Italic, Underline, Strikeout).</summary>
    public FontStyle Style { get; set; } = FontStyle.Regular;

    /// <summary>The font family name.</summary>
    public string FontFamily { get; set; } = "Arial";

    /// <summary>The font size in points.</summary>
    public float FontSize { get; set; } = 12;

    /// <summary>The text color.</summary>
    public Color ForeColor { get; set; } = Color.Empty;

    /// <summary>Gets the length of the text in this run.</summary>
    public override int Length => Text.Length;
}

/// <summary>
/// Represents a line break (&lt;br&gt;).
/// </summary>
public class LineBreakRun : InlineContent
{
    /// <summary>Always 1.</summary>
    public override int Length => 1;
}

/// <summary>
/// Represents an image reference (&lt;img&gt;).
/// </summary>
public class ImageRun : InlineContent
{
    /// <summary>The image source URL.</summary>
    public string Src { get; set; } = string.Empty;

    /// <summary>Always 1 in the flat index.</summary>
    public override int Length => 1;
}

/// <summary>
/// Represents a hyperlink (&lt;a&gt;) with inner inline content.
/// </summary>
public class HyperlinkRun : InlineContent
{
    /// <summary>The hyperlink URL.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>The inner inline content elements.</summary>
    public List<InlineContent> InnerContent { get; } = new();

    /// <summary>Sum of the lengths of all inner content.</summary>
    public override int Length => InnerContent.Sum(c => c.Length);
}

/// <summary>
/// Represents a single row in a table, containing a list of cells.
/// </summary>
public class TableRow
{
    /// <summary>The cells in this row.</summary>
    public List<TableCell> Cells { get; } = new();
}

/// <summary>
/// Represents a single cell in a table row, containing inline content.
/// </summary>
public class TableCell
{
    /// <summary>The inline content elements in this cell.</summary>
    public List<InlineContent> Content { get; } = new();
}

/// <summary>
/// A block of rich text content with a specific type and a list of inline content elements.
/// </summary>
public class RichTextBlock
{
    /// <summary>The block type (paragraph, heading, list item, table, etc.).</summary>
    public RichTextBlockType Type { get; set; } = RichTextBlockType.Paragraph;

    /// <summary>The inline content elements in this block.</summary>
    public List<InlineContent> Content { get; } = new();

    /// <summary>
    /// For table blocks (<see cref="RichTextBlockType.Table"/>), contains the table rows.
    /// Null for non-table blocks.
    /// </summary>
    public List<TableRow>? Rows { get; set; }

    /// <summary>
    /// For table blocks, the number of columns in the table.
    /// Zero for non-table blocks.
    /// </summary>
    public int ColCount { get; set; }
}

/// <summary>
/// Represents the full rich text document as a list of blocks.
/// </summary>
public class RichTextDocument
{
    /// <summary>The blocks in the document.</summary>
    public List<RichTextBlock> Blocks { get; } = new();

    /// <summary>Total character count across all blocks and table cell content.</summary>
    public int TotalLength
    {
        get
        {
            int total = 0;
            foreach (var b in Blocks)
            {
                if (b.Type == RichTextBlockType.Table && b.Rows != null)
                {
                    foreach (var row in b.Rows)
                        foreach (var cell in row.Cells)
                            total += cell.Content.Sum(c => c.Length);
                }
                else
                {
                    total += b.Content.Sum(c => c.Length);
                }
            }
            return total;
        }
    }
}

/// <summary>
/// Represents a cursor or selection position in the document.
/// <paramref name="BlockIndex"/> is the block index,
/// <paramref name="ContentIndex"/> is the index into the block's Content list,
/// <paramref name="CharOffset"/> is the character offset into a TextRun (0 for non-text content).
/// </summary>
public readonly struct DocumentPosition(int blockIndex, int contentIndex, int charOffset)
{
    /// <summary>Index of the block.</summary>
    public int BlockIndex { get; } = blockIndex;
    /// <summary>Index into the block's content list.</summary>
    public int ContentIndex { get; } = contentIndex;
    /// <summary>Character offset within a TextRun.</summary>
    public int CharOffset { get; } = charOffset;

    public static bool operator ==(DocumentPosition a, DocumentPosition b) =>
        a.BlockIndex == b.BlockIndex && a.ContentIndex == b.ContentIndex && a.CharOffset == b.CharOffset;
    public static bool operator !=(DocumentPosition a, DocumentPosition b) => !(a == b);
    public override bool Equals(object? obj) => obj is DocumentPosition p && this == p;
    public override int GetHashCode() => HashCode.Combine(BlockIndex, ContentIndex, CharOffset);
    public override string ToString() => $"({BlockIndex},{ContentIndex},{CharOffset})";
}
