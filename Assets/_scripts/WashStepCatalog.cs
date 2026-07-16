using System.Collections.Generic;

public sealed class WashStepInfo
{
    public string DisplayName { get; }
    public string Instruction { get; }
    public float Duration { get; }
    public RubMode RubMode { get; }

    public WashStepInfo(string displayName, string instruction, float duration, RubMode rubMode)
    {
        DisplayName = displayName;
        Instruction = instruction;
        Duration = duration;
        RubMode = rubMode;
    }
}

public static class WashStepCatalog
{
    private static readonly Dictionary<WashStep, WashStepInfo> Infos = new()
    {
        {
            WashStep.WetHands,
            new WashStepInfo("Wet Hands", "Wet hands with water.", 2f, RubMode.None)
        },
        {
            WashStep.ApplySoap,
            new WashStepInfo("Apply Soap", "Press the soap pump and apply enough soap to cover all hand surfaces.", 1.5f, RubMode.None)
        },
        {
            WashStep.PalmToPalm,
            new WashStepInfo("Palm to Palm", "Rub hands palm to palm.", 4f, RubMode.Shared)
        },
        {
            WashStep.BackOfHands,
            new WashStepInfo("Back of Hands", "Rub right palm over left dorsum, then left palm over right dorsum.", 4f, RubMode.PerSide)
        },
        {
            WashStep.FingersInterlaced,
            new WashStepInfo("Fingers Interlaced", "Rub palm to palm with fingers interlaced.", 4f, RubMode.Shared)
        },
        {
            WashStep.BacksOfFingers,
            new WashStepInfo("Backs of Fingers", "Rub backs of fingers to opposing palms with fingers interlocked.", 4f, RubMode.Shared)
        },
        {
            WashStep.Thumbs,
            new WashStepInfo("Thumb Rotation", "Rotationally rub each thumb clasped in the opposite palm.", 4f, RubMode.PerSide)
        },
        {
            WashStep.Fingertips,
            new WashStepInfo("Fingertips", "Rotationally rub fingertips backwards and forwards in the opposite palm.", 4f, RubMode.PerSide)
        },
        {
            WashStep.Rinse,
            new WashStepInfo("Rinse", "Rinse hands with water.", 3f, RubMode.None)
        },
        {
            WashStep.Dry,
            new WashStepInfo("Dry", "Dry hands thoroughly, then use the towel to turn off the faucet.", 2f, RubMode.None)
        }
    };

    public static readonly IReadOnlyList<WashStep> Ordered = new[]
    {
        WashStep.WetHands,
        WashStep.ApplySoap,
        WashStep.PalmToPalm,
        WashStep.BackOfHands,
        WashStep.FingersInterlaced,
        WashStep.BacksOfFingers,
        WashStep.Thumbs,
        WashStep.Fingertips,
        WashStep.Rinse,
        WashStep.Dry
    };

    public static bool TryGet(WashStep step, out WashStepInfo info) => Infos.TryGetValue(step, out info);

    public static string GetDisplayName(WashStep step) => TryGet(step, out var i) ? i.DisplayName : step.ToString();

    public static string GetInstruction(WashStep step) => TryGet(step, out var i) ? i.Instruction : string.Empty;

    public static float GetDuration(WashStep step) => TryGet(step, out var i) ? i.Duration : 0f;

    public static RubMode GetRubMode(WashStep step) => TryGet(step, out var i) ? i.RubMode : RubMode.None;

    public static bool IsRubbingStep(WashStep step) => GetRubMode(step) != RubMode.None;
}
