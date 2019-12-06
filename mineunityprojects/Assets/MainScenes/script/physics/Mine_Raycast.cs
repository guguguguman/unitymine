using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;

/*레이캐스트와 연관된 기능을 모아놓은 스크립트입니다.*/
public class Mine_Raycast : MonoBehaviour  
{

    [Serializable]
    public class block_key
    {
        public float x;
        public float y;
        public float z;
    }

    public GameObject mycamx;
    public GameObject mycamy;

    //카메라와 카메라가 보고있는 앞지점 오브젝트 이를 기준으로 레이캐스트실행
    public Camera Cameras;
    public GameObject Camfront;

    public GameObject m_player_obj;
    public GameObject ray_cube;
    // 블록 생성지점을 나타내는 오브젝트
    public GameObject ray_cube2;
    // 블록 파괴지점을 나타내는 오브젝트
    public GameObject ray_cube3;
    // 블록파괴할때 크랙효과를 나타내주는 오브젝트
    public GameObject ray_cube4;
    public Mine_BlockCreate mymine;
    public Mine_ItemManager item_manager;

    public float clmpx2 = 0; // 저장용 값
    public float clmpy2 = 0;
    public float clmpz2 = 0;
    public float clmpx4 = 0; // 저장용 값
    public float clmpy4 = 0;
    public float clmpz4 = 0;
    public int blockdestroynum = 0; // 블록 부술때 쓰는 수
    public int[] blockdestroynums; // 블록 부술때 쓰는 배열 블록 번호와 대응
    public Material[] crak_mat;

    public string file_path;
    BinaryFormatter formatter = new BinaryFormatter();
    DirectoryInfo di;
    FileInfo ii;

    List<block_key> add_block = new List<block_key>(); // 추가블록 리스트
    List<block_key> dest_block = new List<block_key>(); // 파괴블록 리스트

    Vector2[] uvs = new Vector2[24];
    Vector2 _00 = new Vector2();
    Vector2 _10 = new Vector2();
    Vector2 _01 = new Vector2();
    Vector2 _11 = new Vector2();

    private bool destkeydown = false;
    private int myblocknum = 0;
    private float xSensitivity = 1.0f;
    private float ySensitivity = 1.0f;

    private void Start()
    {
        file_path = Application.dataPath;
        ray_cube.transform.position = mycamy.transform.position;
        InvokeRepeating("Raycast_CamBoxF" , 0 , 0.1f);
    }

    /*마우스 돌렸을때 카메라 회전을 위한 메소드*/
    void Fps_ControllF()
    {
        //회전하고 싶은 축과 입력축이 반대인 것에 유의
        float yRot = Input.GetAxis("Mouse X") * xSensitivity;
        float xRot = Input.GetAxis("Mouse Y") * ySensitivity;

        //오브젝트(기준이 되는 축을 유지해야 됨)와 카메라 회전을 분리해야 됨
        //쿼터니안은 곱해야 누적됨
        mycamy.transform.localRotation *= Quaternion.Euler(0, yRot, 0);
        mycamx.transform.localRotation *= Quaternion.Euler(-xRot, 0, 0);//부호 주의
    }

