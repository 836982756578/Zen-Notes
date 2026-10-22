// Zen Notes - Masaüstü Kenara Katlanabilir Akıllı Notlar
// Geliştirici: Serkan ŞENTÜRK
// Telif Hakkı © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using FontFamily = System.Windows.Media.FontFamily;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Orientation = System.Windows.Controls.Orientation;
using TextBox = System.Windows.Controls.TextBox;

namespace ZenNotes
{
    public class NoteWindow : Window
    {
        public NoteData Data { get; private set; }
        public event EventHandler NoteDeleted;
        public event EventHandler NoteChanged;
        public event EventHandler CreateNewRequested;

        private Border _outerBorder;
        private Border _headerBorder;
        private TextBox _txtTitle;
        private TextBox _txtContent;
        private Grid _bodyGrid;
        private Button _btnRoll;
        private Button _btnPin;
        private TextBlock _txtCharCount;
        private DispatcherTimer _saveDebounceTimer;
        private double _expandedHeight = 340;

        public NoteWindow(NoteData data)
        {
            Data = data;
            _expandedHeight = data.Height > 80 ? data.Height : 340;

            InitializeComponent();
            ApplyTheme(Data.ColorTheme);

            _saveDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _saveDebounceTimer.Tick += (s, e) =>
            {
                _saveDebounceTimer.Stop();
                if (NoteChanged != null) NoteChanged(this, EventArgs.Empty);
            };
        }

        private void InitializeComponent()
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Width = Data.Width;
            Height = Data.IsRolledUp ? 40 : Data.Height;
            Left = Data.Left;
            Top = Data.Top;
            Topmost = Data.IsPinned;
            ResizeMode = ResizeMode.CanResizeWithGrip;
            MinWidth = 220;
            MinHeight = 40;

            _outerBorder = new Border
            {
                CornerRadius = new CornerRadius(10),
                BorderThickness = new Thickness(1.5)
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Body

            // --- HEADER ---
            _headerBorder = new Border
            {
                Height = 38,
                CornerRadius = new CornerRadius(10, 10, 0, 0),
                Padding = new Thickness(10, 0, 8, 0)
            };
            _headerBorder.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount == 2)
                {
                    ToggleRollUp();
                }
                else if (e.LeftButton == MouseButtonState.Pressed)
                {
                    DragMove();
                    Data.Left = Left;
                    Data.Top = Top;
                    TriggerSave();
                }
            };

            var hGrid = new Grid();
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // ☯️ icon
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Title
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Actions

            var iconTxt = new TextBlock
            {
                Text = "☯️",
                FontSize = 15,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };

            _txtTitle = new TextBox
            {
                Text = Data.Title,
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center
            };
            _txtTitle.TextChanged += (s, e) =>
            {
                Data.Title = _txtTitle.Text;
                TriggerSave();
            };

            var actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            // Theme picker button (palette)
            var btnTheme = CreateSmallButton("🎨", "Tema Rengini Değiştir", (s, e) => CycleTheme());

            // Pin button
            _btnPin = CreateSmallButton(Data.IsPinned ? "📌" : "📍", "Her Zaman Üstte Tut", (s, e) =>
            {
                Data.IsPinned = !Data.IsPinned;
                Topmost = Data.IsPinned;
                _btnPin.Content = Data.IsPinned ? "📌" : "📍";
                TriggerSave();
            });

            // New note button
            var btnNew = CreateSmallButton("➕", "Yeni Not Ekle", (s, e) =>
            {
                if (CreateNewRequested != null) CreateNewRequested(this, EventArgs.Empty);
            });

            // Roll-up toggle button
            _btnRoll = CreateSmallButton(Data.IsRolledUp ? "▼" : "▲", "Kenara Katla / Aç", (s, e) => ToggleRollUp());

            // Close button
            var btnClose = CreateSmallButton("✕", "Notu Sil", (s, e) =>
            {
                if (NoteDeleted != null) NoteDeleted(this, EventArgs.Empty);
                Close();
            });

            actions.Children.Add(btnTheme);
            actions.Children.Add(_btnPin);
            actions.Children.Add(btnNew);
            actions.Children.Add(_btnRoll);
            actions.Children.Add(btnClose);

            Grid.SetColumn(iconTxt, 0);
            Grid.SetColumn(_txtTitle, 1);
            Grid.SetColumn(actions, 2);
            hGrid.Children.Add(iconTxt);
            hGrid.Children.Add(_txtTitle);
            hGrid.Children.Add(actions);
            _headerBorder.Child = hGrid;

            Grid.SetRow(_headerBorder, 0);
            mainGrid.Children.Add(_headerBorder);

            // --- BODY ---
            _bodyGrid = new Grid();
            _bodyGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            _bodyGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Mini footer

            _txtContent = new TextBox
            {
                Text = Data.Content,
                Foreground = new SolidColorBrush(ColorFromHex("#E2E8F0")),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontSize = 13,
                FontFamily = new FontFamily("Segoe UI"),
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                AcceptsTab = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(10, 8, 10, 8)
            };
            _txtContent.TextChanged += (s, e) =>
            {
                Data.Content = _txtContent.Text;
                UpdateFooter();
                TriggerSave();
            };

