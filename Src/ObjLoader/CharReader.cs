// Copyright (c) 2019-2020 Faber Leonardo. All Rights Reserved.

/*=============================================================================
	CharReader.cs
=============================================================================*/



using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
            if (peek is -1 || peek + offset is -1)
            {
				return (char)(peek + offset);
			}

			return '\0';
		}

		public void MoveNext()
		{
			_input.Read();
		}
	}
}
