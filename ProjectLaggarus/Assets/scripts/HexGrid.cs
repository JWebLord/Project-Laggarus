using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class HexGrid : MonoBehaviour {

    public int cellCountX = 20, cellCountZ = 15;//размер грида в клетках

    int chunkCountX, chunkCountZ;

    public HexGridChunk chunkPrefab;//префаб под скрипт чанка
    HexGridChunk[] chunks;//массив чанков

    public HexCell cellPrefab; //префаб под скрипт ячейки
    HexCell[] cells; //Массив ячеек (под весь грид)

    public Text cellLabelPrefab; //префаб текста
    //Canvas gridCanvas; //канвас для вывода координат
    //HexMesh hexMesh; //generic mesh (скрипт)

    public Texture2D noiseSource;//шум для деформации тайлов

    public int seed;

    HexCellPriorityQueue searchFrontier;//лист очередей приоритета поиска пути
    int searchFrontierPhase;
    HexCell currentPathFrom, currentPathTo, currentPathReach;
    bool currentPathExists;//Существование пути

    public HexUnit unitPrefab;
    public List<HexUnit> units = new List<HexUnit>();//лист юнитов

    HexCellShaderData cellShaderData;//информация для шейдера

    public bool wrapping;//Бесшовность грида

    Transform[] columns;//столбцы трансформов чанков для реализации бесшовности

    int currentCenterColumnIndex = -1;//индекс центрального столбца


    void Awake()
    {
        HexMetrics.noiseSource = noiseSource;
        HexMetrics.InitializeHashGrid(seed);
        HexUnit.unitPrefab = unitPrefab;
        cellShaderData = gameObject.AddComponent<HexCellShaderData>();
        cellShaderData.Grid = this;
        CreateMap(cellCountX, cellCountZ, wrapping);
    }

    private void OnEnable()
    {
        if (!HexMetrics.noiseSource)
        {
            HexMetrics.noiseSource = noiseSource;
            HexMetrics.InitializeHashGrid(seed);
            HexUnit.unitPrefab = unitPrefab;
            HexMetrics.wrapSize = wrapping ? cellCountX : 0;
            ResetVisibility();
        }
    }

    public bool CreateMap(int x, int z, bool wrapping)
    {
        //размер карты должен быть кратен размерам чанка
        if (
            x <= 0 || x % HexMetrics.chunkSizeX != 0 ||
            z <= 0 || z % HexMetrics.chunkSizeZ != 0
        )
        {
            Debug.LogError("Unsupported map size.");
            return false;
        }

        ClearPath();
        ClearUnits();

        if (columns != null)
        {
            for (int i = 0; i < columns.Length; i++)
            {
                Destroy(columns[i].gameObject);
            }
        }
        cellCountX = x;
        cellCountZ = z;
        this.wrapping = wrapping;
        currentCenterColumnIndex = -1;

        HexMetrics.wrapSize = wrapping ? cellCountX : 0;

        chunkCountX = cellCountX / HexMetrics.chunkSizeX;
        chunkCountZ = cellCountZ / HexMetrics.chunkSizeZ;

        cellShaderData.Initialize(cellCountX, cellCountZ);

        CreateChunks();
        CreateCells();

        return true;
    }

    void CreateChunks()
    {
        columns = new Transform[chunkCountX];
        for (int x = 0; x < chunkCountX; x++)
        {
            columns[x] = new GameObject("Column").transform;
            columns[x].SetParent(transform, false);
        }

        chunks = new HexGridChunk[chunkCountX * chunkCountZ];

        for (int z = 0, i = 0; z < chunkCountZ; z++)
        {
            for (int x = 0; x < chunkCountX; x++)
            {
                HexGridChunk chunk = chunks[i++] = Instantiate(chunkPrefab);
                chunk.transform.SetParent(columns[x], false);
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

    /// <summary>
    /// Центрировать грид
    /// </summary>
    /// <param name="xPosition"></param>
    public void CenterMap(float xPosition)
    {
        int centerColumnIndex = (int)
            (xPosition / (HexMetrics.innerDiameter * HexMetrics.chunkSizeX));

        if (centerColumnIndex == currentCenterColumnIndex)
        {
            return;
        }
        currentCenterColumnIndex = centerColumnIndex;

        int minColumnIndex = centerColumnIndex - chunkCountX / 2;
        int maxColumnIndex = centerColumnIndex + chunkCountX / 2;

        //перемещение столбцов относительно центра
        Vector3 position;
        position.y = position.z = 0f;
        for (int i = 0; i < columns.Length; i++)
        {
            if (i < minColumnIndex)
            {
                position.x = chunkCountX *
                    (HexMetrics.innerDiameter * HexMetrics.chunkSizeX);
            }
            else if (i > maxColumnIndex)
            {
                position.x = chunkCountX *
                    -(HexMetrics.innerDiameter * HexMetrics.chunkSizeX);
            }
            else
            {
                position.x = 0f;
            }
            columns[i].localPosition = position;
        }
    }

    /// <summary>
    /// Получить ячейку по локальным координатам
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public HexCell GetCell(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        return GetCell(coordinates);
    }

    /// <summary>
    /// Получить ячейку по координатам смещения
    /// </summary>
    /// <param name="xOffset"></param>
    /// <param name="zOffset"></param>
    /// <returns></returns>
    public HexCell GetCell(int xOffset, int zOffset)
    {
        return cells[xOffset + zOffset * cellCountX];
    }

    /// <summary>
    /// Получить ячейку по индексу
    /// </summary>
    /// <param name="cellIndex"></param>
    /// <returns></returns>
    public HexCell GetCell(int cellIndex)
    {
        return cells[cellIndex];
    }

    void CreateCell(int x, int z, int i) //создает ячейку
    {
        Vector3 position; //вычисление позиции клетки
        position.x = (x + z * 0.5f - z / 2) * HexMetrics.innerDiameter;   //кубические координаты в локальные
        position.y = 0f;
        position.z = z * (HexMetrics.outerRadius * 1.5f);

        HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);//спавним префаб гексагона
        cell.transform.localPosition = position;
        cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);//перевод координат из смещения в кубические (в локальной переменной)
        cell.Index = i;
        cell.ColumnIndex = x / HexMetrics.chunkSizeX;
        cell.ShaderData = cellShaderData;

        //скрытие края карты(если не включен врап)
        if (wrapping)
        {
            cell.Explorable = z > 0 && z < cellCountZ - 1;
        }
        else
        {
            cell.Explorable =
                x > 0 && z > 0 && x < cellCountX - 1 && z < cellCountZ - 1;
        }

        if (x > 0)
        {
            cell.SetNeighbor(HexDirection.W, cells[i - 1]);
            if (wrapping && x == cellCountX - 1)
            {
                cell.SetNeighbor(HexDirection.E, cells[i - x]);
            }
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
                else if (wrapping)
                {
                    cell.SetNeighbor(HexDirection.SW, cells[i - 1]);
                }
            }
            else
            {
                cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX]);
                if (x < cellCountX - 1)
                {
                    cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX + 1]);
                }
                else if (wrapping)
                {
                    cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX * 2 + 1]);
                }
            }
        }

        Text label = Instantiate<Text>(cellLabelPrefab);//спавн текста из префаба
        label.rectTransform.anchoredPosition =
            new Vector2(position.x, position.z);
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

    /// <summary>
    /// Сохранение грида
    /// </summary>
    /// <param name="writer"></param>
    public void Save(BinaryWriter writer)
    {
        writer.Write(cellCountX);
        writer.Write(cellCountZ);
        writer.Write(wrapping);

        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Save(writer);
        }

        writer.Write(units.Count);
        for (int i = 0; i < units.Count; i++)
        {
            units[i].Save(writer);
        }
    }

    /// <summary>
    /// Загрузка грида
    /// </summary>
    /// <param name="reader"></param>
    public void Load(BinaryReader reader, int header)
    {
        ClearPath();
        ClearUnits();

        //загрзка ландшафта(формат 0 и выше)

        //нулевой формат формат
        int x = 20, z = 15;
        //первый формат
        if (header >= 1)
        {
            x = reader.ReadInt32();
            z = reader.ReadInt32();
        }

        bool wrapping = header >= 5 ? reader.ReadBoolean() : false;

        //Заново карту при одинаковых параметрах грида не генерим
        if (x != cellCountX || z != cellCountZ || this.wrapping != wrapping)
        {
            if (!CreateMap(x, z, wrapping))
            {
                return;
            }
        }

        bool originalImmediateMode = cellShaderData.ImmediateMode;
        //отключение зон частичной видимости во время движения для уменьшения нагрузки во время загрузки
        cellShaderData.ImmediateMode = true;

        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Load(reader, header);
        }
        for (int i = 0; i < chunks.Length; i++)
        {
            chunks[i].Refresh();
        }

        //загрузка юнитов (формат 2 и выше)
        if (header >= 2)
        {
            int unitCount = reader.ReadInt32();
            for (int i = 0; i < unitCount; i++)
            {
                HexUnit.Load(reader, this);
            }
        }

        cellShaderData.ImmediateMode = originalImmediateMode;
    }

    /// <summary>
    /// Расчитать расстояние до клетки(только для выделенных юнитов)
    /// </summary>
    /// <param name="fromCell"></param>
    /// <param name="toCell"></param>
    public void FindPath(HexCell fromCell, HexCell toCell, HexUnit unit, bool showPath = false)
    {
        ClearPath();
        currentPathFrom = fromCell;
        currentPathTo = toCell;
        //собственно сам поиск
        if(unit.Speed >= 1)
        {
            currentPathExists = Search(fromCell, toCell, unit);
            ShowPath(unit.Speed, showPath);
        }
    }

    /// <summary>
    /// Поиск пути
    /// </summary>
    /// <param name="fromCell"></param>
    /// <param name="toCell"></param>
    /// <returns></returns>
    bool Search(HexCell fromCell, HexCell toCell, HexUnit unit)
    {
        int speed = unit.Speed;

        searchFrontierPhase += 2;

        if (searchFrontier == null)
        {
            searchFrontier = new HexCellPriorityQueue();
        }
        else
        {
            searchFrontier.Clear();
        }

        fromCell.SearchPhase = searchFrontierPhase;
        fromCell.Distance = 0;
        searchFrontier.Enqueue(fromCell);
        while (searchFrontier.Count > 0)
        {
            Debug.Log(toCell);
            HexCell current = searchFrontier.Dequeue();
            current.SearchPhase += 1;

            //выход из подпрограммы, если достигнута клетка, путь до которой нужно найти и подсветка пути
            if (current == toCell)
            {
                Debug.Log("Успешно");
                return true;
            }

            //Сколько ходов требуется для достижения клетки(не той, которая выделена)
            int currentTurn = (current.Distance - 1) / speed;

            // Поиск в ширину
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);
                //если соседа нет или есть еще не достигнутые клетки, то в первую очередь обходим их
                if (neighbor == null || neighbor.SearchPhase > searchFrontierPhase)
                {
                    continue;
                }

                if (!unit.IsValidDestination(neighbor))
                {
                    continue;
                }
                int moveCost = unit.GetMoveCost(current, neighbor, d);
                if (moveCost < 0)
                {
                    continue;
                }

                //расстояние до клетки назначения
                int distance = current.Distance + moveCost;
                //кол-во ходов до клетки назначения
                int turn = (distance - 1) / speed;

                if (turn > currentTurn)
                {
                    distance = turn * speed + moveCost;
                }

                if (neighbor.SearchPhase < searchFrontierPhase)
                {
                    neighbor.SearchPhase = searchFrontierPhase;
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    try
                    {
                        neighbor.SearchHeuristic = neighbor.coordinates.DistanceTo(toCell.coordinates);
                        searchFrontier.Enqueue(neighbor);
                    }
                    catch (System.Exception)
                    {
                        Debug.Log(neighbor);
                        Debug.Log(toCell);
                        throw;
                    }
                    
                }
                else if (distance < neighbor.Distance)
                {
                    int oldPriority = neighbor.SearchPriority;
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    searchFrontier.Change(neighbor, oldPriority);
                }
                
            }
        }
        return false;
    }

    /// <summary>
    /// Показать путь от клетки до клетки в ходах
    /// </summary>
    /// <param name="speed"></param>
    void ShowPath(int speed, bool showPath)
    {
        if (currentPathExists)
        {
            HexCell current = currentPathTo;
            //currentPathReach = null;
            int previousTurn = 0;//чтобы вернуться к клетке, которой можно достигнуть
            while (current != currentPathFrom)
            {

                int turn = (current.Distance - 1) / speed;
                current.SetLabel(turn.ToString());
                if (previousTurn >= 1 && turn == 0)
                {
                    currentPathReach = current;
                    if (showPath) current.EnableHighlight(Color.green);//Ту клетку, которой можем достичь подсвечиваем зеленым
                }
                else
                {
                    if (showPath) current.EnableHighlight(Color.white);
                }
                previousTurn = turn;
                current = current.PathFrom;
            }
        }

        if (showPath) currentPathFrom.EnableHighlight(Color.blue);
        if (showPath) currentPathTo.EnableHighlight(Color.red);
        
        if (currentPathReach == null && currentPathTo.Distance <= speed)
        {
            currentPathReach = currentPathTo;
        }
        else if(!currentPathReach)
        {
            ClearPath();//сначала очистка пути, иначе не будет работать
            currentPathExists = false;//временный костыль, пока не сделал нормальное сохраниение пути
        }
    }

    /// <summary>
    /// Снять выделение с клеток пути
    /// </summary>
    public void ClearPath()
    {
        if (currentPathExists)
        {
            HexCell current = currentPathTo;
            while (current != currentPathFrom)
            {
                current.SetLabel(null);
                current.DisableHighlight();
                current = current.PathFrom;
            }
            current.DisableHighlight();
            currentPathExists = false;
        }
        else if (currentPathFrom)
        {
            currentPathFrom.DisableHighlight();
            currentPathTo.DisableHighlight();
        }
        currentPathFrom = currentPathTo = currentPathReach = null;
    }


    /// <summary>
    /// Убрать все юниты
    /// </summary>
    void ClearUnits()
    {
        for (int i = 0; i < units.Count; i++)
        {
            units[i].Die();
        }
        units.Clear();
    }

    /// <summary>
    /// Добавить юнит
    /// </summary>
    /// <param name="unit"></param>
    /// <param name="location"></param>
    /// <param name="orientation"></param>
    public void AddUnit(HexUnit unit, HexCell location, float orientation)
    {
        units.Add(unit);
        unit.Grid = this;
        unit.Location = location;
        unit.Orientation = orientation;
    }

    /// <summary>
    /// Установить родителем трансформ столбца чанков
    /// </summary>
    /// <param name="child"></param>
    /// <param name="columnIndex"></param>
    public void MakeChildOfColumn(Transform child, int columnIndex)
    {
        child.SetParent(columns[columnIndex], false);
    }

    public void RemoveUnit(HexUnit unit)
    {
        units.Remove(unit);
        unit.Die();
    }

    /// <summary>
    /// Получить ячейку по лучу
    /// </summary>
    /// <param name="ray"></param>
    /// <returns></returns>
    public HexCell GetCell(Ray ray)
    {
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            return GetCell(hit.point);
        }
        return null;
    }

    public bool HasPath
    {
        get
        {
            return currentPathExists;
        }
    }

    /// <summary>
    /// Получить путь
    /// </summary>
    /// <returns></returns>
    public List<HexCell> GetPath()
    {
        if (!currentPathExists)
        {
            return null;
        }
        List<HexCell> path = ListPool<HexCell>.Get();
        for (HexCell c = currentPathReach; c != currentPathFrom; c = c.PathFrom)
        {
            path.Add(c);
        }
        path.Add(currentPathFrom);
        path.Reverse();
        return path;
    }

    /// <summary>
    /// Получить видимые ячейки
    /// </summary>
    /// <param name="fromCell"></param>
    /// <param name="range"></param>
    /// <returns></returns>
    List<HexCell> GetVisibleCells(HexCell fromCell, int range)//по большей части это сильно измененный поиск пути
    {
        List<HexCell> visibleCells = ListPool<HexCell>.Get();
        searchFrontierPhase += 2;

        if (searchFrontier == null)
        {
            searchFrontier = new HexCellPriorityQueue();
        }
        else
        {
            searchFrontier.Clear();
        }


        range += fromCell.ViewElevation;
        fromCell.SearchPhase = searchFrontierPhase;
        fromCell.Distance = 0;
        searchFrontier.Enqueue(fromCell);
        HexCoordinates fromCoordinates = fromCell.coordinates;
        while (searchFrontier.Count > 0)
        {
            HexCell current = searchFrontier.Dequeue();
            current.SearchPhase += 1;

            visibleCells.Add(current);

            // Поиск в ширину
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);
                //если соседа нет или есть еще не достигнутые клетки, то в первую очередь обходим их и не обходим клетки которые не могут быть исследованы
                if (neighbor == null || neighbor.SearchPhase > searchFrontierPhase ||
                    !neighbor.Explorable)
                {
                    continue;
                }

                //расстояние до клетки назначения
                int distance = current.Distance + 1;
                //ограничение на видимость по расстоянию до клетки и высоте
                if (distance + neighbor.ViewElevation > range ||
                    distance > fromCoordinates.DistanceTo(neighbor.coordinates)
                )
                    {
                    continue;
                }

                if (distance > range)
                {
                    continue;
                }

                if (neighbor.SearchPhase < searchFrontierPhase)
                {
                    neighbor.SearchPhase = searchFrontierPhase;
                    neighbor.Distance = distance;
                    neighbor.SearchHeuristic = 0;
                    searchFrontier.Enqueue(neighbor);
                }
                else if (distance < neighbor.Distance)
                {
                    int oldPriority = neighbor.SearchPriority;
                    neighbor.Distance = distance;
                    searchFrontier.Change(neighbor, oldPriority);
                }

            }
        }
        return visibleCells;
    }
    /// <summary>
    /// Увеличить видимость в зоне от ячейки
    /// </summary>
    /// <param name="fromCell"></param>
    /// <param name="range"></param>
    public void IncreaseVisibility(HexCell fromCell, int range)
    {
        List<HexCell> cells = GetVisibleCells(fromCell, range);
        for (int i = 0; i < cells.Count; i++)
        {
            cells[i].IncreaseVisibility();
        }
        ListPool<HexCell>.Add(cells);
    }
    /// <summary>
    /// Уменьшить видимость в зоне от ячейки
    /// </summary>
    /// <param name="fromCell"></param>
    /// <param name="range"></param>
    public void DecreaseVisibility(HexCell fromCell, int range)
    {
        List<HexCell> cells = GetVisibleCells(fromCell, range);
        for (int i = 0; i < cells.Count; i++)
        {
            cells[i].DecreaseVisibility();
        }
        ListPool<HexCell>.Add(cells);
    }

    /// <summary>
    /// Сброс видимости на карте
    /// </summary>
    public void ResetVisibility()
    {
        //ресет
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].ResetVisibility();
        }
        //перерасчёт
        for (int i = 0; i < units.Count; i++)
        {
            HexUnit unit = units[i];
            IncreaseVisibility(unit.Location, unit.VisionRange);
        }
    }
}


