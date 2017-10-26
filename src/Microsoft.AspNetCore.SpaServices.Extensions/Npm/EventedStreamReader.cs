using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.NodeServices.Util
{
    class EventedStreamReader
    {
        public delegate void OnReceivedChunkHandler(ArraySegment<char> chunk);
        public delegate void OnReceivedLineHandler(string line);

        public event OnReceivedChunkHandler OnReceivedChunk;
        public event OnReceivedLineHandler OnReceivedLine;

        private readonly StreamReader _streamReader;
        private readonly StringBuilder _linesBuffer;

        public EventedStreamReader(StreamReader streamReader)
        {
            _streamReader = streamReader ?? throw new ArgumentNullException(nameof(streamReader));
            _linesBuffer = new StringBuilder();
            Task.Factory.StartNew(Run);
        }

        public Task<Match> WaitForMatch(Regex regex, int timeoutMilliseconds = 0)
        {
            var tcs = new TaskCompletionSource<Match>();
            var completionLock = new object();

            OnReceivedLineHandler onReceivedLineHandler = null;
            onReceivedLineHandler = line =>
            {
                var match = regex.Match(line);
                if (match.Success)
                {
                    lock (completionLock)
                    {
                        if (!tcs.Task.IsCompleted)
                        {
                            OnReceivedLine -= onReceivedLineHandler;
                            tcs.SetResult(match);
                        }
                    }
                }
            };

            OnReceivedLine += onReceivedLineHandler;

            if (timeoutMilliseconds > 0)
            {
                var timeoutToken = new CancellationTokenSource(timeoutMilliseconds);
                timeoutToken.Token.Register(() =>
                {
                    lock (completionLock)
                    {
                        if (!tcs.Task.IsCompleted)
                        {
                            OnReceivedLine -= onReceivedLineHandler;
                            tcs.SetCanceled();
                        }
                    }
                });
            }

            return tcs.Task;
        }

        private async Task Run()
        {
            var buf = new char[8 * 1024];
            while (true)
            {
                var chunkLength = await _streamReader.ReadAsync(buf, 0, buf.Length);
                if (chunkLength == 0)
                {
                    break;
                }

                OnChunk(new ArraySegment<char>(buf, 0, chunkLength));

                var lineBreakPos = Array.IndexOf(buf, '\n', 0, chunkLength);
                if (lineBreakPos < 0)
                {
                    _linesBuffer.Append(buf, 0, chunkLength);
                }
                else
                {
                    _linesBuffer.Append(buf, 0, lineBreakPos + 1);
                    OnCompleteLine(_linesBuffer.ToString());
                    _linesBuffer.Clear();
                    _linesBuffer.Append(buf, lineBreakPos + 1, chunkLength - (lineBreakPos + 1));
                }
            }
        }

        private void OnChunk(ArraySegment<char> chunk)
        {
            var dlg = OnReceivedChunk;
            dlg?.Invoke(chunk);
        }

        private void OnCompleteLine(string line)
        {
            var dlg = OnReceivedLine;
            dlg?.Invoke(line);
        }
    }
}
