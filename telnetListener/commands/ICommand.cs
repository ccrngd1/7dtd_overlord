using System.Linq;
using System.Text.RegularExpressions;

namespace lawsoncs.htg.sdtd.AdminServer.commands
{
    interface ICommand
    {
        string Execute(string msg);
    }

    public sealed class CommandBase : ICommand
    {
        private string msgRegex;

        private CommandBase() { }

        public CommandBase(string msg)
        {
            msgRegex = msg;
        }

        public string Execute(string msg)
        {
            var r = new Regex(msgRegex);
            var split = r.Split(msg);

            return split.Length > 1 ? split[1] : null;
        }
    }
}
