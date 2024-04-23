namespace DVF_API.Domain.Interfaces
{
    public interface IUtilityManager
    {
        bool Authenticate(string password, string clientIp);
        void CleanUpRessources();
        int CalculateOptimalDegreeOfParallelism();
    }
}
