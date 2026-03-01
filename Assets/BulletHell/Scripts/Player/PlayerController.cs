using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f; // 移动速度
    [SerializeField] private float screenPadding = 0.2f; // 屏幕边界padding

    [Header("Shoot")]
    [SerializeField] private float fireInterval = 0.1f; // 射击间隔
    [SerializeField] private float bulletSpeed = 15f; // 子弹速度
    [SerializeField] private float bulletDamage = 1f; // 子弹伤害

    private Camera _cam;
    private float _fireTimer;

    // Start is called before the first frame update
    void Start()
    {
        _cam = Camera.main;
        _fireTimer = fireInterval;
    }

    // Update is called once per frame
    void Update()
    {
        if (_cam == null)
            _cam = Camera.main;

        float dt = Time.deltaTime;

        TickMove(dt);
        TickShoot(dt);
    }

    private void TickMove(float dt)
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // 归一化，防止斜角方向移动速度过快
        Vector2 input = new Vector2(h, v);
        if (input.sqrMagnitude > 1f)
            input.Normalize();

        Vector3 pos = transform.position;
        pos.x += input.x * moveSpeed * dt;
        pos.y += input.y * moveSpeed * dt;

        transform.position = ClampToScreen(pos);
    }

    private void TickShoot(float dt)
    {
        _fireTimer += dt; // 累加时间

        if (!Input.GetMouseButton(0))
            return;

        float interval = fireInterval <= 0f ? 0f : fireInterval; // 防止间隔为0
        if (_fireTimer < interval)
            return;

        Vector2 dir = GetAimDirection();
        if (dir.sqrMagnitude < 0.0001f)
            return; 
        
        _fireTimer = 0f;

        if (BulletManager.Instance == null)
        {
            Debug.LogWarning("BulletManager.Instance is null. Please add BulletManager to scene.");
            return;
        }

        BulletManager.Instance.SpawnBullet(
            transform.position,
            dir,
            bulletSpeed,
            bulletDamage,
            1.5f,
            BulletManager.BulletFaction.Player
        );
    }

    private Vector2 GetAimDirection()
    {
        if (_cam == null)
            return Vector2.zero;

        Vector3 mouse = Input.mousePosition;
        float screenToWorldZ = _cam.orthographic ? 0f : Mathf.Abs(_cam.transform.position.z);
        Vector3 world = _cam.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, screenToWorldZ));
        Vector2 from = new Vector2(transform.position.x, transform.position.y);
        Vector2 to = new Vector2(world.x, world.y);
        return to - from;
    }

    private Vector3 ClampToScreen(Vector3 pos)
    {
        if (_cam == null)
            return pos;

        if (_cam.orthographic)
        {
            float halfH = _cam.orthographicSize;
            float halfW = halfH * _cam.aspect;

            float minX = _cam.transform.position.x - halfW + screenPadding;
            float maxX = _cam.transform.position.x + halfW - screenPadding;
            float minY = _cam.transform.position.y - halfH + screenPadding;
            float maxY = _cam.transform.position.y + halfH - screenPadding;

            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            pos.z = 0f;
            return pos;
        }

        // 透视相机：用 viewport 近似限制在当前相机近平面投影范围
        Vector3 v = _cam.WorldToViewportPoint(pos);
        v.x = Mathf.Clamp01(v.x);
        v.y = Mathf.Clamp01(v.y);
        Vector3 clamped = _cam.ViewportToWorldPoint(v);
        clamped.z = 0f;
        return clamped;
    }
}
