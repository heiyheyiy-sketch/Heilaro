using UnityEngine;

public class ScalpelDepthIndicator : MonoBehaviour
{


    // 파일 상단 필드 근처에 추가
    public enum DepthAxis { MinusUp, MinusForward, MinusRight, CustomLocal }
    public DepthAxis depthAxis = DepthAxis.MinusUp;
    public Vector3 customLocalDir = new Vector3(0, -1, 0); // Custom일 때 로컬 방향

    [Header("Refs")]
    public Transform bladeTip;       // 칼끝 (필수)
    public LayerMask bodyLayer;      // 인체 레이어

    [Header("Depth Settings (meters)")]
    public float targetDepth = 0.008f; // 8mm
    public float maxDepth = 0.020f;    // 20mm

    [Header("Outline Settings")]
    public bool useQuickOutline = true;
    public Component outlineComponent;  // QuickOutline/Outline 등 직접 드래그
    public Color shallowColor = Color.red;
    public Color targetColor = Color.blue;
    public float outlineWidthNear = 4f;
    public float outlineWidthFar = 6f;

    [Header("Debug")]
    public bool debugLogs = true;       // 콘솔 로그
    public bool debugDraw = true;       // Scene 뷰 디버그 라인/기즈모
    [Range(0.05f, 2f)] public float logThrottleSec = 0.25f;  // 로그 간격
    private float _nextLog;

    // 내부
    private Renderer[] rends;
    private MaterialPropertyBlock mpb;
    private readonly int _OutlineColor = Shader.PropertyToID("_OutlineColor");
    private readonly int _OutlineWidth = Shader.PropertyToID("_OutlineWidth");

    private bool inside = false;
    private Vector3 entryPointWS;
    private Vector3 surfaceNormalWS;
    private float smoothDepth = 0f;

    // ─────────────────────────────────────────────────────────────────────────────
    // 클래스 내부 아무 곳에 유틸 함수 추가
    private Vector3 GetCastDir()
    {
        switch (depthAxis)
        {
            case DepthAxis.MinusForward: return -bladeTip.forward;
            case DepthAxis.MinusRight: return -bladeTip.right;
            case DepthAxis.CustomLocal: return bladeTip.TransformDirection(customLocalDir.normalized);
            default: return -bladeTip.up;
        }
    }


    void Awake()
    {
        if (bladeTip == null)
        {
            LogErr("bladeTip 이 할당되어 있지 않습니다. 칼끝 Transform을 드래그해 주세요.");
        }

        if (!useQuickOutline)
        {
            rends = GetComponentsInChildren<Renderer>(true);
            if (rends == null || rends.Length == 0) LogWarn("자식 Renderer를 찾지 못했습니다. Visuals 아래에 Renderer가 있는지 확인하세요.");
            mpb = new MaterialPropertyBlock();
        }
        else
        {
            if (outlineComponent == null) LogWarn("useQuickOutline=true 이지만 outlineComponent 가 비어 있습니다. Visuals의 외곽선 컴포넌트를 드래그하세요.");
        }

        if (bodyLayer.value == 0)
        {
            LogWarn("bodyLayer 가 비어 있습니다. 인체 레이어를 지정하세요.");
        }

        SetOutlineEnabled(false);
    }


