using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MessagePack;
using Microsoft.Extensions.DependencyInjection;
using ConduitNet.Core;
using System.Reflection;
using System.Linq;

namespace ConduitNet.Server
{
    public class ConduitDispatcher
    {
        private readonly IConduitMessageProcessor _processor;

        public ConduitDispatcher(IConduitMessageProcessor processor)
        {
            _processor = processor;
        }

        public async Task HandleConnectionAsync(WebSocket webSocket)
        {
            var pipe = new Pipe();
            var outgoingChannel = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

            var fillTask = FillPipeAsync(webSocket, pipe.Writer);
            var readTask = ReadPipeAsync(webSocket, pipe.Reader, outgoingChannel.Writer);
            var sendTask = SendLoopAsync(webSocket, outgoingChannel.Reader);

            await Task.WhenAll(fillTask, readTask, sendTask);
        }

        private async Task SendLoopAsync(WebSocket webSocket, ChannelReader<byte[]> reader)
        {
            try
            {
                while (await reader.WaitToReadAsync())
                {
                    while (reader.TryRead(out var messageBytes))
                    {
                        if (webSocket.State != WebSocketState.Open) break;

                        var lengthPrefix = BitConverter.GetBytes(messageBytes.Length);
                        await webSocket.SendAsync(lengthPrefix, WebSocketMessageType.Binary, false, CancellationToken.None);
                        await webSocket.SendAsync(messageBytes, WebSocketMessageType.Binary, true, CancellationToken.None);
                    }
                }
            }
            catch
            {
                // Ignore send errors
            }
        }

        private async Task FillPipeAsync(WebSocket webSocket, PipeWriter writer)
        {
            while (webSocket.State == WebSocketState.Open)
            {
                try
                {
                    Memory<byte> memory = writer.GetMemory(4096);
                    var result = await webSocket.ReceiveAsync(memory, CancellationToken.None);
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    writer.Advance(result.Count);
                    var flushResult = await writer.FlushAsync();
                    
                    if (flushResult.IsCompleted)
                        break;
                }
                catch
                {
                    break;
                }
            }
            await writer.CompleteAsync();
        }

        private async Task ReadPipeAsync(WebSocket webSocket, PipeReader reader, ChannelWriter<byte[]> writer)
        {
            while (true)
            {
                ReadResult result = await reader.ReadAsync();
                ReadOnlySequence<byte> buffer = result.Buffer;

                while (TryReadMessage(ref buffer, out ConduitMessage? message))
                {
                    if (message != null)
                    {
                        // Process in background to not block reading
                        _ = ProcessAndRespondAsync(message, writer);
                    }
                }

                reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                    break;
            }
            await reader.CompleteAsync();
        }

        private bool TryReadMessage(ref ReadOnlySequence<byte> buffer, out ConduitMessage? message)
        {
            if (buffer.Length < 4)
            {
                message = null;
                return false;
            }

            Span<byte> lengthBytes = stackalloc byte[4];
            buffer.Slice(0, 4).CopyTo(lengthBytes);
            int messageLength = BitConverter.ToInt32(lengthBytes);

            if (buffer.Length < 4 + messageLength)
            {
                message = null;
                return false;
            }

            // Zero-copy deserialization
            var messageSequence = buffer.Slice(4, messageLength);
            message = MessagePackSerializer.Deserialize<ConduitMessage>(messageSequence);
            
            buffer = buffer.Slice(4 + messageLength);
            return true;
        }

        private async Task ProcessAndRespondAsync(ConduitMessage request, ChannelWriter<byte[]> writer)
        {
            var response = await _processor.ProcessAsync(request);

            try
            {
                var messageBytes = MessagePackSerializer.Serialize(response);
                await writer.WriteAsync(messageBytes);
            }
            catch {}
        }
    }
}

