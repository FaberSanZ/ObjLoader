// Copyright (c) 2019-2020 Faber Leonardo. All Rights Reserved.

/*=============================================================================
	CharReader.cs
=============================================================================*/



using System.IO;

namespace ObjLoader
{
    public class CharReader
    {
        private readonly StringReader _input;

        public CharReader(string input)
        {
            _input = new StringReader(input);
        }

        public bool Eof => _input.Peek() is -1;

        public char Current => Peek(0);

        public char Peek(int offset)
        {
            int peek = _input.Peek();

            return (peek is -1 || peek + offset is -1) ? '\0' : (char)(peek + offset);
        }

        public void MoveNext()
        {
            _input.Read();
        }
    }
}
