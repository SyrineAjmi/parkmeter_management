using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SimulatorWPF
{
    public class OperatorDialog : Window
    {
        private readonly ParkMeterInfo _pm;
        private readonly Action<ParkMeterInfo, string> _onAction;

        public OperatorDialog(ParkMeterInfo pm, Action<ParkMeterInfo, string> onAction)
        {
            _pm = pm;
            _onAction = onAction;

            Title = "Action opérateur";
            Width = 360;
            Height = 380;
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

            root.Children.Add(new TextBlock
            {
                Text = "Action opérateur",
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

            var actions = new[]
            {
                ("empty",    "", "Vider la caisse"),
                ("paper",    "", "Recharger papier"),
                ("door",     "", "Fermer la porte"),
                ("hardware", "", "Réparer matériel"),
            };

            foreach (var (key, icon, label) in actions)
            {
                var btn = CreateActionButton(icon, label, key);
                root.Children.Add(btn);
            }

            root.Children.Add(new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B))
            });

            var cancelBtn = new Button
            {
                Content = "Annuler",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                Padding = new Thickness(14, 7, 14, 7),
                Background = new SolidColorBrush(Colors.Transparent),
                Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x41, 0x55)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B)),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Right,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            cancelBtn.Click += (s, e) => Close();
            root.Children.Add(cancelBtn);

            Content = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x0D, 0x16, 0x26)),
                Child = root
            };
        }

        private Button CreateActionButton(string icon, string label, string key)
        {
            var btn = new Button
            {
                Padding = new Thickness(14, 12, 14, 12),
                Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x2D, 0x3D, 0x4E)),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand,
                HorizontalContentAlignment = HorizontalAlignment.Left
            };

            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(new TextBlock
            {
                Text = icon,
                FontSize = 20,
                VerticalAlignment = VerticalAlignment.Center
            });
            row.Children.Add(new TextBlock
            {
                Text = label,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1)),
                VerticalAlignment = VerticalAlignment.Center
            });

            btn.Content = row;
            btn.Click += (s, e) =>
            {
                _onAction(_pm, key);
                Close();
            };

            return btn;
        }
    }
}
