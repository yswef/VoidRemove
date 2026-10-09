using System.Text;

namespace VoidRemove.Core.Backup;

/// <summary>
/// Serializes <see cref="RegKeySnapshot"/> trees into the standard ".reg" format so any
/// removed key can be restored by double-clicking the backup. Pure and unit tested.
/// </summary>
public static class RegExportWriter
{
    private const int LineLimit = 80;

    public static string Write(RegKeySnapshot root) => Write(new[] { root });

    public static string Write(IReadOnlyList<RegKeySnapshot> roots)
    {
        StringBuilder builder = new();
        builder.AppendLine("Windows Registry Editor Version 5.00");
        builder.AppendLine();

        foreach (RegKeySnapshot root in roots)
        {
            AppendKey(builder, root);
        }

        return builder.ToString();
    }

    private static void AppendKey(StringBuilder builder, RegKeySnapshot key)
    {
        builder.Append('[').Append(key.KeyPath).AppendLine("]");

        foreach (RegValueSnapshot value in key.Values)
        {
            AppendValue(builder, value);
        }

        builder.AppendLine();

        foreach (RegKeySnapshot subKey in key.SubKeys)
        {
            AppendKey(builder, subKey);
        }
    }

    private static void AppendValue(StringBuilder builder, RegValueSnapshot value)
    {
        string name = value.Name.Length == 0 ? "@" : "\"" + EscapeName(value.Name) + "\"";

        switch (value.Kind)
        {
            case RegValueKind.Sz:
                builder.Append(name).Append("=\"").Append(EscapeString(Convert.ToString(value.Data) ?? string.Empty)).AppendLine("\"");
                break;

            case RegValueKind.Dword:
                builder.Append(name).Append("=dword:")
                    .AppendFormat("{0:x8}", Convert.ToInt64(value.Data ?? 0)).AppendLine();
                break;

            case RegValueKind.ExpandSz:
                AppendHex(builder, name, "hex(2)", Encoding.Unicode.GetBytes((Convert.ToString(value.Data) ?? string.Empty) + "\0"));
                break;

            case RegValueKind.MultiSz:
                string[] items = value.Data as string[] ?? Array.Empty<string>();
                string joined = string.Join("\0", items) + "\0\0";
                AppendHex(builder, name, "hex(7)", Encoding.Unicode.GetBytes(joined));
                break;

            case RegValueKind.Qword:
                AppendHex(builder, name, "hex(b)", BitConverter.GetBytes(Convert.ToInt64(value.Data ?? 0)));
                break;

            case RegValueKind.Binary:
                AppendHex(builder, name, "hex", value.Data as byte[] ?? Array.Empty<byte>());
                break;
        }
    }

    private static void AppendHex(StringBuilder builder, string name, string prefix, byte[] bytes)
    {
        string hex = prefix + ":" + string.Join(",", bytes.Select(static b => b.ToString("x2")));

        builder.Append(name).Append('=');

        int width = name.Length + 1;
        bool first = true;

        foreach (string chunk in SplitForWrap(hex, LineLimit - width))
        {
            if (!first)
            {
                builder.AppendLine(" \\");
                builder.Append("  ");
            }

            builder.Append(chunk);
            first = false;
        }

        builder.AppendLine();
    }

    private static IEnumerable<string> SplitForWrap(string text, int firstChunk)
    {
        int take = firstChunk;
        int index = 0;
        while (index < text.Length)
        {
            int length = Math.Min(take, text.Length - index);

            // Never split in the middle of a "aa," byte triplet.
            if (length < text.Length - index && length >= 2 && text[index + length - 1] == ',')
            {
                length -= 1;
            }

            yield return text.Substring(index, length);
            index += length;
            take = LineLimit - 2;
        }
    }

    private static string EscapeName(string name) => name.Replace("\"", "\\\"");

    private static string EscapeString(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
