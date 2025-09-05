using UnityEngine;

public class ObjectViewer : MonoBehaviour
{
    public Transform target;        // 회전/확대할 대상 오브젝트
    public float rotationSpeed = 5f;
    public float zoomSpeed = 2f;
    public float minZoom = 2f;
    public float maxZoom = 10f;

    private Vector3 previousMousePos;
    private Camera cam;

    void Start()
    {
        cam = Camera.main; // 또는 ObjectCamera 직접 할당
    }

    void Update()
    {
        // 회전
        if (Input.GetMouseButton(0))
        {
            Vector3 delta = Input.mousePosition - previousMousePos;
            target.Rotate(Vector3.up, -delta.x * rotationSpeed * Time.deltaTime, Space.World);
            target.Rotate(Vector3.right, delta.y * rotationSpeed * Time.deltaTime, Space.World);
        }

        // 확대/축소
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        cam.transform.position += cam.transform.forward * scroll * zoomSpeed;
        float dist = Vector3.Distance(cam.transform.position, target.position);
        if (dist < minZoom) cam.transform.position = target.position - cam.transform.forward * minZoom;
        if (dist > maxZoom) cam.transform.position = target.position - cam.transform.forward * maxZoom;

        previousMousePos = Input.mousePosition;
    }
}
