using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

public class GrabPoseGate : MonoBehaviour
{
    [Header("Recorder에서 만든 Pose의 ActiveState(예: ActiveStateGroup)를 연결")]
    [SerializeField] private MonoBehaviour grabPoseState; // must implement IActiveState
    private IActiveState _grabPoseActive;

    [Header("이 손 오브젝트의 HandGrabInteractor")]
    [SerializeField] private HandGrabInteractor handGrabInteractor;

    private bool _wasActive;
    private bool _warnedOnce;

    private void Awake()
    {
        // 누락 시 자동 탐색(같은 오브젝트/자식)
        if (handGrabInteractor == null)
            handGrabInteractor = GetComponentInChildren<HandGrabInteractor>(true);

        if (grabPoseState != null)
            _grabPoseActive = grabPoseState as IActiveState;

        Debug.Log($"[GrabPoseGate] Awake on {name}. " +
                  $"Interactor={(handGrabInteractor ? handGrabInteractor.name : "NULL")}, " +
                  $"PoseState={(grabPoseState ? grabPoseState.name : "NULL")}");
    }

    private void Start()
    {
        Debug.Log("[GrabPoseGate] Start() 호출됨 — Update가 도는지 확인 중");
    }

    private void Update()
    {
        // 업데이트가 실제 도는지 1회만 알림
        if (!_warnedOnce)
        {
            _warnedOnce = true;
            Debug.Log("[GrabPoseGate] Update() 동작 중");
        }

        if (_grabPoseActive == null)
        {
            // 연결 누락 경고(1회)
            if (grabPoseState == null)
                Debug.LogWarning("[GrabPoseGate] grabPoseState가 비어있습니다. " +
                                 "Recording 프리팹의 ActiveState(IActiveState)를 인스펙터에 연결하세요.");
            else
                Debug.LogWarning("[GrabPoseGate] grabPoseState는 있으나 IActiveState로 캐스팅되지 않습니다. " +
                                 "ActiveStateGroup/ShapeRecognizerActiveState 등 IActiveState 구현인지 확인하세요.");
            return;
        }

        if (handGrabInteractor == null)
        {
            Debug.LogWarning("[GrabPoseGate] HandGrabInteractor 참조가 없습니다. " +
                             "오른손(HandInteractorsRight) 하위의 HandGrabInteractor를 연결하세요.");
            return;
        }

        bool isActive = _grabPoseActive.Active;

        // 인터랙터 on/off
        if (handGrabInteractor.enabled != isActive)
            handGrabInteractor.enabled = isActive;

        // 상태 변화시만 로그
        if (isActive && !_wasActive)
            Debug.Log(" Grab Pose 감지됨! (Interactor ON)");
        else if (!isActive && _wasActive)
            Debug.Log(" Grab Pose 해제됨! (Interactor OFF)");

        _wasActive = isActive;
    }
}
