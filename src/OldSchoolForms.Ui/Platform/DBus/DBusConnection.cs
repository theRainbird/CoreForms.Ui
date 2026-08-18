using static OldSchoolForms.Ui.Platform.DBus.DBusNative;

namespace OldSchoolForms.Ui.Platform.DBus;

/// <summary>
/// Managed D-Bus session bus connection using libdbus-1 P/Invoke.
/// </summary>
internal class DBusConnection : IDisposable
{
    private IntPtr _connection;
    private bool _disposed;

    /// <summary>
    /// Gets the native libdbus connection handle.
    /// </summary>
    public IntPtr Handle => _connection;

    private DBusConnection(IntPtr connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// Opens a connection to the session bus.
    /// </summary>
    public static DBusConnection Connect()
    {
        var error = new DBusError();
        dbus_error_init(out error);

        IntPtr conn = dbus_bus_get(DBusBusType.Session, ref error);
        if (conn == IntPtr.Zero)
        {
            string msg = $"Failed to connect to D-Bus session bus: {PtrToString(error.message)}";
            dbus_error_free(ref error);
            throw new InvalidOperationException(msg);
        }
        dbus_error_free(ref error);

        return new DBusConnection(conn);
    }

    /// <summary>
    /// Sends a method call and waits for the reply while pumping UI events.
    /// </summary>
    public DBusMessage SendAndWaitForReply(DBusMessage request, int timeoutMs = 10000, Action? pumpEvents = null)
    {
        IntPtr msg = request.Handle;
        if (msg == IntPtr.Zero)
            throw new InvalidOperationException("D-Bus message has no native handle");

        if (request.BodyBuilder != null)
        {
            var appendIter = new DBusMessageIter();
            dbus_message_iter_init_append(msg, ref appendIter);
            request.BodyBuilder(ref appendIter);
        }

        if (!dbus_connection_send_with_reply(_connection, msg, out IntPtr pending, timeoutMs))
            throw new InvalidOperationException("Failed to send D-Bus message");
        dbus_connection_flush(_connection);

        if (pending == IntPtr.Zero)
            throw new InvalidOperationException("D-Bus send returned null pending call");

        try
        {
            int elapsed = 0;
            while (!dbus_pending_call_get_completed(pending))
            {
                dbus_connection_read_write_dispatch(_connection, 50);
                pumpEvents?.Invoke();

                if (elapsed >= timeoutMs)
                {
                    dbus_connection_read_write_dispatch(_connection, 0);
                    if (!dbus_pending_call_get_completed(pending))
                        throw new TimeoutException("D-Bus response timed out.");
                    break;
                }

                Thread.Sleep(5);
                elapsed += 5;
            }

            IntPtr reply = dbus_pending_call_steal_reply(pending);
            if (reply == IntPtr.Zero)
                throw new InvalidOperationException("D-Bus call returned no reply");

            var result = new DBusMessage(reply);
            result.ParseBody();
            return result;
        }
        finally
        {
            dbus_pending_call_unref(pending);
        }
    }

    /// <summary>
    /// Adds a D-Bus match rule to subscribe to signals.
    /// </summary>
    public void AddMatch(string rule)
    {
        var error = new DBusError();
        dbus_error_init(out error);

        int result = dbus_bus_add_match(_connection, rule, ref error);
        dbus_error_free(ref error);
    }

    /// <summary>
    /// Waits for a signal matching the given interface/member while pumping UI events.
    /// </summary>
    public DBusMessage? WaitForSignal(string interfaceName, string memberName, int timeoutMs = 60000, Action? pumpEvents = null)
    {
        int elapsed = 0;
        while (elapsed < timeoutMs)
        {
            dbus_connection_read_write_dispatch(_connection, 50);
            pumpEvents?.Invoke();

            IntPtr msgPtr = dbus_connection_pop_message(_connection);
            while (msgPtr != IntPtr.Zero)
            {
                var msg = new DBusMessage(msgPtr);
                if (msg.Type == DBusMessageType.Signal &&
                    msg.Interface == interfaceName &&
                    msg.Member == memberName)
                {
                    msg.ParseBody();
                    return msg;
                }
                msg.Dispose();
                dbus_message_unref(msgPtr);
                msgPtr = dbus_connection_pop_message(_connection);
            }

            Thread.Sleep(10);
            elapsed += 10;
        }

        return null;
    }

    public void Dispose()
    {
        if (!_disposed && _connection != IntPtr.Zero)
        {
            dbus_connection_unref(_connection);
            _connection = IntPtr.Zero;
            _disposed = true;
        }
    }
}

/// <summary>
/// Represents a D-Bus message with parsed body.
/// </summary>
internal class DBusMessage
{
    private readonly IntPtr _nativePtr;

