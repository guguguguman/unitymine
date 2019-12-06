using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;


/* 데이터를 주고받기 위한 싱글톤 클래스입니다.
 * 박스 체킹용 메서드를 전역 메소드로 만들어 편리하게 사용
 */
public class MinePostBox
{

    public struct myList1
    {
        public List<myMeshData> list1s;
    }
    public struct myList2
    {
        public List<myInputData> list1s;
    }
    // 구성된 데이터를 다른 쓰레드 측에서 메인 쓰레드로 넘길때 사용하는 구조체
    public struct myMeshData
    {
        public int datanum;
        public Vector3[] myvert;
        public Vector3[] mynormal;
        public Vector2[] myuv;
        public int myvertnum;
        public int[] mytriangle;
    }
    // 재구성해야할 청크 데이터를 메인쓰레드 측에서 다른 쓰레드로 넘길때 사용하는 구조체
    public struct myInputData
    {
        public int chunknum;
        public Vector3 chunkvector;
        public Vector3 playervector;
        public Mine_BlockCreate.block_Dics mydic;
    }

    //싱글턴 인스턴스
    private static MinePostBox instance;

    public static object minesavelock;

    // 쓰레드의 갯수를 나타내는 변수
    public static int mytreadnum;

    //청크의 가로 세로 높이 길이용 변수
    public static int chunkwidth;

    // 최초 로딩 체크용 변수
    public static int loading;

    // 리스트1 출력용 : 입력요청 들어온 것을 처리해 출력 큐에 쌓음
    public List<myList1> list1 = new List<myList1>();

    // 리스트1 출력용 : 입력요청 들어온 것을 처리해 출력 큐에 쌓음
    public List<myList2> list2 = new List<myList2>();

    // 리스트1 출력용 : 입력요청 들어온 것을 처리해 출력 큐에 쌓음
    public List<myList1> list1water = new List<myList1>();

    // 리스트1 출력용 : 입력요청 들어온 것을 처리해 출력 큐에 쌓음
    public List<myList2> list2water = new List<myList2>();

    public static int[] listnum; // 큐에 담긴 갯수
    public static int[] listnum2; // 큐에 담긴 갯수

    public static int[] listnumWater; // 큐에 담긴 갯수
    public static int[] listnum2Water; // 큐에 담긴 갯수

    public static float[] noise_Value;

    public static Mine_BlockCreate.block_Dics mysavefile;
    //물을 위한 세이브파일 변수
    public static Mine_BlockCreate.block_Dics mysavefile_water;
    public static int[] myBlcok_Value = new int[10];
    myMeshData mydata = new myMeshData();
    myInputData mydata2 = new myInputData();

    public MinePostBox()
    {
        mytreadnum = 7;
        mysavefile = new Mine_BlockCreate.block_Dics();
        set_myset();
    }
    public void set_myset()
    {
        for (int i = 0; i < mytreadnum; ++i)
        {
            list1.Add(new myList1 {  list1s = new List<myMeshData>() });
            list2.Add(new myList2 { list1s = new List<myInputData>() });

            list1water.Add(new myList1 { list1s = new List<myMeshData>() });
            list2water.Add(new myList2 { list1s = new List<myInputData>() });
        }
        listnum = new int[mytreadnum];
        listnum2 = new int[mytreadnum];
        listnumWater = new int[mytreadnum];
        listnum2Water = new int[mytreadnum];

    }
    public static void SaveData(float x, float y, float z , int myAtribute)
    {
        Mine_BlockCreate.ThreeStringKey keys;
        keys.x = (int)x;
        keys.y = (int)y;
        keys.z = (int)z;
        if (mysavefile != null)
        {
            //세이브 파일 존재시
            if (mysavefile.block_diction.ContainsKey(keys)) {
                //Debug.Log("save");
                mysavefile.block_diction[keys].atribute = myAtribute;
            }
            //없을시 새로추가
            else
            {
                //Debug.Log("add");
                mysavefile.block_diction.Add(keys, new Mine_BlockCreate.block_Dic { atribute = myAtribute });
            }
        }

    }
    public static int SaveData_rturn(float x, float y, float z)
    {
        Mine_BlockCreate.ThreeStringKey keys;
        int answer = -1;
        keys.x = (int)x;
        keys.y = (int)y;
        keys.z = (int)z;
        if (mysavefile != null)
        {
            if (mysavefile.block_diction.ContainsKey(keys))
            {
                
                answer = mysavefile.block_diction[keys].atribute;
            }
        }

        return answer;
    }

