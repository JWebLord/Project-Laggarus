using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexFeatureManager : MonoBehaviour {

    public HexFeatureCollection[] urbanCollections, farmCollections, plantCollections;

    Transform container;

    public void Clear()
    {
        if (container)
        {
            Destroy(container.gameObject);
        }
        container = new GameObject("Features Container").transform;
        container.SetParent(transform, false);
    }

    public void Apply() { }

    public void AddFeature(HexCell cell, Vector3 position)
    {
        //значение хэш-сетки для конкретного объекта
        HexHash hash = HexMetrics.SampleHashGrid(position);
        //домики
        Transform prefab = PickPrefab(
            urbanCollections, cell.UrbanLevel, hash.a, hash.d
        );
        //фермы
        Transform otherPrefab = PickPrefab(
            farmCollections, cell.FarmLevel, hash.b, hash.d
        );
        float usedHash = hash.a;
        if (prefab)//выбор, что заспавнить, если существуют 2 префаба или больше(спавн того, чей хэш меньше)
        {
            if (otherPrefab && hash.b < hash.a)
            {
                prefab = otherPrefab;
                usedHash = hash.b;
            }
        }
        else if (otherPrefab)
        {
            prefab = otherPrefab;
            usedHash = hash.b;
        }
        //растения
        otherPrefab = PickPrefab(
            plantCollections, cell.PlantLevel, hash.c, hash.d
        );
        if (prefab)
        {
            if (otherPrefab && hash.c < usedHash)//сравнение с хэшом объекта который ранее был предпочтительным
            {
                prefab = otherPrefab;
            }
        }
        else if (otherPrefab)
        {
            prefab = otherPrefab;
        }
        else
        {
            return;
        }
        Transform instance = Instantiate(prefab);
        position.y += instance.localScale.y * 0.5f;
        instance.localPosition = HexMetrics.Perturb(position);
        instance.localRotation = Quaternion.Euler(0f, 360f * hash.e, 0f);
        //ставим контейнер родителем, чтобы удалять объекты каждый раз, когда обновляется чанк
        //и спавнить новый с привязкой к старому контейнеру
        instance.SetParent(container, false);
    }

    Transform PickPrefab(HexFeatureCollection[] collection, int level, float hash, float choice)
    {

        if (level > 0)
        {
            float[] thresholds = HexMetrics.GetFeatureThresholds(level - 1);
            for (int i = 0; i < thresholds.Length; i++)
            {
                if (hash < thresholds[i])
                {
                    return collection[i].Pick(choice);
                }
            }
        }
        return null;
    }
}
