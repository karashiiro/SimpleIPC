using System;

namespace SimpleIPCHttp
{
    public class IpcEventArgs : EventArgs
    {
        public string SerializedObject { get; set; }
    }
}
