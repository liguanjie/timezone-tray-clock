using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using H.NotifyIcon;
using Microsoft.Win32;

namespace TimezoneTrayClock;

public partial class MainWindow : Window
{
    private const string TargetTimeZoneId = "China Standard Time";
    private const double RightOffset = 16;
    private const double BottomOffset = 8;

    private readonly DispatcherTimer _timer;
    private readonly TimeZoneInfo _targetTimeZone;
    private TaskbarIcon? _taskbarIcon;
    private Settings _settings;

    public MainWindow()
    {
        InitializeComponent();

        _settings = Settings.Load();
        DataContext = _settings;
        ThemeHelper.ApplyTheme(_settings.IsAutoTheme ? ThemeHelper.IsSystemDarkTheme() : _settings.IsDarkMode);

        SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;

        InitializeTrayIcon();

        try
        {
            _targetTimeZone = TimeZoneInfo.FindSystemTimeZoneById(TargetTimeZoneId);
            ToolTip = _targetTimeZone.DisplayName;
        }
        catch (TimeZoneNotFoundException)
        {
            _targetTimeZone = TimeZoneInfo.Local;
            ToolTip = $"未找到时区 '{TargetTimeZoneId}'，已回退到本地时区。";
        }

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += Timer_Tick;
    }

    private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (_settings.IsAutoTheme && e.Category == UserPreferenceCategory.General)
        {
            Dispatcher.Invoke(() => ThemeHelper.ApplyTheme(ThemeHelper.IsSystemDarkTheme()));
        }
    }

    private void InitializeTrayIcon()
    {
        _taskbarIcon = new TaskbarIcon
        {
            IconSource = new BitmapImage(new Uri("pack://application:,,,/Assets/app.ico")),
            ToolTipText = "Timezone Tray Clock"
        };
        
        _taskbarIcon.TrayMouseDoubleClick += (s, e) => 
        {
            if (Visibility == Visibility.Visible)
            {
                Visibility = Visibility.Hidden;
                _timer.Stop();
            }
            else
            {
                Visibility = Visibility.Visible;
                UpdateClock();
                _timer.Start();
                EnsureTopmost();
            }
        };

        var contextMenu = new ContextMenu();

        var autoStartItem = new MenuItem { Header = "开机自启动", IsCheckable = true, IsChecked = AutoStartHelper.IsEnabled() };
        autoStartItem.Click += (s, e) => 
        {
            AutoStartHelper.SetAutoStart(autoStartItem.IsChecked);
            _settings.IsAutoStartEnabled = autoStartItem.IsChecked;
            _settings.Save();
        };

        var clickThroughItem = new MenuItem { Header = "锁定位置(鼠标穿透)", IsCheckable = true, IsChecked = _settings.IsClickThroughEnabled };
        clickThroughItem.Click += (s, e) => 
        {
            _settings.IsClickThroughEnabled = clickThroughItem.IsChecked;
            ApplyShellWindowStyles();
            _settings.Save();
        };

        var themeItem = new MenuItem { Header = "主题颜色" };
        var autoThemeItem = new MenuItem { Header = "跟随系统", IsCheckable = true, IsChecked = _settings.IsAutoTheme };
        var forceDarkItem = new MenuItem { Header = "始终深色", IsCheckable = true, IsChecked = !_settings.IsAutoTheme && _settings.IsDarkMode };
        var forceLightItem = new MenuItem { Header = "始终浅色", IsCheckable = true, IsChecked = !_settings.IsAutoTheme && !_settings.IsDarkMode };
        
        autoThemeItem.Click += (s, e) => { SetThemeMode(true, true); autoThemeItem.IsChecked = true; forceDarkItem.IsChecked = false; forceLightItem.IsChecked = false; };
        forceDarkItem.Click += (s, e) => { SetThemeMode(false, true); autoThemeItem.IsChecked = false; forceDarkItem.IsChecked = true; forceLightItem.IsChecked = false; };
        forceLightItem.Click += (s, e) => { SetThemeMode(false, false); autoThemeItem.IsChecked = false; forceDarkItem.IsChecked = false; forceLightItem.IsChecked = true; };
        
        themeItem.Items.Add(autoThemeItem);
        themeItem.Items.Add(forceDarkItem);
        themeItem.Items.Add(forceLightItem);

        var scaleItem = new MenuItem { Header = "缩放大小(Ctrl+滚轮)" };
        var scaleSmallItem = new MenuItem { Header = "小 (85%)" };
        var scaleDefaultItem = new MenuItem { Header = "默认 (100%)" };
        var scaleLargeItem = new MenuItem { Header = "大 (125%)" };
        var scaleExtraLargeItem = new MenuItem { Header = "特大 (150%)" };
        
        scaleSmallItem.Click += (s, e) => SetScale(0.85);
        scaleDefaultItem.Click += (s, e) => SetScale(1.0);
        scaleLargeItem.Click += (s, e) => SetScale(1.25);
        scaleExtraLargeItem.Click += (s, e) => SetScale(1.5);
        
        scaleItem.Items.Add(scaleSmallItem);
        scaleItem.Items.Add(scaleDefaultItem);
        scaleItem.Items.Add(scaleLargeItem);
        scaleItem.Items.Add(scaleExtraLargeItem);

        var exitItem = new MenuItem { Header = "退出" };
        exitItem.Click += (s, e) => Application.Current.Shutdown();

        contextMenu.Items.Add(autoStartItem);
        contextMenu.Items.Add(clickThroughItem);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(themeItem);
        contextMenu.Items.Add(scaleItem);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(exitItem);

        _taskbarIcon.ContextMenu = contextMenu;
        _taskbarIcon.ForceCreate(false);
    }

    private void SetThemeMode(bool isAuto, bool isDark)
    {
        _settings.IsAutoTheme = isAuto;
        _settings.IsDarkMode = isDark;
        ThemeHelper.ApplyTheme(isAuto ? ThemeHelper.IsSystemDarkTheme() : isDark);
        _settings.Save();
    }

    private void SetScale(double scale)
    {
        _settings.Scale = scale;
        RootScaleTransform.ScaleX = scale;
        RootScaleTransform.ScaleY = scale;
        _settings.Save();
        
        // Wait for layout to update and then ensure it fits on screen
        Dispatcher.InvokeAsync(() => 
        {
            UpdateLayout();
            EnsureWindowInBounds();
        }, DispatcherPriority.ContextIdle);
    }

    private void Window_SourceInitialized(object? sender, EventArgs e)
    {
        ApplyShellWindowStyles();
        EnsureTopmost();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        RootScaleTransform.ScaleX = _settings.Scale;
        RootScaleTransform.ScaleY = _settings.Scale;
        UpdateClock();
        PositionToBottomRight();
        EnsureTopmost();
        _timer.Start();
    }

    private void Window_Activated(object sender, EventArgs e)
    {
        EnsureTopmost();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _settings.WindowLeft = Left;
        _settings.WindowTop = Top;
        _settings.Save();

        _timer.Stop();
        _timer.Tick -= Timer_Tick;
        SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
        
        _taskbarIcon?.Dispose();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.System && e.SystemKey == Key.F4)
        {
            e.Handled = true;
        }
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        UpdateClock();
        EnsureTopmost();
    }

    private void UpdateClock()
    {
        var targetNow = TimeZoneInfo.ConvertTime(DateTime.Now, _targetTimeZone);
        TimeTextBlock.Text = targetNow.ToString("HH:mm:ss");
        DateTextBlock.Text = targetNow.ToString("yyyy/M/d");
    }

    private void PositionToBottomRight()
    {
        UpdateLayout();

        if (_settings.WindowLeft.HasValue && _settings.WindowTop.HasValue)
        {
            var left = _settings.WindowLeft.Value;
            var top = _settings.WindowTop.Value;
            
            var rect = new Rect(left, top, ActualWidth, ActualHeight);
            var virtualScreen = new Rect(
                SystemParameters.VirtualScreenLeft,
                SystemParameters.VirtualScreenTop,
                SystemParameters.VirtualScreenWidth,
                SystemParameters.VirtualScreenHeight);
                
            if (virtualScreen.IntersectsWith(rect))
            {
                Left = left;
                Top = top;
                EnsureWindowInBounds();
                return;
            }
        }

        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - ActualWidth - RightOffset;
        Top = workArea.Bottom - ActualHeight - BottomOffset;
    }

    private void EnsureWindowInBounds()
    {
        var workArea = SystemParameters.WorkArea;
        if (Left + ActualWidth > workArea.Right)
            Left = workArea.Right - ActualWidth;
        if (Top + ActualHeight > workArea.Bottom)
            Top = workArea.Bottom - ActualHeight;
        if (Left < workArea.Left)
            Left = workArea.Left;
        if (Top < workArea.Top)
            Top = workArea.Top;
    }

    protected override void OnLocationChanged(EventArgs e)
    {
        base.OnLocationChanged(e);
        if (IsLoaded)
        {
            _settings.WindowLeft = Left;
            _settings.WindowTop = Top;
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed && !_settings.IsClickThroughEnabled)
        {
            DragMove();
            _settings.Save();
        }
    }

    private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && !_settings.IsClickThroughEnabled)
        {
            double newScale = _settings.Scale + (e.Delta > 0 ? 0.1 : -0.1);
            if (newScale < 0.5) newScale = 0.5;
            if (newScale > 3.0) newScale = 3.0;
            SetScale(Math.Round(newScale, 1));
        }
    }

    private void EnsureTopmost()
    {
        if (Visibility != Visibility.Visible) return;

        Topmost = true;

        var handle = new WindowInteropHelper(this).Handle;
        if (handle != IntPtr.Zero)
        {
            NativeMethods.SetWindowPos(handle, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0, 
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
        }
    }

    private void ApplyShellWindowStyles()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var exStyle = NativeMethods.GetWindowLongPtr(handle, NativeMethods.GWL_EXSTYLE).ToInt32();
        exStyle |= NativeMethods.WS_EX_TOOLWINDOW;
        
        if (_settings.IsClickThroughEnabled)
        {
            exStyle |= NativeMethods.WS_EX_TRANSPARENT;
        }
        else
        {
            exStyle &= ~NativeMethods.WS_EX_TRANSPARENT;
        }

        NativeMethods.SetWindowLongPtr(handle, NativeMethods.GWL_EXSTYLE, new IntPtr(exStyle));
        NativeMethods.SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0, 
            NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_FRAMECHANGED);
    }
}