    /*해당 지점에 박스가 있는지 비어있는지 체크용 메소드*/
    int Compare_BoxnumF(float x, float y, float z)
    {
        //우선 싱글톤에있는 전역 메소드로 체크
        int myboxnum = MinePostBox.OnOffBlockreturnF(new Vector3(x,y,z), 0, 0, 0);

        //키값으로 세이브 파일 딕셔너리 접근해서 있는지 없는지 있다면 해당 블록 atribute로 설정
        Mine_BlockCreate.block_Dics savedblocks = MinePostBox.mysavefile;
        Mine_BlockCreate.ThreeStringKey keys;
        keys.x = (int)x;
        keys.y = (int)y;
        keys.z = (int)z;
        if (savedblocks != null)
        {
            if (savedblocks.block_diction.ContainsKey(keys) == true)
            {
                myboxnum = savedblocks.block_diction[keys].atribute;
            }
        }

        return myboxnum;
    }
    Vector3[] comparevec = new Vector3[8];
    Vector3[] comparevec2 = new Vector3[2];
    void Raycast_CamBoxF()
    {
        ray_cube.transform.position = mycamy.transform.position;
        for (int i = 0; i < 8; i++)
        {
            ray_cube.transform.position += Cameras.transform.forward * 0.95f;
            Vector3 myvec = new Vector3(ray_cube.transform.position.x , ray_cube.transform.position.y , ray_cube.transform.position.z);

            myvec.x = (int)((float)Math.Truncate(myvec.x + 0.5f) + (float)Math.Ceiling(myvec.x - 0.5f)) *0.5f;
            myvec.y = (int)Math.Round(myvec.y + 1f);
            myvec.z = (int)((float)Math.Truncate(myvec.z + 0.5f) + (float)Math.Ceiling(myvec.z - 0.5f)) *0.5f;
            int[] myblocknums2 = new int[6];
            int myblocknums = Compare_BoxnumF(myvec.x, myvec.y, myvec.z);

            if (myblocknums > 0) {
                float best = 999;
                int bestindex = -1;
                float best2 = 999;
                int bestindex2 = -1;
                comparevec[0] = myvec; comparevec[0].x += 1;
                comparevec[1] = myvec; comparevec[1].x -= 1;
                comparevec[2] = myvec; comparevec[2].z += 1;
                comparevec[3] = myvec; comparevec[3].z -= 1;
                comparevec[4] = myvec; comparevec[4].x += 1; comparevec[4].z += 1;
                comparevec[5] = myvec; comparevec[5].x -= 1; comparevec[5].z -= 1;
                comparevec[6] = myvec; comparevec[6].x += 1; comparevec[6].z -= 1;
                comparevec[7] = myvec; comparevec[7].x -= 1; comparevec[7].z += 1;
                for (int i2=0; i2 < 8; ++i2) //xz중에 가장 가까운것 하나 찾음
                {
                    if (Vector3.Distance(Camfront.transform.position, comparevec[i2]) < best)
                    {
                        best = Vector3.Distance(Camfront.transform.position, comparevec[i2]);
                        bestindex = i2;
                    }
                }
                comparevec2[0] = myvec; comparevec2[0].y += 1;
                comparevec2[1] = myvec; comparevec2[1].y -= 1;
                for (int i2 = 0; i2 < 2; ++i2) //xz중에 가장 가까운것 하나 찾음
                {
                    if (Vector3.Distance(Camfront.transform.position, comparevec2[i2]) < best2)
                    {
                        best2 = Vector3.Distance(Camfront.transform.position, comparevec2[i2]);
                        bestindex2 = i2;
                    }
                }

                if (bestindex != -1 && bestindex2 != -1)
                {
                    if (Compare_BoxnumF(comparevec[bestindex].x, comparevec[bestindex].y, comparevec[bestindex].z) != 0
                    && Compare_BoxnumF(comparevec2[bestindex2].x, comparevec2[bestindex2].y, comparevec2[bestindex2].z) != 0
                         ) // 만약 가까운 xz 하나와 y 하나가 모두 막혀있지 않을시
                    {
                        ray_cube3.transform.position = comparevec[bestindex];
                    }
                    else
                    {
                        ray_cube3.transform.position = myvec;

                    }
                }

                best = 999;
                bestindex = -1;
                comparevec[0] =  ray_cube3.transform.position; comparevec[0].y += 1;
                comparevec[1] =  ray_cube3.transform.position; comparevec[1].y -= 1;
                comparevec[2] =  ray_cube3.transform.position; comparevec[2].x += 1;
                comparevec[3] =  ray_cube3.transform.position; comparevec[3].x -= 1;
                comparevec[4] =  ray_cube3.transform.position; comparevec[4].z += 1;
                comparevec[5] = ray_cube3.transform.position; comparevec[5].z -= 1;
                for (int i2=0; i2 < 6; ++i2)
                {
                    myblocknums2[i2] = Compare_BoxnumF(comparevec[i2].x, comparevec[i2].y, comparevec[i2].z);
                    if (Vector3.Distance(Camfront.transform.position, comparevec[i2]) < best && myblocknums2[i2] ==0 )
                        // udrlfb중 블록이 비어있고 카메라와의 거리 기준으로 가장 짧은것을 찾는다.
                    {
                        best = Vector3.Distance(Camfront.transform.position, comparevec[i2]);
                        bestindex = i2;
                    }
                }
                if (bestindex != -1)
                {
                    ray_cube2.transform.position = comparevec[bestindex];

                }
                else
                {
                    ray_cube2.transform.position = myvec;

                }
                //Debug.DrawLine(mycamy.transform.position, (mycamx.transform.forward), Color.red);


                break;
            }

        }
    }
    void Update()
    {
        Fps_ControllF();
        if (Input.GetKeyDown(KeyCode.G)) // G를 누르면 해당 레이블록1이 있는 곳에 블록 생성
        {
            add_block.Add(new block_key { x = ray_cube2.transform.position.x, y = ray_cube2.transform.position.y, z = ray_cube2.transform.position.z });
            Create_BlockF(ray_cube2.transform.position.x, ray_cube2.transform.position.y, ray_cube2.transform.position.z);
        }
        if (Input.GetKey(KeyCode.T)) // T를 누르면 해당 레이블록2가 있는 곳에 있는 블록 파괴
        { 
            if (destkeydown == false)
            {
                destkeydown = true;
                myblocknum = Compare_BoxnumF(ray_cube3.transform.position.x, ray_cube3.transform.position.y, ray_cube3.transform.position.z);
            }
            if (destkeydown == true)
            {
                if (myblocknum > 0 && myblocknum!= 5) // 물블록이 아니거나 비어있지 않을시
                {
                    ray_cube4.transform.GetChild(0).GetComponent<MeshRenderer>().enabled = true;
                    ray_cube4.transform.position = new Vector3(ray_cube3.transform.position.x, ray_cube3.transform.position.y, ray_cube3.transform.position.z);
                    Crak_BlockF(blockdestroynum, myblocknum);
                    if (blockdestroynum > blockdestroynums[myblocknum]) // 파괴변수초과시 파괴
                    {
                        blockdestroynum = 0;
                        //dest_block.Add(new block_key { x = clmpx4, y = clmpy4, z = clmpz4 });
                        Destroy_BlockF(ray_cube3.transform.position.x, ray_cube3.transform.position.y, ray_cube3.transform.position.z, myblocknum);
                    }
                    else
                    {
                        // 맨손일때 1씩 증가
                        blockdestroynum += 1;
                    }
                }
            }
        }
        if (Input.GetKeyUp(KeyCode.T))
        {
            ray_cube4.transform.GetChild(0).GetComponent<MeshRenderer>().enabled = false;
            blockdestroynum = 0;
            destkeydown = false;
        }
    }
    int savecraknum = 0;

