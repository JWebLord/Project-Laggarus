using System.Collections.Generic;
using UnityEngine;

public class HexMapGenerator : MonoBehaviour
{
    /// <summary>
    /// Родительский грид
    /// </summary>
    public HexGrid grid;

    /// <summary>
    /// Семя мира
    /// </summary>
    public int seed;

    /// <summary>
    /// Использовать фиксированное семя
    /// </summary>
    public bool useFixedSeed;

    /// <summary>
    /// Кол-во ячеек на карте и ячеек земли
    /// </summary>
    int cellCount, landCells;

    /// <summary>
    /// Минимальные/максимальные значения по осям для генерации региона
    /// </summary>
    struct MapRegion
    {
        public int xMin, xMax, zMin, zMax;
    }

    /// <summary>
    /// Лист регионов карты
    /// </summary>
    List<MapRegion> regions;

    /// <summary>
    /// Структура для водного цикла(облака, влажность)
    /// </summary>
    struct ClimateData
    {
        public float clouds, moisture;
    }

    /// <summary>
    /// Текущий климат
    /// </summary>
    List<ClimateData> climate = new List<ClimateData>();

    /// <summary>
    /// Будущий климат
    /// </summary>
    List<ClimateData> nextClimate = new List<ClimateData>();


    /// <summary>
    /// Лист очередей приоритета
    /// </summary>
    HexCellPriorityQueue searchFrontier;

    /// <summary>
    /// Граница поиска
    /// </summary>
    int searchFrontierPhase;

    /// <summary>
    /// Рандомность формы "биома"
    /// </summary>
    [Range(0f, 0.5f)]
    public float jitterProbability = 0.25f;

    /// <summary>
    /// Минимальный размер "биома"
    /// </summary>
    [Range(20, 200)]
    public int chunkSizeMin = 30;

    /// <summary>
    /// Максимальный размер "биома"
    /// </summary>
    [Range(20, 200)]
    public int chunkSizeMax = 100;

    /// <summary>
    /// Размер суши
    /// </summary>
    [Range(5, 95)]
    public int landPercentage = 50;

    /// <summary>
    /// Уровень воды
    /// </summary>
    [Range(1, 5)]
    public int waterLevel = 3;

    /// <summary>
    /// Вероятность резких склонов
    /// </summary>
    [Range(0f, 1f)]
    public float highRiseProbability = 0.25f;

    /// <summary>
    /// Вероятность генерации "впадин" ландшафта
    /// </summary>
    [Range(0f, 0.4f)]
    public float sinkProbability = 0.2f;

    /// <summary>
    /// Минимальная высота
    /// </summary>
    [Range(-4, 0)]
    public int elevationMinimum = -2;

    /// <summary>
    /// Максимальная высота
    /// </summary>
    [Range(6, 10)]
    public int elevationMaximum = 8;

    /// <summary>
    /// Отступ по X от края карты
    /// </summary>
    [Range(0, 10)]
    public int mapBorderX = 5;

    /// <summary>
    /// Отступ по Z от края карты
    /// </summary>
    [Range(0, 10)]
    public int mapBorderZ = 5;

    /// <summary>
    /// Граница между регионами
    /// </summary>
    [Range(0, 10)]
    public int regionBorder = 5;

    /// <summary>
    /// Кол-во регионов
    /// </summary>
    [Range(1, 4)]
    public int regionCount = 1;

    /// <summary>
    /// Процент эррозии земли
    /// </summary>
    [Range(0, 100)]
    public int erosionPercentage = 50;

    /// <summary>
    /// Коэффициент испарения
    /// </summary>
    [Range(0f, 1f)]
    public float evaporationFactor = 0.5f;

    /// <summary>
    /// Коэффициент кол-ва выпадения осадков
    /// </summary>
    [Range(0f, 1f)]
    public float precipitationFactor = 0.25f;

    /// <summary>
    /// Коэффициент стока воды
    /// </summary>
    [Range(0f, 1f)]
    public float runoffFactor = 0.25f;

    /// <summary>
    /// Коэффициент просачивания воды
    /// </summary>
    [Range(0f, 1f)]
    public float seepageFactor = 0.125f;

