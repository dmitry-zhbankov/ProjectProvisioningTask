namespace ProjectProvisioningTask.ProjectEventArgs
{
    public class ProvisionStartedEventArgs : ProvisionEventArgs
    {
        public override string Action { get; } = "Provision started";
    }
}
