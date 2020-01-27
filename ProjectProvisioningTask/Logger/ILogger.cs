namespace ProjectProvisioningTask.Logger
{
    public interface ILogger
    {
        void LogError(string message);

        void LogWarning(string message);

        void LogInformation(string message);
    }
}