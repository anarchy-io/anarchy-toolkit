using Anarchy.Toolkit.Hosting.Workers.Helper.Resource;
using AwesomeAssertions;
using System.Reflection;

namespace Anarchy.Toolkit.Tests.Unit.Hosting.Workers.Resources;

public class TextResourceReaderTests
{
    private const string _resourceFileName = "resource1.txt";
    private const string _resourceContents = "contents";
    private const string _subfolderResourceFileName = "resource2.txt";
    private const string _subfolderResourceContents = "subfoldercontents";

    [Fact]
    public void Read_FromAssemblySubfolder()
    {
        const string subfolder = "subfolder";
        var textResourceReader = new TextResourceReader(GetType().GetTypeInfo().Assembly, subfolder);

        var actual = textResourceReader.Read(_subfolderResourceFileName);

        actual.Should().Be(_subfolderResourceContents);
    }

    [Fact]
    public void Read_FromAssemblyBasePath()
    {
        var textResourceReader = new TextResourceReader(GetType().GetTypeInfo().Assembly);

        var actual = textResourceReader.Read(_resourceFileName);

        actual.Should().Be(_resourceContents);
    }

    [Fact]
    public void Read_FromAssemblyOfType()
    {
        var textResourceReader = new TextResourceReader(typeof(TextResourceReaderTests));

        var actual = textResourceReader.Read(_resourceFileName);

        actual.Should().Be(_resourceContents);
    }
}
