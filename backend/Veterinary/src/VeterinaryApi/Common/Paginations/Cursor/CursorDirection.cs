namespace VeterinaryApi.Common.Paginations.Cursor;

/// <summary>Specifies the navigation direction for cursor-based pagination.</summary>
public enum CursorDirection : byte
{
    /// <summary>Navigate to the next (older) page of results.</summary>
    Next = 1,
    /// <summary>Navigate to the previous (newer) page of results.</summary>
    Prev = 2,
}
