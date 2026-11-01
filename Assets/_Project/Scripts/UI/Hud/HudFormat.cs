using System.Text;
using FateBastion.Core;
using FateBastion.Heroes;

namespace FateBastion.UI
{
    /// <summary>Vietnamese HUD texts (S10, B-14). Pure so every label is unit-tested.</summary>
    public static class HudFormat
    {
        /// <summary>18.2 s → "0:19" (rounded up, so the timer reads 0:01 until it really ends).</summary>
        public static string Clock(float seconds)
        {
            int total = seconds <= 0f ? 0 : (int)System.Math.Ceiling(seconds);
            return $"{total / 60}:{total % 60:00}";
        }

        /// <summary>Whole seconds the clock shows; the HUD only rewrites the text when this changes.</summary>
        public static int ClockSeconds(float seconds) => seconds <= 0f ? 0 : (int)System.Math.Ceiling(seconds);

        /// <summary>Top panel text for the match state: preparation and rest show the countdown to the next wave.</summary>
        public static string Wave(GameState state, int currentWave, int waveCount, bool endless, float timeLeft)
        {
            switch (state)
            {
                case GameState.Loading:
                    return "Đang tải…";
                case GameState.Preparing:
                    return $"Chuẩn bị · {Clock(timeLeft)}";
                case GameState.Resting:
                    return $"{WaveNumber(currentWave + 1, waveCount, endless)} · {Clock(timeLeft)}";
                default:
                    return WaveNumber(currentWave, waveCount, endless);
            }
        }

        public static string WaveNumber(int wave, int waveCount, bool endless) =>
            endless || waveCount <= 0 ? $"Wave {wave}" : $"Wave {wave}/{waveCount}";

        public static string Castle(int current, int max) => $"Thành {current}/{max}";

        public static string Gold(int gold) => $"Vàng {UiText.Number(gold)}";

        /// <summary>
        /// Same text as <see cref="Gold"/> written into <paramref name="builder"/> digit by digit, so the HUD can refresh the
        /// gold on every kill without allocating (S10: no GC while gold changes).
        /// </summary>
        public static StringBuilder AppendGold(StringBuilder builder, int gold)
        {
            builder.Append("Vàng ");
            long value = gold;
            if (value < 0)
            {
                builder.Append('-');
                value = -value;
            }

            long divisor = 1;
            int digits = 1;
            while (value / divisor >= 10)
            {
                divisor *= 10;
                digits++;
            }

            for (; divisor > 0; divisor /= 10, digits--)
            {
                builder.Append((char)('0' + value / divisor % 10));
                // Thousands separator "." as in UiText.Number (1.250).
                if (digits > 1 && (digits - 1) % 3 == 0) builder.Append('.');
            }

            return builder;
        }

        public static string UnitCount(int placed, int max) => $"Quân số {placed}/{max}";

        public static string Speed(float speed) => speed > 1.01f ? $"Tốc ×{speed:0.#}" : "Tốc ×1";

        public static string LevelText(int level, int maxLevel) => $"Cấp {level}/{maxLevel}";

        public static string Upgrade(int cost) => cost < 0 ? "Cấp tối đa" : $"Nâng (U) {UiText.Number(cost)}";

        public static string Sell(int refund) => $"Bán (X) {UiText.Number(refund)}";

        public static string Rage(float current, float max) => $"Nộ {(int)current}/{(int)max}";

        public static string ElementName(Element element)
        {
            switch (element)
            {
                case Core.Element.Fire: return "Lửa";
                case Core.Element.Ice: return "Băng";
                case Core.Element.Lightning: return "Sét";
                default: return element.ToString();
            }
        }

        public static string RoleName(Role role)
        {
            switch (role)
            {
                case Core.Role.SingleDps: return "Sát thương đơn";
                case Core.Role.Mage: return "Pháp sư";
                case Core.Role.Control: return "Khống chế";
                case Core.Role.Support: return "Hỗ trợ";
                default: return role.ToString();
            }
        }

        /// <summary>Why a hero cannot be placed (S4: "dừng ở lỗi đầu tiên và hiện lý do").</summary>
        public static string Placement(PlacementError error)
        {
            switch (error)
            {
                case PlacementError.None: return string.Empty;
                case PlacementError.NotEnoughGold: return "Không đủ Vàng";
                case PlacementError.HeroLimitReached: return "Đã đủ số con của tướng này";
                case PlacementError.ArmyFull: return "Quân số đã đầy (15)";
                case PlacementError.NotPlaceableGround: return "Không đặt được ở đây";
                case PlacementError.AreaBlocked: return "Chỗ này bị chắn (đường, vật cản hoặc tướng khác)";
                case PlacementError.AimingMeteor: return "Đang ngắm Thiên thạch";
                default: return error.ToString();
            }
        }

        public static string Upgrade(UpgradeError error)
        {
            switch (error)
            {
                case UpgradeError.NotEnoughGold: return "Không đủ Vàng để nâng cấp";
                case UpgradeError.MaxLevel: return "Tướng đã ở cấp tối đa";
                case UpgradeError.NotPlaced: return "Chưa chọn tướng";
                default: return string.Empty;
            }
        }

        /// <summary>Meteor slot: seconds left, or "Sẵn sàng" at 0.</summary>
        public static string Cooldown(float remaining) => remaining <= 0f ? "Sẵn sàng" : Clock(remaining);
    }
}
