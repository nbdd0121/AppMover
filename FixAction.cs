using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading.Tasks;
using System.Windows;

namespace AppMover {
    public abstract class FixAction {
        public abstract string Summary { get; }
        public abstract string Detail { get; }

        public abstract Task Execute(Action<double> progress);
    }

    public class MoveAction : FixAction {
        public string Src;
        public string Dest;

        public MoveAction(string src, string dest) {
            Src = src;
            Dest = dest;
        }

        public override string Summary
        {
            get
            {
                return "Move folder " + Src;
            }
        }

        public override string Detail
        {
            get
            {
                return "Move folder " + Src + " to " + Dest;
            }
        }

        public override Task Execute(Action<double> progress) {
            return Task.Run(() => {
                try {
                    Directory.Move(Src, Dest);
                    progress(1);
                } catch (Exception e) {
                    progress(-1);
                    MessageBox.Show(e.ToString());
                }
            });
        }
    }

    public class CopyAction : FixAction {
        public string Src;
        public string Dest;

        public CopyAction(string src, string dest) {
            Src = src;
            Dest = dest;
        }

        public override string Summary
        {
            get
            {
                return "Copy folder " + Src;
            }
        }

        public override string Detail
        {
            get
            {
                return "Copy folder " + Src + " to " + Dest + ". You can delete the old copy if the new copy is tested to be functional.";
            }
        }

        public override Task Execute(Action<double> progress) {
            return Task.Run(() => {
                try {
                    // Create directories structure first
                    foreach (var subdir in Directory.EnumerateDirectories(Src, "*", SearchOption.AllDirectories)) {
                        Directory.CreateDirectory(subdir.Replace(Src, Dest));
                    }
                    // Enumerate file and transform to FileInfo
                    var files = Directory.EnumerateFiles(Src, "*", SearchOption.AllDirectories).Select(x => new FileInfo(x));
                    // We add 4096 to each file to account for small files
                    long bytes = files.Sum(x => x.Length + 4096);
                    long copiedBytes = 0;
                    // Copy the files
                    foreach (var file in files) {
                        File.Copy(file.FullName, file.FullName.Replace(Src, Dest));
                        copiedBytes += file.Length + 4096;
                        progress(copiedBytes * 1d / bytes);
                    }
                } catch (Exception e) {
                    progress(-1);
                    MessageBox.Show(e.ToString());
                }
            });
        }
    }
}