    //싱글턴 인스턴스 반환요청 : 생성안되있을시 생성하고 생성되있을시 해당 인스턴스 반환
    public static MinePostBox GetInstance
    {
        get
        {
            if (instance == null)
                instance = new MinePostBox();


            return instance;
        }
    }

    //리스트에 데이타 삽입요청

    public void PushData(myMeshData data , int pushnum)
    {
        bool check = false;
        if (check == false)
        {
            listnum[pushnum] += 1;
            list1[pushnum].list1s.Add(data);
        }

    }

    public void PushData2(myInputData data, int pushnum)
    {

        bool check = false;

        // 이미 데이터가 존재할시 마지막 입력된것으로 교체.
       for (int i = 0; i < list2[pushnum].list1s.Count; ++i) 
       {
           if (list2[pushnum].list1s[i].chunknum == data.chunknum) 
           {
               list2[pushnum].list1s[i] = data;
               check = true;
           }
       }
        if (check == false)
        {
            listnum2[pushnum] += 1;
            list2[pushnum].list1s.Add(data);
        }
    }
 
    

    //리스트에있는 데이타 꺼내서 반환요청
    public myMeshData GetData(int getnum)
    {
        //데이타가 1개라도 있을 경우 꺼내서 반환
        if (list1[getnum].list1s.Count > 0)
        {
            listnum[getnum] -= 1;
            myMeshData mygetdata = list1[getnum].list1s.First();
            list1[getnum].list1s.Remove(list1[getnum].list1s.First());
            return mygetdata;
        }
        else
        {
            mydata.datanum = -1;
            return mydata;    //없으면 빈값을 반환
        }
    }

    public myInputData GetData2(int getnum)
    {
        //데이타가 1개라도 있을 경우 꺼내서 반환
        if (list2[getnum].list1s.Count > 0)
        {
            listnum2[getnum] -= 1;
            myInputData mygetdata = list2[getnum].list1s.First();
            list2[getnum].list1s.Remove(list2[getnum].list1s.First());
            return mygetdata;
        }
        else
        {
            mydata2.chunknum = -1;
            return mydata2;    //없으면 빈값을 반환
        }
    }

    public void PushDataWater(myMeshData data, int pushnum)
    {
        bool check = false;
        if (check == false)
        {
            listnumWater[pushnum] += 1;
            list1water[pushnum].list1s.Add(data);
        }

    }

    public void PushData2Water(myInputData data, int pushnum)
    {

        bool check = false;

        // 이미 데이터가 존재할시 마지막 입력된것으로 교체.
        for (int i = 0; i < list2water[pushnum].list1s.Count; ++i)
        {
            if (list2water[pushnum].list1s[i].chunknum == data.chunknum)
            {
                list2water[pushnum].list1s[i] = data;
                check = true;
            }
        }
        if (check == false)
        {
            listnum2Water[pushnum] += 1;
            list2water[pushnum].list1s.Add(data);
        }
    }



    //리스트에있는 데이타 꺼내서 반환요청
    public myMeshData GetDataWater(int getnum)
    {
        //데이타가 1개라도 있을 경우 꺼내서 반환
        if (list1water[getnum].list1s.Count > 0)
        {
            listnumWater[getnum] -= 1;
            myMeshData mygetdata = list1water[getnum].list1s.First();
            list1water[getnum].list1s.Remove(list1water[getnum].list1s.First());
            return mygetdata;
        }
        else
        {
            mydata.datanum = -1;
            return mydata;    //없으면 빈값을 반환
        }
    }

    public myInputData GetData2Water(int getnum)
    {
        //데이타가 1개라도 있을 경우 꺼내서 반환
        if (list2water[getnum].list1s.Count > 0)
        {
            listnum2Water[getnum] -= 1;
            myInputData mygetdata = list2water[getnum].list1s.First();
            list2water[getnum].list1s.Remove(list2water[getnum].list1s.First());
            return mygetdata;
        }
        else
        {
            mydata2.chunknum = -1;
            return mydata2;    //없으면 빈값을 반환
        }
    }

