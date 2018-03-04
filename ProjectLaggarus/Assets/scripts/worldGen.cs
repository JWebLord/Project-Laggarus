using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class worldGen : MonoBehaviour
{
    public GameObject tile;//тайл
    private Vector3[] directions;

    // Use this for initialization
    void Start()
    {
        directions = new Vector3[6];//см лист 1
        directions[0].Set(1, 0, -1);
        directions[1].Set(1, -1, 0);
        directions[2].Set(0, -1, 1);
        directions[3].Set(-1, 0, 1);
        directions[4].Set(-1, 1, 0);
        directions[5].Set(0, 1, -1);

        generatePlane(10, 10);
        //SpawnOnCors(0, 0, 0);
        //generateCircle(10);
    }

    public static Vector2 CubeToRemoval(Vector3 cube) //кубические координаты в координаты смещения
    {
        float x = cube.x + (cube.z - (cube.z % 2)) / 2;
        float y = cube.z;
        return new Vector2(x, y);
    }

    public static Vector3 RemovalToCube(Vector2 removal) //координаты смещения в кубические координаты
    {
        int x = (int)(removal.x - (removal.y - (removal.y % 2)) / 2);
        int z = (int)removal.y;
        int y = -x - z;
        return new Vector3(x, y, z);
    }

    public static Vector3 CubeToLocal(Vector3 cube) //кубические координаты в локальные координаты
    {
        float x = Mathf.Sqrt(3) / 2 * cube.x - Mathf.Sqrt(3) / 2 * cube.y;
        float z = -1.5f * cube.z;
        return new Vector3(x, 0, z);
    }

    public static Vector3 LocalToCube(Vector3 local) //локальные координаты в кубические координаты
    {
        float x = local.x / Mathf.Sqrt(3) + local.z / 2 / 1.5f;
        float z = local.z / -1.5f;
        float y = local.x / Mathf.Sqrt(3) - local.z / 2 / 1.5f;

        return new Vector3((int)x, (int)y, (int)z);
    }

    void spawnOnCors(Vector3 localCors) //спавн тайла на локальных координатах
    {
        Instantiate(tile, localCors, transform.rotation);
    }

    void generatePlane(int x, int y) //генерация плоскости x на y
    {
        Vector3 currCors = new Vector3();
        currCors.Set(0, 0, 0);
        for (int ix = 0; ix < x; ix++)
        {
            for (int iy = 0; iy < y; iy++)
            {
                spawnOnCors(CubeToLocal(new Vector3(currCors.x, currCors.y, currCors.z)));

                if (iy != y - 1)
                    if (ix % 2 == 0)
                        if (iy % 2 == 0)
                            currCors.Set(
                                currCors.x + directions[2].x, 
                                currCors.y + directions[2].y,
                                currCors.z + directions[2].z);
                        else
                            currCors.Set(
                                currCors.x + directions[3].x, 
                                currCors.y + directions[3].y, 
                                currCors.z + directions[3].z);
                    else
                        if (y % 2 == 0)
                            if (iy % 2 == 0)
                                currCors.Set(
                                    currCors.x + directions[5].x, 
                                    currCors.y + directions[5].y, 
                                    currCors.z + directions[5].z);
                            else
                                currCors.Set(
                                    currCors.x + directions[0].x, 
                                    currCors.y + directions[0].y, 
                                    currCors.z + directions[0].z);
                        else
                            if (iy % 2 == 0)
                                currCors.Set(
                                    currCors.x + directions[0].x, 
                                    currCors.y + directions[0].y, 
                                    currCors.z + directions[0].z);
                            else
                                currCors.Set(
                                    currCors.x + directions[5].x, 
                                    currCors.y + directions[5].y, 
                                    currCors.z + directions[5].z);
            }
            currCors.Set(
                currCors.x + directions[1].x, 
                currCors.y + directions[1].y, 
                currCors.z + directions[1].z);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
