using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AppMover {
    public abstract class RegistryAction : FixAction {
        public string Key;

        protected static RegistryKey OpenKey(string path, bool writable) {
            string[] split = path.Split(new char[] { '\\' }, 2);
            RegistryKey key;
            switch (split[0]) {
                case "HKEY_CURRENT_USER":
                    key = Registry.CurrentUser;
                    break;
                case "HKEY_LOCAL_MACHINE":
                    key = Registry.LocalMachine;
                    break;
                case "HKEY_USERS":
                    key = Registry.Users;
                    break;
                default: throw new KeyNotFoundException("Cannot open key " + path);
            }
            return key.OpenSubKey(split[1], writable);
        }
    }

    public class RegistryValueEditAction : RegistryAction {
        public string Name;
        public object Data;
        public object NewData;

        public override string Summary
        {
            get
            {
                return "Fix registry value " + Key + "\\" + Name;
            }
        }

        public override string Detail
        {
            get
            {
                if (Data is string) {
                    return "Registry value " + Name + " of key " + Key + " will be changed from " + Data + " to " + NewData;
                } else {
                    return "Registry value " + Name + " of key " + Key + " will be changed from " + string.Join(";", (string[])Data) + " to " + string.Join(";", (string[])NewData);
                }
            }
        }

        public override Task Execute(Action<double> progress) {
            try {
                using (var k = OpenKey(Key, true)) {
                    var kind = k.GetValueKind(Name);
                    k.SetValue(Name, NewData, kind);
                }
                progress(1);
            } catch (Exception) {

            }
            return Task.CompletedTask;
        }

    }

    public class RegistryValueRenameAction : RegistryAction {
        public string Name;
        public string NewName;
        public override string Summary
        {
            get
            {
                return "Fix registry value " + Key + "\\" + Name;
            }
        }

        public override string Detail
        {
            get
            {
                return "Registry value " + Name + " of key " + Key + " will be renamed from " + Name + " to " + NewName;
            }
        }

        public override Task Execute(Action<double> progress) {
            try {
                using (var k = OpenKey(Key, true)) {
                    var kind = k.GetValueKind(Name);
                    var v = k.GetValue(Name);
                    k.SetValue(NewName, v, kind);
                    k.DeleteValue(Name);
                }
                progress(1);
            } catch (Exception) {
                
            }
            return Task.CompletedTask;
        }

    }
}
