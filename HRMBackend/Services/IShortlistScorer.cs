namespace HRM.Backend.Services
{
    public interface IShortlistScorer
    {
        (int score, string reason) Score(string jobDesc, string coverLetter);
    }
}