    /// <summary>
    /// Направление ветра
    /// </summary>
    public HexDirection windDirection = HexDirection.NW;

    /// <summary>
    /// Сила ветра
    /// </summary>
    [Range(1f, 10f)]
    public float windStrength = 4f;

    /// <summary>
    /// Начальная влажность
    /// </summary>
    [Range(0f, 1f)]
    public float startingMoisture = 0.1f;

    /// <summary>
    /// Кол-во рек по отношению к поверхности
    /// </summary>
    [Range(0, 20)]
    public int riverPercentage = 10;

    /// <summary>
    /// Лист направлений течения рек
    /// </summary>
    List<HexDirection> flowDirections = new List<HexDirection>();

    /// <summary>
    /// На сколько чаще будут спавниться озера на реках
    /// </summary>
    [Range(0f, 1f)]
    public float extraLakeProbability = 0.25f;

    /// <summary>
    /// Самая низкая температура
    /// </summary>
    [Range(0f, 1f)]
    public float lowTemperature = 0f;

    /// <summary>
    /// Самая высокая температура
    /// </summary>
    [Range(0f, 1f)]
    public float highTemperature = 1f;

    /// <summary>
    /// Енам полушарий
    /// </summary>
    public enum HemisphereMode
    {
        Both, North, South
    }

    /// <summary>
    /// Какие полушария считать под карту
    /// </summary>
    public HemisphereMode hemisphere;

    /// <summary>
    /// Разнообразие температур
    /// </summary>
    [Range(0f, 1f)]
    public float temperatureJitter = 0.1f;

    /// <summary>
    /// "Канал" для разнообразия температур
    /// </summary>
    int temperatureJitterChannel;

    /// <summary>
    /// Диапазоны по температуре для биомов
    /// </summary>
    static float[] temperatureBands = { 0.1f, 0.3f, 0.6f };

    /// <summary>
    /// Диапазоны по влажности для биомов
    /// </summary>
    static float[] moistureBands = { 0.12f, 0.28f, 0.85f };

    /// <summary>
    /// Структура для биома
    /// </summary>
    struct Biome
    {
        public int terrain, plant;

        public Biome(int terrain, int plant)
        {
            this.terrain = terrain;
            this.plant = plant;
        }
    }

    /// <summary>
    /// Матрица биомов(по температурным и влажностным диапазонам)
    /// </summary>
    static Biome[] biomes = {
        new Biome(0, 0), new Biome(3, 0), new Biome(3, 0), new Biome(3, 0),
        new Biome(0, 0), new Biome(4, 0), new Biome(4, 1), new Biome(4, 2),
        new Biome(0, 0), new Biome(1, 0), new Biome(1, 1), new Biome(1, 2),
        new Biome(0, 0), new Biome(1, 1), new Biome(1, 2), new Biome(1, 3)
    };


    /// <summary>
    /// Сгенерировать карту
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    public void GenerateMap(int x, int z, bool wrapping)
    {
        Random.State originalRandomState = Random.state;
        
        if (!useFixedSeed)
        {
            //рандомизация семени(4 строки)
            seed = Random.Range(0, int.MaxValue);
            seed ^= (int)System.DateTime.Now.Ticks;
            seed ^= (int)Time.time;
            seed &= int.MaxValue;
        }

        Random.InitState(seed);

        cellCount = x * z;
        grid.CreateMap(x, z, wrapping);//сначала простое создание карты

        //потом сама генерация
        if (searchFrontier == null)
        {
            searchFrontier = new HexCellPriorityQueue();
        }
        //Установка уровня воды
        for (int i = 0; i < cellCount; i++)
        {
            grid.GetCell(i).WaterLevel = waterLevel;
        }


        CreateRegions();//регионы
        CreateLand();//создание земли
        ErodeLand();//эррозия
        CreateClimate();
        CreateRivers();
        SetTerrainType();//типы ландшафта


        //даем всем ячейкам приоритет поиска 0
        for (int i = 0; i < cellCount; i++)
        {
            grid.GetCell(i).SearchPhase = 0;
        }

        Random.state = originalRandomState;
    }

