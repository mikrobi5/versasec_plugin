namespace CmsCoreBridge
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using PipeCommunication.Interfaces;
    using PipeCommunication.Models;

    using Serilog;

    using VSec.DotNet.CmsCore.Wrapper.Edge;
    using VSec.DotNet.CmsCore.Wrapper.Enums;
    using VSec.DotNet.CmsCore.Wrapper.Models;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="CmsCoreBridge.IIpcPipesProcessor" />
    public class InterprocessPipeProcessor : IIpcPipesProcessor
    {
        /// <summary>
        /// The output pipe
        /// </summary>
        private readonly INamedOutputPipeClient _outputPipe;

        /// <summary>
        /// The input pipe
        /// </summary>
        private readonly INamedInputPipeClient _inputPipe;

        /// <summary>
        /// The CMS core simples
        /// </summary>
        private readonly CmsCoreSimples _cmsCoreSimples;

        /// <summary>
        /// The read cancellation token
        /// </summary>
        private readonly CancellationTokenSource _readCancellationToken;

        /// <summary>
        /// The write cancellation token
        /// </summary>
        private readonly CancellationTokenSource _writeCancellationToken;

        /// <summary>
        /// The read task
        /// </summary>
        private Task _readTask;

        /// <summary>
        /// The disposed value
        /// </summary>
        private bool disposedValue;

        /// <summary>
        /// The cards
        /// </summary>
        private IList<SmartCard> _cards;

        /// <summary>
        /// The event message
        /// </summary>
        private static Queue<EventMessage> _eventMessages;

        /// <summary>
        /// The communication messages
        /// </summary>
        private Queue<string> _communicationMessages;
        private Task _processingTask;

        /// <summary>
        /// Occurs when [pipe message received].
        /// </summary>
        public event PipeMessageProcessing PipeMessageReceived;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="IPipeManager" /> is proxying.
        /// </summary>
        /// <value>
        ///   <c>true</c> if proxying; otherwise, <c>false</c>.
        /// </value>
        public bool Proxying { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InterprocessPipeProcessor"/> class.
        /// </summary>
        /// <param name="outputPipe">The output pipe.</param>
        /// <param name="inputPipe">The input pipe.</param>
        public InterprocessPipeProcessor(INamedOutputPipeClient outputPipe, INamedInputPipeClient inputPipe)
        {
            _eventMessages = new Queue<EventMessage>();
            //_communicationMessages = new Queue<string>();
            this._cmsCoreSimples = new CmsCoreSimples(Log.Logger);
            this._cmsCoreSimples.RaiseCardAddedEvent += _cmsCoreSimples_RaiseCardAddedEvent;
            this._cmsCoreSimples.RaiseCardRemovedEvent += _cmsCoreSimples_RaiseCardRemovedEvent;
            Log.Logger.Information($"InterprocessPipeManager card request starting");
            Task.Factory.StartNew(() =>
                {
                    this._cards = this._cmsCoreSimples?.GetCards();
                });
            Log.Logger.Information($"InterprocessPipeManager started");
            this._inputPipe = inputPipe;
            this._outputPipe = outputPipe;
            this._readCancellationToken = new CancellationTokenSource();
            this._writeCancellationToken = new CancellationTokenSource();
        }

        /// <summary>
        /// Handles the RaiseCardRemovedEvent event of the _cmsCoreSimples control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="a">The <see cref="VSec.DotNet.CmsCore.Wrapper.Models.CardEventArgs"/> instance containing the event data.</param>
        private static void _cmsCoreSimples_RaiseCardRemovedEvent(object sender, CardEventArgs a)
        {
            Log.Information($"++++++++++++  Event: {a.Message}");
            _eventMessages.Enqueue(new EventMessage(a.Message, Severity.Negative));// { Message = $"{a.Message}", Severity = "negative" });
        }

        /// <summary>
        /// Handles the RaiseCardAddedEvent event of the _cmsCoreSimples control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="a">The <see cref="VSec.DotNet.CmsCore.Wrapper.Models.CardEventArgs"/> instance containing the event data.</param>
        private static void _cmsCoreSimples_RaiseCardAddedEvent(object sender, CardEventArgs a)
        {
            Log.Information($"-----------  Event: {a.Message}");
            _eventMessages.Enqueue(new EventMessage(a.Message, Severity.Negative));// { Message = $"{a.Message}", Severity = "positive" });
        }

        /// <summary>
        /// Starts the process messaging.
        /// </summary>
        public void StartProcessMessaging()
        {
            try
            {
                //this._processingTask = new Task(this.ProcessingThread, this._readCancellationToken.Token);
                //_processingTask.Start();
                Log.Logger.Information("Waiting for connection on named pipe USS-Pipe-In");
                this._inputPipe.Connect();
                Log.Logger.Information("connection established");
                Log.Logger.Information("Waiting for connection on named pipe USS-Pipe-Out");
                this._outputPipe.Connect();
                Log.Logger.Information("connection established");
                this._readTask = new Task(this.ReadMessageFromPipe, this._readCancellationToken.Token);
                this._readTask.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Stops the process messaging.
        /// </summary>
        public void StopProcessMessaging()
        {
            this._readCancellationToken.Cancel();
            this._readTask.Dispose();
        }

        /// <summary>
        /// Writes the message to pipe - connected to the cms core bridge
        /// </summary>
        /// <param name="message">The message.</param>
        public async void WriteMessageToPipe(string message)
        {
            try
            {
                Log.Logger.Information($"++++ write message {message}");
                await this._outputPipe.WriteMessage(message).ConfigureAwait(false);
                Log.Logger.Information("---- write message");
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "write message");
            }
        }

        /// <summary>
        /// Reads the message from the pipe - connected to the cms core bridge
        /// </summary>
        public async void ReadMessageFromPipe()
        {
            while (true)
            {
                Log.Logger.Information("try to read from stream");
                var message = this._inputPipe.ReadMessage().Result;
                Log.Logger.Information($"Readmessage {message}");
                this.OnMessageReceived(message);
                Task.Delay(1).Wait();
            }
        }

        private void ProcessingThread()
        {
            while (true)
            {
                if (_communicationMessages.Count > 0)
                {
                    var message = _communicationMessages.Dequeue();
                    CheckReceivedMessage(message);
                }

                Task.Delay(100).Wait();
            }

        }

        /// <summary>
        /// Checks the received message.
        /// </summary>
        /// <param name="msg">The MSG.</param>
        /// <returns></returns>
        public bool CheckReceivedMessage(string msg)
        {
            try
            {
                var jsonMessage = JsonConvert.DeserializeObject<JObject>(msg);
                var messageId = int.Parse(jsonMessage["id"].ToString());
                var cardCommand = jsonMessage["message"]["Command"].ToString();

                Log.Logger.Information("message parsed");
                Log.Logger.Information(cardCommand);
                //return true;
                string outputStream = string.Empty;

                if (cardCommand.Equals("PING"))
                {
                    Log.Logger.Information("PING received");
                    return true;
                }
                if (cardCommand.Equals("Events"))
                {
                    Log.Logger.Information("Event request received");
                    try
                    {
                        var eventMessage = _eventMessages.Count > 0 ? _eventMessages.Dequeue() : null;
                        if (eventMessage != null)
                        {
                            Log.Logger.Information("event message deQueued ");
                            outputStream =
                                $"{{ \"id\" : {messageId} , \"data\" : {{ \"Events\" : \"{eventMessage.Message}\", \"Severity\" : \"{eventMessage.Severity}\" }}}}";
                        }
                        else
                        {
                            Log.Logger.Information("event message are empty ");
                            outputStream = $"{{ \"id\" : {messageId} , \"data\" : {{ \"Events\" : \"\", \"Severity\" : \"\" }}}}";
                        }
                        this.WriteMessageToPipe(outputStream);

                        return true;
                    }
                    catch (Exception ex)
                    {
                        Log.Fatal(ex, "Events: ");
                    }

                }
                var values = jsonMessage["message"]["Values"];
                var csn = values?["CardCsn"].ToString();
                Log.Logger.Information("try to get card");
                var foundCard = this._cards.FirstOrDefault(x => x.Csn == csn);

                if (foundCard == null)
                {
                    Log.Logger.Information("no card found");
                    if (cardCommand.Equals("GCRS")) //Get card Readers
                    {
                        Log.Logger.Information("Try to get Readers");
                        if (this._cards != null && this._cards.Count > 0)
                        {
                            outputStream = $"{{ \"id\" : {messageId}, \"data\" : {{ \"GCRS\" : {JsonConvert.SerializeObject(this._cards.Select(x => new { x.ReaderName, x.Csn, x.Index }))} }}}}";
                        }
                        else
                        {
                            outputStream = $"{{ \"id\" : {messageId}, \"data\" : {{ \"GCRS\" : \"N.A.\" }}}}";
                        }
                        Log.Logger.Information("returned string");
                        this.WriteMessageToPipe(outputStream);
                        return true;
                    }
                    outputStream = $"{{ \"id\" : {messageId}, \"data\" : {{ \"CSN\" : \"N.A.\" }}}}";
                    this.WriteMessageToPipe(outputStream);
                }
                //RCCI - Read Current Card CSN
                if (cardCommand.Equals("RCCC"))
                {
                    if (this._cards != null && this._cards.Count > 0)
                    {

                        outputStream = $"{{ \"id\" : {messageId}, \"data\" : {{ \"CSN\" : \"{foundCard?.Csn}\" }}}}";
                    }
                    else
                    {
                        outputStream = $"{{ \"id\" : {messageId}, \"data\" : {{ \"CSN\" : \"N.A.\" }}}}";
                    }
                    this.WriteMessageToPipe(outputStream);
                }

                if (cardCommand.Equals("RCCRC"))
                {
                    if (this._cards != null && this._cards.Count > 0)
                    {
                        outputStream = $"{{ \"id\" : {messageId.ToString()}, \"data\" : {{ \"RCCRC\" : \"{foundCard.AdminTries.ToString()}\" }}}}";
                    }
                    else
                    {
                        outputStream = $"{{ \"id\" : {messageId.ToString()}, \"data\" : {{ \"RCCRC\" : \"N.A.\" }}}}";
                    }
                    this.WriteMessageToPipe(outputStream);
                }

                if (cardCommand.Equals("LCC"))
                {
                    if (this._cards != null && this._cards.Count > 0)
                    {
                        foundCard.UserRole = Roles.User;
                        foundCard.Pin = values?["PinOne"].ToString();
                        outputStream = $"{{ \"id\" : {messageId}, \"data\" : {{ \"LCC\" : \"{foundCard.LoginUser().ToString()}\" }}}}";
                    }
                    else
                    {
                        outputStream = $"{{ \"id\" : {messageId}, \"data\" : {{ \"LCC\" : \"N.A.\" }}}}";
                    }
                    this.WriteMessageToPipe(outputStream);
                }

                if (cardCommand.Equals("RPCC")) //Reset pin on current card
                {
                    // if (this._cards != null && this._cards.Count > 0)
                    //  {
                    try
                    {

                        foundCard.UserRole = Roles.User;
                        foundCard.NewPin = values?["PinOne"].ToString();
                        Log.Information($"RPCC try to unblock {foundCard.Csn} {foundCard.NewPin}");
                        // foundCard.UnblockUserPin();
                        Log.Information($"RPCC unblocked {foundCard.Csn} {foundCard.NewPin}");
                        foundCard.Pin = foundCard.NewPin;
                        Log.Information($"try to login {foundCard.Csn} {foundCard.NewPin}");
                        var loggedIn = foundCard.LoginUser();
                        Log.Information($"logged in {foundCard.Csn} {foundCard.NewPin}");
                        outputStream =
                          $"{{ \"id\" : {messageId}, \"data\" : {{ \"RPCC\" : \"{loggedIn.ToString()}\" }}}}";
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "RPCC");
                    }
                    //}
                    //else
                    //{
                    //    outputStream = $"{{ \"id\" : {messageId}, \"data\" : {{ \"RPCC\" : \"N.A.\" }}}}";
                    //}
                    this.WriteMessageToPipe(outputStream);
                }

                if (cardCommand.Equals("SNPCC")) //Set new pin
                {
                    if (this._cards != null && this._cards.Count > 0)
                    {
                        var card = this._cards.FirstOrDefault();
                        foundCard.NewPin = values["PinTwo"].ToString();
                        foundCard.Pin = values["PinOne"].ToString();
                        foundCard.UserRole = Roles.User;
                        foundCard.ChangeUserPin();
                        foundCard.Pin = foundCard.NewPin;
                        outputStream = $"{{ \"id\" : {messageId}, \"data\" : {{ \"SNPCC\" : \"{foundCard.LoginUser()}\" }}}}";
                    }
                    else
                    {
                        outputStream = $"{{ \"id\" : {messageId}, \"data\" : {{ \"SNPCC\" : \"N.A.\" }}}}";
                    }
                    this.WriteMessageToPipe(outputStream);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Called when [pipe message received].
        /// </summary>
        /// <param name="message">The message.</param>
        private void OnMessageReceived(string message)
        {
            //_communicationMessages.Enqueue(message);
            CheckReceivedMessage(message);
            if (!string.IsNullOrWhiteSpace(message))
                this.PipeMessageReceived?.Invoke(message);
        }

        /// <summary>
        /// Disposes the specified disposing.
        /// </summary>
        /// <param name="disposing">if set to <c>true</c> [disposing].</param>
        /// <returns></returns>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    // TODO: Verwalteten Zustand (verwaltete Objekte) bereinigen
                }
                this.StopProcessMessaging();
                // TODO: Nicht verwaltete Ressourcen (nicht verwaltete Objekte) freigeben und Finalizer überschreiben
                // TODO: Große Felder auf NULL setzen
                this.disposedValue = true;
            }
        }

        // // TODO: Finalizer nur überschreiben, wenn "Dispose(bool disposing)" Code für die Freigabe nicht verwalteter Ressourcen enthält
        // ~InterprocessPipeProcessor()
        // {
        //     // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
        //     Dispose(disposing: false);
        // }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <returns></returns>
        public void Dispose()
        {
            // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void StopReadTask()
        {
            throw new NotImplementedException();
        }
    }
}
