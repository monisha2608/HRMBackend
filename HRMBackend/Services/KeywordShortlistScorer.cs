namespace HRM.Backend.Services
{
    public class KeywordShortlistScorer : IShortlistScorer
    {
        private readonly IConfiguration _cfg;
        public KeywordShortlistScorer(IConfiguration cfg) { _cfg = cfg; }

        public (int score, string reason) Score(string jobDesc, string coverLetter)
        {
            var dict = _cfg.GetSection("Shortlist:Keywords").GetChildren()
                .ToDictionary(x => x.Key.ToLowerInvariant(), x => int.Parse(x.Value!));
            var text = $"{jobDesc}\n{coverLetter}".ToLowerInvariant();

            int total = 0; var hits = new List<string>();
            foreach (var kv in dict)
            {
                if (text.Contains(kv.Key))
                {
                    total += kv.Value;
                    hits.Add($"{kv.Key}+{kv.Value}");
                }
            }
            total = Math.Min(total, 100);
            return (total, "Hits: " + string.Join(", ", hits));
        }
    }
}
