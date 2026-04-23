using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Interop;

namespace TimezoneTrayClock;

public partial class MainWindow : Window
{
    // 默认显示上海时区。如需替换成别的时区，改成对应的 Windows 时区 ID 即可。
    // 例如："Tokyo Standard Time"、"Pacific Standard Time"。
    private const string TargetTimeZoneId = "China Standard Time";

    // 右下角定位微调参数：正数会向左、向上多留一些距离。
    private const double RightOffset = 16;
    private const double BottomOffset = 8;
    private const int GwlExstyle = -20;
    private const int WsExAppWindow = 0x00040000;
    private const int WsExToolWindow = 0x00000080;
    private static readonly IntPtr HwndTopmost = new(-1);
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpShowWindow = 0x0040;
    private const uint SwpFrameChanged = 0x0020;

    private readonly DispatcherTimer _timer;
    private readonly TimeZoneInfo _targetTimeZone;

    public MainWindow()
    {
        InitializeComponent();

        _targetTimeZone = TimeZoneInfo.FindSystemTimeZoneById(TargetTimeZoneId);

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += Timer_Tick;
    }

    private void Window_SourceInitialized(object? sender, EventArgs e)
    {
        ApplyShellWindowStyles();
        EnsureTopmost();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateClock();
        PositionToBottomRight();
        EnsureTopmost();
        _timer.Start();
    }

    private void Window_Activated(object sender, EventArgs e)
    {
        EnsureTopmost();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        UpdateClock();
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

        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - ActualWidth - RightOffset;
        Top = workArea.Bottom - ActualHeight - BottomOffset;
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void EnsureTopmost()
    {
        Topmost = true;

        var handle = new WindowInteropHelper(this).Handle;
        if (handle != IntPtr.Zero)
        {
            SetWindowPos(handle, HwndTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoActivate | SwpShowWindow);
        }
    }

    private void ApplyShellWindowStyles()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var exStyle = GetWindowLong(handle, GwlExstyle);
        exStyle |= WsExAppWindow;
        exStyle |= WsExToolWindow;
        SetWindowLong(handle, GwlExstyle, exStyle);
        SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoActivate | SwpFrameChanged);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}
