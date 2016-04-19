﻿#region Header
//   Vorspire    _,-'/-'/  Clilocs.cs
//   .      __,-; ,'( '/
//    \.    `-.__`-._`:_,-._       _ , . ``
//     `:-._,------' ` _,`--` -: `_ , ` ,' :
//        `---..__,,--'  (C) 2016  ` -'. -'
//        #  Vita-Nex [http://core.vita-nex.com]  #
//  {o)xxx|===============-   #   -===============|xxx(o}
//        #        The MIT License (MIT)          #
#endregion

#region References
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

using Server;
using Server.Commands;

using VitaNex.IO;
#endregion

namespace VitaNex
{
	public static partial class Clilocs
	{
		private static readonly List<string> _Languages = new List<string>
		{
			"ENU",
			"DEU",
			"ESP",
			"FRA",
			"JPN",
			"KOR",
			"CHT"
		};

		public static readonly Regex VarPattern = new Regex(
			@"(?<args>~(?<index>\d+)(?<sep>_*)(?<desc>\w*)~)",
			RegexOptions.IgnoreCase);

		public static ClilocLNG DefaultLanguage = ClilocLNG.ENU;

		public static CoreServiceOptions CSOptions { get; private set; }

		public static Dictionary<ClilocLNG, ClilocTable> Tables { get; private set; }

		private static void ExportCommand(CommandEventArgs e)
		{
			if (e.Mobile == null || e.Mobile.Deleted)
			{
				return;
			}

			e.Mobile.SendMessage(0x55, "Export requested...");

			if (e.Arguments == null || e.Arguments.Length == 0 || String.IsNullOrWhiteSpace(e.Arguments[0]))
			{
				e.Mobile.SendMessage("Usage: {0}{1} <{2}>", CommandSystem.Prefix, e.Command, String.Join(" | ", _Languages));
				return;
			}

			ClilocLNG lng;

			if (Enum.TryParse(e.Arguments[0], true, out lng) && lng != ClilocLNG.NULL)
			{
				VitaNexCore.TryCatch(
					() =>
					{
						var file = Export(lng);

						if (file != null && file.Exists && file.Length > 0)
						{
							e.Mobile.SendMessage(0x55, "{0} clilocs have been exported to: {1}", lng, file.FullName);
						}
						else
						{
							e.Mobile.SendMessage(0x22, "Could not export clilocs for {0}", lng);
						}
					},
					ex =>
					{
						e.Mobile.SendMessage(0x22, "A fatal exception occurred, check the console for details.");
						CSOptions.ToConsole(ex);
					});
			}
			else
			{
				e.Mobile.SendMessage("Usage: {0}{1} <{2}>", CommandSystem.Prefix, e.Command, String.Join(" | ", _Languages));
			}
		}

		public static FileInfo Export(ClilocLNG lng)
		{
			if (lng == ClilocLNG.NULL)
			{
				lng = DefaultLanguage;
			}

			var list = new XmlDataStore<int, ClilocData>(VitaNexCore.DataDirectory + "/Exported Clilocs/", lng.ToString());
			var table = Tables[lng];

			list.OnSerialize = doc =>
			{
				XmlNode node;
				XmlCDataSection cdata;
				ClilocInfo info;

				XmlNode root = doc.CreateElement("clilocs");

				var attr = doc.CreateAttribute("len");
				attr.Value = table.Count.ToString(CultureInfo.InvariantCulture);

				if (root.Attributes != null)
				{
					root.Attributes.Append(attr);
				}

				attr = doc.CreateAttribute("lng");
				attr.Value = table.Language.ToString();

				if (root.Attributes != null)
				{
					root.Attributes.Append(attr);
				}

				foreach (var d in table.Where(d => d.Length > 0))
				{
					info = d.Lookup(table.InputFile, true);

					if (info == null || String.IsNullOrWhiteSpace(info.Text))
					{
						continue;
					}

					node = doc.CreateElement("cliloc");

					attr = doc.CreateAttribute("idx");
					attr.Value = d.Index.ToString(CultureInfo.InvariantCulture);

					if (node.Attributes != null)
					{
						node.Attributes.Append(attr);
					}

					attr = doc.CreateAttribute("len");
					attr.Value = d.Length.ToString(CultureInfo.InvariantCulture);

					if (node.Attributes != null)
					{
						node.Attributes.Append(attr);
					}

					cdata = doc.CreateCDataSection(info.Text);
					node.AppendChild(cdata);

					root.AppendChild(node);
				}

				doc.AppendChild(root);
				table.Clear();

				return true;
			};

			list.Export();
			list.Clear();

			return list.Document;
		}

