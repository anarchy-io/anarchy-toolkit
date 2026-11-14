using Anarchy.Toolkit.Hosting.Workers.Helper.Resource;
using AwesomeAssertions;
using System.Reflection;

namespace Anarchy.Toolkit.Tests.Unit.Hosting.Workers.Resources;

public class TextResourceReaderTests
{
    private const string ResourceFileName = "resource1.txt";
    private const string ResourceContents = "contents";
    private const string SubfolderResourceFileName = "resource2.txt";
    private const string SubfolderResourceContents = "subfoldercontents";

    [Fact]
    public void Read_FromAssemblySubfolder()
    {
        const string subfolder = "subfolder";
        var textResourceReader = new TextResourceReader(GetType().GetTypeInfo().Assembly, subfolder);

        var actual = textResourceReader.Read(SubfolderResourceFileName);

        actual.Should().Be(SubfolderResourceContents);
    }

    [Fact]
    public void Read_FromAssemblyBasePath()
    {
        var textResourceReader = new TextResourceReader(GetType().GetTypeInfo().Assembly);

        var actual = textResourceReader.Read(ResourceFileName);

        actual.Should().Be(ResourceContents);
    }

    [Fact]
    public void Read_FromAssemblyOfType()
    {
        var textResourceReader = new TextResourceReader(typeof(TextResourceReaderTests));

        var actual = textResourceReader.Read(ResourceFileName);

        actual.Should().Be(ResourceContents);
    }
}
