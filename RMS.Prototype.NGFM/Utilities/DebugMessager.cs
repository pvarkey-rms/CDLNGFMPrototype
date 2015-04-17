using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RMS.Utilities
{
    public class DebugMessager
    {
        public class DebugMessagerEventArgs : EventArgs
        {
            public string Message;
        }

        private DebugMessagerEventArgs m_args;

        public event MessageEventHandler MessageEvent;
        public delegate void MessageEventHandler(Object sender, DebugMessagerEventArgs e);

        public DebugMessager()
        {
            this.m_args = new DebugMessagerEventArgs();
        }

        public void SendMessage(string msg)
        {
            SendMessage(msg, false);
        }

        public void SendMessage(string msg, bool alsoOnConsole = false)
        {
            this.m_args.Message = msg;
            this.OnMessageEvent(this.m_args);
            if (alsoOnConsole)
                Console.WriteLine(msg);
        }

        private void OnMessageEvent(DebugMessagerEventArgs args)
        {
            if (this.MessageEvent != null)
            {
                this.MessageEvent(this, args);
            }
        }
    }
}
