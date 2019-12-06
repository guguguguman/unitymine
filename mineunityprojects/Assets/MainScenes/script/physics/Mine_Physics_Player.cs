using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/*물리에 연관된 기능을 모아둔 스크립트입니다. 이동, 점프, 낙하 */
public class Mine_Physics_Player : MonoBehaviour
{

    public GameObject playerobj;
    public GameObject TEST;
    public Mine_BlockCreate mymine;

    //플레이어가 얼만큼 빠르게 이동할지 결정해주는 스피드 변수
    public float speed = 1.0f;

    //플레이어 이동 위해 플레이어 주변에 설정된 트랜스폼 포지션용 오브젝트 , 8방향 + 플레이어 세이브 key 검색용 지점
    public GameObject[] pos;
    public GameObject[] pos2;


    //플레이어가 점프 수치 변수
    public float forcey = 0;
    //플레이어가 낙하 수치 변수
    public float forcey2 = -0.1f;

    private float savex = 0;
    private float savey = 0;
    private float savez = 0;

    void Start()
    {
        savex = playerobj.transform.position.x;
        savey = playerobj.transform.position.y;
        savez = playerobj.transform.position.z;
    }

    /*
    해당 블록이 막혀있는지 세이브 딕션과 함께 판단하는 메서드 , 딕셔너리형 세이브 파일에서 key로 검색하기 위해 라운드된 y값을 받아줘야함.
    */
    int Myblocknuset_Noise2F(Vector3 posv, Vector3 posv2)
    {
        Mine_BlockCreate.ThreeStringKey keys;
        keys.x = (int)((Math.Truncate(posv2.x + 0.5f) + Math.Ceiling(posv2.x - 0.5f)) * 0.5f);
        keys.y = (int)(posv2.y);
        keys.z = (int)((Math.Truncate(posv2.z + 0.5f) + Math.Ceiling(posv2.z - 0.5f)) * 0.5f);
        int answer = MinePostBox.OnOffBlockreturnF(new Vector3(keys.x, keys.y, keys.z), 0, 0, 0);
        TEST.transform.position = new Vector3(keys.x, keys.y, keys.z);
        if (MinePostBox.mysavefile != null)
        {

            if (MinePostBox.mysavefile.block_diction.ContainsKey(keys) == true)
            {
                answer = MinePostBox.mysavefile.block_diction[keys].atribute;
            }
        }
        return answer;
    }
    /*
     해당 블록이 막혀있는지 세이브 딕션과 함께 판단하는 메서드
     */
    int MyblocknumsetF(Vector3 posv)
    {


        int answer = MinePostBox.OnOffBlockreturnF(new Vector3(posv.x, posv.y , posv.z), 0, 0, 0);

        Mine_BlockCreate.ThreeStringKey keys;
        keys.x = (int)((Math.Truncate(posv.x + 0.5f) + Math.Ceiling(posv.x - 0.5f)) * 0.5f);
        keys.y = (int)(posv.y);
        keys.z = (int)((Math.Truncate(posv.z + 0.5f) + Math.Ceiling(posv.z - 0.5f)) * 0.5f);

        if (MinePostBox.mysavefile != null)
        {

            if (MinePostBox.mysavefile.block_diction.ContainsKey(keys) == true)
            {
                answer = MinePostBox.mysavefile.block_diction[keys].atribute;
            }
        }

        return answer;
    }
    Vector3[] myposvec = new Vector3[16];
    Vector3[] myposvec2 = new Vector3[16];
    void CameraMovingF(int ymove)
    {

        // 플레이어 주변에 8방향으로 설정해놓은 포지션을 받아와서 백터 3로 바꿔줍니다.
        Vector3 testvec = pos[8].transform.position;
        testvec.y = (int)Math.Round(pos[8].transform.position.y);
        myposvec[0] = new Vector3(pos[0].transform.position.x, pos[0].transform.position.y, pos[0].transform.position.z);
        myposvec[1] = new Vector3(pos[1].transform.position.x, pos[1].transform.position.y, pos[1].transform.position.z);
        myposvec[2] = new Vector3(pos[2].transform.position.x, pos[2].transform.position.y, pos[2].transform.position.z);
        myposvec[3] = new Vector3(pos[3].transform.position.x, pos[3].transform.position.y, pos[3].transform.position.z);
        myposvec[4] = new Vector3(pos[4].transform.position.x, pos[4].transform.position.y, pos[4].transform.position.z);
        myposvec[5] = new Vector3(pos[5].transform.position.x, pos[5].transform.position.y, pos[5].transform.position.z);
        myposvec[6] = new Vector3(pos[6].transform.position.x, pos[6].transform.position.y, pos[6].transform.position.z);
        myposvec[7] = new Vector3(pos[7].transform.position.x, pos[7].transform.position.y, pos[7].transform.position.z);


        //세이브 블록 체크를 위해 y를 라운드된 값을 넣어줍니다.
        myposvec2[0] = new Vector3(pos2[0].transform.position.x, testvec.y, pos2[0].transform.position.z);
        myposvec2[1] = new Vector3(pos2[1].transform.position.x, testvec.y, pos2[1].transform.position.z);
        myposvec2[2] = new Vector3(pos2[2].transform.position.x, testvec.y, pos2[2].transform.position.z);
        myposvec2[3] = new Vector3(pos2[3].transform.position.x, testvec.y, pos2[3].transform.position.z);
        myposvec2[4] = new Vector3(pos2[4].transform.position.x, testvec.y, pos2[4].transform.position.z);
        myposvec2[5] = new Vector3(pos2[5].transform.position.x, testvec.y, pos2[5].transform.position.z);
        myposvec2[6] = new Vector3(pos2[6].transform.position.x, testvec.y, pos2[6].transform.position.z);
        myposvec2[7] = new Vector3(pos2[7].transform.position.x, testvec.y, pos2[7].transform.position.z);

        myposvec[8 ] = new Vector3(pos[0].transform.position.x, pos[0].transform.position.y+1, pos[0].transform.position.z);
        myposvec[9 ] = new Vector3(pos[1].transform.position.x, pos[1].transform.position.y+1, pos[1].transform.position.z);
        myposvec[10] = new Vector3(pos[2].transform.position.x, pos[2].transform.position.y+1, pos[2].transform.position.z);
        myposvec[11] = new Vector3(pos[3].transform.position.x, pos[3].transform.position.y+1, pos[3].transform.position.z);
        myposvec[12] = new Vector3(pos[4].transform.position.x, pos[4].transform.position.y+1, pos[4].transform.position.z);
        myposvec[13] = new Vector3(pos[5].transform.position.x, pos[5].transform.position.y+1, pos[5].transform.position.z);
        myposvec[14] = new Vector3(pos[6].transform.position.x, pos[6].transform.position.y+1, pos[6].transform.position.z);
        myposvec[15] = new Vector3(pos[7].transform.position.x, pos[7].transform.position.y+1, pos[7].transform.position.z);


        //세이브 블록 체크를 위해 y를 라운드된 값을 넣어줍니다.
        myposvec2[8 ] = new Vector3(pos2[0].transform.position.x, testvec.y+1, pos2[0].transform.position.z);
        myposvec2[9 ] = new Vector3(pos2[1].transform.position.x, testvec.y+1, pos2[1].transform.position.z);
        myposvec2[10] = new Vector3(pos2[2].transform.position.x, testvec.y+1, pos2[2].transform.position.z);
        myposvec2[11] = new Vector3(pos2[3].transform.position.x, testvec.y+1, pos2[3].transform.position.z);
        myposvec2[12] = new Vector3(pos2[4].transform.position.x, testvec.y+1, pos2[4].transform.position.z);
        myposvec2[13] = new Vector3(pos2[5].transform.position.x, testvec.y+1, pos2[5].transform.position.z);
        myposvec2[14] = new Vector3(pos2[6].transform.position.x, testvec.y+1, pos2[6].transform.position.z);
        myposvec2[15] = new Vector3(pos2[7].transform.position.x, testvec.y+1, pos2[7].transform.position.z);
        // y쪽으로 내려가고있을때 -1에 뭔가 있는데 이동하려고 하면 안됨 

        //if (MyblocknumsetF(new Vector3(playerobj.transform.position.x, playerobj.transform.position.y, playerobj.transform.position.z)) == 0
        //&& MyblocknumsetF(new Vector3(playerobj.transform.position.x, playerobj.transform.position.y +1, playerobj.transform.position.z)) == 0 
        // )
        {
          
            if (Input.GetKey(KeyCode.D))
            {
                if (Myblocknuset_Noise2F(myposvec[2], myposvec2[2]) == 0  || Myblocknuset_Noise2F(myposvec[2], myposvec2[2]) == 5)
                {
                    if (Myblocknuset_Noise2F(myposvec[10], myposvec2[10]) == 0 || Myblocknuset_Noise2F(myposvec[10], myposvec2[10]) == 5)
                    {
                        playerobj.transform.position = Vector3.MoveTowards(playerobj.transform.position, new Vector3(pos[2].transform.position.x, playerobj.transform.position.y, pos[2].transform.position.z), speed * Time.deltaTime);
                        savex = playerobj.transform.position.x;
                        savey = playerobj.transform.position.y;
                        savez = playerobj.transform.position.z;
                    }
                    else
                    {
                        playerobj.transform.position = new Vector3(savex, playerobj.transform.position.y, savez);

                    }

                }
                else
                {
                    playerobj.transform.position = new Vector3(savex, playerobj.transform.position.y, savez);

                }
            }
             if (Input.GetKey(KeyCode.A))
            {
                if (Myblocknuset_Noise2F(myposvec[3], myposvec2[3]) == 0 || Myblocknuset_Noise2F(myposvec[3], myposvec2[3]) == 5)
                {
                    if (Myblocknuset_Noise2F(myposvec[10], myposvec2[10]) == 0 || Myblocknuset_Noise2F(myposvec[10], myposvec2[10]) == 5)
                    {
                        playerobj.transform.position = Vector3.MoveTowards(playerobj.transform.position, new Vector3(pos[3].transform.position.x, playerobj.transform.position.y, pos[3].transform.position.z), speed * Time.deltaTime);
                        savex = playerobj.transform.position.x;
                        savey = playerobj.transform.position.y;
                        savez = playerobj.transform.position.z;
                    }
                    else
                    {
                        playerobj.transform.position = new Vector3(savex, playerobj.transform.position.y, savez);

                    }
                }
                else
                {
                    playerobj.transform.position = new Vector3(savex, playerobj.transform.position.y, savez);
                }
            }
             if (Input.GetKey(KeyCode.W))
            {
                if (Myblocknuset_Noise2F(myposvec[0], myposvec2[0]) == 0 || Myblocknuset_Noise2F(myposvec[0], myposvec2[0]) == 5)
                {
                    if (Myblocknuset_Noise2F(myposvec[8], myposvec2[8]) == 0 || Myblocknuset_Noise2F(myposvec[8], myposvec2[8]) == 5)
                    {
                        playerobj.transform.position = Vector3.MoveTowards(playerobj.transform.position, new Vector3(pos[0].transform.position.x, playerobj.transform.position.y, pos[0].transform.position.z), speed * Time.deltaTime);
                        savex = playerobj.transform.position.x;
                        savey = playerobj.transform.position.y;
                        savez = playerobj.transform.position.z;
                    }
                    else
                    {
                        playerobj.transform.position = new Vector3(savex, playerobj.transform.position.y, savez);

                    }
                }
                else
                {
                    playerobj.transform.position = new Vector3(savex, playerobj.transform.position.y, savez);
                }
            }
             if (Input.GetKey(KeyCode.S))
            {
                if (Myblocknuset_Noise2F(myposvec[1], myposvec2[1]) == 0 || Myblocknuset_Noise2F(myposvec[1], myposvec2[1]) == 5)
                {
                    if (Myblocknuset_Noise2F(myposvec[9], myposvec2[9]) == 0 || Myblocknuset_Noise2F(myposvec[9], myposvec2[9]) == 5)
                    {
                        playerobj.transform.position = Vector3.MoveTowards(playerobj.transform.position, new Vector3(pos[1].transform.position.x, playerobj.transform.position.y, pos[1].transform.position.z), speed * Time.deltaTime);
                        savex = playerobj.transform.position.x;
                        savey = playerobj.transform.position.y;
                        savez = playerobj.transform.position.z;
                    }
                    else
                    {
                        playerobj.transform.position = new Vector3(savex, playerobj.transform.position.y, savez);

                    }
                }
                else
                {
                    playerobj.transform.position = new Vector3(savex, playerobj.transform.position.y, savez);
                }
            }
            
            else if (Input.GetKey(KeyCode.A) && Input.GetKey(KeyCode.W)) // 좌상단
            {
                if (Myblocknuset_Noise2F(myposvec[4], myposvec2[4]) == 0 || Myblocknuset_Noise2F(myposvec[4], myposvec2[4]) == 5)
                {
                    if (Myblocknuset_Noise2F(myposvec[12], myposvec2[12]) == 0 || Myblocknuset_Noise2F(myposvec[12], myposvec2[12]) == 5)
                    {
                        playerobj.transform.position = Vector3.MoveTowards(playerobj.transform.position, new Vector3(pos[4].transform.position.x, playerobj.transform.position.y, pos[4].transform.position.z), speed * Time.deltaTime);
                        savex = playerobj.transform.position.x;
                        savey = playerobj.transform.position.y;
                        savez = playerobj.transform.position.z;
                    }
                    else
                    {
                        playerobj.transform.position = new Vector3(savex, playerobj.transform.position.y, savez);

                    }
                }
                else
                {
                    playerobj.transform.position = new Vector3(savex , playerobj.transform.position.y, savez );
                }
            }
            else if (Input.GetKey(KeyCode.D) && Input.GetKey(KeyCode.W)) // 우상단
            {
                if (Myblocknuset_Noise2F(myposvec[5], myposvec2[5]) == 0 || Myblocknuset_Noise2F(myposvec[5], myposvec2[5]) == 5)
                {
                    if (Myblocknuset_Noise2F(myposvec[13], myposvec2[13]) == 0 || Myblocknuset_Noise2F(myposvec[13], myposvec2[13]) == 5)
                    {
                        playerobj.transform.position = Vector3.MoveTowards(playerobj.transform.position, new Vector3(pos[5].transform.position.x, playerobj.transform.position.y, pos[5].transform.position.z), speed * Time.deltaTime);
                        savex = playerobj.transform.position.x;
                        savey = playerobj.transform.position.y;
                        savez = playerobj.transform.position.z;
                    }
                    else
                    {
                        playerobj.transform.position = new Vector3(savex, playerobj.transform.position.y, savez);

                    }
                }
                else
                {
                     playerobj.transform.position = new Vector3(savex , playerobj.transform.position.y, savez );
                }
            }

            else if (Input.GetKey(KeyCode.A) && Input.GetKey(KeyCode.S)) // 좌하단
            {
                if (Myblocknuset_Noise2F(myposvec[6], myposvec2[6]) == 0 || Myblocknuset_Noise2F(myposvec[6], myposvec2[6]) == 5)
                {
                    if (Myblocknuset_Noise2F(myposvec[14], myposvec2[14]) == 0 || Myblocknuset_Noise2F(myposvec[14], myposvec2[14]) == 5)
                    {
                        playerobj.transform.position = Vector3.MoveTowards(playerobj.transform.position, new Vector3(pos[6].transform.position.x, playerobj.transform.position.y, pos[6].transform.position.z), speed * Time.deltaTime);
                        savex = playerobj.transform.position.x;
                        savey = playerobj.transform.position.y;
                        savez = playerobj.transform.position.z;
                    }
                    else
                    {
                        playerobj.transform.position = new Vector3(savex, playerobj.transform.position.y, savez);

                    }
                }
                else
                {
                    playerobj.transform.position = new Vector3(savex , playerobj.transform.position.y, savez );
                }
            }
            else if (Input.GetKey(KeyCode.D) && Input.GetKey(KeyCode.S)) // 우하단
            {
                if (Myblocknuset_Noise2F(myposvec[7], myposvec2[7]) == 0 || Myblocknuset_Noise2F(myposvec[7], myposvec2[7]) == 5)
                {
                    if (Myblocknuset_Noise2F(myposvec[15], myposvec2[15]) == 0 || Myblocknuset_Noise2F(myposvec[15], myposvec2[15]) == 5)
                    {
                        playerobj.transform.position = Vector3.MoveTowards(playerobj.transform.position, new Vector3(pos[7].transform.position.x, playerobj.transform.position.y, pos[7].transform.position.z), speed * Time.deltaTime);
                        savex = playerobj.transform.position.x;
                        savey = playerobj.transform.position.y;
                        savez = playerobj.transform.position.z;
                    }
                    else
                    {
                        playerobj.transform.position = new Vector3(savex, playerobj.transform.position.y, savez);

                    }
                }
                else
                {
                    playerobj.transform.position = new Vector3(savex, playerobj.transform.position.y, savez );
                }
            }
                    
        }

    }
    void CameraMoving2F()
    {
        {

            if (Input.GetKey(KeyCode.D))
            {
                if (MyblocknumsetF(new Vector3(pos[2].transform.position.x, pos[2].transform.position.y, pos[2].transform.position.z)) == 0
                   // && MyblocknumsetF(new Vector3(pos[2].transform.position.x, pos[2].transform.position.y - 1, pos[2].transform.position.z)) == 0
                    )
                {
                    playerobj.transform.position = Vector3.MoveTowards(playerobj.transform.position, new Vector3(pos[2].transform.position.x, playerobj.transform.position.y, pos[2].transform.position.z), speed * Time.deltaTime);
                    savex = playerobj.transform.position.x;
                    savey = playerobj.transform.position.y;
                    savez = playerobj.transform.position.z;

                }
                else
                {
                    playerobj.transform.position = new Vector3(savex + 0.1f, playerobj.transform.position.y, savez);

                }
            }
            else if (Input.GetKey(KeyCode.A))
            {
                if (MyblocknumsetF(new Vector3(pos[3].transform.position.x, pos[3].transform.position.y, pos[3].transform.position.z)) == 0
                     //&& MyblocknumsetF(new Vector3(pos[3].transform.position.x, pos[3].transform.position.y - 1, pos[3].transform.position.z)) == 0
                     )
                {
                    playerobj.transform.position = Vector3.MoveTowards(playerobj.transform.position, new Vector3(pos[3].transform.position.x, playerobj.transform.position.y, pos[3].transform.position.z), speed * Time.deltaTime);
                    savex = playerobj.transform.position.x;
                    savey = playerobj.transform.position.y;
                    savez = playerobj.transform.position.z;
                }
                else
                {
                     playerobj.transform.position = new Vector3(savex - 0.1f, playerobj.transform.position.y, savez);
                }
            }
            else if (Input.GetKey(KeyCode.W))
            {
                if (MyblocknumsetF(new Vector3(pos[0].transform.position.x, pos[0].transform.position.y, pos[0].transform.position.z)) == 0
                    // && MyblocknumsetF(new Vector3(pos[0].transform.position.x, pos[0].transform.position.y - 1, pos[0].transform.position.z)) == 0
                     )
                {
                    playerobj.transform.position = Vector3.MoveTowards(playerobj.transform.position, new Vector3(pos[0].transform.position.x, playerobj.transform.position.y, pos[0].transform.position.z), speed * Time.deltaTime);
                    savex = playerobj.transform.position.x;
                    savey = playerobj.transform.position.y;
                    savez = playerobj.transform.position.z;
                }
                else
                {
                    playerobj.transform.position = new Vector3(savex, playerobj.transform.position.y, savez + 0.1f);
                }
            }
            else if (Input.GetKey(KeyCode.S))
            {
                if (MyblocknumsetF(new Vector3(pos[1].transform.position.x, pos[1].transform.position.y, pos[1].transform.position.z)) == 0
                     //&& MyblocknumsetF(new Vector3(pos[1].transform.position.x, pos[1].transform.position.y - 1, pos[1].transform.position.z)) == 0
                     )
                {
                    playerobj.transform.position = Vector3.MoveTowards(playerobj.transform.position, new Vector3(pos[1].transform.position.x, playerobj.transform.position.y, pos[1].transform.position.z), speed * Time.deltaTime);
                    savex = playerobj.transform.position.x;
                    savey = playerobj.transform.position.y;
                    savez = playerobj.transform.position.z;
                }
                else
                {
                    playerobj.transform.position = new Vector3(savex, playerobj.transform.position.y, savez - 0.1f);
                }
            }
            else if (Input.GetKey(KeyCode.A) && Input.GetKey(KeyCode.W)) // 좌상단
            {
                if (MyblocknumsetF(new Vector3(pos[4].transform.position.x, pos[4].transform.position.y, pos[4].transform.position.z)) == 0
                     //&& MyblocknumsetF(new Vector3(pos[4].transform.position.x, pos[4].transform.position.y - 1, pos[4].transform.position.z)) == 0
                     )
                {
                    playerobj.transform.position = Vector3.MoveTowards(playerobj.transform.position, new Vector3(pos[4].transform.position.x, playerobj.transform.position.y, pos[4].transform.position.z), speed * Time.deltaTime);
                    savex = playerobj.transform.position.x;
                    savey = playerobj.transform.position.y;
                    savez = playerobj.transform.position.z;
                }
                else
                {
                    playerobj.transform.position = new Vector3(savex + 0.1f, playerobj.transform.position.y, savez - 0.1f);
                }
            }
            else if (Input.GetKey(KeyCode.D) && Input.GetKey(KeyCode.W)) // 우상단
            {
                if (MyblocknumsetF(new Vector3(pos[5].transform.position.x, pos[5].transform.position.y, pos[5].transform.position.z)) == 0
                     //&& MyblocknumsetF(new Vector3(pos[5].transform.position.x, pos[5].transform.position.y - 1, pos[5].transform.position.z)) == 0
                     )
                {
                    playerobj.transform.position = Vector3.MoveTowards(playerobj.transform.position, new Vector3(pos[5].transform.position.x, playerobj.transform.position.y, pos[5].transform.position.z), speed * Time.deltaTime);
                    savex = playerobj.transform.position.x;
                    savey = playerobj.transform.position.y;
                    savez = playerobj.transform.position.z;
                }
                else
                {
                    playerobj.transform.position = new Vector3(savex - 0.1f, playerobj.transform.position.y , savez - 0.1f);
                }
            }

            else if (Input.GetKey(KeyCode.A) && Input.GetKey(KeyCode.S)) // 좌하단
            {
                if (MyblocknumsetF(new Vector3(pos[6].transform.position.x, pos[6].transform.position.y, pos[6].transform.position.z)) == 0
                   //  && MyblocknumsetF(new Vector3(pos[6].transform.position.x, pos[6].transform.position.y - 1, pos[6].transform.position.z)) == 0
                     )
                {
                    playerobj.transform.position = Vector3.MoveTowards(playerobj.transform.position, new Vector3(pos[6].transform.position.x, playerobj.transform.position.y, pos[6].transform.position.z), speed * Time.deltaTime);
                    savex = playerobj.transform.position.x;
                    savey = playerobj.transform.position.y;
                    savez = playerobj.transform.position.z;
                }
                else
                {
                    playerobj.transform.position = new Vector3(savex + 0.1f, playerobj.transform.position.y, savez + 0.1f);
                }
            }
            else if (Input.GetKey(KeyCode.D) && Input.GetKey(KeyCode.S)) // 우하단
            {
                if (MyblocknumsetF(new Vector3(pos[7].transform.position.x, pos[7].transform.position.y, pos[7].transform.position.z)) == 0
                    // && MyblocknumsetF(new Vector3(pos[7].transform.position.x, pos[7].transform.position.y - 1, pos[7].transform.position.z)) == 0
                     )
                {
                    playerobj.transform.position = Vector3.MoveTowards(playerobj.transform.position, new Vector3(pos[7].transform.position.x, playerobj.transform.position.y, pos[7].transform.position.z), speed * Time.deltaTime);
                    savex = playerobj.transform.position.x;
                    savey = playerobj.transform.position.y;
                    savez = playerobj.transform.position.z;
                }
                else
                {
                    playerobj.transform.position = new Vector3(savex - 0.1f, playerobj.transform.position.y, savez + 0.1f);
                }
            }
        }
    }

