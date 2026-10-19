using Content.Shared._UU.Employment;
using Content.Shared.Examine;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Content.Server._UU.Employment;

/// <summary>
///     Осмотр удостоверения о работе: видно, что записано на карте.
/// </summary>
public sealed class WorkIdCardSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WorkIdCardComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<WorkIdCardComponent> ent, ref ExaminedEvent args)
    {
        var comp = ent.Comp;

        if (!comp.Issued)
        {
            args.PushMarkup(Loc.GetString("work-id-examine-blank"));
            return;
        }

        if (!string.IsNullOrWhiteSpace(comp.Holder))
        {
            args.PushMarkup(Loc.GetString("work-id-examine-holder",
                ("holder", (object)FormattedMessage.EscapeText(comp.Holder!))));
        }

        if (!string.IsNullOrWhiteSpace(comp.Position))
        {
            args.PushMarkup(Loc.GetString("work-id-examine-position",
                ("position", (object)FormattedMessage.EscapeText(comp.Position!))));
        }

        if (!string.IsNullOrWhiteSpace(comp.Employer))
        {
            args.PushMarkup(Loc.GetString("work-id-examine-employer",
                ("employer", (object)FormattedMessage.EscapeText(comp.Employer!))));
        }

        if (!string.IsNullOrWhiteSpace(comp.Date))
            args.PushMarkup(Loc.GetString("work-id-examine-date", ("date", (object)comp.Date!)));

        if (!string.IsNullOrWhiteSpace(comp.EmployerSignatureLabel))
        {
            var isState = comp.EmployerSignature == EmploymentContractSystem.StateSignature;
            args.PushMarkup(Loc.GetString(isState
                    ? "work-id-examine-signed-state"
                    : "work-id-examine-signed-person",
                ("name", (object)FormattedMessage.EscapeText(comp.EmployerSignatureLabel!))));
        }
    }
}
