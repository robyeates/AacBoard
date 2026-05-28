namespace AacBoard.Models;

/// <summary>
/// Zero-based (row, column) address of a cell on a communication board.
/// </summary>
/// <remarks>
/// Java note: a textbook Java record equivalent — primary constructor,
/// structural equality, and <c>ToString()</c> all for free.
/// </remarks>
public sealed record GridPosition(int Row, int Column)
{
    public override string ToString() => $"({Row},{Column})";
}