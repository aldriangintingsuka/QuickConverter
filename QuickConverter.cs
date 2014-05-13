﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Windows.Data;
using System.Windows.Markup;

namespace QuickConverter
{
	public class QuickConverter : MarkupExtension
	{
		private static Dictionary<string, Tuple<Func<object, object[], object>, DataContainer[]>> toFunctions = new Dictionary<string, Tuple<Func<object, object[], object>, DataContainer[]>>();
		private static Dictionary<string, Tuple<Func<object, object[], object>, DataContainer[]>> fromFunctions = new Dictionary<string, Tuple<Func<object, object[], object>, DataContainer[]>>();

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

		public QuickConverter()
		{
		}

		public QuickConverter(string convert)
		{
			Convert = convert;
		}

		private Tuple<Delegate, ParameterExpression[], DataContainer[]> GetLambda(string expression, bool convertBack)
		{
			List<ParameterExpression> parameters;
			List<DataContainer> dataContainers;
			Expression exp = EquationTokenizer.Tokenize(expression).GetExpression(out parameters, out dataContainers, DynamicContext);
			ParameterExpression invalid;
			if (convertBack)
				invalid = parameters.FirstOrDefault(par => !((par.Name[0] == 'V' && par.Name.Length == 2 && Char.IsDigit(par.Name[1])) || (par.Name == "value")));
			else
				invalid = parameters.FirstOrDefault(par => !((par.Name[0] == 'P' && par.Name.Length == 1) || (par.Name[0] == 'V' && par.Name.Length == 2 && Char.IsDigit(par.Name[1]))));
			if (invalid != null)
				throw new Exception("\"$" + invalid.Name + "\" is not a valid parameter name for conversion " + (convertBack ? "to" : "from") + " source.");
			Delegate del = Expression.Lambda(Expression.Convert(exp, typeof(object)), parameters.ToArray()).Compile();
			return new Tuple<Delegate, ParameterExpression[], DataContainer[]>(del, parameters.ToArray(), dataContainers.ToArray());
		}

		private Expression GetFinishedLambda(Tuple<Delegate, ParameterExpression[], DataContainer[]> lambda, out ParameterExpression inputP, out ParameterExpression inputV, out ParameterExpression value)
		{
			ParameterExpression val = Expression.Parameter(typeof(object));
			ParameterExpression inP = Expression.Parameter(typeof(object));
			ParameterExpression inV = Expression.Parameter(typeof(object[]));
			var arguments = lambda.Item2.Select<ParameterExpression, Expression>(par =>
			{
				if (par.Name[0] == 'P')
					return inP;
				else if (par.Name[0] == 'V')
					return Expression.ArrayIndex(inV, Expression.Constant((int)(par.Name[1] - '0')));
				return val;
			});
			inputP = inP;
			inputV = inV;
			value = val;

			return Expression.Call(Expression.Constant(lambda.Item1, lambda.Item1.GetType()), lambda.Item1.GetType().GetMethod("Invoke"), arguments);
		}

		internal static Type GetType(object pType)
		{
			if (pType == null)
				return null;
			if (pType is Type)
				return pType as Type;
			if (pType is string)
			{
				string type = "typeof(" + pType + ")";
				Tokens.TokenBase token;
				if (!new Tokens.TypeofToken().TryGetToken(ref type, out token))
					throw new Exception("\"" + pType + "\" is not a valid type.");
				return (token as Tokens.TypeofToken).Type;
			}
			throw new Exception("PType must be either string or a Type.");
		}

		public IValueConverter Get()
		{
			return ProvideValue(null) as IValueConverter;
		}

		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			ParameterExpression inputP, inputV, value;
			Tuple<Func<object, object[], object>, DataContainer[]> toFunc = null;
			Tuple<Func<object, object[], object>, DataContainer[]> fromFunc = null;
			if (Convert != null && !toFunctions.TryGetValue(Convert, out toFunc))
			{
				Tuple<Delegate, ParameterExpression[], DataContainer[]> tuple = GetLambda(Convert, false);
				if (tuple == null)
					return null;

				Expression exp = GetFinishedLambda(tuple, out inputP, out inputV, out value);
				var result = Expression.Lambda<Func<object, object[], object>>(exp, inputP, inputV).Compile();
				toFunc = new Tuple<Func<object, object[], object>, DataContainer[]>(result, tuple.Item3);

				toFunctions.Add(Convert, toFunc);
			}
			if (ConvertBack != null && !fromFunctions.TryGetValue(ConvertBack, out fromFunc))
			{
				Tuple<Delegate, ParameterExpression[], DataContainer[]> tuple = GetLambda(ConvertBack, true);
				if (tuple == null)
					return null;

				Expression exp = GetFinishedLambda(tuple, out inputP, out inputV, out value);
				var result = Expression.Lambda<Func<object, object[], object>>(exp, value, inputV).Compile();
				fromFunc = new Tuple<Func<object, object[], object>, DataContainer[]>(result, tuple.Item3);

				fromFunctions.Add(ConvertBack, fromFunc);
			}

			List<object> vals = new List<object>();
			for (int i = 0; i <= 9; ++i)
				vals.Add(typeof(QuickConverter).GetProperty("V" + i).GetValue(this, null));

			return new DynamicSingleConverter(toFunc != null ? toFunc.Item1 : null, fromFunc != null ? fromFunc.Item1 : null, vals.ToArray(), Convert, ConvertBack, GetType(PType), toFunc != null ? toFunc.Item2 : null, fromFunc != null ? fromFunc.Item2 : null);
		}
	}
}
