using parkmeter_management.Domain.Enums;
using parkmeter_management.SimulatorCore;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SimulatorWPF
{
    public class ParkmeterCard : Border
    {
        // status 
        private static readonly Dictionary<string, (string color, string bg, string label)> StatusConfig = new()
        {
            ["IN_SERVICE"]              = ("#10B981", "#022C22", "EN SERVICE"),
            ["TEMPORARILY_UNAVAILABLE"] = ("#F59E0B", "#2D1A00", "TEMPORAIREMENT INDISPO"),
            ["OUT_OF_SERVICE"]          = ("#EF4444", "#2D0A0A", "HORS SERVICE"),
            ["PENDING_ACTIVATION"]      = ("#818CF8", "#1E1B4B", "EN ATTENTE D'ACTIVATION"),
        };

        // fields 
        private readonly ParkMeterInfo _pm;
        private readonly Action<ParkMeterInfo> _onPowerOn;
        private readonly Action<ParkMeterInfo, AlertType> _onAlert;
        private readonly Action<ParkMeterInfo, string> _onOperator;

        //  ui refs 
        private TextBlock _statusLabel = null!;
        private Border _statusBadge = null!;
        private Ellipse _statusDot = null!;
        private TextBlock _cashValue = null!;
        private Rectangle _cashBar = null!;
        private Grid _cashBarGrid = null!;
        private TextBlock _paperValue = null!;
        private Rectangle _paperBar = null!;
        private Grid _paperBarGrid = null!;
        private StackPanel _flagsPanel = null!;
        private TextBlock _txCount = null!;
        private Button _powerBtn = null!;
        private StackPanel _activeContent = null!;
        private TextBlock _offText = null!;
        private Border _configBadge = null!;
        private TextBlock _configText = null!;

        public ParkmeterCard(
            ParkMeterInfo pm,
            Action<ParkMeterInfo> onPowerOn,
            Action<ParkMeterInfo, AlertType> onAlert,
            Action<ParkMeterInfo, string> onOperator)
        {
            _pm = pm;
            _onPowerOn = onPowerOn;
            _onAlert = onAlert;
            _onOperator = onOperator;

            Width = 280;
            Margin = new Thickness(0, 0, 14, 14);
            CornerRadius = new CornerRadius(14);
            BorderThickness = new Thickness(1);
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B));
            Background = new SolidColorBrush(Color.FromRgb(0x0B, 0x13, 0x22));
            Padding = new Thickness(18);

            BuildUI();
        }

        private void BuildUI()
        {
            var root = new StackPanel();

            // header 
            var header = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleStack = new StackPanel();
            var serialText = new TextBlock
            {
                Text = _pm.SerialNumber,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC)),
                Margin = new Thickness(0, 0, 0, 2)
            };
            var siteText = new TextBlock
            {
                Text = _pm.SiteName ?? "—",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(0x2D, 0x3D, 0x55))
            };
            titleStack.Children.Add(serialText);
            titleStack.Children.Add(siteText);

            _powerBtn = new Button
            {
                Content = "OFF",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(10, 5, 10, 5),
                Background = new SolidColorBrush(Color.FromRgb(0x15, 0x1F, 0x30)),
                Foreground = new SolidColorBrush(Color.FromRgb(0x2D, 0x3D, 0x55)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x22, 0x30, 0x45)),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Top,
                Template = CreateRoundedButtonTemplate()
            };
            _powerBtn.Click += (s, e) => _onPowerOn(_pm);

            Grid.SetColumn(titleStack, 0);
            Grid.SetColumn(_powerBtn, 1);
            header.Children.Add(titleStack);
            header.Children.Add(_powerBtn);
            root.Children.Add(header);

            // status badge 
            _statusBadge = new Border
            {
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 7, 12, 7),
                Margin = new Thickness(0, 0, 0, 10),
                Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1B, 0x4B))
            };
            var statusRow = new StackPanel { Orientation = Orientation.Horizontal };
            _statusDot = new Ellipse
            {
                Width = 8, Height = 8,
                Fill = new SolidColorBrush(Color.FromRgb(0x81, 0x8C, 0xF8)),
                Margin = new Thickness(0, 0, 9, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            _statusLabel = new TextBlock
            {
                Text = "EN ATTENTE",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x81, 0x8C, 0xF8)),
                VerticalAlignment = VerticalAlignment.Center
            };
            statusRow.Children.Add(_statusDot);
            statusRow.Children.Add(_statusLabel);
            _statusBadge.Child = statusRow;
            root.Children.Add(_statusBadge);

            // config badge 
            _configBadge = new Border
            {
                CornerRadius = new CornerRadius(5),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x31, 0x2E, 0x81)),
                Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x18, 0x45)),
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(0, 0, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Left,
                Visibility = Visibility.Collapsed
            };
            _configText = new TextBlock
            {
                FontFamily = new FontFamily("Consolas"),
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(0x63, 0x66, 0xF1))
            };
            _configBadge.Child = _configText;
            root.Children.Add(_configBadge);

            // active content 
            _activeContent = new StackPanel { Visibility = Visibility.Collapsed };

            _activeContent.Children.Add(new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromRgb(0x15, 0x20, 0x32)),
                Margin = new Thickness(0, 0, 0, 10)
            });

            // cash
            _activeContent.Children.Add(CreateLevelSection(
                " CAISSE", out _cashValue, out _cashBar, out _cashBarGrid));

            // papier
            _activeContent.Children.Add(CreateLevelSection(
                " PAPIER", out _paperValue, out _paperBar, out _paperBarGrid));

            _activeContent.Children.Add(new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromRgb(0x15, 0x20, 0x32)),
                Margin = new Thickness(0, 4, 0, 8)
            });

            // flags
            _flagsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 8),
                Visibility = Visibility.Collapsed
            };
            _activeContent.Children.Add(_flagsPanel);

            // transactions
            var txRow = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            txRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            txRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var txLabel = new TextBlock
            {
                Text = "Transactions",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(0x2D, 0x3D, 0x55)),
                VerticalAlignment = VerticalAlignment.Center
            };
            _txCount = new TextBlock
            {
                Text = "0",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x63, 0x66, 0xF1)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(txLabel, 0);
            Grid.SetColumn(_txCount, 1);
            txRow.Children.Add(txLabel);
            txRow.Children.Add(_txCount);
            _activeContent.Children.Add(txRow);

            // buttons
            var btnRow = new Grid();
            btnRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            btnRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            btnRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var alertBtn = CreateActionButton("Alerte",
                Color.FromRgb(0x7F, 0x1D, 0x1D),
                Color.FromRgb(0xEF, 0x44, 0x44));
            alertBtn.Click += (s, e) => ShowAlertDialog();

            var opBtn = CreateActionButton("Opérateur",
                Color.FromRgb(0x0C, 0x29, 0x4F),
                Color.FromRgb(0x3B, 0x82, 0xF6));
            opBtn.Click += (s, e) => ShowOperatorDialog();

            Grid.SetColumn(alertBtn, 0);
            Grid.SetColumn(opBtn, 2);
            btnRow.Children.Add(alertBtn);
            btnRow.Children.Add(opBtn);
            _activeContent.Children.Add(btnRow);

            root.Children.Add(_activeContent);

            // off state 
            _offText = new TextBlock
            {
                Text = "Éteint — cliquer sur ⏻ pour démarrer",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(0x1A, 0x24, 0x36)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 6)
            };
            root.Children.Add(_offText);

            Child = root;
        }

        // level section

        private Border CreateLevelSection(string label,
            out TextBlock valueText, out Rectangle bar, out Grid barGrid)
        {
            var section = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x0E, 0x18, 0x28)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x18, 0x24, 0x38)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 6)
            };

            var stack = new StackPanel();

            var topRow = new Grid();
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var labelBlock = new TextBlock
            {
                Text = label,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 8,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x41, 0x55)),
                VerticalAlignment = VerticalAlignment.Center
            };

            valueText = new TextBlock
            {
                Text = "—",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81)),
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid.SetColumn(labelBlock, 0);
            Grid.SetColumn(valueText, 1);
            topRow.Children.Add(labelBlock);
            topRow.Children.Add(valueText);

            var barBg = new Border
            {
                Height = 6,
                Background = new SolidColorBrush(Color.FromRgb(0x18, 0x24, 0x38)),
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(0, 6, 0, 0)
            };

            bar = new Rectangle
            {
                Height = 6,
                Fill = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81)),
                HorizontalAlignment = HorizontalAlignment.Left,
                RadiusX = 3, RadiusY = 3,
                Width = 0
            };

            barGrid = new Grid();
            barGrid.Children.Add(barBg);
            barGrid.Children.Add(bar);

            stack.Children.Add(topRow);
            stack.Children.Add(barGrid);
            section.Child = stack;

            return section;
        }

        // action button 

        private Button CreateActionButton(string text, Color bg, Color accent)
        {
            return new Button
            {
                Content = text,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(0, 8, 0, 8),
                Background = new SolidColorBrush(bg),
                Foreground = new SolidColorBrush(accent),
                BorderBrush = new SolidColorBrush(
                    Color.FromArgb(80, accent.R, accent.G, accent.B)),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand,
                Template = CreateRoundedButtonTemplate()
            };
        }

        // update state 

        public void UpdateState(ParkMeterSimulator sim)
        {
            var status = sim.Status;

            // power button
            _powerBtn.Content = "⏻ ON";
            _powerBtn.Background = new SolidColorBrush(Color.FromRgb(0x05, 0x1F, 0x12));
            _powerBtn.Foreground = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));
            _powerBtn.BorderBrush = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));
            _powerBtn.IsEnabled = false;

            // status
            var (color, bg, label) = StatusConfig.GetValueOrDefault(
                status, ("#818CF8", "#1E1B4B", status));
            var statusColor = (Color)ColorConverter.ConvertFromString(color);
            var bgColor = (Color)ColorConverter.ConvertFromString(bg);

            _statusLabel.Text = label;
            _statusLabel.Foreground = new SolidColorBrush(statusColor);
            _statusDot.Fill = new SolidColorBrush(statusColor);
            _statusBadge.Background = new SolidColorBrush(bgColor);
            _statusBadge.BorderBrush = new SolidColorBrush(
                Color.FromArgb(60, statusColor.R, statusColor.G, statusColor.B));

            // config
            if (sim.HasConfig)
            {
                _configText.Text = $"⚙  {sim.StrategyType}";
                _configBadge.Visibility = Visibility.Visible;
            }

            // show content
            _activeContent.Visibility = Visibility.Visible;
            _offText.Visibility = Visibility.Collapsed;

            // cash
            var cashPct = Math.Min(100.0, (double)(sim.CashAmount / 40.000m) * 100.0);
            _cashValue.Text = $"{sim.CashAmount:F3} DT";
            var cashColor = cashPct >= 100 ? Color.FromRgb(0xEF, 0x44, 0x44)
                : cashPct >= 70 ? Color.FromRgb(0xF5, 0x9E, 0x0B)
                : Color.FromRgb(0x10, 0xB9, 0x81);
            _cashValue.Foreground = new SolidColorBrush(cashColor);

            // papier
            var paperPct = (double)sim.PaperLevel;
            _paperValue.Text = $"{sim.PaperLevel}%";
            var paperColor = paperPct <= 5 ? Color.FromRgb(0xEF, 0x44, 0x44)
                : paperPct <= 20 ? Color.FromRgb(0xF5, 0x9E, 0x0B)
                : Color.FromRgb(0x10, 0xB9, 0x81);
            _paperValue.Foreground = new SolidColorBrush(paperColor);

            // bars
            Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
            {
                UpdateBar(_cashBar, _cashBarGrid, cashPct, cashColor);
                UpdateBar(_paperBar, _paperBarGrid, paperPct, paperColor);
            });

            // flags of (alerts...) 
            _flagsPanel.Children.Clear();
            var flags = new List<(string text, Color c)>();
            if (sim.CashBoxAlertSent) flags.Add(("CASH_BOX_FULL", Color.FromRgb(0xEF, 0x44, 0x44)));
            else if (sim.CashBoxWarnSent) flags.Add(("CASH_BOX_WARN", Color.FromRgb(0xF5, 0x9E, 0x0B)));
            if (sim.PaperEmptyAlertSent) flags.Add(("PAPER_EMPTY", Color.FromRgb(0xEF, 0x44, 0x44)));
            else if (sim.PaperAlertSent) flags.Add(("PAPER_LOW", Color.FromRgb(0xF5, 0x9E, 0x0B)));
            if (sim.HardwareFailure) flags.Add(("HARDWARE", Color.FromRgb(0xEF, 0x44, 0x44)));
            if (sim.DoorOpen) flags.Add(("DOOR_OPEN", Color.FromRgb(0xF5, 0x9E, 0x0B)));

            foreach (var (flagText, flagColor) in flags)
            {
                var chip = new Border
                {
                    CornerRadius = new CornerRadius(4),
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush(
                        Color.FromArgb(60, flagColor.R, flagColor.G, flagColor.B)),
                    Background = new SolidColorBrush(
                        Color.FromArgb(20, flagColor.R, flagColor.G, flagColor.B)),
                    Padding = new Thickness(6, 2, 6, 2),
                    Margin = new Thickness(0, 0, 4, 0),
                    Child = new TextBlock
                    {
                        Text = $"⚠ {flagText}",
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 8,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(flagColor)
                    }
                };
                _flagsPanel.Children.Add(chip);
            }

            _flagsPanel.Visibility = flags.Count > 0
                ? Visibility.Visible : Visibility.Collapsed;

            // transactions
            _txCount.Text = sim.TransactionCount.ToString();

            // card border + background glow
            BorderBrush = new SolidColorBrush(
                Color.FromArgb(100, statusColor.R, statusColor.G, statusColor.B));

            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Color.FromRgb(0x0B, 0x13, 0x22), 0.0),
                    new GradientStop(Color.FromArgb(40, bgColor.R, bgColor.G, bgColor.B), 1.0),
                }
            };
        }

        private void UpdateBar(Rectangle bar, Grid barGrid, double pct, Color color)
        {
            var width = barGrid.ActualWidth;
            if (width > 4)
            {
                bar.Width = Math.Max(0, width * pct / 100.0);
                bar.Fill = new SolidColorBrush(color);
            }
        }

        private void ShowAlertDialog()
        {
            var dialog = new AlertDialog(_pm, _onAlert);
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
        }

        private void ShowOperatorDialog()
        {
            var dialog = new OperatorDialog(_pm, _onOperator);
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
        }

        private static ControlTemplate CreateRoundedButtonTemplate()
        {
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetBinding(Border.BackgroundProperty,
                new System.Windows.Data.Binding("Background")
                { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            border.SetBinding(Border.BorderBrushProperty,
                new System.Windows.Data.Binding("BorderBrush")
                { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            border.SetBinding(Border.BorderThicknessProperty,
                new System.Windows.Data.Binding("BorderThickness")
                { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            border.SetBinding(Border.PaddingProperty,
                new System.Windows.Data.Binding("Padding")
                { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(7));
            var content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(content);
            template.VisualTree = border;
            return template;
        }
    }
}