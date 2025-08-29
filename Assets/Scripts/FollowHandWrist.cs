using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
using Unity.XR.CoreUtils;

public class FollowHandWrist : MonoBehaviour
{
    public bool isLeft = true;
    public XROrigin xrOrigin;          // XR Origin (XR Rig) 드래그 할당
    public Vector3 positionOffset;     // 위치 보정 (미터 단위)
    public Vector3 eulerOffset;        // 회전 보정 (도 단위)

    XRHandSubsystem hands;

    void OnEnable()
    {
        var loader = XRGeneralSettings.Instance?.Manager?.activeLoader;
        hands = loader?.GetLoadedSubsystem<XRHandSubsystem>();
    }

    void Update()
    {
        if (hands == null || !hands.running) return;

        var hand = isLeft ? hands.leftHand : hands.rightHand;
        if (!hand.isTracked) return;

        var wrist = hand.GetJoint(XRHandJointID.Wrist);
        if (!wrist.TryGetPose(out Pose pose)) return;

        // Origin의 Transform을 가져오기
        Transform originTransform = xrOrigin ? xrOrigin.Origin.transform : null;

        Vector3 worldPos;
        Quaternion worldRot;

        if (originTransform)
        {
            // 트래킹 스페이스 좌표 → 월드 좌표
            worldPos = originTransform.TransformPoint(pose.position);
            worldRot = originTransform.rotation * pose.rotation;
        }
        else
        {
            worldPos = pose.position;
            worldRot = pose.rotation;
        }

        // 오프셋 적용
        worldPos += worldRot * positionOffset;
        worldRot *= Quaternion.Euler(eulerOffset);

        transform.SetPositionAndRotation(worldPos, worldRot);
    }
}
