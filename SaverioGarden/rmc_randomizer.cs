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
	public static SolitaireGameState RMC_getRandomizedSolitaireBoard()
	{
		// based off the corresponding method in RMC
		// although we allow for tweaking the generation, e.g. removing atom types, etc
		// and some methods are factored out

		// TODO: actually do that^ stuff


		// first, pick a bitboard and generate a board template
		HexIndex center = new HexIndex(5, 0);
		List<HexIndex> marbleHexes = getBitBoard();

		// "solve" the board template by generating a move history
		// for convenience, we will assume only one Gold marble exists, and that it is always the center of the board
		// additionally, we assume there are no Quintessence marbles, so every match is always a *pair* of marbles
		List<Tuple<HexIndex,HexIndex>> moveHistory = new();
		while (marbleHexes.Count > 0)
		{
			// find all marbles that could be chosen for the next move
			List <HexIndex> choosableMarbles = new();
			foreach (var hex in marbleHexes.Where(x => HexIsChoosable(x, marbleHexes) && (x != center)))
			{
				choosableMarbles.Add(hex);
			}
			// choose the next move
			if (choosableMarbles.Count >= 2)
			{
				// choose a random pair of marbles to be the next move
				HexIndex marbleA, marbleB;
				marbleA = choosableMarbles[RandomInt(choosableMarbles.Count)];
				choosableMarbles.Remove(marbleA); // don't accidentally choose A again when choosing B!
				marbleB = choosableMarbles[RandomInt(choosableMarbles.Count)];
				moveHistory.Add(Tuple.Create(marbleA, marbleB));
				marbleHexes.Remove(marbleA);
				marbleHexes.Remove(marbleB);
			}
			else if (HexIsChoosable(center, marbleHexes))
			{
				// only option is to choose Gold as our next move
				moveHistory.Add(Tuple.Create(center, center));
				marbleHexes.Remove(center);
			}
			else
			{
				Logger.Log("[SaverioGarden] Encountered a board-template state where no move is possible:");
				foreach (var hex in marbleHexes)
				{
					Logger.Log("    " + hex.Q + "," + hex.R);
				}
				throw new Exception("RMC_getRandomizedSolitaireBoard: Impossible unsolvable state reached.");
			}
		}

		// reverse the list, so moveHistory[0] is the LAST move made to solve the board
		moveHistory.Reverse();
		
		// generate "marble bags" that store the moves to be made
		List<Tuple<AtomType, AtomType>> saltlikeBag = new();
		List<Tuple<AtomType, AtomType>> metalBag = new();
		// there are 55 marbles on the board
		// but one of them is gold, so we have 54 more to add
		int marblesToAdd = 54;

		// put animismus matches in the saltlikeBag
		if (GenerationSettings.includeAnimismus)
		{
			for (int i = 0; i < 2; i++)
			{
				saltlikeBag.Add(Tuple.Create(getAtomType(8), getAtomType(9)));
				marblesToAdd -= 2;
			}
		}

		int[] cardinals = new int[5] { 0, 0, 0, 0, 0 };
		if (GenerationSettings.includeSalt) cardinals[0] = 4;
		if (GenerationSettings.includeAir) cardinals[1] = 6;
		if (GenerationSettings.includeWater) cardinals[2] = 6;
		if (GenerationSettings.includeFire) cardinals[3] = 6;
		if (GenerationSettings.includeEarth) cardinals[4] = 6;
		for (int i = 0; i < cardinals.Length; i++)
		{
			marblesToAdd -= cardinals[i];
		}

		// put salt matches in the saltlikeBag
		while (cardinals[0] > 0)
		{
			cardinals[0] -= 2;
			int match = RandomInt(5);
			if (match == 0)
			{
				saltlikeBag.Add(Tuple.Create(getAtomType(10), getAtomType(10)));
			}
			else
			{	
				cardinals[match] -= 2;
				saltlikeBag.Add(Tuple.Create(getAtomType(10), getAtomType(10 + match)));
				saltlikeBag.Add(Tuple.Create(getAtomType(10), getAtomType(10 + match)));
			}
		}

		// put the remaining cardinal matches in the saltlikeBag
		for (int i = 1; i < 5; i++)
		{
			while (cardinals[i] > 0)
			{
				cardinals[i] -= 2;
				saltlikeBag.Add(Tuple.Create(getAtomType(10 + i), getAtomType(10 + i)));
			}
		}

		// decide how many of each metal we'll have on the board
		// we are biased to adding more iron and copper
		int[] metals = new int[6] { 0, 0, 0, 0, 0, 0 }; // we don't use metals[0]
		int[] metalTable = new int[] { 1, 2, 3, 3, 4, 4, 5};

		// guarantee at least one metal match of each tier from lead to silver
		for (int i = 5; i > 0; i--)
		{
			marblesToAdd -= 2;
			metals[i] += 1;
		}

		// now assign the remaining marbles
		while (marblesToAdd > 0)
		{
			marblesToAdd -= 2;
			var pick = metalTable[RandomInt(metalTable.Length)];
			metals[pick] += 1;
		}

		// add the non-Gold metals into the metalBag, from Silver to Lead
		// we need to insert them in order, since we must solve them in order!
		for (int i = 5; i > 0; i--)
		{
			// generate temporary bag of marbles containing a specific tier of metal
			List<Tuple<AtomType, AtomType>> tempBag = new() {};
			while (metals[i] > 0)
			{
				metals[i]--;
				// add a projection match, or a purification match?
				if (RandomInt(2) == 0 || metals[i] == 0)
				{
					//we guarantee at least 1 projection match
					tempBag.Add(Tuple.Create(getAtomType(i), getAtomType(7)));
				}
				else
				{
					tempBag.Add(Tuple.Create(getAtomType(i), getAtomType(i)));
				}
			}

			// randomly pour the tempBag into the metalBag
			while (tempBag.Count > 0)
			{
				var pick = tempBag[RandomInt(tempBag.Count)];
				metalBag.Add(pick);
				tempBag.Remove(pick);
			}
		}

		// "unsolve" the board by using the move history in reverse to place marbles
		SolitaireGameState solitaireGameState = new SolitaireGameState();

		bool placedGold = false;
		for (int m = 0; m < moveHistory.Count; m++)
		{
			var hexes = moveHistory[m];

			if (hexes.Item1 == hexes.Item2)
			{
				// the Gold match!
				solitaireGameState.field_3864.Add(hexes.Item1, getAtomType(6));
				placedGold = true;
				continue;
			}
			// otherwise, a regular match
			int pick = RandomInt(saltlikeBag.Count + metalBag.Count);
			if (!placedGold && saltlikeBag.Count > 0)
			{
				pick = RandomInt(saltlikeBag.Count);
			}

			Tuple<AtomType, AtomType> match;
			if (pick < saltlikeBag.Count)
			{
				match = saltlikeBag[pick];
				saltlikeBag.Remove(match);
			}
			else
			{
				match = metalBag[0];
				metalBag.Remove(match);
			}
			solitaireGameState.field_3864.Add(hexes.Item1, match.Item1);
			solitaireGameState.field_3864.Add(hexes.Item2, match.Item2);
		}

		// tada! randomized board
		return solitaireGameState;
	}
}