    /// <summary>
    /// Создать регионы карты
    /// </summary>
    void CreateRegions()
    {
        if (regions == null)
        {
            regions = new List<MapRegion>();
        }
        else
        {
            regions.Clear();
        }

        int borderX = grid.wrapping ? regionBorder : mapBorderX;

        MapRegion region;
        switch (regionCount)
        {
            default:
                if (grid.wrapping)
                {
                    borderX = 0;
                }
                region.xMin = borderX;
                region.xMax = grid.cellCountX - borderX;
                region.zMin = mapBorderZ;
                region.zMax = grid.cellCountZ - mapBorderZ;
                regions.Add(region);
                break;
            case 2:
                if (grid.wrapping)
                {
                    borderX = 0;
                }
                if (Random.value < 0.5f)
                {
                    region.xMin = borderX;
                    region.xMax = grid.cellCountX / 2 - regionBorder;
                    region.zMin = mapBorderZ;
                    region.zMax = grid.cellCountZ - mapBorderZ;
                    regions.Add(region);
                    region.xMin = grid.cellCountX / 2 + regionBorder;
                    region.xMax = grid.cellCountX - borderX;
                    regions.Add(region);
                }
                else
                {
                    region.xMin = borderX;
                    region.xMax = grid.cellCountX - borderX;
                    region.zMin = mapBorderZ;
                    region.zMax = grid.cellCountZ / 2 - regionBorder;
                    regions.Add(region);
                    region.zMin = grid.cellCountZ / 2 + regionBorder;
                    region.zMax = grid.cellCountZ - mapBorderZ;
                    regions.Add(region);
                }
                break;
            case 3:
                if (grid.wrapping)
                {
                    borderX = 0;
                }
                region.xMin = borderX;
                region.xMax = grid.cellCountX / 3 - regionBorder;
                region.zMin = mapBorderZ;
                region.zMax = grid.cellCountZ - mapBorderZ;
                regions.Add(region);
                region.xMin = grid.cellCountX / 3 + regionBorder;
                region.xMax = grid.cellCountX * 2 / 3 - regionBorder;
                regions.Add(region);
                region.xMin = grid.cellCountX * 2 / 3 + regionBorder;
                region.xMax = grid.cellCountX - borderX;
                regions.Add(region);
                break;
            case 4:
                if (grid.wrapping)
                {
                    borderX = 0;
                }
                region.xMin = borderX;
                region.xMax = grid.cellCountX / 2 - regionBorder;
                region.zMin = mapBorderZ;
                region.zMax = grid.cellCountZ / 2 - regionBorder;
                regions.Add(region);
                region.xMin = grid.cellCountX / 2 + regionBorder;
                region.xMax = grid.cellCountX - borderX;
                regions.Add(region);
                region.zMin = grid.cellCountZ / 2 + regionBorder;
                region.zMax = grid.cellCountZ - mapBorderZ;
                regions.Add(region);
                region.xMin = borderX;
                region.xMax = grid.cellCountX / 2 - regionBorder;
                regions.Add(region);
                break;
        }
    }

    /// <summary>
    /// Кол-во ячеек под сушу
    /// </summary>
    void CreateLand()
    {
        int landBudget = Mathf.RoundToInt(cellCount * landPercentage * 0.01f);
        landCells = landBudget;

        //TODO: сделать ограничение на основе размеров карты
        //Ограничение генерации карты 10000 итераций(на всякий случай)
        for (int guard = 0; guard < 10000; guard++)
        {
            bool sink = Random.value < sinkProbability;
            for (int i = 0; i < regions.Count; i++)
            {
                MapRegion region = regions[i];
                int chunkSize = Random.Range(chunkSizeMin, chunkSizeMax - 1);
                //генерация подъемов/спусков
                if (sink)
                {
                    landBudget = SinkTerrain(chunkSize, landBudget, region);
                }
                else
                {
                    landBudget = RaiseTerrain(chunkSize, landBudget, region);
                    //Если кончился бюджет, то заканчиваем генерацию
                    if (landBudget == 0)
                    {
                        return;
                    }
                }
            }
        }

        //Если не удалось потратить бюджет(остановка по кол-ву итераций), то выводим ошибку
        if (landBudget > 0)
        {
            Debug.LogWarning("Failed to use up " + landBudget + " land budget.");
            landCells -= landBudget;
        }
    }

