namespace DVF_API.Services.Interfaces
{
    internal interface IMaintenanceService
    {
        public void RemoveData(DateTime deleteDataBeforeThisDate);
        public void RestoreData();
    }
}
