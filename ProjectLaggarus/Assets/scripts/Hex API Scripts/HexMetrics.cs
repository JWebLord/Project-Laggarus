using UnityEngine;

public static class HexMetrics
{
    public const float outerToInner = 0.866025404f;//соотношения радиусов
    public const float innerToOuter = 1f / outerToInner;

    public const float outerRadius = 10f;//внешний радиус
    public const float innerRadius = outerRadius * outerToInner;//внутренний радиус sqrt(3)/2

    public const float solidFactor = 0.8f;
    public const float blendFactor = 1f - solidFactor;

    public const float elevationStep = 3f;

    public const int terracesPerSlope = 2;//кол-во уступов
    public const int terraceSteps = terracesPerSlope * 2 + 1;//кол-во "соединений" для отрисовки
    public const float horizontalTerraceStepSize = 1f / terraceSteps;
    public const float verticalTerraceStepSize = 1f / (terracesPerSlope + 1);//для расчета по y надо прибавлять + 1, чтобы получалось нормальное деление
    public const float cellPerturbStrength = 4f;//сила изменений тайлов
    public const float noiseScale = 0.003f;//множитель размерности текстуры для выборки шума
    public const float elevationPerturbStrength = 1.5f;//множитель разности ячеек в высоте

    public const float streamBedElevationOffset = -1.75f;//глубина канала под малые реки
    public const float waterElevationOffset = -0.5f;//возвышение воды над поверхностью

    public const float waterFactor = 0.6f; // размер клетки воды(соединения растянутся)
    public const float waterBlendFactor = 1f - waterFactor;

    public const int chunkSizeX = 5, chunkSizeZ = 5; //размер чанка

    public const int hashGridSize = 256;//размер хэш-сетки
    public const float hashGridScale = 0.25f;//множитель для хэш-сетки

    static HexHash[] hashGrid;//массив хэш-сетки

    public const float wallHeight = 4f;//высота стен(сгенерированных
    public const float wallThickness = 0.75f;//толщина стен
    public const float wallElevationOffset = verticalTerraceStepSize;//вертикальное смещение стен в землю(на склонах)
    public const float wallTowerThreshold = 0.5f;//Частота спавна башен
    public const float wallYOffset = -1f;//Смещение башен по высоте

    public const float bridgeDesignLength = 7f;//длина мостов(модели) и т.д.
    public const float bridgeDesignHeight = 1f;
    public const float bridgeDesignWidth = 3f;

    public static Color[] colors;//набор цветов


    public static Texture2D noiseSource;

    static Vector3[] corners = {
        new Vector3(0f, 0f, outerRadius),                    //верхний
        new Vector3(innerRadius, 0f, 0.5f * outerRadius),    //верхний правый
        new Vector3(innerRadius, 0f, -0.5f * outerRadius),   //нижний правый
        new Vector3(0f, 0f, -outerRadius),                   //нижний
        new Vector3(-innerRadius, 0f, -0.5f * outerRadius),  //нижний левый
        new Vector3(-innerRadius, 0f, 0.5f * outerRadius),   //нижний правый
        new Vector3(0f , 0f, outerRadius)                    //верхний(для корректной отрисовки)
    };

    public static Vector3 GetFirstCorner(HexDirection direction)
    {
        return corners[(int)direction];
    }

    public static Vector3 GetSecondCorner(HexDirection direction)
    {
        return corners[(int)direction + 1];
    }

    public static Vector3 GetFirstSolidCorner(HexDirection direction)
    {
        return corners[(int)direction] * solidFactor;
    }

    public static Vector3 GetSecondSolidCorner(HexDirection direction)
    {
        return corners[(int)direction + 1] * solidFactor;
    }

    public static Vector3 GetFirstWaterCorner(HexDirection direction)
    {
        return corners[(int)direction] * waterFactor;
    }

    public static Vector3 GetSecondWaterCorner(HexDirection direction)
    {
        return corners[(int)direction + 1] * waterFactor;
    }

    public static Vector3 GetBridge(HexDirection direction)
    {
        return (corners[(int)direction] + corners[(int)direction + 1]) *
            blendFactor;
    }