    /*블록 파괴시 균열 일어나는 효과를 나타내주는 메서드*/

    void Crak_BlockF(int blockdestroynum , int myblocknum)
    {
        int xnum = 1;
        int ynum = 16;

        if (savecraknum != blockdestroynum / (blockdestroynums[myblocknum] / 10)) {

            for (int i = 0; i < 10; ++i)
            {
                if (blockdestroynum / (blockdestroynums[myblocknum] / 10) == i) // 50 / 5 
                {
                    savecraknum = i;


                    xnum = i +1;
                    _00.x = 0 + 0.0625f * (xnum - 1); _00.y = 1 - 0.0625f * ynum;
                    _10.x = 0 + 0.0625f * xnum; _10.y = 1 - 0.0625f * ynum;
                    _01.x = 0 + 0.0625f * (xnum - 1); _01.y = 1 - 0.0625f * (ynum - 1);
                    _11.x = 0 + 0.0625f * xnum; _11.y = 1 - 0.0625f * (ynum - 1);

                    uvs[0] = _11; uvs[1] = _01; uvs[2] = _00; uvs[3] = _10; // 아랫면
                    uvs[4] = _11; uvs[5] = _01; uvs[6] = _00; uvs[7] = _10; // 왼쪽
                    uvs[8] = _11; uvs[9] = _01; uvs[10] = _00; uvs[11] = _10; // 정면
                    uvs[12] = _11; uvs[13] = _01; uvs[14] = _00; uvs[15] = _10; // 뒷면
                    uvs[16] = _11; uvs[17] = _01; uvs[18] = _00; uvs[19] = _10; // 오른쪽
                    uvs[20] = _11; uvs[21] = _01; uvs[22] = _00; uvs[23] = _10; // 윗면
                    break;
                }
            }
            ray_cube4.transform.GetChild(0).GetComponent<MeshFilter>().mesh.uv = uvs;
        }
    }

