using parkmeter_management.Domain.Enums;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SimulatorWPF
{
    public class AlertDialog : Window
    {
        private readonly ParkMeterInfo _pm;
        private readonly Action<ParkMeterInfo, AlertType> _onConfirm;
        private readonly List<(AlertType type, string label, ToggleButton btn)> _options = new();

        public AlertDialog(ParkMeterInfo pm, Action<ParkMeterInfo, AlertType> onConfirm)
        {
            _pm = pm;
            _onConfirm = onConfirm;

            Title = "Déclencher une alerte";
            Width = 400;
            Height = 480;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromRgb(0x0D, 0x16, 0x26));
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B));
            BorderThickness = new Thickness(1);

            BuildUI();
        }

        private void BuildUI()
        {
            var root = new StackPanel { Margin = new Thickness(24) };

            // Title
            root.Children.Add(new TextBlock
            {
                Text = "Déclencher une alerte",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xF1, 0xF5, 0xF9))
            });

            root.Children.Add(new TextBlock
            {
                Text = _pm.SerialNumber,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x41, 0x55))
            });

            // Alert options grid
            var grid = new WrapPanel { Orientation = Orientation.Horizontal };

            var alerts = new[]
            {
                (AlertType.CASH_BOX_WARN,    "Caisse Avertissement", "#F59E0B"),
                (AlertType.CASH_BOX_FULL,    "Caisse Pleine",        "#EF4444"),
                (AlertType.PAPER_LOW,        "Papier Faible",        "#F59E0B"),
                (AlertType.PAPER_EMPTY,      "Papier Vide",          "#EF4444"),
                (AlertType.HARDWARE_FAILURE, "Panne Matérielle",     "#EF4444"),
                (AlertType.DOOR_OPEN,        "Porte Ouverte",        "#F59E0B"),
                (AlertType.CONNECTION_LOST,  "Connexion Perdue",     "#6366F1"),
            };

            foreach (var (type, label, color) in alerts)
            {
                var btn = new ToggleButton(type, label, color);
                btn.Margin = new Thickness(0, 0, 8, 8);
                btn.OnToggled += OnAlertToggled;
                _options.Add((type, label, btn));
                grid.Children.Add(btn);
            }

            root.Children.Add(grid);

            // Separator
            root.Children.Add(new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B))
            });

            // Buttons
            var btnRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
            };

            var cancelBtn = CreateDialogButton("Annuler", false);
            cancelBtn.Click += (s, e) => Close();

            var confirmBtn = CreateDialogButton("Confirmer", true);
            confirmBtn.Click += (s, e) =>
            {
                var selected = _options
                    .Where(o => o.btn.IsChecked)
                    .Select(o => o.type)
                    .ToList();

                if (selected.Count == 0) return;

                // CONNECTION_LOST must be alone
                if (selected.Contains(AlertType.CONNECTION_LOST)
                    && selected.Count > 1)
                {
                    MessageBox.Show(
                        "CONNECTION_LOST doit être sélectionné seul.",
                        "Validation", MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var combined = selected.Aggregate((a, b) => a | b);
                _onConfirm(_pm, combined);
                Close();
            };

            btnRow.Children.Add(cancelBtn);
            btnRow.Children.Add(confirmBtn);
            root.Children.Add(btnRow);

            Content = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x0D, 0x16, 0x26)),
                Child = root
            };
        }

        private void OnAlertToggled(AlertType type, bool isChecked)
        {
            // If CONNECTION_LOST selected → uncheck all others
            if (type == AlertType.CONNECTION_LOST && isChecked)
            {
                foreach (var (t, l, btn) in _options)
                    if (t != AlertType.CONNECTION_LOST)
                        btn.SetChecked(false);
            }
            // If other selected → uncheck CONNECTION_LOST
            else if (type != AlertType.CONNECTION_LOST && isChecked)
            {
                foreach (var (t, l, btn) in _options)
                    if (t == AlertType.CONNECTION_LOST)
                        btn.SetChecked(false);
            }
        }

        private Button CreateDialogButton(string text, bool isPrimary)
        {
            return new Button
            {
                Content = text,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                FontWeight = isPrimary ? FontWeights.Bold : FontWeights.Normal,
                Padding = new Thickness(isPrimary ? 18 : 14, 7, isPrimary ? 18 : 14, 7),
                Background = isPrimary
                    ? new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44))
                    : new SolidColorBrush(Colors.Transparent),
                Foreground = isPrimary
                    ? new SolidColorBrush(Colors.White)
                    : new SolidColorBrush(Color.FromRgb(0x33, 0x41, 0x55)),
                BorderBrush = isPrimary
                    ? new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44))
                    : new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B)),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand
            };
        }
    }

    // ── Toggle Button ─────────────────────────────────────────

    public class ToggleButton : Border
    {
        public AlertType AlertType { get; }
        public bool IsChecked { get; private set; }
        public event Action<AlertType, bool>? OnToggled;

        private readonly TextBlock _label;
        private readonly string _color;

        public ToggleButton(AlertType type, string label, string color)
        {
            AlertType = type;
            _color = color;

            CornerRadius = new CornerRadius(8);
            BorderThickness = new Thickness(1);
            Padding = new Thickness(10, 8, 10, 8);
            Cursor = System.Windows.Input.Cursors.Hand;
            Width = 165;

            _label = new TextBlock
            {
                Text = label,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            };

            Child = _label;
            UpdateStyle();

            MouseLeftButtonDown += (s, e) =>
            {
                IsChecked = !IsChecked;
                UpdateStyle();
                OnToggled?.Invoke(AlertType, IsChecked);
            };
        }

        public void SetChecked(bool value)
        {
            IsChecked = value;
            UpdateStyle();
        }

        private void UpdateStyle()
        {
            var c = (Color)ColorConverter.ConvertFromString(_color);
            if (IsChecked)
            {
                Background = new SolidColorBrush(Color.FromArgb(30, c.R, c.G, c.B));
                BorderBrush = new SolidColorBrush(c);
                _label.Foreground = new SolidColorBrush(c);
            }
            else
            {
                Background = new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A));
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B));
                _label.Foreground = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B));
            }
        }
    }
}
