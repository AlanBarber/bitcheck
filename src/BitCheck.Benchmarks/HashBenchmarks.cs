using System.IO.Hashing;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Blake3;

namespace BitCheck.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(Config))]
public class HashBenchmarks
{
    private class Config : ManualConfig
    {
        public Config()
        {
            SummaryStyle = SummaryStyle.Default.WithRatioStyle(RatioStyle.Trend);
            AddColumn(new ThroughputColumn());
        }
    }

    private class ThroughputColumn : IColumn
    {
        public string Id => "Throughput";
        public string ColumnName => "Throughput";
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Custom;
        public int PriorityInCategory => 0;
        public bool IsNumeric => true;
        public UnitType UnitType => UnitType.Dimensionless;
        public string Legend => "Data throughput in MB/s";

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
        {
            var report = summary[benchmarkCase];
            if (report?.ResultStatistics == null)
                return "N/A";

            var meanNs = report.ResultStatistics.Mean;
            var sizeParam = benchmarkCase.Parameters["DataSizeMB"];
            if (sizeParam == null)
                return "N/A";

            var sizeMb = (int)sizeParam;
            var throughputMBps = sizeMb / (meanNs / 1_000_000_000.0);
            return $"{throughputMBps:F2} MB/s";
        }

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) 
            => GetValue(summary, benchmarkCase);

        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
        public bool IsAvailable(Summary summary) => true;
    }

    [Params(1, 10, 100, 1000)]
    public int DataSizeMB { get; set; }

    private byte[] _data = null!;
    private MemoryStream _stream = null!;

    [GlobalSetup]
    public void Setup()
    {
        _data = new byte[DataSizeMB * 1024 * 1024];
        Random.Shared.NextBytes(_data);
        _stream = new MemoryStream(_data);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _stream.Dispose();
    }

    private void ResetStream() => _stream.Position = 0;

    [Benchmark(Baseline = true)]
    public byte[] XxHash64()
    {
        ResetStream();
        var hasher = new XxHash64();
        hasher.Append(_stream);
        return hasher.GetCurrentHash();
    }

    [Benchmark]
    public byte[] MD5Hash()
    {
        ResetStream();
        return MD5.HashData(_stream);
    }

    [Benchmark]
    public byte[] SHA256Hash()
    {
        ResetStream();
        return SHA256.HashData(_stream);
    }

    [Benchmark]
    public byte[] Blake3Hash()
    {
        ResetStream();
        using var hasher = Hasher.New();
        Span<byte> buffer = stackalloc byte[81920];
        int bytesRead;
        while ((bytesRead = _stream.Read(buffer)) > 0)
        {
            hasher.Update(buffer[..bytesRead]);
        }
        return hasher.Finalize().AsSpan().ToArray();
    }
}
