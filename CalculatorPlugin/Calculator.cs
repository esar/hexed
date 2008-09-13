using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.CodeDom.Compiler;
using System.Reflection;
using Microsoft.JScript;

namespace CalculatorPlugin
{
	class CalculatorPanel : RichTextBox
	{
		public class Evaluator
		{
			public static int EvalToInteger(string statement, HexView.SelectionRange selection, IPluginHost host)
			{
				string s = EvalToString(statement, selection, host);
				return int.Parse(s.ToString());
			}
	
			public static double EvalToDouble(string statement, HexView.SelectionRange selection, IPluginHost host)
			{
				string s = EvalToString(statement, selection, host);
				return double.Parse(s);
			}
	
			public static string EvalToString(string statement, HexView.SelectionRange selection, IPluginHost host)
			{
				object o = EvalToObject(statement, selection, host);
				return o.ToString();
			}
	
			public static object EvalToObject(string statement, HexView.SelectionRange selection, IPluginHost host)
			{
				object selInt = null;
				object selFloat = null;
				object selAscii = null;
				object selUnicode = null;
				object selUtf8 = null;
	
				try { selInt = selection.AsInteger(); } catch(Exception) {}
				try { selFloat = selection.AsFloat(); } catch(Exception) {}
				try { selAscii = selection.AsAscii(); } catch(Exception) {}
				try { selUnicode = selection.AsUnicode(); } catch(Exception) {}
				try { selUtf8 = selection.AsUTF8(); } catch(Exception) {}
	
				_evaluatorType.InvokeMember(	"SetSelection",
												BindingFlags.InvokeMethod, 
												null, 
												_evaluator, 
												new object[] {	selInt, 
																selFloat, 
																selAscii, 
																selUnicode, 
																selUtf8 } );
	
				_evaluatorType.InvokeMember(	"SetResult",
												BindingFlags.InvokeMethod, 
												null, 
												_evaluator, 
												new object[] {	lastResult } );
	
				_evaluatorType.InvokeMember(	"SetHost",
				                            	BindingFlags.InvokeMethod,
				                            	null,
				                            	_evaluator,
				                            	new object[] { host } );
				
				lastResult = _evaluatorType.InvokeMember(	"Eval", 
													BindingFlags.InvokeMethod, 
													null, 
													_evaluator, 
													new object[] {	statement } );
	
				return lastResult;
			}
	               
			static Evaluator()
			{
				ICodeCompiler compiler;
				compiler = new JScriptCodeProvider().CreateCompiler();
	
				CompilerParameters parameters;
				parameters = new CompilerParameters();
				parameters.GenerateInMemory = true;
				parameters.ReferencedAssemblies.Add("System.Windows.Forms.dll");
				
	         
				CompilerResults results;
				
				results = compiler.CompileAssemblyFromSource(parameters, _jscriptSource);
	
				Assembly assembly = results.CompiledAssembly;
				_evaluatorType = assembly.GetType("Evaluator.Evaluator");
	         
				_evaluator = Activator.CreateInstance(_evaluatorType);
	
			}
	      
			private static object _evaluator = null;
			private static Type _evaluatorType = null;
			private static object lastResult = null;
	
