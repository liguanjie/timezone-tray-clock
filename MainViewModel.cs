using System;
using System.Windows.Media;
using System.Windows.Threading;

namespace TimezoneTrayClock;

public class MainViewModel : ObservableObject
{
    private readonly DispatcherTimer _timer;
    private TimeZoneInfo _targetTimeZone;

    private string _timeText = "00:00:00";
    public string TimeText
    {
        get => _timeText;
        set => SetProperty(ref _timeText, value);
    }

    private string _dateText = "YYYY/M/D";
    public string DateText
    {
        get => _dateText;
        set => SetProperty(ref _dateText, value);
    }

    public Settings Settings { get; }

    private Brush _backgroundBrush = null!;
    public Brush BackgroundBrush
    {
        get => _backgroundBrush;
        private set => SetProperty(ref _backgroundBrush, value);
    }

    private static Brush CreateBrushFromHex(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }

    public MainViewModel(Settings settings, string targetTimeZoneId)
    {
        Settings = settings;
        _backgroundBrush = CreateBrushFromHex(settings.BackgroundColor ?? "#D6DEF1");

        Settings.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Settings.BackgroundColor))
                BackgroundBrush = CreateBrushFromHex(Settings.BackgroundColor ?? "#D6DEF1");
        };

        try
        {
            _targetTimeZone = TimeZoneInfo.FindSystemTimeZoneById(targetTimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            _targetTimeZone = TimeZoneInfo.Local;
        }

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500) // Update faster to avoid skipping seconds
        };
        _timer.Tick += Timer_Tick;
    }

    public string GetTimeZoneDisplayName()
    {
        return _targetTimeZone.DisplayName;
    }

    public void StartTimer()
    {
        UpdateClock();
        _timer.Start();
    }

    public void StopTimer()
    {
        _timer.Stop();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        UpdateClock();
    }

    private void UpdateClock()
    {
        var targetNow = TimeZoneInfo.ConvertTime(DateTime.Now, _targetTimeZone);
        TimeText = targetNow.ToString("HH:mm:ss");
        DateText = targetNow.ToString("yyyy/M/d");
    }
}
