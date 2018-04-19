using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System;



public class HexUnit : MonoBehaviour
{
    private HexCell location, currentTravelLocation; //расположение в истинное и во време анимации
    private  float orientation;//направление юнита
    public static List<HexUnit> unitPrefabs = new List<HexUnit>();//префаб юнита
    public int unitOwner;
    List<HexCell> pathToTravel;//Путь юнита

    const float travelSpeed = 4f;//Скорость анимации движения юнита
    const float rotationSpeed = 180f;//Скорость анимации поворота юнита

    public HexCell currentDestinationCell;

    public HexGrid Grid { get; set; }
    /// <summary>
    /// Оставшиеся очки передвижения
    /// </summary>
    private int speed;
    /// <summary>
    /// Кол-во очков, которое должно быть у юнита в начале следующего хода
    /// </summary>
    public const int unitSpeedPerTurn = 24;

    /// <summary>
    /// Дальность видимости юнита
    /// </summary>
    public int VisionRange
    {
        get
        {
            return 3;//константа пока что
        }
    }


    /// <summary>
    /// Кол-во очков перемещения
    /// </summary>
    public int Speed
    {
        get
        {
            return speed;
        }
    }

    public HexCell Location
    {
        get
        {
            return location;
        }
        set
        {
            if (location)
            {
                location.Unit = null;
                Grid.DecreaseVisibility(location, VisionRange);
            }
            location = value;
            value.Unit = this;
            Grid.IncreaseVisibility(value, VisionRange);
            transform.localPosition = value.Position;
            Grid.MakeChildOfColumn(transform, value.ColumnIndex);
        }
    }

    public float Orientation
    {
        get
        {
            return orientation;
        }
        set
        {
            orientation = value;
            transform.localRotation = Quaternion.Euler(0f, value, 0f);
            if (currentTravelLocation)
            {
                Grid.IncreaseVisibility(location, VisionRange);
                Grid.DecreaseVisibility(currentTravelLocation, VisionRange);
                currentTravelLocation = null;
            }
        }
    }

    private void Awake()
    {
        speed = unitSpeedPerTurn;
    }

    void OnEnable()
    {
        if (location)
        {
            transform.localPosition = location.Position;
        }
    }

    public void ValidateLocation()
    {
        transform.localPosition = location.Position;
    }

    public void Die()
    {
        if (location)
        {
            Grid.DecreaseVisibility(location, VisionRange);
        }
        location.Unit = null;
        Destroy(gameObject);
    }

    public void Save(BinaryWriter writer)
    {
        location.coordinates.Save(writer);
        writer.Write(orientation);
        writer.Write(unitOwner);
    }

    public static void Load(BinaryReader reader, HexGrid grid, bool ownerCompatibility = false)
    {
        HexCoordinates coordinates = HexCoordinates.Load(reader);
        float orientation = reader.ReadSingle();
        int owner =  ownerCompatibility ? reader.ReadInt32() : 0;
        grid.AddUnit(
            Instantiate(unitPrefabs[owner]), grid.GetCell(coordinates), orientation, owner
        );

    }

    public void Travel(List<HexCell> path)
    {
        location.Unit = null;
        location = path[path.Count - 1];
        location.Unit = this;

        pathToTravel = path;
        updateUnitSpeed(path);
        StopAllCoroutines();
        StartCoroutine(TravelPath());
    }

    /// <summary>
    /// Обновить очки передвижения после совершения движения
    /// </summary>
    /// <param name="path"></param>
    private void updateUnitSpeed(List<HexCell> path)
    {
        speed = speed - path[path.Count - 1].Distance;
        if (speed < 0) {
            Debug.Log("Unit speed error, current unit speed is " + speed.ToString());
            speed = 0;
        }
        else { Debug.Log("Unit speed is " + speed.ToString()); }
    }

    /// <summary>
    /// Восстановить скорость юнита
    /// </summary>
    public void RestoreUnitSpeed()
    {
        speed = unitSpeedPerTurn;
    }

