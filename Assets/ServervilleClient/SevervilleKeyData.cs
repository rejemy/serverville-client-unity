using System;
using System.Collections.Generic;

namespace Serverville
{
	public class KeyData
	{
		private string Id;
		private KeyDataInfo RecordInfo;
		private Dictionary<string, DataItemReply> Keys;
		private Dictionary<string, DataItemReply> DirtyKeys;
		private double MostRecent = 0;
		private bool Dirty = false;

		private ServervilleClient Server;

		public static void Find(ServervilleClient server, string id, Action<KeyData> onDone)
		{
			if(server == null)
				throw new Exception("Must supply a server");
			
			if(id == null)
			{
				if(server.GetUserInfo() == null)
					throw new Exception("Server not yet logged in");
				id = server.GetUserInfo().user_id;
			}
				
			server.GetKeyDataRecord(id, delegate(KeyDataInfo info) {
				KeyData inst = new KeyData(server, info);

				if(onDone != null)
					onDone(inst);
			},
			delegate(ErrorReply reply) {
				if(onDone != null)
					onDone(null);
			});
		}

		public static void Load(ServervilleClient server, string id, Action<KeyData> onDone)
		{
			Find(server, id, delegate(KeyData obj)
				{
					if(obj == null)
					{
						onDone(null);
						return;
					}
					obj.LoadAll( delegate(bool success)
						{
							onDone(obj);
						}
					);
				}
			);
		}

		private KeyData(ServervilleClient server, KeyDataInfo info)
		{
			Server = server;
			Id = info.id;
			RecordInfo = info;
			Keys = new Dictionary<string, DataItemReply>();
			DirtyKeys = new Dictionary<string, DataItemReply>();
		}

		public string GetId() { return Id; }
		public string GetDataType() { return RecordInfo.record_type; }
		public int GetVersion() { return (int)RecordInfo.version; }

		public string GetOwnerId() { return RecordInfo.owner; }
		public string GetParentId() { return RecordInfo.parent; }

		public void LoadAll(Action<bool> onDone)
		{
			Server.GetAllDataKeys(Id, 0, false,
				delegate(UserDataReply reply)
				{
					Keys.Clear();
					DirtyKeys.Clear();

					Dirty = false;

					foreach(DataItemReply item in reply.values.Values)
					{
						Keys[item.key] = item;
						if(item.modified > MostRecent)
							MostRecent = item.modified;
					}

					onDone(true);
				},
				delegate(ErrorReply err)
				{
					onDone(false);
				}
			);
		}


		public void Refresh(Action<bool> onDone)
		{
			Server.GetAllDataKeys(Id, MostRecent, true,
				delegate(UserDataReply reply)
				{
					foreach(DataItemReply item in reply.values.Values)
					{
						if(item.deleted)
						{
							Keys.Remove(item.key);
						}
						else
						{
							Keys[item.key] = item;
						}
						if(item.modified > MostRecent)
							MostRecent = item.modified;
					}

					onDone(true);
				},
				delegate(ErrorReply err)
				{
					onDone(false);
				}
			);
		}

		public void Save(Action<bool> onDone)
		{
			if(Server.GetUserInfo() == null || Server.GetUserInfo().user_id != RecordInfo.owner)
				throw new Exception("Read-only data!");

			if(!Dirty)
				return;

			List<SetUserDataRequest> saveSet = new List<SetUserDataRequest>();

			foreach(DataItemReply item in DirtyKeys.Values)
			{
				saveSet.Add(new SetUserDataRequest
					{
						key=item.key,
						value=item.value,
						data_type=item.data_type
					}
				);
			}

			Server.SetDataKeys(Id, saveSet,
				delegate(SetDataReply obj)
				{
					DirtyKeys.Clear();
					Dirty = false;

					onDone(true);
				},
				delegate(ErrorReply reply)
				{
					onDone(false);
				}
			);
		}

		public void Set(string key, object value)
		{
			if(Server.GetUserInfo() == null || Server.GetUserInfo().user_id != RecordInfo.owner)
				throw new Exception("Read-only data!");

			DataItemReply item = null;
			Keys.TryGetValue(key, out item);
			if(item != null)
			{
				if(item.value == value)
					return;

				item.value = value;
			}
			else
			{
				item = new DataItemReply();
				item.id = Id;
				item.key = key;
				item.value = value;
				item.created = 0;
				item.modified = 0;
				item.deleted = false;

				Keys.Add(item.key, item);
			}

			DirtyKeys[item.key] = item;
			Dirty = true;

		}

		public object Get(string key)
		{
			DataItemReply item = null;
			Keys.TryGetValue(key, out item);
			if(item != null)
				return item.value;
			return null;
		}
	}
}

