using IWshRuntimeLibrary;
using System;
using System.Text;
using System.Threading.Tasks;

namespace AppMover {
    public class ShortcutAction : FixAction {
        public string Path;
        public string Target;
        public string WorkingDirectory;
        public string Argument;
        public string Icon;
        public string NewTarget;
        public string NewWorkingDirectory;
        public string NewArgument;
        public string NewIcon;

        public override string Summary
        {
            get
            {
                return "Fix shortcut " + Path;
            }
        }

        public override string Detail
        {
            get
            {
                var builder = new StringBuilder();
                builder.AppendLine("Affected File: " + Path);
                if (Target != NewTarget)
                    builder.AppendLine("Target: " + Target + " -> " + NewTarget);
                if (Argument != NewArgument)
                    builder.AppendLine("Argument: " + Argument + " -> " + NewArgument);
                if (WorkingDirectory != NewWorkingDirectory)
                    builder.Append("Start in: " + WorkingDirectory + " -> " + NewWorkingDirectory);
                if (Icon != NewIcon)
                    builder.Append("Icon: " + Icon + " -> " + NewIcon);
                return builder.ToString();
            }
        }

        public override Task Execute(Action<double> progress) {
            var shell = ShortcutScanner.Shell;
            var link = (IWshShortcut)shell.CreateShortcut(Path);
            link.TargetPath = NewTarget;
            link.Arguments = NewArgument;
            link.WorkingDirectory = NewWorkingDirectory;
            link.IconLocation = NewIcon;
            link.Save();
            progress(1);
            return Task.CompletedTask;
        }
    }
}