    //Скольжение по пути(анимация)
    IEnumerator TravelPath()
    {
        Vector3 a, b, c = pathToTravel[0].Position;
        yield return LookAt(pathToTravel[1].Position);

        if (!currentTravelLocation)
        {
            currentTravelLocation = pathToTravel[0];
        }
        Grid.DecreaseVisibility(currentTravelLocation, VisionRange);
        int currentColumn = currentTravelLocation.ColumnIndex;

        float t = Time.deltaTime * travelSpeed;

        for (int i = 1; i < pathToTravel.Count; i++)
        {
            currentTravelLocation = pathToTravel[i];
            a = c;
            b = pathToTravel[i - 1].Position;

            int nextColumn = currentTravelLocation.ColumnIndex;
            if (currentColumn != nextColumn)
            {
                if (nextColumn < currentColumn - 1)
                {
                    a.x -= HexMetrics.innerDiameter * HexMetrics.wrapSize;
                    b.x -= HexMetrics.innerDiameter * HexMetrics.wrapSize;
                }
                else if (nextColumn > currentColumn + 1)
                {
                    a.x += HexMetrics.innerDiameter * HexMetrics.wrapSize;
                    b.x += HexMetrics.innerDiameter * HexMetrics.wrapSize;
                }

                Grid.MakeChildOfColumn(transform, nextColumn);
                currentColumn = nextColumn;
            }

            c = (b + currentTravelLocation.Position) * 0.5f;
            Grid.IncreaseVisibility(pathToTravel[i], VisionRange);

            for (; t < 1f; t += Time.deltaTime * travelSpeed)
            {
                transform.localPosition = Bezier.GetPoint(a, b, c, t);
                Vector3 d = Bezier.GetDerivative(a, b, c, t);
                d.y = 0f;
                transform.localRotation = Quaternion.LookRotation(d);
                yield return null;
            }
            Grid.DecreaseVisibility(pathToTravel[i], VisionRange);
            t -= 1f;
        }
        currentTravelLocation = null;

        a = c;
        b = location.Position;
        c = b;
        Grid.IncreaseVisibility(location, VisionRange);
        for (; t < 1f; t += Time.deltaTime * travelSpeed)
        {
            transform.localPosition = Bezier.GetPoint(a, b, c, t);
            Vector3 d = Bezier.GetDerivative(a, b, c, t);
            d.y = 0f;
            transform.localRotation = Quaternion.LookRotation(d);
            yield return null;
        }

        transform.localPosition = location.Position;
        orientation = transform.localRotation.eulerAngles.y;

        ListPool<HexCell>.Add(pathToTravel);
        pathToTravel = null;
    }


    IEnumerator LookAt(Vector3 point)
    {
        if (HexMetrics.Wrapping)
        {
            float xDistance = point.x - transform.localPosition.x;
            if (xDistance < -HexMetrics.innerRadius * HexMetrics.wrapSize)
            {
                point.x += HexMetrics.innerDiameter * HexMetrics.wrapSize;
            }
            else if (xDistance > HexMetrics.innerRadius * HexMetrics.wrapSize)
            {
                point.x -= HexMetrics.innerDiameter * HexMetrics.wrapSize;
            }
        }

        point.y = transform.localPosition.y;

        Quaternion fromRotation = transform.localRotation;
        Quaternion toRotation = Quaternion.LookRotation(point - transform.localPosition);
        float angle = Quaternion.Angle(fromRotation, toRotation);
        if (angle > 0f)
        {
            float speed = rotationSpeed / angle;

            for (float t = Time.deltaTime * speed; t < 1f; t += Time.deltaTime * speed)
            {
                transform.localRotation = Quaternion.Slerp(fromRotation, toRotation, t);
                yield return null;
            }
        }

        transform.LookAt(point);
        orientation = transform.localRotation.eulerAngles.y;
    }


    /// <summary>
    /// Получить "цену" прохождения по клетке
    /// </summary>
    /// <param name="fromCell"></param>
    /// <param name="toCell"></param>
    /// <param name="direction"></param>
    /// <returns></returns>
    public int GetMoveCost(
        HexCell fromCell, HexCell toCell, HexDirection direction)
    {
        HexEdgeType edgeType = fromCell.GetEdgeType(toCell);
        //обход крутых склонов
        if (edgeType == HexEdgeType.Cliff)
        {
            return -1;
        }
        int moveCost;
        //стоимость по дорогам
        if (fromCell.HasRoadThroughEdge(direction))
        {
            moveCost = 1;
        }
        //обход стен
        else if (fromCell.Walled != toCell.Walled)
        {
            return -1;
        }
        else
        {
            moveCost = edgeType == HexEdgeType.Flat ? 5 : 10;
            moveCost +=
                toCell.UrbanLevel + toCell.FarmLevel + toCell.PlantLevel;
        }
        return moveCost;
    }

    /// <summary>
    /// Возможно ли перемещение по данной ячейке
    /// </summary>
    /// <param name="cell"></param>
    /// <returns></returns>
    public bool IsValidDestination(HexCell cell)
    {
        //обход неисследованныъ ячеек, ячеек с юнитами и ячеек под водой
        return cell.IsExplored && !cell.IsUnderwater && !cell.Unit;
    }
}