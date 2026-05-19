using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Refline.Models;

public class AppActivity : INotifyPropertyChanged
{
    private int _id;
    private string _appName = string.Empty;
    private string _windowTitle = string.Empty;
    private ActivityCategory _category = ActivityCategory.Unknown;
    private ActivityClassificationSource _classificationSource = ActivityClassificationSource.FallbackUnknown;
    private long? _matchedRuleId;
    private string _matchedRuleDescription = string.Empty;
    private bool _isIdle;
    private bool _isProductive;
    private int _timeSpentSeconds;
    private DateTime _lastActive;
    private DateTime _activityDate = DateTime.Today;
    private int _version;

    public int Id
    {
        get => _id;
        set
        {
            if (_id != value)
            {
                _id = value;
                OnPropertyChanged();
            }
        }
    }

    public string AppName
    {
        get => _appName;
        set
        {
            if (_appName != value)
            {
                _appName = value;
                OnPropertyChanged();
            }
        }
    }

    public string WindowTitle
    {
        get => _windowTitle;
        set
        {
            if (_windowTitle != value)
            {
                _windowTitle = value;
                OnPropertyChanged();
            }
        }
    }

    public ActivityCategory Category
    {
        get => _category;
        set
        {
            if (_category != value)
            {
                _category = value;
                OnPropertyChanged();
            }
        }
    }

    public ActivityClassificationSource ClassificationSource
    {
        get => _classificationSource;
        set
        {
            if (_classificationSource != value)
            {
                _classificationSource = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CategorySourceDisplay));
            }
        }
    }

    public long? MatchedRuleId
    {
        get => _matchedRuleId;
        set
        {
            if (_matchedRuleId != value)
            {
                _matchedRuleId = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MatchedRuleDisplay));
            }
        }
    }

    public string MatchedRuleDescription
    {
        get => _matchedRuleDescription;
        set
        {
            if (_matchedRuleDescription != value)
            {
                _matchedRuleDescription = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MatchedRuleDisplay));
            }
        }
    }

    public bool IsIdle
    {
        get => _isIdle;
        set
        {
            if (_isIdle != value)
            {
                _isIdle = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsProductive
    {
        get => _isProductive;
        set
        {
            if (_isProductive != value)
            {
                _isProductive = value;
                OnPropertyChanged();
            }
        }
    }

    public int TimeSpentSeconds
    {
        get => _timeSpentSeconds;
        set
        {
            if (_timeSpentSeconds != value)
            {
                _timeSpentSeconds = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DurationString));
            }
        }
    }

    public DateTime LastActive
    {
        get => _lastActive;
        set
        {
            if (_lastActive != value)
            {
                _lastActive = value;
                OnPropertyChanged();
            }
        }
    }

    public DateTime ActivityDate
    {
        get => _activityDate;
        set
        {
            if (_activityDate != value)
            {
                _activityDate = value;
                OnPropertyChanged();
            }
        }
    }

    public int Version
    {
        get => _version;
        set
        {
            if (_version != value)
            {
                _version = value;
                OnPropertyChanged();
            }
        }
    }

    public string DurationString
    {
        get
        {
            var ts = TimeSpan.FromSeconds(_timeSpentSeconds);
            if (ts.TotalHours >= 1)
            {
                return $"{(int)ts.TotalHours} ч {ts.Minutes:D2} мин";
            }

            return $"{ts.Minutes} мин {ts.Seconds:D2} сек";
        }
    }

    public string CategoryDisplay => Category switch
    {
        ActivityCategory.Work => "Работа",
        ActivityCategory.Communication => "Коммуникации",
        ActivityCategory.ConditionalWork => "Условная работа",
        ActivityCategory.Entertainment => "Развлечения",
        ActivityCategory.System => "Система",
        _ => "Неизвестно"
    };

    public string CategorySourceDisplay => ClassificationSource switch
    {
        ActivityClassificationSource.CompanyRule => "Правило компании",
        ActivityClassificationSource.BuiltIn => "Встроенное правило",
        ActivityClassificationSource.FallbackUnknown => "Fallback Unknown",
        _ => "Не определено"
    };

    public string MatchedRuleDisplay
    {
        get
        {
            if (ClassificationSource != ActivityClassificationSource.CompanyRule)
            {
                return "—";
            }

            if (!string.IsNullOrWhiteSpace(MatchedRuleDescription))
            {
                return MatchedRuleDescription;
            }

            return MatchedRuleId.HasValue ? $"Rule #{MatchedRuleId.Value}" : "Company rule";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
