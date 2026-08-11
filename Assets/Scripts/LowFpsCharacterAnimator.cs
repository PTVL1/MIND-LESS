using UnityEngine;

public class LowFpsCharacterAnimator : MonoBehaviour
{
    [Header("Body Parts")]
    [SerializeField] private Transform head;
    [SerializeField] private Transform body;
    [SerializeField] private Transform armLeft;
    [SerializeField] private Transform handLeft;
    [SerializeField] private Transform armRight;
    [SerializeField] private Transform handRight;
    [SerializeField] private Transform legLeft;
    [SerializeField] private Transform legRight;

    [Header("Animation Source")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private float movingThreshold = 0.1f;

    [Header("Chopped Look")]
    [SerializeField] [Range(2f, 24f)] private float animationFps = 8f;

    [Header("Idle Motion")]
    [SerializeField] private float idleBobSpeed = 1.8f;
    [SerializeField] private float idleBobAmount = 0.05f;
    [SerializeField] private float idleArmSwing = 4f;

    [Header("Walk Motion")]
    [SerializeField] private float walkCycleSpeed = 8f;
    [SerializeField] private float walkLegSwing = 26f;
    [SerializeField] private float walkArmSwing = 20f;
    [SerializeField] private float walkBodyBob = 0.08f;

    [Header("Punch Motion")]
    [SerializeField] private float punchDuration = 0.3f;
    [SerializeField] private float punchReach = 0.35f;
    [SerializeField] private float punchHandTilt = 25f;

    [Header("Melee Attack Motion")]
    [SerializeField] private float meleeAttackDuration = 0.4f;
    [SerializeField] private float meleeWindupAngle = 35f;
    [SerializeField] private float meleeSwingAngle = 80f;
    [SerializeField] private float meleeSideAngle = 18f;
    [SerializeField] private float meleeHandTilt = 25f;
    [SerializeField] private float meleeHandLift = 0.35f;
    [SerializeField] private float meleeHandDrop = 0.18f;

    private Quaternion headBaseRot;
    private Quaternion bodyBaseRot;
    private Quaternion armLeftBaseRot;
    private Quaternion handLeftBaseRot;
    private Quaternion armRightBaseRot;
    private Quaternion handRightBaseRot;
    private Quaternion legLeftBaseRot;
    private Quaternion legRightBaseRot;

    private Vector3 bodyBasePos;
    private Vector3 handLeftBasePos;
    private Vector3 handRightBasePos;
    private float elapsedTime;
    private float sampleTimer;
    private bool isMoving;
    private bool isPunching;
    private bool punchingWithLeftHand;
    private float punchStartTime;
    private bool isMeleeAttacking;
    private bool meleeAttackingWithLeftHand;
    private float meleeAttackStartTime;

    private void Awake()
    {
        if (characterController == null)
        {
            characterController = GetComponentInParent<CharacterController>();
        }

        CacheBasePose();
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;
        sampleTimer += Time.deltaTime;

        float sampleInterval = 1f / Mathf.Max(1f, animationFps);
        if (sampleTimer < sampleInterval)
        {
            return;
        }

        sampleTimer = 0f;
        isMoving = characterController != null && characterController.velocity.magnitude > movingThreshold;

        if (isMoving)
        {
            ApplyWalkPose(elapsedTime);
        }
        else
        {
            ApplyIdlePose(elapsedTime);
        }

        ApplyPunchPose();
        ApplyMeleeAttackPose();
    }

    private void CacheBasePose()
    {
        if (head != null) headBaseRot = head.localRotation;
        if (body != null)
        {
            bodyBaseRot = body.localRotation;
            bodyBasePos = body.localPosition;
        }
        if (armLeft != null) armLeftBaseRot = armLeft.localRotation;
        if (handLeft != null)
        {
            handLeftBaseRot = handLeft.localRotation;
            handLeftBasePos = handLeft.localPosition;
        }
        if (armRight != null) armRightBaseRot = armRight.localRotation;
        if (handRight != null)
        {
            handRightBaseRot = handRight.localRotation;
            handRightBasePos = handRight.localPosition;
        }
        if (legLeft != null) legLeftBaseRot = legLeft.localRotation;
        if (legRight != null) legRightBaseRot = legRight.localRotation;
    }

    private void ApplyIdlePose(float timeValue)
    {
        float bob = Mathf.Sin(timeValue * idleBobSpeed) * idleBobAmount;
        float arm = Mathf.Sin(timeValue * idleBobSpeed) * idleArmSwing;

        if (body != null)
        {
            body.localPosition = bodyBasePos + new Vector3(0f, bob, 0f);
            body.localRotation = bodyBaseRot;
        }

        if (head != null)
        {
            head.localRotation = headBaseRot * Quaternion.Euler(bob * 20f, 0f, 0f);
        }

        if (armLeft != null)
        {
            armLeft.localRotation = armLeftBaseRot * Quaternion.Euler(arm, 0f, 0f);
        }

        if (armRight != null)
        {
            armRight.localRotation = armRightBaseRot * Quaternion.Euler(-arm, 0f, 0f);
        }

        if (handLeft != null)
        {
            handLeft.localPosition = handLeftBasePos;
            handLeft.localRotation = handLeftBaseRot * Quaternion.Euler(arm * 0.5f, 0f, 0f);
        }

        if (handRight != null)
        {
            handRight.localPosition = handRightBasePos;
            handRight.localRotation = handRightBaseRot * Quaternion.Euler(-arm * 0.5f, 0f, 0f);
        }

        if (legLeft != null)
        {
            legLeft.localRotation = legLeftBaseRot;
        }

        if (legRight != null)
        {
            legRight.localRotation = legRightBaseRot;
        }
    }

    private void ApplyWalkPose(float timeValue)
    {
        float cycle = Mathf.Sin(timeValue * walkCycleSpeed);
        float cycleOpposite = Mathf.Sin((timeValue * walkCycleSpeed) + Mathf.PI);

        float leftLeg = cycle * walkLegSwing;
        float rightLeg = cycleOpposite * walkLegSwing;

        float leftArm = cycleOpposite * walkArmSwing;
        float rightArm = cycle * walkArmSwing;

        float bob = Mathf.Abs(cycle) * walkBodyBob;

        if (body != null)
        {
            body.localPosition = bodyBasePos + new Vector3(0f, bob, 0f);
            body.localRotation = bodyBaseRot;
        }

        if (head != null)
        {
            head.localRotation = headBaseRot * Quaternion.Euler(-bob * 50f, 0f, 0f);
        }

        if (legLeft != null)
        {
            legLeft.localRotation = legLeftBaseRot * Quaternion.Euler(leftLeg, 0f, 0f);
        }

        if (legRight != null)
        {
            legRight.localRotation = legRightBaseRot * Quaternion.Euler(rightLeg, 0f, 0f);
        }

        if (armLeft != null)
        {
            armLeft.localRotation = armLeftBaseRot * Quaternion.Euler(leftArm, 0f, 0f);
        }

        if (armRight != null)
        {
            armRight.localRotation = armRightBaseRot * Quaternion.Euler(rightArm, 0f, 0f);
        }

        if (handLeft != null)
        {
            handLeft.localPosition = handLeftBasePos;
            handLeft.localRotation = handLeftBaseRot * Quaternion.Euler(leftArm * 0.6f, 0f, 0f);
        }

        if (handRight != null)
        {
            handRight.localPosition = handRightBasePos;
            handRight.localRotation = handRightBaseRot * Quaternion.Euler(rightArm * 0.6f, 0f, 0f);
        }
    }

    public void PlayPunch(bool useLeftHand)
    {
        isMeleeAttacking = false;
        punchingWithLeftHand = useLeftHand;
        punchStartTime = elapsedTime;
        isPunching = true;
        sampleTimer = 1f / Mathf.Max(1f, animationFps);
    }

    public void PlayMeleeAttack(bool useLeftHand)
    {
        isPunching = false;
        meleeAttackingWithLeftHand = useLeftHand;
        meleeAttackStartTime = elapsedTime;
        isMeleeAttacking = true;
        sampleTimer = 1f / Mathf.Max(1f, animationFps);
    }

    private void ApplyPunchPose()
    {
        if (!isPunching)
        {
            return;
        }

        float duration = Mathf.Max(0.01f, punchDuration);
        float progress = (elapsedTime - punchStartTime) / duration;
        if (progress >= 1f)
        {
            isPunching = false;
            return;
        }

        // The hand reaches out and returns while the rest of the current pose is untouched.
        float reach = Mathf.Sin(progress * Mathf.PI);
        Transform punchingHand = punchingWithLeftHand ? handLeft : handRight;
        Vector3 basePosition = punchingWithLeftHand ? handLeftBasePos : handRightBasePos;
        Quaternion baseRotation = punchingWithLeftHand ? handLeftBaseRot : handRightBaseRot;

        if (punchingHand != null)
        {
            punchingHand.localPosition = basePosition + Vector3.forward * (reach * punchReach);
            punchingHand.localRotation = baseRotation * Quaternion.Euler(-reach * punchHandTilt, 0f, 0f);
        }
    }

    private void ApplyMeleeAttackPose()
    {
        if (!isMeleeAttacking)
        {
            return;
        }

        float duration = Mathf.Max(0.01f, meleeAttackDuration);
        float progress = (elapsedTime - meleeAttackStartTime) / duration;
        if (progress >= 1f)
        {
            isMeleeAttacking = false;
            return;
        }

        // Lift the weapon hand, strike it downward, then recover to the current pose.
        float swing;
        float handHeight;
        if (progress < 0.3f)
        {
            float windupProgress = progress / 0.3f;
            swing = Mathf.Lerp(0f, -meleeWindupAngle, windupProgress);
            handHeight = Mathf.Lerp(0f, meleeHandLift, windupProgress);
        }
        else if (progress < 0.65f)
        {
            float strikeProgress = (progress - 0.3f) / 0.35f;
            swing = Mathf.Lerp(-meleeWindupAngle, meleeSwingAngle, strikeProgress);
            handHeight = Mathf.Lerp(meleeHandLift, -meleeHandDrop, strikeProgress);
        }
        else
        {
            float recoveryProgress = (progress - 0.65f) / 0.35f;
            swing = Mathf.Lerp(meleeSwingAngle, 0f, recoveryProgress);
            handHeight = Mathf.Lerp(-meleeHandDrop, 0f, recoveryProgress);
        }

        float sideSign = meleeAttackingWithLeftHand ? -1f : 1f;
        Transform attackingArm = meleeAttackingWithLeftHand ? armLeft : armRight;
        Transform attackingHand = meleeAttackingWithLeftHand ? handLeft : handRight;
        Quaternion armBaseRotation = meleeAttackingWithLeftHand ? armLeftBaseRot : armRightBaseRot;
        Quaternion handBaseRotation = meleeAttackingWithLeftHand ? handLeftBaseRot : handRightBaseRot;
        Vector3 handBasePosition = meleeAttackingWithLeftHand ? handLeftBasePos : handRightBasePos;

        if (attackingArm != null)
        {
            attackingArm.localRotation = armBaseRotation * Quaternion.Euler(
                swing,
                sideSign * meleeSideAngle * Mathf.Sin(progress * Mathf.PI),
                0f);
        }

        if (attackingHand != null)
        {
            attackingHand.localPosition = handBasePosition + Vector3.up * handHeight;
            attackingHand.localRotation = handBaseRotation * Quaternion.Euler(
                swing * 0.35f,
                0f,
                sideSign * meleeHandTilt * Mathf.Sin(progress * Mathf.PI));
        }
    }
}
