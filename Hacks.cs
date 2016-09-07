namespace LeagueSharp.Common
{
    using System;
    using System.Linq;
    using SharpDX;

    internal class Hacks
    {
        #region Constants

        private const int WM_KEYDOWN = 0x100;

        private const int WM_KEYUP = 0x101;

        #endregion

        #region Static Fields

        private static Menu menu;

        private static MenuItem MenuAntiAfk;

        private static MenuItem MenuDisableDrawings;

        private static MenuItem MenuDisableSay;

        private static MenuItem MenuTowerRange;

        public static bool AntiAFK
        {
            get
            {
                return LeagueSharp.Hacks.AntiAFK;
            }
            set
            {
                if (value == LeagueSharp.Hacks.AntiAFK)
                {
                    return;
                }

                LeagueSharp.Hacks.AntiAFK = value;
            }
        }

        public static bool DisableDrawings
        {
            get
            {
                return LeagueSharp.Hacks.DisableDrawings;
            }
            set
            {
                if (value == LeagueSharp.Hacks.DisableDrawings)
                {
                    return;
                }

                LeagueSharp.Hacks.DisableDrawings = value;
            }
        }

        public static bool DisableSay
        {
            get
            {
                return LeagueSharp.Hacks.DisableSay;
            }
            set
            {
                if (value == LeagueSharp.Hacks.DisableSay)
                {
                    return;
                }

                LeagueSharp.Hacks.DisableSay = value;
            }
        }

        public static bool TowerRanges
        {
            get
            {
                return LeagueSharp.Hacks.TowerRanges;
            }
            set
            {
                if (value == LeagueSharp.Hacks.TowerRanges)
                {
                    return;
                }

                LeagueSharp.Hacks.TowerRanges = value;
            }
        }

        #endregion

        #region Public Methods and Operators

        public static void Shutdown()
        {
            Menu.Remove(menu);
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Initializes this instance.
        /// </summary>
        internal static void Initialize()
        {
            CustomEvents.Game.OnGameLoad += eventArgs =>
            {
                menu = new Menu("Hacks", "Hacks");

                MenuAntiAfk = menu.AddItem(new MenuItem("AfkHack", "Anti-AFK").SetValue(false));
                MenuAntiAfk.ValueChanged += (sender, args) => AntiAFK = args.GetNewValue<bool>();

                MenuDisableDrawings = menu.AddItem(new MenuItem("DrawingHack", "Disable Drawing").SetValue(false));
                MenuDisableDrawings.ValueChanged += (sender, args) => DisableDrawings = args.GetNewValue<bool>();
                MenuDisableDrawings.SetValue(DisableDrawings);

                MenuDisableSay = menu.AddItem(new MenuItem("SayHack", "Disable L# Send Chat").SetValue(false).SetTooltip("Block Game.Say from Assemblies"));
                MenuDisableSay.ValueChanged += (sender, args) => DisableSay = args.GetNewValue<bool>();

                MenuTowerRange = menu.AddItem(new MenuItem("TowerHack", "Show Tower Ranges").SetValue(false));
                MenuTowerRange.ValueChanged += (sender, args) => TowerRanges = args.GetNewValue<bool>();

                AntiAFK = MenuAntiAfk.GetValue<bool>();
                DisableDrawings = MenuDisableDrawings.GetValue<bool>();
                DisableSay = MenuDisableSay.GetValue<bool>();
                TowerRanges = MenuTowerRange.GetValue<bool>();

                CommonMenu.Instance.AddSubMenu(menu);

                Game.OnWndProc += args =>
                {
                    if (!MenuDisableDrawings.GetValue<bool>())
                    {
                        return;
                    }

                    if ((int)args.WParam != Config.ShowMenuPressKey)
                    {
                        return;
                    }

                    if (args.Msg == WM_KEYDOWN)
                    {
                        LeagueSharp.Hacks.DisableDrawings = false;
                    }

                    if (args.Msg == WM_KEYUP)
                    {
                        LeagueSharp.Hacks.DisableDrawings = true;
                    }
                };
            };
        }

        #endregion
    }

    internal class AutoSet
    {
        private Menu _config;
        private int wind;

        public AutoSet(Menu _config)
        {
            this._config = _config;

            _config.Item("autosetwinddd", true).ValueChanged += (sender, args) =>
            {
                _config.Item("MissileCheck").SetValue(!args.GetNewValue<bool>());
            };

            Game.OnUpdate += OnUpdate;
        }

        private void OnUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead)
                return;

            SetYourWindUp();

            if (_config.Item("autosetwinddd", true).GetValue<bool>())
            {
                _config.Item("ExtraWindup").SetValue(wind < 200 ? new Slider(wind, 200, 0) : new Slider(200, 200, 0));
            }
        }

        private void SetYourWindUp()
        {
            var additional = 0;

            if (Game.Ping >= 100)
            {
                additional = Game.Ping / 100 * 10;
            }
            else if (Game.Ping > 40 && Game.Ping < 100)
            {
                additional = Game.Ping / 100 * 20;
            }
            else if (Game.Ping <= 40)
            {
                additional = +20;
            }
            var windUp = Game.Ping + additional;
            if (windUp < 40)
            {
                windUp = 40;
            }

            wind = windUp;
        }
    }
}