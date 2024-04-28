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
using System.Reflection;

namespace SaverioGarden;

using Font = class_1;
//using PartType = class_139;
//using Permissions = enum_149;
//using BondType = enum_126;
//using BondSite = class_222;
//using Bond = class_277;
//using AtomTypes = class_175;
//using PartTypes = class_191;
using Texture = class_256;
public partial class MainClass : QuintessentialMod
{
	public static MethodInfo PrivateMethod<T>(string method) => typeof(T).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
	public override Type SettingsType => typeof(MySettings);
	public static QuintessentialMod MainClassAsMod;
	public static bool showingCustomOptions = true;

	public static Keybinding showCustomOptions => ((MySettings)MainClassAsMod.Settings).showCustomOptionsKeybind;

	public class MySettings
	{
		//[SettingsLabel("Boolean Setting")]
		//public bool booleanSetting = true;

		[SettingsLabel("Show/Hide the customization options during RMC's solitaire.")]
		public Keybinding showCustomOptionsKeybind = new() { Key = "R" };
	}
	public override void ApplySettings()
	{
		base.ApplySettings();
		var SET = (MySettings)Settings;
		//var booleanSetting = SET.booleanSetting;
	}
	public override void Load()
	{
		MainClassAsMod = this;
		Settings = new MySettings();
	}
	public override void LoadPuzzleContent()
	{
		//
	}
	public override void Unload()
	{
		//
	}
	public override void PostLoad()
	{
		On.SolitaireScreen.method_50 += SolitaireScreen_Method_50;
		On.class_198.method_537 += getSolitaireBoard;

		nullAtom = new AtomType()
		{
			field_2283 = (byte)0,
			field_2284 = (string)class_134.method_254("Null"),
			field_2285 = class_134.method_253("Elemental Null", string.Empty),
			field_2286 = class_134.method_253("Null", string.Empty),
			field_2287 = class_238.field_1989.field_81.field_598,
			field_2288 = class_238.field_1989.field_81.field_599,
			field_2290 = new class_106()
			{
				field_994 = class_238.field_1989.field_81.field_596,
				field_995 = class_238.field_1989.field_81.field_597
			}
		};

		// fetch RMC's filepath and campaign
		bool foundTheThing = false;
		string name = "ReductiveMetallurgyCampaign";
		foreach (ModMeta mod in QuintessentialLoader.Mods)
		{
			if (mod.Name == name)
			{
				RMC_FilePath = mod.PathDirectory;
				foundTheThing = true;
				break;
			}
		}
		if (!foundTheThing)
		{
			Logger.Log("[SaverioGarden] Could not find ReductiveMetallurgyCampaign... what? But it's a dependency though...");
			throw new Exception("Load: Failed to find the expected mod.");
		}
		foundTheThing = false;
		foreach (Campaign campaign in QuintessentialLoader.AllCampaigns)
		{
			if (campaign.QuintTitle == "Reductive Metallurgy")
			{
				rmc_campaign = campaign;
				foundTheThing = true;
				break;
			}
		}
		if (!foundTheThing)
		{
			Logger.Log("[SaverioGarden] Could not find the RMC campaign... what? How?");
			throw new Exception("Load: Failed to find the expected campaign.");
		}
	}

	public static AtomType nullAtom;
	static Campaign rmc_campaign;
	public static string RMC_FilePath = "";
	static bool currentCampaignIsRMC() => rmc_campaign == Campaigns.field_2330;
	static bool isQuintessenceSigmarGarden(SolitaireScreen screen) => new DynamicData(screen).Get<bool>("field_3874");
	static bool currentCampaignIsRMC(SolitaireScreen screen) => currentCampaignIsRMC() && !isQuintessenceSigmarGarden(screen);
	public static void checkIfFileExists(string subpath, string file, string error)
	{
		if (!File.Exists(RMC_FilePath + subpath + file))
		{
			Logger.Log("[SaverioGarden] Could not find '" + file + "' in the folder '" + RMC_FilePath + subpath + "'");
			throw new Exception(error);
		}
	}

	public static void SolitaireScreen_Method_50(On.SolitaireScreen.orig_method_50 orig, SolitaireScreen screen_self, float timeDelta)
	{
		if (currentCampaignIsRMC(screen_self) && showCustomOptions.Pressed())
		{
			showingCustomOptions = !showingCustomOptions;
			Sound toggleSound = showingCustomOptions ? class_238.field_1991.field_1872 : class_238.field_1991.field_1873; // ui_modal / ui_modal_close
			toggleSound.method_28(1f);
		}

		orig(screen_self, timeDelta);

		if (!currentCampaignIsRMC(screen_self) || !showingCustomOptions) return;
		if (GameLogic.field_2434.method_938() is class_16) return;

		/////////////////////////////////////////////
		// time to draw the customization options
		Vector2 panelDimensions = new Vector2(1516f, 922f);
		Vector2 panelOrigin = (class_115.field_1433 / 2 - panelDimensions / 2 + new Vector2(-2f, -11f)).Rounded();
		Color textColor = class_181.field_1718;

		// draw paper image over the usual story panel
		Texture paper = class_238.field_1989.field_100.field_131;
		class_135.method_263(paper, Color.Gray(32), panelOrigin + new Vector2(88f, 93f), new Vector2(494f, paper.method_689()));

		// draw title and information
		Vector2 position = panelOrigin + new Vector2(340f, 820f);
		UI.DrawText("Saverio's Garden", position, UI.Title, textColor, TextAlignment.Centred, 463f);
		

		// define helpers
		position = panelOrigin + new Vector2(98f, 780f);
		void drawHeader(string header)
		{
			UI.DrawText(header, position + new Vector2(5f, 9f), UI.SubTitle, textColor, TextAlignment.Left, 463f);
			position -= new Vector2(0, 38f);
		}
		void checkToggle(ref bool toggle, string name)
		{
			if (UI.DrawCheckbox(position, name, toggle)) toggle = !toggle;
			position -= new Vector2(0, 38f);
		}

		// "draw the rest of the owl"
		string info = "Customize the board generation with the settings below. Settings are applied to the next board generated, even if this menu is not visible.";
		info += "\n (Press '" + showCustomOptions.ToString() + "' to toggle menu visibility.)";

		drawHeader(info);
		drawHeader("");
		drawHeader("Include the following atom types:");
		checkToggle(ref GenerationSettings.includeAnimismus, "Vitae/Mors");
		checkToggle(ref GenerationSettings.includeAir, "Air");
		checkToggle(ref GenerationSettings.includeWater, "Water");
		checkToggle(ref GenerationSettings.includeFire, "Fire");
		checkToggle(ref GenerationSettings.includeEarth, "Earth");
		checkToggle(ref GenerationSettings.includeSalt, "Salt");
	}

	public static SolitaireGameState getSolitaireBoard(On.class_198.orig_method_537 orig, bool quintessenceSigmar)
	{
		if (!currentCampaignIsRMC() || quintessenceSigmar) return orig(quintessenceSigmar);

		//otherwise, generate a board
		return RMC_getRandomizedSolitaireBoard();
	}
}
