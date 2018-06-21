using UnityEngine;

public class HexMapCamera : MonoBehaviour
{
    static HexMapCamera instance;

    float zoom = 1f;
    public float stickMinZoom, stickMaxZoom;
    public float swivelMinZoom, swivelMaxZoom;

    public float moveSpeedMinZoom, moveSpeedMaxZoom;

    public float rotationSpeed;
    float rotationAngle;

    public HexGrid grid;

    Transform swivel, stick;

    bool validate;

    void Awake()
    {
        swivel = transform.GetChild(0);
        stick = swivel.GetChild(0);
    }

    void OnEnable()
    {
        instance = this;
        validate = true;
    }

    void Update()
    {
        float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
        if (zoomDelta != 0f)
        {
            AdjustZoom(zoomDelta);
        }

        float rotationDelta = Input.GetAxis("Rotation");
        if (rotationDelta != 0f)
        {
            AdjustRotation(rotationDelta);
        }

        float xDelta = Input.GetAxis("Horizontal");
        float zDelta = Input.GetAxis("Vertical");
        if (xDelta != 0f || zDelta != 0f)
        {
            AdjustPosition(xDelta, zDelta);
        }
        //костыль для начальной валидации позиции и не только
        if(validate) { ValidatePosition(); }
    }

    void AdjustZoom(float delta)
    {
        zoom = Mathf.Clamp01(zoom + delta);

        float distance = Mathf.Lerp(stickMinZoom, stickMaxZoom, zoom);
        stick.localPosition = new Vector3(0f, 0f, distance);

        float angle = Mathf.Lerp(swivelMinZoom, swivelMaxZoom, zoom);
        swivel.localRotation = Quaternion.Euler(angle, 0f, 0f);
    }

    void AdjustRotation(float delta)
    {
        rotationAngle += delta * rotationSpeed * Time.deltaTime;
        if (rotationAngle < 0f)
        {
            rotationAngle += 360f;
        }
        else if (rotationAngle >= 360f)
        {
            rotationAngle -= 360f;
        }
        transform.localRotation = Quaternion.Euler(0f, rotationAngle, 0f);
    }

    void AdjustPosition(float xDelta, float zDelta)
    {//обязательно считать направление относительно вращения!
        Vector3 direction = transform.localRotation * new Vector3(xDelta, 0f, zDelta).normalized;//нормализация вектора, чтобы движение по диагонали было со стандартной скорости
        float damping = Mathf.Max(Mathf.Abs(xDelta), Mathf.Abs(zDelta));//дампинг для мгновенного изменения скорости
        float distance =
            Mathf.Lerp(moveSpeedMinZoom, moveSpeedMaxZoom, zoom) *
            damping * Time.deltaTime;//скорость зависит от приближения

        Vector3 position = transform.localPosition;
        position += direction * distance;
        transform.localPosition = grid.wrapping ? WrapPosition(position) : ClampPosition(position);
    }

    Vector3 ClampPosition(Vector3 position)//ограничивает перемещение камеры размерами карты
    {
        float xMax = (grid.cellCountX - 0.5f) * HexMetrics.innerDiameter;
        position.x = Mathf.Clamp(position.x, 0f, xMax);

        float zMax = (grid.cellCountZ - 1) * (1.5f * HexMetrics.outerRadius);
        position.z = Mathf.Clamp(position.z, 0f, zMax);

        return position;
    }

    /// <summary>
    /// Позиция при склеивании карты
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    Vector3 WrapPosition(Vector3 position)
    {
        float width = grid.cellCountX * HexMetrics.innerDiameter;
        while (position.x < 0f)
        {
            position.x += width;
        }
        while (position.x > width)
        {
            position.x -= width;
        }

        float zMax = (grid.cellCountZ - 1) * (1.5f * HexMetrics.outerRadius);
        position.z = Mathf.Clamp(position.z, 0f, zMax);

        grid.CenterMap(position.x);
        return position;
    }

    public static bool Locked
    {
        set
        {
            instance.enabled = !value;
        }
    }

    public static void ValidatePosition()
    {
        instance.AdjustPosition(0f, 0f);
    }
}