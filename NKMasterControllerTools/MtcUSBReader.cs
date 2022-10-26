using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using Microsoft.Win32;


/// <summary>
/// USBからMTCの読み込みを行なう
/// </summary>
public class MtcUSBReader
{
    private UsbEndpointReader m_Reader = null;

    
    private int m_MAX { get; set; }
    private int m_MIN;

    /// <summary> 前回のUSBの状態 （32ビットに変換する） </summary>
    private uint m_Prev = 0;

    public struct MTC
    {
        /// <summary> 最大ノッチ数　MTCの場合、4 or 5 or 13 </summary>
        public int Max;

        /// <summary> 非常を含む最大ブレーキ数　MTCの場合、-6(B5非常)～-9(B8非常)</summary>
        public int Min;

        /// <summary> 現在のノッチ数　MTCの場合、-9(B8非常)～0(OFF)～13</summary>
        public int Value;

        /// <summary> 前進（+1） or 中立(0) or 後退 (-1) </summary>
        public int FR;

        public bool A;
        public bool A2;
        public bool B;
        public bool C;
        public bool D;

        public bool ATS;
        public bool Start;
        public bool Select;

        public bool Up;
        public bool Down;
        public bool Left;
        public bool Right;
    }

    private object ParentForm;

    private Action<MTC> OnMTCMoved;

    private Action OnError;


    public MtcUSBReader(object parentForm, Action<MTC> onMTCMoved, Action onError)
    {
        this.ParentForm = parentForm;
        this.OnMTCMoved = onMTCMoved;
        this.OnError = onError;
    }

    /// <summary> USBから読み込む </summary>
    public void Read()
    {
        try
        {
            lock (this)
            {
                byte[] buff = new byte[8];  // 8バイトバッファ
                int m_ReadLen = 0;


                if (m_Reader == null)
                {
                    UsbDevice device = null;

                    m_MAX = 0;
                    m_MIN = 0;

                    foreach (UsbRegistry reg in UsbDevice.AllDevices)
                    {
                        if (reg.Vid == 0x0AE4 && reg.Pid == 0x0101 && reg.Rev == 0400) { m_MAX = 4; m_MIN = 6; }   // P4 B6
                        if (reg.Vid == 0x0AE4 && reg.Pid == 0x0101 && reg.Rev == 0300) { m_MAX = 4; m_MIN = 7; }   // P4 B7
                        if (reg.Vid == 0x1C06 && reg.Pid == 0x77A7 && reg.Rev == 0202) { m_MAX = 5; m_MIN = 5; }   // P4 B5
                        if (reg.Vid == 0x0AE4 && reg.Pid == 0x0101 && reg.Rev == 0800) { m_MAX = 5; m_MIN = 7; }   // P5 B7
                        if (reg.Vid == 0x0AE4 && reg.Pid == 0x0101 && reg.Rev == 0000) { m_MAX = 13; m_MIN = 7; }  // P5 B7

                        if (m_MAX > 0 && m_MIN > 0)
                        {
                            if (reg.Open(out device) == false || device == null) throw new Exception($"接続失敗\r\n{reg.Name}\r\n{UsbDevice.LastErrorString}");
                            break;
                        }
                    }

                    if (device == null) return;
                    m_Reader = device.OpenEndpointReader(ReadEndpointID.Ep01);
                }

                ErrorCode code = m_Reader.Read(buff, 2000, out m_ReadLen);

                if (m_ReadLen > 0)
                {
                    // 8バイトのバイト列 → 32ビット数のビット演算
                    //          [1] レバーの状態     [2]～[4] キー入力の状態  　[0] と [5]～[7] は無視する
                    uint tmp = (uint)buff[1] << 00 | (uint)buff[2] << 08 | (uint)buff[3] << 16 | (uint)buff[4] << 24;

                    if (m_Prev != tmp)
                    {
                        uint bitA = 0x0400;
                        uint bitA2 = 0x0c00;
                        uint bitB = 0x1000;
                        uint bitC = 0x2000;
                        uint bitD = 0x0200;

                        uint bitS = 0x0100;
                        uint bitSt = 0x00010000;
                        uint bitSe = 0x00020000;

                        uint bitUp = 0x00040000;
                        uint bitDown = 0x00080000;
                        uint bitLeft = 0x00100000;
                        uint bitRight = 0x00200000;


                        //Console.WriteLine($"tmp=0x{tmp:x8} Lever={(tmp & 0x1F)},{(buff[1] & 0x1F)}  FR = {(tmp & 0xF0) >> 4},{(buff[1] & 0xF0) >> 4}");
                        //Console.WriteLine($"A={CheckBit(tmp, bitA)} A2={CheckBit(tmp, bitA2)} B={CheckBit(tmp, bitB)} C={CheckBit(tmp, bitC)} D={CheckBit(tmp, bitD)}");
                        //Console.WriteLine($"S={CheckBit(tmp, bitS)} Start={CheckBit(tmp, bitSt)} Select={CheckBit(tmp, bitSe)}");

                        //Console.WriteLine($"←{CheckBit(tmp, bitLeft)} ↑{CheckBit(tmp, bitUp)} ↓={CheckBit(tmp, bitDown)} →={CheckBit(tmp, bitRight)}");

                        int fr, val;

                        if (m_MAX == 13)
                        {
                            fr = (int)((tmp & 0xE0) >> 4);
                            if (fr == 0) fr = 0;
                            else if (fr == 4) fr = -1;
                            if (fr == 8) fr = 1;

                            val = (int)(tmp & 0x1F) - 1 - m_MIN - 1;

                        }
                        else
                        {
                            fr = (int)((tmp & 0xF0) >> 4);
                            if (fr == 0) fr = 0;
                            else if (fr == 1) fr = -1;
                            if (fr == 2) fr = 1;

                            val = (int)(tmp & 0x0F) - 1 - m_MIN - 1;
                        }

                        OnMTCMoved(new MTC
                        {
                            Max = m_MAX,
                            Min = -m_MIN - 1,
                            Value = val,
                            FR = fr,
                            A = CheckBit(tmp, bitA),
                            A2 = CheckBit(tmp, bitA2),
                            B = CheckBit(tmp, bitB),
                            C = CheckBit(tmp, bitC),
                            D = CheckBit(tmp, bitD),
                            ATS = CheckBit(tmp, bitS),
                            Start = CheckBit(tmp, bitSt),
                            Select = CheckBit(tmp, bitSe),
                            Left = CheckBit(tmp, bitLeft),
                            Up = CheckBit(tmp, bitUp),
                            Down = CheckBit(tmp, bitDown),
                            Right = CheckBit(tmp, bitRight),
                        }
                        );



                        m_Prev = tmp;
                    }
                }

                if (code != ErrorCode.None && code != ErrorCode.IoTimedOut) throw new Exception($"読込失敗\r\n{UsbDevice.LastErrorString}");
            } // lock (this)
        }
        catch (Exception ex)
        {
            OnError();
            Close();
        }
    }


    private static bool CheckBit(uint bits, uint mask)
    {
        return ( (bits & mask) == mask) ;
    }

    public void Close()
    {
        lock (this)
        {

            if (m_Reader != null)
            {
                try { if (m_Reader.IsDisposed == false) m_Reader.Dispose(); } catch (Exception) { }
                try { m_Reader.Device.Close(); } catch (Exception) { }
                m_Reader = null;
            }

            m_MAX = 0;
            m_MIN = 0;
        } 
    }
}
