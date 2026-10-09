namespace REPOJP.StageRoles;
internal sealed partial class StageRolesConfig
{
    internal Setting<float> TwinsCarryStrengthMultiplier=new(1.5f);
    internal Setting<float> TwinsCollisionReductionPercent=new(50f);
    internal Setting<float> TwinsRestRadius=new(3f);
    internal Setting<float> TwinsRestHoldSeconds=new(3f);
    internal Setting<float> TwinsRestHealPercent=new(10f);
    internal Setting<float> TwinsRestCooldownSeconds=new(60f);
    internal Setting<float> TwinsRendezvousStartRange=new(15f);
    internal Setting<float> TwinsRendezvousEndRange=new(5f);
    internal Setting<float> TwinsRendezvousSpeedMultiplier=new(1.25f);
    internal Setting<float> TwinsDeliveryCarrySeconds=new(3f);
    internal Setting<float> TwinsDeliveryRadius=new(5f);
    internal Setting<float> TwinsDeliveryBonusPercent=new(10f);
    internal Setting<int> TwinsDeliveryStageLimit=new(5000);
}