    //해당 지점에 나무가 있는지 그렇지 않은지를 노이즈와 변수들로 체크하는 전역 메소드
    public static int TreeExistreturnF(Vector3 chunkvector, int i2, int i3, int i4 )
    {
        int check = 0;


        int prs = (int)(Mathf.PerlinNoise((chunkvector.x + i2) * noise_Value[1], (chunkvector.z + i4) * noise_Value[1]) * 10000);

        //지표면 곡선용 y값 노이즈 이 값으로 지표면을 그린다. 이것을 기준으로 지표면을 정한다.

        // 지표기준1
        int height = 200;
        //지표기준2
        int height2 = 170;

        //지표기준3 지표기준1 위에있는 자잘한 섬들의 y값 한계지점
        int height3 = 240;

        //지표기준4
        int np = Mathf.RoundToInt(prs * 0.0001f * noise_Value[0]) + 185;

        //섬만드는 노이즈
        int makeIslandNoise = Perlin3D((chunkvector.x + i2) * noise_Value[10], (chunkvector.y + i3) * noise_Value[10], (chunkvector.z + i4) * noise_Value[10]);

        //지표면 위에있는 지형을 그리기 위한 노이즈 변수
        int noiseValue2 = Perlin3D((chunkvector.x + i2) * noise_Value[13], (chunkvector.y + i3) * noise_Value[13], (chunkvector.z + i4) * noise_Value[13]);

        int noiseValue3 = Perlin3D((chunkvector.x + i2) * noise_Value[15], (chunkvector.y + i3) * noise_Value[15], (chunkvector.z + i4) * noise_Value[15]);

        //지표기준2 보다 클때
        if (chunkvector.y + i3 > height2)
        {
            // 지표기준2 부터 지표기준1까지 각각 바닥과 그 사이에있는 블록 다 채워줌
            if (chunkvector.y + i3 < height)
            {
                check = 5;

                //지표하한계와 지표상한계 사이에 빈공간 물이 있는공간을 만든다.
                if (makeIslandNoise > noise_Value[30])
                {
                    check = 1;
                }
                else
                {
                    //check = 0;
                }
            }
            // 지표층보다 크고 지표상한계보다 작을때 노이즈 2번과 3번을 적용해 불규칙한 산(언덕)을 그린다.
            //&& chunkvector.y + i3 < np
            if (chunkvector.y + i3 >= height && chunkvector.y + i3 < height3)
            {
                if (makeIslandNoise > noise_Value[30])
                {
                    // 굴곡진 지표면표현
                    if (chunkvector.y + i3 < np)
                    {
                        check = 1;
                    }

                    if (noise_Value[21] > noiseValue2 && noise_Value[22] > noiseValue3)
                    {
                        check = 1;
                    }
                }

            }
        }

            return check;
    }
    public static int TreeExistreturn2F(Vector3 chunkvector, int i2, int i3, int i4 )
    {
        int check = 0;

        // 나무노이즈
        int noiseValue4 = Perlin3D((chunkvector.x + i2) * noise_Value[16], (chunkvector.y + i3) * noise_Value[16], (chunkvector.z + i4) * noise_Value[16]);

        //맨 아래가 땅이고 그 위 y가 5개 비어있을시 나무 노이즈 만족할시 4의 간격으로 나무생성
        if (TreeExistreturnF(chunkvector, i2, i3, i4) == 1
         && TreeExistreturnF(chunkvector, i2, i3 + 1, i4) == 0
         && TreeExistreturnF(chunkvector, i2, i3 + 2, i4) == 0
         && TreeExistreturnF(chunkvector, i2, i3 + 3, i4) == 0
         && TreeExistreturnF(chunkvector, i2, i3 + 4, i4) == 0
         && TreeExistreturnF(chunkvector, i2, i3 + 5, i4) == 0
         && noise_Value[23] > noiseValue4
           && (chunkvector.x + i2) % 4 == 1 && (chunkvector.z + i4) % 4 == 1
         )
        {
            check = 1;
        }
        return check;
    }
    //해당 지점에 지형 블록이 비어있는지 그렇지 않은지를 노이즈와 변수들로 체크하는 전역 메소드
    public static int OnOffBlockreturnF(Vector3 chunkvector, int i2, int i3, int i4)
    {

        int prs = (int)(Mathf.PerlinNoise((chunkvector.x + i2) * noise_Value[1], (chunkvector.z + i4) * noise_Value[1]) * 10000);

        //지표면 곡선용 y값 노이즈 이 값으로 지표면을 그린다. 이것을 기준으로 지표면을 정한다.

        // 지표기준1
        int height = 200;
        //지표기준2
        int height2 =170;

        //지표기준3 지표기준1 위에있는 자잘한 섬들의 y값 한계지점
        int height3 = 240;

        //지표기준4
        int np = Mathf.RoundToInt(prs * 0.0001f * noise_Value[0]) + 185;

        int np2 = Perlin3D((chunkvector.x + i2) * noise_Value[10], (chunkvector.y + i3) * noise_Value[10], (chunkvector.z + i4) * noise_Value[10]);

        //섬만드는 노이즈
        int makeIslandNoise = Perlin3D((chunkvector.x + i2) * noise_Value[10], (chunkvector.y + i3) * noise_Value[10], (chunkvector.z + i4) * noise_Value[10]);

        //지표면 아래 빈공간을 표현하기 위한 노이즈 변수
        int noiseValue = Perlin3D((chunkvector.x + i2) * noise_Value[11], (chunkvector.y + i3) * noise_Value[17], (chunkvector.z + i4) * noise_Value[11]); 

        //지표면 위에있는 지형을 그리기 위한 노이즈 변수
        int noiseValue2 = Perlin3D((chunkvector.x + i2) * noise_Value[13], (chunkvector.y + i3) * noise_Value[13], (chunkvector.z + i4) * noise_Value[13]);

        int noiseValue3 = Perlin3D((chunkvector.x + i2) * noise_Value[15], (chunkvector.y + i3) * noise_Value[15], (chunkvector.z + i4) * noise_Value[15]);

        int check = 0;

      

        //지표기준2 보다 아래
        if (chunkvector.y + i3 <= height2)
        {
            if (noise_Value[20] < noiseValue)
            {
                check = 1;
                if (noise_Value[24] > Perlin3D((chunkvector.x + i2) * noise_Value[31], (chunkvector.y + i3) * noise_Value[31], (chunkvector.z + i4) * noise_Value[31])) // 석탄
                {
                    check = 11;
                }
                if (noise_Value[25] > Perlin3D((chunkvector.x + i2) * noise_Value[32], (chunkvector.y + i3) * noise_Value[32], (chunkvector.z + i4) * noise_Value[32]) && chunkvector.y + i3 <= 70) // 철
                {
                    check = 12;
                }
                if (noise_Value[26] > Perlin3D((chunkvector.x + i2) * noise_Value[33], (chunkvector.y + i3) * noise_Value[33], (chunkvector.z + i4) * noise_Value[33]) && chunkvector.y + i3 <= 50) // 코발트
                {
                    check = 13;
                }
                if (noise_Value[27] > Perlin3D((chunkvector.x + i2) * noise_Value[34], (chunkvector.y + i3) * noise_Value[34], (chunkvector.z + i4) * noise_Value[34]) && chunkvector.y + i3 <= 40) // 코발트블루
                {
                    check = 14;
                }
                if (noise_Value[28] > Perlin3D((chunkvector.x + i2) * noise_Value[35], (chunkvector.y + i3) * noise_Value[35], (chunkvector.z + i4) * noise_Value[35]) && chunkvector.y + i3 <= 50) // 금
                {
                    check = 15;
                }
                if (noise_Value[29] > Perlin3D((chunkvector.x + i2) * noise_Value[36], (chunkvector.y + i3) * noise_Value[36], (chunkvector.z + i4) * noise_Value[36]) && chunkvector.y + i3 <= 40) // 다이아
                {
                    check = 16;
                }

            }
            else if (noise_Value[20] > noiseValue)
            {
                check = 0;
            }



        }
        //지표기준2 보다 클때
        if (chunkvector.y + i3 > height2)
        {
            // 지표기준2 부터 지표기준1까지 각각 바닥과 그 사이에있는 블록 다 채워줌
            if (chunkvector.y + i3 < height) 
            {
                check = 5;

                //지표기준1 과 지표기준2 사이에 빈공간 물이 있는공간을 만든다.
                if (makeIslandNoise > noise_Value[30])
                {
                    check = 1;

                    //지표기준 1과 2 사이에 빈 공간을 만들어줌. 인접면도 판단하여 물이 침투하지 못하도록 만듬.
                    if (noise_Value[20] > noiseValue
                        && Perlin3D((chunkvector.x + i2 +1) * noise_Value[10], (chunkvector.y + i3) * noise_Value[10], (chunkvector.z + i4) * noise_Value[10]) > noise_Value[30]
                         && Perlin3D((chunkvector.x + i2 - 1) * noise_Value[10], (chunkvector.y + i3) * noise_Value[10], (chunkvector.z + i4) * noise_Value[10]) > noise_Value[30]
                           && Perlin3D((chunkvector.x + i2) * noise_Value[10], (chunkvector.y + i3+1) * noise_Value[10], (chunkvector.z + i4) * noise_Value[10]) > noise_Value[30]
                           && Perlin3D((chunkvector.x + i2) * noise_Value[10], (chunkvector.y + i3 - 1) * noise_Value[10], (chunkvector.z + i4) * noise_Value[10]) > noise_Value[30]
                           && Perlin3D((chunkvector.x + i2) * noise_Value[10], (chunkvector.y + i3 ) * noise_Value[10], (chunkvector.z + i4+1) * noise_Value[10]) > noise_Value[30]
                           && Perlin3D((chunkvector.x + i2) * noise_Value[10], (chunkvector.y + i3) * noise_Value[10], (chunkvector.z + i4 - 1) * noise_Value[10]) > noise_Value[30]
                        )
                    {
                        check = 0;
                    }
                }
                else
                {
                    //check = 0;
                }
            }
            // 지표기준2 보다크고 지표기준3 보다 작을때 노이즈 2번과 3번을 적용해 불규칙한 산(언덕)을 그린다.
            //&& chunkvector.y + i3 < np
            if (chunkvector.y + i3 >= height && chunkvector.y + i3 < height3)
            {
                    if (makeIslandNoise > noise_Value[30])
                    {
                    // 굴곡진 지표면표현
                    if (chunkvector.y + i3 < np)
                    {
                        check = 1;
                    }

                    if (noise_Value[21] > noiseValue2 && noise_Value[22] > noiseValue3)
                        {
                            check = 1;
                        }
                    }
                
            }

            //나무 몸통과 나무 머리(잎사귀) 를 그리는 부분. 해당지점기준으로 Y축 5개 비어있고
            // 노이즈에 4를 나누어 나무 생성의 최소간격을 정해준다. 잎사귀 최소범위 안에서 나무끼리 붙어있으면 안되기 때문
            if (TreeExistreturn2F(chunkvector, i2, i3, i4) != 0 )
            {
                check = 31;
            }
            if (TreeExistreturn2F(chunkvector, i2, i3 - 1, i4) != 0)
            {
                check = 31;
            }
            if (TreeExistreturn2F(chunkvector, i2, i3 - 2, i4) != 0)
            {
                check = 31;
            }

            if (TreeExistreturn2F(chunkvector, i2, i3 - 3, i4) != 0 )
            {
                check = 34;
            }

            if (TreeExistreturn2F(chunkvector, i2, i3 - 4, i4) != 0)
            {
                check = 34;
            }

            if (TreeExistreturn2F(chunkvector, i2 -1, i3 - 3, i4) != 0)
            {
                check = 34;
            }
            if (TreeExistreturn2F(chunkvector, i2 + 1, i3 - 3, i4) != 0)
            {
                check = 34;
            }
            if (TreeExistreturn2F(chunkvector, i2 , i3 - 3, i4 -1) != 0)
            {
                check = 34;
            }
            if (TreeExistreturn2F(chunkvector, i2, i3 - 3, i4 + 1) != 0)
            {
                check = 34;
            }

        }

        return check;
    }

