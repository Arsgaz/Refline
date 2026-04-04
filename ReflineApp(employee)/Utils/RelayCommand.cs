using System;
using System.Windows.Input;

namespace Refline.Utils
{
    // Класс RelayCommand реализует интерфейс ICommand, необходимый для привязки (биндинга)
    // кнопок и других элементов UI к методам в ViewModel.
    // Это позволяет отделить логику выполнения действий от самих элементов управления (View).
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        // Событие, которое сообщает UI, что нужно перепроверить, может ли команда выполняться (CanExecute)
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        // Конструктор принимает метод для выполнения (execute) и опциональный метод проверки (canExecute)
        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
            : this(_ => execute(), canExecute != null ? _ => canExecute() : null)
        {
        }

        // Определяет, можно ли в данный момент выполнить команду.
        // Если _canExecute не задан, возвращает true (команда всегда доступна).
        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        // Выполняет логику команды.
        public void Execute(object? parameter)
        {
            _execute(parameter);
        }
    }
}
