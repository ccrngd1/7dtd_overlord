using System.Text.RegularExpressions;

namespace lawsoncs.htg.sdtd.AdminServer.commands
{
    interface ICommand
    {
        CommandResult Execute(string msg);
    }

    public struct CommandResult
    {
        public bool NeedsContinuation;
        public bool Success;
    }

    public abstract class CommandBase : ICommand
    {
        private string msgRegex;

        protected CommandBase() { }

        protected CommandBase(string msg)
        {
            msgRegex = msg;
        }

        public CommandResult Execute(string msg)
        {
            var r = new Regex(msgRegex);
            var split = r.Split(msg);

            return new CommandResult {NeedsContinuation = false, Success = false};
        }
    }
}
