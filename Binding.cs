﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;
using System.Xaml;

namespace QuickConverter
{
	/// <summary>
	/// This type can be substituted for System.Windows.Data.Binding. Both Convert and ConvertBack to be specified inline which makes two way binding possible.
	/// </summary>
	public class Binding : MarkupExtension
	{
		private static Dictionary<string, Tuple<Func<object, object[], object>, string[], DataContainer[]>> toFunctions = new Dictionary<string, Tuple<Func<object, object[], object>, string[], DataContainer[]>>();
		private static Dictionary<string, Tuple<Func<object, object[], object>, string[], DataContainer[]>> fromFunctions = new Dictionary<string, Tuple<Func<object, object[], object>, string[], DataContainer[]>>();

		/// <summary>Creates a bound parameter. This can be accessed inside the converter as $P.</summary>
		public System.Windows.Data.Binding P { get; set; }

		/// <summary>Creates a constant parameter. This can be accessed inside the converter as $V0.</summary>
		public object V0 { get; set; }
		/// <summary>Creates a constant parameter. This can be accessed inside the converter as $V1.</summary>
		public object V1 { get; set; }
		/// <summary>Creates a constant parameter. This can be accessed inside the converter as $V2.</summary>
		public object V2 { get; set; }
		/// <summary>Creates a constant parameter. This can be accessed inside the converter as $V3.</summary>
		public object V3 { get; set; }
		/// <summary>Creates a constant parameter. This can be accessed inside the converter as $V4.</summary>
		public object V4 { get; set; }
		/// <summary>Creates a constant parameter. This can be accessed inside the converter as $V5.</summary>
		public object V5 { get; set; }
		/// <summary>Creates a constant parameter. This can be accessed inside the converter as $V6.</summary>
		public object V6 { get; set; }
		/// <summary>Creates a constant parameter. This can be accessed inside the converter as $V7.</summary>
		public object V7 { get; set; }
		/// <summary>Creates a constant parameter. This can be accessed inside the converter as $V8.</summary>
		public object V8 { get; set; }
		/// <summary>Creates a constant parameter. This can be accessed inside the converter as $V9.</summary>
		public object V9 { get; set; }

		/// <summary>
		/// The converter will return DependencyObject.Unset during conversion if P is not of this type.
		/// Both QuickConverter syntax (as a string) and Type objects are valid. 
		/// </summary>
		public object PType { get; set; }

		/// <summary>
		/// The expression to use for converting data from the source.
		/// </summary>
		public string Convert { get; set; }
		/// <summary>
		/// The expression to use for converting data from the target back to the source.
		/// The target value is accessible as $value.
		/// The bound parameter $P cannot be accessed when converting back.
		/// </summary>
		public string ConvertBack { get; set; }

		/// <summary>
		/// This specifies the context to use for dynamic call sites.
		/// </summary>
		public Type DynamicContext { get; set; }

		public Binding()
		{
		}

		public Binding(string convert)
		{
			Convert = convert;
		}

		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (P == null)
				return null;

			bool getExpression;
			if (serviceProvider == null)
				getExpression = false;
			else
			{
				var targetProvider = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
				if (targetProvider == null || !(targetProvider.TargetProperty is PropertyInfo))
					getExpression = true;
				else
				{
					Type propType = (targetProvider.TargetProperty as PropertyInfo).PropertyType;
					if (propType == typeof(Binding))
						return this;
					getExpression = !propType.IsAssignableFrom(typeof(System.Windows.Data.MultiBinding));
				}
			}

			ParameterExpression inputP, inputV, value;
			List<string> values;
			Tuple<Func<object, object[], object>, string[], DataContainer[]> toFunc = null;
			Tuple<Func<object, object[], object>, string[], DataContainer[]> fromFunc = null;
			if (Convert != null && !toFunctions.TryGetValue(Convert, out toFunc))
			{
				Tuple<Delegate, ParameterExpression[], DataContainer[]> tuple = QuickConverter.GetLambda(Convert, false, DynamicContext);
				if (tuple == null)
					return null;

				Expression exp = QuickConverter.GetFinishedLambda(tuple, out inputP, out inputV, out value, out values);
				var result = Expression.Lambda<Func<object, object[], object>>(exp, inputP, inputV).Compile();
				toFunc = new Tuple<Func<object, object[], object>, string[], DataContainer[]>(result, values.ToArray(), tuple.Item3);

				toFunctions.Add(Convert, toFunc);
			}
			if (ConvertBack != null && !fromFunctions.TryGetValue(ConvertBack, out fromFunc))
			{
				Tuple<Delegate, ParameterExpression[], DataContainer[]> tuple = QuickConverter.GetLambda(ConvertBack, true, DynamicContext);
				if (tuple == null)
					return null;

				Expression exp = QuickConverter.GetFinishedLambda(tuple, out inputP, out inputV, out value, out values);
				var result = Expression.Lambda<Func<object, object[], object>>(exp, value, inputV).Compile();
				fromFunc = new Tuple<Func<object, object[], object>, string[], DataContainer[]>(result, values.ToArray(), tuple.Item3);

				fromFunctions.Add(ConvertBack, fromFunc);
			}

			object[] toVals = null;
			if (toFunc != null)
				toVals = toFunc.Item2.Select(str => typeof(Binding).GetProperty(str).GetValue(this, null)).ToArray();

			object[] fromVals = null;
			if (fromFunc != null)
				fromVals = fromFunc.Item2.Select(str => typeof(Binding).GetProperty(str).GetValue(this, null)).ToArray();

			P.Converter = new DynamicSingleConverter(toFunc != null ? toFunc.Item1 : null, fromFunc != null ? fromFunc.Item1 : null, toVals, fromVals, Convert, ConvertBack, QuickConverter.GetType(PType), toFunc != null ? toFunc.Item3 : null, fromFunc != null ? fromFunc.Item3 : null);

			return getExpression ? P.ProvideValue(serviceProvider) : P;
		}
	}
}
