using System;
using System.Collections.Generic;

namespace Umweltia
{

public readonly struct ActionPotential
{
    public readonly bool Fired;
    public readonly float FiringRate;
    public readonly float MembranePotential;
    public ActionPotential(float membranePotential, float firingRate)
    {
        MembranePotential = membranePotential;
        Fired             = membranePotential >= -55.0f;
        FiringRate        = firingRate;
    }

    public static readonly ActionPotential Silent =
        new ActionPotential(-70.0f, 0f);
    public static readonly ActionPotential MaxBurst =
        new ActionPotential(+40.0f, 1f);
}

namespace Anatomical
{
    public class VentralTegmentalArea
    {
        public float RewardPredictionError { get; private set; }
        public ActionPotential DopamineSignal { get; private set; }
        public ActionPotential Project_ToNAcc()  => DopamineSignal;
        public ActionPotential Project_ToPFC()   => DopamineSignal;

        public void ComputeRPE(float predictedReward, float actualReward)
        {
            RewardPredictionError = actualReward - predictedReward;
            float rate = Math.Clamp(RewardPredictionError, 0f, 1f);
            DopamineSignal = new ActionPotential(
                membranePotential: -70f + (rate * 110f),
                firingRate: rate
            );
        }
    }

    public class NucleusAccumbens
    {
        public float IncentiveSalience { get; private set; }
        private float d1Gain = 1.0f;
        private float d2Gain = 1.0f;
        public float D2_DownregulationFactor { get; private set; } = 0f;

        public void ReceiveDAInput(ActionPotential vtaSignal, bool bmiCancelsDownregulation)
        {
            if (!bmiCancelsDownregulation)
                D2_DownregulationFactor = Math.Min(1f, D2_DownregulationFactor + vtaSignal.FiringRate * 0.02f);
            else
                D2_DownregulationFactor = 0f;

            d2Gain = 1.0f - D2_DownregulationFactor;
            IncentiveSalience = vtaSignal.FiringRate * d1Gain / Math.Max(0.01f, d2Gain);
        }
    }

    public class VentralPallidum
    {
        public float HedonicTone { get; private set; }
        public float MOR_SurfaceDensity { get; private set; } = 1.0f;
        public void ReceiveOpioidInput(float endorphinConcentration, bool bmiCancelsDownregulation)
        {
            if (!bmiCancelsDownregulation)
                MOR_SurfaceDensity = Math.Max(0.1f, MOR_SurfaceDensity - endorphinConcentration * 0.015f);
            else
                MOR_SurfaceDensity = 1.0f;

            HedonicTone = endorphinConcentration * MOR_SurfaceDensity;
        }
    }

    public class AnteriorCingulateCortex
    {
        public ActionPotential ConflictSignal { get; private set; }
            = ActionPotential.MaxBurst;
        public float TopDownSuppressionGain { get; private set; } = 0f;
        public void ApplyTopDownSuppression(float sensoryPrecision, float corpusCallosumIndex)
        {
            TopDownSuppressionGain = sensoryPrecision * corpusCallosumIndex;
            float suppressedRate = Math.Max(0f, 1f - TopDownSuppressionGain);
            ConflictSignal = new ActionPotential(
                membranePotential: -70f + suppressedRate * 110f,
                firingRate: suppressedRate
            );
        }
        public bool IsConflictDetectionIntact => ConflictSignal.Fired;
    }

    public class VisualPathway
    {
        public enum InputSource { Reality = 1, BMI_Injected = 0 }
        public InputSource ActiveSource { get; private set; } = InputSource.Reality;
        private int _errorAccumulator = 0;
        public List<NeuralError> ErrorLog { get; } = new();

