namespace HRM.Backend.Services
{
    public interface IVirusScanner
    {
        Task<bool> IsCleanAsync(Stream stream, string fileName);
    }

    public class NoopScanner : IVirusScanner
    {
        public Task<bool> IsCleanAsync(Stream stream, string fileName) => Task.FromResult(true);
    }
}
