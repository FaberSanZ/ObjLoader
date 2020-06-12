// Copyright (c) 2019-2020 Faber Leonardo. All Rights Reserved.

/*=============================================================================
	MaterialFileReader.cs
=============================================================================*/


using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ObjLoader
{
	public abstract class MaterialReader
	{
		public abstract bool Load(string matId, List<Material> materials, Dictionary<string, int> matMap, out string err);
	}

	public class MaterialFileReader : MaterialReader
	{
		private readonly string m_mtlBaseDir;

		public MaterialFileReader(string mtl_basedir)
		{
			m_mtlBaseDir = mtl_basedir;
		}

		public override bool Load(string matId, List<Material> materials, Dictionary<string, int> matMap, out string err)
		{
			string filepath;
			err = null;

			if (!string.IsNullOrWhiteSpace(m_mtlBaseDir))
			{
				filepath = m_mtlBaseDir + matId;
			}
			else
			{
				filepath = matId;
			}

			try
			{
				using (StreamReader matIStream = new StreamReader(filepath))
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
				err = string.Format("WARN: Material file [ {0} ] not found: {1}", filepath, ex.Message);
				return false;
			}
		}
	}
}
