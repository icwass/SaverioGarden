using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Quintessential;
using Quintessential.Settings;
using SDL2;
using System;
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
public class MainClass : QuintessentialMod
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

		foreach (Campaign campaign in QuintessentialLoader.AllCampaigns)
		{
			if (campaign.QuintTitle == "Reductive Metallurgy")
			{
				rmc_campaign = campaign;
				break;
			}
		}
	}

	static Campaign rmc_campaign;
	static bool currentCampaignIsRMC() => rmc_campaign == Campaigns.field_2330;
	static bool isQuintessenceSigmarGarden(SolitaireScreen screen) => new DynamicData(screen).Get<bool>("field_3874");
	static bool currentCampaignIsRMC(SolitaireScreen screen) => currentCampaignIsRMC() && !isQuintessenceSigmarGarden(screen);

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
		//drawHeader("Include the following atom types:");
		//checkToggle(ref includeAnimismus, "Vitae/Mors");
		//checkToggle(ref includeAir, "Air");
		//checkToggle(ref includeWater, "Water");
		//checkToggle(ref includeFire, "Fire");
		//checkToggle(ref includeEarth, "Earth");
		//checkToggle(ref includeSalt, "Salt");


	}

	static bool includeAnimismus = true;
	static bool includeAir = true;
	static bool includeWater = true;
	static bool includeFire = true;
	static bool includeEarth = true;
	static bool includeSalt = true;

	public static SolitaireGameState getSolitaireBoard(On.class_198.orig_method_537 orig, bool quintessenceSigmar)
	{

		if (!currentCampaignIsRMC() || quintessenceSigmar || !showingCustomOptions) return orig(quintessenceSigmar);




		// otherwise, default to the regular RMC variant
		return orig(quintessenceSigmar);
	}
}
