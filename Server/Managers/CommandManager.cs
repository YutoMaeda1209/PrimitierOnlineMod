using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YuchiGames.POM.Shared;
using YuchiGames.POM.Shared.Utils;

namespace YuchiGames.POM.Server.Managers.CommandSystem
{
    public class CommandException : Exception
    {
        public string CommandName { get; set; }
        public CommandException(string command, string message) : base(message)
        {
            CommandName = command;
        }
    }

    public class CommandArgumentExpectedException : Exception
    {
        public CommandArgumentExpectedException(string message, Command.Argument argument) : base($"{message}, {argument.Source} taked.") { }
    }

    public class Command
    {
        public class Argument
        {
            public string Source { get; private set; }
            public Argument(string source)
            {
                Source = source;
            }

            public static implicit operator int(Argument from) =>
                int.TryParse(from.Source, out var value) ?
                value :
                throw new CommandArgumentExpectedException($"Integer requested", from);

            public static implicit operator int?(Argument from) =>
                int.TryParse(from.Source, out var value) ? value : null;

            public (int id, string value) OneOf(params string[] values) =>
                values.IndexOfOrDefault(Source)?.Let(id => (id, Source)) ??
                    throw new CommandArgumentExpectedException($"One of next values: [{string.Join(", ", values)}] requested", this);

        }
        public string Name { get; private set; }
        public Action<Argument[]> OnCommand { get; private set; } = delegate { };
        public Command(string name, Action<Argument[]> onCommand)
        {
            Name = name;
            OnCommand = onCommand;
        }
    }
    public class CommandManager : ICollection<Command>
    {
        public ILogger Log { get; set; } = new EmptyLogger();
        public List<Command> Commands { get; set; } = new();

        public int Count => Commands.Count;
        public bool IsReadOnly => false;

        public void Add(Command item) =>
            Commands.Add(item);
        public void Clear() =>
            Commands.Clear();
        public bool Contains(Command item) =>
            Commands.Contains(item);

        public void CopyTo(Command[] array, int arrayIndex) =>
            Commands.CopyTo(array, arrayIndex);
        public IEnumerator<Command> GetEnumerator() =>
            Commands.GetEnumerator();
        public bool Remove(Command item) =>
            Commands.Remove(item);
        IEnumerator IEnumerable.GetEnumerator() =>
            Commands.GetEnumerator();


        protected virtual void OnCommand(string fullCommand)
        {
            var parts = fullCommand.Split(' ');
            var commandName = parts.First();

            var command = Commands.FirstOrDefault(command => command.Name == commandName) ?? 
                throw new CommandException(commandName, $"Command \"{commandName}\" not found!");

            var arguments = parts.Skip(1)
                .Select(arg => new Command.Argument(arg))
                .ToArray();

            try
            {
                command.OnCommand(arguments);
            } catch (CommandArgumentExpectedException ex)
            { 
                throw new CommandException(commandName, $"Argument error: {ex.Message}");
            }
            
        }

        public void ProcessCommand(string command)
        {
            try
            {
                OnCommand(command);
            } catch (CommandException ex)
            {
                Log.Error($"Error while trying to invoke command {ex.CommandName}: {ex.Message}");
            }
        }

        public Task Start() => Task.Run(() =>
        {
            while (true)
            {
                ProcessCommand(Console.ReadLine() ?? "");
            }
        });
    }
}