		public static ClilocInfo Lookup(this ClilocLNG lng, int index)
		{
			if (lng == ClilocLNG.NULL)
			{
				lng = DefaultLanguage;
			}

			if (Tables.ContainsKey(lng) && Tables[lng] != null)
			{
				return Tables[lng].Lookup(index);
			}

			if (lng != ClilocLNG.ENU)
			{
				return Lookup(ClilocLNG.ENU, index);
			}

			return null;
		}

		private static readonly Dictionary<Type, int> _TypeCache = new Dictionary<Type, int>(0x1000);

		private static readonly ObjectProperty _LabelNumberProp = new ObjectProperty("LabelNumber");

		public static ClilocInfo Lookup(this ClilocLNG lng, Type t)
		{
			if (lng == ClilocLNG.NULL)
			{
				lng = DefaultLanguage;
			}

			int index;

			if (_TypeCache.TryGetValue(t, out index))
			{
				return Lookup(lng, index);
			}

			return VitaNexCore.TryCatchGet(
				() =>
				{
					if (t == null)
					{
						return null;
					}

					if (!_LabelNumberProp.IsSupported(t, typeof(int)))
					{
						return null;
					}

					var o = t.CreateInstanceSafe<object>();

					if (o == null)
					{
						return null;
					}

					index = (int)_LabelNumberProp.GetValue(o, -1); // LabelNumber_get()

					var m = t.GetMethod("Delete");

					if (m != null)
					{
						m.Invoke(o, new object[0]); // Delete_call()
					}

					_TypeCache.Add(t, index);

					return Lookup(lng, index);
				});
		}

		public static string GetRawString(this ClilocLNG lng, int index)
		{
			if (lng == ClilocLNG.NULL)
			{
				lng = DefaultLanguage;
			}

			if (Tables.ContainsKey(lng) && Tables[lng] != null && !Tables[lng].IsNullOrWhiteSpace(index))
			{
				return Tables[lng][index].Text;
			}

			if (lng != ClilocLNG.ENU)
			{
				return GetRawString(ClilocLNG.ENU, index);
			}

			return String.Empty;
		}

		public static string GetString(this ClilocLNG lng, int index, string args)
		{
			if (lng == ClilocLNG.NULL)
			{
				lng = DefaultLanguage;
			}

			var info = Lookup(lng, index);

			return info == null ? String.Empty : info.ToString(args);
		}

		public static string GetString(this ClilocLNG lng, Type t)
		{
			if (lng == ClilocLNG.NULL)
			{
				lng = DefaultLanguage;
			}

			var info = Lookup(lng, t);

			return info == null ? String.Empty : info.ToString();
		}

		public static string GetString(this ClilocLNG lng, int index, params object[] args)
		{
			if (lng == ClilocLNG.NULL)
			{
				lng = DefaultLanguage;
			}

			var info = Lookup(lng, index);

			return info == null ? String.Empty : info.ToString(args);
		}

		public static string GetString(this TextDefinition text)
		{
			return GetString(text, DefaultLanguage);
		}

		public static string GetString(this TextDefinition text, Mobile m)
		{
			return GetString(text, GetLanguage(m));
		}

