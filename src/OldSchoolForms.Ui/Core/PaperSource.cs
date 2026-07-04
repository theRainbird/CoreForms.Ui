namespace OldSchoolForms.Ui.Core;

/// <summary>
/// Specifies the paper source (tray) for a print job.
/// </summary>
public class PaperSource
{
    /// <summary>
    /// Initializes a new PaperSource instance.
    /// </summary>
    /// <param name="name">The name of the paper source.</param>
    /// <param name="kind">The kind of paper source.</param>
    public PaperSource(string name, PaperSourceKind kind = PaperSourceKind.AutomaticFeed)
    {
        SourceName = name;
        Kind = kind;
    }

    /// <summary>
    /// Gets or sets the name of the paper source.
    /// </summary>
    public string SourceName { get; set; }

    /// <summary>
    /// Gets or sets the kind of paper source.
    /// </summary>
    public PaperSourceKind Kind { get; set; }

    /// <summary>
    /// The default automatic paper source.
    /// </summary>
    public static PaperSource Default => new("Automatic", PaperSourceKind.AutomaticFeed);

    public override string ToString() => SourceName;
}

/// <summary>
/// Standard paper source kinds.
/// </summary>
public enum PaperSourceKind
{
    /// <summary>
    /// Upper paper tray.
    /// </summary>
    Upper = 1,

    /// <summary>
    /// Lower paper tray.
    /// </summary>
    Lower = 2,

    /// <summary>
    /// Middle paper tray.
    /// </summary>
    Middle = 3,

    /// <summary>
    /// Manual paper feed.
    /// </summary>
    Manual = 4,

    /// <summary>
    /// Envelope paper tray.
    /// </summary>
    Envelope = 5,

    /// <summary>
    /// Manual envelope feed.
    /// </summary>
    ManualEnvelope = 6,

    /// <summary>
    /// Automatic paper feed.
    /// </summary>
    AutomaticFeed = 7,

    /// <summary>
    /// Tractor paper feed.
    /// </summary>
    TractorFeed = 8,

    /// <summary>
    /// Small format paper tray.
    /// </summary>
    SmallFormat = 9,

    /// <summary>
    /// Large paper tray.
    /// </summary>
    LargeFormat = 10,

    /// <summary>
    /// Large capacity paper tray.
    /// </summary>
    LargeCapacity = 11,

    /// <summary>
    /// Cassette paper tray.
    /// </summary>
    Cassette = 14,

    /// <summary>
    /// Custom paper source.
    /// </summary>
    Custom = 257
}
