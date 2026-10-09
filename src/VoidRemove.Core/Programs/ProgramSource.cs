namespace VoidRemove.Core.Programs;

/// <summary>Where an uninstall entry was found.</summary>
public enum ProgramSource
{
    /// <summary>HKLM, native (64-bit) view.</summary>
    Hklm,

    /// <summary>HKLM under WOW6432Node (32-bit programs on a 64-bit OS).</summary>
    Hklm32Bit,

    /// <summary>HKCU, per-user install.</summary>
    Hkcu,

    /// <summary>HKCU under WOW6432Node.</summary>
    Hkcu32Bit,
}

/// <summary>Detected architecture of an installed program.</summary>
public enum ProgramArchitecture
{
    Unknown,
    X86,
    X64,
    Arm64,
}
