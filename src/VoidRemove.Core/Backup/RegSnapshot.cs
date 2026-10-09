namespace VoidRemove.Core.Backup;

/// <summary>Registry value storage kinds relevant to a .reg backup.</summary>
public enum RegValueKind
{
    Sz,
    ExpandSz,
    Dword,
    Qword,
    Binary,
    MultiSz,
}

/// <summary>A single registry value captured for backup.</summary>
public sealed record RegValueSnapshot(string Name, RegValueKind Kind, object? Data);

/// <summary>A recursive snapshot of a registry key and everything beneath it.</summary>
public sealed record RegKeySnapshot(
    string KeyPath,
    IReadOnlyList<RegValueSnapshot> Values,
    IReadOnlyList<RegKeySnapshot> SubKeys);
