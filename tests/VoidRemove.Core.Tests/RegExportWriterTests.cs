using VoidRemove.Core.Backup;
using Xunit;

namespace VoidRemove.Core.Tests;

public sealed class RegExportWriterTests
{
    [Fact]
    public void WritesHeaderKeyAndTypedValues()
    {
        RegKeySnapshot key = new(
            @"HKEY_LOCAL_MACHINE\Software\VoidRemove",
            new List<RegValueSnapshot>
            {
                new(string.Empty, RegValueKind.Sz, "default"),
                new("Name", RegValueKind.Sz, @"C:\Path"),
                new("Count", RegValueKind.Dword, 5),
                new("Big", RegValueKind.Qword, 1L),
                new("Flags", RegValueKind.Binary, new byte[] { 0xDE, 0xAD }),
            },
            new List<RegKeySnapshot>
            {
                new(@"HKEY_LOCAL_MACHINE\Software\VoidRemove\Child",
                    new List<RegValueSnapshot> { new("On", RegValueKind.Dword, 1) },
                    Array.Empty<RegKeySnapshot>()),
            });

        string output = RegExportWriter.Write(key);

        Assert.StartsWith("Windows Registry Editor Version 5.00", output, StringComparison.Ordinal);
        Assert.Contains("[HKEY_LOCAL_MACHINE\\Software\\VoidRemove]", output, StringComparison.Ordinal);
        Assert.Contains("[HKEY_LOCAL_MACHINE\\Software\\VoidRemove\\Child]", output, StringComparison.Ordinal);
        Assert.Contains("@=\"default\"", output, StringComparison.Ordinal);
        Assert.Contains("\"Name\"=\"C:\\\\Path\"", output, StringComparison.Ordinal);
        Assert.Contains("\"Count\"=dword:00000005", output, StringComparison.Ordinal);
        Assert.Contains("\"Big\"=hex(b):01,00,00,00,00,00,00,00", output, StringComparison.Ordinal);
        Assert.Contains("\"Flags\"=hex:de,ad", output, StringComparison.Ordinal);
        Assert.Contains("\"On\"=dword:00000001", output, StringComparison.Ordinal);
    }

    [Fact]
    public void ExpandStringUsesUtf16Hex()
    {
        RegKeySnapshot key = new(
            @"HKEY_CURRENT_USER\Software\X",
            new List<RegValueSnapshot> { new("Dir", RegValueKind.ExpandSz, "A") },
            Array.Empty<RegKeySnapshot>());

        string output = RegExportWriter.Write(key);

        // "A\0" in UTF-16LE is 41 00 00 00.
        Assert.Contains("\"Dir\"=hex(2):41,00,00,00", output, StringComparison.Ordinal);
    }

    [Fact]
    public void MultipleKeysProduceMultipleSections()
    {
        RegKeySnapshot a = new("HKEY_LOCAL_MACHINE\\A", Array.Empty<RegValueSnapshot>(), Array.Empty<RegKeySnapshot>());
        RegKeySnapshot b = new("HKEY_LOCAL_MACHINE\\B", Array.Empty<RegValueSnapshot>(), Array.Empty<RegKeySnapshot>());

        string output = RegExportWriter.Write(new[] { a, b });

        Assert.Contains("[HKEY_LOCAL_MACHINE\\A]", output, StringComparison.Ordinal);
        Assert.Contains("[HKEY_LOCAL_MACHINE\\B]", output, StringComparison.Ordinal);
    }
}