    void Update()
    {
        // 플레이어가 있는 지점 pos 8 기준으로 
        Vector3 testvec = pos[8].transform.position;
        testvec.y = (int)Math.Round(pos[8].transform.position.y - 1f);
        if (forcey <= 0 && Input.GetKeyDown(KeyCode.Space) && MyblocknumsetF(new Vector3(pos[8].transform.position.x, testvec.y - 1, pos[8].transform.position.z)) > 0)
        {
            forcey = 7.5f;
        }
        if (forcey > 0)
        {
            // 점프시 아무것도 걸리는게 없을시
            if (MyblocknumsetF(new Vector3(testvec.x, testvec.y + 1, testvec.z)) == 0)
            {
                playerobj.transform.Translate(Vector3.up * forcey * Time.deltaTime);
            }
            // 플레이어가 점프때 뭔가 걸렸을시
            else if (MyblocknumsetF(new Vector3(testvec.x, testvec.y + 1, testvec.z)) > 0)
            {
                forcey = 0;
            }
            forcey -= 5f * Time.deltaTime;
        }

        //로딩 완료됬을때부터 움직이기 가능
        if (MinePostBox.loading >= mymine.m_myheight * mymine.m_mywidth * mymine.m_mywidth) {
            //블록이 없을때 내려앉음
            if (MyblocknumsetF(new Vector3(pos[8].transform.position.x , testvec.y, pos[8].transform.position.z)) == 0)
                {
                //TEST.transform.position = new Vector3(pos[8].transform.position.x, testvec.y, pos[8].transform.position.z);
                    playerobj.transform.position = new Vector3(playerobj.transform.position.x, playerobj.transform.position.y + forcey2 * Time.deltaTime, playerobj.transform.position.z);
                    forcey2 -= 5f * Time.deltaTime;
                CameraMovingF(0);
            }
            else
            {
                forcey2 = -0.01f;
                CameraMovingF(0);
            }
            //물인경우 가라앉음
            if (MyblocknumsetF(new Vector3(pos[8].transform.position.x, testvec.y, pos[8].transform.position.z)) == 5) // 블록이 없거나 물인경우 내려앉는다.
            {
                CameraMovingF(0);
                //TEST.transform.position = new Vector3(pos[8].transform.position.x, testvec.y, pos[8].transform.position.z);
                playerobj.transform.position = new Vector3(playerobj.transform.position.x, playerobj.transform.position.y - 1 * Time.deltaTime, playerobj.transform.position.z);
                if (Input.GetKey(KeyCode.Space)
                    )
                {
                    if (MyblocknumsetF(new Vector3(pos[8].transform.position.x, testvec.y + 1, pos[8].transform.position.z)) == 5
                    || MyblocknumsetF(new Vector3(pos[8].transform.position.x, testvec.y + 1, pos[8].transform.position.z)) == 0)
                    {
                        playerobj.transform.position = new Vector3(playerobj.transform.position.x, playerobj.transform.position.y + 3 * Time.deltaTime, playerobj.transform.position.z);
                    }
                }
            }

        }
        
    }
}
