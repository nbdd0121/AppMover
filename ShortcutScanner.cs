using System;
using System.Threading.Tasks;
using System.IO;
using IWshRuntimeLibrary;
using System.Runtime.InteropServices;
using System.Windows;

namespace AppMover {
    public class ShortcutScanner {

        private Action<FixAction> ops;
        private Action<double> progressReporter;
        private double overallProgress = 0;
        private string src;
        private string dest;
        public static WshShell Shell = new WshShell();

        private ShortcutScanner() {

        }

        private void Search(string path, double progress) {
            foreach (var v in Directory.EnumerateFiles(path)) {
                if (Path.GetExtension(v).ToLower() != ".lnk") {
                    continue;
                }
                try {
                    var link = (IWshShortcut)Shell.CreateShortcut(v);

                    if (link.TargetPath.Contains(src, StringComparison.OrdinalIgnoreCase)
                        || link.WorkingDirectory.Contains(src, StringComparison.OrdinalIgnoreCase)
                        || link.Arguments.Contains(src, StringComparison.OrdinalIgnoreCase)
                        || link.IconLocation.Contains(src, StringComparison.OrdinalIgnoreCase)) {
                        var a = new ShortcutAction();
                        a.Path = v;
                        a.Target = link.TargetPath;
                        a.Argument = link.Arguments;
                        a.WorkingDirectory = link.WorkingDirectory;
                        a.Icon = link.IconLocation;
                        a.NewTarget = link.TargetPath.Replace(src, dest, StringComparison.OrdinalIgnoreCase);
                        a.NewArgument = link.Arguments.Replace(src, dest, StringComparison.OrdinalIgnoreCase);
                        a.NewWorkingDirectory = link.WorkingDirectory.Replace(src, dest, StringComparison.OrdinalIgnoreCase);
                        a.NewIcon = link.IconLocation.Replace(src, dest, StringComparison.OrdinalIgnoreCase);
                        ops(a);
                    }
                } catch (COMException) {
                }
            }

            string[] dir = Directory.GetDirectories(path);
            double subprogress = progress / (dir.Length + 1);
            overallProgress += subprogress;
            progressReporter?.Invoke(overallProgress);

            foreach (var v in dir) {
                Search(v, subprogress);
            }

            return;
        }

        private void ScanSync() {
            Search(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), 0.6);
            Search(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), 0.4);
        }

        public static Task Scan(string src, string dest, Action<FixAction> recv, Action<double> progress = null) {
            return Task.Run(() => {
                try {
                    var scanner = new ShortcutScanner();
                    scanner.progressReporter = progress;
                    scanner.ops = recv;
                    scanner.src = src;
                    scanner.dest = dest;
                    scanner.ScanSync();
                }catch(Exception e) {
                    MessageBox.Show(e.ToString());
                }
            });
        }
    }
}
