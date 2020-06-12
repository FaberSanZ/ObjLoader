// Copyright (c) 2019-2020 Faber Leonardo. All Rights Reserved.

/*=============================================================================
	LoaderData.cs
=============================================================================*/


using System;
using System.Collections.Generic;
using System.Text;

namespace ObjLoader
{
	public class LoaderData
	{
		public LoaderData()
		{

		}

		public Attrib Attrib = new Attrib();

		public List<Shape> Shapes = new List<Shape>();

		public List<Material> Materials = new List<Material>();
	}
}
