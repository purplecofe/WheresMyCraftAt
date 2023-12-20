using ExileCore;

namespace WheresMyCraftAt.Extensions
{
    public static partial class CurrencyExtensions
    {
        private static GameController GC;
        private static WheresMyCraftAt Main;

        public static void Initialize(WheresMyCraftAt main)
        {
            Main = main;
            GC = main.GameController;
        }

        public static bool TryGetCurrencyName(Currency currencyName, out string rawName)
        {
            switch (currencyName)
            {
                case Currency.OrbOfTransmutation:
                    rawName = "Orb of Transmutation";
                    return true;

                case Currency.OrbOfScouring:
                    rawName = "Orb of Scouring";
                    return true;

                case Currency.OrbOfAlteration:
                    rawName = "Orb of Alteration";
                    return true;

                case Currency.RegalOrb:
                    rawName = "Regal Orb";
                    return true;

                case Currency.ChaosOrb:
                    rawName = "Chaos Orb";
                    return true;

                case Currency.OrbOfAlchemy:
                    rawName = "Orb of Alchemy";
                    return true;

                case Currency.None:
                default:
                    rawName = null;
                    return false;
            }
        }
    }
}