namespace Triton.Benchmarks {
    public interface IBenchmark {
        bool Enabled { get; }
        string Name { get; }
    }
}
