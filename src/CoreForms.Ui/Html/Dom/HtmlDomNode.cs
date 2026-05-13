using System.Collections.Generic;

namespace CoreForms.Ui.Html.Dom;

public enum HtmlNodeType { Document, Element, Text, Comment, DocumentFragment }

public abstract class HtmlDomNode
{
    public HtmlDomDocument? OwnerDocument { get; set; }
    public HtmlDomNode? Parent { get; set; }
    public HtmlNodeType NodeType { get; protected set; }
    public List<HtmlDomNode> Children { get; } = new();

    public abstract string GetTextContent();
    public abstract void SetTextContent(string text);

    public virtual void AppendChild(HtmlDomNode child)
    {
        child.Parent = this;
        Children.Add(child);
    }

    public virtual void RemoveChild(HtmlDomNode child)
    {
        if (Children.Remove(child)) child.Parent = null;
    }

    public virtual void InsertBefore(HtmlDomNode newNode, HtmlDomNode? existingNode)
    {
        if (existingNode == null) { AppendChild(newNode); return; }
        int index = Children.IndexOf(existingNode);
        if (index >= 0) { newNode.Parent = this; Children.Insert(index, newNode); }
    }

    public virtual void InsertAfter(HtmlDomNode newNode, HtmlDomNode? existingNode)
    {
        if (existingNode == null) { AppendChild(newNode); return; }
        int index = Children.IndexOf(existingNode);
        if (index >= 0) { newNode.Parent = this; Children.Insert(index + 1, newNode); }
    }

    public HtmlDomNode? NextSibling => Parent == null ? null : (Parent.Children.IndexOf(this) is int i && i >= 0 && i < Parent.Children.Count - 1 ? Parent.Children[i + 1] : null);
    public HtmlDomNode? PreviousSibling => Parent == null ? null : (Parent.Children.IndexOf(this) is int i && i > 0 ? Parent.Children[i - 1] : null);
    public HtmlDomNode? FirstChild => Children.Count > 0 ? Children[0] : null;
    public HtmlDomNode? LastChild => Children.Count > 0 ? Children[^1] : null;
    public bool HasChildren => Children.Count > 0;
}