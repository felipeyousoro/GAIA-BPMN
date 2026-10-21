using System.Xml;
using System.Xml.Linq;

namespace BpmnProperties.Application;

public sealed record BpmnTask(string ExternalElementId, string? Name, string ElementType, string? Input, string? Output);

public sealed record BpmnDocument(string ExternalKey, string Name, XDocument Xml, IReadOnlyList<BpmnTask> Tasks);

public static class BpmnXml
{
    public const string BpmnNamespace = "http://www.omg.org/spec/BPMN/20100524/MODEL";
    public const string GaiaNamespace = "http://gaia-bpmn/schema/1.0";
    public const int MaxBytes = 10 * 1024 * 1024;

    public static BpmnDocument Parse(Stream stream, string? fileName = null)
    {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        using var bounded = new BoundedStream(stream, MaxBytes);
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };
        using var reader = XmlReader.Create(bounded, settings);
        var document = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
        var process = document.Descendants().FirstOrDefault(x => x.Name.LocalName == "process")
            ?? throw new InvalidDataException("BPMN document does not contain a process.");
        var externalKey = (string?)process.Attribute("id");
        if (string.IsNullOrWhiteSpace(externalKey))
        {
            externalKey = Path.GetFileNameWithoutExtension(fileName) ?? "process";
        }
        var tasks = process.Descendants()
            .Where(IsTask)
            .Select(element =>
            {
                var io = element.Descendants(XName.Get("io", GaiaNamespace)).FirstOrDefault();
                return new BpmnTask(
                    (string?)element.Attribute("id") ?? throw new InvalidDataException("BPMN task is missing an id."),
                    (string?)element.Attribute("name"),
                    element.Name.LocalName,
                    io?.Element(XName.Get("input", GaiaNamespace))?.Value,
                    io?.Element(XName.Get("output", GaiaNamespace))?.Value);
            })
            .ToList();
        return new BpmnDocument(externalKey, (string?)process.Attribute("name") ?? externalKey, document, tasks);
    }

    public static void ApplyTaskValues(XDocument document, IDictionary<string, (string? Input, string? Output)> values)
    {
        var definitions = document.Root ?? throw new InvalidDataException("BPMN document has no root element.");
        var processTasks = definitions.Descendants().Where(IsTask);
        var gaia = XNamespace.Get(GaiaNamespace);
        var bpmn = definitions.Name.Namespace;
        definitions.SetAttributeValue(XNamespace.Xmlns + "gaia", GaiaNamespace);
        foreach (var task in processTasks)
        {
            var id = (string?)task.Attribute("id");
            if (id is null || !values.TryGetValue(id, out var value)) continue;
            var extension = task.Element(bpmn + "extensionElements");
            if (extension is null)
            {
                extension = new XElement(bpmn + "extensionElements");
                task.AddFirst(extension);
            }
            var io = extension.Element(gaia + "io");
            if (io is null)
            {
                io = new XElement(gaia + "io");
                extension.Add(io);
            }
            SetElement(io, gaia + "input", value.Input);
            SetElement(io, gaia + "output", value.Output);
        }
    }

    public static string Serialize(XDocument document)
    {
        using var writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        document.Save(writer, SaveOptions.DisableFormatting);
        return writer.ToString().Replace("encoding=\"utf-16\"", "encoding=\"utf-8\"", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTask(XElement element) =>
        element.Name.NamespaceName == BpmnNamespace
        && (element.Name.LocalName.EndsWith("Task", StringComparison.Ordinal)
            || element.Name.LocalName is "task" or "subProcess" or "callActivity");

    private static void SetElement(XElement parent, XName name, string? value)
    {
        var element = parent.Element(name);
        if (string.IsNullOrEmpty(value))
        {
            element?.Remove();
            return;
        }
        if (element is null) parent.Add(new XElement(name, value));
        else element.Value = value;
    }

    private sealed class BoundedStream(Stream inner, long maxBytes) : Stream
    {
        private long _read;
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => inner.Length;
        public override long Position { get => inner.Position; set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));
        public override int Read(Span<byte> buffer)
        {
            var read = inner.Read(buffer);
            _read += read;
            if (_read > maxBytes) throw new InvalidDataException("BPMN file exceeds the maximum size.");
            return read;
        }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var read = inner.ReadAsync(buffer, cancellationToken);
            if (read.IsCompletedSuccessfully)
            {
                var count = read.Result;
                _read += count;
                if (_read > maxBytes) throw new InvalidDataException("BPMN file exceeds the maximum size.");
            }
            return TrackAsync(read);
        }
        private async ValueTask<int> TrackAsync(ValueTask<int> read)
        {
            var count = await read;
            if (!read.IsCompletedSuccessfully)
            {
                _read += count;
                if (_read > maxBytes) throw new InvalidDataException("BPMN file exceeds the maximum size.");
            }
            return count;
        }
        protected override void Dispose(bool disposing) { if (disposing) inner.Dispose(); base.Dispose(disposing); }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
