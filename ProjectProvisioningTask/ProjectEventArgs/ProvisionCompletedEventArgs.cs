using Microsoft.SharePoint;
using ProjectProvisioningTask.Constants;

namespace ProjectProvisioningTask.ProjectEventArgs
{
    public class ProvisionCompletedEventArgs : ProvisionEventArgs
    {
        public override string Action { get; } = "Provision completed";

        public ProvisionResultStatus Status { get; }
    }
}