    public static Vector3 GetWaterBridge(HexDirection direction)
    {
        return (corners[(int)direction] + corners[(int)direction + 1]) *
            waterBlendFactor;
    }

    public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step)//интерполяция склонов
    {
        float h = step * HexMetrics.horizontalTerraceStepSize;
        a.x += (b.x - a.x) * h;
        a.z += (b.z - a.z) * h;
        float v = ((step + 1) / 2) * HexMetrics.verticalTerraceStepSize;
        a.y += (b.y - a.y) * v;
        return a;
    }

    public static Color TerraceLerp(Color a, Color b, int step)//интерполяция для цвета
    {
        float h = step * HexMetrics.horizontalTerraceStepSize;
        return Color.Lerp(a, b, h);
    }

    public static HexEdgeType GetEdgeType(int elevation1, int elevation2)
    {
        if (elevation1 == elevation2) { return HexEdgeType.Flat; }
        int deltaElevation = elevation2 - elevation1;
        if((deltaElevation == 1) || (deltaElevation == -1)) { return HexEdgeType.Slope; }
        return HexEdgeType.Cliff;
    }

    public static Vector3 GetSolidEdgeMiddle(HexDirection direction)
    {
        return
            (corners[(int)direction] + corners[(int)direction + 1]) *
            (0.5f * solidFactor);
    }
    /// <summary>
    /// Шум для конкретных координат
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public static Vector4 SampleNoise(Vector3 position)
    {
        return noiseSource.GetPixelBilinear(position.x * noiseScale, position.y * noiseScale);
    }
    /// <summary>
    /// Изменить вектор с помощью шума
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public static Vector3 Perturb(Vector3 position)//изменение вершины шумом
    {
        Vector4 sample = SampleNoise(position);
        position.x += (sample.x * 2f - 1f) * cellPerturbStrength;
        position.z += (sample.z * 2f - 1f) * cellPerturbStrength;
        return position;
    }

    /// <summary>
    /// Инициализация хэш-сетки
    /// </summary>
    public static void InitializeHashGrid(int seed)
    {
        hashGrid = new HexHash[hashGridSize * hashGridSize];
        Random.State currentState = Random.state;
        Random.InitState(seed);
        for (int i = 0; i < hashGrid.Length; i++)
        {
            hashGrid[i] = HexHash.Create();
        }
        Random.state = currentState;
    }
    /// <summary>
    /// Взять значение хэш-сетки для конкретной позиции
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public static HexHash SampleHashGrid(Vector3 position)
    {
        int x = (int)(position.x * hashGridScale) % hashGridSize;
        if (x < 0)
        {
            x += hashGridSize;
        }
        int z = (int)(position.z * hashGridScale) % hashGridSize;
        if (z < 0)
        {
            z += hashGridSize;
        }
        return hashGrid[x + z * hashGridSize];
    }
    /// <summary>
    /// Шансы зданий для разных типов застройки
    /// </summary>
    static float[][] featureThresholds = {
        new float[] {0.0f, 0.0f, 0.4f},
        new float[] {0.0f, 0.4f, 0.6f},
        new float[] {0.4f, 0.6f, 0.8f}
    };

    public static float[] GetFeatureThresholds(int level)
    {
        return featureThresholds[level];
    }

    /// <summary>
    /// Смещение координат для толщины стен
    /// </summary>
    /// <param name="near"></param>
    /// <param name="far"></param>
    /// <returns></returns>
    public static Vector3 WallThicknessOffset(Vector3 near, Vector3 far)
    {
        Vector3 offset;
        offset.x = far.x - near.x;
        offset.y = 0f;
        offset.z = far.z - near.z;
        return offset.normalized * (wallThickness * 0.5f);
    }

    /// <summary>
    /// Смещение стен по высоте
    /// </summary>
    /// <param name="near"></param>
    /// <param name="far"></param>
    /// <returns></returns>
    public static Vector3 WallLerp(Vector3 near, Vector3 far)
    {
        near.x += (far.x - near.x) * 0.5f;
        near.z += (far.z - near.z) * 0.5f;
        float v =
            near.y < far.y ? wallElevationOffset : (1f - wallElevationOffset);
        near.y += (far.y - near.y) * v + wallYOffset;
        return near;
    }
}
