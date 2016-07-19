using System;
using System.Windows.Input;

namespace ZXing.Net.Mobile.Presentation
{
    public class Command : ICommand
    {
        private readonly Action _execute;

        public Command(Action execute)
        {
            _execute = execute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            _execute?.Invoke();
        }
    }
}