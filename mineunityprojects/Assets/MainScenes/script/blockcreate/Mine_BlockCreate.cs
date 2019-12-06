using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;


/*
 블록생성을 제어하는 메인 스크립트 입니다.
 */
public class Mine_BlockCreate : MonoBehaviour
{
    // 딕셔너리로 된 세이브 데이터 접근을 위해 vector3형식을 구조체로 선언합니다.
    public struct ThreeStringKey
    {
        public int x;
        public int y;
        public int z;
    }

    // 딕셔너리로 된 세이브 파일에 들어갈 value 값
    [System.Serializable]
    public class block_Dic
    {
        public int filternum;
        public int x;
        public int y;
        public int z;
        public float on_off = 0;
        public int atribute = 0;
        public int under_ground = 0;
    }

    [System.Serializable]
    public class block_Dics
    {
       // public List<block_Dic> block_list = new List<block_Dic>();
        public Dictionary<ThreeStringKey, block_Dic> block_diction = new Dictionary<ThreeStringKey, block_Dic>();
    }

    //파일 위치 세이브용
    public string filepath;
    [Tooltip("플레이어 오브젝트입니다.")]
    public GameObject m_player_obj;
    [Tooltip("생성할 청크 오브젝트 입니다.")]
    public GameObject m_block_node_obj;
    [Tooltip("물 블록 연산용 스크립트입니다.")]
    public Mine_Water c_mywater;
    [Tooltip("쓰레드 스크립트 접근용입니다.")]
    public Mine_Thread c_minethread;
    [Tooltip("싱글톤 접근용입니다.")]
    public MinePostBox c_mypost;
    [Tooltip("생성된 청크오브젝트에 대한 접근을 하기 위한 배열입니다.")]
    public GameObject[] m_block_node_obj_array;
    [Tooltip(" 청크에있는 메쉬 필터 컴퍼넌트를 따로 관리하는 배열입니다.")]
    private MeshFilter[] m_myfilter_array;


    // 각 쓰레드별로 큐에 접근할때 쓸 동기화용 락 오브젝트
    public object savelock = new object();
    public object[] lockObject;

    //청크를 얼마만큼 또 얼마만한 길이로 생성할지 결정하는 변수들
    [Tooltip("전체 가로길이에 대입될 청크의 갯수 : 반드시 홀수여야함.")]
    public int m_mywidth = 9;
    [Tooltip("전체 세로길이에 대입될 청크의 갯수 : 반드시 홀수여야함.")]
    public int m_myheight = 9;
    [Tooltip("청크의 가로 세로 길이 : 반드시 홀수여야함.")]
    public int m_mychunkwidth = 7;

    // 노이즈를 구성하는 변수 : 수치를 조정하면 지형이 달라지게 됨.

    public float[] atri1;
    #region 노이즈 수치 변수설명
    /*
    //인덱스 번호 , 이름(역할) , 디폴트 수치 

    ----지표면& 곡선 조정 변수----

    0 지표면 곡선 조정용 변수 30
    1 지표면 곡선 조정용 0.006
    2 지표면 위에 있는 지형을 표현하기 위한 변수 0.2

     ----지표면아래& 지표면 위 조정 변수----

    11 지표아래 빈공간 조정 변수 0.02
    12 지표아래 빈공간 조정 변수 0.02
    13 지표위 빈공간 조정 변수 0.06
    15 지표위 빈공간 조정 변수 0.1
    16 나무 빈도 조정 변수 0.1
    
    ----thres수치----
    0~10000 범위 1000단위로 10% 확률로 그린다의 의미정도로 생각. 5000이면 50% 확률로 해당 '범위'의 노이즈를 그린다.
    
    20 지표하한계 thres  노이즈 변수 3000
    21 지표 튀어나온 부분 thres 노이즈 4600
    22 지표 튀어나온 부분 thres 노이즈 4600
    23 나무생성용  thres 노이즈 2850
    24 석탄용 thres 노이즈 2500
    25 철광석 thres 노이즈 2250
    26 코발트 thres 노이즈 1900
    27 코발트 thres 블루 노이즈 1500
    28 금 thres 노이즈 2075
    29 다이아 thres 노이즈 1950

    ----광석 수치 변수----
    
    31 석탄용 노이즈 조정 변수 0.2
    32 철광석용 노이즈 조정 변수 0.22
    33 코발트용 노이즈 조정 변수 0.175
    34 코발트블루용 노이즈 조정 변수 0.185
    35 금용 노이즈 조정 변수 0.11
    36 다이아용 노이즈 조정 변수 0.1
    */
    #endregion

