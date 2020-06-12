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
		private readonly StringReader m_input;

		public CharReader(string input)
		{
			m_input = new StringReader(input);
		}

		public bool Eof => m_input.Peek() == -1;

		public char Current
		{
			get
			{
				int val = m_input.Peek();
				if (val == -1)
				{
					return (char)0;
				}

				return (char)val;
			}
		}

		public void MoveNext()
		{
			m_input.Read();
		}
	}
}
