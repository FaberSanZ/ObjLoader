// Copyright (c) 2019-2020 Faber Leonardo. All Rights Reserved.

/*=============================================================================
	TextureOption.cs
=============================================================================*/

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace ObjLoader
{
	public struct TextureOption
	{
		public TextureType Type;        // -type (default TEXTURE_TYPE_NONE)
		public float Sharpness;         // -boost (default 1.0?)
		public float Brightness;        // base_value in -mm option (default 0)
		public float Contrast;          // gain_value in -mm option (default 1)
		public Vector3 OriginOffset;	// -o u [v [w]] (default 0 0 0)
		public Vector3 Scale;			// -s u [v [w]] (default 1 1 1)
		public Vector3 Turbulence;		// -t u [v [w]] (default 0 0 0)
										// int   texture_resolution; // -texres resolution (default = ?) TODO
		public bool Clamp;				// -clamp (default false)
		public char Imfchan;			// -imfchan (the default for bump is 'l' and for decal is 'm')
		public bool BlendU;				// -blendu (default on)
		public bool BlendV;				// -blendv (default on)
		public float BumpMultiplier;	// -bm (for bump maps only, default 1.0)
	}
}
