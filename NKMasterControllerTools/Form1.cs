﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using LibUsbDotNet;
using LibUsbDotNet.Main;
using static System.Net.Mime.MediaTypeNames;
using static MtcUSBReader;

namespace NKMasterControllerTools
{
    public partial class Form1 : Form
    {

        private Task m_Task = null;
        private bool m_EndFlag = false;

        private MtcUSBReader mtcUSB;
        private JreMtcSetting JreMtcSetting;

        public Form1()
        {
            mtcUSB = new MtcUSBReader(this, OnMtcMoved, OnMtcErr);
            JreMtcSetting = new JreMtcSetting();
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            m_Task = new Task(() =>
            {
                string prevStr = "";

                while (m_EndFlag == false)
                {
                    mtcUSB.Read();

                }

                mtcUSB.Close();
            });
            m_Task.Start();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            m_EndFlag = true;

            if (m_Task != null)
            {
                m_Task.Wait();
            }
        }


        private void OnMtcMoved(MtcUSBReader.MTC mtc)
        {
            string text = $"{mtc.Value} ({mtc.Min}～{mtc.Max})  " + 
                          $"{(mtc.FR > 0 ? "前" : "")}{(mtc.FR == 0 ? "中" : "")}{(mtc.FR < 0 ? "後" : "")}" +
                          $"{(mtc.A && (mtc.A2==false) ? " Ａ" : "")}" + $"{(mtc.A && mtc.A2 ? " Ａ強" : "")}" +
                          $"{(mtc.B ? " Ｂ" : "")}{(mtc.C ? " Ｃ" : "")}{(mtc.D ? " Ｄ" : "")}{(mtc.ATS ? " Ｓ" : "")}" +
                          $"{(mtc.Left ? " ←" : "")}{(mtc.Up ? " ↑" : "")}{(mtc.Down ? " ↓" : "")}{(mtc.Right ? " →" : "")}" +
                          $"{(mtc.Start ? " |Start>" : "")}{(mtc.Select ? " |Select|" : "")}" +
                          "";

            JreMtcSetting.OnKeyDown(mtc);
            JreMtcSetting.OnValueChanged(mtc);
            JreMtcSetting.OnFrChanged(mtc);

            DoInvoke(() => Text = text);
        }


        private void OnMtcErr()
        {
            JreMtcSetting.ClereKeyDown();
            DoInvoke(() => Text = "読み込み失敗");
        }

        private void DoInvoke(Action action)
        {
            Invoke((MethodInvoker)(() => action()));
        }

    }
}