// Copyright (c) 2019-2020 Faber Leonardo. All Rights Reserved.

/*=============================================================================
	IndexT.cs
=============================================================================*/



using System;
using System.Collections.Generic;
using System.Text;

namespace ObjLoader
{
	// Index struct to support different indices for vtx/normal/texcoord.
	// -1 means not used.
	public class IndexT
	{
		public int VertexIndex;
		public int NormalIndex;
		public int TexcoordIndex;
	}
}
