// Copyright (c) 2019-2020 Faber Leonardo. All Rights Reserved.

/*=============================================================================
	LineReader.cs
=============================================================================*/


using System.IO;
using System.Text;

namespace ObjLoader
{
    public class LineReader
    {
        public LineReader(string inputLine)
        {
            if (inputLine == null)
            {
                Eof = true;
            }
            else
            {
                _reader = new StringReader(inputLine);
            }
        }

        private readonly TextReader _reader;
        public bool Eof { get; set; }



        public string ReadWord()
        {
            if (Eof)
            {
                return "";
            }

            StringBuilder ret = new StringBuilder();

            while (_reader.Peek() != -1)
            {
                char c = (char)_reader.Read();
                if (c == ' ' || c == '\t' || c == '\r' || c == '\n' || c == '\0')
                {
                    if (ret.Length > 0)
                    {
                        break;
                    }

                    continue;

                }
                ret.Append(c);
            }

            if (ret.Length == 0 || _reader.Peek() == -1)
            {
                Eof = true;
            }

            return ret.ToString();
        }

        public string[] ReadWordSplit(params char[] separator)
        {
            string str = ReadWord();
            if (str != null)
            {
                return str.Split(separator);
            }

            return new string[0];
        }



        public int ReadInt(int value = 0)
        {
            string input = ReadWord();

            if (input != null && int.TryParse(input, out int ret))
            {
                return ret;
            }

            return value;
        }

        public string[] ReadSplit(params char[] separator)
        {
            string str = _reader.ReadLine();
            if (str == null)
            {
                return new string[0];
            }

            return str.Split(separator);
        }

        public string ReadLine()
        {
            try
            {
                return _reader.ReadLine();
            }
            finally
            {
                Eof = _reader.Peek() == -1;
            }
        }
    }
}
