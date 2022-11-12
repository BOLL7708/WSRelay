using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BOLL7708;

namespace WSRelay
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainController _controller = new MainController();
        private Properties.Settings _settings = Properties.Settings.Default;
        private ConcurrentDictionary<string, int> _stateDic = new ConcurrentDictionary<string, int>();

        const string 
            KEY_SECOND_SESSION = "second_session",
            KEY_SECOND_IN = "second_in",
            KEY_SECOND_OUT = "second_out",
            KEY_SESSION_COUNT = "session_count",
            KEY_MESSAGES_IN_TOTAL = "messages_in_total",
            KEY_MESSAGES_OUT_TOTAL = "messages_out_total";

        public MainWindow()
        {
            InitializeComponent();

            WindowUtils.CheckIfAlreadyRunning("WSRelay");
            var icon = Properties.Resources.Logo.Clone() as System.Drawing.Icon;
            WindowUtils.CreateTrayIcon(this, icon, Properties.Resources.AppName);
            WindowUtils.Minimize(this, false);

            UpdatePortLabel();

            _stateDic.TryAdd(KEY_SECOND_SESSION, 0);
            _stateDic.TryAdd(KEY_SESSION_COUNT, 0);
            _stateDic.TryAdd(KEY_MESSAGES_IN_TOTAL, 0);
            _stateDic.TryAdd(KEY_MESSAGES_OUT_TOTAL, 0);

            _controller.StatusAction += (status, count) =>
            {
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        var now = DateTime.Now;
                        var shouldUpdateStatisticsText = false;
                        switch (status)
                        {
                            case SuperServer.ServerStatus.Connected:
                                Label_Status.Background = Brushes.OliveDrab;
                                Label_Status.Content = "Online";
                                break;
                            case SuperServer.ServerStatus.Disconnected:
                                Label_Status.Background = Brushes.Tomato;
                                Label_Status.Content = "Offline";
                                break;
                            case SuperServer.ServerStatus.Error:
                                Label_Status.Background = Brushes.Gray;
                                Label_Status.Content = "Error";
                                break;
                            case SuperServer.ServerStatus.DeliveredCount:
                                {
                                    _stateDic.TryGetValue(KEY_SECOND_OUT, out int second);
                                    if (now.Second != second)
                                    {
                                        _stateDic.TryUpdate(KEY_SECOND_OUT, now.Second, second);
                                        UpdateValueInDic(KEY_MESSAGES_OUT_TOTAL, count);
                                        shouldUpdateStatisticsText = true;
                                    }
                                }
                                break;
                            case SuperServer.ServerStatus.ReceivedCount:
                                {
                                    _stateDic.TryGetValue(KEY_SECOND_IN, out int second);
                                    if (now.Second != second)
                                    {
                                        _stateDic.TryUpdate(KEY_SECOND_IN, now.Second, second);
                                        UpdateValueInDic(KEY_MESSAGES_IN_TOTAL, count);
                                        shouldUpdateStatisticsText = true;
                                    }
                                }
                                break;
                            case SuperServer.ServerStatus.SessionCount:
                                {
                                    _stateDic.TryGetValue(KEY_SECOND_SESSION, out int second);
                                    if (now.Second != second)
                                    {
                                        _stateDic.TryUpdate(KEY_SECOND_SESSION, now.Second, second);
                                        UpdateValueInDic(KEY_SESSION_COUNT, count);
                                        shouldUpdateStatisticsText = true;
                                    }
                                }
                                break;
                        }
                        if(shouldUpdateStatisticsText)
                        {
                            UpdateStatistics();
                        }
                    });
                }
                catch (TaskCanceledException e)
                {
                    Debug.WriteLine($"Caught exception: {e.Message}");
                }
            };

            UpdateStatistics();
            _controller.StartOrRestartServer(_settings.Port);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SingleInputDialog(this, _settings.Port.ToString(), "Port");
            dlg.ShowDialog();
            var result = dlg.DialogResult == true ? dlg.value : null;
            if(result != null)
            {
                var port = int.Parse(result);
                _controller.StartOrRestartServer(port);
                _settings.Port = port;
                _settings.Save();
                UpdatePortLabel();
            }
            
        }

        private void UpdateStatistics() {
            _stateDic.TryGetValue(KEY_SESSION_COUNT, out int sessionCount);
            _stateDic.TryGetValue(KEY_MESSAGES_IN_TOTAL, out int inCount);
            _stateDic.TryGetValue(KEY_MESSAGES_OUT_TOTAL, out int outCount);
            Label_Stats.Content = $"Sessions: {sessionCount}\nMessages in: {inCount}\nMessages out: {outCount}\nVersion: {Properties.Resources.Version}";
        }

        private void UpdatePortLabel() {
            Label_Port.Content = "Port: "+ _settings.Port;
        }

        private bool UpdateValueInDic(string key, int value)
        {
            var ok = _stateDic.TryGetValue(key, out int oldValue);
            return ok && _stateDic.TryUpdate(key, value, oldValue);
        }

        protected override void OnStateChanged(EventArgs e)
        {
            WindowUtils.OnStateChange(this, false);
            base.OnStateChanged(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            WindowUtils.DestroyTrayIcon();
            base.OnClosing(e);
        }
    }
}
