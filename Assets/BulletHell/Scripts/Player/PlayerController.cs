using UnityEngine;
using BulletHell.Simulation.Core;

public class PlayerController : MonoBehaviour
{
    [Header("Simulation")]
    [SerializeField] private bool simulationDriven;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f; // 移动速度
    [SerializeField] private float screenPadding = 0.2f; // 屏幕边界padding

    [Header("Shoot")]
    [SerializeField] private float fireInterval = 0.3f; // 射击间隔
    [SerializeField] private float bulletSpeed = 5f; // 子弹速度
    [SerializeField] private float bulletDamage = 1f; // 子弹伤害
    [SerializeField] private float lifetime = 30f; // 子弹伤害

    private Camera _cam;
    private float _fireTimer;
    private int _inputTick;

    // Start is called before the first frame update
    void Start()
    {
        _cam = Camera.main;
        _fireTimer = fireInterval;
        _inputTick = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (simulationDriven)
            return;

        if (_cam == null)
            _cam = Camera.main;

        float dt = Time.deltaTime;

        InputFrame inputFrame = SampleCurrentInputFrame();
        TickMove(dt, inputFrame);
        TickShoot(dt, inputFrame);
        _inputTick++;
    }

    public void SetSimulationDriven(bool isSimulationDriven)
    {
        simulationDriven = isSimulationDriven;
    }

    private void TickMove(float dt, InputFrame inputFrame)
    {
        float h = inputFrame.MoveX;
        float v = inputFrame.MoveY;

        // 归一化，防止斜角方向移动速度过快
        Vector2 input = new Vector2(h, v);
        if (input.sqrMagnitude > 1f)
            input.Normalize();

        Vector3 pos = transform.position;
        pos.x += input.x * moveSpeed * dt;
        pos.y += input.y * moveSpeed * dt;

        transform.position = ClampToScreen(pos);
    }

    private void TickShoot(float dt, InputFrame inputFrame)
    {
        _fireTimer += dt; // 累加时间

        if (!inputFrame.FirePressed)
            return;

        float interval = fireInterval <= 0f ? 0f : fireInterval; // 防止间隔为0
        if (_fireTimer < interval)
            return;

        Vector2 dir = new Vector2(inputFrame.AimX / 1000f, inputFrame.AimY / 1000f);
        if (dir.sqrMagnitude > 1f)
            dir.Normalize();

        if (dir.sqrMagnitude < 0.0001f)
            return;

        _fireTimer = 0f;

        if (BulletManager.Instance == null)
        {
            return;
        }

        BulletManager.Instance.SpawnBullet(
            transform.position,
            dir,
            bulletSpeed,
            bulletDamage,
            lifetime,
            BulletFaction.Player
        );
    }

    private Vector3 ScreenToWorldPoint(Vector3 mouse)
    {
        float screenToWorldZ = _cam.orthographic ? 0f : Mathf.Abs(_cam.transform.position.z);
        Vector3 worldPoint = _cam.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, screenToWorldZ));
        return worldPoint;
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

    public InputFrame SampleCurrentInputFrame(int tick)
    {
        if (_cam == null)
            _cam = Camera.main;

        sbyte moveX = (sbyte)Mathf.RoundToInt(Mathf.Clamp(Input.GetAxisRaw("Horizontal"), -1f, 1f));
        sbyte moveY = (sbyte)Mathf.RoundToInt(Mathf.Clamp(Input.GetAxisRaw("Vertical"), -1f, 1f));
        bool firePressed = Input.GetMouseButton(0);

        short aimX = 0;
        short aimY = 0;

        if (_cam != null)
        {
            Vector3 mousePos = Input.mousePosition;
            Vector3 worldMouse = ScreenToWorldPoint(mousePos);
            Vector3 direction = worldMouse - transform.position;

            if (direction.sqrMagnitude > 0.0001f)
            {
                Vector3 normalizedDir = direction.normalized;
                aimX = (short)(normalizedDir.x * 1000f);
                aimY = (short)(normalizedDir.y * 1000f);
            }
        }

        return new InputFrame(tick, moveX, moveY, aimX, aimY, firePressed);
    }

    private InputFrame SampleCurrentInputFrame()
    {
        return SampleCurrentInputFrame(_inputTick);
    }
}
