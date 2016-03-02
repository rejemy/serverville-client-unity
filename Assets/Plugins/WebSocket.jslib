var LibraryServervilleWebSockets =
{

	Serverville_SocketCreate: function(id, urlPtr)
	{
		if(window._Serverville_Sockets == null)
			window._Serverville_Sockets = {};

		var url = Pointer_stringify(urlPtr);
		var socket = new WebSocket(url);

		window._Serverville_Sockets[id] = socket;

		var targetId = "ServervilleWSHandler"+id;

		socket.onopen = function(evt)
		{
			SendMessage (targetId, "OnWebsocketOpen", evt.type);
		};

		socket.onmessage = function(evt)
		{
			SendMessage (targetId, "OnWebsocketStringMessage", evt.data);
		};

		socket.onerror = function(evt)
		{
			SendMessage (targetId, "OnWebsocketError", evt.message);
		};

		socket.onclose = function(evt)
		{
			SendMessage (targetId, "OnWebsocketClose", evt.code);
		};
		
	},

	Serverville_SocketConnect: function(id)
	{
		var socket = window._Serverville_Sockets[id];
		return socket.connect();
	},

	Serverville_SocketReadyState: function(id)
	{
		var socket = window._Serverville_Sockets[id];
		return socket.readyState;
	},

	Serverville_SocketURL: function(id)
	{
		var socket = window._Serverville_Sockets[id];
		return socket.url;
	},

	Serverville_SocketProtocol: function(id)
	{
		var socket = window._Serverville_Sockets[id];
		return socket.protocol;
	},

	Serverville_SocketBinaryType: function(id)
	{
		var socket = window._Serverville_Sockets[id];
		return socket.binaryType;
	},

	Serverville_SocketExtensions: function(id)
	{
		var socket = window._Serverville_Sockets[id];
		return socket.extensions;
	},

	Serverville_SocketBufferedAmount: function(id)
	{
		var socket = window._Serverville_Sockets[id];
		return socket.bufferedAmount;
	},

	Serverville_SocketSendString: function(id, strPtr)
	{
		var socket = window._Serverville_Sockets[id];
		var str = Pointer_stringify(strPtr);
		socket.send(str);
	},

	Serverville_SocketSendBinary: function(id, ptr, length)
	{
		var socket = window._Serverville_Sockets[id];
		socket.send (HEAPU8.buffer.slice(ptr, ptr+length));
	},

	Serverville_SocketClose: function(id)
	{
		var socket = window._Serverville_Sockets[id];
		socket.close();
	},

	Serverville_SocketDispose: function(id)
	{
		var socket = window._Serverville_Sockets[id];
		socket.onopen = null;
		socket.onmessage = null;
		socket.onerror = null;
		socket.onclose = null;
		delete window._Serverville_Sockets[id];
	}
};

mergeInto(LibraryManager.library, LibraryServervilleWebSockets);
