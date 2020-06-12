// Copyright (c) 2019-2020 Faber Leonardo. All Rights Reserved.

/*=============================================================================
	Material.cs
=============================================================================*/


using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace ObjLoader
{
	public class Material
	{
		public string Name;

		public Vector3 Ambient;
		public Vector3 Diffuse;
		public Vector3 Specular;
		public Vector3 Transmittance;
		public Vector3 Emission;
		public float Shininess;
		public float Ior;       // index of refraction
		public float Dissolve;  // 1 == opaque; 0 == fully transparent
								
		public int Illum;       // illumination model (see http://www.fileformat.info/format/material/)

		public int Dummy;  // Suppress padding warning.

		public string AmbientTexName;             // map_Ka
		public string DiffuseTexName;             // map_Kd
		public string SpecularTexName;            // map_Ks
		public string SpecularHighlightTexName;   // map_Ns
		public string BumpTexName;                // map_bump, map_Bump, bump
		public string DisplacementTexName;        // disp
		public string AlphaTexName;               // map_d
		public string ReflectionTexName;          // refl

		public TextureOption AmbientTexOpt;
		public TextureOption DiffuseTexOpt;
		public TextureOption SpecularTexOpt;
		public TextureOption SpecularHighlightTexOpt;
		public TextureOption BumpTexOpt;
		public TextureOption DisplacementTexOpt;
		public TextureOption AlphaTexOpt;
		public TextureOption ReflectionTexOpt;

		// PBR extension
		// http://exocortex.com/blog/extending_wavefront_mtl_to_support_pbr
		public float Roughness;            // [0, 1] default 0
		public float Metallic;             // [0, 1] default 0
		public float Sheen;                // [0, 1] default 0
		public float ClearcoatThickness;  // [0, 1] default 0
		public float ClearcoatRoughness;  // [0, 1] default 0
		public float Anisotropy;           // aniso. [0, 1] default 0
		public float AnisotropyRotation;  // anisor. [0, 1] default 0
		public float Pad0;
		public string RoughnessTexName;  // map_Pr
		public string MetallicTexName;   // map_Pm
		public string SheenTexName;      // map_Ps
		public string EmissiveTexName;   // map_Ke
		public string NormalTexName;     // norm. For normal mapping.

		public TextureOption RoughnessTexOpt;
		public TextureOption MetallicTexOpt;
		public TextureOption SheenTexOpt;
		public TextureOption EmissiveTexOpt;
		public TextureOption NormalTexOpt;

		public int Pad2;

		public Dictionary<string, string> UnknownParameter;

		public Material()
		{
			UnknownParameter = new Dictionary<string, string>();
		}
	}
}