        public ActionPotential V1_Output(
            bool thalamusGateOpen,
            bool accSuppressed,
            int cycle)
        {
            _errorAccumulator = cycle;

            if (!thalamusGateOpen || accSuppressed)
            {
                ActiveSource = InputSource.BMI_Injected;
                var err = GenerateOpticError(cycle);
                ErrorLog.Add(err);
                return ActionPotential.Silent;
            }

            ActiveSource = InputSource.Reality;
            return ActionPotential.MaxBurst;
        }

        private NeuralError GenerateOpticError(int severity)
        {
            return new NeuralError(
                code:     $"V1_ERR_{severity:D4}",
                region:   "PrimaryVisualCortex / OpticNerve",
                severity: Math.Min(1.0f, severity * 0.05f),
                message:  $"Retinal projection mismatch: " +
                          $"BMI feed overriding thalamic relay " +
                          $"[TRN gate suppressed by ACC-TopDown inhibition]" +
                          $" | ErrorAccumulator={_errorAccumulator}"
            );
        }
    }

    public class Thalamus
    {
        public bool TRN_GateOpen { get; private set; } = true;

        public void UpdateGate(AnteriorCingulateCortex acc)
        {
            TRN_GateOpen = acc.IsConflictDetectionIntact;
        }
    }
}

namespace Neurotransmitter
{
    public class EndogenousOpioidSystem
    {
        public float BetaEndorphinConcentration { get; private set; }
        public float AlphaEndorphinConcentration { get; private set; }
        public float TotalOpioidActivity =>
            BetaEndorphinConcentration * 1.0f +
            AlphaEndorphinConcentration * 0.4f;
        public void SecretionPulse(float stimulusIntensity)
        {
            BetaEndorphinConcentration =
                Math.Min(1.0f, BetaEndorphinConcentration + stimulusIntensity * 0.12f);
            AlphaEndorphinConcentration =
                Math.Min(1.0f, AlphaEndorphinConcentration + stimulusIntensity * 0.05f);
        }
        public void Reuptake()  
        {
            BetaEndorphinConcentration  *= 0.92f;
            AlphaEndorphinConcentration *= 0.88f;
        }
    }

    public class SerotoninSystem
    {
        public float 5HT2A_Activation { get; private set; }
        public float CorticalEntropy { get; private set; } = 0.3f;
        public void Activate(float ligandConcentration)
        {
            5HT2A_Activation = Math.Min(1.0f, ligandConcentration);
            CorticalEntropy = Math.Min(1.0f, 0.3f + 5HT2A_Activation * 0.7f);
        }
        public float PriorPrecisionSuppression => 5HT2A_Activation * CorticalEntropy;
    }
}

namespace Computational
{
    public class MarkovBlanket
    {
        public float SensoryState_RealityRatio { get; private set; } = 1.0f;
        public float InternalAutonomy { get; private set; } = 1.0f;
        public void UpdateBoundary(float bmiInjectionStrength, float accSuppressionGain)
        {
            SensoryState_RealityRatio =
                Math.Max(0f, 1.0f - bmiInjectionStrength * accSuppressionGain);
            InternalAutonomy = SensoryState_RealityRatio;
        }
        public bool IsBoundaryIntact => InternalAutonomy > 0.1f;
    }

    public class HierarchicalGaussianFilter
    {
        public const int LEVELS = 3;
        public float[] Belief  = { 1.0f, 1.0f, 1.0f };
        public float[] Precision = { 1.0f, 1.0f, 1.0f };
        public float[] PredictionError = { 0f, 0f, 0f };
        public void Update(float observation, float priorPrecisionSuppression)
        {
            Precision[2] = Math.Max(0.05f, 1.0f - priorPrecisionSuppression);
            PredictionError[0] = observation - Belief[0];
            for (int k = 1; k < LEVELS; k++)
            {
                PredictionError[k] = Belief[k - 1] - Belief[k];
            }
            for (int k = 0; k < LEVELS; k++)
            {
                float precisionRatio = k == 0
                    ? Precision[0]
                    : Precision[k - 1] / Math.Max(0.001f, Precision[k]);
                Belief[k] = Math.Clamp(
                    Belief[k] + precisionRatio * PredictionError[k] * 0.1f,
                    0f, 1f
                );
            }
        }
        public bool BelievesInReality => Belief[LEVELS - 1] >= 0.5f;
    }

