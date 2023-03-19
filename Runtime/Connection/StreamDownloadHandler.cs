using UnityEngine.Networking; 
using System;

namespace SmartNPC
{
    public class StreamDownloadHandler : DownloadHandlerScript {
        private StreamDownloadHandlerOptions _options;
        private string _result = "";

        public StreamDownloadHandler(StreamDownloadHandlerOptions options): base() {
            _options = options;
        }

        // Required by DownloadHandler base class. Called when you address the 'bytes' property.
        protected override byte[] GetData() { return null; }

        // Called once per frame when data has been received from the network.
        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            if (data == null || data.Length < 1) return false;

            string chunk = ByteArrayToString(data);

            _result += chunk;

            if (_options?.OnProgress != null) _options.OnProgress(_result, chunk);

            return true;
        }

        protected override void CompleteContent()
        {
            if (_options?.OnComplete != null) _options.OnComplete(_result);
        }

        private string ByteArrayToString(byte[] ba)
        {
            string result = "";

            for (int i = 0; i < ba.Length; i++) {
                result += Convert.ToChar(ba[i]);
            }

            return result;
        }
    }
}