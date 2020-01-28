using Microsoft.SharePoint;

namespace Test.Project.Provisioning.ProjectEventArgs
{
    public class GroupsCreatingEventArgs : ProvisionEventArgs
    {
        public override string Action { get; } = "Creating groups";

        public GroupsCreatingEventArgs(string url, SPUser user) : base(url, user)
        {
        }
    }
}
