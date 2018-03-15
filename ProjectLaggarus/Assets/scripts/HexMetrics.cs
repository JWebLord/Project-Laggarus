using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HexMetrics
{
    public const float outerRadius = 10f;//внешний радиус
    public const float innerRadius = outerRadius * 0.866025404f;//внутренний радиус sqrt(3)/2

    public const float solidFactor = 0.75f;
    public const float blendFactor = 1f - solidFactor;

    public const float elevationStep = 5f;

    public const int terracesPerSlope = 2;//кол-во уступов
    public const int terraceSteps = terracesPerSlope * 2 + 1;//кол-во "соединений" для отрисовки
    public const float horizontalTerraceStepSize = 1f / terraceSteps;
    public const float verticalTerraceStepSize = 1f / (terracesPerSlope + 1);//для расчета по y надо прибавлять + 1, чтобы получалось нормальное деление

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

    public static Vector3 GetBridge(HexDirection direction)
    {
        return (corners[(int)direction] + corners[(int)direction + 1]) *
            blendFactor;
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
}