    void Update()
    {
        if (bladeTip == null) return;

        Vector3 dir = GetCastDir().normalized;

        // 디버그 시축 보기
        if (debugDraw)
        {
            Debug.DrawRay(bladeTip.position, dir * 0.05f, Color.green);   // +dir
            Debug.DrawRay(bladeTip.position, -dir * 0.05f, Color.cyan);    // -dir (실제 캐스트)
        }

        // 진단: 모든 레이어 대상으로 RaycastAll (원인 파악용)
        float diagMax = 0.1f; // 10cm 여유
        var allHits = Physics.RaycastAll(bladeTip.position, -dir, diagMax, ~0, QueryTriggerInteraction.Collide);
        if (allHits.Length > 0 && debugLogs)
        {
            System.Array.Sort(allHits, (a, b) => a.distance.CompareTo(b.distance));
            int n = Mathf.Min(3, allHits.Length);
            for (int i = 0; i < n; i++)
            {
                var h = allHits[i];
                Debug.Log($"[ScalpelDepth][ALL] hit {i}: {h.collider.name}, layer={LayerMask.LayerToName(h.collider.gameObject.layer)}, dist={h.distance:F4}", this);
            }
        }
        else if (debugLogs)
        {
            Debug.Log("[ScalpelDepth][ALL] no objects hit on ANY layer.", this);
        }

        //  본 로직: 표면 안쪽에서 시작하지 않도록 '뒤로' 백오프
        float backoff = 0.01f; // 1cm 뒤에서 쏨
        Vector3 origin = bladeTip.position + dir * backoff; // '밖쪽'에서 표면쪽(-dir)으로 쏨
        float castDist = Mathf.Max(maxDepth + backoff + 0.02f, 0.05f); // 여유

        if (debugDraw) Debug.DrawRay(origin, -dir * castDist, Color.blue);

        bool hitBody = Physics.Raycast(
            origin: origin,
            direction: -dir,
            hitInfo: out RaycastHit hit,
            maxDistance: castDist,
            layerMask: bodyLayer,
            queryTriggerInteraction: QueryTriggerInteraction.Collide // ⬅ 트리거도 맞게
        );

        if (hitBody)
        {
            if (!inside)
            {
                inside = true;
                entryPointWS = hit.point;
                surfaceNormalWS = hit.normal;
                SetOutlineEnabled(true);
                if (debugDraw) Debug.DrawRay(hit.point, hit.normal * 0.02f, Color.yellow, 0.5f);
                Log($"ENTER body. entry={entryPointWS:F3}, normal={surfaceNormalWS:F3}");
            }

            float rawDepth = -Vector3.Dot((bladeTip.position - entryPointWS), surfaceNormalWS);
            rawDepth = Mathf.Max(0f, rawDepth);
            smoothDepth = Mathf.Lerp(smoothDepth, rawDepth, 0.25f);

            float t = Mathf.InverseLerp(0f, targetDepth, Mathf.Clamp(smoothDepth, 0f, targetDepth));
            Color c = Color.Lerp(shallowColor, targetColor, t);
            float w = Mathf.Lerp(outlineWidthNear, outlineWidthFar,
                                 Mathf.InverseLerp(0f, maxDepth, Mathf.Min(smoothDepth, maxDepth)));

            ApplyOutline(c, w);
            Log($"HIT depth(raw/smth)={rawDepth:F4}/{smoothDepth:F4}, t={t:F2}, width={w:F2}");
        }
        else
        {
            if (inside)
            {
                inside = false;
                smoothDepth = 0f;
                SetOutlineEnabled(false);
                Log("EXIT body. outline OFF");
            }
            else
            {
                Log("NO HIT(Body). Check layerMask/direction/backoff/distance/PhysicsMatrix");
            }
        }
    }


    private void ApplyOutline(Color color, float width)
    {
        if (useQuickOutline && outlineComponent != null)
        {
            var t = outlineComponent.GetType();
            var cProp = t.GetProperty("OutlineColor");
            var wProp = t.GetProperty("OutlineWidth");
            if (cProp != null) cProp.SetValue(outlineComponent, color, null);
            if (wProp != null) wProp.SetValue(outlineComponent, width, null);
        }
        else
        {
            if (rends == null) return;
            foreach (var r in rends)
            {
                if (r == null) continue;
                if (mpb == null) mpb = new MaterialPropertyBlock();
                r.GetPropertyBlock(mpb);
                mpb.SetColor(_OutlineColor, color);
                mpb.SetFloat(_OutlineWidth, width);
                r.SetPropertyBlock(mpb);
            }
        }
    }

    private void SetOutlineEnabled(bool on)
    {
        if (useQuickOutline && outlineComponent != null)
        {
            if (outlineComponent is Behaviour b) b.enabled = on;
            else
            {
                var t = outlineComponent.GetType();
                var eProp = t.GetProperty("enabled");
                if (eProp != null) eProp.SetValue(outlineComponent, on, null);
            }
        }
        else
        {
            if (rends == null) return;
            foreach (var r in rends)
            {
                if (r == null) continue;
                if (mpb == null) mpb = new MaterialPropertyBlock();
                r.GetPropertyBlock(mpb);
                mpb.SetFloat(_OutlineWidth, on ? outlineWidthNear : 0f);
                r.SetPropertyBlock(mpb);
            }
        }
    }

    // ── Debug helpers ────────────────────────────────────────────────────────────
    private void Log(string msg)
    {
        if (!debugLogs) return;
        if (Time.unscaledTime < _nextLog) return;
        _nextLog = Time.unscaledTime + logThrottleSec;
        Debug.Log($"[ScalpelDepth] {msg}", this);
    }
    private void LogWarn(string msg)
    {
        if (!debugLogs) return;
        Debug.LogWarning($"[ScalpelDepth] {msg}", this);
    }
    private void LogErr(string msg)
    {
        Debug.LogError($"[ScalpelDepth] {msg}", this);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!debugDraw || bladeTip == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(bladeTip.position, 0.002f);
        // entry/normal 표시
        if (inside)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(entryPointWS, entryPointWS + surfaceNormalWS * 0.02f);
        }
    }
#endif
}
