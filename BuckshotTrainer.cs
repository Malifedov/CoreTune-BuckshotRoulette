using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace BuckshotTrainer
{
    /// <summary>
    /// Loader entrypoint called from injected hooks in Assembly-CSharp.dll
    /// </summary>
    public static class Loader
    {
        private static GameObject _hostObject;
        private static bool _initialized = false;

        public static void Init()
        {
            if (_initialized && _hostObject != null)
                return;

            try
            {
                _hostObject = GameObject.Find("Buckshot_Trainer_Host_v2");
                if (_hostObject == null)
                {
                    _hostObject = new GameObject("Buckshot_Trainer_Host_v2");
                    _hostObject.AddComponent<TrainerBehaviour>();
                    UnityEngine.Object.DontDestroyOnLoad(_hostObject);
                }
                _initialized = true;
                Debug.Log("[CoreTune] In-Game Trainer initialized!");
            }
            catch (Exception ex)
            {
                Debug.LogError("[CoreTune] Failed to initialize: " + ex);
            }
        }
    }

    /// <summary>
    /// Core Trainer Behaviour with In-Game IMGUI Menu, HUD, and Cheats
    /// </summary>
    public class TrainerBehaviour : MonoBehaviour
    {
        public const string ModVersion = "v2.5";
        public const string AuthorName = "malifedov";
        public const string BoostyUrl = "https://boosty.to/malifedov";
        public const string NexusModsUrl = "https://www.nexusmods.com/profile/Malifedov";

        public static TrainerBehaviour Instance { get; private set; }

        // --- In-Game Menu State ---
        private bool _isMenuOpen = false;
        private Rect _windowRect = new Rect(40, 40, 860, 580);
        private bool _isDragging = false;
        private Vector2 _dragOffset = Vector2.zero;
        private int _activeTab = 0;
        private Vector2 _tabScrollPos = Vector2.zero;

        // --- Cheats State ---
        public static bool GodMode = false;
        public static bool OneHitKill = false;
        public static bool AlwaysLive = false;
        public static bool AlwaysBlank = false;
        public static bool SmartRoulette = false;
        public static bool ForceDealerSelfHarm = false;
        public static bool PermanentDealerCuffs = false;
        public static bool AntiCuffs = false;
        public static bool AntiWireCut = false;
        public static bool SkipNextDealerTurn = false;
        public static bool DealerTurnSkipInProgress = false;
        public static bool InfiniteItems = false;
        public static bool LuckyMedicine = false;
        public static bool PermanentDoubleOrNothing = true;
        public static int BonusMoney = 0;
        public static float GameSpeed = 1.0f;
        public static bool FreezeTime = false;
        public static bool FastBoot = false;

        private static ItemType[] _playerSlotSnapshot = new ItemType[8];
        private static bool[] _slotRespawning = new bool[8];

        // --- Overlay & HUD State ---
        public static bool ShowOverlay = false;
        public static bool ShowFps = false;
        public static bool ShowShellInfo = false;
        public static bool ShowActiveCheats = false;
        public static int OverlayPosition = 0; // 0=Top-Left, 1=Top-Right, 2=Bottom-Left, 3=Bottom-Right
        public static int OverlayStyle = 0;    // 0=Tactical Glass, 1=Minimal Stealth, 2=Cyber Terminal

        // --- Configuration & Localization ---
        public static KeyCode ToggleKey = KeyCode.Insert;
        public static int Language = 0; // 0 = Russian, 1 = English
        private static bool _isBindingToggleKey = false;
        private static string _bindingActionId = null;

        // --- Configurable Hotkeys ---
        public static KeyCode KeyGodMode = KeyCode.F2;
        public static KeyCode KeyOneHitKill = KeyCode.None;
        public static KeyCode KeySmartRoulette = KeyCode.F3;
        public static KeyCode KeyAlwaysLive = KeyCode.F4;
        public static KeyCode KeyAlwaysBlank = KeyCode.F5;
        public static KeyCode KeyInfiniteItems = KeyCode.F6;
        public static KeyCode KeyForceDealerSelfHarm = KeyCode.F7;
        public static KeyCode KeyLuckyMedicine = KeyCode.Home;
        public static KeyCode KeyAntiWireCut = KeyCode.None;
        public static KeyCode KeyAntiCuffs = KeyCode.None;
        public static KeyCode KeyFreezeTime = KeyCode.None;
        public static KeyCode KeyFullHeal = KeyCode.F8;
        public static KeyCode KeySkipDealerTurn = KeyCode.F9;
        public static KeyCode KeyInvertShell = KeyCode.F10;
        public static KeyCode KeySawShotgun = KeyCode.F11;
        public static KeyCode KeyAddMoney = KeyCode.F12;

        public static KeyCode GetHotkeyById(string id)
        {
            switch (id)
            {
                case "GodMode": return KeyGodMode;
                case "OneHitKill": return KeyOneHitKill;
                case "SmartRoulette": return KeySmartRoulette;
                case "AlwaysLive": return KeyAlwaysLive;
                case "AlwaysBlank": return KeyAlwaysBlank;
                case "InfiniteItems": return KeyInfiniteItems;
                case "ForceDealerSelfHarm": return KeyForceDealerSelfHarm;
                case "LuckyMedicine": return KeyLuckyMedicine;
                case "AntiWireCut": return KeyAntiWireCut;
                case "AntiCuffs": return KeyAntiCuffs;
                case "FreezeTime": return KeyFreezeTime;
                case "FullHeal": return KeyFullHeal;
                case "SkipDealerTurn": return KeySkipDealerTurn;
                case "InvertShell": return KeyInvertShell;
                case "SawShotgun": return KeySawShotgun;
                case "AddMoney": return KeyAddMoney;
                default: return KeyCode.None;
            }
        }

        public static void SetHotkeyById(string id, KeyCode key)
        {
            switch (id)
            {
                case "GodMode": KeyGodMode = key; break;
                case "OneHitKill": KeyOneHitKill = key; break;
                case "SmartRoulette": KeySmartRoulette = key; break;
                case "AlwaysLive": KeyAlwaysLive = key; break;
                case "AlwaysBlank": KeyAlwaysBlank = key; break;
                case "InfiniteItems": KeyInfiniteItems = key; break;
                case "ForceDealerSelfHarm": KeyForceDealerSelfHarm = key; break;
                case "LuckyMedicine": KeyLuckyMedicine = key; break;
                case "AntiWireCut": KeyAntiWireCut = key; break;
                case "AntiCuffs": KeyAntiCuffs = key; break;
                case "FreezeTime": KeyFreezeTime = key; break;
                case "FullHeal": KeyFullHeal = key; break;
                case "SkipDealerTurn": KeySkipDealerTurn = key; break;
                case "InvertShell": KeyInvertShell = key; break;
                case "SawShotgun": KeySawShotgun = key; break;
                case "AddMoney": KeyAddMoney = key; break;
            }
        }

        public static void UnbindCheatIfKeyMatches(KeyCode key)
        {
            if (key == KeyCode.None) return;
            if (KeyGodMode == key) KeyGodMode = KeyCode.None;
            if (KeyOneHitKill == key) KeyOneHitKill = KeyCode.None;
            if (KeySmartRoulette == key) KeySmartRoulette = KeyCode.None;
            if (KeyAlwaysLive == key) KeyAlwaysLive = KeyCode.None;
            if (KeyAlwaysBlank == key) KeyAlwaysBlank = KeyCode.None;
            if (KeyInfiniteItems == key) KeyInfiniteItems = KeyCode.None;
            if (KeyForceDealerSelfHarm == key) KeyForceDealerSelfHarm = KeyCode.None;
            if (KeyLuckyMedicine == key) KeyLuckyMedicine = KeyCode.None;
            if (KeyAntiWireCut == key) KeyAntiWireCut = KeyCode.None;
            if (KeyAntiCuffs == key) KeyAntiCuffs = KeyCode.None;
            if (KeyFreezeTime == key) KeyFreezeTime = KeyCode.None;
            if (KeyFullHeal == key) KeyFullHeal = KeyCode.None;
            if (KeySkipDealerTurn == key) KeySkipDealerTurn = KeyCode.None;
            if (KeyInvertShell == key) KeyInvertShell = KeyCode.None;
            if (KeySawShotgun == key) KeySawShotgun = KeyCode.None;
            if (KeyAddMoney == key) KeyAddMoney = KeyCode.None;
        }

        public static void ResetHotkeysToDefault()
        {
            KeyGodMode = KeyCode.F2;
            KeyOneHitKill = KeyCode.None;
            KeySmartRoulette = KeyCode.F3;
            KeyAlwaysLive = KeyCode.F4;
            KeyAlwaysBlank = KeyCode.F5;
            KeyInfiniteItems = KeyCode.F6;
            KeyForceDealerSelfHarm = KeyCode.F7;
            KeyLuckyMedicine = (ToggleKey == KeyCode.Home) ? KeyCode.None : KeyCode.Home;
            KeyAntiWireCut = KeyCode.None;
            KeyAntiCuffs = KeyCode.None;
            KeyFreezeTime = KeyCode.None;
            KeyFullHeal = KeyCode.F8;
            KeySkipDealerTurn = KeyCode.F9;
            KeyInvertShell = KeyCode.F10;
            KeySawShotgun = KeyCode.F11;
            KeyAddMoney = KeyCode.F12;
        }

        private struct HotkeyDef
        {
            public string Id;
            public string Ru;
            public string En;
            public HotkeyDef(string id, string ru, string en)
            {
                Id = id;
                Ru = ru;
                En = en;
            }
        }

        private static readonly HotkeyDef[] HotkeyDefinitions = new HotkeyDef[]
        {
            new HotkeyDef("GodMode", "Режим бога", "God Mode"),
            new HotkeyDef("OneHitKill", "Убийство с 1 выстрела", "One-Hit Kill"),
            new HotkeyDef("SmartRoulette", "Умная рулетка", "Smart Roulette"),
            new HotkeyDef("AlwaysLive", "Всегда боевые", "Always Live"),
            new HotkeyDef("AlwaysBlank", "Всегда холостые", "Always Blank"),
            new HotkeyDef("InfiniteItems", "Бесконечные предметы", "Infinite Items"),
            new HotkeyDef("ForceDealerSelfHarm", "Самоубийство дилера", "Dealer Self-Harm"),
            new HotkeyDef("LuckyMedicine", "Удачливая просрочка", "Lucky Medicine"),
            new HotkeyDef("AntiWireCut", "Защита проводов", "Anti-Wire Cut"),
            new HotkeyDef("AntiCuffs", "Иммунитет к наручникам", "Anti-Cuffs"),
            new HotkeyDef("FreezeTime", "Остановка времени", "Freeze Time"),
            new HotkeyDef("FullHeal", "Полное лечение (6 HP)", "Full Heal (6 HP)"),
            new HotkeyDef("SkipDealerTurn", "Пропустить ход дилера", "Skip Dealer Turn"),
            new HotkeyDef("InvertShell", "Перевернуть патрон", "Invert Shell"),
            new HotkeyDef("SawShotgun", "Спилить ствол дробовика", "Saw Off Shotgun"),
            new HotkeyDef("AddMoney", "Добавить +$10,000", "Add +$10,000")
        };

        public static string L(string ru, string en)
        {
            return (Language == 1) ? en : ru;
        }

        private static string GetConfigPath()
        {
            try
            {
                string dir = Path.GetDirectoryName(typeof(TrainerBehaviour).Assembly.Location);
                if (!string.IsNullOrEmpty(dir))
                {
                    return Path.Combine(dir, "CoreTune_config.json");
                }
            }
            catch { }
            return "CoreTune_config.json";
        }

        public static void SaveConfig()
        {
            try
            {
                PlayerPrefs.SetString("CoreTune_ToggleKey", ToggleKey.ToString());
                PlayerPrefs.SetInt("CoreTune_Language", Language);
                PlayerPrefs.SetInt("CoreTune_ShowOverlay", ShowOverlay ? 1 : 0);
                PlayerPrefs.SetInt("CoreTune_ShowFps", ShowFps ? 1 : 0);
                PlayerPrefs.SetInt("CoreTune_OverlayPos", OverlayPosition);
                PlayerPrefs.SetInt("CoreTune_OverlayStyle", OverlayStyle);
                PlayerPrefs.SetInt("CoreTune_PermDoN", PermanentDoubleOrNothing ? 1 : 0);

                string[] bindIds = new string[] {
                    "GodMode", "OneHitKill", "SmartRoulette", "AlwaysLive", "AlwaysBlank",
                    "InfiniteItems", "ForceDealerSelfHarm", "LuckyMedicine", "AntiWireCut",
                    "AntiCuffs", "FreezeTime", "FullHeal", "SkipDealerTurn", "InvertShell",
                    "SawShotgun", "AddMoney"
                };
                foreach (var bid in bindIds)
                {
                    PlayerPrefs.SetString("CoreTune_Key_" + bid, GetHotkeyById(bid).ToString());
                }
                PlayerPrefs.Save();
            }
            catch { }

            try
            {
                string json = string.Format(
                    "{{\n  \"toggleKey\": \"{0}\",\n  \"language\": {1},\n  \"showOverlay\": {2},\n  \"showFps\": {3},\n  \"overlayPosition\": {4},\n  \"overlayStyle\": {5},\n  \"permanentDoubleOrNothing\": {6}\n}}",
                    ToggleKey,
                    Language,
                    ShowOverlay ? "true" : "false",
                    ShowFps ? "true" : "false",
                    OverlayPosition,
                    OverlayStyle,
                    PermanentDoubleOrNothing ? "true" : "false"
                );
                File.WriteAllText(GetConfigPath(), json);
            }
            catch { }
        }

        public static void LoadConfig()
        {
            try
            {
                if (PlayerPrefs.HasKey("CoreTune_ToggleKey"))
                {
                    string kStr = PlayerPrefs.GetString("CoreTune_ToggleKey");
                    if (!string.IsNullOrEmpty(kStr))
                    {
                        try { ToggleKey = (KeyCode)Enum.Parse(typeof(KeyCode), kStr, true); } catch { }
                    }
                }
                if (PlayerPrefs.HasKey("CoreTune_Language"))
                {
                    Language = PlayerPrefs.GetInt("CoreTune_Language", 0);
                }
                if (PlayerPrefs.HasKey("CoreTune_ShowOverlay"))
                {
                    ShowOverlay = PlayerPrefs.GetInt("CoreTune_ShowOverlay", 0) == 1;
                }
                if (PlayerPrefs.HasKey("CoreTune_ShowFps"))
                {
                    ShowFps = PlayerPrefs.GetInt("CoreTune_ShowFps", 0) == 1;
                }
                if (PlayerPrefs.HasKey("CoreTune_OverlayPos"))
                {
                    OverlayPosition = PlayerPrefs.GetInt("CoreTune_OverlayPos", 0);
                }
                if (PlayerPrefs.HasKey("CoreTune_OverlayStyle"))
                {
                    OverlayStyle = PlayerPrefs.GetInt("CoreTune_OverlayStyle", 0);
                }
                if (PlayerPrefs.HasKey("CoreTune_PermDoN"))
                {
                    PermanentDoubleOrNothing = PlayerPrefs.GetInt("CoreTune_PermDoN", 1) == 1;
                }

                string[] bindIds = new string[] {
                    "GodMode", "OneHitKill", "SmartRoulette", "AlwaysLive", "AlwaysBlank",
                    "InfiniteItems", "ForceDealerSelfHarm", "LuckyMedicine", "AntiWireCut",
                    "AntiCuffs", "FreezeTime", "FullHeal", "SkipDealerTurn", "InvertShell",
                    "SawShotgun", "AddMoney"
                };
                foreach (var bid in bindIds)
                {
                    string prefKey = "CoreTune_Key_" + bid;
                    if (PlayerPrefs.HasKey(prefKey))
                    {
                        string val = PlayerPrefs.GetString(prefKey);
                        if (!string.IsNullOrEmpty(val))
                        {
                            try { SetHotkeyById(bid, (KeyCode)Enum.Parse(typeof(KeyCode), val, true)); } catch { }
                        }
                    }
                }
            }
            catch { }

            try
            {
                string path = GetConfigPath();
                if (File.Exists(path))
                {
                    string text = File.ReadAllText(path);
                    if (!string.IsNullOrEmpty(text))
                    {
                        if (text.IndexOf("\"language\": 1", StringComparison.OrdinalIgnoreCase) >= 0) Language = 1;
                        else if (text.IndexOf("\"language\": 0", StringComparison.OrdinalIgnoreCase) >= 0) Language = 0;

                        if (text.IndexOf("\"permanentDoubleOrNothing\": false", StringComparison.OrdinalIgnoreCase) >= 0) PermanentDoubleOrNothing = false;
                        else if (text.IndexOf("\"permanentDoubleOrNothing\": true", StringComparison.OrdinalIgnoreCase) >= 0) PermanentDoubleOrNothing = true;

                        int idx = text.IndexOf("\"toggleKey\":", StringComparison.OrdinalIgnoreCase);
                        if (idx >= 0)
                        {
                            int q1 = text.IndexOf('"', idx + 12);
                            if (q1 >= 0)
                            {
                                int q2 = text.IndexOf('"', q1 + 1);
                                if (q2 > q1)
                                {
                                    string keyName = text.Substring(q1 + 1, q2 - q1 - 1);
                                    try { ToggleKey = (KeyCode)Enum.Parse(typeof(KeyCode), keyName, true); } catch { }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        // --- Custom Cursor ---
        private static Texture2D _customCursorTex;

        public static bool IsMenuOpen
        {
            get { return Instance != null && Instance._isMenuOpen; }
        }

        public static bool OnChangeMouseState(bool enable)
        {
            if (IsMenuOpen)
            {
                return true;
            }
            return enable;
        }

        // --- Toast System ---
        private static string _toastMessage = "CoreTune v2.5 ready!";
        private static float _toastTimer = 5.0f;
        private static bool _splashBypassed = false;
        private static float _splashTimer = 0f;

        // --- FPS Calculation ---
        private float _fpsAccumulator = 0f;
        private int _fpsFrames = 0;
        private float _fpsTimeLeft = 0.5f;
        private float _currentFps = 60f;

        // --- Cached Reflection ---
        private static FieldInfo _roundManagerScoreField;
        private static FieldInfo _roundManagerEndScoreField;
        private static FieldInfo _roundManagerInitialScoreField;
        private static FieldInfo _scorePresenterPrevScoreField;
        private static FieldInfo _scorePresenterStartScoreField;
        private static FieldInfo _scorePresenterScoreTextField;
        private static FieldInfo _roundManagerDealerControllerField;
        private static FieldInfo _roundManagerPlayerItemsField;
        private static FieldInfo _roundManagerDealerItemsField;
        private static FieldInfo _roundManagerIsEndlessField;
        private static FieldInfo _roundManagerCurrentRoundField;
        private static MethodInfo _roundManagerBeginPlayerTurnMethod;
        private static PropertyInfo _roundDataRoundManagerProp;
        private static FieldInfo _dealerControllerTargetField;
        private static FieldInfo _dealerControllerKnownShellField;
        private static FieldInfo _dealerControllerKnowShellsField;

        // --- GUI Assets & Styles ---
        private static Texture2D _whiteTex;
        private static Texture2D _darkBgTex;
        private static Texture2D _cardBgTex;
        private static Texture2D _headerBgTex;
        private static Texture2D _btnNormalTex;
        private static Texture2D _btnHoverTex;
        private static Texture2D _btnActiveTex;
        private static Texture2D _btnDangerTex;
        private static Texture2D _tabActiveTex;
        private static Texture2D _liveShellTex;
        private static Texture2D _blankShellTex;

        private GUIStyle _windowStyle;
        private GUIStyle _headerTitleStyle;
        private GUIStyle _headerSubStyle;
        private GUIStyle _tabStyle;
        private GUIStyle _activeTabStyle;
        private GUIStyle _sectionHeaderStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _subLabelStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _actionButtonStyle;
        private GUIStyle _dangerButtonStyle;
        private GUIStyle _toastStyle;
        private bool _stylesInitialized = false;

        public static UniTask GetCompletedTask()
        {
            return UniTask.CompletedTask;
        }

        public static RoundData GetRoundDataSafe()
        {
            try
            {
                return RoundData.IsNull ? null : RoundData.Instance;
            }
            catch
            {
                return null;
            }
        }

        public static ShotgunData GetShotgunDataSafe()
        {
            try
            {
                return ShotgunData.IsNull ? null : ShotgunData.Instance;
            }
            catch
            {
                return null;
            }
        }

        public static PlayerData GetPlayerDataSafe()
        {
            try
            {
                return PlayerData.IsNull ? null : PlayerData.Instance;
            }
            catch
            {
                return null;
            }
        }

        public static DealerData GetDealerDataSafe()
        {
            try
            {
                return DealerData.IsNull ? null : DealerData.Instance;
            }
            catch
            {
                return null;
            }
        }

        public static ItemsData GetItemsDataSafe()
        {
            try
            {
                return ItemsData.IsNull ? null : ItemsData.Instance;
            }
            catch
            {
                return null;
            }
        }

        private static RoundManager FindRoundManager()
        {
            try
            {
                var rd = GetRoundDataSafe();
                if (rd != null)
                {
                    if (_roundDataRoundManagerProp == null)
                    {
                        _roundDataRoundManagerProp = typeof(RoundData).GetProperty("RoundManager", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    }
                    if (_roundDataRoundManagerProp != null)
                    {
                        var rm = _roundDataRoundManagerProp.GetValue(rd, null) as RoundManager;
                        if (rm != null) return rm;
                    }
                }
                return UnityEngine.Object.FindObjectOfType<RoundManager>();
            }
            catch
            {
                return null;
            }
        }

        private static DealerController FindDealerController()
        {
            try
            {
                var rm = FindRoundManager();
                if (rm != null && _roundManagerDealerControllerField != null)
                {
                    var dc = _roundManagerDealerControllerField.GetValue(rm) as DealerController;
                    if (dc != null) return dc;
                }
                return UnityEngine.Object.FindObjectOfType<DealerController>();
            }
            catch
            {
                return null;
            }
        }

        private static bool IsDealerButton(ItemGridButton btn)
        {
            if (btn == null) return false;
            try
            {
                Transform cur = btn.transform;
                while (cur != null)
                {
                    string nm = cur.name.ToLowerInvariant();
                    if (nm.Contains("dealer") || nm.Contains("opponent") || nm.Contains("ai"))
                    {
                        return true;
                    }
                    cur = cur.parent;
                }
            }
            catch { }
            return false;
        }

        private void Awake()
        {
            Instance = this;
            LoadConfig();
            InitializeReflection();

            if (PermanentDoubleOrNothing)
            {
                UnlockDoubleOrNothing();
            }

            // Center window initially
            float w = Mathf.Min(860f, Screen.width - 20f);
            float h = Mathf.Min(580f, Screen.height - 20f);
            _windowRect = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);

            SetToast(L("CoreTune " + ModVersion + " загружен! Меню: [" + ToggleKey + "]", "CoreTune " + ModVersion + " loaded! Menu: [" + ToggleKey + "]"));
        }

        private void InitializeReflection()
        {
            try
            {
                var rmType = typeof(RoundManager);
                _roundManagerScoreField = rmType.GetField("_doubleOrNothingScore", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) 
                    ?? rmType.GetField("_score", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                _roundManagerEndScoreField = rmType.GetField("_endscore", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    ?? rmType.GetField("_endScore", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                _roundManagerInitialScoreField = rmType.GetField("_doubleOrNothingInitialScore", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    ?? rmType.GetField("_initialScore", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                _roundManagerDealerControllerField = rmType.GetField("_dealerController", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                _roundManagerPlayerItemsField = rmType.GetField("_playerItemsManager", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    ?? rmType.GetField("playerItemsManager", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                _roundManagerDealerItemsField = rmType.GetField("_dealerItemsManager", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                _roundManagerIsEndlessField = rmType.GetField("_isEndless", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                _roundManagerCurrentRoundField = rmType.GetField("_currentRound", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                _roundManagerBeginPlayerTurnMethod = rmType.GetMethod("BeginPlayerTurn", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                var spType = typeof(DefibrillatorDoubleOrNothingScorePresenter);
                _scorePresenterPrevScoreField = spType.GetField("_prevScore", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                _scorePresenterStartScoreField = spType.GetField("_startScore", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                _scorePresenterScoreTextField = spType.GetField("_scoreText", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                var dcType = typeof(DealerController);
                _dealerControllerTargetField = dcType.GetField("_dealerTarget", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                _dealerControllerKnownShellField = dcType.GetField("_knownShellType", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                _dealerControllerKnowShellsField = dcType.GetField("_knowAllShells", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[CoreTune] Reflection init warning: " + ex.Message);
            }
        }

        public static void SetToast(string msg)
        {
            _toastMessage = msg;
            _toastTimer = 3.2f;
        }

        public void ToggleMenu()
        {
            SetMenuOpen(!_isMenuOpen);
        }

        public void SetMenuOpen(bool open)
        {
            _isMenuOpen = open;
            _isDragging = false;
            if (_isMenuOpen)
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                // Restore game native cursor and mouse state
                try
                {
                    _2AwesomeStudio.Buckshot.Utility.Util.ChangeMouseState(true);
                }
                catch { }
                try
                {
                    var cm = CursorManager.Instance;
                    if (cm != null)
                    {
                        cm.SetCursor(CursorType.Default);
                    }
                }
                catch { }
            }

            // Ensure any EventSystems remain active and unbroken
            try
            {
                var allEs = UnityEngine.Object.FindObjectsOfType<UnityEngine.EventSystems.EventSystem>();
                if (allEs != null)
                {
                    for (int i = 0; i < allEs.Length; i++)
                    {
                        if (allEs[i] != null && !allEs[i].enabled)
                        {
                            allEs[i].enabled = true;
                        }
                    }
                }
            }
            catch { }
        }

        private static float _lastUrlOpenTime = 0f;

        [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr ShellExecute(IntPtr hwnd, string lpOperation, string lpFile, string lpParameters, string lpDirectory, int nShowCmd);

        public static void OpenBrowserUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return;

            // Debounce: prevent opening multiple tabs within 1.5 seconds
            if (Time.unscaledTime - _lastUrlOpenTime < 1.5f) return;
            _lastUrlOpenTime = Time.unscaledTime;

            // 1. Copy to clipboard
            try
            {
                GUIUtility.systemCopyBuffer = url;
            }
            catch { }

            bool opened = false;

            // 2. Native Win32 ShellExecute
            try
            {
                long res = (long)ShellExecute(IntPtr.Zero, "open", url, null, null, 1);
                if (res > 32)
                {
                    opened = true;
                }
            }
            catch { }

            // 3. Fallback: Process.Start with UseShellExecute
            if (!opened)
            {
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    };
                    var p = System.Diagnostics.Process.Start(psi);
                    if (p != null) opened = true;
                }
                catch { }
            }

            // 4. Fallback: cmd.exe start
            if (!opened)
            {
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/c start \"\" \"" + url + "\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    System.Diagnostics.Process.Start(psi);
                    opened = true;
                }
                catch { }
            }

            // 5. Fallback: Unity OpenURL
            if (!opened)
            {
                try
                {
                    Application.OpenURL(url);
                    opened = true;
                }
                catch { }
            }

            SetToast(L("Ссылка открыта в браузере (и скопирована в буфер)!", "Link opened in browser (and copied to clipboard)!"));
        }

        private void Update()
        {
            // Maintain unlocked cursor if trainer menu is open
            if (_isMenuOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // FPS calculation
            _fpsFrames++;
            _fpsAccumulator += Time.unscaledDeltaTime;
            _fpsTimeLeft -= Time.unscaledDeltaTime;
            if (_fpsTimeLeft <= 0f)
            {
                _currentFps = _fpsAccumulator > 0f ? (_fpsFrames / _fpsAccumulator) : 60f;
                _fpsFrames = 0;
                _fpsAccumulator = 0f;
                _fpsTimeLeft = 0.5f;
            }

            // Toast timer
            if (_toastTimer > 0f)
            {
                _toastTimer -= Time.unscaledDeltaTime;
            }

            // Fast-Boot & Instant Start: ensure game never hangs on SplashScene or 5-second StartGame fade
            if (FastBoot)
            {
                try
                {
                    var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                    if ((activeScene.name == "SplashScene" || activeScene.name == "Splash") && !_splashBypassed)
                    {
                        _splashTimer += Time.unscaledDeltaTime;
                        if (_splashTimer > 0.4f)
                        {
                            _splashBypassed = true;
                            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
                        }
                    }
                    else if (activeScene.name == "MainMenu")
                    {
                        var mmc = UnityEngine.Object.FindObjectOfType<MainMenuController>();
                        if (mmc != null)
                        {
                            var titleField = typeof(MainMenuController).GetField("_mainMenuTitle", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                            if (titleField != null)
                            {
                                var titleObj = titleField.GetValue(mmc) as GameObject;
                                if (titleObj != null && !titleObj.activeSelf)
                                {
                                    // Start button was clicked and title disabled: skip 5-second fade and load MainScene immediately!
                                    mmc.LoadMainScene();
                                }
                            }
                        }
                    }
                    else
                    {
                        _splashBypassed = true;
                    }
                }
                catch { }
            }

            // Hotkey rebinding for cheat actions
            if (!string.IsNullOrEmpty(_bindingActionId))
            {
                if (Input.anyKeyDown)
                {
                    if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete))
                    {
                        SetHotkeyById(_bindingActionId, KeyCode.None);
                        SetToast(L("Горячая клавиша отключена", "Hotkey unbound"));
                        _bindingActionId = null;
                        SaveConfig();
                    }
                    else
                    {
                        foreach (KeyCode k in Enum.GetValues(typeof(KeyCode)))
                        {
                            if (k != KeyCode.None && (k < KeyCode.Mouse0 || k > KeyCode.Mouse6) && Input.GetKeyDown(k))
                            {
                                if (k == ToggleKey)
                                {
                                    SetToast(L("Клавиша занята кнопкой открытия меню! Выберите другую.", "Key is used for menu toggle! Choose another."));
                                }
                                else
                                {
                                    SetHotkeyById(_bindingActionId, k);
                                    SetToast(string.Format(L("Клавиша назначена: [{0}]", "Hotkey bound: [{0}]"), k));
                                    _bindingActionId = null;
                                    SaveConfig();
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            // Menu Toggle Key listener & rebinding
            if (_isBindingToggleKey)
            {
                if (Input.anyKeyDown)
                {
                    if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete))
                    {
                        _isBindingToggleKey = false;
                        SetToast(L("Назначение кнопки меню отменено", "Menu toggle key assignment cancelled"));
                    }
                    else
                    {
                        foreach (KeyCode k in Enum.GetValues(typeof(KeyCode)))
                        {
                            if (k != KeyCode.None && (k < KeyCode.Mouse0 || k > KeyCode.Mouse6) && Input.GetKeyDown(k))
                            {
                                ToggleKey = k;
                                _isBindingToggleKey = false;
                                UnbindCheatIfKeyMatches(k);
                                SaveConfig();
                                SetToast(string.Format(L("Кнопка меню сохранена: [{0}]", "Menu toggle key saved: [{0}]"), ToggleKey));
                                break;
                            }
                        }
                    }
                }
            }
            else if (string.IsNullOrEmpty(_bindingActionId))
            {
                if (ToggleKey != KeyCode.None && Input.GetKeyDown(ToggleKey))
                {
                    ToggleMenu();
                }
            }

            // Escape closes menu when open (only if not currently binding a key)
            if (_isMenuOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                if (string.IsNullOrEmpty(_bindingActionId) && !_isBindingToggleKey)
                {
                    SetMenuOpen(false);
                }
            }

            // Quick Hotkeys
            HandleHotkeys();

            // Continuous Cheats Maintenance
            MaintainActiveCheats();
        }

        private void LateUpdate()
        {
            // Ensure cursor stays unlocked and visible while trainer menu is open
            if (_isMenuOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private static bool CheckHotkeyDown(KeyCode key)
        {
            return key != KeyCode.None && Input.GetKeyDown(key);
        }

        private void HandleHotkeys()
        {
            if (!string.IsNullOrEmpty(_bindingActionId) || _isBindingToggleKey)
                return;

            if (CheckHotkeyDown(KeyGodMode))
            {
                GodMode = !GodMode;
                SetToast(L("Бессмертие: ", "God Mode: ") + (GodMode ? L("ВКЛ", "ON") : L("ВЫКЛ", "OFF")));
            }

            if (CheckHotkeyDown(KeyOneHitKill))
            {
                OneHitKill = !OneHitKill;
                SetToast(L("Убийство с 1 выстрела: ", "One-Hit Kill: ") + (OneHitKill ? L("ВКЛ", "ON") : L("ВЫКЛ", "OFF")));
            }

            if (CheckHotkeyDown(KeySmartRoulette))
            {
                SmartRoulette = !SmartRoulette;
                SetToast(L("Умная рулетка: ", "Smart Roulette: ") + (SmartRoulette ? L("ВКЛ", "ON") : L("ВЫКЛ", "OFF")));
            }

            if (CheckHotkeyDown(KeyAlwaysLive))
            {
                AlwaysLive = !AlwaysLive;
                if (AlwaysLive) AlwaysBlank = false;
                SetToast(L("Всегда боевые: ", "Always Live: ") + (AlwaysLive ? L("ВКЛ", "ON") : L("ВЫКЛ", "OFF")));
            }

            if (CheckHotkeyDown(KeyAlwaysBlank))
            {
                AlwaysBlank = !AlwaysBlank;
                if (AlwaysBlank) AlwaysLive = false;
                SetToast(L("Всегда холостые: ", "Always Blank: ") + (AlwaysBlank ? L("ВКЛ", "ON") : L("ВЫКЛ", "OFF")));
            }

            if (CheckHotkeyDown(KeyInfiniteItems))
            {
                InfiniteItems = !InfiniteItems;
                SetToast(L("Бесконечные предметы: ", "Infinite Items: ") + (InfiniteItems ? L("ВКЛ", "ON") : L("ВЫКЛ", "OFF")));
            }

            if (CheckHotkeyDown(KeyForceDealerSelfHarm))
            {
                ForceDealerSelfHarm = !ForceDealerSelfHarm;
                SetToast(L("Самоубийство дилера: ", "Dealer Self-Harm: ") + (ForceDealerSelfHarm ? L("ВКЛ", "ON") : L("ВЫКЛ", "OFF")));
            }

            if (CheckHotkeyDown(KeyLuckyMedicine))
            {
                LuckyMedicine = !LuckyMedicine;
                SetToast(L("Удачливая просрочка: ", "Lucky Medicine: ") + (LuckyMedicine ? L("ВКЛ", "ON") : L("ВЫКЛ", "OFF")));
            }

            if (CheckHotkeyDown(KeyAntiWireCut))
            {
                AntiWireCut = !AntiWireCut;
                SetToast(L("Защита проводов: ", "Anti-Wire Cut: ") + (AntiWireCut ? L("ВКЛ", "ON") : L("ВЫКЛ", "OFF")));
            }

            if (CheckHotkeyDown(KeyAntiCuffs))
            {
                AntiCuffs = !AntiCuffs;
                SetToast(L("Иммунитет к наручникам: ", "Anti-Cuffs: ") + (AntiCuffs ? L("ВКЛ", "ON") : L("ВЫКЛ", "OFF")));
            }

            if (CheckHotkeyDown(KeyFreezeTime))
            {
                FreezeTime = !FreezeTime;
                SetToast(L("Остановка времени: ", "Freeze Time: ") + (FreezeTime ? L("ВКЛ", "ON") : L("ВЫКЛ", "OFF")));
            }

            if (CheckHotkeyDown(KeyFullHeal))
            {
                FullHeal();
            }

            if (CheckHotkeyDown(KeySkipDealerTurn))
            {
                SkipDealerTurn();
            }

            if (CheckHotkeyDown(KeyInvertShell))
            {
                InvertCurrentShell();
            }

            if (CheckHotkeyDown(KeySawShotgun))
            {
                SawShotgun();
            }

            if (CheckHotkeyDown(KeyAddMoney))
            {
                AddMoney(10000);
            }
        }

        private void MaintainActiveCheats()
        {
            try
            {
                // Game Speed & Time freeze
                if (FreezeTime)
                {
                    Time.timeScale = 0f;
                }
                else if (Mathf.Abs(Time.timeScale - GameSpeed) > 0.01f)
                {
                    Time.timeScale = GameSpeed;
                }

                var rd = GetRoundDataSafe();
                var sd = GetShotgunDataSafe();

                if (rd != null)
                {
                    if (GodMode)
                    {
                        int targetHp = 6;
                        if (rd.PlayerHealth < targetHp)
                        {
                            rd.PlayerHealth = targetHp;
                            UpdateHealthDisplays();
                        }
                    }
                    if (AntiWireCut)
                    {
                        rd.IsPlayerWireCut = false;
                    }
                    if (AntiCuffs)
                    {
                        rd.PlayerHandcuffed = false;
                        rd.PlayerAboutToBreakFree = false;
                        try
                        {
                            var hm = HandcuffManager.Instance ?? UnityEngine.Object.FindObjectOfType<HandcuffManager>();
                            if (hm != null)
                            {
                                var cuffsField = typeof(HandcuffManager).GetField("_playerHandcuffsObject", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                                if (cuffsField != null)
                                {
                                    var cuffsObj = cuffsField.GetValue(hm) as GameObject;
                                    if (cuffsObj != null && cuffsObj.activeSelf) cuffsObj.SetActive(false);
                                }
                            }
                            var anims = UnityEngine.Object.FindObjectsOfType<HandcuffsAnimator>();
                            if (anims != null)
                            {
                                for (int a = 0; a < anims.Length; a++)
                                {
                                    if (anims[a] != null) anims[a].ToggleHandcuff(0);
                                }
                            }
                        }
                        catch { }
                    }
                    if (PermanentDealerCuffs)
                    {
                        rd.DealerHandcuffed = true;
                        rd.IsDealerAboutToBreakFree = false;
                    }
                }

                // Shotgun shells override & damage repair
                if (sd != null)
                {
                    if (sd.OverrideShotgunDamage.HasValue && sd.OverrideShotgunDamage.Value <= 0)
                    {
                        sd.SetShotgunDamage(-1);
                    }
                    if (sd.ShellsList != null && sd.ShellsList.Count > 0)
                    {
                        var chamber = sd.ShellsList[0];
                        if (chamber != null)
                        {
                            if (AlwaysLive && chamber.ShellType != ShellType.Live)
                            {
                                chamber.ShellType = ShellType.Live;
                                chamber.Init(ShellType.Live);
                            }
                            else if (AlwaysBlank && chamber.ShellType != ShellType.Blank)
                            {
                                chamber.ShellType = ShellType.Blank;
                                chamber.Init(ShellType.Blank);
                            }
                        }
                    }
                }

                // Dealer controller force self harm
                var dealer = FindDealerController();
                if (dealer != null)
                {
                    if (ForceDealerSelfHarm && _dealerControllerTargetField != null)
                    {
                        _dealerControllerTargetField.SetValue(dealer, DealerTarget.Self);
                    }

                    if (dealer.IsDealerHoldingShotgun && sd != null && sd.ShellsList != null && sd.ShellsList.Count > 0)
                    {
                        var chamber = sd.ShellsList[0];
                        DealerTarget dTarget = DealerTarget.Player;
                        if (_dealerControllerTargetField != null)
                        {
                            try { dTarget = (DealerTarget)_dealerControllerTargetField.GetValue(dealer); } catch { }
                        }
                        if (dTarget == DealerTarget.Player)
                        {
                            if ((SmartRoulette || GodMode) && chamber != null && chamber.ShellType != ShellType.Blank)
                            {
                                chamber.ShellType = ShellType.Blank;
                                chamber.Init(ShellType.Blank);
                                sd.ShotgunDamage = 0;
                            }
                        }
                        else if (dTarget == DealerTarget.Self)
                        {
                            if (SmartRoulette && chamber != null && chamber.ShellType != ShellType.Live)
                            {
                                chamber.ShellType = ShellType.Live;
                                chamber.Init(ShellType.Live);
                            }
                        }
                    }
                }

                // Restore player items if InfiniteItems is active
                var pim = UnityEngine.Object.FindObjectOfType<PlayerItemsManager>();
                if (pim != null && pim.ItemGridButtons != null)
                {
                    for (int i = 0; i < pim.ItemGridButtons.Count && i < 8; i++)
                    {
                        var btn = pim.ItemGridButtons[i];
                        if (btn != null && !IsDealerButton(btn))
                        {
                            var curItem = btn.GetItem();
                            if (curItem != null)
                            {
                                _playerSlotSnapshot[i] = curItem.GetItemType();
                            }
                            else if (InfiniteItems && _playerSlotSnapshot[i] != ItemType.NONE && !_slotRespawning[i] && !pim.IsPlacingItem)
                            {
                                int slotIdx = i;
                                ItemType itypeToRestore = _playerSlotSnapshot[i];
                                StartCoroutine(RespawnSlotItemCoroutine(slotIdx, itypeToRestore));
                            }
                        }
                    }
                }

                // Permanent Double or Nothing mode persistence
                if (PermanentDoubleOrNothing)
                {
                    try
                    {
                        var fu = _2AwesomeStudio.TACoreUtils.FileUtils.TAFileUtils.Instance;
                        if (fu != null)
                        {
                            fu.SetBool("DoNEnabled", true, 0);
                        }
                    }
                    catch { }

                    try
                    {
                        var rm = ReferencesManager.Instance;
                        var intro = (rm != null) ? rm.IntroManager : UnityEngine.Object.FindObjectOfType<IntroManager>();
                        if (intro != null)
                        {
                            if (!intro.IsEndlessModeUnlocked) intro.IsEndlessModeUnlocked = true;
                            if (!intro.allowingPills) intro.allowingPills = true;
                            var pillField = typeof(IntroManager).GetField("_pillBottle", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                            if (pillField != null)
                            {
                                var pillObj = pillField.GetValue(intro) as GameObject;
                                if (pillObj != null && !pillObj.activeSelf)
                                {
                                    pillObj.SetActive(true);
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private IEnumerator RespawnSlotItemCoroutine(int slotIndex, ItemType itype)
        {
            _slotRespawning[slotIndex] = true;
            yield return new WaitForSeconds(0.4f);

            try
            {
                var pim = UnityEngine.Object.FindObjectOfType<PlayerItemsManager>();
                if (pim != null && pim.ItemGridButtons != null && slotIndex < pim.ItemGridButtons.Count && slotIndex < 8)
                {
                    var btn = pim.ItemGridButtons[slotIndex];
                    if (btn != null && !IsDealerButton(btn) && btn.GetItem() == null)
                    {
                        ItemsHelper helper = null;
                        var helperField = typeof(PlayerItemsManager).GetField("_itemsHelper", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        if (helperField != null) helper = helperField.GetValue(pim) as ItemsHelper;
                        if (helper == null) helper = new ItemsHelper();

                        ItemSO so = (helper != null) ? helper.GetItemSoByType(itype) : null;
                        if (so != null && so.Prefab != null)
                        {
                            Transform btnT = btn.transform;
                            Vector3 pos = btnT.position + so.PositionOffset;
                            Quaternion rot = Quaternion.Euler(btnT.localEulerAngles + so.RotationOffset);

                            Transform parentT = null;
                            var parentField = typeof(PlayerItemsManager).GetField("_itemsParent", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                            if (parentField != null) parentT = parentField.GetValue(pim) as Transform;
                            if (parentT == null) parentT = btnT;

                            Item newItem = UtilMesh.InstantiateWithMeshFix<Item>(so.Prefab, pos, rot, parentT);
                            if (newItem != null)
                            {
                                newItem.Init(so);
                                btn.SetItem(newItem);
                                btn.Init();
                                btn.gameObject.SetActive(true);

                                if (pim.OccupiedGridButtonsIndexes != null)
                                {
                                    pim.OccupiedGridButtonsIndexes[slotIndex] = newItem;
                                }
                                pim.NumberPlayerItems++;
                                pim.SetItemButtonsParentVisiblity(true);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[CoreTune] RespawnSlotItemCoroutine error: " + ex.Message);
            }
            finally
            {
                _slotRespawning[slotIndex] = false;
            }
        }

        #region Injected IL Hook Callbacks
        // Injected at beginning of RoundData.DamageTarget()
        public static void OnDamageTarget(ref Participant participant, ref int value)
        {
            try
            {
                if (GodMode && participant == Participant.Player)
                {
                    value = 0;
                    SetToast("God Mode: Урон заблокирован (0 HP)");
                    return;
                }
                if (OneHitKill && participant == Participant.Dealer)
                {
                    value = 999;
                    SetToast("One-Hit Kill: Смертельный выстрел!");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[CoreTune] Error in OnDamageTarget hook: " + ex);
            }
        }

        // Injected at beginning of ShotgunManager.Shoot()
        public static void OnShotgunShoot(ShootTarget shootTarget)
        {
            try
            {
                var sd = GetShotgunDataSafe();
                var chamber = (sd != null && sd.ShellsList != null && sd.ShellsList.Count > 0) ? sd.ShellsList[0] : null;

                if (OneHitKill && shootTarget == ShootTarget.Dealer)
                {
                    if (chamber != null)
                    {
                        chamber.ShellType = ShellType.Live;
                        chamber.Init(ShellType.Live);
                    }
                    if (sd != null)
                    {
                        var rd = GetRoundDataSafe();
                        int dHp = (rd != null) ? rd.DealerHealth : 6;
                        sd.ShotgunDamage = Mathf.Max(dHp, 10);
                    }
                    SetToast(L("One-Hit Kill: Смертельный выстрел по дилеру!", "One-Hit Kill: Lethal shot to dealer!"));
                    return;
                }

                if (GodMode && shootTarget == ShootTarget.Player)
                {
                    if (sd != null)
                    {
                        sd.ShotgunDamage = 0;
                    }
                    if (chamber != null)
                    {
                        chamber.ShellType = ShellType.Blank;
                        chamber.Init(ShellType.Blank);
                    }
                    SetToast(L("God Mode: Выстрел по игроку заблокирован (0 урона)!", "God Mode: Shot to player blocked (0 damage)!"));
                    return;
                }

                if (AlwaysLive)
                {
                    if (chamber != null)
                    {
                        chamber.ShellType = ShellType.Live;
                        chamber.Init(ShellType.Live);
                    }
                }
                else if (AlwaysBlank)
                {
                    if (chamber != null)
                    {
                        chamber.ShellType = ShellType.Blank;
                        chamber.Init(ShellType.Blank);
                    }
                }
                else if (SmartRoulette)
                {
                    if (chamber != null)
                    {
                        if (shootTarget == ShootTarget.Dealer)
                        {
                            chamber.ShellType = ShellType.Live;
                            chamber.Init(ShellType.Live);
                        }
                        else if (shootTarget == ShootTarget.Player)
                        {
                            chamber.ShellType = ShellType.Blank;
                            chamber.Init(ShellType.Blank);
                            if (sd != null) sd.ShotgunDamage = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[CoreTune] Error in OnShotgunShoot hook: " + ex);
            }
        }

        // Injected at beginning of DealerController.ShootToTarget()
        public static bool OnDealerShootToTarget(DealerController dealer)
        {
            try
            {
                if (dealer == null) return true;
                var sd = GetShotgunDataSafe();
                var chamber = (sd != null && sd.ShellsList != null && sd.ShellsList.Count > 0) ? sd.ShellsList[0] : null;

                if (SkipNextDealerTurn || DealerTurnSkipInProgress || PermanentDealerCuffs)
                {
                    SkipNextDealerTurn = false;
                    DealerTurnSkipInProgress = false;

                    if (sd != null) sd.ShotgunDamage = 0;
                    if (chamber != null)
                    {
                        chamber.ShellType = ShellType.Blank;
                        chamber.Init(ShellType.Blank);
                    }

                    try
                    {
                        var mEndTurnMain = typeof(DealerController).GetMethod("EndTurnMain", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        if (mEndTurnMain != null)
                        {
                            mEndTurnMain.Invoke(dealer, null);
                        }
                    }
                    catch { }

                    return false;
                }

                if (ForceDealerSelfHarm && _dealerControllerTargetField != null)
                {
                    _dealerControllerTargetField.SetValue(dealer, DealerTarget.Self);
                }

                DealerTarget target = DealerTarget.Player;
                if (_dealerControllerTargetField != null)
                {
                    try { target = (DealerTarget)_dealerControllerTargetField.GetValue(dealer); } catch { }
                }

                if (target == DealerTarget.Self)
                {
                    if ((SmartRoulette || ForceDealerSelfHarm || OneHitKill) && chamber != null)
                    {
                        chamber.ShellType = ShellType.Live;
                        chamber.Init(ShellType.Live);
                    }
                    if (OneHitKill && sd != null)
                    {
                        var rd = GetRoundDataSafe();
                        int dHp = (rd != null) ? rd.DealerHealth : 6;
                        sd.ShotgunDamage = Mathf.Max(dHp, 10);
                    }
                }
                else // Target is Player
                {
                    if ((SmartRoulette || GodMode) && chamber != null)
                    {
                        chamber.ShellType = ShellType.Blank;
                        chamber.Init(ShellType.Blank);
                        if (sd != null) sd.ShotgunDamage = 0;
                    }
                    else if (GodMode && sd != null)
                    {
                        sd.ShotgunDamage = 0;
                    }
                }

                if (AlwaysLive && chamber != null)
                {
                    chamber.ShellType = ShellType.Live;
                    chamber.Init(ShellType.Live);
                }
                else if (AlwaysBlank && chamber != null)
                {
                    chamber.ShellType = ShellType.Blank;
                    chamber.Init(ShellType.Blank);
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("[CoreTune] OnDealerShootToTarget error: " + ex);
                return true;
            }
        }

        // Hooked at beginning of MainMenuController.StartGame()
        public static bool OnMainMenuStartGame(MainMenuController mmc)
        {
            try
            {
                if (FastBoot && mmc != null)
                {
                    mmc.LoadMainScene();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[CoreTune] OnMainMenuStartGame error: " + ex);
            }
            return false;
        }

        // Hooked in ExpiredMedicineSingleplayer.<UseExpiredMedicinePlayer>d__2.MoveNext
        public static bool GetMedicineResult(bool original)
        {
            if (LuckyMedicine) return true;
            return original;
        }

        // Hooked in DefibrillatorDoubleOrNothingScorePresenter.<BeginScoreLerping>d__22.MoveNext
        public static int GetAdjustedRound1Score(int originalScore, DefibrillatorDoubleOrNothingScorePresenter presenter)
        {
            try
            {
                int adjusted = originalScore;
                if (BonusMoney > 0)
                {
                    adjusted = originalScore + BonusMoney;
                    BonusMoney = 0;
                }
                else if (presenter != null)
                {
                    if (_scorePresenterPrevScoreField == null)
                    {
                        _scorePresenterPrevScoreField = typeof(DefibrillatorDoubleOrNothingScorePresenter).GetField("_prevScore", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    }
                    if (_scorePresenterPrevScoreField != null)
                    {
                        int prevVal = (int)_scorePresenterPrevScoreField.GetValue(presenter);
                        if (prevVal > 0)
                        {
                            adjusted = prevVal + originalScore;
                        }
                    }
                }
                return adjusted;
            }
            catch
            {
                return originalScore;
            }
        }
        #endregion

        #region Cheat Actions
        public void InvertCurrentShell()
        {
            try
            {
                var sd = GetShotgunDataSafe();
                if (sd != null && sd.ShellsList != null && sd.ShellsList.Count > 0)
                {
                    var chamber = sd.ShellsList[0];
                    if (chamber != null)
                    {
                        ShellType newType = (chamber.ShellType == ShellType.Live) ? ShellType.Blank : ShellType.Live;
                        chamber.ShellType = newType;
                        chamber.Init(newType);
                        SetToast(string.Format("Патрон в патроннике изменен: {0}", (newType == ShellType.Live ? "БОЕВОЙ [LIVE]" : "ХОЛОСТОЙ [BLANK]")));
                        return;
                    }
                }
                SetToast("Дробовик пуст или патрон не заряжен");
            }
            catch (Exception ex)
            {
                SetToast("Ошибка смены патрона: " + ex.Message);
            }
        }

        public void SawShotgun()
        {
            try
            {
                var sd = GetShotgunDataSafe();
                if (sd != null)
                {
                    sd.SetShotgunDamage(2);
                    SetToast("Ствол спилен: Урон удвоен (2 HP)!");
                    return;
                }
                SetToast("Дробовик не найден");
            }
            catch (Exception ex)
            {
                SetToast("Ошибка спиливания: " + ex.Message);
            }
        }

        public void SetShotgunDamage(int dmg)
        {
            try
            {
                var sd = GetShotgunDataSafe();
                if (sd != null)
                {
                    sd.SetShotgunDamage(dmg);
                    SetToast(string.Format("Урон дробовика установлен: {0} HP", dmg));
                    return;
                }
                SetToast("Дробовик не найден");
            }
            catch (Exception ex)
            {
                SetToast("Ошибка установки урона: " + ex.Message);
            }
        }

        private static void UpdateHealthDisplays()
        {
            try
            {
                var dhp = UnityEngine.Object.FindObjectOfType<DefibrillatorHealthPresenter>();
                if (dhp != null)
                {
                    dhp.UpdateDisplay();
                    return;
                }
                var duim = UnityEngine.Object.FindObjectOfType<DefibrillatorUIManager>();
                if (duim != null && duim.HealthPresenter != null)
                {
                    duim.HealthPresenter.UpdateDisplay();
                }
            }
            catch { }
        }

        public void FullHeal()
        {
            try
            {
                var rd = GetRoundDataSafe();
                if (rd != null)
                {
                    rd.PlayerHealth = 6;
                    UpdateHealthDisplays();
                    SetToast("Здоровье игрока полностью восстановлено (6 HP)");
                    return;
                }
                SetToast("Игра не в раунде");
            }
            catch (Exception ex)
            {
                SetToast("Ошибка лечения: " + ex.Message);
            }
        }

        public void SetPlayerHealth(int hp)
        {
            try
            {
                var rd = GetRoundDataSafe();
                if (rd != null)
                {
                    if (hp <= 0)
                    {
                        rd.PlayerHealth = 0;
                        UpdateHealthDisplays();
                        var rm = FindRoundManager();
                        if (rm != null)
                        {
                            Cysharp.Threading.Tasks.UniTaskExtensions.Forget(rm.OutOfHealth(ShootTarget.Player));
                        }
                        SetToast("Здоровье игрока: 0 (Поражение)");
                    }
                    else
                    {
                        rd.PlayerHealth = hp;
                        UpdateHealthDisplays();
                        SetToast(string.Format("Здоровье игрока: {0} HP", hp));
                    }
                    return;
                }
                SetToast("Игра не в раунде");
            }
            catch (Exception ex)
            {
                SetToast("Ошибка здоровья: " + ex.Message);
            }
        }

        public void SetDealerHealth(int hp)
        {
            try
            {
                var rd = GetRoundDataSafe();
                if (rd != null)
                {
                    if (hp <= 0)
                    {
                        rd.DealerHealth = 0;
                        UpdateHealthDisplays();
                        var rm = FindRoundManager();
                        if (rm != null)
                        {
                            Cysharp.Threading.Tasks.UniTaskExtensions.Forget(rm.OutOfHealth(ShootTarget.Dealer));
                        }
                        SetToast("Дилер повержен (HP: 0)");
                    }
                    else
                    {
                        rd.DealerHealth = hp;
                        UpdateHealthDisplays();
                        SetToast(string.Format("Здоровье дилера: {0} HP", hp));
                    }
                    return;
                }
                SetToast("Игра не в раунде");
            }
            catch (Exception ex)
            {
                SetToast("Ошибка дилера: " + ex.Message);
            }
        }

        public void SkipDealerTurn()
        {
            try
            {
                SkipNextDealerTurn = true;
                var dc = FindDealerController();
                var sd = GetShotgunDataSafe();
                if (sd != null) sd.ShotgunDamage = 0;

                if (dc != null && dc.IsDealerHoldingShotgun)
                {
                    DealerTurnSkipInProgress = true;
                    try
                    {
                        var mEndTurnMain = typeof(DealerController).GetMethod("EndTurnMain", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        if (mEndTurnMain != null)
                        {
                            mEndTurnMain.Invoke(dc, null);
                        }
                    }
                    catch { }
                }

                SetToast("Ход дилера пропущен!");
            }
            catch (Exception ex)
            {
                SetToast("Ошибка пропуска хода: " + ex.Message);
            }
        }

        public void ClearDealerItems()
        {
            try
            {
                var dim = UnityEngine.Object.FindObjectOfType<DealerItemsManager>();
                if (dim != null)
                {
                    var slots = dim.ItemSlots;
                    if (slots != null)
                    {
                        for (int i = 0; i < slots.Count; i++)
                        {
                            var slot = slots[i];
                            if (slot != null)
                            {
                                if (slot.Item != null && slot.Item.gameObject != null)
                                {
                                    UnityEngine.Object.Destroy(slot.Item.gameObject);
                                }
                                if (slot.itemGridButton != null)
                                {
                                    slot.itemGridButton.ClearItem();
                                }
                                if (slot.SlotTransform != null)
                                {
                                    for (int c = slot.SlotTransform.childCount - 1; c >= 0; c--)
                                    {
                                        UnityEngine.Object.Destroy(slot.SlotTransform.GetChild(c).gameObject);
                                    }
                                }
                                slot.Item = null;
                                slot.IsOccupied = false;
                            }
                        }
                    }

                    var dData = GetDealerDataSafe();
                    if (dData != null && dData.OwnedItems != null)
                    {
                        for (int i = dData.OwnedItems.Count - 1; i >= 0; i--)
                        {
                            var item = dData.OwnedItems[i];
                            if (item != null && item.gameObject != null)
                            {
                                UnityEngine.Object.Destroy(item.gameObject);
                            }
                        }
                        dData.OwnedItems.Clear();
                    }

                    var _idSafe = GetItemsDataSafe();
                    if (_idSafe != null && _idSafe.CurrentItemAmounts != null)
                    {
                        foreach (var ia in _idSafe.CurrentItemAmounts)
                        {
                            if (ia != null) ia.DealerAmount = 0;
                        }
                    }

                    dim.CheckNullOnList();
                    SetToast("Предметы дилера полностью очищены");
                }
                else
                {
                    SetToast("Дилер еще не за столом");
                }
            }
            catch (Exception ex)
            {
                SetToast("Ошибка очистки дилера: " + ex.Message);
            }
        }

        public void ClearPlayerItems()
        {
            try
            {
                var pim = UnityEngine.Object.FindObjectOfType<PlayerItemsManager>();
                if (pim != null)
                {
                    if (pim.ItemGridButtons != null)
                    {
                        for (int i = 0; i < pim.ItemGridButtons.Count && i < 8; i++)
                        {
                            var btn = pim.ItemGridButtons[i];
                            if (btn != null && !IsDealerButton(btn))
                            {
                                if (btn.GetItem() != null && btn.GetItem().gameObject != null)
                                {
                                    UnityEngine.Object.Destroy(btn.GetItem().gameObject);
                                }
                                btn.ClearItem();
                            }
                        }
                    }

                    if (pim.OccupiedGridButtonsIndexes != null)
                    {
                        var keysToRemove = new List<int>();
                        foreach (var kvp in pim.OccupiedGridButtonsIndexes)
                        {
                            if (kvp.Key < 8) keysToRemove.Add(kvp.Key);
                        }
                        foreach (var k in keysToRemove)
                        {
                            pim.OccupiedGridButtonsIndexes.Remove(k);
                        }
                    }

                    pim.NumberPlayerItems = 0;

                    var _idSafe = GetItemsDataSafe();
                    if (_idSafe != null && _idSafe.CurrentItemAmounts != null)
                    {
                        foreach (var ia in _idSafe.CurrentItemAmounts)
                        {
                            if (ia != null) ia.PlayerAmount = 0;
                        }
                    }

                    for (int s = 0; s < 8; s++)
                    {
                        _playerSlotSnapshot[s] = ItemType.NONE;
                    }

                    SetToast("Инвентарь игрока очищен");
                }
                else
                {
                    SetToast("Игрок еще не за столом");
                }
            }
            catch (Exception ex)
            {
                SetToast("Ошибка очистки игрока: " + ex.Message);
            }
        }

        public void GiveItemToTable(string itemName)
        {
            try
            {
                ItemType itype = ItemType.NONE;
                if (!string.IsNullOrEmpty(itemName))
                {
                    try { itype = (ItemType)Enum.Parse(typeof(ItemType), itemName, true); } catch { }
                }
                if (itype == ItemType.NONE)
                {
                    SetToast("Предмет не выбран");
                    return;
                }

                var rm = FindRoundManager();
                PlayerItemsManager pim = null;
                if (rm != null && _roundManagerPlayerItemsField != null)
                {
                    pim = _roundManagerPlayerItemsField.GetValue(rm) as PlayerItemsManager;
                }
                if (pim == null)
                {
                    pim = UnityEngine.Object.FindObjectOfType<PlayerItemsManager>();
                }

                if (pim != null && pim.ItemGridButtons != null && pim.OccupiedGridButtonsIndexes != null)
                {
                    int freeSlot = -1;
                    for (int i = 0; i < pim.ItemGridButtons.Count && i < 8; i++)
                    {
                        var btn = pim.ItemGridButtons[i];
                        if (btn != null && !IsDealerButton(btn))
                        {
                            if (!pim.OccupiedGridButtonsIndexes.ContainsKey(i) && btn.GetItem() == null)
                            {
                                freeSlot = i;
                                break;
                            }
                        }
                    }

                    if (freeSlot == -1 || pim.NumberPlayerItems >= 8)
                    {
                        SetToast("Стол игрока полон (макс 8 предметов)");
                        return;
                    }

                    ItemsHelper helper = null;
                    var helperField = typeof(PlayerItemsManager).GetField("_itemsHelper", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (helperField != null) helper = helperField.GetValue(pim) as ItemsHelper;
                    if (helper == null) helper = new ItemsHelper();

                    ItemSO so = (helper != null) ? helper.GetItemSoByType(itype) : null;
                    if (so != null && so.Prefab != null)
                    {
                        var btn = pim.ItemGridButtons[freeSlot];
                        Transform btnT = btn.transform;
                        Vector3 pos = btnT.position + so.PositionOffset;
                        Quaternion rot = Quaternion.Euler(btnT.localEulerAngles + so.RotationOffset);

                        Transform parentT = null;
                        var parentField = typeof(PlayerItemsManager).GetField("_itemsParent", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        if (parentField != null) parentT = parentField.GetValue(pim) as Transform;
                        if (parentT == null) parentT = btnT;

                        Item newItem = UtilMesh.InstantiateWithMeshFix<Item>(so.Prefab, pos, rot, parentT);
                        if (newItem != null)
                        {
                            newItem.Init(so);
                            btn.SetItem(newItem);
                            btn.Init();
                            btn.gameObject.SetActive(true);

                            pim.OccupiedGridButtonsIndexes[freeSlot] = newItem;
                            pim.NumberPlayerItems++;
                            pim.SetItemButtonsParentVisiblity(true);

                            var _idSafe = GetItemsDataSafe();
                            if (_idSafe != null && _idSafe.CurrentItemAmounts != null)
                            {
                                foreach (var ia in _idSafe.CurrentItemAmounts)
                                {
                                    if (ia != null && ia.ItemType == itype)
                                    {
                                        ia.PlayerAmount++;
                                        break;
                                    }
                                }
                            }

                            _playerSlotSnapshot[freeSlot] = itype;
                            SetToast(L("Выдан игроку: ", "Given to Player: ") + GetItemDisplayName(itype));
                            return;
                        }
                    }
                    SetToast(L("Не удалось создать предмет", "Failed to create item"));
                }
                else
                {
                    SetToast(L("Игрок еще не за столом", "Player not at table yet"));
                }
            }
            catch (Exception ex)
            {
                SetToast(L("Ошибка спавна: ", "Spawn error: ") + ex.Message);
            }
        }

        public static string GetItemDisplayName(ItemType itype)
        {
            switch (itype)
            {
                case ItemType.CIGARETTE: return L("Сигареты", "Cigarettes");
                case ItemType.BEER: return L("Пиво", "Beer");
                case ItemType.HANDCUFFS: return L("Наручники", "Handcuffs");
                case ItemType.HANDSAW: return L("Ножовка", "Handsaw");
                case ItemType.MAGNIFIER: return L("Лупа", "Magnifier");
                case ItemType.INVERTER: return L("Инвертор", "Inverter");
                case ItemType.ADRENALINE: return L("Адреналин", "Adrenaline");
                case ItemType.PHONE: return L("Телефон", "Burner Phone");
                case ItemType.MEDICINE: return L("Таблетки", "Expired Medicine");
                default: return itype.ToString();
            }
        }

        public void SpawnDealerItem(string itemName)
        {
            try
            {
                ItemType itype = ItemType.NONE;
                if (!string.IsNullOrEmpty(itemName))
                {
                    try { itype = (ItemType)Enum.Parse(typeof(ItemType), itemName, true); } catch { }
                }
                if (itype == ItemType.NONE)
                {
                    SetToast(L("Предмет не выбран", "Item not selected"));
                    return;
                }

                var rm = FindRoundManager();
                DealerItemsManager dim = null;
                if (rm != null && _roundManagerDealerItemsField != null)
                {
                    dim = _roundManagerDealerItemsField.GetValue(rm) as DealerItemsManager;
                }
                if (dim == null)
                {
                    dim = UnityEngine.Object.FindObjectOfType<DealerItemsManager>();
                }

                if (dim != null && dim.ItemSlots != null)
                {
                    DealerItemSlot freeSlot = null;
                    for (int i = 0; i < dim.ItemSlots.Count && i < 8; i++)
                    {
                        var s = dim.ItemSlots[i];
                        if (s != null && !s.IsOccupied && s.Item == null)
                        {
                            freeSlot = s;
                            break;
                        }
                    }

                    if (freeSlot == null)
                    {
                        SetToast(L("Стол дилера полон (макс 8 предметов)", "Dealer table is full (max 8 items)"));
                        return;
                    }

                    ItemsHelper helper = null;
                    var helperField = typeof(DealerItemsManager).GetField("_itemsHelper", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (helperField != null) helper = helperField.GetValue(dim) as ItemsHelper;
                    if (helper == null) helper = new ItemsHelper();

                    ItemSO so = (helper != null) ? helper.GetItemSoByType(itype) : null;
                    if (so != null && so.Prefab != null)
                    {
                        Vector3 pos = freeSlot.SlotTransform.position + new Vector3(-so.DealerItemTransformData.PositionOffset.x, so.DealerItemTransformData.PositionOffset.y, so.DealerItemTransformData.PositionOffset.z);
                        Vector3 rotEuler = so.Prefab.transform.localEulerAngles;
                        Quaternion rot = Quaternion.Euler(rotEuler.x - so.DealerItemTransformData.RotationOffset.x, rotEuler.y - so.DealerItemTransformData.RotationOffset.y, rotEuler.z + so.DealerItemTransformData.RotationOffset.z);

                        Item newItem = UtilMesh.InstantiateWithMeshFix<Item>(so.Prefab, pos, rot, freeSlot.SlotTransform);
                        if (newItem != null)
                        {
                            newItem.Init(so);
                            freeSlot.IsOccupied = true;
                            freeSlot.Item = newItem;
                            if (freeSlot.itemGridButton != null)
                            {
                                freeSlot.itemGridButton.SetItem(newItem);
                                freeSlot.itemGridButton.Init();
                                freeSlot.itemGridButton.gameObject.SetActive(true);
                            }

                            var im = (ReferencesManager.Instance != null) ? ReferencesManager.Instance.InteractionManager : null;
                            if (im != null && freeSlot.itemGridButton != null)
                            {
                                im.InteractWithGridDealerItem(freeSlot.itemGridButton.GridIndex, newItem);
                            }

                            var dData = GetDealerDataSafe();
                            if (dData != null && dData.OwnedItems != null)
                            {
                                dData.OwnedItems.Add(newItem);
                            }

                            var _idSafe = GetItemsDataSafe();
                            if (_idSafe != null && _idSafe.CurrentItemAmounts != null)
                            {
                                foreach (var ia in _idSafe.CurrentItemAmounts)
                                {
                                    if (ia != null && ia.ItemType == itype)
                                    {
                                        ia.DealerAmount++;
                                        break;
                                    }
                                }
                            }

                            SetToast(L("Выдан дилеру: ", "Given to Dealer: ") + GetItemDisplayName(itype));
                            return;
                        }
                    }
                    SetToast(L("Не удалось создать предмет", "Failed to create item"));
                }
                else
                {
                    SetToast(L("Дилер еще не за столом", "Dealer not at table yet"));
                }
            }
            catch (Exception ex)
            {
                SetToast(L("Ошибка спавна дилеру: ", "Dealer spawn error: ") + ex.Message);
            }
        }

        public void UnlockDoubleOrNothing()
        {
            try
            {
                PermanentDoubleOrNothing = true;
                SaveConfig();

                try
                {
                    var fu = _2AwesomeStudio.TACoreUtils.FileUtils.TAFileUtils.Instance;
                    if (fu != null)
                    {
                        fu.SetBool("DoNEnabled", true, 0);
                        fu.SetInt("DoNCount", 0, 0);
                        fu.Save();
                    }
                }
                catch { }

                var sfm = SaveFileManager.Instance ?? UnityEngine.Object.FindObjectOfType<SaveFileManager>();
                if (sfm != null)
                {
                    sfm.UnlockDoubleOrNothing();
                    sfm.SaveGame();
                }

                try
                {
                    var rm = ReferencesManager.Instance;
                    var intro = (rm != null) ? rm.IntroManager : UnityEngine.Object.FindObjectOfType<IntroManager>();
                    if (intro != null)
                    {
                        intro.IsEndlessModeUnlocked = true;
                        intro.allowingPills = true;
                        var pillField = typeof(IntroManager).GetField("_pillBottle", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        if (pillField != null)
                        {
                            var pillObj = pillField.GetValue(intro) as GameObject;
                            if (pillObj != null && !pillObj.activeSelf)
                            {
                                pillObj.SetActive(true);
                            }
                        }
                    }
                }
                catch { }

                SetToast(L("Режим 'Всё или ничего' закреплен навсегда!", "Double or Nothing mode locked permanently!"));
            }
            catch (Exception ex)
            {
                SetToast("Ошибка разблокировки: " + ex.Message);
            }
        }

        public void AddMoney(int amount)
        {
            try
            {
                BonusMoney += amount;
                var rm = FindRoundManager();
                if (rm != null)
                {
                    int cur = 0;
                    if (_roundManagerScoreField != null)
                    {
                        try { cur = (int)_roundManagerScoreField.GetValue(rm); } catch { }
                    }
                    cur += amount;
                    ApplyScoreToGame(cur);
                    SetToast(string.Format("Добавлено +${0:N0}. Текущий счет: ${1:N0}", amount, cur));
                    return;
                }
                SetToast(string.Format("Бонусные деньги добавлены к раунду: +${0:N0}", amount));
            }
            catch (Exception ex)
            {
                SetToast("Ошибка начисления денег: " + ex.Message);
            }
        }

        public void SetMoney(int amount)
        {
            try
            {
                BonusMoney = 0;
                ApplyScoreToGame(amount);
                SetToast(string.Format("Счет установлен: ${0:N0}", amount));
            }
            catch (Exception ex)
            {
                SetToast("Ошибка установки денег: " + ex.Message);
            }
        }

        public void DoubleMoney()
        {
            try
            {
                var rm = FindRoundManager();
                int cur = 0;
                if (rm != null && _roundManagerScoreField != null)
                {
                    try { cur = (int)_roundManagerScoreField.GetValue(rm); } catch { }
                }
                if (cur <= 0) cur = 50000;
                int doubled = cur * 2;
                ApplyScoreToGame(doubled);
                SetToast(string.Format("Счет удвоен: ${0:N0}", doubled));
            }
            catch (Exception ex)
            {
                SetToast("Ошибка удвоения: " + ex.Message);
            }
        }

        private void ApplyScoreToGame(int score)
        {
            var rm = FindRoundManager();
            if (rm != null)
            {
                if (_roundManagerScoreField != null) _roundManagerScoreField.SetValue(rm, score);
                if (_roundManagerEndScoreField != null) _roundManagerEndScoreField.SetValue(rm, score);
            }

            var presenter = UnityEngine.Object.FindObjectOfType<DefibrillatorDoubleOrNothingScorePresenter>();
            if (presenter != null)
            {
                if (_scorePresenterStartScoreField != null) _scorePresenterStartScoreField.SetValue(presenter, score);
                if (_scorePresenterPrevScoreField != null) _scorePresenterPrevScoreField.SetValue(presenter, score);
                if (_scorePresenterScoreTextField != null)
                {
                    try
                    {
                        var textObj = _scorePresenterScoreTextField.GetValue(presenter);
                        if (textObj != null)
                        {
                            var textProp = textObj.GetType().GetProperty("text");
                            if (textProp != null)
                            {
                                textProp.SetValue(textObj, score.ToString(), null);
                            }
                        }
                    }
                    catch { }
                }
            }

            var pd = GetPlayerDataSafe();
            if (pd != null)
            {
                try
                {
                    pd.Stat_totalCash = score;
                    pd.UpdateGlobalStats(score);
                }
                catch { }
            }
        }
        #endregion

        #region GUI Rendering Helpers
        private static Texture2D MakeSolidTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
                pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private static void DrawSolidRect(Rect rect, Color color)
        {
            if (_whiteTex == null)
            {
                _whiteTex = MakeSolidTex(2, 2, Color.white);
            }
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, _whiteTex);
            GUI.color = old;
        }

        private static void DrawOutline(Rect rect, Color color, float thickness)
        {
            DrawSolidRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            DrawSolidRect(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), color);
            DrawSolidRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            DrawSolidRect(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), color);
        }

        private static Texture2D CreateCursorTexture()
        {
            int w = 16;
            int h = 20;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color[] cols = new Color[w * h];
            for (int i = 0; i < cols.Length; i++) cols[i] = Color.clear;

            string[] map = new string[]
            {
                "1...............",
                "11..............",
                "121.............",
                "1221............",
                "12221...........",
                "122221..........",
                "1222221.........",
                "12222221........",
                "122222221.......",
                "1222222221......",
                "12222211111.....",
                "1221221.........",
                "121..1221.......",
                "11...1221.......",
                "1.....1221......",
                "......1221......",
                ".......121......",
                ".......11.......",
                "................",
                "................"
            };

            for (int y = 0; y < h; y++)
            {
                string row = map[y];
                int texY = h - 1 - y;
                for (int x = 0; x < w; x++)
                {
                    char c = row[x];
                    if (c == '1') cols[texY * w + x] = new Color(0f, 0f, 0f, 1f);
                    else if (c == '2') cols[texY * w + x] = new Color(1f, 1f, 1f, 1f);
                }
            }

            tex.SetPixels(cols);
            tex.Apply();
            return tex;
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            if (_customCursorTex == null)
            {
                _customCursorTex = CreateCursorTexture();
            }

            _whiteTex = MakeSolidTex(2, 2, Color.white);
            _darkBgTex = MakeSolidTex(2, 2, new Color(0.08f, 0.09f, 0.11f, 0.98f));
            _cardBgTex = MakeSolidTex(2, 2, new Color(0.12f, 0.13f, 0.16f, 0.95f));
            _headerBgTex = MakeSolidTex(2, 2, new Color(0.10f, 0.11f, 0.14f, 1f));
            _btnNormalTex = MakeSolidTex(2, 2, new Color(0.16f, 0.17f, 0.22f, 1f));
            _btnHoverTex = MakeSolidTex(2, 2, new Color(0.22f, 0.24f, 0.30f, 1f));
            _btnActiveTex = MakeSolidTex(2, 2, new Color(0.06f, 0.65f, 0.45f, 1f));
            _btnDangerTex = MakeSolidTex(2, 2, new Color(0.70f, 0.18f, 0.18f, 1f));
            _tabActiveTex = MakeSolidTex(2, 2, new Color(0.08f, 0.55f, 0.85f, 1f));
            _liveShellTex = MakeSolidTex(2, 2, new Color(0.80f, 0.15f, 0.15f, 1f));
            _blankShellTex = MakeSolidTex(2, 2, new Color(0.15f, 0.40f, 0.80f, 1f));

            _windowStyle = new GUIStyle(GUI.skin.window);
            _windowStyle.normal.background = _darkBgTex;
            _windowStyle.onNormal.background = _darkBgTex;
            _windowStyle.padding = new RectOffset(16, 16, 12, 16);
            _windowStyle.border = new RectOffset(0, 0, 0, 0);

            _headerTitleStyle = new GUIStyle(GUI.skin.label);
            _headerTitleStyle.fontSize = 15;
            _headerTitleStyle.fontStyle = FontStyle.Bold;
            _headerTitleStyle.normal.textColor = new Color(0.2f, 0.85f, 1.0f);

            _headerSubStyle = new GUIStyle(GUI.skin.label);
            _headerSubStyle.fontSize = 11;
            _headerSubStyle.fontStyle = FontStyle.Normal;
            _headerSubStyle.normal.textColor = new Color(0.65f, 0.7f, 0.75f);

            _tabStyle = new GUIStyle(GUI.skin.button);
            _tabStyle.normal.background = _btnNormalTex;
            _tabStyle.hover.background = _btnHoverTex;
            _tabStyle.normal.textColor = new Color(0.8f, 0.85f, 0.9f);
            _tabStyle.fontSize = 11;
            _tabStyle.fontStyle = FontStyle.Bold;
            _tabStyle.margin = new RectOffset(2, 2, 2, 2);

            _activeTabStyle = new GUIStyle(_tabStyle);
            _activeTabStyle.normal.background = _tabActiveTex;
            _activeTabStyle.hover.background = _tabActiveTex;
            _activeTabStyle.normal.textColor = Color.white;

            _sectionHeaderStyle = new GUIStyle(GUI.skin.label);
            _sectionHeaderStyle.fontSize = 13;
            _sectionHeaderStyle.fontStyle = FontStyle.Bold;
            _sectionHeaderStyle.normal.textColor = new Color(0.95f, 0.75f, 0.25f);
            _sectionHeaderStyle.margin = new RectOffset(0, 0, 6, 4);

            _subHeaderStyle = new GUIStyle(GUI.skin.label);
            _subHeaderStyle.fontSize = 11;
            _subHeaderStyle.fontStyle = FontStyle.Bold;
            _subHeaderStyle.normal.textColor = new Color(0.4f, 0.8f, 1f);
            _subHeaderStyle.alignment = TextAnchor.MiddleCenter;

            _labelStyle = new GUIStyle(GUI.skin.label);
            _labelStyle.fontSize = 11;
            _labelStyle.fontStyle = FontStyle.Bold;
            _labelStyle.normal.textColor = Color.white;

            _subLabelStyle = new GUIStyle(GUI.skin.label);
            _subLabelStyle.fontSize = 10;
            _subLabelStyle.normal.textColor = new Color(0.6f, 0.65f, 0.7f);

            _valueStyle = new GUIStyle(GUI.skin.label);
            _valueStyle.fontSize = 11;
            _valueStyle.fontStyle = FontStyle.Bold;
            _valueStyle.normal.textColor = new Color(0.2f, 0.95f, 0.6f);

            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.normal.background = _btnNormalTex;
            _buttonStyle.hover.background = _btnHoverTex;
            _buttonStyle.normal.textColor = Color.white;
            _buttonStyle.fontSize = 11;
            _buttonStyle.fontStyle = FontStyle.Bold;

            _actionButtonStyle = new GUIStyle(_buttonStyle);
            _actionButtonStyle.normal.background = _btnActiveTex;
            _actionButtonStyle.normal.textColor = Color.white;

            _dangerButtonStyle = new GUIStyle(_buttonStyle);
            _dangerButtonStyle.normal.background = _btnDangerTex;
            _dangerButtonStyle.normal.textColor = new Color(1f, 0.85f, 0.85f);

            _toastStyle = new GUIStyle(GUI.skin.box);
            _toastStyle.normal.background = _headerBgTex;
            _toastStyle.normal.textColor = new Color(1f, 0.9f, 0.35f);
            _toastStyle.fontSize = 12;
            _toastStyle.fontStyle = FontStyle.Bold;
            _toastStyle.alignment = TextAnchor.MiddleCenter;

            _stylesInitialized = true;
        }

        private static bool IsKnownHotkeyId(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            for (int i = 0; i < HotkeyDefinitions.Length; i++)
            {
                if (HotkeyDefinitions[i].Id == id) return true;
            }
            return false;
        }

        private bool DrawToggleRow(string title, string desc, bool currentValue, string hotkeyIdOrBadge = "")
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label(title, _labelStyle);
            if (!string.IsNullOrEmpty(desc))
            {
                GUILayout.Label(desc, _subLabelStyle);
            }
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            if (!string.IsNullOrEmpty(hotkeyIdOrBadge))
            {
                if (IsKnownHotkeyId(hotkeyIdOrBadge))
                {
                    KeyCode curK = GetHotkeyById(hotkeyIdOrBadge);
                    bool isThisBinding = (_bindingActionId == hotkeyIdOrBadge);
                    string keyName = isThisBinding ? "..." : (curK == KeyCode.None ? "[-]" : curK.ToString());
                    GUIStyle bStyle = isThisBinding ? _actionButtonStyle : _buttonStyle;

                    if (GUILayout.Button(keyName, bStyle, GUILayout.Width(64), GUILayout.Height(24)))
                    {
                        _bindingActionId = isThisBinding ? null : hotkeyIdOrBadge;
                        if (_bindingActionId != null)
                        {
                            SetToast(L("Нажмите любую клавишу для бинда (или ESC/Delete для сброса)...", "Press any key to bind (or ESC/Delete to unbind)..."));
                        }
                    }
                }
                else
                {
                    GUILayout.Box(hotkeyIdOrBadge, _buttonStyle, GUILayout.Width(50), GUILayout.Height(24));
                }
                GUILayout.Space(6);
            }

            GUIStyle toggleBtnStyle = currentValue ? _actionButtonStyle : _buttonStyle;
            string toggleText = currentValue ? L("ВКЛ", "ON") : L("ВЫКЛ", "OFF");
            if (GUILayout.Button(toggleText, toggleBtnStyle, GUILayout.Width(75), GUILayout.Height(26)))
            {
                currentValue = !currentValue;
                SetToast(string.Format("{0}: {1}", title, toggleText));
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(4);
            return currentValue;
        }

        private void DrawHotkeyBindCell(string id, string label, float width)
        {
            GUILayout.BeginHorizontal(GUILayout.Width(width));
            GUILayout.Label(label, _labelStyle, GUILayout.Width(width - 80f));
            KeyCode curKey = GetHotkeyById(id);
            bool isBinding = (_bindingActionId == id);
            string keyText = isBinding ? "..." : (curKey == KeyCode.None ? "[-]" : curKey.ToString());
            GUIStyle bStyle = isBinding ? _actionButtonStyle : _buttonStyle;
            if (GUILayout.Button(keyText, bStyle, GUILayout.Width(74), GUILayout.Height(24)))
            {
                _bindingActionId = isBinding ? null : id;
                if (_bindingActionId != null)
                {
                    SetToast(L("Нажмите любую клавишу (ESC/Del для сброса)...", "Press any key (ESC/Del to unbind)..."));
                }
            }
            GUILayout.EndHorizontal();
        }
        #endregion

        private void DrawStandaloneFps()
        {
            float w = 90f;
            float h = 24f;
            Rect r = new Rect(Screen.width - w - 16f, 12f, w, h);
            DrawSolidRect(new Rect(r.x - 4f, r.y - 2f, r.width + 8f, r.height + 4f), new Color(0.08f, 0.10f, 0.14f, 0.85f));
            DrawOutline(new Rect(r.x - 4f, r.y - 2f, r.width + 8f, r.height + 4f), new Color(0.2f, 0.7f, 0.9f, 0.6f), 1f);

            GUIStyle fpsStyle = new GUIStyle(GUI.skin.label);
            fpsStyle.fontSize = 11;
            fpsStyle.fontStyle = FontStyle.Bold;
            fpsStyle.alignment = TextAnchor.MiddleCenter;
            fpsStyle.normal.textColor = _currentFps >= 50f ? new Color(0.2f, 0.95f, 0.4f) : (_currentFps >= 30f ? new Color(1f, 0.85f, 0.2f) : new Color(0.95f, 0.25f, 0.25f));
            GUI.Label(r, string.Format("{0:F0} FPS", _currentFps), fpsStyle);
        }

        #region OnGUI Master Handler
        private void OnGUI()
        {
            InitStyles();

            // 1. Draw Standalone FPS Counter ONLY if enabled AND HUD overlay is NOT currently active
            if (ShowFps && (!_isMenuOpen && !ShowOverlay))
            {
                DrawStandaloneFps();
            }

            // 2. Draw HUD Overlay if menu is closed and ShowOverlay is enabled
            if (!_isMenuOpen && ShowOverlay)
            {
                DrawHUDOverlay();
            }

            // 3. Draw In-Game Menu Window if opened
            if (_isMenuOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                // Fullscreen background dim
                DrawSolidRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.55f));

                // Process window dragging in screen coordinates
                Event evt = Event.current;
                if (evt != null)
                {
                    Rect dragBarScreen = new Rect(_windowRect.x, _windowRect.y, Mathf.Max(100f, _windowRect.width - 250f), 45f);
                    if (evt.type == EventType.MouseDown && evt.button == 0 && dragBarScreen.Contains(evt.mousePosition))
                    {
                        _isDragging = true;
                        _dragOffset = evt.mousePosition - new Vector2(_windowRect.x, _windowRect.y);
                        evt.Use();
                    }
                    else if (evt.type == EventType.MouseUp && evt.button == 0)
                    {
                        _isDragging = false;
                    }
                    else if (_isDragging && (evt.type == EventType.MouseDrag || evt.type == EventType.Repaint))
                    {
                        Vector2 mp = evt.mousePosition;
                        _windowRect.x = Mathf.Clamp(mp.x - _dragOffset.x, 0f, Mathf.Max(0f, Screen.width - _windowRect.width));
                        _windowRect.y = Mathf.Clamp(mp.y - _dragOffset.y, 0f, Mathf.Max(0f, Screen.height - _windowRect.height));
                    }
                }

                _windowRect = GUI.Window(8888, _windowRect, DrawTrainerWindow, "", _windowStyle);

                // Custom software cursor (rendered on top of all GUI windows)
                if (_customCursorTex != null)
                {
                    GUI.depth = -99999;
                    Vector2 mPos = Event.current != null ? Event.current.mousePosition : new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                    GUI.DrawTexture(new Rect(mPos.x, mPos.y, 16, 20), _customCursorTex);
                }
            }

            // 4. Draw Toast Notifications (always on top)
            DrawToastNotification();
        }
        #endregion

        #region In-Game Menu Window
        private void DrawTrainerWindow(int windowId)
        {
            // Title bar dragging zone (leaves the right-side buttons clickable)
            GUI.DragWindow(new Rect(0, 0, Mathf.Max(100f, _windowRect.width - 250f), 45f));

            // Title bar background
            DrawSolidRect(new Rect(0, 0, _windowRect.width, 42), new Color(0.10f, 0.11f, 0.14f, 1f));
            DrawOutline(new Rect(0, 0, _windowRect.width, _windowRect.height), new Color(0.2f, 0.7f, 0.9f, 0.6f), 1.5f);

            // Top Header: Title, Author, Links, Close Button
            GUILayout.BeginHorizontal();
            GUILayout.Space(4);
            GUILayout.BeginVertical();
            GUILayout.Label("CoreTune " + ModVersion, _headerTitleStyle);
            GUILayout.Label(string.Format(L("Buckshot Roulette | Автор: {0} | Меню: [{1}] или [ESC]", "Buckshot Roulette | Author: {0} | Menu: [{1}] or [ESC]"), AuthorName, ToggleKey), _headerSubStyle);
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            // Boosty Link Button
            if (GUILayout.Button("Boosty", _buttonStyle, GUILayout.Width(80), GUILayout.Height(26)))
            {
                OpenBrowserUrl(BoostyUrl);
            }

            GUILayout.Space(4);

            // Nexus Mods Link Button
            if (GUILayout.Button("Nexus Mods", _buttonStyle, GUILayout.Width(100), GUILayout.Height(26)))
            {
                OpenBrowserUrl(NexusModsUrl);
            }

            GUILayout.Space(6);

            // Close [X] Button
            if (GUILayout.Button("X", _dangerButtonStyle, GUILayout.Width(30), GUILayout.Height(26)))
            {
                ToggleMenu();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            // Navigation Tabs Bar
            string[] tabNames = new string[] {
                L("ESP & Патроны", "ESP & Shells"),
                L("Игрок & HP", "Player & HP"),
                L("Дилер & ИИ", "Dealer & AI"),
                L("Спавнер Вещей", "Item Spawner"),
                L("Деньги & Счет", "Money & Score"),
                L("Настройки & HUD", "Settings & HUD")
            };

            GUILayout.BeginHorizontal();
            for (int i = 0; i < tabNames.Length; i++)
            {
                GUIStyle tStyle = (i == _activeTab) ? _activeTabStyle : _tabStyle;
                if (GUILayout.Button(tabNames[i], tStyle, GUILayout.Height(30)))
                {
                    _activeTab = i;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            // Tab Content Box
            _tabScrollPos = GUILayout.BeginScrollView(_tabScrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);

            switch (_activeTab)
            {
                case 0:
                    DrawEspTab();
                    break;
                case 1:
                    DrawPlayerTab();
                    break;
                case 2:
                    DrawDealerTab();
                    break;
                case 3:
                    DrawItemsTab();
                    break;
                case 4:
                    DrawMoneyTab();
                    break;
                case 5:
                    DrawSettingsTab();
                    break;
            }

            GUILayout.EndScrollView();

            GUILayout.Space(4);

            // Bottom Footer
            GUILayout.BeginHorizontal();
            string footerHotkeys = string.Format(
                L("Горячие клавиши: {0}=GodMode | {1}=Smart | {2}=InfItems | {3}=LuckyMed | {4}=Heal | {5}=SkipTurn",
                  "Hotkeys: {0}=GodMode | {1}=Smart | {2}=InfItems | {3}=LuckyMed | {4}=Heal | {5}=SkipTurn"),
                KeyGodMode, KeySmartRoulette, KeyInfiniteItems, KeyLuckyMedicine, KeyFullHeal, KeySkipDealerTurn);
            GUILayout.Label(footerHotkeys, _subLabelStyle);
            GUILayout.FlexibleSpace();
            if (ShowFps)
            {
                GUILayout.Label(string.Format("{0:F0} FPS", _currentFps), _valueStyle);
            }
            GUILayout.EndHorizontal();
        }

        #region Tab 1: ESP & Shotgun
        private void DrawEspTab()
        {
            var sd = GetShotgunDataSafe();

            // Shell ESP Panel
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(L("РЕАЛЬНОЕ СОСТОЯНИЕ ДРОБОВИКА И ПАТРОНОВ (X-RAY)", "SHOTGUN & CHAMBER STATE (X-RAY)"), _sectionHeaderStyle);

            if (sd != null && sd.ShellsList != null && sd.ShellsList.Count > 0)
            {
                int liveCount = 0;
                int blankCount = 0;
                for (int i = 0; i < sd.ShellsList.Count; i++)
                {
                    if (sd.ShellsList[i] != null)
                    {
                        if (sd.ShellsList[i].ShellType == ShellType.Live) liveCount++;
                        else blankCount++;
                    }
                }

                GUILayout.Label(string.Format(L("В МАГАЗИНЕ: {0} ПАТРОНОВ ( {1} БОЕВЫХ  /  {2} ХОЛОСТЫХ )", "MAGAZINE: {0} SHELLS ( {1} LIVE  /  {2} BLANK )"), sd.ShellsList.Count, liveCount, blankCount), _valueStyle);
                GUILayout.Label(string.Format(L("Текущий урон выстрела: {0} HP {1}", "Current Shot Damage: {0} HP {1}"), sd.ShotgunDamage, (sd.ShotgunDamage > 1 ? L("(СТВОЛ СПИЛЕН 2X)", "(SAWED OFF 2X)") : "")), _labelStyle);

                GUILayout.Space(6);

                // Horizontal Shell Queue Display
                GUILayout.BeginHorizontal();
                for (int i = 0; i < sd.ShellsList.Count; i++)
                {
                    var sh = sd.ShellsList[i];
                    if (sh == null) continue;

                    bool isLive = (sh.ShellType == ShellType.Live);
                    Color boxCol = isLive ? new Color(0.75f, 0.15f, 0.15f, 0.9f) : new Color(0.15f, 0.40f, 0.80f, 0.9f);
                    Color borderCol = (i == 0) ? new Color(1f, 0.9f, 0.2f, 1f) : Color.white;

                    Rect shellRect = GUILayoutUtility.GetRect(84, 52);
                    DrawSolidRect(shellRect, boxCol);
                    DrawOutline(shellRect, borderCol, (i == 0) ? 2f : 1f);

                    GUIStyle shNumStyle = new GUIStyle(GUI.skin.label);
                    shNumStyle.alignment = TextAnchor.MiddleCenter;
                    shNumStyle.fontSize = 11;
                    shNumStyle.fontStyle = FontStyle.Bold;
                    shNumStyle.normal.textColor = Color.white;

                    GUIStyle shSubStyle = new GUIStyle(GUI.skin.label);
                    shSubStyle.alignment = TextAnchor.MiddleCenter;
                    shSubStyle.fontSize = 9;
                    shSubStyle.fontStyle = FontStyle.Bold;
                    shSubStyle.normal.textColor = (i == 0) ? new Color(1f, 0.9f, 0.2f) : new Color(0.9f, 0.9f, 0.9f);

                    GUI.Label(new Rect(shellRect.x, shellRect.y + 4, shellRect.width, 20), string.Format("#{0} {1}", i + 1, isLive ? L("БОЕВОЙ", "LIVE") : L("ХОЛОСТОЙ", "BLANK")), shNumStyle);
                    GUI.Label(new Rect(shellRect.x, shellRect.y + 26, shellRect.width, 20), (i == 0) ? L("[В СТВОЛЕ]", "[IN CHAMBER]") : string.Format(L("Очередь {0}", "Queue {0}"), i + 1), shSubStyle);

                    GUILayout.Space(6);
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label(L("Дробовик разряжен или раунд еще не начат", "Shotgun is empty or round not started"), _subLabelStyle);
            }
            GUILayout.EndVertical();

            GUILayout.Space(8);

            // Shell & Roulette Toggles
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(L("АВТОМАТИКА И МАНИПУЛЯЦИЯ РУЛЕТКОЙ", "ROULETTE AUTOMATION & CONTROL"), _sectionHeaderStyle);

            SmartRoulette = DrawToggleRow(L("Умная рулетка", "Smart Roulette"), L("Боевой патрон при выстреле в дилера, холостой при выстреле в себя", "Live round at dealer, blank round at self"), SmartRoulette, "SmartRoulette");
            AlwaysLive = DrawToggleRow(L("Всегда боевые", "Always Live"), L("Каждый патрон в стволе принудительно становится боевым", "Forces every shell in the gun to be a live round"), AlwaysLive, "AlwaysLive");
            if (AlwaysLive) AlwaysBlank = false;

            AlwaysBlank = DrawToggleRow(L("Всегда холостые", "Always Blank"), L("Каждый патрон в стволе принудительно становится холостым", "Forces every shell in the gun to be a blank"), AlwaysBlank, "AlwaysBlank");
            if (AlwaysBlank) AlwaysLive = false;
            GUILayout.EndVertical();

            GUILayout.Space(8);

            // Shotgun Quick Actions
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(L("БЫСТРЫЕ ДЕЙСТВИЯ С ДРОБОВИКОМ", "SHOTGUN QUICK ACTIONS"), _sectionHeaderStyle);

            GUILayout.BeginHorizontal();
            string invertKey = KeyInvertShell != KeyCode.None ? " [" + KeyInvertShell + "]" : "";
            string sawKey = KeySawShotgun != KeyCode.None ? " [" + KeySawShotgun + "]" : "";
            if (GUILayout.Button(L("Перевернуть патрон в стволе" + invertKey, "Invert Shell in Chamber" + invertKey), _buttonStyle, GUILayout.Height(30)))
            {
                InvertCurrentShell();
            }
            if (GUILayout.Button(L("Спилить ствол дробовика (2x)" + sawKey, "Saw Off Shotgun Barrel (2x)" + sawKey), _buttonStyle, GUILayout.Height(30)))
            {
                SawShotgun();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            GUILayout.Label(L("Установить прямой урон:", "Set Direct Damage:"), _labelStyle, GUILayout.Width(180));
            if (GUILayout.Button(L("1 HP (Обычный)", "1 HP (Normal)"), _buttonStyle, GUILayout.Height(26))) SetShotgunDamage(1);
            if (GUILayout.Button(L("2 HP (Спиленный)", "2 HP (Sawed)"), _buttonStyle, GUILayout.Height(26))) SetShotgunDamage(2);
            if (GUILayout.Button(L("4 HP (Смертоносный)", "4 HP (Lethal)"), _actionButtonStyle, GUILayout.Height(26))) SetShotgunDamage(4);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
        #endregion

        #region Tab 2: Player & Health
        private void DrawPlayerTab()
        {
            var rd = GetRoundDataSafe();

            // Player Status Card
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(L("СОСТОЯНИЕ ИГРОКА И ДЕФИБРИЛЛЯТОРА", "PLAYER & DEFIBRILLATOR STATUS"), _sectionHeaderStyle);
            int curHp = (rd != null) ? rd.PlayerHealth : 0;
            bool isWireCut = (rd != null) ? rd.IsPlayerWireCut : false;
            bool isCuffed = (rd != null) ? rd.PlayerHandcuffed : false;

            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format(L("Здоровье игрока: {0} / 6 HP", "Player Health: {0} / 6 HP"), curHp), _valueStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label(string.Format(L("Провода дефибриллятора: {0}", "Defibrillator Wires: {0}"), isWireCut ? L("ПЕРЕРЕЗАНЫ", "CUT") : L("ЦЕЛЫ", "INTACT")), isWireCut ? _subLabelStyle : _valueStyle);
            GUILayout.Space(12);
            GUILayout.Label(string.Format(L("Наручники: {0}", "Handcuffs: {0}"), isCuffed ? L("НАДЕТЫ", "CUFFED") : L("СВОБОДЕН", "FREE")), isCuffed ? _subLabelStyle : _valueStyle);
            GUILayout.EndHorizontal();

            // HP Visual Blocks
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            for (int i = 0; i < 6; i++)
            {
                Color pipCol = (i < curHp) ? new Color(0.1f, 0.8f, 0.4f, 1f) : new Color(0.2f, 0.22f, 0.26f, 1f);
                Rect pipRect = GUILayoutUtility.GetRect(40, 16);
                DrawSolidRect(pipRect, pipCol);
                DrawOutline(pipRect, Color.black, 1f);
                GUILayout.Space(4);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(8);

            // God Mode & Immunity Toggles
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(L("РЕЖИМ БОГА И ЗАЩИТА", "GOD MODE & DEFENSE"), _sectionHeaderStyle);

            GodMode = DrawToggleRow(L("Режим бога", "God Mode"), L("Полное бессмертие: здоровье не падает, иммунитет к выстрелам", "Complete invulnerability: health never drops, immune to shots"), GodMode, "GodMode");
            OneHitKill = DrawToggleRow(L("Убийство с одного выстрела", "One-Hit Kill"), L("Любой выстрел по дилеру наносит смертельный урон", "Any shot to dealer deals lethal damage"), OneHitKill, "OneHitKill");
            LuckyMedicine = DrawToggleRow(L("Удачливая просрочка", "Lucky Medicine"), L("Просроченные таблетки всегда восстанавливают +2 HP со 100% шансом", "Expired medicine always restores +2 HP with 100% chance"), LuckyMedicine, "LuckyMedicine");
            AntiWireCut = DrawToggleRow(L("Защита от перерезания проводов", "Anti-Wire Cut"), L("Дилер никогда не сможет перерезать провода вашего дефибриллятора", "Dealer can never cut your defibrillator wires"), AntiWireCut, "AntiWireCut");
            AntiCuffs = DrawToggleRow(L("Иммунитет к наручникам", "Anti-Cuffs"), L("Игрок мгновенно освобождается от любых наручников", "Player instantly breaks free from handcuffs"), AntiCuffs, "AntiCuffs");
            GUILayout.EndVertical();

            GUILayout.Space(8);

            // Health Management Actions
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(L("УПРАВЛЕНИЕ ЗДОРОВЬЕМ", "HEALTH MANAGEMENT"), _sectionHeaderStyle);

            string healKey = KeyFullHeal != KeyCode.None ? " [" + KeyFullHeal + "]" : "";
            if (GUILayout.Button(L("Полное восстановление здоровья (6 HP)" + healKey, "Full Health Restore (6 HP)" + healKey), _actionButtonStyle, GUILayout.Height(30)))
            {
                FullHeal();
            }

            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            GUILayout.Label(L("Задать точное HP:", "Set Exact HP:"), _labelStyle, GUILayout.Width(150));
            if (GUILayout.Button("1 HP", _buttonStyle, GUILayout.Height(26))) SetPlayerHealth(1);
            if (GUILayout.Button("2 HP", _buttonStyle, GUILayout.Height(26))) SetPlayerHealth(2);
            if (GUILayout.Button("4 HP", _buttonStyle, GUILayout.Height(26))) SetPlayerHealth(4);
            if (GUILayout.Button("6 HP", _buttonStyle, GUILayout.Height(26))) SetPlayerHealth(6);
            if (GUILayout.Button(L("Самоубийство (0 HP)", "Suicide (0 HP)"), _dangerButtonStyle, GUILayout.Height(26))) SetPlayerHealth(0);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
        #endregion

        #region Tab 3: Dealer Control
        private void DrawDealerTab()
        {
            var rd = GetRoundDataSafe();

            // Dealer Status Card
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(L("СОСТОЯНИЕ ДИЛЕРА", "DEALER STATUS"), _sectionHeaderStyle);
            int dHp = (rd != null) ? rd.DealerHealth : 0;
            bool dCuffed = (rd != null) ? rd.DealerHandcuffed : false;

            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format(L("Здоровье дилера: {0} HP", "Dealer Health: {0} HP"), dHp), _valueStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label(string.Format(L("Наручники: {0}", "Handcuffs: {0}"), dCuffed ? L("НАДЕТЫ", "CUFFED") : L("СВОБОДЕН", "FREE")), dCuffed ? _valueStyle : _subLabelStyle);
            GUILayout.EndHorizontal();

            // Dealer HP Visual Blocks
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            for (int i = 0; i < 6; i++)
            {
                Color pipCol = (i < dHp) ? new Color(0.85f, 0.2f, 0.2f, 1f) : new Color(0.2f, 0.22f, 0.26f, 1f);
                Rect pipRect = GUILayoutUtility.GetRect(40, 16);
                DrawSolidRect(pipRect, pipCol);
                DrawOutline(pipRect, Color.black, 1f);
                GUILayout.Space(4);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(8);

            // Dealer AI Toggles
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(L("КОНТРОЛЬ И МАНИПУЛЯЦИЯ ИИ ДИЛЕРА", "DEALER AI CONTROL & MANIPULATION"), _sectionHeaderStyle);

            ForceDealerSelfHarm = DrawToggleRow(L("Принудительное самоубийство дилера", "Force Dealer Self-Harm"), L("Дилер гарантированно стреляет исключительно в самого себя", "Dealer always shoots himself regardless of shell or situation"), ForceDealerSelfHarm, "ForceDealerSelfHarm");
            PermanentDealerCuffs = DrawToggleRow(L("Вечные наручники на дилере", "Permanent Dealer Handcuffs"), L("Дилер никогда не сможет освободиться из наручников", "Dealer can never break out of handcuffs"), PermanentDealerCuffs);
            GUILayout.EndVertical();

            GUILayout.Space(8);

            // Dealer Actions
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(L("ДЕЙСТВИЯ НАД ДИЛЕРОМ", "DEALER ACTIONS"), _sectionHeaderStyle);

            GUILayout.BeginHorizontal();
            string skipKey = KeySkipDealerTurn != KeyCode.None ? " [" + KeySkipDealerTurn + "]" : "";
            if (GUILayout.Button(L("Пропустить ход дилера" + skipKey, "Skip Dealer Turn" + skipKey), _actionButtonStyle, GUILayout.Height(30)))
            {
                SkipDealerTurn();
            }
            if (GUILayout.Button(L("Очистить предметы дилера со стола", "Clear Dealer Items from Table"), _buttonStyle, GUILayout.Height(30)))
            {
                ClearDealerItems();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            GUILayout.Label(L("Задать HP дилеру:", "Set Dealer HP:"), _labelStyle, GUILayout.Width(150));
            if (GUILayout.Button("1 HP", _buttonStyle, GUILayout.Height(26))) SetDealerHealth(1);
            if (GUILayout.Button("2 HP", _buttonStyle, GUILayout.Height(26))) SetDealerHealth(2);
            if (GUILayout.Button("4 HP", _buttonStyle, GUILayout.Height(26))) SetDealerHealth(4);
            if (GUILayout.Button(L("Мгновенно убить дилера (0 HP)", "Kill Dealer Instantly (0 HP)"), _dangerButtonStyle, GUILayout.Height(26))) SetDealerHealth(0);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
        #endregion

        #region Tab 4: Items & Spawner
        private void DrawItemsTab()
        {
            // Item Cheats Toggles
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(L("ЧИТЫ ДЛЯ ПРЕДМЕТОВ", "ITEM CHEATS"), _sectionHeaderStyle);

            InfiniteItems = DrawToggleRow(L("Бесконечные предметы", "Infinite Items"), L("Использованный предмет не пропадает из ячейки стола", "Items are not consumed upon usage"), InfiniteItems, "InfiniteItems");
            GUILayout.EndVertical();

            GUILayout.Space(8);

            // Item Spawner Grid: 2 Columns (Player and Dealer) with 3x3 grids + full-width clear buttons
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(L("СПАВНЕР ПРЕДМЕТОВ НА СТОЛ", "ITEM SPAWNER"), _sectionHeaderStyle);

            float totalW = _windowRect.width - 95f;
            float colW = (totalW - 16f) * 0.5f;
            float btnW = (colW - 24f) / 3f;

            string[,] itemsList = new string[,] {
                { "CIGARETTE", "Сигареты", "Cigarettes" },
                { "BEER", "Пиво", "Beer" },
                { "HANDCUFFS", "Наручники", "Handcuffs" },
                { "HANDSAW", "Ножовка", "Handsaw" },
                { "MAGNIFIER", "Лупа", "Magnifier" },
                { "INVERTER", "Инвертор", "Inverter" },
                { "ADRENALINE", "Адреналин", "Adrenaline" },
                { "PHONE", "Телефон", "Phone" },
                { "MEDICINE", "Таблетки", "Medicine" }
            };

            GUILayout.BeginHorizontal();

            // --- Left Column: Player (3x3 Grid + Clear) ---
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(colW));
            GUILayout.Label(L("ПРЕДМЕТЫ ДЛЯ ИГРОКА", "PLAYER ITEMS"), _subHeaderStyle);
            GUILayout.Space(6);

            for (int row = 0; row < 3; row++)
            {
                GUILayout.BeginHorizontal();
                for (int col = 0; col < 3; col++)
                {
                    int idx = row * 3 + col;
                    string id = itemsList[idx, 0];
                    string name = L(itemsList[idx, 1], itemsList[idx, 2]);

                    if (GUILayout.Button(name, _actionButtonStyle, GUILayout.Width(btnW), GUILayout.Height(30)))
                    {
                        GiveItemToTable(id);
                    }
                    if (col < 2) GUILayout.Space(4);
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(4);
            }

            GUILayout.Space(4);
            if (GUILayout.Button(L("Очистить стол игрока", "Clear Player Table"), _dangerButtonStyle, GUILayout.Height(28)))
            {
                ClearPlayerItems();
            }
            GUILayout.EndVertical();

            GUILayout.Space(8);

            // --- Right Column: Dealer (3x3 Grid + Clear) ---
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(colW));
            GUILayout.Label(L("ПРЕДМЕТЫ ДЛЯ ДИЛЕРА", "DEALER ITEMS"), _subHeaderStyle);
            GUILayout.Space(6);

            for (int row = 0; row < 3; row++)
            {
                GUILayout.BeginHorizontal();
                for (int col = 0; col < 3; col++)
                {
                    int idx = row * 3 + col;
                    string id = itemsList[idx, 0];
                    string name = L(itemsList[idx, 1], itemsList[idx, 2]);

                    if (GUILayout.Button(name, _buttonStyle, GUILayout.Width(btnW), GUILayout.Height(30)))
                    {
                        SpawnDealerItem(id);
                    }
                    if (col < 2) GUILayout.Space(4);
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(4);
            }

            GUILayout.Space(4);
            if (GUILayout.Button(L("Очистить стол дилера", "Clear Dealer Table"), _dangerButtonStyle, GUILayout.Height(28)))
            {
                ClearDealerItems();
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
        #endregion

        #region Tab 5: Money & Score
        private void DrawMoneyTab()
        {
            var rm = FindRoundManager();
            int curScore = 0;
            if (rm != null && _roundManagerScoreField != null)
            {
                try { curScore = (int)_roundManagerScoreField.GetValue(rm); } catch { }
            }

            // Money Status Card
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(L("РЕЖИМ 'ВСЁ ИЛИ НИЧЕГО'", "DOUBLE OR NOTHING MODE"), _sectionHeaderStyle);
            GUILayout.Label(string.Format(L("Текущий выигрыш / Счет в раунде: ${0:N0}", "Current Score / Round Earnings: ${0:N0}"), curScore), _valueStyle);
            GUILayout.Space(4);

            bool newPermDoN = DrawToggleRow(
                L("Режим 'Всё или ничего' навсегда", "Permanent Double or Nothing Mode"),
                L("Автоматически сохраняет и активирует бесконечный режим при каждом старте и перезаходе", "Always keeps endless mode unlocked across all restarts and rounds"),
                PermanentDoubleOrNothing);
            if (newPermDoN != PermanentDoubleOrNothing)
            {
                PermanentDoubleOrNothing = newPermDoN;
                SaveConfig();
                if (PermanentDoubleOrNothing) UnlockDoubleOrNothing();
            }

            GUILayout.Space(4);
            if (GUILayout.Button(L("Принудительно разблокировать прямо сейчас", "Force Unlock Right Now"), _actionButtonStyle, GUILayout.Height(30)))
            {
                UnlockDoubleOrNothing();
            }
            GUILayout.EndVertical();

            GUILayout.Space(8);

            // Money Actions
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(L("ДОБАВЛЕНИЕ И МОДИФИКАЦИЯ ДЕНЕГ", "MONEY & SCORE MODIFICATIONS"), _sectionHeaderStyle);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+$1,000", _buttonStyle, GUILayout.Height(30))) AddMoney(1000);
            string addMoneyKey = KeyAddMoney != KeyCode.None ? " [" + KeyAddMoney + "]" : "";
            if (GUILayout.Button("+$10,000" + addMoneyKey, _buttonStyle, GUILayout.Height(30))) AddMoney(10000);
            if (GUILayout.Button("+$50,000", _buttonStyle, GUILayout.Height(30))) AddMoney(50000);
            if (GUILayout.Button("+$100,000", _buttonStyle, GUILayout.Height(30))) AddMoney(100000);
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(L("2X УДВОИТЬ ТЕКУЩИЙ ВЫИГРЫШ", "2X DOUBLE CURRENT EARNINGS"), _actionButtonStyle, GUILayout.Height(32)))
            {
                DoubleMoney();
            }
            if (GUILayout.Button(L("УСТАНОВИТЬ $1,000,000", "SET $1,000,000"), _actionButtonStyle, GUILayout.Height(32)))
            {
                SetMoney(1000000);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
        #endregion

        #region Tab 6: Settings & HUD
        private void DrawSettingsTab()
        {
            // Language & Controls
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(L("ЯЗЫК И УПРАВЛЕНИЕ", "LANGUAGE & CONTROLS"), _sectionHeaderStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label(L("Язык интерфейса:", "Interface Language:"), _labelStyle, GUILayout.Width(180));
            if (GUILayout.Button("Русский", Language == 0 ? _actionButtonStyle : _buttonStyle, GUILayout.Width(100), GUILayout.Height(26)))
            {
                Language = 0;
                SaveConfig();
            }
            if (GUILayout.Button("English", Language == 1 ? _actionButtonStyle : _buttonStyle, GUILayout.Width(100), GUILayout.Height(26)))
            {
                Language = 1;
                SaveConfig();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUILayout.Label(L("Кнопка открытия меню:", "Menu Toggle Key:"), _labelStyle, GUILayout.Width(180));
            string keyLabel = _isBindingToggleKey ? L("[ Нажмите любую клавишу... ]", "[ Press any key... ]") : string.Format("[ {0} ]", ToggleKey);
            GUIStyle kStyle = _isBindingToggleKey ? _actionButtonStyle : _buttonStyle;
            if (GUILayout.Button(keyLabel, kStyle, GUILayout.Width(180), GUILayout.Height(26)))
            {
                _isBindingToggleKey = !_isBindingToggleKey;
                _bindingActionId = null;
                if (_isBindingToggleKey)
                {
                    SetToast(L("Нажмите любую клавишу на клавиатуре (ESC для отмены)", "Press any key on keyboard (ESC to cancel)"));
                }
            }
            if (GUILayout.Button("INSERT", ToggleKey == KeyCode.Insert ? _actionButtonStyle : _buttonStyle, GUILayout.Width(65), GUILayout.Height(26)))
            {
                ToggleKey = KeyCode.Insert;
                _isBindingToggleKey = false;
                UnbindCheatIfKeyMatches(KeyCode.Insert);
                SaveConfig();
                SetToast(L("Кнопка меню: [INSERT]", "Menu toggle key: [INSERT]"));
            }
            if (GUILayout.Button("HOME", ToggleKey == KeyCode.Home ? _actionButtonStyle : _buttonStyle, GUILayout.Width(55), GUILayout.Height(26)))
            {
                ToggleKey = KeyCode.Home;
                _isBindingToggleKey = false;
                UnbindCheatIfKeyMatches(KeyCode.Home);
                SaveConfig();
                SetToast(L("Кнопка меню: [HOME]", "Menu toggle key: [HOME]"));
            }
            if (GUILayout.Button("F1", ToggleKey == KeyCode.F1 ? _actionButtonStyle : _buttonStyle, GUILayout.Width(45), GUILayout.Height(26)))
            {
                ToggleKey = KeyCode.F1;
                _isBindingToggleKey = false;
                UnbindCheatIfKeyMatches(KeyCode.F1);
                SaveConfig();
                SetToast(L("Кнопка меню: [F1]", "Menu toggle key: [F1]"));
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(8);

            // Speed & Time Control
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(L("СКОРОСТЬ И ВРЕМЯ ИГРЫ", "GAME SPEED & TIME"), _sectionHeaderStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format(L("Скорость игры: {0:F1}x", "Game Speed: {0:F1}x"), GameSpeed), _labelStyle, GUILayout.Width(180));
            GameSpeed = GUILayout.HorizontalSlider(GameSpeed, 0.2f, 3.0f);
            if (GUILayout.Button(L("1.0x (Норма)", "1.0x (Normal)"), _buttonStyle, GUILayout.Width(110), GUILayout.Height(24)))
            {
                GameSpeed = 1.0f;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            FreezeTime = DrawToggleRow(L("Остановка времени", "Freeze Time"), L("Полностью приостанавливает игровой процесс на паузу", "Completely freezes gameplay"), FreezeTime, "FreezeTime");
            FastBoot = DrawToggleRow(L("Быстрый запуск игры", "Fast Boot & Instant Start"), L("Пропуск заставки SplashScene и моментальный запуск без 5-секундного затемнения экрана", "Skips splash scene and removes the 5-second fade delay on Start Game"), FastBoot);

            GUILayout.EndVertical();

            GUILayout.Space(8);

            // Hotkeys Rebinding Section
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(L("НАЗНАЧЕНИЕ ГОРЯЧИХ КЛАВИШ", "HOTKEY ASSIGNMENTS"), _sectionHeaderStyle);
            GUILayout.Label(L("Кликните на кнопку с клавишей для переназначения. ESC или Delete очищает бинд.", "Click a hotkey button to rebind. ESC or Delete clears the binding."), _subLabelStyle);
            GUILayout.Space(6);

            float hkTotalW = _windowRect.width - 95f;
            float hkColW = (hkTotalW - 16f) * 0.5f;

            for (int i = 0; i < HotkeyDefinitions.Length; i += 2)
            {
                GUILayout.BeginHorizontal();
                DrawHotkeyBindCell(HotkeyDefinitions[i].Id, L(HotkeyDefinitions[i].Ru, HotkeyDefinitions[i].En), hkColW);
                GUILayout.Space(8);
                if (i + 1 < HotkeyDefinitions.Length)
                {
                    DrawHotkeyBindCell(HotkeyDefinitions[i + 1].Id, L(HotkeyDefinitions[i + 1].Ru, HotkeyDefinitions[i + 1].En), hkColW);
                }
                else
                {
                    GUILayout.Space(hkColW);
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(2);
            }

            GUILayout.Space(6);
            if (GUILayout.Button(L("Сбросить все горячие клавиши по умолчанию", "Reset All Hotkeys to Default"), _buttonStyle, GUILayout.Height(28)))
            {
                ResetHotkeysToDefault();
                SaveConfig();
                SetToast(L("Все горячие клавиши сброшены по умолчанию", "All hotkeys reset to default"));
            }
            GUILayout.EndVertical();

            GUILayout.Space(8);

            // In-Game HUD Settings
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(L("ВНУТРИИГРОВОЙ ОВЕРЛЕЙ (HUD ВО ВРЕМЯ ИГРЫ)", "IN-GAME HUD OVERLAY"), _sectionHeaderStyle);

            ShowOverlay = DrawToggleRow(L("Показывать оверлей во время игры", "Show In-Game HUD Overlay"), L("Отображает информацию о патронах и здоровье при закрытом меню", "Displays shells and health when trainer is closed"), ShowOverlay);
            ShowFps = DrawToggleRow(L("Счетчик кадров", "FPS Counter"), L("Отображает текущий FPS", "Displays current FPS"), ShowFps);
            ShowShellInfo = DrawToggleRow(L("Информация о патронах", "Shells & Chamber Info"), L("Отображает патрон в патроннике и количество пуль в магазине", "Displays chambered shell and magazine count"), ShowShellInfo);
            ShowActiveCheats = DrawToggleRow(L("Список активных читов", "Active Cheats List"), L("Отображает включенные функции на экране", "Displays enabled trainer cheats on screen"), ShowActiveCheats);

            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUILayout.Label(L("Положение HUD на экране:", "HUD Screen Position:"), _labelStyle, GUILayout.Width(180));
            string[] posNames = (Language == 1)
                ? new string[] { "Top-Left", "Top-Right", "Bottom-Left", "Bottom-Right" }
                : new string[] { "Слева сверху", "Справа сверху", "Слева снизу", "Справа снизу" };
            for (int p = 0; p < 4; p++)
            {
                GUIStyle pStyle = (p == OverlayPosition) ? _actionButtonStyle : _buttonStyle;
                if (GUILayout.Button(posNames[p], pStyle, GUILayout.Height(24)))
                {
                    OverlayPosition = p;
                    SaveConfig();
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            GUILayout.Label(L("Визуальный стиль HUD:", "HUD Visual Style:"), _labelStyle, GUILayout.Width(180));
            string[] styleNames = (Language == 1)
                ? new string[] { "Tactical Glass", "Minimalist", "Cyber Terminal" }
                : new string[] { "Тактическое стекло", "Минималистичный", "Кибер-терминал" };
            for (int s = 0; s < 3; s++)
            {
                GUIStyle sStyle = (s == OverlayStyle) ? _actionButtonStyle : _buttonStyle;
                if (GUILayout.Button(styleNames[s], sStyle, GUILayout.Height(24)))
                {
                    OverlayStyle = s;
                    SaveConfig();
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(8);

            // About & Credits
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(L("О МОДИФИКАЦИИ И АВТОРЕ", "ABOUT & CREDITS"), _sectionHeaderStyle);
            GUILayout.Label("Buckshot Roulette - CoreTune " + ModVersion, _valueStyle);
            GUILayout.Label(string.Format(L("Автор: {0}", "Author: {0}"), AuthorName), _labelStyle);
            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Boosty", _actionButtonStyle, GUILayout.Height(30)))
            {
                OpenBrowserUrl(BoostyUrl);
            }
            if (GUILayout.Button("Nexus Mods", _buttonStyle, GUILayout.Height(30)))
            {
                OpenBrowserUrl(NexusModsUrl);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
        #endregion
        #endregion

        #region In-Game HUD Overlay (When Menu is Closed)
        private void DrawHUDOverlay()
        {
            var sd = GetShotgunDataSafe();
            var rd = GetRoundDataSafe();

            string chamberStr = L("ПУСТО", "EMPTY");
            int liveCount = 0;
            int blankCount = 0;
            Color chamberCol = new Color(0.6f, 0.65f, 0.7f);

            if (sd != null && sd.ShellsList != null && sd.ShellsList.Count > 0)
            {
                var chamber = sd.ShellsList[0];
                if (chamber != null)
                {
                    bool isLive = (chamber.ShellType == ShellType.Live);
                    chamberStr = isLive ? L("БОЕВОЙ", "LIVE") : L("ХОЛОСТОЙ", "BLANK");
                    chamberCol = isLive ? new Color(1f, 0.25f, 0.25f) : new Color(0.35f, 0.65f, 1f);
                }

                for (int i = 0; i < sd.ShellsList.Count; i++)
                {
                    if (sd.ShellsList[i] != null)
                    {
                        if (sd.ShellsList[i].ShellType == ShellType.Live) liveCount++;
                        else blankCount++;
                    }
                }
            }

            int pHealth = (rd != null) ? rd.PlayerHealth : 0;
            int dHealth = (rd != null) ? rd.DealerHealth : 0;

            List<string> activeCheats = new List<string>();
            if (GodMode) activeCheats.Add("GOD");
            if (OneHitKill) activeCheats.Add("1-HIT");
            if (SmartRoulette) activeCheats.Add("SMART");
            if (AlwaysLive) activeCheats.Add("ALL-LIVE");
            if (AlwaysBlank) activeCheats.Add("ALL-BLANK");
            if (ForceDealerSelfHarm) activeCheats.Add("SELF-HARM");
            if (PermanentDealerCuffs) activeCheats.Add("CUFFS");
            if (InfiniteItems) activeCheats.Add("INF-ITEMS");
            if (LuckyMedicine) activeCheats.Add("LUCKY-MED");

            float sw = Screen.width;
            float sh = Screen.height;
            float margin = 20f;
            float w = 260f;
            float h = 105f;
            if (ShowActiveCheats && activeCheats.Count > 0) h += 20f;

            float x = margin;
            float y = margin;
            if (OverlayPosition == 1) { x = sw - w - margin; y = margin; }
            else if (OverlayPosition == 2) { x = margin; y = sh - h - margin; }
            else if (OverlayPosition == 3) { x = sw - w - margin; y = sh - h - margin; }

            Rect overlayRect = new Rect(x, y, w, h);

            // Style 0: Tactical Glass
            if (OverlayStyle == 0)
            {
                DrawSolidRect(overlayRect, new Color(0.06f, 0.08f, 0.11f, 0.90f));
                DrawOutline(overlayRect, new Color(0.18f, 0.24f, 0.32f, 0.85f), 1.5f);

                GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
                headerStyle.fontSize = 11;
                headerStyle.fontStyle = FontStyle.Bold;
                headerStyle.normal.textColor = new Color(0.25f, 0.85f, 1f);

                GUI.Label(new Rect(x + 12f, y + 8f, 150f, 18f), "CORETUNE " + ModVersion, headerStyle);

                if (ShowFps)
                {
                    GUIStyle fpsStyle = new GUIStyle(GUI.skin.label);
                    fpsStyle.alignment = TextAnchor.MiddleRight;
                    fpsStyle.fontSize = 10;
                    fpsStyle.fontStyle = FontStyle.Bold;
                    fpsStyle.normal.textColor = new Color(0.2f, 0.95f, 0.5f);
                    GUI.Label(new Rect(x + w - 85f, y + 8f, 75f, 18f), string.Format("{0:F0} FPS", _currentFps), fpsStyle);
                }

                float curY = y + 28f;
                if (ShowShellInfo)
                {
                    GUIStyle badgeStyle = new GUIStyle(GUI.skin.label);
                    badgeStyle.fontSize = 11;
                    badgeStyle.fontStyle = FontStyle.Bold;
                    badgeStyle.normal.textColor = chamberCol;
                    GUI.Label(new Rect(x + 12f, curY, w - 24f, 18f), L("В СТВОЛЕ: ", "IN CHAMBER: ") + chamberStr, badgeStyle);

                    curY += 18f;
                    GUIStyle qStyle = new GUIStyle(GUI.skin.label);
                    qStyle.fontSize = 10;
                    qStyle.normal.textColor = Color.white;
                    GUI.Label(new Rect(x + 12f, curY, w - 24f, 16f), string.Format(L("Обойма: {0} боевых / {1} холостых", "Magazine: {0} live / {1} blank"), liveCount, blankCount), qStyle);
                    curY += 18f;
                }

                GUIStyle hpStyle = new GUIStyle(GUI.skin.label);
                hpStyle.fontSize = 10;
                hpStyle.normal.textColor = new Color(0.85f, 0.9f, 0.95f);
                GUI.Label(new Rect(x + 12f, curY, w - 24f, 16f), string.Format(L("ИГРОК: {0} HP   |   ДИЛЕР: {1} HP", "PLAYER: {0} HP   |   DEALER: {1} HP"), pHealth, dHealth), hpStyle);
                curY += 18f;

                if (ShowActiveCheats && activeCheats.Count > 0)
                {
                    GUIStyle chStyle = new GUIStyle(GUI.skin.label);
                    chStyle.fontSize = 9;
                    chStyle.fontStyle = FontStyle.Bold;
                    chStyle.normal.textColor = new Color(0.95f, 0.75f, 0.2f);
                    GUI.Label(new Rect(x + 12f, curY, w - 24f, 16f), L("ЧИТЫ: ", "CHEATS: ") + string.Join(" | ", activeCheats.ToArray()), chStyle);
                }
            }
            // Style 1: Minimal Stealth
            else if (OverlayStyle == 1)
            {
                DrawSolidRect(overlayRect, new Color(0.02f, 0.03f, 0.05f, 0.85f));
                DrawSolidRect(new Rect(x, y, 3.5f, h), chamberCol);

                GUIStyle mStyle = new GUIStyle(GUI.skin.label);
                mStyle.fontSize = 10;
                mStyle.fontStyle = FontStyle.Bold;
                mStyle.normal.textColor = Color.white;

                string line1 = string.Format("{0} ({1}L/{2}B) | {3}/{4} HP", chamberStr, liveCount, blankCount, pHealth, dHealth);
                if (ShowFps) line1 += string.Format(" | {0:F0} FPS", _currentFps);
                GUI.Label(new Rect(x + 10f, y + 6f, w - 16f, 18f), line1, mStyle);

                if (ShowActiveCheats && activeCheats.Count > 0)
                {
                    GUIStyle mCh = new GUIStyle(GUI.skin.label);
                    mCh.fontSize = 9;
                    mCh.normal.textColor = new Color(0.2f, 0.9f, 0.5f);
                    GUI.Label(new Rect(x + 10f, y + 24f, w - 16f, 16f), string.Format(L("{0} активных читов: {1}", "{0} active cheats: {1}"), activeCheats.Count, string.Join(", ", activeCheats.ToArray())), mCh);
                }
            }
            // Style 2: Cyber Terminal
            else if (OverlayStyle == 2)
            {
                DrawSolidRect(overlayRect, new Color(0.01f, 0.03f, 0.02f, 0.95f));
                DrawOutline(overlayRect, new Color(0.0f, 0.8f, 0.4f, 0.85f), 1.5f);

                GUIStyle termStyle = new GUIStyle(GUI.skin.label);
                termStyle.fontSize = 10;
                termStyle.fontStyle = FontStyle.Bold;
                termStyle.normal.textColor = new Color(0.2f, 0.95f, 0.45f);

                string termHeader = ">> CORETUNE " + ModVersion;
                if (ShowFps) termHeader += string.Format(" [{0:F0} FPS]", _currentFps);
                GUI.Label(new Rect(x + 10f, y + 6f, w - 20f, 16f), termHeader, termStyle);

                if (ShowShellInfo)
                {
                    GUI.Label(new Rect(x + 10f, y + 22f, w - 20f, 16f), string.Format(">> {0} : {1} ({2}L/{3}B)", L("СТВОЛ", "CHAMBER"), chamberStr, liveCount, blankCount), termStyle);
                }
                GUI.Label(new Rect(x + 10f, y + 38f, w - 20f, 16f), string.Format(">> HP {0}[{1}]  {2}[{3}]", L("ВЫ", "YOU"), pHealth, L("ДИЛЕР", "DEALER"), dHealth), termStyle);

                if (ShowActiveCheats && activeCheats.Count > 0)
                {
                    GUI.Label(new Rect(x + 10f, y + 54f, w - 20f, 16f), ">> ACT: " + string.Join("+", activeCheats.ToArray()), termStyle);
                }
            }
        }
        #endregion

        #region Toast System
        private void DrawToastNotification()
        {
            if (_toastTimer <= 0f || string.IsNullOrEmpty(_toastMessage)) return;

            float alpha = Mathf.Clamp01(_toastTimer);
            Color oldCol = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);

            float tw = 480f;
            float th = 34f;
            float tx = (Screen.width - tw) * 0.5f;
            float ty = 18f;

            Rect toastRect = new Rect(tx, ty, tw, th);
            DrawSolidRect(toastRect, new Color(0.08f, 0.10f, 0.14f, 0.95f));
            DrawOutline(toastRect, new Color(0.2f, 0.75f, 1.0f, 0.8f), 1.5f);

            GUI.Label(toastRect, _toastMessage, _toastStyle);

            GUI.color = oldCol;
        }
        #endregion
    }
}
