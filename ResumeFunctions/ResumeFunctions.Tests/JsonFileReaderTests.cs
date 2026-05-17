using ResumeFunctions.Models;
using ResumeFunctions.Tests.Helpers;
using System.Text.Json;
using Xunit;

namespace ResumeFunctions.Tests;

public class JsonFileReaderTests : IDisposable
{
    private readonly string _tempFile = Path.GetTempFileName();

    [Fact]
    public void Read_ValidJson_DeserializesCorrectly()
    {
        File.WriteAllText(_tempFile, TestData.ResumeJson);

        var result = ResumeApi.JsonFileReader.Read<DigitalResumeModel[]>(_tempFile);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Jane", result[0].FName);
        Assert.Equal("Doe", result[0].LName);
        Assert.Equal("Software Engineer", result[0].Position);
    }

    [Fact]
    public void Read_ValidJson_DeserializesNestedCollections()
    {
        File.WriteAllText(_tempFile, TestData.ResumeJson);

        var result = ResumeApi.JsonFileReader.Read<DigitalResumeModel[]>(_tempFile);

        Assert.NotNull(result);
        var resume = result[0];
        Assert.Equal(2, resume.Profile?.Count());
        Assert.Equal(2, resume.Contact?.Count());
        Assert.Single(resume.Education!);
        Assert.Single(resume.WorkExperience!);
        Assert.Single(resume.CustomSections!);
    }

    [Fact]
    public void Read_MissingFile_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() =>
            ResumeApi.JsonFileReader.Read<DigitalResumeModel[]>("does-not-exist.json"));
    }

    [Fact]
    public void Read_InvalidJson_ThrowsJsonException()
    {
        File.WriteAllText(_tempFile, "{ this is not valid json }");

        Assert.Throws<JsonException>(() =>
            ResumeApi.JsonFileReader.Read<DigitalResumeModel[]>(_tempFile));
    }

    [Fact]
    public void Read_EmptyArray_ReturnsEmptyCollection()
    {
        File.WriteAllText(_tempFile, "[]");

        var result = ResumeApi.JsonFileReader.Read<DigitalResumeModel[]>(_tempFile);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    public void Dispose() => File.Delete(_tempFile);
}
