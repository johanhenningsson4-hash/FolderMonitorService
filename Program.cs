
using System.ServiceProcess;

namespace FolderMonitorService
{
    static class Program
    {
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new FolderMonitorService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
