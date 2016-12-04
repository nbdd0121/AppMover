using System;
using System.Text;

namespace AppMover {
    public static class StringExtension {
        public static bool Contains(this string str, string substr, StringComparison comp) {
            if (str == null) {
                throw new ArgumentNullException("str");
            }
            if (substr == null) {
                throw new ArgumentNullException("substr");
            }
            return str.IndexOf(substr, comp) >= 0;
        }

        public static string Replace(this string str, string oldValue, string newValue, StringComparison comparison) {
            if (str == null) {
                throw new ArgumentNullException("str");
            }
            if (oldValue == null) {
                throw new ArgumentNullException("oldValue");
            }
            if (newValue == null) {
                throw new ArgumentNullException("newValue");
            }
            if (oldValue == "") {
                throw new ArgumentException("String cannot be of zero length.", "oldValue");
            }

            StringBuilder sb = new StringBuilder();

            int previousIndex = 0;
            int index = str.IndexOf(oldValue, comparison);
            while (index != -1) {
                sb.Append(str.Substring(previousIndex, index - previousIndex));
                sb.Append(newValue);
                index += oldValue.Length;

                previousIndex = index;
                index = str.IndexOf(oldValue, index, comparison);
            }
            sb.Append(str.Substring(previousIndex));

            return sb.ToString();
        }
    }
}
