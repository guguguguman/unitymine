using System;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using System.Threading.Tasks;

/*  청크 데이터를 구성하기 위한 쓰레드를 사용하는 클래스입니다.
 * 
 */
public class Mine_Thread

{
    //파일위치
    public string file_path;

    //돌릴 워크 쓰레드들
    private Thread[] myThread;
    private Thread myThread2;

    public MinePostBox mypost;

    //동기화용 락 오브젝트
    public object[] lockobj;
    public object mysavelock;

    //쓰레드별로 딕셔너리 접근위해 키 배열 선언
    private Mine_BlockCreate.ThreeStringKey[] mykey2;
    //메인 쓰레드에 넘겨줄 메쉬 데이터
    private MinePostBox.myMeshData mymeshdat;
    //읽어온 세이브 파일 구조체
    private Mine_BlockCreate.block_Dics savedblocks = null;

    //메인 쓰레드에 넘겨줄 메쉬 데이터를 구성하는 배열들 , 각 쓰레드별로 쓰기 위해 이중 배열로 선언
    private int[][] l_triangles;
    private Vector3[][] l_vertices;
    private Vector3[][] l_normales;
    private Vector2[][] l_uvs;

    /* 인스턴스시 실행 함수 : mine_blocreate 에서 start 문에서 호출해줍니다.
     * 
     */
    public Mine_Thread(object[] obj, object mysavelocks)
    {
        try
        {
            mysavelock = mysavelocks;

            file_path = Application.dataPath;

            //키나 락 오브젝트 메쉬 데이터용 어레이들을 '쓰레드 수'에 동일하게 할당해줍니다.
            lockobj = new object[MinePostBox.mytreadnum];
            l_triangles = new int[MinePostBox.mytreadnum][];
            l_vertices = new Vector3[MinePostBox.mytreadnum][];
            l_normales = new Vector3[MinePostBox.mytreadnum][];
            l_uvs = new Vector2[MinePostBox.mytreadnum][];
            mykey2 = new Mine_BlockCreate.ThreeStringKey[MinePostBox.mytreadnum];

            //메쉬용 더블 어레이는 해당 길이만큼 다시 포문으로 청크길이 세제곱 * 버택스 노말 uv는 24 or 트라이앵글은 36을 곱해서 할당해줍니다.
            for (int i = 0; i < MinePostBox.mytreadnum; ++i)
            {
                l_vertices[i] = new Vector3[24 * MinePostBox.chunkwidth * MinePostBox.chunkwidth * MinePostBox.chunkwidth];
                l_normales[i] = new Vector3[24 * MinePostBox.chunkwidth * MinePostBox.chunkwidth * MinePostBox.chunkwidth];
                l_uvs[i] = new Vector2[24 * MinePostBox.chunkwidth * MinePostBox.chunkwidth * MinePostBox.chunkwidth];
                l_triangles[i] = new int[36 * MinePostBox.chunkwidth * MinePostBox.chunkwidth * MinePostBox.chunkwidth];
               
                for (int i2 = 0; i2 < 24 * MinePostBox.chunkwidth * MinePostBox.chunkwidth * MinePostBox.chunkwidth; ++i2)
                {
                    l_vertices[i][i2] = new Vector3(0, 0, 0);
                    l_normales[i][i2] = new Vector3(0, 0, 0);
                    l_uvs[i][i2] = new Vector2(0, 0);
                }
                for (int i3 = 0; i3 < 36 * MinePostBox.chunkwidth * MinePostBox.chunkwidth * MinePostBox.chunkwidth; ++i3)
                {
                    l_triangles[i][i3] = 0;
                }
            }
            //락용 더블어레이를 포문돌면서 할당합니다.
            for (int i = 0; i < obj.Length; ++i)
            {
                lockobj[i] = obj[i];
            }

            //각 쓰레드를 제어하기 위해 싱글톤에 있는 쓰레드 수만큼 할당합니다.
            myThread = new Thread[MinePostBox.mytreadnum];
            for (int i = 0; i < MinePostBox.mytreadnum; ++i)
            {
                // 더블 어레이이므로 포문 돌면서 할당함과 동시에 쓰레드 실행해줍니다.
                myThread[i] = new Thread(new ParameterizedThreadStart(ListenForData));
                myThread[i].Start(i); //i
                

            }
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }

    /*워크 쓰레드  : 청크 데이터를 재구성하는 역할을 함.
     * 
     */
    void ListenForData(object mynum)
    {
        int listnum = (int)mynum;

        while (true)
        {
            while (MinePostBox.listnum2[listnum] > 0) // 입력 큐에 들어온 값이 있을시
            {
                MinePostBox.myInputData myinput;
                lock (lockobj[listnum])
                {
                    myinput = MinePostBox.GetInstance.GetData2(listnum);

                }
                Create_BoxF(myinput ,listnum );
            }

            while (MinePostBox.listnum2Water[listnum] > 0) // 입력 큐에 들어온 값이 있을시
            {
                MinePostBox.myInputData myinput;
                lock (lockobj[listnum])
                {
                    myinput = MinePostBox.GetInstance.GetData2Water(listnum);

                }
                Create_BoxWaterF(myinput, listnum);
            }

            Thread.Sleep(50);

        }
    }

    /*
     * 청크 재구성 함수 : 노이즈 함수를 이용해 지형을 그리고 합친 메쉬데이터를 큐에 쌓는다
    */
    public void Create_BoxF(MinePostBox.myInputData myinput, int thrnum)
    {
        Mine_BlockCreate.block_Dics mysavefile = MinePostBox.mysavefile;

        //버택스 노말을 설정할떄 필요한 VECTOR
        Vector3 p0;
        Vector3 p1;
        Vector3 p2;
        Vector3 p3;
        Vector3 p4;
        Vector3 p5;
        Vector3 p6;
        Vector3 p7;

        //블록 버택스를 생성할때 길이
        float length = 1f;
        float width = 1f;
        float height = 1f;

        // 최종 존재하는 블록의 수를 계산하기 위한 변수
        int i = 0;

        // 기준 블록을 기준으로 위아래앞뒤옆오른쪽 6개 면에 대한 BLOCK을 검색 모두 막힌 경우 '그릴필요없다' 판단.
        int[] block1 = new int[6];

        // 청크의 한 면의 길이 이것을 기준으로 X Y Z 에 대한 포문 돌림
        int chunkwidth = MinePostBox.chunkwidth;

        // 버택스와 UV 어레이 재할당
        l_vertices[thrnum] = new Vector3[24 * chunkwidth *chunkwidth * chunkwidth];
        l_uvs[thrnum] = new Vector2[24 *chunkwidth * chunkwidth * chunkwidth];

        for (int i2 = -chunkwidth / 2 ; i2 <chunkwidth /2 +1; ++i2)
        {
            for (int i3 = -chunkwidth / 2; i3 < chunkwidth / 2 + 1; ++i3)
            {
                for (int i4 = -chunkwidth / 2; i4 < chunkwidth / 2 + 1; ++i4)
                {
                    int myblocknum = MinePostBox.OnOffBlockreturnF(myinput.chunkvector, i2 , i3, i4);
                    block1[0] =  MinePostBox.OnOffBlockreturnF(myinput.chunkvector, i2 - 1, i3, i4);
                    block1[1] =  MinePostBox.OnOffBlockreturnF(myinput.chunkvector, i2 + 1, i3, i4);
                    block1[2] =  MinePostBox.OnOffBlockreturnF(myinput.chunkvector, i2, i3, i4 - 1);
                    block1[3] =  MinePostBox.OnOffBlockreturnF(myinput.chunkvector, i2, i3, i4 + 1);
                    block1[4] =  MinePostBox.OnOffBlockreturnF(myinput.chunkvector, i2, i3 - 1, i4);
                    block1[5] = MinePostBox.OnOffBlockreturnF(myinput.chunkvector, i2, i3 + 1, i4);
                    //세이브 블록이 존재할시
                    lock (mysavelock)
                    {
                        if (mysavefile != null)
                        {
                            mykey2[thrnum].x = (int)(myinput.chunkvector.x) + i2;
                            mykey2[thrnum].y = (int)(myinput.chunkvector.y) + i3;
                            mykey2[thrnum].z = (int)(myinput.chunkvector.z) + i4;
                            if (mysavefile.block_diction.ContainsKey(mykey2[thrnum]) == true)
                            {
                                myblocknum = mysavefile.block_diction[mykey2[thrnum]].atribute;
                            }

                            mykey2[thrnum].x = (int)(myinput.chunkvector.x) + i2 - 1;
                            mykey2[thrnum].y = (int)(myinput.chunkvector.y) + i3;
                            mykey2[thrnum].z = (int)(myinput.chunkvector.z) + i4;
                            if (mysavefile.block_diction.ContainsKey(mykey2[thrnum]) == true)
                            {
                                block1[0] = mysavefile.block_diction[mykey2[thrnum]].atribute;
                            }
                            mykey2[thrnum].x = (int)(myinput.chunkvector.x) + i2 + 1;
                            mykey2[thrnum].y = (int)(myinput.chunkvector.y) + i3;
                            mykey2[thrnum].z = (int)(myinput.chunkvector.z) + i4;
                            if (mysavefile.block_diction.ContainsKey(mykey2[thrnum]) == true)
                            {
                                block1[1] = mysavefile.block_diction[mykey2[thrnum]].atribute;
                            }

                            mykey2[thrnum].x = (int)(myinput.chunkvector.x) + i2;
                            mykey2[thrnum].y = (int)(myinput.chunkvector.y) + i3 - 1;
                            mykey2[thrnum].z = (int)(myinput.chunkvector.z) + i4;
                            if (mysavefile.block_diction.ContainsKey(mykey2[thrnum]) == true)
                            {
                                block1[2] = mysavefile.block_diction[mykey2[thrnum]].atribute;
                            }
                            mykey2[thrnum].x = (int)(myinput.chunkvector.x) + i2;
                            mykey2[thrnum].y = (int)(myinput.chunkvector.y) + i3 + 1;
                            mykey2[thrnum].z = (int)(myinput.chunkvector.z) + i4;
                            if (mysavefile.block_diction.ContainsKey(mykey2[thrnum]) == true)
                            {
                                block1[3] = mysavefile.block_diction[mykey2[thrnum]].atribute;
                            }
                            mykey2[thrnum].x = (int)(myinput.chunkvector.x) + i2;
                            mykey2[thrnum].y = (int)(myinput.chunkvector.y) + i3;
                            mykey2[thrnum].z = (int)(myinput.chunkvector.z) + i4 - 1;
                            if (mysavefile.block_diction.ContainsKey(mykey2[thrnum]) == true)
                            {
                                block1[4] = mysavefile.block_diction[mykey2[thrnum]].atribute;
                            }
                            mykey2[thrnum].x = (int)(myinput.chunkvector.x) + i2;
                            mykey2[thrnum].y = (int)(myinput.chunkvector.y) + i3;
                            mykey2[thrnum].z = (int)(myinput.chunkvector.z) + i4 + 1;
                            if (mysavefile.block_diction.ContainsKey(mykey2[thrnum]) == true)
                            {
                                block1[5] = mysavefile.block_diction[mykey2[thrnum]].atribute;
                            }
                        }
                    }

                    if (
                           block1[0] == 0
                        || block1[1] == 0
                        || block1[2] == 0
                        || block1[3] == 0
                        || block1[4] == 0
                        || block1[5] == 0
                        //|| myblocknum == 5
                        || block1[0] == 5
                        || block1[1] == 5
                        || block1[2] == 5
                        || block1[3] == 5
                        || block1[4] == 5
                        || block1[5] == 5
                        || myblocknum == 34 // 풀 블록이면 무시하고 그냥 그림

                          )

                    {



                        //만약 Y 축으로 3개까지 비어있으면 위에 없는것으로 간주하고 이 블록을 풀이 있는 블록으로 그린다.
                        int gb1 = MinePostBox.OnOffBlockreturnF(myinput.chunkvector, i2, i3 + 1, i4);
                        int gb2 = MinePostBox.OnOffBlockreturnF(myinput.chunkvector, i2, i3 + 2, i4);
                        int gb3 = MinePostBox.OnOffBlockreturnF(myinput.chunkvector, i2, i3 + 3, i4);

                        // 현재 이 위치 블록에 뭔가가 있는지 있다면 그린다.
                        if (myblocknum > 0)
                        {
                            if (myblocknum == 1 && myinput.chunkvector.y < 170)
                            {
                                myblocknum = 3;
                            }

                            if (myblocknum == 1 && gb1 == 0 && myinput.chunkvector.y > 200) // 흙인데 y축으로 위쪽 블록 3개가 비어있을시 지표라 생각하고 풀그린다.
                            {
                                myblocknum = 2; // 풀블록
                            }
                            if (myblocknum == 5) // 물 블록일시
                            {
                                MinePostBox.Water_Update(myinput.chunkvector.x, myinput.chunkvector.y, myinput.chunkvector.z, i2, i3, i4);

                            }


                            // 버택스 정보를 할당된 백터와 INT 배열에 쌓는다
                            #region l_vertices
                            p0.x = -length * .5f + i2; p0.y = -width * .5f + i3; p0.z = height * .5f + i4;
                            p1.x = length * .5f + i2; p1.y = -width * .5f + i3; p1.z = height * .5f + i4;
                            p2.x = length * .5f + i2; p2.y = -width * .5f + i3; p2.z = -height * .5f + i4;
                            p3.x = -length * .5f + i2; p3.y = -width * .5f + i3; p3.z = -height * .5f + i4;
                            p4.x = -length * .5f + i2; p4.y = width * .5f + i3; p4.z = height * .5f + i4;
                            p5.x = length * .5f + i2; p5.y = width * .5f + i3; p5.z = height * .5f + i4;
                            p6.x = length * .5f + i2; p6.y = width * .5f + i3; p6.z = -height * .5f + i4;
                            p7.x = -length * .5f + i2; p7.y = width * .5f + i3; p7.z = -height * .5f + i4;

                            l_vertices[thrnum][0 + i * 24] = p0; l_vertices[thrnum][1 + i * 24] = p1; l_vertices[thrnum][2 + i * 24] = p2; l_vertices[thrnum][3 + i * 24] = p3;
                            l_vertices[thrnum][4 + i * 24] = p7; l_vertices[thrnum][5 + i * 24] = p4; l_vertices[thrnum][6 + i * 24] = p0; l_vertices[thrnum][7 + i * 24] = p3;
                            l_vertices[thrnum][8 + i * 24] = p4; l_vertices[thrnum][9 + i * 24] = p5; l_vertices[thrnum][10 + i * 24] = p1; l_vertices[thrnum][11 + i * 24] = p0;
                            l_vertices[thrnum][12 + i * 24] = p6; l_vertices[thrnum][13 + i * 24] = p7; l_vertices[thrnum][14 + i * 24] = p3; l_vertices[thrnum][15 + i * 24] = p2;
                            l_vertices[thrnum][16 + i * 24] = p5; l_vertices[thrnum][17 + i * 24] = p6; l_vertices[thrnum][18 + i * 24] = p2; l_vertices[thrnum][19 + i * 24] = p1;
                            l_vertices[thrnum][20 + i * 24] = p7; l_vertices[thrnum][21 + i * 24] = p6; l_vertices[thrnum][22 + i * 24] = p5; l_vertices[thrnum][23 + i * 24] = p4;
                            #endregion

                            #region l_normales
                            Vector3 up = Vector3.up;
                            Vector3 down = Vector3.down;
                            Vector3 front = Vector3.forward;
                            Vector3 back = Vector3.back;
                            Vector3 left = Vector3.left;
                            Vector3 right = Vector3.right;

                            l_normales[thrnum][0 + i * 24] = down; l_normales[thrnum][1 + i * 24] = down; l_normales[thrnum][2 + i * 24] = down; l_normales[thrnum][3 + i * 24] = down;
                            l_normales[thrnum][4 + i * 24] = left; l_normales[thrnum][5 + i * 24] = left; l_normales[thrnum][6 + i * 24] = left; l_normales[thrnum][7 + i * 24] = left;
                            l_normales[thrnum][8 + i * 24] = front; l_normales[thrnum][9 + i * 24] = front; l_normales[thrnum][10 + i * 24] = front; l_normales[thrnum][11 + i * 24] = front;
                            l_normales[thrnum][12 + i * 24] = back; l_normales[thrnum][13 + i * 24] = back; l_normales[thrnum][14 + i * 24] = back; l_normales[thrnum][15 + i * 24] = back;
                            l_normales[thrnum][16 + i * 24] = right; l_normales[thrnum][17 + i * 24] = right; l_normales[thrnum][18 + i * 24] = right; l_normales[thrnum][19 + i * 24] = right;
                            l_normales[thrnum][20 + i * 24] = up; l_normales[thrnum][21 + i * 24] = up; l_normales[thrnum][22 + i * 24] = up; l_normales[thrnum][23 + i * 24] = up;
                            #endregion

                            #region l_uvs
                            UVset(myblocknum, thrnum, i);
                            #endregion

                            #region l_triangles

                            l_triangles[thrnum][0 + i * 36] = 3 + i * 24;
                            l_triangles[thrnum][1 + i * 36] = 1 + i * 24;
                            l_triangles[thrnum][2 + i * 36] = 0 + i * 24;

                            l_triangles[thrnum][3 + i * 36] = 3 + i * 24;
                            l_triangles[thrnum][4 + i * 36] = 2 + i * 24;
                            l_triangles[thrnum][5 + i * 36] = 1 + i * 24;

                            l_triangles[thrnum][6 + i * 36] = 3 + 4 * 1 + i * 24;
                            l_triangles[thrnum][7 + i * 36] = 1 + 4 * 1 + i * 24;
                            l_triangles[thrnum][8 + i * 36] = 0 + 4 * 1 + i * 24;

                            l_triangles[thrnum][9 + i * 36] = 3 + 4 * 1 + i * 24;
                            l_triangles[thrnum][10 + i * 36] = 2 + 4 * 1 + i * 24;
                            l_triangles[thrnum][11 + i * 36] = 1 + 4 * 1 + i * 24;

                            l_triangles[thrnum][12 + i * 36] = 3 + 4 * 2 + i * 24;
                            l_triangles[thrnum][13 + i * 36] = 1 + 4 * 2 + i * 24;
                            l_triangles[thrnum][14 + i * 36] = 0 + 4 * 2 + i * 24;

                            l_triangles[thrnum][15 + i * 36] = 3 + 4 * 2 + i * 24;
                            l_triangles[thrnum][16 + i * 36] = 2 + 4 * 2 + i * 24;
                            l_triangles[thrnum][17 + i * 36] = 1 + 4 * 2 + i * 24;

                            l_triangles[thrnum][18 + i * 36] = 3 + 4 * 3 + i * 24;
                            l_triangles[thrnum][19 + i * 36] = 1 + 4 * 3 + i * 24;
                            l_triangles[thrnum][20 + i * 36] = 0 + 4 * 3 + i * 24;

                            l_triangles[thrnum][21 + i * 36] = 3 + 4 * 3 + i * 24;
                            l_triangles[thrnum][22 + i * 36] = 2 + 4 * 3 + i * 24;
                            l_triangles[thrnum][23 + i * 36] = 1 + 4 * 3 + i * 24;

                            l_triangles[thrnum][24 + i * 36] = 3 + 4 * 4 + i * 24;
                            l_triangles[thrnum][25 + i * 36] = 1 + 4 * 4 + i * 24;
                            l_triangles[thrnum][26 + i * 36] = 0 + 4 * 4 + i * 24;

                            l_triangles[thrnum][27 + i * 36] = 3 + 4 * 4 + i * 24;
                            l_triangles[thrnum][28 + i * 36] = 2 + 4 * 4 + i * 24;
                            l_triangles[thrnum][29 + i * 36] = 1 + 4 * 4 + i * 24;

                            l_triangles[thrnum][30 + i * 36] = 3 + 4 * 5 + i * 24;
                            l_triangles[thrnum][31 + i * 36] = 1 + 4 * 5 + i * 24;
                            l_triangles[thrnum][32 + i * 36] = 0 + 4 * 5 + i * 24;

                            l_triangles[thrnum][33 + i * 36] = 3 + 4 * 5 + i * 24;
                            l_triangles[thrnum][34 + i * 36] = 2 + 4 * 5 + i * 24;
                            l_triangles[thrnum][35 + i * 36] = 1 + 4 * 5 + i * 24;


                            #endregion
                            ++i;
                        }

                    }

                }

            }
        }
        mymeshdat.myvertnum = i;
        mymeshdat.datanum = myinput.chunknum;
        mymeshdat.myvert = l_vertices[thrnum];
        mymeshdat.mynormal = l_normales[thrnum];
        mymeshdat.myuv = l_uvs[thrnum];
        mymeshdat.mytriangle = l_triangles[thrnum];

        // 최종적으로 만든 데이터를 큐에다 푸쉬
        lock (lockobj[thrnum])
        {
            MinePostBox.GetInstance.PushData(mymeshdat, thrnum);
        }

    }


    /*
    * 청크 재구성 함수 :  물 움직임을 위해 매서드 따로구성
   */
    public void Create_BoxWaterF(MinePostBox.myInputData myinput, int thrnum)
    {
        Mine_BlockCreate.block_Dics mysavefile = MinePostBox.mysavefile;

        //버택스 노말을 설정할떄 필요한 VECTOR
        Vector3 p0;
        Vector3 p1;
        Vector3 p2;
        Vector3 p3;
        Vector3 p4;
        Vector3 p5;
        Vector3 p6;
        Vector3 p7;

        //블록 버택스를 생성할때 길이
        float length = 1f;
        float width = 1f;
        float height = 1f;

        // 최종 존재하는 블록의 수를 계산하기 위한 변수
        int i = 0;

        // 기준 블록을 기준으로 위아래앞뒤옆오른쪽 6개 면에 대한 BLOCK을 검색 모두 막힌 경우 '그릴필요없다' 판단.
        int[] block1 = new int[6];

        // 청크의 한 면의 길이 이것을 기준으로 X Y Z 에 대한 포문 돌림
        int chunkwidth = MinePostBox.chunkwidth;

        // 버택스와 UV 어레이 재할당
        l_vertices[thrnum] = new Vector3[24 * chunkwidth * chunkwidth * chunkwidth];
        l_uvs[thrnum] = new Vector2[24 * chunkwidth * chunkwidth * chunkwidth];

        for (int i2 = -chunkwidth / 2; i2 < chunkwidth / 2 + 1; ++i2)
        {
            for (int i3 = -chunkwidth / 2; i3 < chunkwidth / 2 + 1; ++i3)
            {
                for (int i4 = -chunkwidth / 2; i4 < chunkwidth / 2 + 1; ++i4)
                {
                    int myblocknum = MinePostBox.OnOffBlockreturnF(myinput.chunkvector, i2, i3, i4);
                    block1[0] = MinePostBox.OnOffBlockreturnF(myinput.chunkvector, i2 - 1, i3, i4);
                    block1[1] = MinePostBox.OnOffBlockreturnF(myinput.chunkvector, i2 + 1, i3, i4);
                    block1[2] = MinePostBox.OnOffBlockreturnF(myinput.chunkvector, i2, i3, i4 - 1);
                    block1[3] = MinePostBox.OnOffBlockreturnF(myinput.chunkvector, i2, i3, i4 + 1);
                    block1[4] = MinePostBox.OnOffBlockreturnF(myinput.chunkvector, i2, i3 - 1, i4);
                    block1[5] = MinePostBox.OnOffBlockreturnF(myinput.chunkvector, i2, i3 + 1, i4);
                    //세이브 블록이 존재할시
                    lock (mysavelock)
                    {
                        if (mysavefile != null)
                        {
                            mykey2[thrnum].x = (int)(myinput.chunkvector.x) + i2;
                            mykey2[thrnum].y = (int)(myinput.chunkvector.y) + i3;
                            mykey2[thrnum].z = (int)(myinput.chunkvector.z) + i4;
                            if (mysavefile.block_diction.ContainsKey(mykey2[thrnum]) == true)
                            {
                                myblocknum = mysavefile.block_diction[mykey2[thrnum]].atribute;
                            }

                            mykey2[thrnum].x = (int)(myinput.chunkvector.x) + i2 - 1;
                            mykey2[thrnum].y = (int)(myinput.chunkvector.y) + i3;
                            mykey2[thrnum].z = (int)(myinput.chunkvector.z) + i4;
                            if (mysavefile.block_diction.ContainsKey(mykey2[thrnum]) == true)
                            {
                                block1[0] = mysavefile.block_diction[mykey2[thrnum]].atribute;
                            }
                            mykey2[thrnum].x = (int)(myinput.chunkvector.x) + i2 + 1;
                            mykey2[thrnum].y = (int)(myinput.chunkvector.y) + i3;
                            mykey2[thrnum].z = (int)(myinput.chunkvector.z) + i4;
                            if (mysavefile.block_diction.ContainsKey(mykey2[thrnum]) == true)
                            {
                                block1[1] = mysavefile.block_diction[mykey2[thrnum]].atribute;
                            }

                            mykey2[thrnum].x = (int)(myinput.chunkvector.x) + i2;
                            mykey2[thrnum].y = (int)(myinput.chunkvector.y) + i3 - 1;
                            mykey2[thrnum].z = (int)(myinput.chunkvector.z) + i4;
                            if (mysavefile.block_diction.ContainsKey(mykey2[thrnum]) == true)
                            {
                                block1[2] = mysavefile.block_diction[mykey2[thrnum]].atribute;
                            }
                            mykey2[thrnum].x = (int)(myinput.chunkvector.x) + i2;
                            mykey2[thrnum].y = (int)(myinput.chunkvector.y) + i3 + 1;
                            mykey2[thrnum].z = (int)(myinput.chunkvector.z) + i4;
                            if (mysavefile.block_diction.ContainsKey(mykey2[thrnum]) == true)
                            {
                                block1[3] = mysavefile.block_diction[mykey2[thrnum]].atribute;
                            }
                            mykey2[thrnum].x = (int)(myinput.chunkvector.x) + i2;
                            mykey2[thrnum].y = (int)(myinput.chunkvector.y) + i3;
                            mykey2[thrnum].z = (int)(myinput.chunkvector.z) + i4 - 1;
                            if (mysavefile.block_diction.ContainsKey(mykey2[thrnum]) == true)
                            {
                                block1[4] = mysavefile.block_diction[mykey2[thrnum]].atribute;
                            }
                            mykey2[thrnum].x = (int)(myinput.chunkvector.x) + i2;
                            mykey2[thrnum].y = (int)(myinput.chunkvector.y) + i3;
                            mykey2[thrnum].z = (int)(myinput.chunkvector.z) + i4 + 1;
                            if (mysavefile.block_diction.ContainsKey(mykey2[thrnum]) == true)
                            {
                                block1[5] = mysavefile.block_diction[mykey2[thrnum]].atribute;
                            }
                        }
                    }



                    // 주변 6개 면에 대한 검색을 수행한다. 만약 하나라도 뚫려있을 경우 '그려야한다'로 가정한다.
                    // 이를통해 메인 쓰레드에서 메쉬의 그릴 양을 줄인다.
                    if (
                           block1[0] == 0
                        || block1[1] == 0
                        || block1[2] == 0
                        || block1[3] == 0
                        || block1[4] == 0
                        || block1[5] == 0
                        //|| myblocknum == 5
                        ||  block1[0] == 5
                        || block1[1] == 5
                        || block1[2] == 5
                        || block1[3] == 5
                        || block1[4] == 5
                        || block1[5] == 5
                        || myblocknum == 34 // 풀 블록이면 무시하고 그냥 그림

                          )

                    {

                        //만약 Y 축으로 3개까지 비어있으면 위에 없는것으로 간주하고 이 블록을 풀이 있는 블록으로 그린다.
                        int gb1 = MinePostBox.OnOffBlockreturnF(myinput.chunkvector, i2, i3 + 1, i4);
                        int gb2 = MinePostBox.OnOffBlockreturnF(myinput.chunkvector, i2, i3 + 2, i4);
                        int gb3 = MinePostBox.OnOffBlockreturnF(myinput.chunkvector, i2, i3 + 3, i4);

                        // 현재 이 위치 블록에 뭔가가 있는지 있다면 그린다.
                        if (myblocknum > 0)
                        {
                            if (myblocknum == 1 && myinput.chunkvector.y < 170)
                            {
                                myblocknum = 3;
                            }

                            if (myblocknum == 1 && gb1 == 0 && myinput.chunkvector.y > 200) // 흙인데 y축으로 위쪽 블록 3개가 비어있을시 지표라 생각하고 풀그린다.
                            {
                                myblocknum = 2; // 풀블록
                            }
                            if (myblocknum == 5) // 물 블록일시
                            {
                                MinePostBox.Water_Update(myinput.chunkvector.x, myinput.chunkvector.y, myinput.chunkvector.z, i2, i3, i4);

                            }

                            // 버택스 정보를 할당된 백터와 INT 배열에 쌓는다
                            #region l_vertices
                            p0.x = -length * .5f + i2; p0.y = -width * .5f + i3; p0.z = height * .5f + i4;
                            p1.x = length * .5f + i2; p1.y = -width * .5f + i3; p1.z = height * .5f + i4;
                            p2.x = length * .5f + i2; p2.y = -width * .5f + i3; p2.z = -height * .5f + i4;
                            p3.x = -length * .5f + i2; p3.y = -width * .5f + i3; p3.z = -height * .5f + i4;
                            p4.x = -length * .5f + i2; p4.y = width * .5f + i3; p4.z = height * .5f + i4;
                            p5.x = length * .5f + i2; p5.y = width * .5f + i3; p5.z = height * .5f + i4;
                            p6.x = length * .5f + i2; p6.y = width * .5f + i3; p6.z = -height * .5f + i4;
                            p7.x = -length * .5f + i2; p7.y = width * .5f + i3; p7.z = -height * .5f + i4;

                            l_vertices[thrnum][0 + i * 24] = p0; l_vertices[thrnum][1 + i * 24] = p1; l_vertices[thrnum][2 + i * 24] = p2; l_vertices[thrnum][3 + i * 24] = p3;
                            l_vertices[thrnum][4 + i * 24] = p7; l_vertices[thrnum][5 + i * 24] = p4; l_vertices[thrnum][6 + i * 24] = p0; l_vertices[thrnum][7 + i * 24] = p3;
                            l_vertices[thrnum][8 + i * 24] = p4; l_vertices[thrnum][9 + i * 24] = p5; l_vertices[thrnum][10 + i * 24] = p1; l_vertices[thrnum][11 + i * 24] = p0;
                            l_vertices[thrnum][12 + i * 24] = p6; l_vertices[thrnum][13 + i * 24] = p7; l_vertices[thrnum][14 + i * 24] = p3; l_vertices[thrnum][15 + i * 24] = p2;
                            l_vertices[thrnum][16 + i * 24] = p5; l_vertices[thrnum][17 + i * 24] = p6; l_vertices[thrnum][18 + i * 24] = p2; l_vertices[thrnum][19 + i * 24] = p1;
                            l_vertices[thrnum][20 + i * 24] = p7; l_vertices[thrnum][21 + i * 24] = p6; l_vertices[thrnum][22 + i * 24] = p5; l_vertices[thrnum][23 + i * 24] = p4;
                            #endregion

                            #region l_normales
                            Vector3 up = Vector3.up;
                            Vector3 down = Vector3.down;
                            Vector3 front = Vector3.forward;
                            Vector3 back = Vector3.back;
                            Vector3 left = Vector3.left;
                            Vector3 right = Vector3.right;

                            l_normales[thrnum][0 + i * 24] = down; l_normales[thrnum][1 + i * 24] = down; l_normales[thrnum][2 + i * 24] = down; l_normales[thrnum][3 + i * 24] = down;
                            l_normales[thrnum][4 + i * 24] = left; l_normales[thrnum][5 + i * 24] = left; l_normales[thrnum][6 + i * 24] = left; l_normales[thrnum][7 + i * 24] = left;
                            l_normales[thrnum][8 + i * 24] = front; l_normales[thrnum][9 + i * 24] = front; l_normales[thrnum][10 + i * 24] = front; l_normales[thrnum][11 + i * 24] = front;
                            l_normales[thrnum][12 + i * 24] = back; l_normales[thrnum][13 + i * 24] = back; l_normales[thrnum][14 + i * 24] = back; l_normales[thrnum][15 + i * 24] = back;
                            l_normales[thrnum][16 + i * 24] = right; l_normales[thrnum][17 + i * 24] = right; l_normales[thrnum][18 + i * 24] = right; l_normales[thrnum][19 + i * 24] = right;
                            l_normales[thrnum][20 + i * 24] = up; l_normales[thrnum][21 + i * 24] = up; l_normales[thrnum][22 + i * 24] = up; l_normales[thrnum][23 + i * 24] = up;
                            #endregion

                            #region l_uvs
                            UVset(myblocknum, thrnum, i);
                            #endregion

                            #region l_triangles

                            l_triangles[thrnum][0 + i * 36] = 3 + i * 24;
                            l_triangles[thrnum][1 + i * 36] = 1 + i * 24;
                            l_triangles[thrnum][2 + i * 36] = 0 + i * 24;

                            l_triangles[thrnum][3 + i * 36] = 3 + i * 24;
                            l_triangles[thrnum][4 + i * 36] = 2 + i * 24;
                            l_triangles[thrnum][5 + i * 36] = 1 + i * 24;

                            l_triangles[thrnum][6 + i * 36] = 3 + 4 * 1 + i * 24;
                            l_triangles[thrnum][7 + i * 36] = 1 + 4 * 1 + i * 24;
                            l_triangles[thrnum][8 + i * 36] = 0 + 4 * 1 + i * 24;

                            l_triangles[thrnum][9 + i * 36] = 3 + 4 * 1 + i * 24;
                            l_triangles[thrnum][10 + i * 36] = 2 + 4 * 1 + i * 24;
                            l_triangles[thrnum][11 + i * 36] = 1 + 4 * 1 + i * 24;

                            l_triangles[thrnum][12 + i * 36] = 3 + 4 * 2 + i * 24;
                            l_triangles[thrnum][13 + i * 36] = 1 + 4 * 2 + i * 24;
                            l_triangles[thrnum][14 + i * 36] = 0 + 4 * 2 + i * 24;

                            l_triangles[thrnum][15 + i * 36] = 3 + 4 * 2 + i * 24;
                            l_triangles[thrnum][16 + i * 36] = 2 + 4 * 2 + i * 24;
                            l_triangles[thrnum][17 + i * 36] = 1 + 4 * 2 + i * 24;

                            l_triangles[thrnum][18 + i * 36] = 3 + 4 * 3 + i * 24;
                            l_triangles[thrnum][19 + i * 36] = 1 + 4 * 3 + i * 24;
                            l_triangles[thrnum][20 + i * 36] = 0 + 4 * 3 + i * 24;

                            l_triangles[thrnum][21 + i * 36] = 3 + 4 * 3 + i * 24;
                            l_triangles[thrnum][22 + i * 36] = 2 + 4 * 3 + i * 24;
                            l_triangles[thrnum][23 + i * 36] = 1 + 4 * 3 + i * 24;

                            l_triangles[thrnum][24 + i * 36] = 3 + 4 * 4 + i * 24;
                            l_triangles[thrnum][25 + i * 36] = 1 + 4 * 4 + i * 24;
                            l_triangles[thrnum][26 + i * 36] = 0 + 4 * 4 + i * 24;

                            l_triangles[thrnum][27 + i * 36] = 3 + 4 * 4 + i * 24;
                            l_triangles[thrnum][28 + i * 36] = 2 + 4 * 4 + i * 24;
                            l_triangles[thrnum][29 + i * 36] = 1 + 4 * 4 + i * 24;

                            l_triangles[thrnum][30 + i * 36] = 3 + 4 * 5 + i * 24;
                            l_triangles[thrnum][31 + i * 36] = 1 + 4 * 5 + i * 24;
                            l_triangles[thrnum][32 + i * 36] = 0 + 4 * 5 + i * 24;

                            l_triangles[thrnum][33 + i * 36] = 3 + 4 * 5 + i * 24;
                            l_triangles[thrnum][34 + i * 36] = 2 + 4 * 5 + i * 24;
                            l_triangles[thrnum][35 + i * 36] = 1 + 4 * 5 + i * 24;


                            #endregion
                            ++i;
                        }


                    }
                }

            }
        }
        mymeshdat.myvertnum = i;
        mymeshdat.datanum = myinput.chunknum;
        mymeshdat.myvert = l_vertices[thrnum];
        mymeshdat.mynormal = l_normales[thrnum];
        mymeshdat.myuv = l_uvs[thrnum];
        mymeshdat.mytriangle = l_triangles[thrnum];

        // 최종적으로 만든 데이터를 큐에다 푸쉬
        lock (lockobj[thrnum])
        {
            MinePostBox.GetInstance.PushDataWater(mymeshdat, thrnum);
        }

    }

    // 블록속성에 따라 uv 조절해 그림을 다르게함

    public void UVset(int myblocknum , int thrnum , int i) 
    {
        Vector2 _00 = new Vector2();
        Vector2 _10 = new Vector2();
        Vector2 _01 = new Vector2();
        Vector2 _11 = new Vector2();
        Vector2 _002 = new Vector2();
        Vector2 _102 = new Vector2();
        Vector2 _012 = new Vector2();
        Vector2 _112 = new Vector2();
        Vector2 _003 = new Vector2();
        Vector2 _103 = new Vector2();
        Vector2 _013 = new Vector2();
        Vector2 _113 = new Vector2();
        int drawonemat = 1; 
        // 한개 텍스쳐로 6개면을 다 그리는경우 2일시 두개로 적용
         // 전체 블록 텍스쳐 에서 top left 기준으로 x로 몇번째 y몇번째 블록인지 설정.
        int xnum = 1; int ynum = 1; // 모두 같은 텍스쳐일시
        int xnum2 = 1;int ynum2 = 1; // 윗면 아랫면만 다른경우 윗면
        int xnum3 = 1;int ynum3 = 1; // 윗면 아랫면만 다른경우 아랫면
        int xnum4 = 1; int ynum4 = 1; // 왼 면
        int xnum5 = 1; int ynum5 = 1; // 정 면
        int xnum6 = 1; int ynum6 = 1; // 뒷 면
        int xnum7 = 1; int ynum7 = 1; // 오른 면
        if (myblocknum == 1) // 흙
        {
            xnum=3; ynum=1;
            drawonemat = 1;
        }
        else if (myblocknum == 2) // 풀
        {
            xnum = 4; ynum = 1;
            xnum2 = 13; ynum2 = 13;
            xnum3 = 3; ynum3 = 1;
            drawonemat = 2;
        }
        else if (myblocknum == 3) // 밝은 회색 돌
        {
            xnum = 1; ynum = 1;
            drawonemat = 1;
        }
        else if (myblocknum == 4) // 깊은 회색 돌
        {
            xnum = 2; ynum = 1;
            drawonemat = 1;
        }
        else if (myblocknum == 5) // 물블록
        {
            xnum = 14; ynum = 13;
            drawonemat = 1;
        }
        else if (myblocknum == 31 || myblocknum == 32 || myblocknum == 33 ) // 나무
        {
            xnum = 5; ynum = 2;
            xnum2 = 6; ynum2 = 2;
            xnum3 = 6; ynum3 = 2;
            drawonemat = 2;
        }
        else if (myblocknum == 34) // 나무풀
        {
            xnum = 5; ynum = 4;
            drawonemat = 1;
        }
        else if (myblocknum == 35) // 나무풀
        {
            xnum = 6; ynum = 4;
            drawonemat = 1;
        }
        else if (myblocknum == 11) // 석탄
        {
            xnum = 3; ynum = 3;
            drawonemat = 1;
        }
        else if (myblocknum == 12) // 철
        { 
            xnum = 2; ynum = 3;
            drawonemat = 1;
        }
        else if (myblocknum == 13) // 코발트
        {
            xnum = 4; ynum = 4;
            drawonemat = 1;
        }
        else if (myblocknum == 14) // 코발트 블루
        {
            xnum = 1; ynum = 11;
            drawonemat = 1;
        }
        else if (myblocknum == 15) // 금
        {
            xnum = 1; ynum = 3;
            drawonemat = 1;
        }
        else if (myblocknum == 16) // 다이아
        {
            xnum = 3; ynum = 4;
            drawonemat = 1;
        }
        if (drawonemat == 1) //d그리는 방식 6면이 다 같은 것일시
        {
            _00.x = 0 + 0.0625f * (xnum - 1); _00.y = 1 - 0.0625f * ynum;
            _10.x = 0 + 0.0625f * xnum; _10.y = 1 - 0.0625f * ynum;
            _01.x = 0 + 0.0625f * (xnum - 1); _01.y = 1 - 0.0625f * (ynum - 1);
            _11.x = 0 + 0.0625f * xnum; _11.y = 1 - 0.0625f * (ynum - 1);
            l_uvs[thrnum][0 + i * 24] = _11; l_uvs[thrnum][1 + i * 24] = _01; l_uvs[thrnum][2 + i * 24] = _00; l_uvs[thrnum][3 + i * 24] = _10; // 아랫면
            l_uvs[thrnum][4 + i * 24] = _11; l_uvs[thrnum][5 + i * 24] = _01; l_uvs[thrnum][6 + i * 24] = _00; l_uvs[thrnum][7 + i * 24] = _10; // 왼쪽
            l_uvs[thrnum][8 + i * 24] = _11; l_uvs[thrnum][9 + i * 24] = _01; l_uvs[thrnum][10 + i * 24] = _00; l_uvs[thrnum][11 + i * 24] = _10; // 정면
            l_uvs[thrnum][12 + i * 24] = _11; l_uvs[thrnum][13 + i * 24] = _01; l_uvs[thrnum][14 + i * 24] = _00; l_uvs[thrnum][15 + i * 24] = _10; // 뒷면
            l_uvs[thrnum][16 + i * 24] = _11; l_uvs[thrnum][17 + i * 24] = _01; l_uvs[thrnum][18 + i * 24] = _00; l_uvs[thrnum][19 + i * 24] = _10; // 오른쪽
            l_uvs[thrnum][20 + i * 24] = _11; l_uvs[thrnum][21 + i * 24] = _01; l_uvs[thrnum][22 + i * 24] = _00; l_uvs[thrnum][23 + i * 24] = _10; // 윗면
        }
        else if (drawonemat == 2) // 윗면 아랫면이 다른 면을 그려야할경우
        {
            _00.x = 0 + 0.0625f * (xnum - 1); _00.y = 1 - 0.0625f * ynum; // 오른쪽 왼쪽 뒷쪽 정면
            _10.x = 0 + 0.0625f * xnum; _10.y = 1 - 0.0625f * ynum;
            _01.x = 0 + 0.0625f * (xnum - 1); _01.y = 1 - 0.0625f * (ynum - 1);
            _11.x = 0 + 0.0625f * xnum; _11.y = 1 - 0.0625f * (ynum - 1);

            _002.x = 0 + 0.0625f * (xnum2 - 1); _002.y = 1 - 0.0625f * ynum2; // 윗면
            _102.x = 0 + 0.0625f * xnum2; _102.y = 1 - 0.0625f * ynum2;
            _012.x = 0 + 0.0625f * (xnum2 - 1); _012.y = 1 - 0.0625f * (ynum2 - 1);
            _112.x = 0 + 0.0625f * xnum2; _112.y = 1 - 0.0625f * (ynum2 - 1);

            _003.x = 0 + 0.0625f * (xnum3 - 1); _003.y = 1 - 0.0625f * ynum3; //아랫면
            _103.x = 0 + 0.0625f * xnum3; _103.y = 1 - 0.0625f * ynum3;
            _013.x = 0 + 0.0625f * (xnum3 - 1); _013.y = 1 - 0.0625f * (ynum3 - 1);
            _113.x = 0 + 0.0625f * xnum3; _113.y = 1 - 0.0625f * (ynum3 - 1);
            l_uvs[thrnum][0 + i * 24] = _113; l_uvs[thrnum][1 + i * 24] = _013; l_uvs[thrnum][2 + i * 24] = _003; l_uvs[thrnum][3 + i * 24] = _103; // 아랫면
            l_uvs[thrnum][4 + i * 24] = _11; l_uvs[thrnum][5 + i * 24] = _01; l_uvs[thrnum][6 + i * 24] = _00; l_uvs[thrnum][7 + i * 24] = _10; // 왼쪽
            l_uvs[thrnum][8 + i * 24] = _11; l_uvs[thrnum][9 + i * 24] = _01; l_uvs[thrnum][10 + i * 24] = _00; l_uvs[thrnum][11 + i * 24] = _10; // 정면
            l_uvs[thrnum][12 + i * 24] = _11; l_uvs[thrnum][13 + i * 24] = _01; l_uvs[thrnum][14 + i * 24] = _00; l_uvs[thrnum][15 + i * 24] = _10; // 뒷면
            l_uvs[thrnum][16 + i * 24] = _11; l_uvs[thrnum][17 + i * 24] = _01; l_uvs[thrnum][18 + i * 24] = _00; l_uvs[thrnum][19 + i * 24] = _10; // 오른쪽
            l_uvs[thrnum][20 + i * 24] = _112; l_uvs[thrnum][21 + i * 24] = _012; l_uvs[thrnum][22 + i * 24] = _002; l_uvs[thrnum][23 + i * 24] = _102; // 윗면
        }

    }

    // 게임 종료시 쓰레드 종료
    public void application_end()
    {
        for (int i = 0; i < MinePostBox.mytreadnum; ++i)
        {
            myThread[i].Abort();
        }
    }
}
