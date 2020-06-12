// Copyright (c) 2019-2020 Faber Leonardo. All Rights Reserved.

/*=============================================================================
	VertexIndex.cs
=============================================================================*/



using System;
using System.Collections.Generic;
using System.Text;

namespace ObjLoader
{
	public class VertexIndex
	{
		public int V_Index;
		public int VT_Index;
		public int VN_Index;

		public VertexIndex()
		{
			V_Index = -1;
			VT_Index = -1;
			VN_Index = -1;
		}

		public VertexIndex(int index)
		{
			V_Index = index;
			VT_Index = index;
			VN_Index = index;
		}

		public VertexIndex(int VIndex, int VTIndex, int VNIndex)
		{
			V_Index = VIndex;
			VT_Index = VTIndex;
			VN_Index = VNIndex;
		}
	}
}
