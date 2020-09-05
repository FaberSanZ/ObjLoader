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

        private readonly TextReader _reader;

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

        public bool Eof { get; set; }


        public int Current => Peek(0);


        public int Peek(int offset)
        {
            int peek = _reader.Peek();

            return (peek is -1 || peek + offset is -1) ? '\0' : (peek + offset);
        }

        public string ReadWord()
        {
            if (Eof)
            {
                return string.Empty;
            }

            StringBuilder ret = new StringBuilder();

            while (Current is not -1)
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

            if (ret.Length is 0 || Current is -1)
            {
                Eof = true;
            }

            return ret.ToString();
        }

        public string[] ReadWordSplit(params char[] separator)
        {
            string str = ReadWord();

            if (str is not null)
            {
                return str.Split(separator);
            }

            return new string[0];
        }



        public int ReadInt(int value = 0)
        {
            string input = ReadWord();

            if (input is not null && int.TryParse(input, out int ret))
            {
                return ret;
            }

            return value;
        }

        public string[] ReadSplit(params char[] separator)
        {
            string str = _reader.ReadLine();
            if (str is null)
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
                Eof = Current is -1;
            }
        }
    }
}
