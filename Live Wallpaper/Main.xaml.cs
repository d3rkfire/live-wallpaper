﻿using System;
using System.Windows;
using System.IO;

namespace Live_Wallpaper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Main : Window
    {
        // Name of auto startup registry key's value
        private const string KEY_NAME = "Live Wallpaper";

        public Main()
        {
            InitializeComponent();
            UpdateStartupRegistry();
            // Initialize Video
            if (!File.Exists((string)Properties.Settings.Default["VideoPath"])) new Settings().ShowDialog();
            mediaElement.Volume = (double)Properties.Settings.Default["VideoVolume"];
            mediaElement.Source = new Uri((string)Properties.Settings.Default["VideoPath"]);
            mediaElement.MediaEnded += (o, e) =>
            {
                mediaElement.Position = TimeSpan.Zero;
                mediaElement.Play();
            };
            mediaElement.Play();
            
            // Load - Get Handle and SetParent
            this.Loaded += (o, e) =>
            {
                // Create and find workerw
                IntPtr progman = User32.FindWindow("Progman", null);
                UIntPtr result = UIntPtr.Zero;
                User32.SendMessageTimeout(progman, 0x052C, new UIntPtr(0), IntPtr.Zero, User32.SendMessageTimeoutFlags.SMTO_NORMAL, 1000, out result);

                IntPtr workerw = IntPtr.Zero;
                User32.EnumWindows(new User32.EnumWindowsProc((hwnd, lParam) =>
                {
                    IntPtr p = User32.FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                    if (p != IntPtr.Zero)
                    {
                        workerw = User32.FindWindowEx(IntPtr.Zero, hwnd, "WorkerW", null);
                    }
                    return true;
                }), IntPtr.Zero);

                // Set Parent
                IntPtr windowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                User32.SetParent(windowHandle, workerw);
            };

            // Add Tray Icon
            System.Windows.Forms.NotifyIcon ni = new System.Windows.Forms.NotifyIcon();
            ni.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
            ni.Visible = true;
            ni.ContextMenu = new System.Windows.Forms.ContextMenu();

            System.Windows.Forms.MenuItem itemExit = new System.Windows.Forms.MenuItem("Exit", (o, e) => {
                // Reset Wallpaper
                string originalWallpaperPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft\\Windows\\Themes\\TranscodedWallpaper");
                string tempPath = Path.Combine(Path.GetTempPath(), "original-wallpaper.jpg");
                File.Copy(originalWallpaperPath, tempPath, true);
                mediaElement.Source = new Uri(tempPath);
                mediaElement.MediaEnded += (o2, e2) =>
                {
                    File.Delete(tempPath);
                    Application.Current.Shutdown();
                };
                mediaElement.Play();
            });
            System.Windows.Forms.MenuItem itemSetting = new System.Windows.Forms.MenuItem("Settings", (o, e) =>
            {
                string currentVideoPath = (string)Properties.Settings.Default["VideoPath"];
                Settings settings = new Settings();
                if (settings.ShowDialog() == true)
                {
                    string newVideoPath = (string)Properties.Settings.Default["VideoPath"];
                    mediaElement.Volume = (double)Properties.Settings.Default["VideoVolume"];
                    if (newVideoPath != currentVideoPath)
                    {
                        mediaElement.Source = new Uri(newVideoPath);
                        mediaElement.Play();
                    }
                    UpdateStartupRegistry();
                }
            });
            ni.ContextMenu.MenuItems.Add(itemSetting);
            ni.ContextMenu.MenuItems.Add(itemExit);
        }

        private void UpdateStartupRegistry()
        {
            // Registry Key Value will be deleted when startup is disabled in order to prevent unused startup entry as this is a portable program.
            Microsoft.Win32.RegistryKey startups = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if ((bool)Properties.Settings.Default["Startup"]) startups.SetValue(KEY_NAME, "\"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\"");
            else startups.DeleteValue(KEY_NAME, false);
        }
    }
}