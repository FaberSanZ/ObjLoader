// Copyright (c) 2019-2020 Faber Leonardo. All Rights Reserved.

/*=============================================================================
	LoaderData.cs
=============================================================================*/

using System;
using System.Collections.Generic;
using System.Text;

namespace ObjLoader
{
	public class Attrib
	{
		public Attrib()
		{
			Vertices = new List<float>();
			Normals = new List<float>();
			Texcoords = new List<float>();
			Colors = new List<float>();
		}

		public List<float> Vertices;   // 'v'
		public List<float> Normals;    // 'vn'
		public List<float> Texcoords;  // 'vt'
		public List<float> Colors;     // extension: vertex colors

	}
}
