using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Refline.Models;
using System.Windows.Threading;

namespace Refline.Services
{
    public class WindowTracker
    {
        private static readonly HashSet<string> ReflineProcessNames = new(StringComparer.Ordinal)
        {
            "Refline",
            "ReflineAdmin"
        };

        private static readonly HashSet<string> ReflineExecutableNames = new(StringComparer.Ordinal)
        {
            "Refline.exe",
            "ReflineAdmin.exe"
        };

        // Импорт функции GetForegroundWindow из библиотеки user32.dll
        // Эта функция возвращает дескриптор (handle) активного окна, с которым в данный момент работает пользователь.
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        // Импорт функции GetWindowText из библиотеки user32.dll
        // Эта функция позволяет получить текст (обычно это заголовок) окна по его дескриптору.
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        // Таймер для периодической проверки активного окна каждую секунду.
        // Используем DispatcherTimer, чтобы безопасно обновлять UI, если потребуется, и работать в основном потоке WPF.
        private readonly DispatcherTimer _timer;

        private string _lastWindowTitle = string.Empty;
        private int _secondsSinceLastChange = 0;
        private const int IdleTimeoutSeconds = 5 * 60; // 5 минут без изменения окна = простой (Idle)

        // Событие, которое вызывается каждую секунду для передачи данных об активном окне
        public event Action<TrackedWindowInfo> OnWindowTracked = delegate { };

        public WindowTracker()
        {
            // Инициализация таймера с интервалом в 1 секунду.
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            
            // Привязка обработчика события, который будет вызываться при каждом тике таймера (каждую секунду).
            _timer.Tick += Timer_Tick;
        }

        public void Start()
        {
            _lastWindowTitle = string.Empty;
            _secondsSinceLastChange = 0;
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            string currentTitle = GetActiveWindowTitle();

            if (currentTitle == _lastWindowTitle)
            {
                // Если заголовок не изменился, увеличиваем счетчик времени.
                _secondsSinceLastChange++;
            }
            else
            {
                // Если заголовок изменился, сбрасываем счетчик и запоминаем новый заголовок.
                _lastWindowTitle = currentTitle;
                _secondsSinceLastChange = 0;
            }

            // Если время с последнего изменения превышает 5 минут, считаем что пользователь бездействует (Idle).
            bool isIdle = _secondsSinceLastChange >= IdleTimeoutSeconds;

            if (isIdle)
            {
                // Передаем статус "простоя", чтобы таймер времени на приложение был "на паузе".
                OnWindowTracked(new TrackedWindowInfo
                {
                    WindowTitle = "Idle",
                    IsIdle = true
                });
            }
            else
            {
                var windowInfo = GetActiveWindowInfo(currentTitle);
                OnWindowTracked(windowInfo);
            }
        }

        // Вспомогательный метод для получения заголовка активного окна с использованием WinAPI.
        private string GetActiveWindowTitle()
        {
            IntPtr hWnd = GetForegroundWindow();
            if (hWnd == IntPtr.Zero)
            {
                return string.Empty; // Нет активного окна
            }

            const int nChars = 256;
            StringBuilder buff = new StringBuilder(nChars);
            
            if (GetWindowText(hWnd, buff, nChars) > 0)
            {
                return buff.ToString();
            }

            return string.Empty;
        }

        private TrackedWindowInfo GetActiveWindowInfo(string currentTitle)
        {
            IntPtr hWnd = GetForegroundWindow();
            string processName = string.Empty;
            string executableName = string.Empty;

            if (hWnd != IntPtr.Zero)
            {
                try
                {
                    GetWindowThreadProcessId(hWnd, out var processId);
                    if (processId > 0)
                    {
                        using var process = Process.GetProcessById((int)processId);
                        processName = process.ProcessName ?? string.Empty;
                        executableName = process.MainModule?.ModuleName ?? string.Empty;
                    }
                }
                catch
                {
                }
            }

            string titleToReport = string.IsNullOrWhiteSpace(currentTitle) ? "Unknown/Desktop" : currentTitle;
            var ignoreReason = GetReflineIgnoreReason(processName, executableName);

            return new TrackedWindowInfo
            {
                WindowTitle = titleToReport,
                ProcessName = processName,
                ExecutableName = executableName,
                IsIdle = false,
                IsReflineOwnedWindow = ignoreReason != null,
                IgnoreReason = ignoreReason
            };
        }

        private static string? GetReflineIgnoreReason(string processName, string executableName)
        {
            var process = processName ?? string.Empty;
            var executable = executableName ?? string.Empty;

            // Служебные окна Refline определяем только по имени процесса/EXE.
            // Заголовок окна использовать нельзя: IDE и другие приложения могут
            // содержать "Refline" в WindowTitle и должны корректно трекаться.
            if (ReflineProcessNames.Contains(process) || ReflineExecutableNames.Contains(executable))
            {
                return "process/exe относится к Refline";
            }

            return null;
        }
    }
}
