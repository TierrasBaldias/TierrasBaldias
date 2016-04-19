#region Header
//   Vorspire    _,-'/-'/  GumpLine.cs
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
using System.Drawing;
using System.Linq;

using Server;
using Server.Gumps;
using Server.Network;
#endregion

namespace VitaNex.SuperGumps
{
	public class GumpLine : SuperGumpEntry, IGumpEntryVector
	{
		private const string _Format1 = @"{{ htmlgump {0} {1} {2} {3} {4} 0 0 }}";

		private static readonly byte[] _Layout1A = Gump.StringToBuffer("htmlgump");
		private static readonly byte[] _Layout1B = Gump.StringToBuffer(" }{ htmlgump");

		private static int GetLength(int x1, int y1, int x2, int y2)
		{
			return (int)Line2D.GetLength(new Point2D(x1, y1), new Point2D(x2, y2));
		}

		private int _X1, _Y1;
		private int _X2, _Y2;

		private int _Size;
		private Color _Color;

		int IGumpEntryPoint.X { get { return StartX; } set { StartX = value; } }
		int IGumpEntryPoint.Y { get { return StartY; } set { StartY = value; } }

		public int StartX { get { return _X1; } set { Delta(ref _X1, value); } }
		public int StartY { get { return _Y1; } set { Delta(ref _Y1, value); } }

		public int EndX { get { return _X2; } set { Delta(ref _X2, value); } }
		public int EndY { get { return _Y2; } set { Delta(ref _Y2, value); } }

		public int Width
		{
			get { return Math.Abs(_X2 - _X1); }
			set
			{
				if (_X2 >= _X1)
				{
					Delta(ref _X2, _X2 + (value - Width));
				}
				else
				{
					Delta(ref _X1, _X1 + (value - Width));
				}

				Delta(ref _Rotation, Angle.FromPoints(_X1, _Y1, _X2, _Y2));
				Delta(ref _Length, GetLength(_X1, _Y1, _X2, _Y2));
			}
		}

		public int Height
		{
			get { return Math.Abs(_Y2 - _Y1); }
			set
			{
				if (_Y2 >= _Y1)
				{
					Delta(ref _Y2, _Y2 + (value - Height));
				}
				else
				{
					Delta(ref _Y1, _Y1 + (value - Height));
				}

				Delta(ref _Rotation, Angle.FromPoints(_X1, _Y1, _X2, _Y2));
				Delta(ref _Length, GetLength(_X1, _Y1, _X2, _Y2));
			}
		}

		private Angle _Rotation;

		public Angle Rotation
		{
			get { return _Rotation; }
			set
			{
				if (value == _Rotation)
				{
					return;
				}

				Delta(ref _Rotation, value);

				int x = _X1, y = _Y1;

				_Rotation.Transform(ref x, ref y, _Length);

				Delta(ref _X2, x);
				Delta(ref _Y2, y);
			}
		}

		private int _Length;

		public int Length
		{
			get { return _Length; }
			set
			{
				if (_Length == value)
				{
					return;
				}

				Delta(ref _Length, value);

				int x = _X1, y = _Y1;

				_Rotation.Transform(ref x, ref y, _Length);

				Delta(ref _X2, x);
				Delta(ref _Y2, y);
			}
		}

		public Color Color { get { return _Color; } set { Delta(ref _Color, value); } }
		public int Size { get { return _Size; } set { Delta(ref _Size, value); } }

		public GumpLine(IPoint2D start, IPoint2D end, Color color)
			: this(start, end, color, 1)
		{ }

		public GumpLine(IPoint2D start, IPoint2D end, Color color, int size)
		{
			_X1 = start.X;
			_Y1 = start.Y;
			_X2 = end.X;
			_Y2 = end.Y;

			_Rotation = Angle.FromPoints(_X1, _Y1, _X2, _Y2);
			_Length = GetLength(_X1, _Y1, _X2, _Y2);

			_Color = color;
			_Size = size;
		}

		public GumpLine(IPoint2D start, Angle angle, int length, Color color)
			: this(start, angle, length, color, 1)
		{ }

		public GumpLine(IPoint2D start, Angle angle, int length, Color color, int size)
			: this(start.X, start.Y, angle, length, color, size)
		{ }

		public GumpLine(int x, int y, Angle angle, int length, Color color)
			: this(x, y, angle, length, color, 1)
		{ }

		public GumpLine(int x, int y, Angle angle, int length, Color color, int size)
		{
			_Rotation = angle;
			_Length = length;

			_X1 = x;
			_Y1 = y;

			_Rotation.Transform(ref x, ref y, _Length);

			_X2 = x;
			_Y2 = y;

			_Color = color;
			_Size = size;
		}

		public override string Compile()
		{
			if (_Size <= 0 || _Color.IsEmpty || _Color == Color.Transparent)
			{
				return Compile(_X1, _Y1, 1, Color.Transparent);
			}

			var line = new Point2D(_X1, _Y1).PlotLine2D(new Point2D(_X2, _Y2));

			var compiled = line.Aggregate(String.Empty, (c, p) => c + Compile(p.X, p.Y, _Size, _Color));

			if (String.IsNullOrWhiteSpace(compiled))
			{
				compiled = Compile(_X1, _Y1, 1, Color.Transparent);
			}

			return compiled;
		}

		public virtual string Compile(int x, int y, int s, Color color)
		{
			return String.Format(_Format1, x - (s / 2), y - (s / 2), s, s, Parent.Intern(" ".WrapUOHtmlBG(color)));
		}

		public override void AppendTo(IGumpWriter disp)
		{
			var first = true;

			if (_Size <= 0 || _Color.IsEmpty || _Color == Color.Transparent)
			{
				AppendTo(disp, ref first, _X1, _Y1, 1, Color.Transparent);
				return;
			}

			var line = new Point2D(_X1, _Y1).PlotLine2D(new Point2D(_X2, _Y2));

			foreach (var p in line)
			{
				AppendTo(disp, ref first, p.X, p.Y, _Size, _Color);
			}

			if (first)
			{
				AppendTo(disp, ref first, _X1, _Y1, 1, Color.Transparent);
			}
		}

		public virtual void AppendTo(IGumpWriter disp, ref bool first, int x, int y, int s, Color color)
		{
			disp.AppendLayout(first ? _Layout1A : _Layout1B);
			disp.AppendLayout(x - (s / 2));
			disp.AppendLayout(y - (s / 2));
			disp.AppendLayout(s);
			disp.AppendLayout(s);
			disp.AppendLayout(Parent.Intern(" ".WrapUOHtmlBG(color)));
			disp.AppendLayout(false);
			disp.AppendLayout(false);

			first = false;
		}
	}
}