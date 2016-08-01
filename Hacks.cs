namespace LeagueSharp.Common
{

    using System;

    internal class Hacks
    {
        private static Menu menu;

        private static MenuItem MenuAntiAfk;

        private static MenuItem MenuDisableDrawings;

        private static MenuItem MenuDisableSay;

        private static MenuItem MenuTowerRange;

        private const int WM_KEYDOWN = 0x100;

        private const int WM_KEYUP = 0x101;

        internal static void Initialize()
        {
            CustomEvents.Game.OnGameLoad += eventArgs =>
            {
                menu = new Menu("Hacks", "Hacks");

                MenuAntiAfk = menu.AddItem(new MenuItem("AfkHack", "Anti-AFK").SetValue(false));
                MenuAntiAfk.ValueChanged += (sender, args) => LeagueSharp.Hacks.AntiAFK = args.GetNewValue<bool>();

                MenuDisableDrawings = menu.AddItem(new MenuItem("DrawingHack", "Disable Drawing").SetValue(false));
                MenuDisableDrawings.ValueChanged += (sender, args) => LeagueSharp.Hacks.DisableDrawings = args.GetNewValue<bool>();
                MenuDisableDrawings.SetValue(LeagueSharp.Hacks.DisableDrawings);

                MenuDisableSay = menu.AddItem(new MenuItem("SayHack", "Disable L# Send Chat").SetValue(false).SetTooltip("Block Game.Say from Assemblies"));
                MenuDisableSay.ValueChanged += (sender, args) => LeagueSharp.Hacks.DisableSay = args.GetNewValue<bool>();

                MenuTowerRange = menu.AddItem(new MenuItem("TowerHack", "Show Tower Ranges").SetValue(false));
                MenuTowerRange.ValueChanged += (sender, args) => LeagueSharp.Hacks.TowerRanges = args.GetNewValue<bool>();

                LeagueSharp.Hacks.AntiAFK = MenuAntiAfk.GetValue<bool>();
                LeagueSharp.Hacks.DisableDrawings = MenuDisableDrawings.GetValue<bool>();
                LeagueSharp.Hacks.DisableSay = MenuDisableSay.GetValue<bool>();
                LeagueSharp.Hacks.TowerRanges = MenuTowerRange.GetValue<bool>();

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

        public static void Shutdown()
        {
            Menu.Remove(menu);
        }
    }
        
    internal class AutoSet
    {
        private Menu _config;
        private int wind;

        public AutoSet(Menu _config)
        {
            this._config = _config;

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
                _config.Item("MissileCheck").SetValue(false);
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