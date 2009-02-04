using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;


public class CommandEventArgs : EventArgs
{
	public object Arg = 0;

	public CommandEventArgs(object arg)
	{
		Arg = arg;
	}
}

public delegate void CommandEventHandler(object sender, CommandEventArgs e);

public class Command
{
	protected string _Name;
	protected Image _Image;
	protected string _Label;
	protected Keys[] _Shortcuts;
	protected string _Description;
	protected int _Enabled = -1;
	protected int _Checked = -1;
	
	public EventHandler StateChanged;
	public CommandEventHandler Invoked;
	
	public string Name
	{
		get { return _Name; }
	}
	
	public Image Image { get { return _Image; } }
	public string Label { get { return _Label; } }
	public Keys[] Shortcuts { get { return _Shortcuts; } }
	public string Description { get { return _Description; } }
	
	public bool Enabled
	{
		get { return (_Enabled != 0); }
		set 
		{ 
			if(value != Enabled || _Enabled == -1)
			{
				_Enabled = value ? 1 : 0;
				if(StateChanged != null) 
					StateChanged(this, new EventArgs());
			}
		}
	}
	
	public bool Checked
	{
		get { return (_Checked > 0); }
		set 
		{ 
			if(value != Checked || _Enabled == -1)
			{
				_Checked = value ? 1 : 0;
				if(StateChanged != null) 
					StateChanged(this, new EventArgs()); 
			}
		}
	}
	
	public Command(string name)
	{
		_Name = name;
	}
	
	public Command(string name, string description, string label, Image image, Keys[] shortcuts)
	{
		_Name = name;
		_Description = description;
		_Label = label;
		_Image = image;
		_Shortcuts = shortcuts;
	}
	
	public void Invoke(object arg)
	{
		if(Invoked != null)
			Invoked(this, new CommandEventArgs(arg));
	}
}


public class CommandSet : IEnumerable< KeyValuePair<string, Command> >
{
	private Dictionary<string, Command> Commands = new Dictionary<string, Command>();
	private Dictionary<string, Command> MergedCommands;
	
	public Command this[string name]
	{
		get
		{
			Command c = null;
			if(MergedCommands != null)
				MergedCommands.TryGetValue(name, out c);
			else
				Commands.TryGetValue(name, out c);
			return c;
		}
	}
	
	public Command Add(string name, string description, string label, Image image, Keys[] shortcut)
	{
		Command c;
		Commands.TryGetValue(name, out c);
		if(c == null)
		{
			c = new Command(name, description, label, image, shortcut);
			Commands.Add(name, c);
		}
		return c;
	}
	
	public Command Add(string name, CommandEventHandler invokeHandler)
	{
		return Add(name, null, null, null, null, invokeHandler);
	}
		
	public Command Add(string name, string description, string label, Image image, Keys[] shortcut, CommandEventHandler invokeHandler)
	{
		Command c = Add(name, description, label, image, shortcut);
		c.Invoked += invokeHandler;
		return c;
	}

	public Command Add(string name, CommandEventHandler invokeHandler, EventHandler stateChangeHandler)
	{
		return Add(name, null, null, null, null, invokeHandler, stateChangeHandler);
	}
	
	public Command Add(string name, string description, string label, Image image, Keys[] shortcut, CommandEventHandler invokeHandler, EventHandler stateChangeHandler)
	{
		Command c = Add(name, description, label, image, shortcut);
		if(invokeHandler != null)
			c.Invoked += invokeHandler;
		if(stateChangeHandler != null)
			c.StateChanged += stateChangeHandler;
		return c;
	}
	
	public Command Find(string name)
	{
		Command cmd;
		if(Commands.TryGetValue(name, out cmd))
			return cmd;
		else
			return null;
	}
	
	public Command FindShortcut(Keys keys)
	{
		foreach(KeyValuePair<string, Command> kvp in Commands)
		{
			if(kvp.Value.Shortcuts != null)
			{
				foreach(Keys k in kvp.Value.Shortcuts)
					if(k == keys)
						return kvp.Value;
			}
		}
		
		return null;
	}
	
	public IEnumerator< KeyValuePair<string, Command> > GetEnumerator()
	{
		return Commands.GetEnumerator();
	}
	
	IEnumerator IEnumerable.GetEnumerator()
	{
		return Commands.GetEnumerator();
	}
	
	public void Merge(CommandSet setToMerge)
	{
		MergedCommands = new Dictionary<string, Command>();
		foreach(KeyValuePair<string, Command> kvp in Commands)
		{
			Command command = kvp.Value;
			Command newCommand = new Command(kvp.Key);
		
			if(command.StateChanged != null)
				foreach(Delegate d in command.StateChanged.GetInvocationList())
					newCommand.StateChanged += (EventHandler)d;
			if(command.Invoked != null)
				foreach(Delegate d in command.Invoked.GetInvocationList())
					newCommand.Invoked += (CommandEventHandler)d;
			
			MergedCommands.Add(kvp.Key, newCommand);
		}
		
		foreach(KeyValuePair<string, Command> kvp in setToMerge.Commands)
		{
			Command command = kvp.Value;
			Command oldCommand = null;
			Command newCommand;
			
			MergedCommands.TryGetValue(kvp.Key, out oldCommand);
			if(oldCommand != null)
				newCommand = oldCommand;
			else
				newCommand = new Command(kvp.Key);

			if(command.Invoked != null)
				foreach(Delegate d in command.Invoked.GetInvocationList())
					newCommand.Invoked += (CommandEventHandler)d;
			if(command.StateChanged != null)
				foreach(Delegate d in command.StateChanged.GetInvocationList())
					newCommand.StateChanged += (EventHandler)d;
			
			if(oldCommand == null)
				MergedCommands.Add(kvp.Key, newCommand);
		}
	}
	
	public void RevertMerge()
	{
		foreach(KeyValuePair<string, Command> kvp in MergedCommands)
		{
			Command cmd = null;
			Commands.TryGetValue(kvp.Key, out cmd);
			if(cmd != null)
			{
				cmd.Enabled = kvp.Value.Enabled;
				cmd.Checked = kvp.Value.Checked;
			}
		}
		MergedCommands = null;
	}
}
