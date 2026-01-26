using System.ComponentModel;
using System.ServiceProcess;

namespace FolderMonitorService
{
    [RunInstaller(true)]
    public class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            var serviceProcessInstaller = new ServiceProcessInstaller
            {
                Account = ServiceAccount.LocalSystem
            };

            var serviceInstaller = new ServiceInstaller
            {
                ServiceName = "FolderMonitorService",
                DisplayName = "Folder Monitor Service",
                Description = "Övervakar en mapp och skickar e-post om inga filer kommer in under angiven tid.",
                StartType = ServiceStartMode.Automatic
            };

            Installers.Add(serviceProcessInstaller);
            Installers.Add(serviceInstaller);
        }
    }
}
