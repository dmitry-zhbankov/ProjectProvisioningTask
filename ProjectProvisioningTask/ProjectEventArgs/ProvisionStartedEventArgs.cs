using Microsoft.SharePoint;

namespace Test.Project.Provisioning.ProjectEventArgs
{
    public class ProvisionStartedEventArgs : ProvisionEventArgs
    {
        public override string Action { get; } = "Provision started";

        public ProvisionStartedEventArgs(string url, SPUser user) : base(url, user)
        {
        }
    }
}
