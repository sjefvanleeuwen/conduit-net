using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MessagePack;
using ConduitNet.Rpc.Core;

namespace ConduitNet.Rpc.Client
{
    public class RpcTransport
    {
        private readonly ConcurrentDictionary<string, Connection> _connections = new();

        public async Task<RpcMessage> SendAsync(RpcMessage message)
        {
            if (!message.Headers.TryGetValue("Target-Url", out var url))
            {
                throw new InvalidOperationException("Target-Url header is missing. Ensure ServiceDiscoveryFilter is configured.");
            }

            var connection = _connections.GetOrAdd(url, u => new Connection(u));
            return await connection.SendAsync(message);
        }

        private class Connection
        {
            private readonly string _url;
            private ClientWebSocket? _webSocket;
            private readonly ConcurrentDictionary<string, TaskCompletionSource<RpcMessage>> _pendingCalls = new();
            private readonly Channel<RpcMessage> _outgoingChannel;
            private readonly SemaphoreSlim _connectLock = new(1, 1);
            private Task? _receiveTask;
            private Task? _sendTask;

            public Connection(string url)
            {
                _url = url;
                _outgoingChannel = Channel.CreateUnbounded<RpcMessage>(new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false
                });
            }

            public async Task<RpcMessage> SendAsync(RpcMessage message)
            {
                await EnsureConnectedAsync();

                var tcs = new TaskCompletionSource<RpcMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
                _pendingCalls[message.Id] = tcs;

                if (!_outgoingChannel.Writer.TryWrite(message))
                {
                    _pendingCalls.TryRemove(message.Id, out _);
                    throw new InvalidOperationException("Failed to queue message for sending.");
                }

                return await tcs.Task;
            }

            private async Task EnsureConnectedAsync()
            {
                if (_webSocket != null && _webSocket.State == WebSocketState.Open) return;

                await _connectLock.WaitAsync();
                try
                {
                    if (_webSocket != null && _webSocket.State == WebSocketState.Open) return;

                    _webSocket?.Dispose();
                    _webSocket = new ClientWebSocket();
                    await _webSocket.ConnectAsync(new Uri(_url), CancellationToken.None);

                    _receiveTask = Task.Run(ReceiveLoopAsync);
                    _sendTask = Task.Run(SendLoopAsync);
                }
                finally
                {
                    _connectLock.Release();
                }
            }

            private async Task SendLoopAsync()
            {
                var reader = _outgoingChannel.Reader;
                try
                {
                    while (await reader.WaitToReadAsync())
                    {
                        while (reader.TryRead(out var message))
                        {
                            if (_webSocket == null || _webSocket.State != WebSocketState.Open) break;

                            try
                            {
                                var messageBytes = MessagePackSerializer.Serialize(message);
                                var lengthPrefix = BitConverter.GetBytes(messageBytes.Length);
                                
                                await _webSocket.SendAsync(lengthPrefix, WebSocketMessageType.Binary, false, CancellationToken.None);
                                await _webSocket.SendAsync(messageBytes, WebSocketMessageType.Binary, true, CancellationToken.None);
                            }
                            catch
                            {
                                // If sending fails, we should probably fail the pending call
                                if (_pendingCalls.TryRemove(message.Id, out var tcs))
                                {
                                    tcs.TrySetException(new Exception("Failed to send message"));
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Log error
                }
            }

            private async Task ReceiveLoopAsync()
            {
                var pipe = new Pipe();
                var fillTask = FillPipeAsync(pipe.Writer);
                var readTask = ReadPipeAsync(pipe.Reader);
                await Task.WhenAll(fillTask, readTask);
            }

            private async Task FillPipeAsync(PipeWriter writer)
            {
                while (_webSocket != null && _webSocket.State == WebSocketState.Open)
                {
                    try
                    {
                        Memory<byte> memory = writer.GetMemory(4096);
                        var result = await _webSocket.ReceiveAsync(memory, CancellationToken.None);
                        
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

            private async Task ReadPipeAsync(PipeReader reader)
            {
                while (true)
                {
                    ReadResult result = await reader.ReadAsync();
                    ReadOnlySequence<byte> buffer = result.Buffer;

                    while (TryReadMessage(ref buffer, out RpcMessage? message))
                    {
                        if (message != null && _pendingCalls.TryRemove(message.Id, out var tcs))
                        {
                            tcs.SetResult(message);
                        }
                    }

                    reader.AdvanceTo(buffer.Start, buffer.End);

                    if (result.IsCompleted)
                        break;
                }
                await reader.CompleteAsync();
            }

            private bool TryReadMessage(ref ReadOnlySequence<byte> buffer, out RpcMessage? message)
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
                message = MessagePackSerializer.Deserialize<RpcMessage>(messageSequence);
                
                buffer = buffer.Slice(4 + messageLength);
                return true;
            }
        }
    }
}
