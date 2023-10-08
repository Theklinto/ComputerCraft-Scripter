using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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
using Path = System.IO.Path;
using SendKeys = System.Windows.Forms.SendKeys;

namespace ComputerCraftScipter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private class ProcessModel
        {
            public string Name { get; set; }
            public Process Process { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }

        private static IEnumerable<ProcessModel> _processes;
        private IEnumerable<ProcessModel> GetProcesses(string searchInput = "")
        {
            if (_processes == null)
                _processes = Process.GetProcesses()
                    .Where(x => string.IsNullOrWhiteSpace(x.MainWindowTitle) is false)
                    .Select(x => new ProcessModel
                    {
                        Name = x.MainWindowTitle,
                        Process = x
                    });

            if (string.IsNullOrWhiteSpace(searchInput))
                return _processes;



            return _processes
                .Where(x => x.Name.ToLower().Contains(searchInput.ToLower()));
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ProgramLoaded(object sender, RoutedEventArgs e)
        {
            ProcessSelector.ItemsSource = GetProcesses();
        }

        private void ProcessSearch(object sender, KeyEventArgs e)
        {
            string searchString = ((TextBox)sender).Text;
            ProcessSelector.ItemsSource = GetProcesses(searchString);
        }

        private string _filePath = string.Empty;
        private void SelectFile(object sender, RoutedEventArgs e)
        {
            string folderPath = string.IsNullOrWhiteSpace(_filePath) ?
               Environment.GetFolderPath(Environment.SpecialFolder.Desktop) : Path.GetDirectoryName(_filePath);
            OpenFileDialog dialog = new OpenFileDialog
            {
                InitialDirectory = folderPath,
            };


            if (dialog.ShowDialog() == true)
            {
                _filePath = dialog.FileName;
                SelectedFile.Text = _filePath;
            }
        }

        [DllImport("User32.dll")]
        static extern int SetForegroundWindow(IntPtr point);

        private void ScriptFile(object sender, RoutedEventArgs e)
        {
            using (StreamReader reader = new StreamReader(_filePath))
            {
                Process selectedProcess = ((ProcessModel)ProcessSelector.SelectedItem).Process;
                SetForegroundWindow(selectedProcess.MainWindowHandle);

                while (reader.EndOfStream is false)
                {
                    string text = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(text) is false)
                    {
                        Clipboard.Clear();
                        Clipboard.SetText(text);
                        SendKeys.SendWait("^{v}");

                    }
                    SendKeys.SendWait("{ENTER}");
                    SendKeys.SendWait("{HOME}");
                }
            }
        }
    }
}
