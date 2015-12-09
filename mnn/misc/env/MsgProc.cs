﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace mnn.misc.env {
    public abstract class MsgProc : module.IModule, IMsgProc {
        class DataHandleMsg {
            public IPEndPoint EP;
            public string Content;
        }

        // Fileds for Main Thread
        private const int max_msg_count = 1000;
        private bool isExitThread = false;
        private Semaphore sem = new Semaphore(0, max_msg_count);
        private Queue<DataHandleMsg> msgQueue = new Queue<DataHandleMsg>();

        // Socket for sending cmd
        protected Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        protected abstract string LogPrefix { get; }
        protected abstract string ErrLogPrefix { get; }

        // IModule ========================================================================

        public void Init()
        {
            Thread thread = new Thread(() =>
            {
                isExitThread = false;
                DataHandleMsg msg = null;

                while (true) {
                    if (isExitThread == true) {
                        isExitThread = false;
                        break;
                    }

                    sem.WaitOne();
                    lock (msgQueue) {
                        msg = msgQueue.Dequeue();
                    }
                    try {
                        HandleMsg(msg.EP, msg.Content);
                    } catch (Exception ex) {
                        util.Logger.WriteException(ex, ErrLogPrefix);
                    }
                }
            });

            //thread.IsBackground = true;
            //thread.Start();
        }

        public void Final()
        {
            isExitThread = true;
        }

        public abstract string GetModuleID();

        public abstract string GetModuleInfo();

        // IMsgProc ========================================================================

        public virtual void AtCmdResult(AtCommand atCmd) { }

        public void AppendMsg(System.Net.IPEndPoint ep, string msg)
        {
            if (msgQueue.Count() >= max_msg_count)
                return;

            lock (msgQueue) {
                msgQueue.Enqueue(new DataHandleMsg() { EP = ep, Content = msg });
            }
            sem.Release();
        }

        public abstract void HandleMsg(IPEndPoint ep, string msg);

        protected abstract void HandleAlive(IPEndPoint ep, IDictionary<string, string> dc);

        protected abstract void HandleAlarm(IPEndPoint ep, IDictionary<string, string> dc);

        protected abstract void HandleDetect(IPEndPoint ep, IDictionary<string, string> dc);

        // Private Tools ===========================================================================

        private void SendToClient(string url)
        {
            url = mnn.net.EncryptSym.AESEncrypt(url);
            byte[] buffer = Encoding.UTF8.GetBytes(url);
            buffer = new byte[] { 0x01, 0x0C, (byte)(0x04 + buffer.Length & 0xff), (byte)(0x04 + buffer.Length >> 8 & 0xff) }
                .Concat(buffer).ToArray();

            try {
                if (!socket.Connected)
                    socket.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2000));
                if (socket.Connected)
                    socket.Send(buffer);
            } catch (Exception ex) {
                util.Logger.WriteException(ex, ErrLogPrefix);
                //util.Logger.Write("Connect to Center(port: 2000) failed!", ErrLogPrefix);
            }
        }

        protected void SendClientClose(string ip, int port)
        {
            string url = "/center/clientclose"
                + "?type=accept" + "&ip=" + ip + "&port=" + port;

            SendToClient(url);
        }

        protected void SendClientMsg(string ip, int port, string msg)
        {
            string url = "/center/clientsend"
                + "?type=accept" + "&ip=" + ip + "&port=" + port + "&data=" + msg;

            SendToClient(url);
        }

        protected void SendClientMsgByCcid(string ccid, string msg)
        {
            string url = "/center/clientsendbyccid"
                + "?type=accept" + "&ccid=" + ccid + "&data=" + msg;

            SendToClient(url);
        }

        protected void SendClientUpdate(string ip, int port, string ccid, string name)
        {
            string url = "/center/clientupdate"
                + "?ip=" + ip + "&port=" + port + "&ccid=" + ccid + "&name=" + name;

            SendToClient(url);
        }
    }
}
