using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour {

    public int width = 6;
    public int height = 6;

    public Color defaultColor = Color.white;//цвета

    public HexCell cellPrefab; //префаб ячейки
    HexCell[] cells; //Массив ячеек

    public Text cellLabelPrefab; //префаб текста
    Canvas gridCanvas; //канвас для вывода координат
    HexMesh hexMesh; //generic mesh

    void Awake()
    {
        gridCanvas = GetComponentInChildren<Canvas>();//канвас под текст
        hexMesh = GetComponentInChildren<HexMesh>();//меш для тайлов

        cells = new HexCell[height * width];//массив h*w под карту

        for (int z = 0, i = 0; z < height; z++) //генерация карты
        {
            for (int x = 0; x < width; x++)
            {
                CreateCell(x, z, i++);
            }
        }
    }

    void Start()
    {
        hexMesh.Triangulate(cells);
    }

    public void Refresh()//перерисовка меша
    {
        hexMesh.Triangulate(cells);
    }

    public HexCell GetCell(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        int index = coordinates.X + coordinates.Z * width + coordinates.Z / 2;//последнее прибавляем т.к x каждые 2 уровня -1
        return cells[index];
    }

    void CreateCell(int x, int z, int i) //создает ячейку
    {
        Vector3 position; //вычисление позиции клетки
        position.x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f);   //кубические координаты в локальные
        position.y = 0f;
        position.z = z * (HexMetrics.outerRadius * 1.5f);

        HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);//спавним префаб гексагона
        cell.transform.SetParent(transform, false);//отвязывание от родителей
        cell.transform.localPosition = position;
        cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);//перевод координат из смещения в кубические (в локальной переменной)
        cell.color = defaultColor;//установка цвета

        if (x > 0)
        {
            cell.SetNeighbor(HexDirection.W, cells[i - 1]);
        }
        if (z > 0)
        {
            if ((z % 2) == 0)
            {
                cell.SetNeighbor(HexDirection.SE, cells[i - width]);
                if (x > 0)
                {
                    cell.SetNeighbor(HexDirection.SW, cells[i - width - 1]);
                }
            }
            else
            {
                cell.SetNeighbor(HexDirection.SW, cells[i - width]);
                if (x < width - 1)
                {
                    cell.SetNeighbor(HexDirection.SE, cells[i - width + 1]);
                }
            }
        }

        Text label = Instantiate<Text>(cellLabelPrefab);//спавн текста из префаба
        label.rectTransform.SetParent(gridCanvas.transform, false);
        label.rectTransform.anchoredPosition =
            new Vector2(position.x, position.z);
        label.text = cell.coordinates.ToStringOnSeparateLines();//текст=координаты
        cell.uiRect = label.rectTransform;
    }


}

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
            Debug.LogWarning("rounding error!");
        }

        return new HexCoordinates(iX, iZ);
    }
}
