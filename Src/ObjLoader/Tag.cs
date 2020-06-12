// Copyright (c) 2019-2020 Faber Leonardo. All Rights Reserved.

/*=============================================================================
	Tag.cs
=============================================================================*/


using System;
using System.Collections.Generic;
using System.Text;

namespace ObjLoader
{
	public class Tag
	{
		public string Name;

		public List<int> IntValues;
		public List<float> FloatValues;
		public List<string> StringValues;

		public Tag()
		{
			IntValues = new List<int>();
			FloatValues = new List<float>();
			StringValues = new List<string>();
		}
	}
}
