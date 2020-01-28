using Microsoft.SharePoint;
using Test.Project.Provisioning.Constants;

namespace Test.Project.Provisioning.ProjectEventArgs
{
    public class ProvisionCompletedEventArgs : ProvisionEventArgs
    {
        public override string Action { get; } = "Provision completed";

        public ProvisionResultStatus Status { get; }

        public string Message { get; }

        public ProvisionCompletedEventArgs(string url, SPUser user, ProvisionResultStatus status, string message="") : base(url, user)
        {
            Status = status;
            Message = message;
        }
    }
}