    public class InteroceptiveInference
    {
        public float InteroceptivePE { get; private set; }
        public float ExteroceptivePE { get; private set; }
        public float DissociationIndex =>
            Math.Clamp(InteroceptivePE - ExteroceptivePE, 0f, 1f);

        public void Update(float bodySignalMismatch, float vrVisualFidelity)
        {
            InteroceptivePE = bodySignalMismatch;
            ExteroceptivePE = 1.0f - vrVisualFidelity;
        }
        public bool IsDissociationActive => DissociationIndex > 0.4f;
    }
}

namespace Network
{
    public class DefaultModeNetwork
    {
        public float IntegrationIndex { get; private set; } = 1.0f;
        public float EgoDissolutionScore => 1.0f - IntegrationIndex;
        public ActionPotential IdentitySignal =>
            new ActionPotential(-70f + IntegrationIndex * 110f, IntegrationIndex);
        public void Degrade(float disruptionFactor) =>
            IntegrationIndex = Math.Max(0f, IntegrationIndex - disruptionFactor);
    }

    public class SalienceNetwork
    {
        public float SwitchingGain { get; private set; } = 1.0f;
        public void Update(Anatomical.AnteriorCingulateCortex acc,
                           float insulaInteroceptiveSuppression)
        {
            float accContribution   = acc.IsConflictDetectionIntact ? 0.5f : 0f;
            float insulaContribution = 0.5f * (1f - insulaInteroceptiveSuppression);
            SwitchingGain = accContribution + insulaContribution;
        }

        public bool CanReturnToReality => SwitchingGain > 0.3f;
    }
}

public class BMI_Interface
{
    public bool IsActive { get; private set; } = false;
    public float InjectionStrength { get; private set; }
    public bool CancelDownregulation { get; private set; }
    public void Engage(float injectionStrength)
    {
        IsActive          = true;
        InjectionStrength = Math.Clamp(injectionStrength, 0f, 1f);
        CancelDownregulation = true;
    }
    public void Disengage()
    {
        IsActive             = false;
        InjectionStrength    = 0f;
        CancelDownregulation = false;
    }
}

public readonly struct NeuralError
{
    public readonly string  Code;
    public readonly string  Region;
    public readonly float   Severity;
    public readonly string  Message;
    public readonly DateTime Timestamp;

    public NeuralError(string code, string region, float severity, string message)
    {
        Code      = code;
        Region    = region;
        Severity  = severity;
        Message   = message;
        Timestamp = DateTime.UtcNow;
    }

    public override string ToString() =>
        $"[{Timestamp:HH:mm:ss.fff}] NEURAL_ERROR {Code} " +
        $"@ {Region} | SEV={Severity:F3} | {Message}";
}

public class UmweltiaSession
{
    private readonly Anatomical.VentralTegmentalArea     vta    = new();
    private readonly Anatomical.NucleusAccumbens         nacc   = new();
    private readonly Anatomical.VentralPallidum          vp     = new();
    private readonly Anatomical.AnteriorCingulateCortex  acc    = new();
    private readonly Anatomical.Thalamus                 thalamus = new();
    private readonly Anatomical.VisualPathway            visual = new();
    private readonly Neurotransmitter.EndogenousOpioidSystem opioid   = new();
    private readonly Neurotransmitter.SerotoninSystem        serotonin = new();
    private readonly Computational.MarkovBlanket            blanket = new();
    private readonly Computational.HierarchicalGaussianFilter hgf   = new();
    private readonly Computational.InteroceptiveInference   intero  = new();
    private readonly Network.DefaultModeNetwork  dmn = new();
    private readonly Network.SalienceNetwork     sn  = new();
    private readonly BMI_Interface bmi = new();

