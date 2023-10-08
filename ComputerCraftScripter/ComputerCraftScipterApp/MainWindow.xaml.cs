using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Path = System.IO.Path;
using SendKeys = System.Windows.Forms.SendKeys;

namespace ComputerCraftScipterApp
{
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
            ProcessSelector.IsDropDownOpen = true;
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
                    string text = reader.ReadLine().Trim();

                    Indentation indentation = null;

                    if (string.IsNullOrWhiteSpace(text) is false)
                    {
                        indentation = CalculateIndentationLevel(text);

                        if (indentation != null && indentation.Before < 0)
                        {
                            SendKeys.SendWait("{BS}");
                            Thread.Sleep(10);
                        }

                        Clipboard.Clear();
                        Clipboard.SetDataObject(text);
                        SendKeys.SendWait("^{v}");
                    }
                    Thread.Sleep(10);
                    SendKeys.SendWait("{ENTER}");
                    if (indentation != null && indentation.After > 0)
                    {
                        SendKeys.SendWait("{TAB}");
                        Thread.Sleep(10);
                    }
                }
            }
        }

        private static readonly List<Indentation> indentationChangers = new List<Indentation>
        {
            new Indentation(@"^elseif", before: -1, after: 1),
            new Indentation(@"then$", after: 1),
            new Indentation(@"\sfunction\s", after: 1),
            new Indentation(@"do$", after: 1),
            new Indentation(@"end$", before: -1),
            new Indentation(@"else$", after: 1, before: -1)
        };

        private class Indentation
        {
            public Indentation(string regex, int after = 0, int before = 0)
            {
                Regex = regex;
                After = after;
                Before = before;
            }
            public int Before { get; set; }
            public int After { get; set; }
            public string Regex { get; set; }
        }

        private static Indentation CalculateIndentationLevel(string text)
        {
            text = text.Trim().ToLower();
            foreach (Indentation indentation in indentationChangers)
                if (Regex.IsMatch(text, indentation.Regex))
                    return indentation;

            return null;
        }
    }
}
