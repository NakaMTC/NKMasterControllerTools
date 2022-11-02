using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NKMasterControllerTools
{
    internal class JreMtcSetting
    {

        [DllImport("USER32.dll", CallingConvention = CallingConvention.StdCall)]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUPDOWN = 0x0001;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const byte VK_UP = 0x26;
        private const byte VK_DOWN = 0x28;


        [DllImport("USER32.dll", CallingConvention = CallingConvention.StdCall)]
        static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);
        private const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const int MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const int MOUSEEVENTF_WHEEL = 0x0800;


        public readonly MtcKey[] AllKeys_JRE = new[] {
            new MtcKey{ID = 0,  VK = 0x00, Name = "----------"           },  // 
            new MtcKey{ID = 1,  VK = 0x0d, Name = "電笛 Enetr"           },  // A
            new MtcKey{ID = 2,  VK = 0x08, Name = "空笛 BackSpase"       },  // A2 ,B
            new MtcKey{ID = 3,  VK = 'W',  Name = "W 定速・機関ブレーキ" },  // C
            new MtcKey{ID = 4,  VK = 'E',  Name = "E EB装置"             },  // D
            new MtcKey{ID = 5,  VK= 0x1B,  Name = "Esc 終了"             },  // ABCD
            new MtcKey{ID = 6,  VK= 'B',   Name = "B ブザー"             },  // ATS
            new MtcKey{ID = 7,  VK= 'P',   Name="P 一時停止"             },  // START
            new MtcKey{ID = 8,  VK= 'C',   Name="C 視点切替１"           },  // SELECT 1
            new MtcKey{ID = 9,  VK= 'V',   Name="V 視点切替２"           },  // SELECT 2
            new MtcKey{ID = 10, VK=0x25 , Name="←" },
            new MtcKey{ID = 11, VK=0x26 , Name="↑" },
            new MtcKey{ID = 12, VK=0x27 , Name="→" },
            new MtcKey{ID = 13, VK=0x28 , Name="↓" },
        };

        MTCSetting setting;

        public JreMtcSetting()
        {
            setting = new MTCSetting
            {
                A = new[] { 0x0d, 0 },      // 電笛 Enetr
                A2 = new[] { 0x08, 0 },     // 空笛 BackSpase
                B = new[] { 0x08, 0 },      // 空笛 BackSpase
                C = new[] { 'W', 0 },       // 定速・機関ブレーキ
                D = new[] { 'E', 0 },       // EB装置
                S = new[] { 'B', 0 },       // ブザー
                ABCD = new[] { 0x1B, 0 },   // Esc 終了
                SA = new int[] { 0, 0 },
                SA2 = new int[] { 0, 0 },
                SB = new int[] { 0, 0 },
                SC = new int[] { 0, 0 },
                SD = new int[] { 0, 0 },
                SABCD = new int[] { 0x1B, 0 },   // Esc 終了
                START = new int[] { 'P', 0 },        // ポーズ
                SELECT = new int[] { 'C', 'V' },    // 死点切替
                LEFT = new int[] { 0x25, 0 },
                UP = new int[] { 0, 0 },
                //UP = new int[] { 0x26, 0 },
                RIGHT = new int[] { 0x27, 0 },
                DOWN = new int[] { 0, 0 },
                //DOWN = new int[] { 0x28, 0 },
            };
        }

        List<int> prevKeys=new List<int>();

        void KeyChange(List<int> list)
        {
            for (int i = prevKeys.Count - 1; i >= 0; i--)
            {
               if (list.Contains(prevKeys[i]) == false)
               {
                    keybd_event((byte)prevKeys[i], 0, KEYEVENTF_KEYUP, 0);
                    MtcKey[] mtcKey = AllKeys_JRE.Where(x => x.VK == prevKeys[i]).ToArray();
                    prevKeys.RemoveAt(i);
                }
            }

            foreach (var item  in list)
            {
                if (item != 0 && prevKeys.Contains(item) == false )
                {
                    keybd_event((byte)item, 0, KEYEVENTF_KEYDOWN, 0);
                    prevKeys.Add(item);
                }
            }
        }


        int prevFr = 0;

        public void OnFrChanged(MtcUSBReader mtc)
        {
            if(mtc.FR != prevFr)
            {
                if (mtc.FR > 0)
                {   // OnFrChanged : 前進 ↑↑
                    keybd_event(VK_UP, 0, KEYEVENTF_KEYUPDOWN, 0);
                    Thread.Sleep(10);
                    keybd_event(VK_UP, 0, KEYEVENTF_KEYUPDOWN, 0);
                }
                else if (mtc.FR < 0)
                {   // OnFrChanged : 後退 ↓↓
                    keybd_event(VK_DOWN, 0, KEYEVENTF_KEYUPDOWN, 0);
                    Thread.Sleep(10);
                    keybd_event(VK_DOWN, 0, KEYEVENTF_KEYUPDOWN, 0);
                }
                else if (prevFr > 0)
                {   // OnFrChanged : 中立 ↓
                    keybd_event(VK_DOWN, 0, KEYEVENTF_KEYUPDOWN, 0);
                }
                else if (prevFr < 0)
                {   // OnFrChanged : 中立 ↑
                    keybd_event(VK_UP, 0, KEYEVENTF_KEYUPDOWN, 0);
                }
                prevFr = mtc.FR;
            }
        }


        int prevValue = 0;

        public void OnValueChanged(MtcUSBReader mtc)
        {
            // 値変更
            int val = 0;
            if (mtc.Value > 0)
            {
                if (mtc.Max == 4 && mtc.Value <= 4) val = setting.Acc[0][mtc.Value];
                if (mtc.Max == 5 && mtc.Value <= 5) val = setting.Acc[1][mtc.Value];
                if (mtc.Max == 13 && mtc.Value <= 13) val = setting.Acc[2][mtc.Value];
            }
            else if (mtc.Value < 0)
            {
                if (mtc.Min == -6 && mtc.Value >= -6) val = setting.Brake[0][-mtc.Value];
                if (mtc.Min == -7 && mtc.Value >= -7) val = setting.Brake[1][-mtc.Value];
                if (mtc.Min == -8 && mtc.Value >= -8) val = setting.Brake[2][-mtc.Value];
                if (mtc.Min == -9 && mtc.Value >= -9) val = setting.Brake[2][-mtc.Value];
            }

            if (val > 5) val = 5;
            if (val < -9) val = -9;

            if(val != prevValue)
            {
                int move;
                if (val >= 5) move = 20;
                else if (val <= -9) move = -20;
                else move = val - prevValue;

                prevValue = val;


                if (mtc.Value == 0)
                {
                    mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, 0);
                    Thread.Sleep(10);
                    mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, 0);
                }
                else if (move != 0)
                {
                    mouse_event(MOUSEEVENTF_WHEEL, 0, 0, -move * 120, 0);
                }
            }
        }



        private bool pnd_SABCD;
        private bool pnd_ABCD;
        private bool pnd_SA2;
        private bool pnd_SA;
        private bool pnd_SB;
        private bool pnd_SC;
        private bool pnd_SD;
        private bool pnd_A2;

        public void ClereKeyDown()
        {
            KeyChange(new List<int>());
        }

        public void OnKeyDown(MtcUSBReader mtc)
        {
            bool A = mtc.A;
            bool A2 = mtc.A2;
            bool B = mtc.B;
            bool C = mtc.C;
            bool D = mtc.D;
            bool S = mtc.ATS;
            bool ABCD = A && B && C && D;
            bool SA = S && A;
            bool SA2 = S && A2;
            bool SB = S && B;
            bool SC = S && C;
            bool SD = S && D;
            bool SABCD = S && A && B && C && D;

            bool Start = mtc.Start;
            bool Select = mtc.Select;
            bool Left = mtc.Left & !mtc.Right;
            bool Right = !mtc.Left & mtc.Right;
            bool Up = !mtc.Down & mtc.Up;
            bool Down = mtc.Down & !mtc.Up;


            if (SABCD) pnd_SABCD = true;
            if(!S & !A & !B & !C & !D) pnd_SABCD = false;

            if (pnd_SABCD)
            {
                A = false;
                A2 = false;
                B = false;
                C = false;
                D = false;
                S = false;
                ABCD = false;
                SA = false;
                SA2 = false;
                SB = false;
                SC = false;
                SD = false;
            }

            if (ABCD) pnd_ABCD = true;
            if (!A & !B & !C & !D) pnd_ABCD = false;

            if (pnd_ABCD)
            {
                A = false;
                A2 = false;
                B = false;
                C = false;
                D = false;
            }

            if (SA2) pnd_SA2 = true;
            if (!A & !A2 & !S) pnd_SA2 = false;

            if (pnd_SA2)
            {
                S = false;
                SA = false;
                A2 = false;
                A = false;
            }

            if (SA) pnd_SA = true;
            if (!A & !S) pnd_SA = false;

            if (pnd_SA)
            {
                S = false;
                A = false;
                A2 = false;
            }

            if (SB) pnd_SB = true;
            if (!B & !S) pnd_SB = false;

            if (pnd_SB)
            {
                S = false;
                B = false;
            }

            if (SC) pnd_SC = true;
            if (!C & !S) pnd_SC = false;

            if (pnd_SC)
            {
                S = false;
                C = false;
            }

            if (SD) pnd_SD = true;
            if (!D & !S) pnd_SD = false;

            if (pnd_SD)
            {
                S = false;
                D = false;
            }

            if (A2) pnd_A2 = true;
            if (!A & !A2) pnd_A2 = false;

            if (pnd_A2)
            {
                A = false;
            }


            string str = (A ? "[A]" : "") + (A2 ? "[A2]" : "") + (B ? "[B]" : "") + (C ? "[C]" : "")
                       + (D ? "[D]" : "") + (S ? "[S]" : "")
                       + (ABCD ? "[ABCD]" : "")
                       + (SA ? "[SA]" : "") + (SA2 ? "[SA2]" : "") + (SB ? "[SB]" : "") + (SC ? "[SC]" : "") + (SD ? "[SD]" : "")
                       + (SABCD ? "[SABCD]" : "")
                       + (Start ? "[Start]" : "") + (Select ? "[Select]" : "")
                       + (Left ? "[←]" : "") + (Right ? "[→]" : "") + (Up ? "[↑]" : "") + (Down ? "[↓]" : "");


            List<int> list = new List<int>();

            if (A) list.AddRange(setting.A);
            if (A2) list.AddRange(setting.A2);
            if (B) list.AddRange(setting.B);
            if (C) list.AddRange(setting.C);
            if (D) list.AddRange(setting.D);
            if (S) list.AddRange(setting.S);
            if (ABCD) list.AddRange(setting.ABCD);
            if (SA) list.AddRange(setting.SA);
            if (SA2) list.AddRange(setting.SA2);
            if (SB) list.AddRange(setting.SB);
            if (SC) list.AddRange(setting.SC);
            if (SD) list.AddRange(setting.SD);
            if (SABCD) list.AddRange(setting.SABCD);

            if (Start) list.AddRange(setting.START);
            if (Select) list.AddRange(setting.SELECT);

            if (Left) list.AddRange(setting.LEFT);
            if (Right) list.AddRange(setting.RIGHT);
            if (Up) list.AddRange(setting.UP);
            if (Down) list.AddRange(setting.DOWN);

            KeyChange(list);
            //Console.WriteLine(str);



        }

    }
}