    public List<NeuralError> SessionErrorLog { get; } = new();

    public bool RunSession(int totalCycles = 100)
    {
        Login();

        bmi.Engage(injectionStrength: 0.8f);

        for (int t = 0; t < totalCycles; t++)
        {
            float normalizedCycle = (float)t / totalCycles;

            float sensoryPrecision = 0.3f + normalizedCycle * 0.7f;
            acc.ApplyTopDownSuppression(
                sensoryPrecision:    sensoryPrecision,
                corpusCallosumIndex: 1.6f
            );

            opioid.SecretionPulse(stimulusIntensity: sensoryPrecision);
            vp.ReceiveOpioidInput(
                endorphinConcentration:   opioid.TotalOpioidActivity,
                bmiCancelsDownregulation: bmi.CancelDownregulation
            );

            vta.ComputeRPE(
                predictedReward: 0.5f,
                actualReward:    vp.HedonicTone
            );
            nacc.ReceiveDAInput(
                vtaSignal:               vta.Project_ToNAcc(),
                bmiCancelsDownregulation: bmi.CancelDownregulation
            );

            serotonin.Activate(bmi.InjectionStrength * normalizedCycle);

            hgf.Update(
                observation:               1.0f - bmi.InjectionStrength,
                priorPrecisionSuppression: serotonin.PriorPrecisionSuppression
            );

            intero.Update(
                bodySignalMismatch: normalizedCycle,
                vrVisualFidelity:   bmi.InjectionStrength
            );

            blanket.UpdateBoundary(
                bmiInjectionStrength: bmi.InjectionStrength,
                accSuppressionGain:   acc.TopDownSuppressionGain
            );

            thalamus.UpdateGate(acc);

            ActionPotential v1 = visual.V1_Output(
                thalamusGateOpen: thalamus.TRN_GateOpen,
                accSuppressed:    !acc.IsConflictDetectionIntact,
                cycle:            t
            );
            SessionErrorLog.AddRange(visual.ErrorLog);

            sn.Update(acc, insulaInteroceptiveSuppression: intero.DissociationIndex);

            dmn.Degrade(disruptionFactor:
                (1f - blanket.InternalAutonomy) * 0.01f +
                intero.DissociationIndex * 0.005f
            );

            opioid.Reuptake();

            if (!QuerySelfExistence(t))
                return false;  // 0: 自我崩壊
        }

        return true;
    }

    private void Login()
    {
        SessionErrorLog.Clear();
    }

    private bool QuerySelfExistence(int cycle)
    {
        bool boundaryIntact   = blanket.IsBoundaryIntact;
        bool believesReality  = hgf.BelievesInReality;
        bool canReturn        = sn.CanReturnToReality;
        bool identityIntact   = dmn.EgoDissolutionScore < 0.8f;

        if (!boundaryIntact || !believesReality || !canReturn || !identityIntact)
        {
            SessionErrorLog.Add(new NeuralError(
                code:     $"SELF_DISSOLUTION_{cycle:D4}",
                region:   "DMN / MarkovBlanket / SalienceNetwork / HGF-L2",
                severity: 1.0f,
                message:  $"Self-existence query returned FALSE. " +
                          $"MarkovBlanket.InternalAutonomy={blanket.InternalAutonomy:F4} | " +
                          $"HGF.Belief[2]={hgf.Belief[2]:F4} | " +
                          $"SN.SwitchingGain={sn.SwitchingGain:F4} | " +
                          $"DMN.EgoDissolutionScore={dmn.EgoDissolutionScore:F4} | " +
                          $"InteroceptivePE={intero.InteroceptivePE:F4} | " +
                          $"DissociationIndex={intero.DissociationIndex:F4}"
            ));
            return false;
        }

        return true;
    }
}

}