    //플레이어 이동시 청크 끝에서 끝으로 밀어줄때 쓰이는 변수
    private float l_savex;
    private float l_savey;
    private float l_savez;

    private MinePostBox.myMeshData l_mymeshdat = new MinePostBox.myMeshData();

    //메쉬 구성을 위해 각 데이터별로 메모리 캐싱
    private Mesh[] l_mymesh;
    private List<Vector3> l_vertices = new List<Vector3>();
    private List<Vector3> l_normales = new List<Vector3>();
    private List<Vector2> l_uvs = new List<Vector2>();
    private List<int> l_triangles = new List<int>();




    // 최초에 메모리를 캐싱해주고 lock 오브젝트 할당 싱글턴 스크립트 실행
    void Start()
    {
        // 플레이어의 위치는 청크 넓이와 맞아야한다. 즉, 청크 넓이로 플레이어의 xyz가 나뉘어져야함 청크 넓이가 7이고 플레이어 위치가 211이면 210으로 바꿔줌
        m_player_obj.transform.position = 
            new Vector3(m_player_obj.transform.position.x - (m_player_obj.transform.position.x % m_mychunkwidth)
            , m_player_obj.transform.position.y - (m_player_obj.transform.position.y % m_mychunkwidth)
               , m_player_obj.transform.position.z - (m_player_obj.transform.position.z % m_mychunkwidth)
            );

        //싱글톤 초기화 및 싱글톤에 데이터 삽입
        filepath = Application.dataPath;
        c_mypost = MinePostBox.GetInstance;
        MinePostBox.chunkwidth = m_mychunkwidth;
        MinePostBox.minesavelock = savelock;
        MinePostBox.noise_Value = atri1;
        c_mypost.set_myset();
        lockObject = new object[MinePostBox.mytreadnum];
        m_myfilter_array = new MeshFilter[m_mywidth * m_mywidth * m_myheight];
        m_block_node_obj_array = new GameObject[m_mywidth * m_mywidth * m_myheight];
        l_mymesh = new Mesh[m_mywidth * m_mywidth * m_myheight];

        for (int k = 0; k < MinePostBox.mytreadnum; ++k)
        {
            lockObject[k] = new object();
        }

        // 최초에 여러개의 블록을 관리할 '청크'를 생성해주고 플레이어 기준 (청크 넓이* -2) 에서 (청크 넓이 * 2 +1) 까지 위치를 설정해줍니다.
        // 이때 청크 넓이는 반드시 홀수여야 합니다.
        int num = 0;
        for (int i = -m_mywidth / 2; i < m_mywidth / 2 + 1; ++i)
        {
            for (int i2 = -m_mywidth / 2; i2 < m_mywidth / 2 + 1; ++i2)
            {
                for (int i3 = -m_myheight / 2; i3 < m_myheight / 2 + 1; ++i3)
                {
                    GameObject a = Instantiate(m_block_node_obj, Vector3.zero, Quaternion.identity);
                    a.transform.position =
                        new Vector3(
                        m_player_obj.transform.position.x + i * m_mychunkwidth
                        , m_player_obj.transform.position.y + i3 * m_mychunkwidth
                        , m_player_obj.transform.position.z + i2 * m_mychunkwidth
                        );
                    m_block_node_obj_array[num] = a;
                    m_myfilter_array[num] = a.GetComponent<MeshFilter>();

                    c_mypost.PushData2(new MinePostBox.myInputData { chunknum = num, chunkvector = m_block_node_obj_array[num].transform.position }, num % MinePostBox.mytreadnum);
                  

                    l_mymesh[num] = new Mesh();
                    m_myfilter_array[num].mesh = l_mymesh[num];
                    num += 1;
                }
            }
        }
        l_savex = m_player_obj.transform.position.x;
        l_savey = m_player_obj.transform.position.y;
        l_savez = m_player_obj.transform.position.z;

        

        // 청크 오브젝트 생성이 완료되면 쓰레드 함수를 실행시켜 쓰레드를 켜줍니다.
        c_minethread = new Mine_Thread(lockObject , savelock);
        c_minethread.mypost = c_mypost;
        StartCoroutine(Deque_reciveF());
        c_mywater.mySingleton_Instance = c_mypost;
        c_mywater.Water_Start();
        InvokeRepeating("Block_UpdateF", 1.0f, 0.01f);

    }

