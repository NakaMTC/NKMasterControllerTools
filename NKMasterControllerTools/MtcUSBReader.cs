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

    public MtcUSBReader(Action OnMTCMoved, Action OnError)
    {
        this.OnMTCMoved = OnMTCMoved;
        this.OnError = OnError;
    }

    /// <summary> MTCの実行時に実行する処理 </summary>
    private readonly Action OnMTCMoved;

    /// <summary> エラー時に実行する処理 </summary>
    private readonly Action OnError;

    /// <summary> USBのReader </summary>
    private UsbEndpointReader m_UsbEndpointReader = null;

    /// <summary> 前回読み込み時のUSBのバイト列→32ビットに変換したもの </summary>
    private uint m_PrevUint32 = 0;


    public bool IsOK
    {
        get
        {
            return (m_UsbEndpointReader != null);
        }
    } 

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


    /// <summary> USBから読み込む </summary>
    public void Read()
    {
        try
        {
            lock (this)
            {
                byte[] buff = new byte[8];  // 8バイトバッファ
                int readLen = 0;

                
                if (m_UsbEndpointReader == null)    // UsbEndpointReader の OPEN
                {
                    UsbDevice device = null;

                    Max = 0;
                    Min = 0;

                    foreach (UsbRegistry reg in UsbDevice.AllDevices)
                    {
                        if (reg.Vid == 0x0AE4 && reg.Pid == 0x0101 && reg.Rev == 0400) { Max = 4; Min = -7; }   // P4 B6 (非常を含めて-7)
                        if (reg.Vid == 0x0AE4 && reg.Pid == 0x0101 && reg.Rev == 0300) { Max = 4; Min = -8; }   // P4 B7 (非常を含めて-8)
                        if (reg.Vid == 0x1C06 && reg.Pid == 0x77A7 && reg.Rev == 0202) { Max = 5; Min = -6; }   // P4 B5 (非常を含めて-6)
                        if (reg.Vid == 0x0AE4 && reg.Pid == 0x0101 && reg.Rev == 0800) { Max = 5; Min = -8; }   // P5 B7 (非常を含めて-8)
                        if (reg.Vid == 0x0AE4 && reg.Pid == 0x0101 && reg.Rev == 0000) { Max = 13; Min = -8; }  // P5 B7 (非常を含めて-8)

                        if (Max > 0 && Min < 0)
                        {
                            if (reg.Open(out device) && device != null) break;  // 接続に成功 → 次のステップに進む
                            else throw new Exception($"接続失敗\r\n{reg.Name}\r\n{UsbDevice.LastErrorString}");
                        }
                    }

                    if (device == null) return;
                    m_UsbEndpointReader = device.OpenEndpointReader(ReadEndpointID.Ep01);
                }

                // 8バイトバッファへの読み込み
                ErrorCode code = m_UsbEndpointReader.Read(buff, 2000, out readLen);

                // 読み込み失敗 かつ タイムアウト以外 の場合→読み込み失敗
                if ((readLen == 0 || code != ErrorCode.None) && code != ErrorCode.IoTimedOut) throw new Exception($"読込失敗\r\n{UsbDevice.LastErrorString}");


                if (readLen > 0)
                {
                    // 8バイトのバイト列 → 32ビット数のビット演算
                    //          [1] レバーの状態     [2]～[4] キー入力の状態  　[0] と [5]～[7] は無視する
                    uint tmp = (uint)buff[1] << 00 | (uint)buff[2] << 08 | (uint)buff[3] << 16 | (uint)buff[4] << 24;

                    if (m_PrevUint32 != tmp)
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


                        if (Max == 13)  // P13～Bxの場合
                        {
                            int fr = (int)((tmp & 0xE0) >> 4);
                            if (fr == 0) FR = 0;
                            else if (fr == 4) FR = -1;
                            if (fr == 8) FR = 1;

                            Value = (int)(tmp & 0x1F) + Min - 1;
                        }
                        else
                        {
                            int fr = (int)((tmp & 0xF0) >> 4);
                            if (fr == 0) FR = 0;
                            else if (fr == 4) FR = -1;
                            if (fr == 8) FR = 1;

                            Value = (int)(tmp & 0x0F) + Min - 1;
                        }

                        // 各ボタンの状態を保持
                        A = CheckBit(tmp, bitA);
                        A2 = CheckBit(tmp, bitA2);
                        B = CheckBit(tmp, bitB);
                        C = CheckBit(tmp, bitC);
                        D = CheckBit(tmp, bitD);
                        ATS = CheckBit(tmp, bitS);
                        Start = CheckBit(tmp, bitSt);
                        Select = CheckBit(tmp, bitSe);
                        Left = CheckBit(tmp, bitLeft);
                        Up = CheckBit(tmp, bitUp);
                        Down = CheckBit(tmp, bitDown);
                        Right = CheckBit(tmp, bitRight);

                        // 前回の状態を保持
                        m_PrevUint32 = tmp;

                        // mtc動作時の処理を実行する
                        if (OnMTCMoved != null) OnMTCMoved();
                    }
                }
                
            } // lock (this)
        }
        catch (Exception ex)
        {
            Close();
            if (OnError != null) OnError();
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
            if (m_UsbEndpointReader != null)
            {
                try { if (m_UsbEndpointReader.IsDisposed == false) m_UsbEndpointReader.Dispose(); } catch (Exception) { }
                try { m_UsbEndpointReader.Device.Close(); } catch (Exception) { }
                m_UsbEndpointReader = null;
            }

            m_PrevUint32 = 0;
        }
    }
}
