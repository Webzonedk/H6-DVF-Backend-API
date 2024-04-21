namespace DVF_API.Data.Interfaces
{
    public interface ILocationRepository
    {
        internal int GetLocationCount();
        internal HashSet<string> GetLatitudesAndLongitudes();
    }
}