    public DBusMessage(IntPtr nativePtr)
    {
        _nativePtr = nativePtr;
        dbus_message_ref(nativePtr);

        Type = dbus_message_get_type(nativePtr);
        Path = DBusNative.PtrToString(dbus_message_get_path(nativePtr));
        Interface = DBusNative.PtrToString(dbus_message_get_interface(nativePtr));
        Member = DBusNative.PtrToString(dbus_message_get_member(nativePtr));
    }

    public IntPtr Handle => _nativePtr;
    public DBusMessageType Type { get; }
    public string? Path { get; }
    public string? Interface { get; }
    public string? Member { get; }
    public string? Destination { get; set; }

    public delegate void BodyBuilderDelegate(ref DBusMessageIter iter);
    public BodyBuilderDelegate? BodyBuilder { get; set; }
    public object?[]? BodyValues { get; set; }

    ~DBusMessage()
    {
        if (_nativePtr != IntPtr.Zero)
            dbus_message_unref(_nativePtr);
    }

    public void Dispose()
    {
        if (_nativePtr != IntPtr.Zero)
        {
            dbus_message_unref(_nativePtr);
            GC.SuppressFinalize(this);
        }
    }

    public void ParseBody()
    {
        var values = new List<object?>();
        var iter = new DBusMessageIter();
        dbus_message_iter_init(_nativePtr, ref iter);
        ParseIter(ref iter, values);
        BodyValues = values.ToArray();
    }

    private static void ParseIter(ref DBusMessageIter iter, List<object?> values)
    {
        int argType;
        while ((argType = dbus_message_iter_get_arg_type(ref iter)) != DBUS_TYPE_INVALID)
        {
            if (argType == DBUS_TYPE_STRING || argType == DBUS_TYPE_OBJECT_PATH)
            {
                dbus_message_iter_get_basic(ref iter, out IntPtr strPtr);
                values.Add(PtrToString(strPtr));
            }
            else if (argType == DBUS_TYPE_UINT32)
            {
                dbus_message_iter_get_basic(ref iter, out uint uVal);
                values.Add(uVal);
            }
            else if (argType == DBUS_TYPE_BOOLEAN)
            {
                dbus_message_iter_get_basic(ref iter, out uint bVal);
                values.Add(bVal != 0);
            }
            else if (argType == DBUS_TYPE_ARRAY)
            {
                int elemType = dbus_message_iter_get_element_type(ref iter);
                dbus_message_iter_recurse(ref iter, out DBusMessageIter subIter);

                if (elemType == DBUS_TYPE_STRING)
                {
                    var strings = new List<string>();
                    while (dbus_message_iter_get_arg_type(ref subIter) != DBUS_TYPE_INVALID)
                    {
                        dbus_message_iter_get_basic(ref subIter, out IntPtr sPtr);
                        strings.Add(PtrToString(sPtr) ?? "");
                        dbus_message_iter_next(ref subIter);
                    }
                    values.Add(strings.ToArray());
                }
                else if (elemType == DBUS_TYPE_DICT_ENTRY)
                {
                    var dict = new Dictionary<string, object?>();
                    while (dbus_message_iter_get_arg_type(ref subIter) != DBUS_TYPE_INVALID)
                    {
                        dbus_message_iter_recurse(ref subIter, out DBusMessageIter entryIter);
                        dbus_message_iter_get_basic(ref entryIter, out IntPtr keyPtr);
                        string key = PtrToString(keyPtr) ?? "";
                        dbus_message_iter_next(ref entryIter);

                        int valType = dbus_message_iter_get_arg_type(ref entryIter);
                        if (valType == DBUS_TYPE_VARIANT)
                        {
                            dbus_message_iter_recurse(ref entryIter, out DBusMessageIter varIter);
                            var subVals = new List<object?>();
                            ParseIter(ref varIter, subVals);
                            dict[key] = subVals.Count == 1 ? subVals[0] : subVals.ToArray();
                        }
                        else
                        {
                            var subVals = new List<object?>();
                            ParseIter(ref entryIter, subVals);
                            dict[key] = subVals.Count == 1 ? subVals[0] : subVals.ToArray();
                        }

                        dbus_message_iter_next(ref subIter);
                    }
                    values.Add(dict);
                }
                else
                {
                    dbus_message_iter_recurse(ref iter, out DBusMessageIter skipIter);
                    var skipVals = new List<object?>();
                    ParseIter(ref skipIter, skipVals);
                    values.Add(skipVals.ToArray());
                }
            }
            else if (argType == DBUS_TYPE_VARIANT)
            {
                dbus_message_iter_recurse(ref iter, out DBusMessageIter varIter);
                var varVals = new List<object?>();
                ParseIter(ref varIter, varVals);
                values.Add(varVals.Count == 1 ? varVals[0] : varVals.ToArray());
            }

            dbus_message_iter_next(ref iter);
        }
    }
}
