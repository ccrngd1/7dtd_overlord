using System;
using System.Collections;
using System.Configuration.Install;
using System.Diagnostics;
using System.Reflection;

namespace lawsoncs.htg.sdtd.AdminServer
{
    public class ServiceBase : System.ServiceProcess.ServiceBase
    {
        public event EventHandler<InstallEventArgs> ServiceInstalled;

        public event EventHandler<InstallEventArgs> ServiceUninstalled;

        protected override void OnStart(string[] args)
        {
            throw new NotImplementedException();
        }

        protected override void OnStop()
        {
            throw new NotImplementedException();
        }

        protected void Execute(string[] args, StartLocal startDebug, StopLocal stopDebug)
        {
            this.Execute(args, startDebug, stopDebug, Assembly.GetCallingAssembly());
        }

        protected void Execute(string[] args, StartLocal startDebug, StopLocal stopDebug, Assembly executingAssembly)
        {
            if (args.Length > 0)
            {
                CommandArguments commandArguments = new CommandArguments();
                if (!Parser.ParseArgumentsWithUsage(args, (object)commandArguments) || !commandArguments.DoExtendedValidations())
                    return;
                if (commandArguments.ActionType == ActionType.Debug)
                    ServiceBase.RunDebug(startDebug, stopDebug);
                else
                    this.RunInstallTask(commandArguments, executingAssembly);
            }
            else
                System.ServiceProcess.ServiceBase.Run((System.ServiceProcess.ServiceBase[])new ServiceBase[1]
        {
          this
        });
        }

        private static void RunDebug(StartLocal startDebug, StopLocal stopDebug)
        {
            try
            {
                if (startDebug == null)
                    throw new ArgumentNullException("startDebug");
                if (stopDebug == null)
                    throw new ArgumentNullException("stopDebug");
                Trace.Listeners.Add((TraceListener)new TextWriterTraceListener(Console.Out));
                Console.WriteLine("Attempting to start service. Hit Enter to stop the service");
                startDebug(new string[0]);
                Console.ReadKey();
                stopDebug();
                Trace.WriteLine("Debug finishing.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void RunInstallTask(CommandArguments commandArguments, Assembly executingAssembly)
        {
            try
            {
                TransactedInstaller transactedInstaller = new TransactedInstaller();
                GenericInstaller genericInstaller = new GenericInstaller(commandArguments);
                genericInstaller.AfterInstall += new InstallEventHandler(this.OnAfterInstallBase);
                genericInstaller.AfterUninstall += new InstallEventHandler(this.OnAfterUninstallBase);
                transactedInstaller.Installers.Add((Installer)genericInstaller);
                InstallContext installContext = new InstallContext("", new string[1]
        {
          string.Format("/assemblypath={0}", (object) executingAssembly.Location)
        });
                transactedInstaller.Context = installContext;
                if (commandArguments.ActionType == ActionType.Install)
                    transactedInstaller.Install((IDictionary)new Hashtable());
                else
                    transactedInstaller.Uninstall((IDictionary)null);
            }
            catch
            {
                Console.WriteLine("Error performing " + ((object)commandArguments.ActionType).ToString() + " task");
            }
        }

        private void OnAfterInstallBase(object sender, InstallEventArgs e)
        {
            if (this.ServiceInstalled == null)
                return;
            this.ServiceInstalled(sender, e);
        }

        private void OnAfterUninstallBase(object sender, InstallEventArgs e)
        {
            if (this.ServiceUninstalled == null)
                return;
            this.ServiceUninstalled(sender, e);
        }
    }


    public delegate void StartLocal(string[] args);
    public delegate void StopLocal();

    public enum StartupType
    {
        None = 0,
        Automatic = 2,
        Manual,
        Disabled
    }

    internal enum ActionType
    {
        Install = 0,
        Uninstall,
        Debug
    }
}