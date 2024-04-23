namespace DVF_API.Domain.Interfaces
{
    public interface IUtilityManager
    {
        void CleanUpRessources();
        int CalculateOptimalDegreeOfParallelism();
    }
}