    //레이캐스트된 지점에 새로운 블록을 생성해주는 메서드
    void Create_BlockF(float myx, float myy, float myz)
    {
        // 생성된 블록리스트가 1이상일시
        for (int i = 0; i < add_block.Count; ++i)
        {
            //ex)만약 포지션이 187 , 청크 길이가 9면 188 에 저장될수 잇도록 187을 9로 나눈뒤 곱하여 180으로 만든뒤 187 % 180은 7남는다.7을 4.5로 나누어 1이하면 180 1넘으면 188

            float clmpx = (float)Math.Truncate(add_block[i].x / mymine.m_mychunkwidth) *  mymine.m_mychunkwidth; // 5로 나눈것을 반올림 한 것에 5를 곱한다
            if ((add_block[i].x % clmpx) / (mymine.m_mychunkwidth * 0.5f) > 1) { clmpx += mymine.m_mychunkwidth; }
            float clmpy = (float)Math.Truncate(add_block[i].y / mymine.m_mychunkwidth) *  mymine.m_mychunkwidth;
            if ((add_block[i].y % clmpy) / (mymine.m_mychunkwidth * 0.5f) > 1) { clmpy += mymine.m_mychunkwidth; }
            float clmpz = (float)Math.Truncate(add_block[i].z / mymine.m_mychunkwidth) * mymine.m_mychunkwidth;
            if (( add_block[i].z % clmpz) / (mymine.m_mychunkwidth * 0.5f) > 1) { clmpz += mymine.m_mychunkwidth; }
            //해당 블록위치와 동일한 파일을 연다
            Mine_BlockCreate.block_Dics savedblocks;
            //lock (MinePostBox.minesavelock)
            {
                savedblocks = MinePostBox.mysavefile;
            }
            Mine_BlockCreate.ThreeStringKey keys;
            keys.x = (int)myx;
            keys.y = (int)myy;
            keys.z = (int)myz;


            if (savedblocks != null)
            {
                if (savedblocks.block_diction.ContainsKey(keys) == true)
                {
                    savedblocks.block_diction[keys].atribute = 1;
                }
                else
                {
                    savedblocks.block_diction.Add(keys, new Mine_BlockCreate.block_Dic { atribute = 1, x = (int)myx, y = (int)myy, z = (int)myz });
                }
            }

            lock (MinePostBox.minesavelock)
            {
                MinePostBox.mysavefile = savedblocks;
            }
            Block_UpdateF(0, new Vector3(clmpx, clmpy, clmpz));
            add_block.Remove(add_block.First());
        }
    }
    //레이캐스트된 지점에 있는 블록을 파괴(atribute 를 0으로 저장)해주는 메서드
    void Destroy_BlockF(float myx , float myy , float myz ,int myblocknum)
    {
        // 생성된 블록리스트가 1이상일시
        //for (int i = 0; i < dest_block.Count; ++i)
        {
            //만약 187이면 188 에 저장될수 잇도록 187을 9로 나눈뒤 곱하여 180으로 만든뒤 187 % 180은 7남는다.7을 4로 나누어 1이하면 180 1넘으면 188

            float clmpx = (float)Math.Truncate(myx / mymine.m_mychunkwidth) * mymine.m_mychunkwidth; // 5로 나눈것을 반올림 한 것에 5를 곱한다
            if ((myx % mymine.m_mychunkwidth) / (mymine.m_mychunkwidth * 0.5f) >= 1) { clmpx += mymine.m_mychunkwidth; } // 나머지를 절반으로 나눳을시 
            float clmpy = (float)Math.Truncate(myy / mymine.m_mychunkwidth) * mymine.m_mychunkwidth ;
            if ((myy % mymine.m_mychunkwidth) / (mymine.m_mychunkwidth * 0.5f) >= 1) { clmpy += mymine.m_mychunkwidth; }
            float clmpz = (float)Math.Truncate(myz / mymine.m_mychunkwidth) * mymine.m_mychunkwidth;
            if ((myz % mymine.m_mychunkwidth) / (mymine.m_mychunkwidth * 0.5f) >= 1) { clmpz += mymine.m_mychunkwidth; }
            //해당 블록위치와 동일한 파일을 연다

            Mine_BlockCreate.ThreeStringKey keys;
            keys.x = (int)myx;
            keys.y = (int)myy;
            keys.z = (int)myz;
            lock (MinePostBox.minesavelock)
            {
                if (MinePostBox.mysavefile != null)
                {
                    if (MinePostBox.mysavefile.block_diction.ContainsKey(keys) == true)
                    {
                        MinePostBox.mysavefile.block_diction[keys].atribute = 0;
                    }
                    else
                    {
                        MinePostBox.mysavefile.block_diction.Add(keys, new Mine_BlockCreate.block_Dic { atribute = 0, x = (int)myx, y = (int)myy, z = (int)myz });
                    }
                }
            }

            Block_UpdateF(0,new Vector3(clmpx, clmpy, clmpz));
            item_manager.call_instant_item(myblocknum,new Vector3(myx, myy, myz));
            //dest_block.Remove(dest_block.First());
        }


    }
    void Block_UpdateF(int num , Vector3 chunkvec)
    {
        float searchposx = chunkvec.x;
        float searchposy = chunkvec.y;
        float searchposz = chunkvec.z;
        for (int i2 = 0; i2 < mymine.m_block_node_obj_array.Length; ++i2)
        {
            if ((int)mymine.m_block_node_obj_array[i2].transform.position.x / (int)mymine.m_mychunkwidth * mymine.m_mychunkwidth == searchposx
            && (int)mymine.m_block_node_obj_array[i2].transform.position.y / (int)mymine.m_mychunkwidth * mymine.m_mychunkwidth == searchposy
            && (int)mymine.m_block_node_obj_array[i2].transform.position.z / (int)mymine.m_mychunkwidth * mymine.m_mychunkwidth == searchposz)
            {
                //Instantiate(ray_cube4, mymine.m_block_node_obj_array[i2].transform.position, Quaternion.identity);
                lock (mymine.lockObject[i2 % MinePostBox.mytreadnum])
                {
                    mymine.c_mypost.PushData2(new MinePostBox.myInputData { chunknum = i2, chunkvector = mymine.m_block_node_obj_array[i2].transform.position}, i2 % MinePostBox.mytreadnum);
                }
            }

            
            if ((int)mymine.m_block_node_obj_array[i2].transform.position.x / (int)mymine.m_mychunkwidth * mymine.m_mychunkwidth == searchposx - mymine.m_mychunkwidth
                && (int)mymine.m_block_node_obj_array[i2].transform.position.y / (int)mymine.m_mychunkwidth * mymine.m_mychunkwidth == searchposy
                 && (int)mymine.m_block_node_obj_array[i2].transform.position.z / (int)mymine.m_mychunkwidth * mymine.m_mychunkwidth == searchposz)
            {
                //Instantiate(ray_cube4, mymine.m_block_node_obj_array[i2].transform.position, Quaternion.identity);
                lock (mymine.lockObject[i2 % MinePostBox.mytreadnum])
                {
                    mymine.c_mypost.PushData2(new MinePostBox.myInputData { chunknum = i2, chunkvector = new Vector3( mymine.m_block_node_obj_array[i2].transform.position.x , mymine.m_block_node_obj_array[i2].transform.position.y , mymine.m_block_node_obj_array[i2].transform.position.z) }, i2 % MinePostBox.mytreadnum);
                }
            }
            if ((int)mymine.m_block_node_obj_array[i2].transform.position.x / (int)mymine.m_mychunkwidth * mymine.m_mychunkwidth == searchposx + mymine.m_mychunkwidth
                && (int)mymine.m_block_node_obj_array[i2].transform.position.y / (int)mymine.m_mychunkwidth * mymine.m_mychunkwidth == searchposy
                 && (int)mymine.m_block_node_obj_array[i2].transform.position.z / (int)mymine.m_mychunkwidth * mymine.m_mychunkwidth == searchposz)
            {
                //Instantiate(ray_cube4, mymine.m_block_node_obj_array[i2].transform.position, Quaternion.identity);
                lock (mymine.lockObject[i2 % MinePostBox.mytreadnum])
                {
                    mymine.c_mypost.PushData2(new MinePostBox.myInputData { chunknum = i2, chunkvector = new Vector3(mymine.m_block_node_obj_array[i2].transform.position.x, mymine.m_block_node_obj_array[i2].transform.position.y, mymine.m_block_node_obj_array[i2].transform.position.z) }, i2 % MinePostBox.mytreadnum);
                }
            }
            if ((int)mymine.m_block_node_obj_array[i2].transform.position.x / (int)mymine.m_mychunkwidth * mymine.m_mychunkwidth == searchposx
            && (int)mymine.m_block_node_obj_array[i2].transform.position.y / (int)mymine.m_mychunkwidth * mymine.m_mychunkwidth == searchposy - mymine.m_mychunkwidth
             && (int)mymine.m_block_node_obj_array[i2].transform.position.z / (int)mymine.m_mychunkwidth * mymine.m_mychunkwidth == searchposz)
            {
                //Instantiate(ray_cube4, mymine.m_block_node_obj_array[i2].transform.position, Quaternion.identity);
                lock (mymine.lockObject[i2 % MinePostBox.mytreadnum])
                {
                    mymine.c_mypost.PushData2(new MinePostBox.myInputData { chunknum = i2, chunkvector = new Vector3(mymine.m_block_node_obj_array[i2].transform.position.x , mymine.m_block_node_obj_array[i2].transform.position.y, mymine.m_block_node_obj_array[i2].transform.position.z) }, i2 % MinePostBox.mytreadnum);
                }
            }
            if ((int)mymine.m_block_node_obj_array[i2].transform.position.x / (int)mymine.m_mychunkwidth * mymine.m_mychunkwidth == searchposx
                && (int)mymine.m_block_node_obj_array[i2].transform.position.y / (int)mymine.m_mychunkwidth * mymine.m_mychunkwidth == searchposy + mymine.m_mychunkwidth
                 && (int)mymine.m_block_node_obj_array[i2].transform.position.z / (int)mymine.m_mychunkwidth * mymine.m_mychunkwidth == searchposz)
            {
                //Instantiate(ray_cube4, mymine.m_block_node_obj_array[i2].transform.position, Quaternion.identity);
                lock (mymine.lockObject[i2 % MinePostBox.mytreadnum])
                {
                    mymine.c_mypost.PushData2(new MinePostBox.myInputData { chunknum = i2, chunkvector = new Vector3(mymine.m_block_node_obj_array[i2].transform.position.x , mymine.m_block_node_obj_array[i2].transform.position.y , mymine.m_block_node_obj_array[i2].transform.position.z) }, i2 % MinePostBox.mytreadnum);
                }
            }
            if ((int)mymine.m_block_node_obj_array[i2].transform.position.x / (int)mymine.m_mychunkwidth * mymine.m_mychunkwidth == searchposx
             && (int)mymine.m_block_node_obj_array[i2].transform.position.y / (int)mymine.m_mychunkwidth * mymine.m_mychunkwidth == searchposy
             && (int)mymine.m_block_node_obj_array[i2].transform.position.z / (int)mymine.m_mychunkwidth * mymine.m_mychunkwidth == searchposz - mymine.m_mychunkwidth)
            {
                //Instantiate(ray_cube4, mymine.m_block_node_obj_array[i2].transform.position, Quaternion.identity);
                lock (mymine.lockObject[i2 % MinePostBox.mytreadnum])
                {
                    mymine.c_mypost.PushData2(new MinePostBox.myInputData { chunknum = i2, chunkvector = new Vector3(mymine.m_block_node_obj_array[i2].transform.position.x , mymine.m_block_node_obj_array[i2].transform.position.y, mymine.m_block_node_obj_array[i2].transform.position.z ) }, i2 % MinePostBox.mytreadnum);
                }
            }
            if ((int)mymine.m_block_node_obj_array[i2].transform.position.x / (int)mymine.m_mychunkwidth * mymine.m_mychunkwidth == searchposx
                && (int)mymine.m_block_node_obj_array[i2].transform.position.y / (int)mymine.m_mychunkwidth * mymine.m_mychunkwidth == searchposy
                 && (int)mymine.m_block_node_obj_array[i2].transform.position.z / (int)mymine.m_mychunkwidth * mymine.m_mychunkwidth == searchposz + mymine.m_mychunkwidth)
            {
                //Instantiate(ray_cube4, mymine.m_block_node_obj_array[i2].transform.position, Quaternion.identity);
                lock (mymine.lockObject[i2 % MinePostBox.mytreadnum])
                {
                    mymine.c_mypost.PushData2(new MinePostBox.myInputData { chunknum = i2, chunkvector = new Vector3(mymine.m_block_node_obj_array[i2].transform.position.x, mymine.m_block_node_obj_array[i2].transform.position.y, mymine.m_block_node_obj_array[i2].transform.position.z)}, i2 % MinePostBox.mytreadnum);
                }
            }
            
            
        }
    }


} 

