using System.Runtime.InteropServices;

namespace RoboRouter;

public static class Output
{
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    static extern IntPtr SetForegroundWindow(IntPtr hWnd);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool SetConsoleMode(IntPtr handle, int mode);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool GetConsoleMode(IntPtr handle, out int mode);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr GetStdHandle(int handle);

    public static void Initialize()
    {
        AllocConsole();
        Console.Title = "Router Output Console";

        ClearAndShow(false);

        // try to make custom color work in the published app (ANSI codes)
        var handle = GetStdHandle(-11);
        GetConsoleMode(handle, out int mode);
        if (!SetConsoleMode(handle, mode | 0x4))
            SetColor = col => { };
    }

    public static void ClearAndShow(bool focus)
    {
        Console.Clear();
        var console = GetConsoleWindow();
        ShowWindow(console, 1);
        if (focus)
            SetForegroundWindow(console);
    }

    public static Action<Color> SetColor = col => Console.Write($"\x1b[38;2;{col.R};{col.G};{col.B}m");

    public static void WriteCol(string msg, Color col)
    {
        SetColor(col);
        Console.Write(msg);
    }

    public static int inputRow;
    public static void CursorBack() => Console.SetCursorPosition(4, inputRow);

    public static Color errorColor = Color.FromArgb(255, 61, 61);
    public static void PrintError(string msg)
    {
        SetColor(errorColor);
        Console.Write(msg);
        SetColor(Color.White);
    }
}
