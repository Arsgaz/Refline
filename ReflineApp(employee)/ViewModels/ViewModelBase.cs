using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Refline.ViewModels
{
    // Базовый класс для всех ViewModel. Реализует INotifyPropertyChanged,
    // чтобы уведомлять UI об изменениях свойств и автоматически обновлять интерфейс.
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        // Метод для вызова события PropertyChanged. Атрибут CallerMemberName
        // позволяет автоматически подставить имя вызывающего свойства.
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Вспомогательный метод для обновления значения поля и вызова OnPropertyChanged,
        // если значение действительно изменилось.
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
