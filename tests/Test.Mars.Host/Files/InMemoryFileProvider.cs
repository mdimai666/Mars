using System.Text;
using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Test.Mars.Host.Files;

/// <summary>
/// https://source.dot.net/#Microsoft.AspNetCore.Components.WebView.Test/StaticContentProviderTests.cs,44
/// </summary>
internal sealed class InMemoryFileProvider : IFileProvider
{
    
    public InMemoryFileProvider(IDictionary<string, string> filePathsAndContents)
    {
        ArgumentNullException.ThrowIfNull(filePathsAndContents);

        FilePathsAndContents = filePathsAndContents;
    }

    public IDictionary<string, string> FilePathsAndContents { get; }

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        return new InMemoryDirectoryContents(this, subpath);
    }

    public IFileInfo GetFileInfo(string subpath)
    {
        return FilePathsAndContents
            .Where(kvp => kvp.Key == subpath)
            .Select(x => new InMemoryFileInfo(x.Key, x.Value))
            .Single();
    }

    public IChangeToken Watch(string filter)
    {
        throw new NotImplementedException();
    }

    private sealed class InMemoryDirectoryContents : IDirectoryContents
    {
        private readonly InMemoryFileProvider _inMemoryFileProvider;
        private readonly string _subPath;

        public InMemoryDirectoryContents(InMemoryFileProvider inMemoryFileProvider, string subPath)
        {
            _inMemoryFileProvider = inMemoryFileProvider ?? throw new ArgumentNullException(nameof(inMemoryFileProvider));
            _subPath = subPath ?? throw new ArgumentNullException(nameof(inMemoryFileProvider));
        }

        public bool Exists => true;

        public IEnumerator<IFileInfo> GetEnumerator()
        {
            return
                _inMemoryFileProvider
                    .FilePathsAndContents
                    .Where(kvp => kvp.Key.StartsWith(_subPath, StringComparison.Ordinal))
                    .Select(x => new InMemoryFileInfo(x.Key, x.Value))
                    .GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private sealed class InMemoryFileInfo : IFileInfo
    {
        private readonly string _filePath;
        private readonly string _fileContents;

        public InMemoryFileInfo(string filePath, string fileContents)
        {
            _filePath = filePath;
            _fileContents = fileContents;
        }

        public bool Exists => true;

        public long Length => _fileContents.Length;

        public string PhysicalPath => null!;

        public string Name => Path.GetFileName(_filePath);

        public DateTimeOffset LastModified => DateTimeOffset.Now;

        public bool IsDirectory => false;

        public Stream CreateReadStream()
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(_fileContents));
        }
    }
}
