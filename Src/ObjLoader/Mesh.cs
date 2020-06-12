// Copyright (c) 2019-2020 Faber Leonardo. All Rights Reserved.

/*=============================================================================
	Mesh.cs
=============================================================================*/



using System;
using System.Collections.Generic;
using System.Text;

namespace ObjLoader
{
	public class Mesh
	{
		public Mesh()
		{
			Indices = new List<IndexT>();
			NumFaceVertices = new List<byte>();
			MaterialIds = new List<int>();
			SmoothingGroupIds = new List<uint>();
			Tags = new List<Tag>();
		}

		public List<IndexT> Indices;
		public List<byte> NumFaceVertices;  //  The number of vertices perface. 3 = polygon, 4 = quad, ... Up to 255.                

		public List<int> MaterialIds;                 // per-face material ID
		public List<uint> SmoothingGroupIds;  // per-face smoothing group
											  // ID(0 = off. positive value = group id)
		public List<Tag> Tags;                        // SubD tag
	}
}
