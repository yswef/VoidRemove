using VoidRemove.Shared.Theming;
using Xunit;

namespace VoidRemove.Core.Tests;

public sealed class RgbaColorTests
{
    [Theory]
    [InlineData("#2EE6A8", 255, 0x2E, 0xE6, 0xA8)]
    [InlineData("2EE6A8", 255, 0x2E, 0xE6, 0xA8)]
    [InlineData("#222EE6A8", 0x22, 0x2E, 0xE6, 0xA8)]
    [InlineData("#000000", 255, 0, 0, 0)]
    [InlineData("#FFFFFF", 255, 255, 255, 255)]
    public void ParsesHexFormats(string hex, byte a, byte r, byte g, byte b)
    {
        RgbaColor color = RgbaColor.FromHex(hex);

        Assert.Equal(a, color.A);
        Assert.Equal(r, color.R);
        Assert.Equal(g, color.G);
        Assert.Equal(b, color.B);
    }

    [Fact]
    public void ExpandsShortHex()
    {
        RgbaColor color = RgbaColor.FromHex("#2EA");

        Assert.Equal(0x22, color.R);
        Assert.Equal(0xEE, color.G);
        Assert.Equal(0xAA, color.B);
        Assert.Equal((byte)255, color.A);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-color")]
    [InlineData("#12345")]
    [InlineData("#GGGGGG")]
    public void RejectsInvalidInput(string hex)
    {
        Assert.False(RgbaColor.TryFromHex(hex, out _));
        Assert.Throws<FormatException>(() => RgbaColor.FromHex(hex));
    }

    [Fact]
    public void RejectsNullInput()
    {
        Assert.False(RgbaColor.TryFromHex(null, out _));
    }

    [Fact]
    public void HexRoundTrips()
    {
        Assert.Equal("#2EE6A8", RgbaColor.FromHex("#2EE6A8").ToHex());
        Assert.Equal("#222EE6A8", RgbaColor.FromHex("#222EE6A8").ToHex());
    }

    [Fact]
    public void ArgbPacksChannelsInOrder()
    {
        Assert.Equal(0xFF2EE6A8u, RgbaColor.FromHex("#2EE6A8").ToArgb());
        Assert.Equal(0x222EE6A8u, RgbaColor.FromHex("#222EE6A8").ToArgb());
    }

    [Fact]
    public void ReadableForegroundFlipsWithLuminance()
    {
        Assert.Equal(new RgbaColor(11, 13, 17), RgbaColor.FromHex("#FFFFFF").ReadableForeground());
        Assert.Equal(new RgbaColor(255, 255, 255), RgbaColor.FromHex("#0B0E14").ReadableForeground());
    }

    [Fact]
    public void EqualityComparesAllChannels()
    {
        Assert.Equal(RgbaColor.FromHex("#2EE6A8"), RgbaColor.FromHex("#2EE6A8"));
        Assert.NotEqual(RgbaColor.FromHex("#2EE6A8"), RgbaColor.FromHex("#222EE6A8"));
        Assert.True(RgbaColor.FromHex("#2EE6A8") == RgbaColor.FromHex("#2EE6A8"));
        Assert.True(RgbaColor.FromHex("#2EE6A8") != RgbaColor.FromHex("#2EE6A9"));
    }

    [Fact]
    public void MixingClampsToTheByteRange()
    {
        RgbaColor mixed = ColorMath.Mix(new RgbaColor(0, 0, 0), new RgbaColor(255, 255, 255), 2);

        Assert.Equal(new RgbaColor(255, 255, 255), mixed);
        Assert.Equal(new RgbaColor(0, 0, 0), ColorMath.Mix(new RgbaColor(0, 0, 0), new RgbaColor(255, 255, 255), -1));
    }
}
