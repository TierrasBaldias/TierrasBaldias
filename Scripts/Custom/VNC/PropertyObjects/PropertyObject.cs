﻿#region Header
//   Vorspire    _,-'/-'/  PropertyObject.cs
//   .      __,-; ,'( '/
//    \.    `-.__`-._`:_,-._       _ , . ``
//     `:-._,------' ` _,`--` -: `_ , ` ,' :
//        `---..__,,--'  (C) 2016  ` -'. -'
//        #  Vita-Nex [http://core.vita-nex.com]  #
//  {o)xxx|===============-   #   -===============|xxx(o}
//        #        The MIT License (MIT)          #
#endregion

#region References
using Server;
#endregion

namespace VitaNex
{
	[PropertyObject]
	public abstract class PropertyObject
	{
		[CommandProperty(AccessLevel.Administrator)]
		public virtual bool InvokeClear
		{
			get { return true; }
			set
			{
				if (value)
				{
					Clear();
				}
			}
		}

		[CommandProperty(AccessLevel.Administrator)]
		public virtual bool InvokeReset
		{
			get { return true; }
			set
			{
				if (value)
				{
					Reset();
				}
			}
		}

		public PropertyObject()
		{ }

		public PropertyObject(GenericReader reader)
			: this(reader, false)
		{ }

		public PropertyObject(GenericReader reader, bool deferred)
			: this()
		{
			if (deferred)
			{
				Timer.DelayCall(Deserialize, reader);
			}
			else
			{
				Deserialize(reader);
			}
		}

		public abstract void Clear();
		public abstract void Reset();

		public virtual void Serialize(GenericWriter writer)
		{
			writer.SetVersion(0);
		}

		public virtual void Deserialize(GenericReader reader)
		{
			reader.GetVersion();
		}

		public override string ToString()
		{
			return "...";
		}
	}
}