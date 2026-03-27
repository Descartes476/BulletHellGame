using UnityEngine;
using BulletHell.Simulation.Core;

public class PlayerController : MonoBehaviour
{
    [Header("Simulation")]
    [SerializeField] private bool simulationDriven;

    private Camera _cam;
    private int _inputTick;

    // Start is called before the first frame update
    void Start()
    {
        _cam = Camera.main;
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
        _inputTick++;
    }

    public void SetSimulationDriven(bool isSimulationDriven)
    {
        simulationDriven = isSimulationDriven;
    }

    private Vector3 ScreenToWorldPoint(Vector3 mouse)
    {
        float screenToWorldZ = _cam.orthographic ? 0f : Mathf.Abs(_cam.transform.position.z);
        Vector3 worldPoint = _cam.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, screenToWorldZ));
        return worldPoint;
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