    /*해당 청크에 대한 물 블록 계산*/
    public static void Water_Update(float chunkx, float chunky, float chunkz, int x, int y, int z)
    {
        for (int i = 0; i < 10; ++i)
        {
            myBlcok_Value[i] = -1;
        }

        myBlcok_Value[0] = MinePostBox.OnOffBlockreturnF(new Vector3(chunkx, chunky, chunkz), x, y, z);
        if (MinePostBox.SaveData_rturn(chunkx + x, chunky + y, chunkz + z) != 0) { myBlcok_Value[0] = MinePostBox.SaveData_rturn(chunkx + x, chunky + y, chunkz + z); }
        //해당 블록이 물 블록이면
            //Debug.Log(3);
            //인접 블록들의 데이터 받아온다.
            myBlcok_Value[1] = MinePostBox.OnOffBlockreturnF(new Vector3(chunkx, chunky, chunkz), x + 1, y, z);
            if (MinePostBox.SaveData_rturn(chunkx + x + 1, chunky + y, chunkz + z) != -1) { myBlcok_Value[1] = MinePostBox.SaveData_rturn(chunkx + x + 1, chunky + y, chunkz + z); }
            myBlcok_Value[2] = MinePostBox.OnOffBlockreturnF(new Vector3(chunkx, chunky, chunkz), x - 1, y, z);
            if (MinePostBox.SaveData_rturn(chunkx + x - 1, chunky + y, chunkz + z) != -1) { myBlcok_Value[2] = MinePostBox.SaveData_rturn(chunkx + x - 1, chunky + y, chunkz + z); }

            myBlcok_Value[3] = MinePostBox.OnOffBlockreturnF(new Vector3(chunkx, chunky, chunkz), x, y, z + 1);
            if (MinePostBox.SaveData_rturn(chunkx + x, chunky + y, chunkz + z + 1) != -1) { myBlcok_Value[3] = MinePostBox.SaveData_rturn(chunkx + x, chunky + y, chunkz + z + 1); }
            myBlcok_Value[4] = MinePostBox.OnOffBlockreturnF(new Vector3(chunkx, chunky, chunkz), x, y, z - 1);
            if (MinePostBox.SaveData_rturn(chunkx + x, chunky + y, chunkz + z - 1) != -1) { myBlcok_Value[4] = MinePostBox.SaveData_rturn(chunkx + x, chunky + y, chunkz + z - 1); }

            myBlcok_Value[5] = MinePostBox.OnOffBlockreturnF(new Vector3(chunkx, chunky, chunkz), x, y - 1, z);
            if (MinePostBox.SaveData_rturn(chunkx + x, chunky + y -1, chunkz + z ) != -1) { myBlcok_Value[5] = MinePostBox.SaveData_rturn(chunkx + x, chunky + y - 1, chunkz + z); }

            lock (MinePostBox.minesavelock)
            {
                // 먼저 y축으로 비어있는지 확인후 비어있으면 물로 바꿔줌
                if (myBlcok_Value[5] == 0)
                {
                   SaveData(chunkx + x, chunky + y - 1, chunkz + z, 5);
                }
                // 인접 블록이 비어있으면 물 블록으로 세이브파일 변경해준다.
                else if (myBlcok_Value[5] != 0)
                {
                   // Debug.Log(5);
                    if (myBlcok_Value[1] == 0)
                    {
                    //Debug.Log((chunkx + x + 1).ToString() +" , " + (chunky + y).ToString() + " , " + (chunkz + z).ToString()  );
                    SaveData(chunkx + x + 1, chunky + y, chunkz + z, 5);
                    }
                    if (myBlcok_Value[2] == 0)
                    {
                    //Debug.Log((chunkx + x - 1).ToString() + " , " + (chunky + y).ToString() + " , " + (chunkz + z).ToString());
                    SaveData(chunkx + x - 1, chunky + y, chunkz + z, 5);
                    }
                    if (myBlcok_Value[3] == 0)
                    {
                    //Debug.Log((chunkx + x ).ToString() + " , " + (chunky + y).ToString() + " , " + (chunkz + z +1).ToString());
                    SaveData(chunkx + x, chunky + y, chunkz + z + 1, 5);
                    }
                    if (myBlcok_Value[4] == 0)
                    {
                    //Debug.Log((chunkx + x ).ToString() + " , " + (chunky + y).ToString() + " , " + (chunkz + z -1).ToString());
                    SaveData(chunkx + x, chunky + y, chunkz + z - 1, 5);
                    }

                }
            }

        


    }
    // 노이즈 함수 : X Y Z 를 받아 2D 펄린을 적용후 각각을 곱해 3으로 나누면 3차원 노이즈가 생성된다.
    public static int Perlin3D(float x, float y, float z) 
    {
        int ab = (int)(Mathf.PerlinNoise(x, y) * 10000);
        int bc = (int)(Mathf.PerlinNoise(y, z) * 10000);
        int ac = (int)(Mathf.PerlinNoise(x, z) * 10000);

        int ba = (int)(Mathf.PerlinNoise(y, x) * 10000);
        int cb = (int)(Mathf.PerlinNoise(z, y) * 10000);
        int ca = (int)(Mathf.PerlinNoise(z, x) * 10000);



        int abc = (int)((ab + bc + ac) * 0.3333f);
        return abc;
    }
}