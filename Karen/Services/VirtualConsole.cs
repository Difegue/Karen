using Karen.Views;
using Microsoft.UI.Dispatching;
using System.Collections.ObjectModel;

namespace Karen.Services
{

    public class VirtualConsole
    {
        public readonly ObservableCollection<string> Lines = new();

        private readonly DispatcherQueue Dispatcher;
        private ConsoleWindow? ConsoleWindow;

        public VirtualConsole()
        {
            Dispatcher = DispatcherQueue.GetForCurrentThread();
        }

        public void AddLine(string? line)
        {
            if (line == null) return;

            Dispatcher.TryEnqueue(() =>
            {
                foreach (var line in line.Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries))
                    Lines.Add(line);
            });
        }

        public void ShowConsole()
        {
            if (ConsoleWindow != null)
            {
                ConsoleWindow.Activate();
            }
            else
            {
                ConsoleWindow = new();
                ConsoleWindow.Closed += (sender, e) =>
                {
                    ConsoleWindow = null;
                };
                ConsoleWindow.Activate();
            }
        }

        public void CloseConsole()
        {
            ConsoleWindow?.Close();
        }
    }
}
