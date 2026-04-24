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

    private TaskbarIcon? _taskbarIcon;
    private readonly MainViewModel _viewModel;
    private int _topmostTickCounter;

    public MainWindow()
    {
        InitializeComponent();

        var settings = Settings.Load();
        _viewModel = new MainViewModel(settings, TargetTimeZoneId);
        DataContext = _viewModel;
        
        ThemeHelper.ApplyTheme(settings.IsAutoTheme ? ThemeHelper.IsSystemDarkTheme() : settings.IsDarkMode);

        SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;

        InitializeTrayIcon();
        ToolTip = _viewModel.GetTimeZoneDisplayName();
        
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.TimeText))
        {
            // EnsureTopmost every ~5 seconds (every 10 ticks at 500ms interval) instead of every tick
            _topmostTickCounter++;
            if (_topmostTickCounter >= 10)
            {
                _topmostTickCounter = 0;
                EnsureTopmost();
            }
        }
    }

    private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (_viewModel.Settings.IsAutoTheme && e.Category == UserPreferenceCategory.General)
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
                _viewModel.StopTimer();
            }
            else
            {
                Visibility = Visibility.Visible;
                _viewModel.StartTimer();
                EnsureTopmost();
            }
        };

        var contextMenu = new ContextMenu();
        contextMenu.Items.Add(CreateAutoStartMenuItem());
        contextMenu.Items.Add(CreateClickThroughMenuItem());
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(CreateThemeMenuItem());
        contextMenu.Items.Add(CreateBackgroundColorMenuItem());
        contextMenu.Items.Add(CreateScaleMenuItem());
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(CreateExitMenuItem());

        _taskbarIcon.ContextMenu = contextMenu;
        _taskbarIcon.ForceCreate(false);
    }

    private MenuItem CreateAutoStartMenuItem()
    {
        var item = new MenuItem { Header = "开机自启动", IsCheckable = true, IsChecked = AutoStartHelper.IsEnabled() };
        item.Click += (s, e) =>
        {
            AutoStartHelper.SetAutoStart(item.IsChecked);
            _viewModel.Settings.IsAutoStartEnabled = item.IsChecked;
            _viewModel.Settings.Save();
        };
        return item;
    }

    private MenuItem CreateClickThroughMenuItem()
    {
        var item = new MenuItem { Header = "锁定位置(鼠标穿透)", IsCheckable = true, IsChecked = _viewModel.Settings.IsClickThroughEnabled };
        item.Click += (s, e) =>
        {
            _viewModel.Settings.IsClickThroughEnabled = item.IsChecked;
            ApplyShellWindowStyles();
            _viewModel.Settings.Save();
        };
        return item;
    }

    private MenuItem CreateThemeMenuItem()
    {
        var themeItem = new MenuItem { Header = "主题颜色" };
        var autoThemeItem = new MenuItem { Header = "跟随系统", IsCheckable = true, IsChecked = _viewModel.Settings.IsAutoTheme };
        var forceDarkItem = new MenuItem { Header = "始终深色", IsCheckable = true, IsChecked = !_viewModel.Settings.IsAutoTheme && _viewModel.Settings.IsDarkMode };
        var forceLightItem = new MenuItem { Header = "始终浅色", IsCheckable = true, IsChecked = !_viewModel.Settings.IsAutoTheme && !_viewModel.Settings.IsDarkMode };

        void UpdateThemeChecks(MenuItem active)
        {
            autoThemeItem.IsChecked = active == autoThemeItem;
            forceDarkItem.IsChecked = active == forceDarkItem;
            forceLightItem.IsChecked = active == forceLightItem;
        }

        autoThemeItem.Click += (s, e) => { SetThemeMode(true, true); UpdateThemeChecks(autoThemeItem); };
        forceDarkItem.Click += (s, e) => { SetThemeMode(false, true); UpdateThemeChecks(forceDarkItem); };
        forceLightItem.Click += (s, e) => { SetThemeMode(false, false); UpdateThemeChecks(forceLightItem); };

        themeItem.Items.Add(autoThemeItem);
        themeItem.Items.Add(forceDarkItem);
        themeItem.Items.Add(forceLightItem);
        return themeItem;
    }

    private MenuItem CreateBackgroundColorMenuItem()
    {
        var colorItem = new MenuItem { Header = "背景颜色" };
        var presets = new[]
        {
            ("默认蓝", "#D6DEF1"),
            ("淡紫", "#E8DFF0"),
            ("薄荷绿", "#D4EDDA"),
            ("暖米", "#F5E6D3"),
            ("浅灰", "#E8E8E8"),
            ("纯白", "#FFFFFF"),
            ("烟白", "#F2F1F6"),
        };

        foreach (var (label, color) in presets)
        {
            var item = new MenuItem
            {
                IsCheckable = true,
                IsChecked = string.Equals(_viewModel.Settings.BackgroundColor, color, StringComparison.OrdinalIgnoreCase),
                Tag = color
            };
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            var swatch = new Border
            {
                Width = 14, Height = 14,
                CornerRadius = new CornerRadius(3),
                BorderBrush = System.Windows.Media.Brushes.Gray,
                BorderThickness = new Thickness(0.5),
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color)),
                Margin = new Thickness(0, 0, 8, 0)
            };
            sp.Children.Add(swatch);
            sp.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
            item.Header = sp;
            item.Click += (s, e) =>
            {
                _viewModel.Settings.BackgroundColor = color;
                _viewModel.Settings.Save();
            };
            colorItem.Items.Add(item);
        }

        colorItem.SubmenuOpened += (s, e) =>
        {
            foreach (MenuItem child in colorItem.Items)
            {
                if (child.Tag is string c)
                    child.IsChecked = string.Equals(_viewModel.Settings.BackgroundColor, c, StringComparison.OrdinalIgnoreCase);
            }
        };

        return colorItem;
    }

    private MenuItem CreateScaleMenuItem()
    {
        var scaleItem = new MenuItem { Header = "缩放大小(Ctrl+滚轮)" };
        foreach (var (label, value) in new[] { ("小 (85%)", 0.85), ("默认 (100%)", 1.0), ("大 (125%)", 1.25), ("特大 (150%)", 1.5) })
        {
            var item = new MenuItem
            {
                Header = label,
                IsCheckable = true,
                IsChecked = Math.Abs(_viewModel.Settings.Scale - value) < 0.01,
                Tag = value
            };
            item.Click += (s, e) => SetScale(value);
            scaleItem.Items.Add(item);
        }

        // Refresh checkmarks when submenu opens (handles Ctrl+scroll changes)
        scaleItem.SubmenuOpened += (s, e) =>
        {
            foreach (MenuItem child in scaleItem.Items)
            {
                if (child.Tag is double v)
                    child.IsChecked = Math.Abs(_viewModel.Settings.Scale - v) < 0.01;
            }
        };

        return scaleItem;
    }

    private static MenuItem CreateExitMenuItem()
    {
        var item = new MenuItem { Header = "退出" };
        item.Click += (s, e) => Application.Current.Shutdown();
        return item;
    }

    private void SetThemeMode(bool isAuto, bool isDark)
    {
        _viewModel.Settings.IsAutoTheme = isAuto;
        _viewModel.Settings.IsDarkMode = isDark;
        ThemeHelper.ApplyTheme(isAuto ? ThemeHelper.IsSystemDarkTheme() : isDark);
        _viewModel.Settings.Save();
    }

    private void SetScale(double scale)
    {
        _viewModel.Settings.Scale = scale;
        _viewModel.Settings.Save();
        
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
        PositionToBottomRight();
        EnsureTopmost();
        _viewModel.StartTimer();
    }

    private void Window_Activated(object sender, EventArgs e)
    {
        EnsureTopmost();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _viewModel.Settings.WindowLeft = Left;
        _viewModel.Settings.WindowTop = Top;
        _viewModel.Settings.Save();
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.StopTimer();
        _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
        _taskbarIcon?.Dispose();
        base.OnClosed(e);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.System && e.SystemKey == Key.F4)
        {
            e.Handled = true;
        }
    }

    private void PositionToBottomRight()
    {
        UpdateLayout();

        if (_viewModel.Settings.WindowLeft.HasValue && _viewModel.Settings.WindowTop.HasValue)
        {
            var left = _viewModel.Settings.WindowLeft.Value;
            var top = _viewModel.Settings.WindowTop.Value;
            
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
            _viewModel.Settings.WindowLeft = Left;
            _viewModel.Settings.WindowTop = Top;
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed && !_viewModel.Settings.IsClickThroughEnabled)
        {
            try { DragMove(); } catch (InvalidOperationException) { }
            _viewModel.Settings.Save();
        }
    }

    private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && !_viewModel.Settings.IsClickThroughEnabled)
        {
            double newScale = _viewModel.Settings.Scale + (e.Delta > 0 ? 0.1 : -0.1);
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
        
        if (_viewModel.Settings.IsClickThroughEnabled)
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