		public static string GetString(this TextDefinition text, ClilocLNG lng)
		{
			if (lng == ClilocLNG.NULL)
			{
				lng = DefaultLanguage;
			}

			return text.Number > 0 ? GetString(lng, text.Number) : (text.String ?? String.Empty);
		}

		public static bool IsNullOrEmpty(this TextDefinition text)
		{
			return text.Number <= 0 && String.IsNullOrEmpty(text.String) && String.IsNullOrEmpty(text.GetString());
		}

		public static bool IsNullOrWhiteSpace(this TextDefinition text)
		{
			return text.Number <= 0 && String.IsNullOrWhiteSpace(text.String) && String.IsNullOrWhiteSpace(text.GetString());
		}

		public static string[] DecodePropertyList(this ObjectPropertyList list)
		{
			return DecodePropertyList(list, DefaultLanguage);
		}

		public static string[] DecodePropertyList(this ObjectPropertyList list, Mobile m)
		{
			return DecodePropertyList(list, GetLanguage(m));
		}

		public static string[] DecodePropertyList(this ObjectPropertyList list, ClilocLNG lng)
		{
			if (lng == ClilocLNG.NULL)
			{
				lng = DefaultLanguage;
			}

			var msgs = new List<string>(10);

			try
			{
				var data = list.UnderlyingStream.UnderlyingStream.ToArray();

				var index = 15;

				while (index + 4 < data.Length)
				{
					var id = data[index++] << 24 | data[index++] << 16 | data[index++] << 8 | data[index++];

					if (index + 2 > data.Length)
					{
						break;
					}

					var paramLength = data[index++] << 8 | data[index++];

					var param = String.Empty;

					if (paramLength > 0)
					{
						var terminate = index + paramLength;

						if (terminate >= data.Length)
						{
							terminate = data.Length - 1;
						}

						while (index + 2 <= terminate + 1)
						{
							var peek = (short)(data[index++] | data[index++] << 8);

							if (peek == 0)
							{
								break;
							}

							param += Encoding.Unicode.GetString(BitConverter.GetBytes(peek));
						}
					}

					msgs.Add(GetString(lng, id, param) ?? String.Empty);
				}

				var join = String.Join("\n", msgs);

				msgs.Clear();
				msgs.AddRange(join.Split('\n'));
			}
			catch
			{ }

			var arr = msgs.ToArray();

			msgs.Free(true);

			return arr;
		}

		public static string DecodePropertyListHeader(this ObjectPropertyList list, ClilocLNG lng)
		{
			if (lng == ClilocLNG.NULL)
			{
				lng = DefaultLanguage;
			}

			var header = String.Empty;

			try
			{
				var data = list.UnderlyingStream.UnderlyingStream.ToArray();

				var index = 15;

				while (index + 4 < data.Length)
				{
					var id = data[index++] << 24 | data[index++] << 16 | data[index++] << 8 | data[index++];

					if (index + 2 > data.Length)
					{
						break;
					}

					var paramLength = data[index++] << 8 | data[index++];

					var param = String.Empty;

					if (paramLength > 0)
					{
						var terminate = index + paramLength;

						if (terminate >= data.Length)
						{
							terminate = data.Length - 1;
						}

						while (index + 2 <= terminate + 1)
						{
							var peek = (short)(data[index++] | data[index++] << 8);

							if (peek == 0)
							{
								break;
							}

							param += Encoding.Unicode.GetString(BitConverter.GetBytes(peek));
						}
					}

					header = GetString(lng, id, param) ?? String.Empty;
					break;
				}
			}
			catch
			{ }

			return header;
		}

		public static ClilocLNG GetLanguage(this Mobile m)
		{
			if (m == null)
			{
				return ClilocLNG.NULL;
			}

			ClilocLNG lng;

			return !Enum.TryParse(m.Language, out lng) ? ClilocLNG.ENU : lng;
		}
	}
}