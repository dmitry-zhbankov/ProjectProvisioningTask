using Microsoft.SharePoint;

namespace Test.Project.Provisioning.ProjectEventArgs
{
    public class ListsCreatingEventArgs:ProvisionEventArgs
    {
        public override string Action { get; } = "Creating lists";

        public ListsCreatingEventArgs(string url, SPUser user) : base(url, user)
        {
        }
    }
}
