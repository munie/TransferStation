﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mnn.util
{
    public delegate void AtCmdDelegate(object arg);

    class AtCmd
    {
        public string name;
        public AtCmdDelegate func;
        public List<object> args;

        public AtCmd(string name, AtCmdDelegate func)
        {
            this.name = name;
            this.func = func;
            this.args = new List<object>();
        }
    }

    public class AtCmdCenter
    {
        private List<AtCmd> atcmd_table;

        public AtCmdCenter()
        {
            atcmd_table = new List<AtCmd>();
        }

        public void Perform()
        {
            lock (atcmd_table) {
                foreach (var item in atcmd_table) {
                    foreach (var arg in item.args.ToArray()) {
                        item.func(arg);
                        item.args.Remove(arg);
                    }
                }
            }
        }

        public void Add(string name, AtCmdDelegate func)
        {
            lock (atcmd_table) {
                atcmd_table.Add(new AtCmd(name, func));
            }
        }

        public void Del(string name)
        {
            lock (atcmd_table) {
                foreach (var item in atcmd_table) {
                    if (item.name.Equals(name))
                        atcmd_table.Remove(item);
                }
            }
        }

        public void AppendCommand(string name, object arg)
        {
            lock (atcmd_table) {
                foreach (var item in atcmd_table) {
                    if (item.name.Equals(name))
                        item.args.Add(arg);
                }
            }
        }
    }
}
