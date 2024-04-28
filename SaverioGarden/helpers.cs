using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Quintessential;
using Quintessential.Settings;
using SDL2;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
//using System.Reflection;

namespace SaverioGarden;

//using PartType = class_139;
//using Permissions = enum_149;
//using BondType = enum_126;
//using BondSite = class_222;
//using AtomTypes = class_175;
//using PartTypes = class_191;
//using Texture = class_256;
//using Song = class_186;
//using Tip = class_215;
//using Font = class_1;

public partial class MainClass
{
	public static class GenerationSettings
	{
		public static bool includeAnimismus = true;
		public static bool includeAir = true;
		public static bool includeWater = true;
		public static bool includeFire = true;
		public static bool includeEarth = true;
		public static bool includeSalt = true;
	}
	static int RandomInt(int max) => class_269.field_2103.method_299(0, max);
	public static AtomType getAtomType(int i)
	{
		return new AtomType[17]
		{
			nullAtom, // 00 - filler
			class_175.field_1681, // 01 - lead
			class_175.field_1683, // 02 - tin
			class_175.field_1684, // 03 - iron
			class_175.field_1682, // 04 - copper
			class_175.field_1685, // 05 - silver
			class_175.field_1686, // 06 - gold
			class_175.field_1680, // 07 - quicksilver
			class_175.field_1687, // 08 - vitae
			class_175.field_1688, // 09 - mors
			class_175.field_1675, // 10 - salt
			class_175.field_1676, // 11 - air
			class_175.field_1679, // 12 - water
			class_175.field_1678, // 13 - fire
			class_175.field_1677, // 14 - earth
			class_175.field_1689, // 15 - repeat
			class_175.field_1690, // 16 - quintessence
		}[i];
	}
	static bool HexIsChoosable(HexIndex hex_self, List<HexIndex> marbleHexes)
	{
		bool h1, h2, h3, h4, h5, h6;
		h1 = !marbleHexes.Contains(hex_self + new HexIndex(1, 0));
		h2 = !marbleHexes.Contains(hex_self + new HexIndex(0, 1));
		h3 = !marbleHexes.Contains(hex_self + new HexIndex(-1, 1));
		h4 = !marbleHexes.Contains(hex_self + new HexIndex(-1, 0));
		h5 = !marbleHexes.Contains(hex_self + new HexIndex(0, -1));
		h6 = !marbleHexes.Contains(hex_self + new HexIndex(1, -1));
		// return true if three contiguous hexes are empty
		return (h2 && h3 && (h1 || h4)) || (h4 && h5 && (h3 || h6)) || (h6 && h1 && (h5 || h2));
	}

	static List<HexIndex> getBitBoard(int rotation = -1, int mirror = -1, int boardIndex = -1)
	{
		List<HexIndex> marbleHexes = new();

		// try to find solitaire-bitboards.dat
		string subpath = "/Content/";
		string file = "solitaire-bitboards.dat";
		checkIfFileExists(subpath, file, "getBitBoard: Solitaire data is missing.");

		HexIndex center = new HexIndex(5, 0);
		using (BinaryReader binaryReader = new BinaryReader(new FileStream(RMC_FilePath + subpath + file, FileMode.Open, FileAccess.Read)))
		{
			const int bytesPerBitboard = 16;
			int bitboardCount = binaryReader.ReadInt32();

			if (bitboardCount <= 0)
			{
				throw new Exception("getBitBoard: Solitaire data contains no bitboards!");
			}

			if (rotation < 0) rotation = RandomInt(6);
			HexRotation hexRotation = new HexRotation(rotation % 6);

			if (mirror < 0) mirror = RandomInt(2);
			bool mirrorBoard = mirror % 2 == 0;

			if (boardIndex < 0) boardIndex = RandomInt(bitboardCount);
			int boardID = boardIndex % bitboardCount;

			binaryReader.BaseStream.Seek(boardID * bytesPerBitboard, SeekOrigin.Current);
			for (int i = 0; i < 16; i++)
			{
				int boardbyte = binaryReader.ReadByte();
				for (int j = 0; j < 8; j++)
				{
					if (boardbyte % 2 == 1)
					{
						// add hex
						int num = i * 8 + j;
						int q = num / 11;
						int r = (num % 11) - 5;
						if (mirrorBoard)
						{
							q += r;
							r = -r;
						}
						marbleHexes.Add(new HexIndex(q, r).RotatedAround(center, hexRotation));
					}
					boardbyte = boardbyte >> 1;
				}
			}
		}
		return marbleHexes;
	}
}