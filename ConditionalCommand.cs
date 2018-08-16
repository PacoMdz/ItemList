using System.Windows.Input;
using System.Diagnostics;
using System;

namespace Wb.TaskManager.App.Models
{
    public sealed class ConditionalCommand<T, U> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<U> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public ConditionalCommand(Action<T> execute, Predicate<U> canExecute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute", "Execute action can´t be null.");

            if (canExecute == null)
                throw new ArgumentNullException("canExecute", "Can execute action can´t be null.");

            _execute = execute;
            _canExecute = canExecute;
        }
        public void Execute(object p)
        {
            if (p is T) { _execute((T)p); }
            else
            {
                Debug.Fail("El parametro no se puede convertit a la clase especificada.",
                   $"El parametro es de tipo [{ p.GetType() }], se trato de convertir a [{ typeof(T) }].");
            }
        }
        public bool CanExecute(object p) => (p is U) ? _canExecute((U)p) : false;
    }

    public sealed class ConditionalCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public ConditionalCommand(Action<object> execute, Predicate<object> canExecute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute", "Execute action can´t be null.");

            if (canExecute == null)
                throw new ArgumentNullException("canExecute", "Can execute action can´t be null.");

            _execute = execute;
            _canExecute = canExecute;
        }
        public void Execute(object p) => _execute(p);
        public bool CanExecute(object p) => _canExecute(p);
    }
}