            // Keyboard shortcut for to-do items: Ctrl+T inserts "- [ ] "
            _txtContent.KeyDown += (s, e) =>
            {
                if (e.Key == Key.T && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    int caret = _txtContent.CaretIndex;
                    _txtContent.Text = _txtContent.Text.Insert(caret, "☐ ");
                    _txtContent.CaretIndex = caret + 2;
                    e.Handled = true;
                }
            };

            var miniFooter = new Border
            {
                Height = 22,
                Padding = new Thickness(10, 0, 10, 0),
                BorderThickness = new Thickness(0, 1, 0, 0),
                BorderBrush = new SolidColorBrush(ColorFromHex("#262635"))
            };
            var fGrid = new Grid();
            fGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            fGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _txtCharCount = new TextBlock
            {
                Text = "0 karakter",
                FontSize = 10,
                Foreground = new SolidColorBrush(ColorFromHex("#6E6E82")),
                VerticalAlignment = VerticalAlignment.Center
            };

            var authorText = new TextBlock
            {
                Text = "Zen Notes",
                FontSize = 10,
                Foreground = new SolidColorBrush(ColorFromHex("#6E6E82")),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand
            };
            authorText.MouseLeftButtonDown += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo("https://github.com/836982756578") { UseShellExecute = true }); } catch { }
            };

            Grid.SetColumn(_txtCharCount, 0);
            Grid.SetColumn(authorText, 1);
            fGrid.Children.Add(_txtCharCount);
            fGrid.Children.Add(authorText);
            miniFooter.Child = fGrid;

            Grid.SetRow(_txtContent, 0);
            Grid.SetRow(miniFooter, 1);
            _bodyGrid.Children.Add(_txtContent);
            _bodyGrid.Children.Add(miniFooter);

            Grid.SetRow(_bodyGrid, 1);
            mainGrid.Children.Add(_bodyGrid);

            _outerBorder.Child = mainGrid;
            Content = _outerBorder;

            if (Data.IsRolledUp)
            {
                _bodyGrid.Visibility = Visibility.Collapsed;
            }

            UpdateFooter();

            SizeChanged += (s, e) =>
            {
                if (!Data.IsRolledUp && Height > 60)
                {
                    _expandedHeight = Height;
                    Data.Height = Height;
                    Data.Width = Width;
                    TriggerSave();
                }
            };
        }

        public void ToggleRollUp()
        {
            Data.IsRolledUp = !Data.IsRolledUp;
            if (Data.IsRolledUp)
            {
                _expandedHeight = Height;
                _bodyGrid.Visibility = Visibility.Collapsed;
                Height = 40;
                _btnRoll.Content = "▼";
            }
            else
            {
                _bodyGrid.Visibility = Visibility.Visible;
                Height = _expandedHeight > 80 ? _expandedHeight : 340;
                _btnRoll.Content = "▲";
            }
            TriggerSave();
        }

        public void ApplyTheme(string theme)
        {
            Data.ColorTheme = theme;
            string bgHex = "#121217";
            string headerHex = "#1A1A24";
            string borderHex = "#2E2E3E";

            switch (theme)
            {
                case "Emerald":
                    bgHex = "#0C1F17";
                    headerHex = "#112F23";
                    borderHex = "#10B981";
                    break;
                case "Sunset":
                    bgHex = "#23160D";
                    headerHex = "#362111";
                    borderHex = "#F59E0B";
                    break;
                case "Lavender":
                    bgHex = "#191128";
                    headerHex = "#271B3E";
                    borderHex = "#8B5CF6";
                    break;
                case "Ocean":
                    bgHex = "#0B1A28";
                    headerHex = "#10273D";
                    borderHex = "#0EA5E9";
                    break;
                default: // Obsidian
                    bgHex = "#121217";
                    headerHex = "#1A1A24";
                    borderHex = "#333344";
                    break;
            }

            _outerBorder.Background = new SolidColorBrush(ColorFromHex(bgHex));
            _outerBorder.BorderBrush = new SolidColorBrush(ColorFromHex(borderHex));
            _headerBorder.Background = new SolidColorBrush(ColorFromHex(headerHex));
        }

        private void CycleTheme()
        {
            string[] themes = new string[] { "Obsidian", "Emerald", "Sunset", "Lavender", "Ocean" };
            int idx = Array.IndexOf(themes, Data.ColorTheme);
            idx = (idx + 1) % themes.Length;
            ApplyTheme(themes[idx]);
            TriggerSave();
        }

        private void UpdateFooter()
        {
            int len = _txtContent.Text != null ? _txtContent.Text.Length : 0;
            _txtCharCount.Text = string.Format("{0} karakter", len);
        }

        private void TriggerSave()
        {
            _saveDebounceTimer.Stop();
            _saveDebounceTimer.Start();
        }

        private Button CreateSmallButton(string text, string tooltip, RoutedEventHandler onClick)
        {
            var btn = new Button
            {
                Content = text,
                Width = 24,
                Height = 24,
                Foreground = new SolidColorBrush(ColorFromHex("#A0A0BA")),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontSize = 12,
                Cursor = Cursors.Hand,
                ToolTip = tooltip,
                Margin = new Thickness(2, 0, 0, 0)
            };
            btn.Click += onClick;
            return btn;
        }

        private static Color ColorFromHex(string hex)
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }
    }
}