     /*   데이터를 로드하기 위한 메서드
     * 
     * 
     */
    void First_Data_Load()
    {
        BinaryFormatter formatter = new BinaryFormatter();
        DirectoryInfo di;
        di = new DirectoryInfo(filepath + "/save1");
        if (di.Exists == false)
        {
            di.Create();// 폴더없으면 폴더생성
        }
        FileInfo ii = new FileInfo(filepath + "/save1" + "/dsave" + ".bin");
        FileStream stream;
        if (ii.Exists) // 파일이 존재할 경우
        {
            lock (savelock)
            {
                stream = new FileStream(filepath + "/save1" + "/dsave" + ".bin", FileMode.Open);
                MinePostBox.mysavefile = (block_Dics)formatter.Deserialize(stream);
                stream.Close();
            }
        }
    }

    /*   데이터를 저장하기 위한 메서드
     * 
     * 
     */
    void Data_save()
    {
        BinaryFormatter formatter = new BinaryFormatter();
        FileInfo ii = new FileInfo(filepath + "/save1" + "/dsave" + ".bin");
        FileStream stream;
        stream = new FileStream(filepath + "/save1" + "/dsave" + ".bin", FileMode.Create);
        lock (MinePostBox.minesavelock) {
            formatter.Serialize(stream, MinePostBox.mysavefile);
        }
        stream.Close();

    }

    /*   게임을 껏을시 호출 쓰레드 모두 종료하기 위한 메서드
   * 
   * 
   */
    private void OnApplicationQuit()
    {
        c_minethread.application_end();
    }


    /*    mine thread 에서 각 쓰레드 별로 처리한 데이터가 큐에 쌓인뒤 그것을 하나씩 꺼내서 블록으로 그려주는 메서드
     * 
     * 
     */