    /// <summary>
    /// Поднять уровень поверхности в регионе
    /// </summary>
    /// <param name="chunkSize"></param>
    int RaiseTerrain(int chunkSize, int budget, MapRegion region)
    {
        //получаем рандомную ячейку, добавляем ее в лист 
        searchFrontierPhase += 1;
        HexCell firstCell = GetRandomCell(region);
        firstCell.SearchPhase = searchFrontierPhase;
        firstCell.Distance = 0;
        firstCell.SearchHeuristic = 0;
        searchFrontier.Enqueue(firstCell);

        //при помощи измененного алгоритма поиска пути берем кусок поверхности нужного размера
        HexCoordinates center = firstCell.coordinates;
        int rise = Random.value < highRiseProbability ? 2 : 1;//величина подъема
        int size = 0;
        while (size < chunkSize && searchFrontier.Count > 0)
        {
            HexCell current = searchFrontier.Dequeue();
            int originalElevation = current.Elevation;
            int newElevation = originalElevation + rise;
            if (newElevation > elevationMaximum)
            {
                continue;
            }
            current.Elevation = newElevation;
            //Если кончилось кол-во клеток под сушу, то выходим из цикла
            //(при этом не учитываются ячейки, которые уже были отредактированны и им можно еще поднять уровень)
            if (
                originalElevation < waterLevel &&
                newElevation >= waterLevel && --budget == 0
            )
            {
                break;
            }
            size += 1;

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);
                if (neighbor && neighbor.SearchPhase < searchFrontierPhase)
                {
                    neighbor.SearchPhase = searchFrontierPhase;
                    //чтобы держать все ячейки куска земли вместе, считаем дистанцию от центра, чтобы управлять приоритетом
                    neighbor.Distance = neighbor.coordinates.DistanceTo(center);
                    //рандомизация формы куска при помощи изменения эвристики
                    neighbor.SearchHeuristic = Random.value < jitterProbability ? 1 : 0;
                    searchFrontier.Enqueue(neighbor);
                }
            }
        }
        searchFrontier.Clear();

        return budget;
    }

    /// <summary>
    /// Опустить уровень поверхности
    /// </summary>
    /// <param name="chunkSize"></param>
    int SinkTerrain(int chunkSize, int budget, MapRegion region)
    {
        //получаем рандомную ячейку, добавляем ее в лист 
        searchFrontierPhase += 1;
        HexCell firstCell = GetRandomCell(region);
        firstCell.SearchPhase = searchFrontierPhase;
        firstCell.Distance = 0;
        firstCell.SearchHeuristic = 0;
        searchFrontier.Enqueue(firstCell);

        //при помощи измененного алгоритма поиска пути берем кусок поверхности нужного размера
        HexCoordinates center = firstCell.coordinates;
        int sink = Random.value < highRiseProbability ? 2 : 1;//величина спуска
        int size = 0;
        while (size < chunkSize && searchFrontier.Count > 0)
        {
            HexCell current = searchFrontier.Dequeue();
            int originalElevation = current.Elevation;
            int newElevation = current.Elevation - sink;
            if (newElevation < elevationMinimum)
            {
                continue;
            }
            current.Elevation = newElevation;
            //Если кончилось кол-во клеток под сушу, то выходим из цикла
            //(при этом не учитываются ячейки, которые уже были отредактированны и им можно еще поднять уровень)
            if (
                originalElevation >= waterLevel &&
                newElevation < waterLevel
            )
            {
                budget += 1;
            }
            size += 1;

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);
                if (neighbor && neighbor.SearchPhase < searchFrontierPhase)
                {
                    neighbor.SearchPhase = searchFrontierPhase;
                    //чтобы держать все ячейки куска земли вместе, считаем дистанцию от центра, чтобы управлять приоритетом
                    neighbor.Distance = neighbor.coordinates.DistanceTo(center);
                    //рандомизация формы куска при помощи изменения эвристики
                    neighbor.SearchHeuristic = Random.value < jitterProbability ? 1 : 0;
                    searchFrontier.Enqueue(neighbor);
                }
            }
        }
        searchFrontier.Clear();

        return budget;
    }

    /// <summary>
    /// Получить рандомную ячейку в пределах региона
    /// </summary>
    /// <returns></returns>
    HexCell GetRandomCell(MapRegion region)
    {
        return grid.GetCell(Random.Range(region.xMin, region.xMax), Random.Range(region.zMin, region.zMax));
    }

    /// <summary>
    /// Установить тип поверхности ячейки
    /// </summary>
    void SetTerrainType()
    {
        temperatureJitterChannel = Random.Range(0, 4);
        //высота каменной пустыни
        int rockDesertElevation =
            elevationMaximum - (elevationMaximum - waterLevel) / 2;

        for (int i = 0; i < cellCount; i++)
        {
            HexCell cell = grid.GetCell(i);
            float temperature = DetermineTemperature(cell);

            float moisture = climate[i].moisture;
            if (!cell.IsUnderwater)
            {
                //устанавливаем тип биома по матрице
                int t = 0;
                for (; t < temperatureBands.Length; t++)
                {
                    if (temperature < temperatureBands[t])
                    {
                        break;
                    }
                }
                int m = 0;
                for (; m < moistureBands.Length; m++)
                {
                    if (moisture < moistureBands[m])
                    {
                        break;
                    }
                }
                Biome cellBiome = biomes[t * 4 + m];

                if (cellBiome.terrain == 0)
                {
                    if (cell.Elevation >= rockDesertElevation)
                    {
                        cellBiome.terrain = 4;//типа камень
                    }
                }
                else if (cell.Elevation == elevationMaximum)
                {
                    cellBiome.terrain = 3;//если пустыня слишком высоко, то она снежная
                }

                //на снегу деревьев нет
                if (cellBiome.terrain == 3)
                {
                    cellBiome.plant = 0;
                }
                else if (cellBiome.plant != 3  && cell.HasRiver)
                {
                    //если растения не на снегу и у речки, то их уровень увеличивется
                    cellBiome.plant += 1;
                }

                cell.TerrainTypeIndex = cellBiome.terrain;
                cell.PlantLevel = cellBiome.plant;
            }
            else
            {
                //разный тип ландшафта для ячеек под водой
                int terrain;
                if (cell.Elevation == waterLevel - 1)
                {
                    int cliffs = 0, slopes = 0;
                    for (
                        HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++
                    )
                    {
                        HexCell neighbor = cell.GetNeighbor(d);
                        if (!neighbor)
                        {
                            continue;
                        }
                        int delta = neighbor.Elevation - cell.WaterLevel;
                        if (delta == 0)
                        {
                            slopes += 1;
                        }
                        else if (delta > 0)
                        {
                            cliffs += 1;
                        }
                    }
                    //в зависимости от гористости местности рядом выставляем ландшафт
                    if (cliffs + slopes > 3)
                    {
                        terrain = 1;
                    }
                    else if (cliffs > 0)
                    {
                        terrain = 4;
                    }
                    else if (slopes > 0)
                    {
                        terrain = 0;
                    }
                    else
                    {
                        terrain = 1;
                    }
                }
                else if (cell.Elevation >= waterLevel)
                {
                    terrain = 1;
                }
                else if (cell.Elevation < 0)
                {
                    terrain = 4;
                }
                else
                {
                    terrain = 2;
                }
                //убираем зелень на холодных ячейках
                if (terrain == 1 && temperature < temperatureBands[0])
                {
                    terrain = 2;
                }

                cell.TerrainTypeIndex = terrain;
            }
            //данные для отображения влажности шейдером
            //cell.SetMapData(moisture);
            //Данные для отображения совокупности влажности и высоты над уровнем моря
            //float weight = moisture * (cell.Elevation - waterLevel) /(elevationMaximum - waterLevel);cell.SetMapData(weight);
            //Отображение карты температуры
            //cell.SetMapData(temperature);
        }
    }


    void ErodeLand()
    {
        List<HexCell> erodibleCells = ListPool<HexCell>.Get();

        for (int i = 0; i < cellCount; i++)
        {
            HexCell cell = grid.GetCell(i);
            if (IsErodible(cell))
            {
                erodibleCells.Add(cell);
            }
        }

        int targetErodibleCount =
            (int)(erodibleCells.Count * (100 - erosionPercentage) * 0.01f);

        while (erodibleCells.Count > targetErodibleCount)
        {
            int index = Random.Range(0, erodibleCells.Count - 1);
            HexCell cell = erodibleCells[index];
            HexCell targetCell = GetErosionTarget(cell);

            //перемещение земли из одной ячейки в другую эррозией
            cell.Elevation -= 1;
            targetCell.Elevation += 1;

            //если клетка не склонна к эррозии, то выкидываем ее из списка
            if (!IsErodible(cell))
            {
                erodibleCells[index] = erodibleCells[erodibleCells.Count - 1];
                erodibleCells.RemoveAt(erodibleCells.Count - 1);
            }

            //если в результате эррозии появились новые ячейки склонные к ней, то закидываем их в список
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = cell.GetNeighbor(d);
                if (
                    neighbor && neighbor.Elevation == cell.Elevation + 2 
                    &&!erodibleCells.Contains(neighbor)
                )
                {
                    erodibleCells.Add(neighbor);
                }
            }

            //если целевая ячека стала склонной к эррозии, то ее тоже закидываем в список
            if (IsErodible(targetCell) && !erodibleCells.Contains(targetCell))
            {
                erodibleCells.Add(targetCell);
            }

            //если в результате эррозии рядом с целевой ячейкой появились новые ячейки склонные к ней, то закидываем их в список
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = targetCell.GetNeighbor(d);
                if (
                    neighbor && neighbor != cell &&
                    neighbor.Elevation == targetCell.Elevation + 1 
                    && !IsErodible(neighbor)
                )
                {
                    erodibleCells.Remove(neighbor);
                }
            }
        }

        ListPool<HexCell>.Add(erodibleCells);
    }

    /// <summary>
    /// Провека, склонна ли ячейка к эррозии
    /// </summary>
    /// <param name="cell"></param>
    /// <returns></returns>
    bool IsErodible(HexCell cell)
    {
        int erodibleElevation = cell.Elevation - 2;
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            HexCell neighbor = cell.GetNeighbor(d);
            if (neighbor && neighbor.Elevation <= erodibleElevation)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Получить клетку в которую часть земли переместится эррозией
    /// </summary>
    /// <param name="cell"></param>
    /// <returns></returns>
    HexCell GetErosionTarget(HexCell cell)
    {
        List<HexCell> candidates = ListPool<HexCell>.Get();
        int erodibleElevation = cell.Elevation - 2;
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            HexCell neighbor = cell.GetNeighbor(d);
            if (neighbor && neighbor.Elevation <= erodibleElevation)
            {
                candidates.Add(neighbor);
            }
        }
        HexCell target = candidates[Random.Range(0, candidates.Count)];
        ListPool<HexCell>.Add(candidates);
        return target;
    }

    /// <summary>
    /// Создать лист информации о климате
    /// </summary>
    void CreateClimate()
    {
        climate.Clear();
        nextClimate.Clear();
        ClimateData initialData = new ClimateData();
        initialData.moisture = startingMoisture;
        ClimateData clearData = new ClimateData();
        for (int i = 0; i < cellCount; i++)
        {
            climate.Add(initialData);
            nextClimate.Add(clearData);
        }

        //добавляем облака из воды(40 циклов для симуляции)
        for (int cycle = 0; cycle < 40; cycle++)
        {
            for (int i = 0; i < cellCount; i++)
            {
                EvolveClimate(i);
            }
            List<ClimateData> swap = climate;
            climate = nextClimate;
            nextClimate = swap;
        }
    }


    void EvolveClimate(int cellIndex)
    {
        HexCell cell = grid.GetCell(cellIndex);
        ClimateData cellClimate = climate[cellIndex];

        if (cell.IsUnderwater)
        {
            cellClimate.moisture = 1f;//если под водой, то влажность 1
            cellClimate.clouds += evaporationFactor;
        }
        else
        {
            //если не под водой, то испарение расчитывается на основе влажности
            float evaporation = cellClimate.moisture * evaporationFactor;
            cellClimate.moisture -= evaporation;//влажность соответственно уменьшается
            cellClimate.clouds += evaporation; //а облака растут
        }

        //осадки уменьшают кол/во облаков, но увеличивают влажность
        float precipitation = cellClimate.clouds * precipitationFactor;
        cellClimate.clouds -= precipitation;
        cellClimate.moisture += precipitation;

        //максимум воды в облаках на определенной высоте
        float cloudMaximum = 1f - cell.ViewElevation / (elevationMaximum + 1f);
        //если облаков больше, чем может быть на этой высоте, то вода из них выпадает до нормы
        if (cellClimate.clouds > cloudMaximum)
        {
            cellClimate.moisture += cellClimate.clouds - cloudMaximum;
            cellClimate.clouds = cloudMaximum;
        }

        //распространение облаков
        HexDirection mainDispersalDirection = windDirection.Opposite();
        float cloudDispersal = cellClimate.clouds * (1f / (5f + windStrength));
        //сток воды(из влаги в почве)
        float runoff = cellClimate.moisture * runoffFactor * (1f / 6f);
        //просачивание воды(из влаги в почве)
        float seepage = cellClimate.moisture * seepageFactor * (1f / 6f);

        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            HexCell neighbor = cell.GetNeighbor(d);
            if (!neighbor)
            {
                continue;
            }
            ClimateData neighborClimate = nextClimate[neighbor.Index];
            //если направление ветра равно тому в котором дует ветер, то этому соседу достается больше "облаков"
            if (d == mainDispersalDirection)
            {
                neighborClimate.clouds += cloudDispersal * windStrength;
            }
            else
            {
                neighborClimate.clouds += cloudDispersal;
            }

            //вода либо стекает вниз, либо просачивается к соседям на том же уровне
            int elevationDelta = neighbor.ViewElevation - cell.ViewElevation;
            if (elevationDelta < 0)
            {
                cellClimate.moisture -= runoff;
                neighborClimate.moisture += runoff;
            }
            else if (elevationDelta == 0)
            {
                cellClimate.moisture -= seepage;
                neighborClimate.moisture += seepage;
            }


            nextClimate[neighbor.Index] = neighborClimate;
        }

        ClimateData nextCellClimate = nextClimate[cellIndex];
        nextCellClimate.moisture += cellClimate.moisture;
        if (nextCellClimate.moisture > 1f)
        {
            nextCellClimate.moisture = 1f;
        }
        nextClimate[cellIndex] = nextCellClimate;
        climate[cellIndex] = new ClimateData();
    }

    /// <summary>
    /// Создать реки
    /// </summary>
    void CreateRivers()
    {
        List<HexCell> riverOrigins = ListPool<HexCell>.Get();
        //если в ячейке можно разместить реку, то добавляем ее в список
        for (int i = 0; i < cellCount; i++)
        {
            HexCell cell = grid.GetCell(i);
            if (cell.IsUnderwater)
            {
                continue;
            }
            ClimateData data = climate[i];
            float weight =
                data.moisture * (cell.Elevation - waterLevel) /
                (elevationMaximum - waterLevel);
            if (weight > 0.75f)
            {
                riverOrigins.Add(cell);
                riverOrigins.Add(cell);
            }
            if (weight > 0.5f)
            {
                riverOrigins.Add(cell);
            }
            if (weight > 0.25f)
            {
                riverOrigins.Add(cell);
            }
        }

        int riverBudget = Mathf.RoundToInt(landCells * riverPercentage * 0.01f);

        //добавляем реки
        while (riverBudget > 0 && riverOrigins.Count > 0)
        {
            int index = Random.Range(0, riverOrigins.Count);
            int lastIndex = riverOrigins.Count - 1;
            HexCell origin = riverOrigins[index];
            riverOrigins[index] = riverOrigins[lastIndex];
            riverOrigins.RemoveAt(lastIndex);

            if (!origin.HasRiver)
            {
                bool isValidOrigin = true;
                for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    HexCell neighbor = origin.GetNeighbor(d);
                    if (neighbor && (neighbor.HasRiver || neighbor.IsUnderwater))
                    {
                        isValidOrigin = false;
                        break;
                    }
                }
                if (isValidOrigin)
                {
                    riverBudget -= CreateRiver(origin);
                }
            }
        }

        if (riverBudget > 0)
        {
            Debug.LogWarning("Failed to use up river budget.");
        }

        ListPool<HexCell>.Add(riverOrigins);
    }

    /// <summary>
    /// Создать реку в ячейке
    /// </summary>
    /// <param name="origin"></param>
    /// <returns></returns>
    int CreateRiver(HexCell origin)
    {
        int length = 1;
        HexCell cell = origin;
        HexDirection direction = HexDirection.NE;
        while (!cell.IsUnderwater)
        {
            int minNeighborElevation = int.MaxValue;
            //проверка есть ли реки у ячеек вокруг
            flowDirections.Clear();
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = cell.GetNeighbor(d);
                //реки не располагаюся рядом друг с другом, т.к. не реализовано их слияние
                if (!neighbor)
                {
                    continue;
                }

                if (neighbor.Elevation < minNeighborElevation)
                {
                    minNeighborElevation = neighbor.Elevation;
                }

                if (neighbor == origin || neighbor.HasIncomingRiver)
                {
                    continue;
                }

                //Течение рек вниз
                int delta = neighbor.Elevation - cell.Elevation;
                if (delta > 0)
                {
                    continue;
                }
                //если реку можно"продлить", то делаем это
                if (neighbor.HasOutgoingRiver)
                {
                    cell.SetOutgoingRiver(d);
                    return length;
                }

                //предпочтительно идти вниз по склону
                if (delta < 0)
                {
                    flowDirections.Add(d);
                    flowDirections.Add(d);
                    flowDirections.Add(d);
                }
                if (
                    length == 1 ||
                    (d != direction.Next2() && d != direction.Previous2())
                )
                {
                    flowDirections.Add(d);
                }

                flowDirections.Add(d);
            }

            if (flowDirections.Count == 0)
            {
                if (length == 1)
                {
                    return 0;
                }
                //оканчивание рек озерами
                if (minNeighborElevation >= cell.Elevation)
                {
                    cell.WaterLevel = minNeighborElevation;
                    if (minNeighborElevation == cell.Elevation)
                    {
                        cell.Elevation = minNeighborElevation - 1;
                    }
                }
                break;
            }

            direction = flowDirections[Random.Range(0, flowDirections.Count)];
            cell.SetOutgoingRiver(direction);
            length += 1;

            //добавляем озер
            if (minNeighborElevation >= cell.Elevation &&
                Random.value < extraLakeProbability)
            {
                cell.WaterLevel = cell.Elevation;
                cell.Elevation -= 1;
            }

            cell = cell.GetNeighbor(direction);
        }
        return length;
    }

    /// <summary>
    /// Определить температуру(по широте)
    /// </summary>
    /// <param name="cell"></param>
    /// <returns></returns>
    float DetermineTemperature(HexCell cell)
    {
        //широта
        float latitude = (float)cell.coordinates.Z / grid.cellCountZ;
        //зависимость от полушарий
        if (hemisphere == HemisphereMode.Both)
        {
            latitude *= 2f;
            if (latitude > 1f)
            {
                latitude = 2f - latitude;
            }
        }
        else if (hemisphere == HemisphereMode.North)
        {
            latitude = 1f - latitude;
        }

        //зависимость от широты
        float temperature =
            Mathf.LerpUnclamped(lowTemperature, highTemperature, latitude);

        //зависимость от высоты
        temperature *= 1f - (cell.ViewElevation - waterLevel) /
            (elevationMaximum - waterLevel + 1f);

        //разнообразие от параметра разнообразия и шума
        float jitter =
            HexMetrics.SampleNoise(cell.Position * 0.1f)[temperatureJitterChannel];

        temperature += (jitter * 2f - 1f) * temperatureJitter;

        return temperature;
    }
}