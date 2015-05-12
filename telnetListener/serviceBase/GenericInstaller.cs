using System.Configuration.Install; 
using System.ServiceProcess;

namespace lawsoncs.htg.sdtd.AdminServer
{
    internal class GenericInstaller : Installer
    {
        private ServiceInstaller _serviceInstaller;
        private ServiceProcessInstaller _processInstaller;

        internal GenericInstaller(CommandArguments arguments)
        {
            _processInstaller = new ServiceProcessInstaller();
            _serviceInstaller = new ServiceInstaller();

            _serviceInstaller.ServiceName = arguments.servicename;
            if (arguments.ActionType == ActionType.Install)
            {
                _serviceInstaller.DisplayName = arguments.displayname;
                _serviceInstaller.Description = arguments.description + "(" + arguments.servicename + ")";
                _serviceInstaller.StartType = (ServiceStartMode)arguments.StartupType;
                if (!string.IsNullOrEmpty(arguments.username))
                {
                    _processInstaller.Account = ServiceAccount.User;
                    _processInstaller.Username = arguments.username;
                    _processInstaller.Password = arguments.password;
                }
                else
                {
                    _processInstaller.Account = ServiceAccount.LocalSystem;
                }
            }

            Installers.Add(_serviceInstaller);
            Installers.Add(_processInstaller);
        }
    }
}
