using UnityEngine;
using Oculus.Interaction.HandGrab;
using System.Reflection;

public class HandPoseGhostApplier : MonoBehaviour
{
    public HandGrabPose sourcePose;
    public Transform[] index; // 테스트용

    private Quaternion[] _jointRotations;

    void Awake()
    {
        if (sourcePose && sourcePose.HandPose != null)
        {
            var hp = sourcePose.HandPose;
            // private 필드 "_jointRotations" 꺼내오기
            var f = typeof(HandPose).GetField("_jointRotations", BindingFlags.Instance | BindingFlags.NonPublic);
            if (f != null)
                _jointRotations = f.GetValue(hp) as Quaternion[];
        }
    }

    void LateUpdate()
    {
        if (_jointRotations == null) return;

        // 예시: index 손가락 1,2,3에 포즈 적용
        for (int i = 0; i < index.Length; i++)
        {
            int poseIdx = 1 * 4 + (i + 1); // Index finger=1, joint1~3
            if (poseIdx < _jointRotations.Length)
                index[i].localRotation = _jointRotations[poseIdx];
        }
    }
}
