using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
using Unity.XR.CoreUtils;

public class FollowHandWrist : MonoBehaviour
{
    public bool isLeft = true;
    public XROrigin xrOrigin;          // XR Origin (XR Rig) �巡�� �Ҵ�
    public Vector3 positionOffset;     // ��ġ ���� (���� ����)
    public Vector3 eulerOffset;        // ȸ�� ���� (�� ����)

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

        // Origin�� Transform�� ��������
        Transform originTransform = xrOrigin ? xrOrigin.Origin.transform : null;

        Vector3 worldPos;
        Quaternion worldRot;

        if (originTransform)
        {
            // Ʈ��ŷ �����̽� ��ǥ �� ���� ��ǥ
            worldPos = originTransform.TransformPoint(pose.position);
            worldRot = originTransform.rotation * pose.rotation;
        }
        else
        {
            worldPos = pose.position;
            worldRot = pose.rotation;
        }

        // ������ ����
        worldPos += worldRot * positionOffset;
        worldRot *= Quaternion.Euler(eulerOffset);

        transform.SetPositionAndRotation(worldPos, worldRot);
    }
}
