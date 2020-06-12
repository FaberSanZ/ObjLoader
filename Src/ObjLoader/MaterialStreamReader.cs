// Copyright (c) 2019-2020 Faber Leonardo. All Rights Reserved.

/*=============================================================================
	MaterialStreamReader.cs
=============================================================================*/



using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ObjLoader
{
	public class MaterialStreamReader : MaterialReader
	{
		private Stream m_Stream;

		public MaterialStreamReader(Stream inStream)
		{
			m_Stream = inStream;
		}

		public override bool Load(string matId, List<Material> materials, Dictionary<string, int> matMap, out string err)
		{
			err = null;

			if (!m_Stream.CanRead)
			{
				err = "WARN: Material stream in error state.";
				return false;
			}

			try
			{
				using (StreamReader matIStream = new StreamReader(m_Stream))
				{
					TinyObjLoader.LoadMtl(matMap, materials, matIStream, out string warning);

					if (!string.IsNullOrWhiteSpace(warning))
					{
						err = warning;
					}

					return true;
				}
			}
			catch (IOException ex)
			{
				err = string.Format("WARN: Material file not ready: {0}", ex.Message);
				return false;
			}
		}
	}
}
