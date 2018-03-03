using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class worldGen : MonoBehaviour
{
    public GameObject tile;
    private float dubTileW;
    private Vector3 currCors;

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

        dubTileW = Mathf.Sqrt(3);
        generatePlane(10, 10);
        //SpawnOnCors(0, 0, 0);
        //generateCircle(10);
    }

    void SpawnOnCors(int x, int y, int z)
    {
        float xU = dubTileW / 2 * x - dubTileW / 2 * y;
        float zU = -1.5f * z;

        Instantiate(tile, new Vector3(xU, 0, zU), transform.rotation);
    }

    void generateCircle(int circle)
    {
        for (int radius = 0; radius < circle; radius++)
        {
            currCors.Set(-1 * (radius + 1), 0, 1 * (radius + 1));
            for (int dir = 0; dir < 6; dir++)
            {
                int lineL = radius + 1;
                for (int i = 0; i < lineL; i++)
                {
                    currCors += directions[dir];
                    SpawnOnCors((int)currCors.x, (int)currCors.y, (int)currCors.z);
                }
            }
        }
    }

    void generatePlane(int x, int y)
    {

        currCors.Set(0, 0, 0);
        for (int ix = 0; ix < x; ix++)
        {
            for (int iy = 0; iy < y; iy++)
            {
                SpawnOnCors((int)currCors.x, (int)currCors.y, (int)currCors.z);
                if (iy != y - 1)
                {
                    if (ix % 2 == 0)
                    {
                        if (iy % 2 == 0)
                        {
                            currCors.Set(currCors.x + directions[2].x, currCors.y + directions[2].y, currCors.z + directions[2].z);
                        }
                        else
                        {
                            currCors.Set(currCors.x + directions[3].x, currCors.y + directions[3].y, currCors.z + directions[3].z);
                        }
                    }
                    else
                    {
                        if (y % 2 == 0)
                        {
                            if (iy % 2 == 0)
                            {
                                currCors.Set(currCors.x + directions[5].x, currCors.y + directions[5].y, currCors.z + directions[5].z);
                            }
                            else
                            {
                                currCors.Set(currCors.x + directions[0].x, currCors.y + directions[0].y, currCors.z + directions[0].z);
                            }
                        }
                        else
                        {
                            if (iy % 2 == 0)
                            {
                                currCors.Set(currCors.x + directions[0].x, currCors.y + directions[0].y, currCors.z + directions[0].z);
                            }
                            else
                            {
                                currCors.Set(currCors.x + directions[5].x, currCors.y + directions[5].y, currCors.z + directions[5].z);
                            }
                        }
                    }
                }
            }
            currCors.Set(currCors.x + directions[1].x, currCors.y + directions[1].y, currCors.z + directions[1].z);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
