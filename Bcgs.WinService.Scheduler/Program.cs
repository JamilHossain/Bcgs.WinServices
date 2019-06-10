using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Bcgs.WinService.Scheduler
{
    class Program
    {
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly string _exePath = Assembly.GetExecutingAssembly().Location;
        internal static Object globalLock = new Object();
        static void Main(string[] args)
        {
            Console.Clear();



            BcgsService service = new BcgsService(logger);
            Boolean consoleRun = Environment.UserInteractive;
            if (args.Length > 0)
            {
                String cmd = args[0].Trim().Trim('-').Trim('/').Trim();
                switch (cmd)
                {
                    case "install":
                    case "i":
                        SelfInstall();
                        break;
                    case "uninstall":
                    case "u":
                        SelfUninstall();
                        break;
                    case "console":
                    case "c":
                        consoleRun = true;
                        break;
                    default:
                        break;
                }
            }
            else
            {
                if (consoleRun)
                {

                    logger.Info("Starting Console Service");
                    service.Start(args);

                    Console.ReadKey();
                    service.Stop();
                    logger.Info("Stopped Console Service");
                }
                else
                {
                    System.ServiceProcess.ServiceBase.Run(service);
                }

            }
        }

        static void SelfInstall()
        {
#if TRACE
            logger.Info("Program.SelfInstall");
#endif
            try
            {
                ManagedInstallerClass.InstallHelper(new string[] { _exePath });
                logger.Info("Service Installed");
            }
            catch (Exception e)
            {

                logger.Error("Could not self - install service", e);
            }
        }

        static void SelfUninstall()
        {

            logger.Info("Program.SelfUninstall");

            try
            {
                ManagedInstallerClass.InstallHelper(new string[] { "/u", _exePath });
                logger.Info("Service Uninstalled");
            }
            catch (Exception e)
            {

                logger.Error("Could not self - uninstall service", e);

            }
        }
    }
}
