using System.Collections.Generic;
using CoreForms.Ui.Core;

namespace CoreForms.Ui.Controls;

/// <summary>
/// Holds a collection of images that can be referenced by index.
/// </summary>
public class ImageList
{
    private readonly List<IGraphicsImage> _images = new();

    /// <summary>
    /// Gets the image at the specified index.
    /// </summary>
    /// <param name="index">Zero‑based index of the image.</param>
    /// <returns>The image at the specified index.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Index is out of range.</exception>
    public IGraphicsImage this[int index]
    {
        get => _images[index];
        set => _images[index] = value;
    }

    /// <summary>
    /// Adds an image to the list and returns its index.
    /// </summary>
    /// <param name="image">The image to add.</param>
    /// <returns>The index of the newly added image.</returns>
    public int Add(IGraphicsImage image)
    {
        _images.Add(image);
        return _images.Count - 1;
    }

    /// <summary>
    /// Gets the number of images in the list.
    /// </summary>
    public int Count => _images.Count;
}
