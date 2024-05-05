namespace DVF_API.Services.Interfaces
{
    /// <summary>
    /// This interface connects the maintenance controller with the maintenance service
    /// </summary>
    public interface IMaintenanceService
    {
        public void RemoveData(DateTime deleteDataBeforeThisDate);
        public void RestoreData();
    }
}
