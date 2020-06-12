// Copyright (c) 2019-2020 Faber Leonardo. All Rights Reserved.

/*=============================================================================
	Shape.cs
=============================================================================*/


using System;
using System.Collections.Generic;
using System.Text;

namespace ObjLoader
{
	public class Shape
	{
		public string Name;
		public Mesh Mesh;

		public Shape()
		{
			Mesh = new Mesh();
		}
	}
}
