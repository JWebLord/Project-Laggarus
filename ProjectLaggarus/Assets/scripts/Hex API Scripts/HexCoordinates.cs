using UnityEngine;
using System.IO;

[System.Serializable]
public struct HexCoordinates
{

    [SerializeField]
    private int x, z;

    public int X
    {
        get
        {
            return x;
        }
    }

    public int Y
    {
        get
        {
            return -X - Z;
        }
    }

    public int Z
    {
        get
        {
            return z;
        }
    }

    public HexCoordinates(int x, int z)
    {
        this.x = x;
        this.z = z;
    }

    public static HexCoordinates FromOffsetCoordinates(int x, int z)
    {
        int xCube = x - (z - (z % 2)) / 2;
        int zCube = z;
        return new HexCoordinates(xCube, zCube);
    }

    public override string ToString()
    {
        return "(" +
            X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ")";
    }

    public string ToStringOnSeparateLines()
    {
        return X.ToString() + "\n" + Y.ToString() + "\n" + Z.ToString();
    }

    public static HexCoordinates FromPosition(Vector3 position)//локальные координаты в смещения
    {
        float x = position.x / (HexMetrics.innerRadius * 2f);//x относительно локальной координаты x
        float y = -x;//x - зеркально к y
        float offset = position.z / (HexMetrics.outerRadius * 3f);//z относительно локальной z
        x -= offset;
        y -= offset;
        int iX = Mathf.RoundToInt(x);
        int iY = Mathf.RoundToInt(y);
        int iZ = Mathf.RoundToInt(-x - y);

        if (iX + iY + iZ != 0)//проверка сходимости координат к нулю
        {
            float dX = Mathf.Abs(x - iX);//вычисление дельты по координатам
            float dY = Mathf.Abs(y - iY);
            float dZ = Mathf.Abs(-x - y - iZ);

            if (dX > dY && dX > dZ)//вычисление координат на основе дельты и отзеркаливания
            {
                iX = -iY - iZ;
            }
            else if (dZ > dY)
            {
                iZ = -iX - iY;
            }
            //Debug.LogWarning("rounding error!"); //показывает, если произошло округление координат
        }

        return new HexCoordinates(iX, iZ);
    }

    /// <summary>
    /// Расчитать расстояние до клетки
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public int DistanceTo(HexCoordinates other)
    {
        return
            ((x < other.x ? other.x - x : x - other.x) +
            (Y < other.Y ? other.Y - Y : Y - other.Y) +
            (z < other.z ? other.z - z : z - other.z)) / 2;
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(x);
        writer.Write(z);
    }

    public static HexCoordinates Load(BinaryReader reader)
    {
        HexCoordinates c;
        c.x = reader.ReadInt32();
        c.z = reader.ReadInt32();
        return c;
    }
}