using System;

namespace SimpleIPC
{
    public class IpcEventArgs : EventArgs
    {
        public string SerializedObject { get; set; }
    }
}
