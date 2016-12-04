using Microsoft.Win32;
using System;
using System.Security;
using System.Threading.Tasks;

namespace AppMover {
    public class RegistryScanner {

        private bool forceFullSearch = false;

        private Action<FixAction> ops;
        private Action<double> progressReporter;
        private double overallProgress = 0;
        private string src;
        private string dest;

        private RegistryScanner() {

        }

        private bool ShouldCheck(string subkey) {
            if (forceFullSearch) {
                return true;
            }
            switch (subkey) {
                case "AppCompatFlags":
                case "FirewallPolicy":
                case "MuiCache":
                    // exist for many executed programs, not particularly related to installation
                    return false;
                default: return true;
            }
        }

        private void Search(RegistryKey key, double progress) {
            foreach (var v in key.GetValueNames()) {
                // Process value of the key first
                bool multi = false;
                switch (key.GetValueKind(v)) {
                    case RegistryValueKind.String:
                    case RegistryValueKind.ExpandString:
                        break;
                    case RegistryValueKind.MultiString:
                        multi = true;
                        break;
                    default:
                        // Not a string value
                        goto procKey;
                }

                var value = key.GetValue(v);
                if (value != null) {
                    if (multi) {
                        var values = (string[])value;
                        bool match = false;
                        foreach (var sz in values) {
                            if (sz.Contains(src, StringComparison.OrdinalIgnoreCase)) {
                                match = true;
                                break;
                            }
                        }
                        if (match) {
                            var newData = new string[values.Length];
                            for (int i = 0; i < values.Length; i++) {
                                newData[i] = values[i].Replace(src, dest, StringComparison.OrdinalIgnoreCase);
                            }
                            var action = new RegistryValueEditAction();
                            action.Key = key.ToString();
                            action.Name = v;
                            action.Data = value;
                            action.NewData = newData;
                            ops(action);
                            break;
                        }
                    } else {
                        var val = (string)value;
                        if (val.Contains(src, StringComparison.OrdinalIgnoreCase)) {
                            var action = new RegistryValueEditAction();
                            action.Key = key.ToString();
                            action.Name = v;
                            action.Data = value;
                            action.NewData = ((string)value).Replace(src, dest, StringComparison.OrdinalIgnoreCase);
                            ops(action);
                        }
                    }
                }

                // Process key after value, so we can do the modification with original name
                // and rename afterwards
                procKey:
                if (v.Contains(src, StringComparison.OrdinalIgnoreCase)) {
                    var action = new RegistryValueRenameAction();
                    action.Key = key.ToString();
                    action.Name = v;
                    action.NewName = v.Replace(src, dest, StringComparison.OrdinalIgnoreCase);
                    ops(action);
                }
            }

            // Short-circuit check
            if (key.SubKeyCount == 0) {
                overallProgress += progress;
                progressReporter?.Invoke(overallProgress);
                return;
            }

            double subprogress = progress / (key.SubKeyCount + 1);
            overallProgress += subprogress;
            progressReporter?.Invoke(overallProgress);
            foreach (var v in key.GetSubKeyNames()) {
                // Opt-out some keys
                if (!ShouldCheck(v)) {
                    overallProgress += subprogress;
                    continue;
                }

                try {
                    using (var subkey = key.OpenSubKey(v)) {
                        Search(subkey, subprogress);
                    }
                } catch (SecurityException) {
                    overallProgress += subprogress;
                    progressReporter?.Invoke(overallProgress);
                }
            }
        }

        private void ScanSync() {
            if (forceFullSearch) {
                Search(Registry.LocalMachine, 0.9);
            } else {
                using (var key = Registry.LocalMachine.OpenSubKey("SOFTWARE")) {
                    Search(key, 0.8);
                }
                using (var key = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Services")) {
                    Search(key, 0.1);
                }
            }
            Search(Registry.Users, 0.1);
            progressReporter(1);
        }

        public static Task Scan(string src, string dest, Action<FixAction> recv, Action<double> progress = null) {
            return Task.Run(() => {
                var scanner = new RegistryScanner();
                scanner.progressReporter = progress;
                scanner.ops = recv;
                scanner.src = src;
                scanner.dest = dest;
                scanner.ScanSync();
            });
        }
    }
}
