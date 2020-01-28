using Microsoft.SharePoint;

namespace Test.Project.Provisioning.ProjectEventArgs
{
    public class SubWebCreatingEventArgs : ProvisionEventArgs
    {
        public override string Action { get; } = "SubWeb creating";

        public SubWebCreatingEventArgs(string url, SPUser user):base(url,user)
        {
        }
    }
}
