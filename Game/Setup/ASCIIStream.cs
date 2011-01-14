#region

using System;
using System.IO;
using System.Text;

#endregion

namespace Game.Setup {
    class ASCIIStream : FileStream {
        private const int BUFFER_LEN = 1024;
        private const char DELIMITER = ',';

        private Byte[] data;
        private bool data_empty;
        private string data_string;
        private string[] data_strings;
        private int token_index;

        public ASCIIStream(string filename) : base(filename, FileMode.Open) {
            data = new byte[BUFFER_LEN];
            data_empty = true;
        }

        public override int Read(byte[] array, int index, int count) {
            int len, i = 0;
            string leftover = "";
            while (true) {
                if (data_empty) {
                    // Console.Out.Write('-');

                    if ((len = base.Read(data, leftover.Length, BUFFER_LEN - leftover.Length)) > 0) {
                        data_empty = false;
                        data_string = leftover + Encoding.UTF8.GetString(data, 0, len);
                        int last = data_string.LastIndexOf(',');

                        leftover = data_string.Substring(last);
                        if (leftover.Length > 1)
                            Console.Out.WriteLine(leftover);
                        data_string.Remove(last);
                        data_strings = data_string.Split(' ', '\n', '\r', ',');
                        token_index = 0;
                    } else {
                        if (i != count) {
                            Console.Out.WriteLine("no more to read, stopped at [{0}]", i);
                            return i;
                        }
                    }
                }
                while (token_index < data_strings.Length) {
                    if (Byte.TryParse(data_strings[token_index++], out array[index + i])) {
                        ++i;
                        //                   Console.Out.WriteLine(i);
                    }
                    //       Console.Out.Write('.');
                    if (i == count)
                        return count;
                }
                if (token_index >= data_strings.Length)
                    data_empty = true;
            }
        }
    }
}