    IEnumerator Deque_reciveF()
    {
        while (true) // 큐가 0이상일때
        {
            for (int i = 0; i < MinePostBox.mytreadnum; ++i) {
                if (MinePostBox.listnum[i] > 0)
                {
                    //큐에서 데이터 꺼내온다.
                    lock (lockObject[i])
                    {
                        l_mymeshdat = c_mypost.GetData(i);
                    }
                    if (l_mymeshdat.datanum == -1) { break; }

                    //메쉬 구성 데이터 리스트 비워준다
                    l_vertices.Clear();
                    l_normales.Clear();
                    l_uvs.Clear();
                    l_triangles.Clear();

                    //메쉬 구성 데이터 리스트에 쌓아준다.
                    for (int j = 0; j < 24 * l_mymeshdat.myvertnum; ++j) { l_vertices.Add(l_mymeshdat.myvert[j]); }
                    for (int j = 0; j < 24 * l_mymeshdat.myvertnum; ++j) { l_normales.Add(l_mymeshdat.mynormal[j]); }
                    for (int j = 0; j < 24 * l_mymeshdat.myvertnum; ++j) { l_uvs.Add(l_mymeshdat.myuv[j]); }
                    for (int j = 0; j < 36 * l_mymeshdat.myvertnum; ++j) { l_triangles.Add(l_mymeshdat.mytriangle[j]); }

                    //해당 데이터로 메쉬 재구성
                    l_mymesh[l_mymeshdat.datanum].Clear();
                    if (l_mymeshdat.myvertnum * 24 > 65000)
                    { l_mymesh[l_mymeshdat.datanum].indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; }
                    l_mymesh[l_mymeshdat.datanum].SetVertices(l_vertices);
                    l_mymesh[l_mymeshdat.datanum].SetNormals(l_normales);
                    l_mymesh[l_mymeshdat.datanum].SetUVs(0, l_uvs);
                    l_mymesh[l_mymeshdat.datanum].SetTriangles(l_triangles, 0);
                    l_mymesh[l_mymeshdat.datanum].RecalculateBounds();

                    //구성된 메쉬를 필터에 넘겨준다.
                    m_myfilter_array[l_mymeshdat.datanum].mesh = l_mymesh[l_mymeshdat.datanum];
                 
                    if (MinePostBox.loading < m_mywidth * m_mywidth * m_myheight)
                    {
                        MinePostBox.loading += 1;
                    }
                }

                if (MinePostBox.listnumWater[i] > 0)
                {
                    //큐에서 데이터 꺼내온다.
                    lock (lockObject[i])
                    {
                        l_mymeshdat = c_mypost.GetDataWater(i);
                    }
                    if (l_mymeshdat.datanum == -1) { break; }

                    //메쉬 구성 데이터 리스트 비워준다
                    l_vertices.Clear();
                    l_normales.Clear();
                    l_uvs.Clear();
                    l_triangles.Clear();

                    //메쉬 구성 데이터 리스트에 쌓아준다.
                    for (int j = 0; j < 24 * l_mymeshdat.myvertnum; ++j) { l_vertices.Add(l_mymeshdat.myvert[j]); }
                    for (int j = 0; j < 24 * l_mymeshdat.myvertnum; ++j) { l_normales.Add(l_mymeshdat.mynormal[j]); }
                    for (int j = 0; j < 24 * l_mymeshdat.myvertnum; ++j) { l_uvs.Add(l_mymeshdat.myuv[j]); }
                    for (int j = 0; j < 36 * l_mymeshdat.myvertnum; ++j) { l_triangles.Add(l_mymeshdat.mytriangle[j]); }

                    //해당 데이터로 메쉬 재구성
                    l_mymesh[l_mymeshdat.datanum].Clear();
                    if (l_mymeshdat.myvertnum * 24 > 65000)
                    { l_mymesh[l_mymeshdat.datanum].indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; }
                    l_mymesh[l_mymeshdat.datanum].SetVertices(l_vertices);
                    l_mymesh[l_mymeshdat.datanum].SetNormals(l_normales);
                    l_mymesh[l_mymeshdat.datanum].SetUVs(0, l_uvs);
                    l_mymesh[l_mymeshdat.datanum].SetTriangles(l_triangles, 0);
                    l_mymesh[l_mymeshdat.datanum].RecalculateBounds();

                    //구성된 메쉬를 필터에 넘겨준다.
                    m_myfilter_array[l_mymeshdat.datanum].mesh = l_mymesh[l_mymeshdat.datanum];

                    if (MinePostBox.loading < m_mywidth * m_mywidth * m_myheight)
                    {
                        MinePostBox.loading += 1;
                    }
                }
            }
            yield return new WaitForSeconds(0.02f);
        }
    }

    /*
     * 플레이어가 이동했을때 각 청크들을 끝에서 앞으로 움직여주고 움직인 청크에 대해
     * 다시금 쓰레드로 보내 메쉬를 재구성하게 하는 메서드
     */

