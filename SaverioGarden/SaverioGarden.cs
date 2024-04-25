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

//using PartType = class_139;
//using Permissions = enum_149;
//using BondType = enum_126;
//using BondSite = class_222;
//using Bond = class_277;
//using AtomTypes = class_175;
//using PartTypes = class_191;
//using Texture = class_256;
public class MainClass : QuintessentialMod
{
	public static MethodInfo PrivateMethod<T>(string method) => typeof(T).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
	public override Type SettingsType => typeof(MySettings);
	public static QuintessentialMod MainClassAsMod;
	public static bool showingCustomOptions = false;

	public static bool PressedShowCustomOptions() => MySettings.Instance.showCustomOptionsKeybind.Pressed();
	public class MySettings
	{
		public static MySettings Instance => MainClassAsMod.Settings as MySettings;

		//[SettingsLabel("Boolean Setting")]
		//public bool booleanSetting = true;

		[SettingsLabel("Show the customization options during RMC's solitaire.")]
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
		if (PressedShowCustomOptions())
		{
			showingCustomOptions = !showingCustomOptions;
			Sound toggleSound = showingCustomOptions ? class_238.field_1991.field_1872 : class_238.field_1991.field_1873; // ui_modal / ui_modal_close
			toggleSound.method_28(1f);
		}

		orig(screen_self, timeDelta);

		if (!currentCampaignIsRMC(screen_self) || !showingCustomOptions) return;
		if (GameLogic.field_2434.method_938() is class_16) return;

		//Vector2 vector2_1 = new Vector2(1516f, 922f);
		//Vector2 vector2_2 = (class_115.field_1433 / 2 - vector2_1 / 2 + new Vector2(-2f, -11f)).Rounded();
		//Vector2 vector2_3 = vector2_2 + new Vector2(980f, 127f);




	}

	public static SolitaireGameState getSolitaireBoard(On.class_198.orig_method_537 orig, bool quintessenceSigmar)
	{

		if (!currentCampaignIsRMC() || quintessenceSigmar || !showingCustomOptions) return orig(quintessenceSigmar);




		// otherwise, default to the regular RMC variant
		return orig(quintessenceSigmar);
	}
}
