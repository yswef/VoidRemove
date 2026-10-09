using VoidRemove.Core.Primitives;
using Xunit;

namespace VoidRemove.Core.Tests;

public sealed class ResultTests
{
    [Fact]
    public void OkIsSuccessful()
    {
        Result result = Result.Ok();

        Assert.True(result.Success);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
        Assert.Null(result.Exception);
    }

    [Fact]
    public void FailCarriesTheReason()
    {
        InvalidOperationException exception = new("locked");
        Result result = Result.Fail("The file is in use.", exception);

        Assert.False(result.Success);
        Assert.True(result.IsFailure);
        Assert.Equal("The file is in use.", result.Error);
        Assert.Same(exception, result.Exception);
        Assert.Contains("The file is in use.", result.ToString());
    }

    [Fact]
    public void GenericOkCarriesTheValue()
    {
        Result<long> result = Result<long>.Ok(4096);

        Assert.True(result.Success);
        Assert.Equal(4096, result.Value);
    }

    [Fact]
    public void GenericFailHasNoValue()
    {
        Result<long> result = Result<long>.Fail("not found");

        Assert.True(result.IsFailure);
        Assert.Equal(default, result.Value);
        Assert.Equal("not found", result.Error);
    }

    [Fact]
    public void APlainResultConvertsToTheGenericForm()
    {
        Result<long> fromOk = Result.Ok();
        Result<long> fromFail = Result.Fail("nope");

        Assert.True(fromOk.Success);
        Assert.False(fromFail.Success);
        Assert.Equal("nope", fromFail.Error);
    }

    [Fact]
    public void ResultsCompareByValue()
    {
        Assert.Equal(Result.Ok(), Result.Ok());
        Assert.Equal(Result.Fail("x"), Result.Fail("x"));
        Assert.NotEqual(Result.Ok(), Result.Fail("x"));
    }
}
