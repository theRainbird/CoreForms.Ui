using System.Text;
using System.Web;
using OldSchoolForms.Ui.Core;

namespace OldSchoolForms.Ui.Html;

/// <summary>
/// Serializes a <see cref="RichTextDocument"/> back to HTML.
/// </summary>
public static class HtmlExport
{
    /// <summary>
    /// Converts the document to an HTML string.
    /// </summary>
    public static string ToHtml(RichTextDocument doc)
    {
        var sb = new StringBuilder();
        sb.Append("<body>");

        string? currentListTag = null;

        for (int bi = 0; bi < doc.Blocks.Count; bi++)
        {
            var block = doc.Blocks[bi];

            // Table block serialization
            if (block.Type == RichTextBlockType.Table && block.Rows != null)
            {
                if (sb.Length > 6) sb.AppendLine();
                sb.Append("<table>");
                foreach (var row in block.Rows)
                {
                    sb.AppendLine();
                    sb.Append("<tr>");
                    foreach (var cell in row.Cells)
                    {
                        string cellInner = RenderContent(cell.Content);
                        sb.AppendLine();
                        sb.Append($"<td>{cellInner}</td>");
                    }
                    sb.AppendLine();
                    sb.Append("</tr>");
                }
                sb.AppendLine();
                sb.Append("</table>");
                continue;
            }

            string tag = GetTag(block.Type);
            string listTag = block.Type is RichTextBlockType.BulletItem ? "ul"
                : block.Type is RichTextBlockType.NumberItem ? "ol" : null!;

            // Close previous list if needed
            if (currentListTag != null && listTag != currentListTag)
            {
                sb.AppendLine();
                sb.Append($"</{currentListTag}>");
                currentListTag = null;
            }

            // Open new list if needed
            if (listTag != null && listTag != currentListTag)
            {
                if (sb.Length > 6) sb.AppendLine();
                sb.Append($"<{listTag}>");
                currentListTag = listTag;
            }

            string inner = RenderContent(block.Content);
            string alignStyle = block.Alignment != BlockAlignment.Left ? $" style=\"text-align:{block.Alignment.ToString().ToLowerInvariant()}\"" : "";

            if (tag != null)
            {
                if (listTag != null)
                {
                    sb.AppendLine();
                    sb.Append($"  <{tag}{alignStyle}>{inner}</{tag}>");
                }
                else
                {
                    if (sb.Length > 6) sb.AppendLine();
                    sb.Append($"<{tag}{alignStyle}>{inner}</{tag}>");
                }
            }
        }

        // Close final list
        if (currentListTag != null)
        {
            sb.AppendLine();
            sb.Append($"</{currentListTag}>");
        }

        sb.AppendLine();
        sb.Append("</body>");
        return sb.ToString();
    }

    private static string RenderContent(List<InlineContent> content)
    {
        var sb = new StringBuilder();

        foreach (var item in content)
        {
            if (item is TextRun run)
            {
                string t = HtmlEncode(run.Text);
                bool hasStyle = run.FontFamily != "Arial" || Math.Abs(run.FontSize - 12) > 0.01f
                    || !run.ForeColor.Equals(Color.Empty)
                    || !run.BackColor.Equals(Color.Empty);
                string styleAttr = hasStyle ? BuildStyleAttr(run) : "";

                if (run.Style.HasFlag(FontStyle.Bold)) t = $"<b>{t}</b>";
                if (run.Style.HasFlag(FontStyle.Italic)) t = $"<i>{t}</i>";
                if (run.Style.HasFlag(FontStyle.Underline)) t = $"<u>{t}</u>";
                if (run.Style.HasFlag(FontStyle.Strikeout)) t = $"<s>{t}</s>";

                if (hasStyle)
                    t = $"<span style=\"{styleAttr}\">{t}</span>";

                sb.Append(t);
            }
            else if (item is LineBreakRun)
            {
                sb.Append("<br>");
            }
            else if (item is ImageRun image)
            {
                string src = HttpUtility.HtmlAttributeEncode(image.Src);
                sb.Append($"<img src=\"{src}\">");
            }
            else if (item is HyperlinkRun link)
            {
                string url = HttpUtility.HtmlAttributeEncode(link.Url);
                string inner = RenderContent(link.InnerContent);
                sb.Append($"<a href=\"{url}\">{inner}</a>");
            }
        }

        return sb.ToString();
    }

    private static string BuildStyleAttr(TextRun run)
    {
        var parts = new List<string>();
        if (run.FontFamily != "Arial")
            parts.Add($"font-family:{run.FontFamily}");
        if (Math.Abs(run.FontSize - 12) > 0.01f)
            parts.Add($"font-size:{run.FontSize}pt");
        if (!run.ForeColor.Equals(Color.Empty))
            parts.Add($"color:#{run.ForeColor.R:X2}{run.ForeColor.G:X2}{run.ForeColor.B:X2}");
        if (!run.BackColor.Equals(Color.Empty))
            parts.Add($"background-color:#{run.BackColor.R:X2}{run.BackColor.G:X2}{run.BackColor.B:X2}");
        return string.Join(";", parts);
    }

    private static string GetTag(RichTextBlockType type) => type switch
    {
        RichTextBlockType.Heading1 => "h1",
        RichTextBlockType.Heading2 => "h2",
        RichTextBlockType.Heading3 => "h3",
        RichTextBlockType.Heading4 => "h4",
        RichTextBlockType.Heading5 => "h5",
        RichTextBlockType.Heading6 => "h6",
        RichTextBlockType.BulletItem => "li",
        RichTextBlockType.NumberItem => "li",
        _ => "p"
    };

    private static string HtmlEncode(string text) =>
        HttpUtility.HtmlEncode(text);
}