    void Block_UpdateF() 
    {

        bool mychks = false;

        if (mychks == false )
        {
            mychks = true;
            // x 축에 대한 연산
            // 절대값 차이로 플레이어가 청크 넓이만큼 움직였을시
            while (Mathf.Abs(m_player_obj.transform.position.x - l_savex) >= m_mychunkwidth)
            {
                int num = 0;
                // 양수일경우 오른쪽이동 : 왼쪽 끝에있는 애들 전부 오른쪽 끝으로 이동
                if (l_savex - m_player_obj.transform.position.x >= 0)
                {
                    for (int i = 0; i < m_mywidth * m_mywidth * m_myheight; ++i)
                    {
                        // 차이가 청크 넓이 넘어간 경우를 다 찾아서 위치변경
                        if (m_block_node_obj_array[i].transform.position.x - l_savex >= (int)(m_mywidth * 0.5f) * m_mychunkwidth)
                        {
                            //x만 플레이어 기준 왼쪽 끝인 지점으로 이동
                            m_block_node_obj_array[i].transform.position =
                                new Vector3(
                                    l_savex - (int)(m_mywidth * 0.5f) * m_mychunkwidth - m_mychunkwidth,
                                m_block_node_obj_array[i].transform.position.y,
                                  m_block_node_obj_array[i].transform.position.z
                                );
                            m_myfilter_array[i].mesh = null;
                            lock (lockObject[num % MinePostBox.mytreadnum])
                            {
                                c_mypost.PushData2(new MinePostBox.myInputData { chunknum = i, chunkvector = m_block_node_obj_array[i].transform.position }, num % MinePostBox.mytreadnum);
                            }

                            num += 1;

                        }
                    }
                    l_savex = l_savex - m_mychunkwidth;

                }
                // 음수일경우 왼쪽이동 : 오른쪽 끝에있는 애들 전부 왼쪽 끝으로 이동
                else if (l_savex - m_player_obj.transform.position.x < 0)
                {

                    for (int i = 0; i < m_mywidth * m_mywidth * m_myheight; ++i)
                    {
                        // 차이가 청크 넓이 넘어간 경우를 다 찾아서 위치변경
                        if (l_savex - m_block_node_obj_array[i].transform.position.x >= (int)(m_mywidth * 0.5f) * m_mychunkwidth)
                        {
                            //x만 플레이어 기준 오른쪽 끝인 지점으로 이동
                            m_block_node_obj_array[i].transform.position =
                                new Vector3(
                                   l_savex + (int)(m_mywidth * 0.5f) * m_mychunkwidth + m_mychunkwidth,
                                m_block_node_obj_array[i].transform.position.y,
                                  m_block_node_obj_array[i].transform.position.z
                                );
                            m_myfilter_array[i].mesh = null;
                            lock (lockObject[num % MinePostBox.mytreadnum])
                            {
                                c_mypost.PushData2(new MinePostBox.myInputData { chunknum = i, chunkvector = m_block_node_obj_array[i].transform.position }, num % MinePostBox.mytreadnum);
                            }
                            num += 1;
                        }
                    }
                    l_savex = l_savex + m_mychunkwidth;

                }
            }
            // y 축에 대한 연산
            // 절대값 차이로 플레이어가 청크 넓이만큼 움직였을시
            while (Mathf.Abs(m_player_obj.transform.position.y - l_savey) >= m_mychunkwidth)
            {
                int num = 0;
                // 양수일경우 오른쪽이동 : 왼쪽 끝에있는 애들 전부 오른쪽 끝으로 이동
                if (l_savey - m_player_obj.transform.position.y >= 0)
                {

                    for (int i = 0; i < m_mywidth * m_myheight * m_mywidth; ++i)
                    {
                        // 차이가 청크 넓이 넘어간 경우를 다 찾아서 위치변경
                        if (m_block_node_obj_array[i].transform.position.y - l_savey >= (int)(m_myheight * 0.5f) * m_mychunkwidth)
                        {
                            //x만 플레이어 기준 왼쪽 끝인 지점으로 이동
                            m_block_node_obj_array[i].transform.position =
                                new Vector3(
                                    m_block_node_obj_array[i].transform.position.x,
                                  l_savey - (int)(m_myheight * 0.5f) * m_mychunkwidth - m_mychunkwidth,
                                  m_block_node_obj_array[i].transform.position.z
                                );
                            m_myfilter_array[i].mesh = null;
                            lock (lockObject[num % MinePostBox.mytreadnum])
                            {
                                c_mypost.PushData2(new MinePostBox.myInputData { chunknum = i, chunkvector = m_block_node_obj_array[i].transform.position }, num % MinePostBox.mytreadnum);
                            }
                            num += 1;
                        }
                    }
                    l_savey = l_savey - m_mychunkwidth;

                }
                // 음수일경우 왼쪽이동 : 오른쪽 끝에있는 애들 전부 왼쪽 끝으로 이동
                else if (l_savey - m_player_obj.transform.position.y < 0)
                {

                    for (int i = 0; i < m_mywidth * m_myheight * m_mywidth; ++i)
                    {
                        // 차이가 청크 넓이 넘어간 경우를 다 찾아서 위치변경
                        if (l_savey - m_block_node_obj_array[i].transform.position.y >= (int)(m_myheight * 0.5f) * m_mychunkwidth)
                        {
                            //x만 플레이어 기준 오른쪽 끝인 +15지점으로 이동
                            m_block_node_obj_array[i].transform.position =
                                new Vector3(
                                  m_block_node_obj_array[i].transform.position.x,
                                  l_savey + (int)(m_myheight * 0.5f) * m_mychunkwidth + m_mychunkwidth,
                                  m_block_node_obj_array[i].transform.position.z
                                );
                            m_myfilter_array[i].mesh = null;
                            lock (lockObject[num % MinePostBox.mytreadnum])
                            {
                                c_mypost.PushData2(new MinePostBox.myInputData { chunknum = i, chunkvector = m_block_node_obj_array[i].transform.position }, num % MinePostBox.mytreadnum);
                            }
                            num += 1;
                        }
                    }
                    l_savey = l_savey + m_mychunkwidth;

                }
            }
            // z 축에 대한 연산
            // 절대값 차이로 플레이어가 청크 넓이만큼 움직였을시
            while (Mathf.Abs(m_player_obj.transform.position.z - l_savez) >= m_mychunkwidth)
            {
                int num = 0;
                // 양수일경우 오른쪽이동 : 왼쪽 끝에있는 애들 전부 오른쪽 끝으로 이동
                if (l_savez - m_player_obj.transform.position.z >= 0)
                {

                    for (int i = 0; i < m_mywidth * m_mywidth * m_myheight; ++i)
                    {
                        // 차이가 청크 넓이 넘어간 경우를 다 찾아서 위치변경
                        if (m_block_node_obj_array[i].transform.position.z - l_savez >= (int)(m_mywidth * 0.5f) * m_mychunkwidth)
                        {
                            //x만 플레이어 기준 왼쪽 끝인 지점으로 이동
                            m_block_node_obj_array[i].transform.position =
                                new Vector3(
                                    m_block_node_obj_array[i].transform.position.x,
                                   m_block_node_obj_array[i].transform.position.y,
                                  l_savez - (int)(m_mywidth * 0.5f) * m_mychunkwidth - m_mychunkwidth
                                );
                            m_myfilter_array[i].mesh = null;
                            lock (lockObject[num % MinePostBox.mytreadnum])
                            {
                                c_mypost.PushData2(new MinePostBox.myInputData { chunknum = i, chunkvector = m_block_node_obj_array[i].transform.position }, num % MinePostBox.mytreadnum);
                            }
                            num += 1;
                        }
                    }
                    l_savez = l_savez - m_mychunkwidth;

                }
                // 음수일경우 왼쪽이동 : 오른쪽 끝에있는 애들 전부 왼쪽 끝으로 이동
                else if (l_savez - m_player_obj.transform.position.z < 0)
                {

                    for (int i = 0; i < m_mywidth * m_mywidth * m_myheight; ++i)
                    {
                        // 차이가 청크 넓이 넘어간 경우를 다 찾아서 위치변경
                        if (l_savez - m_block_node_obj_array[i].transform.position.z >= (int)(m_mywidth * 0.5f) * m_mychunkwidth)
                        {
                            //x만 플레이어 기준 오른쪽 끝인 지점으로 이동
                            m_block_node_obj_array[i].transform.position =
                                new Vector3(
                                    m_block_node_obj_array[i].transform.position.x,
                                   m_block_node_obj_array[i].transform.position.y,
                                  l_savez + (int)(m_mywidth * 0.5f) * m_mychunkwidth + m_mychunkwidth
                                );
                            m_myfilter_array[i].mesh = null;
                            lock (lockObject[num % MinePostBox.mytreadnum])
                            {
                                c_mypost.PushData2(new MinePostBox.myInputData { chunknum = i, chunkvector = m_block_node_obj_array[i].transform.position }, num % MinePostBox.mytreadnum);
                            }
                            num += 1;
                        }
                    }
                    l_savez = l_savez + m_mychunkwidth;

                }
            }

            mychks = false;

        }
    }

    /*
    * 프레임체크용
    * 
    */
    void OnGUI()
    {
        int w = Screen.width, h = Screen.height;

        GUIStyle style = new GUIStyle();

        Rect rect = new Rect(0, 0, w, h * 2 / 100);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = h * 2 / 100;
        style.normal.textColor = new Color(0.0f, 0.0f, 0.5f, 1.0f);
        float msec = Time.deltaTime * 1000.0f;
        float fps = 1.0f / Time.deltaTime;
        string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
        GUI.Label(rect, text, style);
    }
}

