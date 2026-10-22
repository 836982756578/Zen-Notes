// Zen Notes - Masaüstü Kenara Katlanabilir Akıllı Notlar
// Geliştirici: Serkan ŞENTÜRK
// Telif Hakkı © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using Application = System.Windows.Application;

namespace ZenNotes
{
    public class NotesApp : Application
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint VK_N = 0x4E;
        private const int HOTKEY_ID = 9001;

        private List<NoteData> _notesData;
        private List<NoteWindow> _windows = new List<NoteWindow>();
        private NotifyIcon _notifyIcon;
        private HwndSource _source;

        [STAThread]
        public static void Main()
        {
            var app = new NotesApp();
            app.Run();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _notesData = NotesStorage.LoadNotes();

            InitNotifyIcon();
            RegisterGlobalHotkey();

            foreach (var data in _notesData)
            {
                CreateAndShowNoteWindow(data);
            }
        }

        private void CreateAndShowNoteWindow(NoteData data)
        {
            var win = new NoteWindow(data);
            win.NoteChanged += (s, e) => SaveAll();
            win.NoteDeleted += (s, e) =>
            {
                _windows.Remove(win);
                _notesData.Remove(data);
                SaveAll();
            };
            win.CreateNewRequested += (s, e) =>
            {
                AddNewNote(win.Left + 40, win.Top + 40);
            };

            _windows.Add(win);
            win.Show();
        }

        private void AddNewNote(double left = 300, double top = 200)
        {
            var newNote = new NoteData
            {
                Title = "Yeni Not " + (_notesData.Count + 1),
                Left = left,
                Top = top
            };
            _notesData.Add(newNote);
            SaveAll();
            CreateAndShowNoteWindow(newNote);
        }

        private void SaveAll()
        {
            NotesStorage.SaveNotes(_notesData);
        }

        private void RegisterGlobalHotkey()
        {
            try
            {
                var helper = new WindowInteropHelper(new Window());
                helper.EnsureHandle();

                _source = HwndSource.FromHwnd(helper.Handle);
                if (_source != null)
                {
                    _source.AddHook(HwndHook);
                    RegisterHotKey(helper.Handle, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, VK_N);
                }
            }
            catch { }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                AddNewNote();
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void InitNotifyIcon()
        {
            try
            {
                _notifyIcon = new NotifyIcon();
                string exePath = Assembly.GetExecutingAssembly().Location;
                _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath) ?? System.Drawing.SystemIcons.Application;
                _notifyIcon.Text = "Zen Notes - Akıllı Masaüstü Notları";
                _notifyIcon.Visible = true;

                var menu = new ContextMenuStrip();
                menu.Items.Add("➕ Yeni Not Ekle (Ctrl+Shift+N)", null, (s, e) => AddNewNote());
                menu.Items.Add("▲ Tümünü Katla", null, (s, e) => SetAllRollState(true));
                menu.Items.Add("▼ Tümünü Aç", null, (s, e) => SetAllRollState(false));
                menu.Items.Add("-");
                menu.Items.Add("🌐 Geliştirici: Serkan ŞENTÜRK", null, (s, e) =>
                {
                    try { Process.Start(new ProcessStartInfo("https://github.com/836982756578") { UseShellExecute = true }); } catch { }
                });
                menu.Items.Add("✕ Çıkış", null, (s, e) => ShutdownApp());

                _notifyIcon.ContextMenuStrip = menu;
                _notifyIcon.DoubleClick += (s, e) => AddNewNote();
            }
            catch { }
        }

        private void SetAllRollState(bool rolled)
        {
            foreach (var win in _windows)
            {
                if (win.Data.IsRolledUp != rolled)
                {
                    win.ToggleRollUp();
                }
            }
        }

        private void ShutdownApp()
        {
            SaveAll();
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_source != null)
            {
                UnregisterHotKey(_source.Handle, HOTKEY_ID);
                _source.Dispose();
            }
            base.OnExit(e);
        }
    }
}