			private static readonly string _jscriptSource = 
			@"package Evaluator
			{	
				class Evaluator
				{
	
					class Selection
					{
						public var int;
						public var float;
						public var ascii;
						public var unicode;
						public var utf8;
					}
	
					public function SetResult(r : Object)
					{
						result = r;
					}
	
					public function SetSelection(selInt : Object, selFloat : Object, selAscii : Object, selUnicode : Object, selUtf8 : Object)
					{
						sel.ascii = selAscii;
						sel.unicode = selUnicode;
						sel.utf8 = selUtf8;
						sel.int = selInt;
						sel.float = selFloat;
					}
	
					public function SetHost(host : Object)
					{
						Structure = host;
	                }
	
					// Duplicate Math class for users covenience
					public var E		= Math.E;
					public var LN10		= Math.LN10;
					public var LN2		= Math.LN2;
					public var LOG10E	= Math.LOG10E;
					public var LOG2E	= Math.LOG2E;
					public var PI		= Math.PI;
					public var SQRT1_2	= Math.SQRT1_2;
					public var SQRT2	= Math.SQRT2;
	
					public function abs(x)		{ return Math.abs(x); }
					public function acos(x)		{ return Math.acos(x); }
					public function asin(x)		{ return Math.asin(x); }
					public function atan(x)		{ return Math.atan(x); }
					public function atan2(y,x)	{ return Math.atan2(y,x); }
					public function ceil(x)		{ return Math.ceil(x); }
					public function cos(x)		{ return Math.cos(x); }
					public function exp(x)		{ return Math.exp(x); }
					public function floor(x)	{ return Math.floor(x); }
					public function log(x)		{ return Math.log(x); }
					public function pow(x,y)	{ return Math.pow(x,y); }
					public function random()	{ return Math.random(); }
					public function round(x)	{ return Math.round(x); }
					public function sin(x)		{ return Math.sin(x); }
					public function sqrt(x)		{ return Math.sqrt(x); }
					public function tan(x)		{ return Math.tan(x); }
	
	
	
					public function hex(x)		{ return '0x' + Number(x).toString(16); }
					public function oct(x)		{ return '0' + Number(x).toString(8); }
					public function bin(x)		{ return 'b' + Number(x).toString(2); }
					public function bswap(x)	
					{
						var v = Number(x);
						return	((v & 0xFF00000000000000) >> 56)	|
								((v & 0x00FF000000000000) >> 40)	|
								((v & 0x0000FF0000000000) >> 24)	|
								((v & 0x000000FF00000000) >> 8)		|
								((v & 0x00000000FF000000) << 8)		|
								((v & 0x0000000000FF0000) << 24)	|
								((v & 0x000000000000FF00) << 40)	|
								((v & 0x00000000000000FF) << 56);
					}
	
					public var sel = new Selection();
					public var result = undefined;
					public var mem = new Object();
					public var Structure = undefined;
	
	
					public function memClear()
					{
						for(var m in mem)
							delete mem[m];
					}
	
					public function memList()
					{
						var s = '';
						for(var m in mem)
							s += m + ': ' + mem[m] + '\r\n';
						return s;
					}
	
					public function Evaluator()
					{
					}
	
					public function Moo3(x)
					{
						return x + 1;
					}
	
					public function Eval(expr : String) : Object 
					{ 
						return eval(expr); 
					}
				}
			}";
		}
	
	
		private IPluginHost	Host;
		private string		LastExpression;
	
		public CalculatorPanel(IPluginHost host)
		{
			Host = host;
			Multiline = true;
			Font = new Font("Courier New", 10);
		}
	
		protected override void OnKeyUp(KeyEventArgs e)
		{
				base.OnKeyUp(e);
			if(e.KeyCode == Keys.Enter)
			{
				try
				{
					HexView.SelectionRange selection = null;
	
					if(Host.ActiveView != null)
						selection = Host.ActiveView.Selection;
	
					SelectionFont = new Font("Courier New", 10, FontStyle.Bold);
					LastExpression = Lines[Lines.Length - 2];
					SelectedText = Evaluator.EvalToString(LastExpression, selection, Host) + "\r\n";
				}
				catch(TargetInvocationException exp)
				{
					if(exp.InnerException != null)
					{
						SelectionColor = Color.Red;
						SelectionAlignment = HorizontalAlignment.Left;
						SelectionFont = Font;
						SelectedText = exp.InnerException.Message + "\r\n";
					}
				}
				catch(Exception exp)
				{
					AppendText(exp.Message + "\r\n");
				}
				SelectionStart = Text.Length;
			}
			if(e.KeyCode != Keys.Left && e.KeyCode != Keys.Right)
			{
				Select(Text.Length + 1, 0);
			}
		}
	
		protected override void OnKeyDown(KeyEventArgs e)
		{
				base.OnKeyDown(e);
			if(e.KeyCode != Keys.Left && e.KeyCode != Keys.Right)
			{
				Select(Text.Length + 1, 0);
			}
	
			if(e.KeyCode == Keys.Up)
				SelectedText = LastExpression;
		}
	}

	
	
	public class MyClass : IPlugin
	{
		string IPlugin.Name { get { return "Javascript Calculator"; } }
		string IPlugin.Author { get { return "Stephen Robinson"; } }
		string IPlugin.Version { get { return "1.0"; } }

		void IPlugin.Initialize(IPluginHost host)
		{
			host.AddWindow(new CalculatorPanel(host), "JScript Calculator");
		}
		
		void IPlugin.Dispose()
		{
		}
	}
}
