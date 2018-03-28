using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class HexCell : MonoBehaviour {

    public HexCoordinates coordinates;//координаты ячейки(кубические)
    public RectTransform uiRect;//координаты для координат:) ячейки
    int terrainTypeIndex;//индекс типа поверхности ячейки
    public HexGridChunk chunk;//чанк к которому принадлежит ячейка
    private int elevation = int.MinValue;//высота ячейки(изначально -100500, чтобы произошла ее переотрисовка при создании)

    bool hasIncomingRiver, hasOutgoingRiver;//входят/выходят ли малые реки
    HexDirection incomingRiver, outgoingRiver;//направление рек

    [SerializeField]
    bool[] roads;//Массив присутствия дорог в направлениях
    int urbanLevel, farmLevel, plantLevel;//уровень застройки, ферм и лесов
    bool walled;//есть ли стены
    int specialIndex;//номер мегаструктуры в ячейке

    int distance;//расстояние от выбраной ячейки до этой
    public HexCell PathFrom { get; set; } //из какой ячейки проложен путь в эту
    public int SearchHeuristic { get; set; }//Для приоритета поиска
    public HexCell NextWithSamePriority { get; set; }//следующая ячейка с тем же приоритетом в поиске
    public int SearchPhase { get; set; } //Отслеживание положения клетки относительно границы поиска 0 - не достигнута 1 - на границе 2 - уже найден путь

    public int Distance
    {
        get
        {
            return distance;
        }
        set
        {
            distance = value;
        }
    }

    public int SpecialIndex
    {
        get
        {
            return specialIndex;
        }
        set
        {
            if (specialIndex != value && !HasRiver)
            {
                specialIndex = value;
                RemoveRoads();
                RefreshSelfOnly();
            }
        }
    }

    public bool IsSpecial
    {
        get
        {
            return specialIndex > 0;
        }
    }

    public bool Walled
    {
        get
        {
            return walled;
        }
        set
        {
            if (walled != value)
            {
                walled = value;
                Refresh();
            }
        }
    }

    public int Elevation//публичные методы для задания и считывания высоты ячейки
    {
        get
        {
            return elevation;
        }
        set
        {
            if (elevation == value)
            {
                return;
            }

            elevation = value;
            RefreshPosition();//обновить позиционирование клетки по высоте
            //проверки на то, чтобы реки текли правильно(по высотам)
            ValidateRivers();
            //Проверка на корректную отрисовку дорог
            for (int i = 0; i < roads.Length; i++)
            {
                if (roads[i] && GetElevationDifference((HexDirection)i) > 1)
                {
                    SetRoad(i, false);
                }
            }

            Refresh();
        }
    }

    void RefreshPosition()
    {
        Vector3 position = transform.localPosition;
        position.y = elevation * HexMetrics.elevationStep;
        position.y +=
            (HexMetrics.SampleNoise(position).y * 2f - 1f) *
            HexMetrics.elevationPerturbStrength;
        transform.localPosition = position;

        Vector3 uiPosition = uiRect.localPosition;
        uiPosition.z = -position.y;
        uiRect.localPosition = uiPosition;
    }

    public int UrbanLevel
    {
        get
        {
            return urbanLevel;
        }
        set
        {
            if (urbanLevel != value)
            {
                urbanLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public int FarmLevel
    {
        get
        {
            return farmLevel;
        }
        set
        {
            if (farmLevel != value)
            {
                farmLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public int PlantLevel
    {
        get
        {
            return plantLevel;
        }
        set
        {
            if (plantLevel != value)
            {
                plantLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    /// <summary>
    /// Наличие дороги в заданном направлении
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public bool HasRoadThroughEdge(HexDirection direction)
    {
        return roads[(int)direction];
    }
    /// <summary>
    /// Наличие дорог в клетке
    /// </summary>
    public bool HasRoads
    {
        get
        {
            for (int i = 0; i < roads.Length; i++)
            {
                if (roads[i])
                {
                    return true;
                }
            }
            return false;
        }
    }
    /// <summary>
    /// Получить позицию клетки
    /// </summary>
    public Vector3 Position
    {
        get
        {
            return transform.localPosition;
        }
    }

    public int TerrainTypeIndex
    {
        get
        {
            return terrainTypeIndex;
        }
        set
        {
            if (terrainTypeIndex != value)
            {
                terrainTypeIndex = value;
                Refresh();
            }
        }
    }

    public int WaterLevel
    {
        get
        {
            return waterLevel;
        }
        set
        {
            if (waterLevel == value)
            {
                return;
            }
            waterLevel = value;
            ValidateRivers();
            Refresh();
        }
    }
    [SerializeField]
    int waterLevel;

    public bool IsUnderwater
    {
        get
        {
            return waterLevel > elevation;
        }
    }

    /// <summary>
    /// Добавить дорогу в клетку в заданном направлении(РЕКОМЕНДУЕТСЯ)
    /// </summary>
    /// <param name="direction"></param>
    public void AddRoad(HexDirection direction)
    {
        if (
            !roads[(int)direction] && !HasRiverThroughEdge(direction) &&
            !IsSpecial && !GetNeighbor(direction).IsSpecial &&
            GetElevationDifference(direction) <= 1
        )
        {
            SetRoad((int)direction, true);
        }
    }
    /// <summary>
    /// Установить состояние дороги(есть/нет) в двух соседних клетках
    /// </summary>
    /// <param name="index"></param>
    /// <param name="state"></param>
    void SetRoad(int index, bool state)
    {
        roads[index] = state;
        neighbors[index].roads[(int)((HexDirection)index).Opposite()] = state;
        neighbors[index].RefreshSelfOnly();
        RefreshSelfOnly();
    }

    /// <summary>
    /// Удалить дороги в клетке и в соседней клетке в направлении этой клетки
    /// </summary>
    public void RemoveRoads()
    {
        for (int i = 0; i < neighbors.Length; i++)
        {
            if (roads[i])
            {
                roads[i] = false;
                neighbors[i].roads[(int)((HexDirection)i).Opposite()] = false;
                neighbors[i].RefreshSelfOnly();
                RefreshSelfOnly();
            }
        }
    }
    /// <summary>
    /// Получить разницу высот между клеткои и соседом в определенном направлении
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public int GetElevationDifference(HexDirection direction)
    {
        int difference = elevation - GetNeighbor(direction).elevation;
        return difference >= 0 ? difference : -difference;
    }

    //тут ReadOnly методы для рек
    /// <summary>
    /// Взять высоту расположения канала
    /// </summary>
    public float StreamBedY
    {
        get
        {
            return
                (elevation + HexMetrics.streamBedElevationOffset) *
                HexMetrics.elevationStep;
        }
    }

    public HexDirection RiverBeginOrEndDirection
    {
        get
        {
            return hasIncomingRiver ? incomingRiver : outgoingRiver;
        }
    }

    bool IsValidRiverDestination(HexCell neighbor)
    {
        return neighbor && (
            elevation >= neighbor.elevation || waterLevel == neighbor.elevation
        );
    }
    /// <summary>
    /// Наличие в тайле втекающей в нее реки
    /// </summary>
    public bool HasIncomingRiver
    {
        get
        {
            return hasIncomingRiver;
        }
    }
    /// <summary>
    /// Наличие в клетке вытекающей из нее реки
    /// </summary>
    public bool HasOutgoingRiver
    {
        get
        {
            return hasOutgoingRiver;
        }
    }
    /// <summary>
    /// Направление вытекающей из клетки реки
    /// </summary>
    public HexDirection IncomingRiver
    {
        get
        {
            return incomingRiver;
        }
    }
    /// <summary>
    /// Направление втекающей в клетку реки
    /// </summary>
    public HexDirection OutgoingRiver
    {
        get
        {
            return outgoingRiver;
        }
    }
    /// <summary>
    /// Наличие реки в клетке
    /// </summary>
    public bool HasRiver
    {
        get
        {
            return hasIncomingRiver || hasOutgoingRiver;
        }
    }
    /// <summary>
    /// Наличие в клетке начала или конца реки
    /// </summary>
    public bool HasRiverBeginOrEnd
    {
        get
        {
            return hasIncomingRiver != hasOutgoingRiver;
        }
    }
    /// <summary>
    /// Проверка, есть ли река в данном направлении
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public bool HasRiverThroughEdge(HexDirection direction)
    {
        return
            hasIncomingRiver && incomingRiver == direction ||
            hasOutgoingRiver && outgoingRiver == direction;
    }
    /// <summary>
    /// Получить высоту реки
    /// </summary>
    public float RiverSurfaceY
    {
        get
        {
            return
                (elevation + HexMetrics.waterElevationOffset) *
                HexMetrics.elevationStep;
        }
    }
    /// <summary>
    /// Уровень поверхности воды
    /// </summary>
    public float WaterSurfaceY
    {
        get
        {
            return
                (waterLevel + HexMetrics.waterElevationOffset) *
                HexMetrics.elevationStep;
        }
    }
    

    /// <summary>
    /// Удаление выходящей из клетки реки
    /// </summary>
    public void RemoveOutgoingRiver()
    {
        if (!hasOutgoingRiver)
        {
            return;
        }
        hasOutgoingRiver = false;//чистим исходную ячейку
        RefreshSelfOnly();//обновление только текущего чанка, чтобы не шел пересчет соседних, т.к. цвета не меняются

        HexCell neighbor = GetNeighbor(outgoingRiver);//чистим ячейку куда впадает река
        neighbor.hasIncomingRiver = false;
        neighbor.RefreshSelfOnly();
    }
    /// <summary>
    /// Удаление входящей в клетку реки
    /// </summary>
    public void RemoveIncomingRiver()
    {
        if (!hasIncomingRiver)
        {
            return;
        }
        hasIncomingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(incomingRiver);
        neighbor.hasOutgoingRiver = false;
        neighbor.RefreshSelfOnly();
    }
    /// <summary>
    /// Удаление рек на клетке(и входящих и выходящих)РЕКОМЕНДУЕТСЯ
    /// </summary>
    public void RemoveRiver()//просто удаляем любые реки на тайле
    {
        RemoveOutgoingRiver();
        RemoveIncomingRiver();
    }
    /// <summary>
    /// Установить выходящую из клетки реку
    /// </summary>
    /// <param name="direction"></param>Направление
    public void SetOutgoingRiver(HexDirection direction)
    {
        if (hasOutgoingRiver && outgoingRiver == direction)//если есть река -> выход
        {
            return;
        }
        HexCell neighbor = GetNeighbor(direction);//если есть соседа нет, или тайл ниже соседа -> выход
        if (!IsValidRiverDestination(neighbor))
        {
            return;
        }
        RemoveOutgoingRiver();//Удаление старого стока реки
        if (hasIncomingRiver && incomingRiver == direction)//если река раньше втекала из направления, то ее тоже удаляем
        {
            RemoveIncomingRiver();
        }
        hasOutgoingRiver = true;
        specialIndex = 0;
        outgoingRiver = direction;//ну и наконец ставим речку

        neighbor.RemoveIncomingRiver();//соседу назначаем текущую к нему реку
        neighbor.hasIncomingRiver = true;
        neighbor.specialIndex = 0;
        neighbor.incomingRiver = direction.Opposite();

        SetRoad((int)direction, false);
    }

    void ValidateRivers()
    {
        if (
            hasOutgoingRiver &&
            !IsValidRiverDestination(GetNeighbor(outgoingRiver))
        )
        {
            RemoveOutgoingRiver();
        }
        if (
            hasIncomingRiver &&
            !GetNeighbor(incomingRiver).IsValidRiverDestination(this)
        )
        {
            RemoveIncomingRiver();
        }
    }

    [SerializeField]
    HexCell[] neighbors;//сериализованые соседи, т.к. их мы будем иногда передавать

    void Refresh()//переотрисовка чанка(и соседних)
    {
        if (chunk)
        {
            chunk.Refresh();
            for (int i = 0; i < neighbors.Length; i++)//отрисовка чанков соседних с изменяемой ячейкой
            {
                HexCell neighbor = neighbors[i];
                if (neighbor != null && neighbor.chunk != chunk)
                {
                    neighbor.chunk.Refresh();
                }
            }
        }
    }

    void RefreshSelfOnly()//обновление только одного чанка
    {
        chunk.Refresh();
    }

    public HexCell GetNeighbor(HexDirection direction)//взять соседа
    {
        return neighbors[(int)direction];
    }

    public void SetNeighbor(HexDirection direction, HexCell cell)//назначить соседа ячейке
    {
        neighbors[(int)direction] = cell;
        cell.neighbors[(int)direction.Opposite()] = this;
    }

    public HexEdgeType GetEdgeType(HexDirection direction)//узнать тип угла в направлении
    {
        return HexMetrics.GetEdgeType(elevation, neighbors[(int)direction].elevation);
    }

    public HexEdgeType GetEdgeType(HexCell otherCell)//узнать тип угла относительно двух ячеек
    {
        return HexMetrics.GetEdgeType(elevation, otherCell.elevation);
    }

    /// <summary>
    /// Закрузка клетки
    /// </summary>
    /// <param name="writer"></param>
    public void Save(BinaryWriter writer)
    {
        //некоторые пишутся в байт, т.к. диапазон небольшой
        writer.Write((byte)terrainTypeIndex);
        writer.Write((byte)elevation);
        writer.Write((byte)waterLevel);
        writer.Write((byte)urbanLevel);
        writer.Write((byte)farmLevel);
        writer.Write((byte)plantLevel);
        writer.Write((byte)specialIndex);
        writer.Write(walled);

        //для рек пишу +128, если река существует, а оставшееся место под направление
        if (hasIncomingRiver)
        {
            writer.Write((byte)(incomingRiver + 128));
        }
        else
        {
            writer.Write((byte)0);
        }

        if (hasOutgoingRiver)
        {
            writer.Write((byte)(outgoingRiver + 128));
        }
        else
        {
            writer.Write((byte)0);
        }

        int roadFlags = 0;//дороги пишу смещением побитно
        for (int i = 0; i < roads.Length; i++)
        {
            if (roads[i])
            {
                roadFlags |= 1 << i;
            }
        }
        writer.Write((byte)roadFlags);
    }

    /// <summary>
    /// Сохранение клетки
    /// </summary>
    /// <param name="reader"></param>
    public void Load(BinaryReader reader)
    {
        //кодирование см. в Save
        terrainTypeIndex = reader.ReadByte();
        elevation = reader.ReadByte();
        RefreshPosition();
        waterLevel = reader.ReadByte();
        urbanLevel = reader.ReadByte();
        farmLevel = reader.ReadByte();
        plantLevel = reader.ReadByte();
        specialIndex = reader.ReadByte();
        walled = reader.ReadBoolean();

        byte riverData = reader.ReadByte();
        if (riverData >= 128)
        {
            hasIncomingRiver = true;
            incomingRiver = (HexDirection)(riverData - 128);
        }
        else
        {
            hasIncomingRiver = false;
        }

        riverData = reader.ReadByte();
        if (riverData >= 128)
        {
            hasOutgoingRiver = true;
            outgoingRiver = (HexDirection)(riverData - 128);
        }
        else
        {
            hasOutgoingRiver = false;
        }

        int roadFlags = reader.ReadByte();
        for (int i = 0; i < roads.Length; i++)
        {
            roads[i] = (roadFlags & (1 << i)) != 0;
        }
    }
    /// <summary>
    /// Установить текст на клетке
    /// </summary>
    /// <param name="text"></param>
    public void SetLabel(string text)
    {
        UnityEngine.UI.Text label = uiRect.GetComponent<Text>();
        label.text = text;
    }


    /// <summary>
    /// Выкл. подсветку клетки
    /// </summary>
    public void DisableHighlight()
    {
        Image highlight = uiRect.GetChild(0).GetComponent<Image>();
        highlight.enabled = false;
    }

    /// <summary>
    /// Подсветить клетку
    /// </summary>
    /// <param name="color"></param>
    public void EnableHighlight(Color color)
    {
        Image highlight = uiRect.GetChild(0).GetComponent<Image>();
        highlight.color = color;
        highlight.enabled = true;
    }

    /// <summary>
    /// Вычислить приоритет поиска клетки
    /// </summary>
    public int SearchPriority
    {
        get
        {
            return distance + SearchHeuristic;
        }
    }

}
