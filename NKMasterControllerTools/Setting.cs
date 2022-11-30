using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class MtcKey
{
    public int ID;
    public int VK;
    public string Name;
    public override string ToString()
    {
        return Name;
    }
}

public class MTCSetting
{

    // P1～P13の場合の段数
    public int[][] Acc = new[]
    {
        new[] { 0, 2, 3, 4, 99},                                 // Max P4の場合
        new[] { 0, 1, 2, 3, 4, 99, },                            // Max P5の場合
        new[] { 0, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4, 5, 99 },     // Max P13の場合
        //      0  1  2  3  4  5  6  7  8  9  0  1  2  3
    };

    // B1～P9の場合の段数
    public int[][] Brake = new []
    {
        new [] { 0, -2, -4, -6, -7, -8, -99},                // Max B5+非常の場合
        new [] { 0, -2, -3, -4, -6, -7, -8, -99},            // Max B6+非常の場合
        new [] { 0, -2, -3, -4, -5, -6, -7, -8, -99},        // Max B7+非常の場合
        new [] { 0, -1, -2, -3, -4, -5, -6, -7, -8, -99},    // Max B8+非常の場合
        //       0  -1  -2  -3  -4  -5  -6  -7  -8  -9
    };



    public int[] A = new[] { 0, 0 };
    public int[] A2 = new[] { 0, 0 };
    public int[] B = new[] { 0, 0 };
    public int[] C = new[] { 0, 0 };
    public int[] D = new[] { 0, 0 };
    public int[] S = new[] { 0, 0 };
    public int[] SA = new[] { 0, 0 };
    public int[] SA2 = new[] { 0, 0 };
    public int[] SB = new[] { 0, 0 };
    public int[] SC = new[] { 0, 0 };
    public int[] SD = new[] { 0, 0 };
    public int[] ABCD = new[] { 0, 0 };
    public int[] SABCD = new[] { 0, 0 };

    public int[] START = new[] { 0, 0 };
    public int[] SELECT = new[] { 0, 0 };

    public int[] S_START = new[] { 0, 0 };
    public int[] S_SELECT = new[] { 0, 0 };

    public int[] UP = new[] { 0, 0 };
    public int[] DOWN = new[] { 0, 0 };

    public int[] LEFT = new[] { 0, 0 };
    public int[] RIGHT = new[] { 0, 0 };

    const uint KEYEVENTF_KEYDOWN = 0x0000;
    const uint KEYEVENTF_KEYPRES = 0x0001;
    const uint KEYEVENTF_KEYUP = 0x0002;


}



