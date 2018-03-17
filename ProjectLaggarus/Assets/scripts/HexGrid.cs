using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour {

    public int chunkCountX = 4, chunkCountZ = 3;//размер грида в чанках

    private int cellCountX, cellCountZ;

    public Color defaultColor = Color.white;//цвета

    public HexGridChunk chunkPrefab;//префаб под скрипт чанка
    HexGridChunk[] chunks;//массив чанков

    public HexCell cellPrefab; //префаб под скрипт ячейки
    HexCell[] cells; //Массив ячеек (под весь грид)

    public Text cellLabelPrefab; //префаб текста
    //Canvas gridCanvas; //канвас для вывода координат
    //HexMesh hexMesh; //generic mesh (скрипт)

    public Texture2D noiseSource;//шум для деформации тайлов

    void Awake()
    {
        HexMetrics.noiseSource = noiseSource;

        cellCountX = chunkCountX * HexMetrics.chunkSizeX;
        cellCountZ = chunkCountZ * HexMetrics.chunkSizeZ;

        CreateChunks();
        CreateCells();
    }

    void CreateChunks()
    {
        chunks = new HexGridChunk[chunkCountX * chunkCountZ];

        for (int z = 0, i = 0; z < chunkCountZ; z++)
        {
            for (int x = 0; x < chunkCountX; x++)
            {
                HexGridChunk chunk = chunks[i++] = Instantiate(chunkPrefab);
                chunk.transform.SetParent(transform);
            }
        }
    }

    private void CreateCells()
    {
        cells = new HexCell[cellCountZ * cellCountX];//глобальный массив под весь грид

        for (int z = 0, i = 0; z < cellCountZ; z++)
        {
            for (int x = 0; x < cellCountX; x++)
            {
                CreateCell(x, z, i++);
            }
        }
    }

    private void OnEnable()
    {
        HexMetrics.noiseSource = noiseSource;
    }

    public HexCell GetCell(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;//последнее прибавляем т.к x каждые 2 уровня -1
        return cells[index];
    }

    void CreateCell(int x, int z, int i) //создает ячейку
    {
        Vector3 position; //вычисление позиции клетки
        position.x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f);   //кубические координаты в локальные
        position.y = 0f;
        position.z = z * (HexMetrics.outerRadius * 1.5f);

        HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);//спавним префаб гексагона
        cell.transform.localPosition = position;
        cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);//перевод координат из смещения в кубические (в локальной переменной)
        cell.Color = defaultColor;//установка цвета

        if (x > 0)
        {
            cell.SetNeighbor(HexDirection.W, cells[i - 1]);
        }
        if (z > 0)
        {
            if ((z % 2) == 0)
            {
                cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX]);
                if (x > 0)
                {
                    cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX - 1]);
                }
            }
            else
            {
                cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX]);
                if (x < cellCountX - 1)
                {
                    cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX + 1]);
                }
            }
        }

        Text label = Instantiate<Text>(cellLabelPrefab);//спавн текста из префаба
        label.rectTransform.anchoredPosition =
            new Vector2(position.x, position.z);
        label.text = cell.coordinates.ToStringOnSeparateLines();//текст=координаты
        cell.uiRect = label.rectTransform;

        cell.Elevation = 0;

        AddCellToChunk(x, z, cell);
    }

    void AddCellToChunk(int x, int z, HexCell cell)
    {
        int chunkX = x / HexMetrics.chunkSizeX;//делением нацело находим координаты нужного чанка
        int chunkZ = z / HexMetrics.chunkSizeZ;
        HexGridChunk chunk = chunks[chunkX + chunkZ * chunkCountX];

        int localX = x - chunkX * HexMetrics.chunkSizeX;//нахождение локальных координат внутри конкретного чанка
        int localZ = z - chunkZ * HexMetrics.chunkSizeZ;
        chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, cell);
    }

    public HexCell GetCell(HexCoordinates coordinates)
    {
        int z = coordinates.Z;
        if (z < 0 || z >= cellCountZ)
        {
            return null;
        }
        int x = coordinates.X + z / 2;
        if (x < 0 || x >= cellCountX)
        {
            return null;
        }
        return cells[x + z * cellCountX];
    }

    public void ShowUI(bool visible)
    {
        for (int i = 0; i < chunks.Length; i++)
        {
            chunks[i].ShowUI(visible);
        }
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
