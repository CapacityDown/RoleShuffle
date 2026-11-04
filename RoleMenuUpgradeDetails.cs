using System.Collections.Generic;
using MenuLib.MonoBehaviors;
using TMPro;
namespace REPOJP.StageRoles;
internal sealed partial class RoleMenu
{
    private static void RefreshUpgradeDetails(REPOPopupPage page)
    {
        var entries = new List<RoleMenuEntry>();
        void Text(string value) { foreach (string line in WrapGuideText(MeasurementText(page), value, ContentWidth(page), _guideLanguage))
            entries.Add(new RoleMenuEntry(line, GuideFontSize, FontStyles.Normal, GuideLineHeight, false, UseLanguageFont)); }
        entries.Add(new RoleMenuEntry(Localized("BASE UPGRADES"), 20, FontStyles.Bold, 40, false, UseLanguageFont,
            () => SwitchView(page, RoleMenuView.BaseUpgrades), isControl: true));
        Text(Localized("Your actual upgrade levels reported by the host. These are levels, not HP or movement speed."));
        var player = UpgradeBreakdownSync.ReadLocalPlayer();
        if (player == null) Text(Localized("Waiting for host upgrade details."));
        else
        {
            Text(RoleText.Format("Role: {0}", _guideLanguage, player.Role.HasValue ? RoleCatalog.AssignmentName(player.Role.Value) : Localized("None")));
            Text(Localized("Role targets include applicable retained items. Fixed roles can override them. Other changes include purchases or effects outside these entries."));
            foreach (var row in player.Rows)
            {
                entries.Add(new RoleMenuEntry(DisplayUpgradeName(row.Name) + "  " + row.Actual, 21, FontStyles.Bold, 36, false, UseLanguageFont, separatorBefore: true));
                Text(RoleText.Format("Base {0} | Retained items {1} | Role target {2}", _guideLanguage, row.Base, row.Retained, row.Target));
                Text(RoleText.Format("Dynamic role {0} | Support {1} | Cargo penalty {2} | Other {3}", _guideLanguage,
                    SignedValue(row.Dynamic), SignedValue(row.Support), SignedValue(-row.Penalty), row.Other.ToString("+0;-0;0", System.Globalization.CultureInfo.InvariantCulture)));
            }
        }
        ApplyEntries(page, entries); _openSignature = UpgradeBreakdownSync.Signature;
    }
}
