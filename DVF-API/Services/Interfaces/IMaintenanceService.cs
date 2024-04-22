namespace DVF_API.Services.Interfaces
{
    public interface IMaintenanceService
    {
        public void RemoveData(DateTime deleteDataBeforeThisDate);
        public void RestoreData();
    }
}
