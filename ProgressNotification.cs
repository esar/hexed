using System;
using System.Collections;
using System.Collections.Generic;


public class ProgressNotification
{
	public event EventHandler Changed;
	
	protected int _Index;
	public int Index
	{
		get { return _Index; }
		set { _Index = value; }
	}
	
	protected int _PercentComplete;
	public int PercentComplete
	{
		get { return _PercentComplete; }
		set { _PercentComplete = value; OnChanged(EventArgs.Empty); }
	}
	
	protected string _Message;
	public string Message
	{
		get { return _Message; }
		set { _Message = value; OnChanged(EventArgs.Empty); }
	}
	
	public ProgressNotification()
	{
	}
	
	public void Update(int percentComplete, string message)
	{
		_PercentComplete = percentComplete;
		_Message = message;
		OnChanged(EventArgs.Empty);
	}
	
	protected void OnChanged(EventArgs e)
	{
		if(Changed != null)
			Changed(this, e);
	}
}

public class ProgressNotificationEventArgs : EventArgs
{
	protected ProgressNotification _Notification;
	public ProgressNotification Notification
	{
		get { return _Notification; }
	}
	
	public ProgressNotificationEventArgs(ProgressNotification notification)
	{
		_Notification = notification;
	}
}

public delegate void ProgressNotificationEventHandler(object sender, ProgressNotificationEventArgs e);

public class ProgressNotificationCollection : ICollection<ProgressNotification>
{
	protected List<ProgressNotification> Notifications = new List<ProgressNotification>();

	public event ProgressNotificationEventHandler NotificationAdded;
	public event ProgressNotificationEventHandler NotificationRemoved;
	
	public virtual ProgressNotification this[int index]
	{
		get { return Notifications[index]; }
		set 
		{
			if(value == Notifications[index])
				return;
			if(Notifications[index] != null)
				OnNotificationRemoved(new ProgressNotificationEventArgs(Notifications[index]));
			Notifications[index] = value; 
			if(value != null)
				OnNotificationAdded(new ProgressNotificationEventArgs(value));
		}
	}

	public ProgressNotificationCollection()
	{
	}
	
	public virtual int Count
	{
		get { return Notifications.Count; }
	}

	public virtual bool IsReadOnly
	{
		get { return false; }
	}

	public virtual void Add(ProgressNotification n)
	{
		Notifications.Add(n);
		n.Index = Notifications.Count - 1;
		OnNotificationAdded(new ProgressNotificationEventArgs(n));
	}

	public virtual bool Remove(ProgressNotification n) 
	{
		bool result = Notifications.Remove(n);
		if(result)
			OnNotificationRemoved(new ProgressNotificationEventArgs(n));
		return result;
	}

	public bool Contains(ProgressNotification n)
	{
		return Notifications.Contains(n);
	}
 
	public virtual void CopyTo(ProgressNotification[] dest, int index)
	{
		Notifications.CopyTo(dest, index);
	}

	public virtual void Clear()
	{
		Notifications.Clear();
	}

	public virtual IEnumerator<ProgressNotification> GetEnumerator()
	{
		return Notifications.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return Notifications.GetEnumerator();
	}
	
	protected void OnNotificationAdded(ProgressNotificationEventArgs e)
	{
		if(NotificationAdded != null)
			NotificationAdded(this, e);
	}
	
	protected void OnNotificationRemoved(ProgressNotificationEventArgs e)
	{
		if(NotificationRemoved != null)
			NotificationRemoved(this, e);
	}
}
