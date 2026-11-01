using System.Collections.Generic;
using MenuLib.MonoBehaviors;
using TMPro;

namespace REPOJP.StageRoles;

internal sealed partial class RoleMenu
{
    private static bool _guideEligibility;
    private static void OpenDrawEligibility(REPOPopupPage page)
    {
        _guideEligibility = true;
        SwitchView(page, RoleMenuView.Guide);
    }
    private static void AddDrawEligibilityRows(REPOPopupPage page, List<RoleMenuEntry> entries)
    {
        void Text(string value)
        {
            foreach (string line in WrapGuideText(MeasurementText(page), value, ContentWidth(page), _guideLanguage))
                entries.Add(new RoleMenuEntry(line, GuideFontSize, FontStyles.Normal, GuideLineHeight, false, UseLanguageFont));
        }
        Text(Localized("Your fresh draw under current conditions. Order, team limits and other players' results can change the outcome; eligibility is not a guarantee."));
        RoleDrawSnapshot? snapshot = RoleDrawEligibilitySync.Read();
        string id = PlayerIdentity.SteamId(SemiFunc.PlayerGetLocal());
        IReadOnlyDictionary<StageRole, DrawReason>? rows = null;
        if (snapshot != null) snapshot.Players.TryGetValue(id, out rows);
        if (snapshot == null) Text(Localized("Waiting for host eligibility data. Older hosts do not provide it."));
        else
        {
            Text(RoleText.Format("Current players: {0}", _guideLanguage, snapshot.PlayerCount));
            if (!snapshot.StageKnown) Text(Localized("Outside a generated stage: item-dependent roles remain unconfirmed."));
            if (rows == null) Text(Localized("Waiting for your player information."));
        }
        foreach (StageRole role in RoleCatalog.AllRoles)
        {
            bool secret = RoleCatalog.IsSecretRole(role);
            DrawReason reason = secret ? DrawReason.Secret : rows != null && rows.TryGetValue(role, out DrawReason value) ? value : DrawReason.PlayerUnknown;
            entries.Add(new RoleMenuEntry(RoleCatalog.DisplayName(role), 20, FontStyles.Bold, 64, false,
                emblemRole: role, unrevealedEmblem: secret, emblemGrayedOut: RoleDrawEligibility.Status(reason) == DrawStatus.Excluded));
            Text("[" + RoleDrawEligibility.Label(reason, _guideLanguage) + "]");
            string detail = RoleDrawEligibility.Describe(reason, _guideLanguage);
            if (detail.Length > 0) Text(detail);
        }
    }
}
