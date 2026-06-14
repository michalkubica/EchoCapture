using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.IO;

namespace EchoCapture
{
    public sealed partial class MainWindow : Window
    {
        private WasapiLoopbackCapture? _capture;
        private WaveFileWriter? _writer;
        private bool _isRecording = false;
        private DispatcherTimer _timer;
        private int _secondsElapsed;


        public MainWindow()
        {
            this.InitializeComponent();
            var appWindow = this.AppWindow;
            appWindow.Resize(new Windows.Graphics.SizeInt32(400, 300));

            var titleBar = appWindow.TitleBar;
            titleBar.ExtendsContentIntoTitleBar = true;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            titleBar.ButtonForegroundColor = Colors.White;
            titleBar.ButtonInactiveForegroundColor = Colors.Gray;
            this.SetTitleBar(TitleBar);
            this.SystemBackdrop = new MicaBackdrop();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object? sender, object e)
        {
            _secondsElapsed++;

            int minutes = _secondsElapsed / 60;
            int seconds = _secondsElapsed % 60;

            RecordingTimer.Text = $"{minutes:00}:{seconds:00}";
        }

        private async void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isRecording)
            {
                _isRecording = true;

                StartRecording();
                RecordIcon.Glyph = "\uEE95";
                RecordIcon.Foreground = new SolidColorBrush(Colors.Red);

                _secondsElapsed = 0;
                RecordingTimer.Text = "00:00";
                RecordingTimer.Visibility = Visibility.Visible;
                _timer.Start();
            }
            else
            {
                _isRecording = false;

                await StopAndSaveRecording();
                RecordIcon.Glyph = "\uE720";
                RecordIcon.Foreground = new SolidColorBrush(Colors.White);

                _timer.Stop();
                RecordingTimer.Visibility = Visibility.Collapsed;
            }
        }

        private void StartRecording()
        {
            var device = new MMDeviceEnumerator()
                .GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            _capture = new WasapiLoopbackCapture(device);

            string music = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            string basePath = Path.Combine(music, "EchoCapture");

            Directory.CreateDirectory(basePath);

            string fileName = $"recording_{DateTime.Now:yyyyMMdd_HHmmss}.wav";
            string filePath = Path.Combine(basePath, fileName);

            _writer = new WaveFileWriter(filePath, _capture.WaveFormat);

            _capture.DataAvailable += (s, a) =>
            {
                _writer.Write(a.Buffer, 0, a.BytesRecorded);
            };

            _capture.StartRecording();
        }

        private async System.Threading.Tasks.Task StopAndSaveRecording()
        {
            _capture!.StopRecording();
            _capture!.Dispose();
            _writer!.Dispose();

            var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
            {
                Title = "Saved",
                Content = "Saved in Music/EchoCapture",
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }
}
