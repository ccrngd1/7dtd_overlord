using System; 

namespace lawsoncs.htg.sdtd.AdminServer
{
    public class CommandArguments
    {

        #region Public arguments consumed by the Command Line Parser classes
        [Argument(ArgumentType.Required, ShortName = "a", HelpText = "The action to take. Supported actions are install, uninstall and debug")]
        public string action;
        [Argument(ArgumentType.AtMostOnce, ShortName = "dn", HelpText = "The display name of the service that shows in the services control panel. For ZirMed usages, this is normally prefaced with 'ZirMed:'. Must be present if action is 'install'. ")]
        public string displayname;
        [Argument(ArgumentType.AtMostOnce, ShortName = "desc", HelpText = "The description of the service that shows in the control panel. Must be present if action is 'install'.")]
        public string description;
        [Argument(ArgumentType.AtMostOnce, ShortName = "sn", HelpText = "The short name of the service. Does not display anywhere. Usually the last part of the fully qualified application name. (ClpProcessor instead of ZirMed.Claims.ClpProcessor). Must be present if action is 'install'.")]
        public string servicename;
        [Argument(ArgumentType.AtMostOnce, ShortName = "user", HelpText = "The user credential to use when installing the service. If specified, needs to be in 'domain\\user' format.  If not specified, the LocalSystem account will be used.")]
        public string username;
        [Argument(ArgumentType.AtMostOnce, ShortName = "pass", HelpText = "The password to use when installing the service as a specific user. Must be supplied if a user name is supplied.")]
        public string password;
        [Argument(ArgumentType.AtMostOnce, ShortName = "start", DefaultValue = "Automatic", HelpText = "The startup type to use.  Acceptable values are Disabled, Manual and Automatic.")]
        public string starttype;
        #endregion

        #region Private variables
        private ActionType _actionType;
        private StartupType _startupType;
        #endregion

        #region Public/Internal variables
        internal ActionType ActionType
        {
            get
            {
                return _actionType;
            }
            set
            {
                _actionType = value;
            }
        }
        public StartupType StartupType
        {
            get
            {
                return _startupType;
            }
            set
            {
                _startupType = value;
            }
        }
        #endregion

        #region Constructor
        public CommandArguments()
        {
            _startupType = StartupType.Automatic;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Performs validations beyond just checking for correct command line arguments.
        /// </summary>
        /// <returns></returns>
        public bool DoExtendedValidations()
        {
            // Go validate that we have a good value for action
            if (!ValidateAction())
            {
                return false;
            }

            // If this is a debug or uninstall request, we don't need to go any further, as the rest of the items are not required.
            if (ActionType == ActionType.Debug)
            {
                return true;
            }
            if (ActionType == ActionType.Uninstall)
            {
                // Check to see if we have the required values for installing or uninstalling a service and if we don't, fail.
                if (!CheckRequiredInstallUninstallValues())
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            // Check to see if we have the required values for installing a service, and fail if we don't.
            if (!CheckRequiredInstallValues())
            {
                return false;
            }

            // Check to see if we have a username but not a password and fail if that is the case.
            if (!ValidatateUser())
            {
                return false;
            }

            //  Check the startup type to ensure it is correct.
            if (!ValidateStartType())
            {
                return false;
            }
            return true;

        }
        #endregion

        #region Private validation methods
        /// <summary>
        /// Validates that the action supplied on the command line is an allowable <see cref="ActionType"/>.
        /// </summary>
        /// <returns>true if action is valid, otherwise false.</returns>
        private bool ValidateAction()
        {
            bool actionOkay = true;
            if (string.IsNullOrEmpty(action))
            {
                Console.WriteLine("Action is a required parameter");
                Console.WriteLine(Parser.ArgumentsUsage(this.GetType()));
                actionOkay = false;
            }
            else
            {
                if (string.Compare(action, "install", true) == 0)
                {
                    ActionType = ActionType.Install;
                }
                else if (string.Compare(action, "uninstall", true) == 0)
                {
                    ActionType = ActionType.Uninstall;
                }
                else if (string.Compare(action, "debug", true) == 0)
                {
                    ActionType = ActionType.Debug;
                }
                else
                {
                    Console.WriteLine("Unrecognized value specified for Action: {0}", action);
                    Console.WriteLine(Parser.ArgumentsUsage(this.GetType()));
                    actionOkay = false;
                }
            }
            return actionOkay;
        }

        private bool CheckRequiredInstallUninstallValues()
        {
            // If we are doing an install, servicename name is required
            if (string.IsNullOrEmpty(servicename))
            {
                Console.WriteLine("Missing required parameter when action is install or uninstall: servicename");
                Console.WriteLine(Parser.ArgumentsUsage(this.GetType()));
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates that all necessary information is present when doing a service install.
        /// </summary>
        /// <returns>true if required data present, otherwise false.</returns>
        private bool CheckRequiredInstallValues()
        {
            // If we are doing an install, display name is required
            if (string.IsNullOrEmpty(displayname))
            {
                Console.WriteLine("Missing required parameter when action is install: displayname");
                Console.WriteLine(Parser.ArgumentsUsage(this.GetType()));
                return false;
            }

            // If we are doing an install, description name is required
            if (string.IsNullOrEmpty(description))
            {
                Console.WriteLine("Missing required parameter when action is install: description");
                Console.WriteLine(Parser.ArgumentsUsage(this.GetType()));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Confirms that is a user name is specified, a password is also specified.
        /// </summary>
        /// <returns>true is either username and password are present or username is not present, false if username is present but password is not.</returns>
        private bool ValidatateUser()
        {
            if (!string.IsNullOrEmpty(username) && string.IsNullOrEmpty(password))
            {
                Console.WriteLine("Missing required parameter when username specified: password");
                Console.WriteLine(Parser.ArgumentsUsage(this.GetType()));
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates that the starttype supplied on the command line is an allowable <see cref="StartUpType"/>.
        /// </summary>
        /// <returns>true if action is valid, otherwise false.</returns>
        private bool ValidateStartType()
        {
            bool validStartType = true;
            if (string.Compare(starttype, "automatic", true) == 0)
            {
                _startupType = StartupType.Automatic;
            }
            else if (string.Compare(starttype, "manual", true) == 0)
            {
                _startupType = StartupType.Manual;
            }
            else if (string.Compare(starttype, "disabled", true) == 0)
            {
                _startupType = StartupType.Disabled;
            }
            else
            {
                Console.WriteLine("Unrecognized value specified for Action: {0}", action);
                Console.WriteLine(Parser.ArgumentsUsage(this.GetType()));
                validStartType = false;
            }
            return validStartType;
        }
        #endregion

    }
}
