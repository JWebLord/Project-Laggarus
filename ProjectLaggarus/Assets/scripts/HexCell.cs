using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexCell : MonoBehaviour {

    public HexCoordinates coordinates;//координаты ячейки(кубические)
    public RectTransform uiRect;//координаты для координат:) ячейки
    private Color color;//цвет ячейки
    public HexGridChunk chunk;//чанк к которому принадлежит ячейка
    private int elevation = int.MinValue;//высота ячейки(изначально -100500, чтобы произошла ее переотрисовка при создании)

    bool hasIncomingRiver, hasOutgoingRiver;//входят/выходят ли малые реки
    HexDirection incomingRiver, outgoingRiver;//направление рек

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
            Vector3 position = transform.localPosition;
            position.y = value * HexMetrics.elevationStep;
            position.y +=
                (HexMetrics.SampleNoise(position).y * 2f - 1f) *
                HexMetrics.elevationPerturbStrength;//добавление разности ячеек в высотах
            transform.localPosition = position;

            Vector3 uiPosition = uiRect.localPosition;
            uiPosition.z = -position.y;
            uiRect.localPosition = uiPosition;

            //проверки на то, чтобы реки текли правильно(по высотам)
            if (
                hasOutgoingRiver &&
                elevation < GetNeighbor(outgoingRiver).elevation
            )
            {
                RemoveOutgoingRiver();
            }
            if (
                hasIncomingRiver &&
                elevation > GetNeighbor(incomingRiver).elevation
            )
            {
                RemoveIncomingRiver();
            }

            Refresh();
        }
    }

    public Vector3 Position//позиция readOnly
    {
        get
        {
            return transform.localPosition;
        }
    }

    public Color Color//метод задания и считывания цвета ячейки
    {
        get
        {
            return color;
        }
        set
        {
            if (color == value)
            {
                return;
            }
            color = value;
            Refresh();
        }
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
                (elevation + HexMetrics.riverSurfaceElevationOffset) *
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
        if (!neighbor || elevation < neighbor.elevation)
        {
            return;
        }
        RemoveOutgoingRiver();//Удаление старого стока реки
        if (hasIncomingRiver && incomingRiver == direction)//если река раньше втекала из направления, то ее тоже удаляем
        {
            RemoveIncomingRiver();
        }
        hasOutgoingRiver = true;
        outgoingRiver = direction;//ну и наконец ставим речку
        RefreshSelfOnly();

        neighbor.RemoveIncomingRiver();//соседу назначаем текущую к нему реку
        neighbor.hasIncomingRiver = true;
        neighbor.incomingRiver = direction.Opposite();
        neighbor.RefreshSelfOnly();
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
}
