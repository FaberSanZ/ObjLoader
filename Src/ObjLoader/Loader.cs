// Copyright (c) 2019-2020 Faber Leonardo. All Rights Reserved.

/*=============================================================================
	Loader.cs
=============================================================================*/



using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace ObjLoader
{
	// ------ TODO: OBJ cleaning and optimization ------
	//
	//--------------------------------------------------


	// Internal data structure for face representation
	// index + smoothing group.
	public class face_t
	{

		public face_t()
		{
			vertex_indices = new List<VertexIndex>();
		}
		public uint smoothing_group_id;  // smoothing group id. 0 = smoothing groupd is off.
		public int pad_;
		public List<VertexIndex> vertex_indices;  // face vertex indices.

	}

	public class tag_sizes
	{
		public int num_ints;
		public int num_reals;
		public int num_strings;
	}

	public class obj_shape
	{

		public obj_shape()
		{
			v = new List<float>();
			vn = new List<float>();
			vt = new List<float>();
		}

		public List<float> v;
		public List<float> vn;
		public List<float> vt;
	}

	public static class TinyObjLoader
	{

		private static bool IS_SPACE(char x)
		{
			return ((x == ' ') || (x == '\t'));
		}

		private static bool IS_DIGIT(char x)
		{
			return ((uint)(x - '0') < 10);
		}

		private static bool IS_NEW_LINE(char x)
		{
			return ((x == '\r') || (x == '\n') || (x == '\0'));
		}




		// Make index zero-base, and also support relative index.
		private static bool fixIndex(int idx, int n, ref int ret)
		{
			if (idx > 0)
			{
				ret = idx - 1;
				return true;
			}

			if (idx == 0)
			{
				// zero is not allowed according to the spec.
				return false;
			}

			if (idx < 0)
			{
				ret = n + idx;  // negative value = relative
				return true;
			}

			return false;  // never reach here.
		}

		private static string parseString(LineReader token)
		{
			return token.ReadWord();
		}

		private static int parseInt(LineReader token)
		{
			return token.ReadInt();
		}

		// Tries to parse a floating point number located at s.
		//
		// s_end should be a location in the string where reading should absolutely
		// stop. For example at the end of the string, to prevent buffer overflows.
		//
		// Parses the following EBNF grammar:
		//   sign    = "+" | "-" ;
		//   END     = ? anything not in digit ?
		//   digit   = "0" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" ;
		//   integer = [sign] , digit , {digit} ;
		//   decimal = integer , ["." , integer] ;
		//   float   = ( decimal , END ) | ( decimal , ("E" | "e") , integer , END ) ;
		//
		//  Valid strings are for example:
		//   -0  +3.1417e+2  -0.0E-3  1.0324  -1.41   11e2
		//
		// If the parsing is a success, result is set to the parsed value and true
		// is returned.
		//
		// The function is greedy and will parse until any of the following happens:
		//  - a non-conforming character is encountered.
		//  - s_end is reached.
		//
		// The following situations triggers a failure:
		//  - s >= s_end.
		//  - parse failure.
		//
		private static bool tryParseDouble(string s, object s_end, ref double result)
		{
			double mantissa = 0.0;

			// This exponent is base 2 rather than 10.
			// However the exponent we parse is supposed to be one of ten,
			// thus we must take care to convert the exponent/and or the
			// mantissa to a * 2^E, where a is the mantissa and E is the
			// exponent.
			// To get the final double we will use ldexp, it requires the
			// exponent to be in base 2.
			int exponent = 0;

			// NOTE: THESE MUST BE DECLARED HERE SINCE WE ARE NOT ALLOWED
			// TO JUMP OVER DEFINITIONS.
			char sign = '+';
			char exp_sign = '+';
			CharReader curr = new CharReader(s);

			// How many characters were read in a loop.
			int read = 0;
			// Tells whether a loop terminated due to reaching s_end.
			bool end_not_reached = false;

			/*
					BEGIN PARSING.
			*/

			// Find out what sign we've got.
			if (curr.Current == '+' || curr.Current == '-')
			{
				sign = curr.Current;
				curr.MoveNext();
			}
			else if (IS_DIGIT(curr.Current))
			{ /* Pass through. */
			}
			else
			{
				goto fail;
			}

			// Read the integer part.
			while (!curr.Eof && IS_DIGIT(curr.Current))
			{
				mantissa *= 10;
				mantissa += curr.Current - 0x30;
				curr.MoveNext();
				read++;
			}

			// We must make sure we actually got something.
			if (read == 0)
			{
				goto fail;
			}
			// We allow numbers of form "#", "###" etc.
			if (curr.Eof)
			{
				goto assemble;
			}

			// Read the decimal part.
			if (curr.Current == '.')
			{
				curr.MoveNext();
				read = 1;
				// end_not_reached = (curr != s_end);
				while ((!curr.Eof) && IS_DIGIT(curr.Current))
				{
					double[] pow_lut = { 1.0, 0.1, 0.01, 0.001, 0.0001, 0.00001, 0.000001, 0.0000001, };
					int lut_entries = pow_lut.Length;

					// NOTE: Don't use powf here, it will absolutely murder precision.
					mantissa += (curr.Current - 0x30) *
								(read < lut_entries ? pow_lut[read] : Math.Pow(10.0, -read));
					read++;
					curr.MoveNext();
				}
			}
			else if (curr.Current == 'e' || curr.Current == 'E')
			{
			}
			else
			{
				goto assemble;
			}

			if (curr.Eof)
			{
				goto assemble;
			}

			// Read the exponent part.
			if (curr.Current == 'e' || curr.Current == 'E')
			{
				curr.MoveNext();
				// Figure out if a sign is present and if it is.

				if ((!curr.Eof) && (curr.Current == '+' || curr.Current == '-'))
				{
					exp_sign = curr.Current;
					curr.MoveNext();
				}
				else if (IS_DIGIT(curr.Current))
				{ /* Pass through. */
				}
				else
				{
					// Empty E is not allowed.
					goto fail;
				}

				read = 0;

				while ((!curr.Eof) && IS_DIGIT(curr.Current))
				{
					exponent *= 10;
					exponent += curr.Current - 0x30;
					curr.MoveNext();
					read++;
					end_not_reached = (curr != s_end);
				}
				exponent *= (exp_sign == '+' ? 1 : -1);
				if (read == 0)
				{
					goto fail;
				}
			}

		assemble:
			result = (sign == '+' ? 1 : -1) *
					  (exponent != 0 ? std_ldexp(mantissa * Math.Pow(5.0, exponent), exponent) : mantissa);
			return true;

		fail:
			return false;
		}

		private static double std_ldexp(double number, int exponent)
		{
			// return = number * 2 ^ exponent
			return number * Math.Pow(2, exponent);
		}

		private static float parseReal(LineReader token, double default_value = 0.0)
		{
			double val = default_value;
			tryParseDouble(token.ReadWord(), null, ref val);
			return (float)val;
		}

		private static bool parseReal(LineReader token, ref float outVal)
		{
			double val = 0;
			bool ret = tryParseDouble(token.ReadWord(), null, ref val);
			if (ret)
			{
				outVal = (float)val;
			}
			return ret;
		}

		private static void parseReal2(out float x, out float y, LineReader token, double default_x = 0.0, double default_y = 0.0)
		{
			x = parseReal(token, default_x);
			y = parseReal(token, default_y);
		}

		private static void parseReal3(out float x, out float y, out float z, LineReader token, double default_x = 0.0, double default_y = 0.0, double default_z = 0.0)
		{
			x = parseReal(token, default_x);
			y = parseReal(token, default_y);
			z = parseReal(token, default_z);
		}

		private static void parseV(out float x, out float y, out float z, out float w, LineReader token, double default_x = 0.0, double default_y = 0.0, double default_z = 0.0, double default_w = 1.0)
		{
			x = parseReal(token, default_x);
			y = parseReal(token, default_y);
			z = parseReal(token, default_z);
			w = parseReal(token, default_w);
		}

		// Extension: parse vertex with colors(6 items)
		private static void parseVertexWithColor(out float x, out float y, out float z, out float r, out float g, out float b, LineReader token, double default_x = 0.0, double default_y = 0.0, double default_z = 0.0)
		{
			x = parseReal(token, default_x);
			y = parseReal(token, default_y);
			z = parseReal(token, default_z);

			r = parseReal(token, 1.0);
			g = parseReal(token, 1.0);
			b = parseReal(token, 1.0);
		}

		private static bool parseOnOff(LineReader token, bool default_value = true)
		{
			bool ret = default_value;
			string str = token.ReadWord();
			if (str != null)
			{
				if (str == "on")
				{
					ret = true;
				}
				else if (str == "off")
				{
					ret = false;
				}
			}
			return ret;
		}

		private static TextureType ParseTextureType(LineReader token, TextureType default_value = TextureType.None)
		{
			string str = token.ReadWord();

			if (str == null)
			{
				return default_value;
			}

			switch (str)
			{
				case "cube_top":
					return TextureType.CubeTop;

				case "cube_bottom":
					return TextureType.CubeBottom;

				case "cube_left":
					return TextureType.CubeLeft;

				case "cube_right":
					return TextureType.CubeRight;

				case "cube_front":
					return TextureType.CubeFront;

				case "cube_back":
					return TextureType.CubeBack;

				case "sphere":
					return TextureType.Sphere;
			}

			return default_value;
		}

		private static tag_sizes parseTagTriple(LineReader token)
		{
			tag_sizes ts = new tag_sizes();
			string[] list = token.ReadWordSplit('/');

			if (list.Length >= 1)
			{
				ts.num_ints = atoi(list[0]);
			}

			if (list.Length >= 2)
			{
				ts.num_reals = atoi(list[1]);
			}

			if (list.Length >= 3)
			{
				ts.num_strings = atoi(list[2]);
			}

			return ts;
		}

		// Parse triples with index offsets: i, i/j/k, i//k, i/j
		private static bool ParseTriple(LineReader token, int vsize, int vnsize, int vtsize, ref VertexIndex ret)
		{
			VertexIndex vi = new VertexIndex(-1);

			string[] list = token.ReadWordSplit('/');

			if (list.Length >= 1 && list[0].Length > 0)
			{
				if (!fixIndex(atoi(list[0]), vsize, ref vi.V_Index))
				{
					return false;
				}
			}

			if (list.Length >= 2 && list[1].Length > 0)
			{
				if (!fixIndex(atoi(list[1]), vtsize, ref vi.VT_Index))
				{
					return false;
				}
			}

			if (list.Length >= 3 && list[2].Length > 0)
			{
				if (!fixIndex(atoi(list[2]), vnsize, ref vi.VN_Index))
				{
					return false;
				}
			}

			ret = vi;
			return true;
		}

		// Parse raw triples: i, i/j/k, i//k, i/j
		private static VertexIndex ParseRawTriple(LineReader token)
		{
			VertexIndex vi = new VertexIndex(0); // 0 is an invalid index in OBJ

			string[] list = token.ReadWordSplit('/');

			if (list.Length >= 1 && list[0].Length > 0)
			{
				vi.V_Index = atoi(list[0]);
			}

			if (list.Length >= 2 && list[1].Length > 0)
			{
				vi.VT_Index = atoi(list[1]);
			}

			if (list.Length >= 1 && list[2].Length > 0)
			{
				vi.VN_Index = atoi(list[2]);
			}

			return vi;
		}

		private static bool ParseTextureNameAndOption(out string texname, ref TextureOption texopt, LineReader linebuf, bool is_bump)
		{
			// @todo { write more robust lexer and parser. }
			bool found_texname = false;
			string texture_name = null;

			// Fill with default value for texopt.
			if (is_bump)
			{
				texopt.Imfchan = 'l';
			}
			else
			{
				texopt.Imfchan = 'm';
			}
			texopt.BumpMultiplier = 1.0f;
			texopt.Clamp = false;
			texopt.BlendU = true;
			texopt.BlendV = true;
			texopt.Sharpness = 1.0f;
			texopt.Brightness = 0.0f;
			texopt.Contrast = 1.0f;
			texopt.OriginOffset.X = 0.0f;
			texopt.OriginOffset.Y = 0.0f;
			texopt.OriginOffset.Z = 0.0f;
			texopt.Scale.X = 1.0f;
			texopt.Scale.Y = 1.0f;
			texopt.Scale.Z = 1.0f;
			texopt.Turbulence.X = 0.0f;
			texopt.Turbulence.Y = 0.0f;
			texopt.Turbulence.Z = 0.0f;
			texopt.Type = TextureType.None;

			string token;

			while (linebuf.Eof)
			{
				token = linebuf.ReadWord();
				if (string.IsNullOrWhiteSpace(token))
				{
					break;
				}

				if (token == "-blendu")
				{
					texopt.BlendU = parseOnOff(linebuf, /* default */ true);
				}
				else if (token == "-blendv")
				{
					texopt.BlendV = parseOnOff(linebuf, /* default */ true);
				}
				else if (token == "-clamp")
				{
					texopt.Clamp = parseOnOff(linebuf, /* default */ true);
				}
				else if (token == "-boost")
				{
					texopt.Sharpness = parseReal(linebuf, 1.0);
				}
				else if (token == "-bm")
				{
					texopt.BumpMultiplier = parseReal(linebuf, 1.0);
				}
				else if (token == "-o")
				{
					parseReal3(out float x, out float y, out float z, linebuf);
					texopt.OriginOffset.X = x;
					texopt.OriginOffset.Y = y;
					texopt.OriginOffset.Z = z;
				}
				else if (token == "-s")
				{
					parseReal3(out float x, out float y, out float z, linebuf, 1, 1, 1);
					texopt.Scale.X = x;
					texopt.Scale.Y = y;
					texopt.Scale.Z = z;
				}
				else if (token == "-t")
				{
					parseReal3(out float x, out float y, out float z, linebuf);
					texopt.Turbulence.X = x;
					texopt.Turbulence.Y = y;
					texopt.Turbulence.Z = z;
				}
				else if (token == "-type")
				{
					texopt.Type = ParseTextureType(linebuf, TextureType.None);
				}
				else if (token == "-imfchan")
				{
					string str = linebuf.ReadWord();
					if (str != null && str.Length >= 1)
					{
						texopt.Imfchan = str[0];
					}
				}
				else if (token == "-mm")
				{
					parseReal2(out texopt.Brightness, out texopt.Contrast, linebuf, 0.0, 1.0);
				}
				else
				{
					// Assume texture filename
					// Read filename until line end to parse filename containing whitespace
					// TODO(syoyo): Support parsing texture option flag after the filename.
					texture_name = token;
					found_texname = true;
				}
			}

			if (found_texname)
			{
				texname = texture_name;
				return true;
			}
			else
			{
				texname = "";
				return false;
			}
		}

		private static void InitMaterial(ref Material material)
		{
			material = new Material
			{
				Name = "",
				AmbientTexName = "",
				DiffuseTexName = "",
				SpecularTexName = "",
				SpecularHighlightTexName = "",
				BumpTexName = "",
				DisplacementTexName = "",
				ReflectionTexName = "",
				AlphaTexName = ""
			};

			for (int i = 0; i < 3; i++)
			{
				material.Ambient = new Vector3(0f);
				material.Diffuse = new Vector3(0f);
				material.Specular = new Vector3(0f);
				material.Transmittance = new Vector3(0f);
				material.Emission = new Vector3(0f);
			}

			material.Illum = 0;
			material.Dissolve = 1f;
			material.Shininess = 1f;
			material.Ior = 1f;

			material.Roughness = 0f;
			material.Metallic = 0f;
			material.Sheen = 0f;
			material.ClearcoatThickness = 0f;
			material.ClearcoatRoughness = 0f;
			material.AnisotropyRotation = 0f;
			material.Anisotropy = 0f;
			material.RoughnessTexName = "";
			material.MetallicTexName = "";
			material.SheenTexName = "";
			material.EmissiveTexName = "";
			material.NormalTexName = "";

			material.UnknownParameter.Clear();
		}

		// code from https://wrf.ecse.rpi.edu//Research/Short_Notes/pnpoly.html
		private static bool pnpoly(int nvert, float[] vertx, float[] verty, float testx, float testy)
		{
			int i, j;
			bool c = false;
			for (i = 0, j = nvert - 1; i < nvert; j = i++)
			{
				if (((verty[i] > testy) != (verty[j] > testy)) &&
					(testx < (vertx[j] - vertx[i]) * (testy - verty[i]) / (verty[j] - verty[i]) + vertx[i]))
				{
					c = !c;
				}
			}
			return c;
		}

		private static bool exportFaceGroupToShape(ref Shape shape, List<face_t> faceGroup, List<Tag> tags, int material_id, string name, bool triangulate, List<float> v)
		{
			if (!faceGroup.Any())
			{
				return false;
			}

			// Flatten vertices and indices
			for (int i = 0; i < faceGroup.Count; i++)
			{
				face_t face = faceGroup[i];

				if (face.vertex_indices.Count < 3)
				{
					// Face must have 3+ vertices.
					continue;
				}

				VertexIndex i0 = face.vertex_indices[0];
				VertexIndex i1 = new VertexIndex(-1);
				VertexIndex i2 = face.vertex_indices[1];

				int npolys = face.vertex_indices.Count;

				if (triangulate)
				{
					// find the two axes to work in
					int[] axes = new int[] { 1, 2 };
					for (int k = 0; k < npolys; ++k)
					{
						i0 = face.vertex_indices[(k + 0) % npolys];
						i1 = face.vertex_indices[(k + 1) % npolys];
						i2 = face.vertex_indices[(k + 2) % npolys];
						int vi0 = i0.V_Index;
						int vi1 = i1.V_Index;
						int vi2 = i2.V_Index;
						float v0x = v[vi0 * 3 + 0];
						float v0y = v[vi0 * 3 + 1];
						float v0z = v[vi0 * 3 + 2];
						float v1x = v[vi1 * 3 + 0];
						float v1y = v[vi1 * 3 + 1];
						float v1z = v[vi1 * 3 + 2];
						float v2x = v[vi2 * 3 + 0];
						float v2y = v[vi2 * 3 + 1];
						float v2z = v[vi2 * 3 + 2];
						float e0x = v1x - v0x;
						float e0y = v1y - v0y;
						float e0z = v1z - v0z;
						float e1x = v2x - v1x;
						float e1y = v2y - v1y;
						float e1z = v2z - v1z;
						float cx = Math.Abs(e0y * e1z - e0z * e1y);
						float cy = Math.Abs(e0z * e1x - e0x * e1z);
						float cz = Math.Abs(e0x * e1y - e0y * e1x);
						const float epsilon = 0.0001f;
						if (cx > epsilon || cy > epsilon || cz > epsilon)
						{
							// found a corner
							if (cx > cy && cx > cz)
							{
							}
							else
							{
								axes[0] = 0;
								if (cz > cx && cz > cy)
								{
									axes[1] = 1;
								}
							}
							break;
						}
					}

					float area = 0;
					for (int k = 0; k < npolys; ++k)
					{
						i0 = face.vertex_indices[(k + 0) % npolys];
						i1 = face.vertex_indices[(k + 1) % npolys];
						int vi0 = i0.V_Index;
						int vi1 = i1.V_Index;
						float v0x = v[vi0 * 3 + axes[0]];
						float v0y = v[vi0 * 3 + axes[1]];
						float v1x = v[vi1 * 3 + axes[0]];
						float v1y = v[vi1 * 3 + axes[1]];
						area += (v0x * v1y - v0y * v1x) * 0.5f;
					}

					int maxRounds =
						10;  // arbitrary max loop count to protect against unexpected errors

					face_t remainingFace = face;  // copy
					int guess_vert = 0;
					VertexIndex[] ind = new VertexIndex[3];
					float[] vx = new float[3];
					float[] vy = new float[3];
					while (remainingFace.vertex_indices.Count > 3 && maxRounds > 0)
					{
						npolys = remainingFace.vertex_indices.Count;
						if (guess_vert >= npolys)
						{
							maxRounds -= 1;
							guess_vert -= npolys;
						}
						for (int k = 0; k < 3; k++)
						{
							ind[k] = remainingFace.vertex_indices[(guess_vert + k) % npolys];
							int vi = ind[k].V_Index;
							vx[k] = v[vi * 3 + axes[0]];
							vy[k] = v[vi * 3 + axes[1]];
						}
						float e0x = vx[1] - vx[0];
						float e0y = vy[1] - vy[0];
						float e1x = vx[2] - vx[1];
						float e1y = vy[2] - vy[1];
						float cross = e0x * e1y - e0y * e1x;
						// if an internal angle
						if (cross * area < 0.0f)
						{
							guess_vert += 1;
							continue;
						}

						// check all other verts in case they are inside this triangle
						bool overlap = false;
						for (int otherVert = 3; otherVert < npolys; ++otherVert)
						{
							int ovi = remainingFace.vertex_indices[(guess_vert + otherVert) % npolys].V_Index;
							float tx = v[ovi * 3 + axes[0]];
							float ty = v[ovi * 3 + axes[1]];
							if (pnpoly(3, vx, vy, tx, ty))
							{
								overlap = true;
								break;
							}
						}

						if (overlap)
						{
							guess_vert += 1;
							continue;
						}

						// this triangle is an ear
						{
							IndexT idx0 = new IndexT();
							IndexT idx1 = new IndexT();
							IndexT idx2 = new IndexT();
							idx0.VertexIndex = ind[0].V_Index;
							idx0.NormalIndex = ind[0].VN_Index;
							idx0.TexcoordIndex = ind[0].VT_Index;
							idx1.VertexIndex = ind[1].V_Index;
							idx1.NormalIndex = ind[1].VN_Index;
							idx1.TexcoordIndex = ind[1].VT_Index;
							idx2.VertexIndex = ind[2].V_Index;
							idx2.NormalIndex = ind[2].VN_Index;
							idx2.TexcoordIndex = ind[2].VT_Index;

							shape.Mesh.Indices.Add(idx0);
							shape.Mesh.Indices.Add(idx1);
							shape.Mesh.Indices.Add(idx2);

							shape.Mesh.NumFaceVertices.Add(3);
							shape.Mesh.MaterialIds.Add(material_id);
							shape.Mesh.SmoothingGroupIds.Add(face.smoothing_group_id);
						}

						// remove v1 from the list
						int removed_vert_index = (guess_vert + 1) % npolys;
						while (removed_vert_index + 1 < npolys)
						{
							remainingFace.vertex_indices[removed_vert_index] = remainingFace.vertex_indices[removed_vert_index + 1];
							removed_vert_index += 1;
						}
						remainingFace.vertex_indices.RemoveAt(remainingFace.vertex_indices.Count - 1);
					}

					if (remainingFace.vertex_indices.Count == 3)
					{
						i0 = remainingFace.vertex_indices[0];
						i1 = remainingFace.vertex_indices[1];
						i2 = remainingFace.vertex_indices[2];
						{
							IndexT idx0 = new IndexT();
							IndexT idx1 = new IndexT();
							IndexT idx2 = new IndexT();

							idx0.VertexIndex = i0.V_Index;
							idx0.NormalIndex = i0.VN_Index;
							idx0.TexcoordIndex = i0.VT_Index;
							idx1.VertexIndex = i1.V_Index;
							idx1.NormalIndex = i1.VN_Index;
							idx1.TexcoordIndex = i1.VT_Index;
							idx2.VertexIndex = i2.V_Index;
							idx2.NormalIndex = i2.VN_Index;
							idx2.TexcoordIndex = i2.VT_Index;

							shape.Mesh.Indices.Add(idx0);
							shape.Mesh.Indices.Add(idx1);
							shape.Mesh.Indices.Add(idx2);

							shape.Mesh.NumFaceVertices.Add(3);
							shape.Mesh.MaterialIds.Add(material_id);
							shape.Mesh.SmoothingGroupIds.Add(face.smoothing_group_id);
						}
					}
				}
				else
				{
					for (int k = 0; k < npolys; k++)
					{
						IndexT idx = new IndexT
						{
							VertexIndex = face.vertex_indices[k].V_Index,
							NormalIndex = face.vertex_indices[k].VN_Index,
							TexcoordIndex = face.vertex_indices[k].VT_Index
						};
						shape.Mesh.Indices.Add(idx);
					}

					shape.Mesh.NumFaceVertices.Add((byte)npolys);
					shape.Mesh.MaterialIds.Add(material_id);  // per face
					shape.Mesh.SmoothingGroupIds.Add(face.smoothing_group_id);  // per face
				}
			}

			shape.Name = name;
			shape.Mesh.Tags = tags;

			return true;
		}

		public static void LoadMtl(Dictionary<string, int> material_map, List<Material> materials, TextReader inStream, out string warning)
		{
			warning = "";
			// Create a default material anyway.
			Material material = new Material();
			InitMaterial(ref material);

			// Issue 43. `d` wins against `Tr` since `Tr` is not in the MTL specification.
			bool has_d = false;
			bool has_tr = false;

			while (inStream.Peek() != -1)
			{
				string lineData = inStream.ReadLine();
				if (string.IsNullOrWhiteSpace(lineData))
				{
					continue;
				}

				LineReader linebuf = new LineReader(lineData);

				string token = linebuf.ReadWord();
				if (string.IsNullOrWhiteSpace(token))
				{
					continue;
				}

				if (token.StartsWith("#"))
				{
					continue;  // comment line
				}

				// new mtl
				if (token == "newmtl")
				{
					// flush previous material.
					if (!string.IsNullOrWhiteSpace(material.Name))
					{
						material_map.Add(material.Name, materials.Count);
						materials.Add(material);
					}

					// initial temporary material
					InitMaterial(ref material);

					has_d = false;
					has_tr = false;

					material.Name = linebuf.ReadWord();
					continue;
				}

				// ambient
				if (token == "Ka")
				{
					parseReal3(out float r, out float g, out float b, linebuf);
					material.Ambient.X = r;
					material.Ambient.Y = g;
					material.Ambient.Z = b;
					continue;
				}

				// diffuse
				if (token == "Kd")
				{
					parseReal3(out float r, out float g, out float b, linebuf);
					material.Diffuse.X = r;
					material.Diffuse.Y = g;
					material.Diffuse.Z = b;
					continue;
				}

				// specular
				if (token == "Ks")
				{
					parseReal3(out float r, out float g, out float b, linebuf);
					material.Specular.X = r;
					material.Specular.Y = g;
					material.Specular.Z = b;
					continue;
				}

				// transmittance
				if (token == "Kt" || token == "Tf")
				{
					parseReal3(out float r, out float g, out float b, linebuf);
					material.Transmittance.X = r;
					material.Transmittance.Y = g;
					material.Transmittance.Z = b;
					continue;
				}

				// ior(index of refraction)
				if (token == "Ni")
				{
					material.Ior = parseReal(linebuf);
					continue;
				}

				// emission
				if (token == "Ke")
				{
					parseReal3(out float r, out float g, out float b, linebuf);
					material.Emission.X = r;
					material.Emission.Y = g;
					material.Emission.Z = b;
					continue;
				}

				// shininess
				if (token == "Ns")
				{
					material.Shininess = parseReal(linebuf);
					continue;
				}

				// illum model
				if (token == "illum")
				{
					material.Illum = parseInt(linebuf);
					continue;
				}

				// dissolve
				if (token == "d")
				{
					material.Dissolve = parseReal(linebuf);

					if (has_tr)
					{
						warning += "WARN: Both `d` and `Tr` parameters defined for \"" + material.Name + "\". Use the value of `d` for dissolve.";
					}
					has_d = true;
					continue;
				}

				if (token == "Tr")
				{
					if (has_d)
					{
						// `d` wins. Ignore `Tr` value.
						warning += "WARN: Both `d` and `Tr` parameters defined for \"" + material.Name + "\". Use the value of `d` for dissolve.";
					}
					else
					{
						// We invert value of Tr(assume Tr is in range [0, 1])
						// NOTE: Interpretation of Tr is application(exporter) dependent. For
						// some application(e.g. 3ds max obj exporter), Tr = d(Issue 43)
						material.Dissolve = 1.0f - parseReal(linebuf);
					}
					has_tr = true;
					continue;
				}

				// PBR: roughness
				if (token == "Pr")
				{
					material.Roughness = parseReal(linebuf);
					continue;
				}

				// PBR: metallic
				if (token == "Pm")
				{
					material.Metallic = parseReal(linebuf);
					continue;
				}

				// PBR: sheen
				if (token == "Ps")
				{
					material.Sheen = parseReal(linebuf);
					continue;
				}

				// PBR: clearcoat thickness
				if (token == "Pc")
				{
					material.ClearcoatThickness = parseReal(linebuf);
					continue;
				}

				// PBR: clearcoat roughness
				if (token == "Pcr")
				{
					material.ClearcoatRoughness = parseReal(linebuf);
					continue;
				}

				// PBR: anisotropy
				if (token == "aniso")
				{
					material.Anisotropy = parseReal(linebuf);
					continue;
				}

				// PBR: anisotropy rotation
				if (token == "anisor")
				{
					material.AnisotropyRotation = parseReal(linebuf);
					continue;
				}

				// ambient texture
				if (token == "map_Ka")
				{
					ParseTextureNameAndOption(
						out material.AmbientTexName,
						ref material.AmbientTexOpt,
						linebuf, /* is_bump */ false);
					continue;
				}

				// diffuse texture
				if (token == "map_Kd")
				{
					ParseTextureNameAndOption(out (material.DiffuseTexName),
											  ref (material.DiffuseTexOpt),
											  linebuf,
											  /* is_bump */ false);
					continue;
				}

				// specular texture
				if (token == "map_Ks")
				{
					ParseTextureNameAndOption(out (material.SpecularTexName),
											  ref (material.SpecularTexOpt),
											  linebuf,
											  /* is_bump */ false);
					continue;
				}

				// specular highlight texture
				if (token == "map_Ns")
				{
					ParseTextureNameAndOption(out (material.SpecularHighlightTexName),
											  ref (material.SpecularHighlightTexOpt),
											  linebuf,
											  /* is_bump */ false);
					continue;
				}

				// bump texture
				if (token == "map_bump" || token == "map_Bump" || token == "bump")
				{
					ParseTextureNameAndOption(out (material.BumpTexName),
											  ref (material.BumpTexOpt),
											  linebuf,
											  /* is_bump */ true);
					continue;
				}

				// alpha texture
				if (token == "map_d")
				{
					material.AlphaTexName = linebuf.ReadWord();
					ParseTextureNameAndOption(out (material.AlphaTexName),
											  ref (material.AlphaTexOpt),
											  linebuf,
											  /* is_bump */ false);
					continue;
				}

				// displacement texture
				if (token == "disp")
				{
					ParseTextureNameAndOption(out (material.DisplacementTexName),
											  ref (material.DisplacementTexOpt),
											  linebuf,
											  /* is_bump */ false);
					continue;
				}

				// reflection map
				if (token == "refl")
				{
					ParseTextureNameAndOption(out (material.ReflectionTexName),
											  ref (material.ReflectionTexOpt),
											  linebuf,
											  /* is_bump */ false);
					continue;
				}

				// PBR: roughness texture
				if (token == "map_Pr")
				{
					ParseTextureNameAndOption(out (material.RoughnessTexName),
											  ref (material.RoughnessTexOpt),
											  linebuf,
											  /* is_bump */ false);
					continue;
				}

				// PBR: metallic texture
				if (token == "map_Pm")
				{
					ParseTextureNameAndOption(out (material.MetallicTexName),
											  ref (material.MetallicTexOpt),
											  linebuf,
											  /* is_bump */ false);
					continue;
				}

				// PBR: sheen texture
				if (token == "map_Ps")
				{
					ParseTextureNameAndOption(out (material.SheenTexName),
											  ref (material.SheenTexOpt),
											  linebuf,
											  /* is_bump */ false);
					continue;
				}

				// PBR: emissive texture
				if (token == "map_Ke")
				{
					ParseTextureNameAndOption(out (material.EmissiveTexName),
											  ref (material.EmissiveTexOpt),
											  linebuf,
											  /* is_bump */ false);
					continue;
				}

				// PBR: normal map texture
				if (token == "norm")
				{
					ParseTextureNameAndOption(
						out (material.NormalTexName), ref (material.NormalTexOpt), linebuf,
						/* is_bump */ false);  // @fixme { is_bump will be true? }
					continue;
				}

				material.UnknownParameter.Add(token, linebuf.ReadLine());
			}

			// flush last material.
			material_map.Add(material.Name, materials.Count);
			materials.Add(material);
		}

		public static LoaderData LoadObj(string filename, string mtl_basedir = null, bool triangulate = true)
		{

			using StreamReader ifs = new StreamReader(filename);

			MaterialFileReader matFileReader = new MaterialFileReader(mtl_basedir);

			return LoadObj(ifs, matFileReader, triangulate);

		}

		private static LoaderData LoadObj(TextReader inStream, MaterialReader readMatFn, bool triangulate)
		{
			string err = string.Empty;
			Attrib attrib = new Attrib();
			List<Material> materials = new List<Material>();
			List<float> v = new List<float>();
			List<float> vn = new List<float>();
			List<float> vt = new List<float>();
			List<float> vc = new List<float>();
			List<Tag> tags = new List<Tag>();
			List<face_t> faceGroup = new List<face_t>();
			string name = null;
			List<Shape> shapes = new List<Shape>();
			// material
			Dictionary<string, int> material_map = new Dictionary<string, int>();
			int material = -1;

			// smoothing group id
			uint current_smoothing_id = 0; // Initial value. 0 means no smoothing.

			Shape shape = new Shape();

			err = "";

			while (inStream.Peek() != -1)
			{
				string linebuf = inStream.ReadLine();

				if (string.IsNullOrWhiteSpace(linebuf))
				{
					continue;
				}

				LineReader lineReader = new LineReader(linebuf);
				string token = lineReader.ReadWord();

				if (token.StartsWith("#") || string.IsNullOrWhiteSpace(token))
				{
					continue;
				}

				// vertex
				if (token == "v")
				{
					parseVertexWithColor(out float x, out float y, out float z, out float r, out float g, out float b, lineReader);
					v.Add(x);
					v.Add(y);
					v.Add(z);
					vc.Add(r);
					vc.Add(g);
					vc.Add(b);
					continue;
				}

				// normal
				if (token == "vn")
				{
					parseReal3(out float x, out float y, out float z, lineReader);
					vn.Add(x);
					vn.Add(y);
					vn.Add(z);
					continue;
				}

				// texcoord
				if (token == "vt")
				{
					parseReal2(out float x, out float y, lineReader);
					vt.Add(x);
					vt.Add(y);
					continue;
				}

				// face
				if (token == "f")
				{
					face_t face = new face_t();
					face.smoothing_group_id = current_smoothing_id;

					while (!lineReader.Eof)
					{
						VertexIndex vi = new VertexIndex();
						if (!ParseTriple(lineReader, v.Count / 3, vn.Count / 3, vt.Count / 2, ref vi))
						{
							err = "Failed parse 'f' line(e.g zero value for face index).";
							return new LoaderData()
							{
								Shapes = shapes,
							};
						}

						face.vertex_indices.Add(vi);
					}

					faceGroup.Add(face);

					continue;
				}

				// use mtl
				if (token == "usemtl")
				{
					int newMaterialId = -1;
					token = lineReader.ReadWord();

					if (material_map.ContainsKey(token))
					{
						newMaterialId = material_map[token];
					}
					else
					{
						// { error!! material not found }
					}

					if (newMaterialId != material)
					{
						// Create per-face material. Thus we don't add `shape` to `shapes` at
						// this time.
						// just clear `faceGroup` after `exportFaceGroupToShape()` call.
						exportFaceGroupToShape(ref shape, faceGroup, tags, material, name, triangulate, v);
						faceGroup.Clear();
						material = newMaterialId;
					}
				}

				// load mtl
				if (token == "mtllib")
				{
					if (readMatFn != null)
					{
						string[] filenames = lineReader.ReadSplit(' ', '\t');

						if (filenames.Length == 0)
						{
							err += "WARN: Looks like empty filename for mtllib. Use default material. ";
						}
						else
						{
							bool found = false;
							for (int s = 0; s < filenames.Length; s++)
							{
								bool ok = readMatFn.Load(filenames[s].Trim(), materials, material_map, out string err_mtl);
								if (!string.IsNullOrWhiteSpace(err_mtl))
								{
									err += err_mtl;
								}

								if (ok)
								{
									found = true;
									break;
								}
							}

							if (!found)
							{
								err += "WARN: Failed to load material file(s). Use default material. ";
							}
						}
					}
					continue;
				}

				if (token == "g")
				{
					// flush previous face group;
					exportFaceGroupToShape(ref shape, faceGroup, tags, material, name, triangulate, v);

					if (shape.Mesh.Indices.Count > 0)
					{
						shapes.Add(shape);
					}

					// material = -1;
					faceGroup.Clear();
					shape = new Shape();

					name = lineReader.ReadWord() ?? "";

					continue;
				}

				// object name
				if (token == "o")
				{
					// flush previous face group;
					bool ret1 = exportFaceGroupToShape(ref shape, faceGroup, tags, material, name, triangulate, v);

					if (ret1)
					{
						shapes.Add(shape);
					}

					// material = -1;
					faceGroup.Clear();
					shape = new Shape();

					/// TODO: { multiple object name? }
					name = lineReader.ReadWord() ?? "";

					continue;
				}

				if (token == "t")
				{
					Tag tag = new Tag
					{
						Name = parseString(lineReader)
					};

					tag_sizes ts = parseTagTriple(lineReader);

					for (int i = 0; i < ts.num_ints; i++)
					{
						tag.IntValues.Add(parseInt(lineReader));
					}

					for (int i = 0; i < ts.num_reals; i++)
					{
						tag.FloatValues.Add(parseReal(lineReader));
					}

					for (int i = 0; i < ts.num_strings; i++)
					{
						tag.StringValues.Add(parseString(lineReader));
					}

					tags.Add(tag);

					continue;
				}

				if (token == "s")
				{
					// smoothing group id
					token = lineReader.ReadWord();

					if (string.IsNullOrWhiteSpace(token))
					{
						continue;
					}

					if (token.Length >= 3)
					{
						if (token.StartsWith("off"))
						{
							current_smoothing_id = 0;
						}
					}
					else
					{
						// assume number
						int smGroupId = parseInt(new LineReader(token));
						if (smGroupId < 0)
						{
							// parse error. force set to 0.
							// FIXME(syoyo): Report warning.
							current_smoothing_id = 0;
						}
						else
						{
							current_smoothing_id = (uint)smGroupId;
						}
					}

					continue;
				} // smoothing group id

				// Ignore unknown command.
			}

			bool ret = exportFaceGroupToShape(ref shape, faceGroup, tags, material, name, triangulate, v);
			// exportFaceGroupToShape return false when `usemtl` is called in the last
			// line.
			// we also add `shape` to `shapes` when `shape.mesh` has already some
			// faces(indices)
			if (ret || shape.Mesh.Indices.Count > 0)
			{
				shapes.Add(shape);
			}
			faceGroup.Clear();  // for safety

			attrib.Vertices = v;
			attrib.Normals = vn;
			attrib.Texcoords = vt;
			attrib.Colors = vc;

			return new LoaderData()
			{
				Shapes = shapes,
				Attrib = attrib,
				Materials = materials,
			};
		}

		private static int atoi(string input, int default_value = 0)
		{
			if (input != null && int.TryParse(input, out int ret))
			{
				return ret;
			}

			return default_value;
		}
	}
}
