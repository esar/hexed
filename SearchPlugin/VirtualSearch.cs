using System;
using System.Collections.Generic;
using System.ComponentModel;


namespace SearchPlugin
{
	class SearchResult
	{
		public long Position;
		public int Length;
		
		public SearchResult(long position, int length)
		{
			Position = position;
			Length = length;
		}
	}
	
	struct RestartPosition : IComparable
	{
		public long  Index;
		public long Position;
		
		public RestartPosition(long index, long position)
		{
			Index = index;
			Position = position;
		}
		
		public int CompareTo(object o)
		{
			return Index.CompareTo(((RestartPosition)o).Index);
		}
	}
	
	class WorkerArgs
	{
		public Document	Document;
		public byte[]	Pattern;
		public long		StartDataOffset;
		public long     StartIndex;
		public int		MaxResults;
		
		public WorkerArgs(Document document, byte[] pattern, long startDataOffset, long startIndex, int maxResults)
		{
			Document = document;
			Pattern = pattern;
			StartDataOffset = startDataOffset;
			StartIndex = startIndex;
			MaxResults = maxResults;
		}
	}
	
	class WorkerProgress
	{
		public long StartOffset;
		public long EndOffset;
		public long StartIndex;
		public SearchResult[] NewResults;
		public int NumNewResults;
		public float MBps;
		public WorkerArgs Args;
		public bool Cancelled;
		
		public WorkerProgress(long startOffset, long endOffset, long startIndex, SearchResult[] newResults, int numNewResults, float mbps, WorkerArgs args, bool cancelled)
		{
			StartOffset = startOffset;
			EndOffset = endOffset;
			StartIndex = startIndex;
			NewResults = newResults;
			NumNewResults = numNewResults;
			MBps = mbps;
			Args = args;
			Cancelled = cancelled;
		}
	}

	class VirtualSearchProgressEventArgs : EventArgs
	{
		public int PercentComplete;
		public float MBps;
		
		public VirtualSearchProgressEventArgs(int percent, float mbps)
		{
			PercentComplete = percent;
			MBps = mbps;
		}
	}
	
	class VirtualSearch
	{
		private const int RESTART_GAP = 1024 * 1024;
		private const int CACHE_SIZE = 10000;
		private const int BATCH_SIZE = 10000;
		
		private Document  Document;
		private byte[]    Pattern;
		
		List<RestartPosition> RestartPositions = new List<RestartPosition>();
		SearchResult[]   CachedResults;
		long             FirstCachedResult;
		BackgroundWorker Worker;

		long WorkerIndex;
		int WorkerCount;
		long NextWorkerIndex;
		int NextWorkerCount;
		long ContinueFullScanIndex;
		long ContinueFullScanDataOffset;
		
		
		public event EventHandler ResultCountChanged;
		public delegate void VirtualSearchProgressEventHandler(object sender, VirtualSearchProgressEventArgs e); 
		public event VirtualSearchProgressEventHandler ProgressChanged;
		
		private long     _ResultCount;
		public long      ResultCount
		{
			get { return _ResultCount; }
		}
		
		public bool IsBusy
		{
			get { return Worker.IsBusy; }
		}
		
		public SearchResult this[long index]
		{
			get 
			{
				if(index >= FirstCachedResult && index < FirstCachedResult + CACHE_SIZE)
					return CachedResults[index - FirstCachedResult];
				
				PreCache(index);
				return null;
			}
		}
		
		public VirtualSearch()
		{
			CachedResults = new SearchResult[CACHE_SIZE];
		}
		
		public void Initialize(Document document, byte[] pattern)
		{
			Document = document;
			Pattern = pattern;

			_ResultCount = 0;
			FirstCachedResult = 0;
			Array.Clear(CachedResults, 0, CACHE_SIZE);
			RestartPositions.Clear();
		
			NextWorkerIndex = -1;
			NextWorkerCount = 0;
			ContinueFullScanIndex = -1;
			ContinueFullScanDataOffset = -1;
			
			RunWorker(0, 0, -1);
		}
		
		public void PreCache(long index)
		{
			if(WorkerCount <= 0 || index < WorkerIndex || index >= WorkerIndex + WorkerCount)
			{
				index -= CACHE_SIZE / 2;
				if(index < 0)
					index = 0;
				
				NextWorkerIndex = index;
				NextWorkerCount = CACHE_SIZE;
				if(Worker.IsBusy && !Worker.CancellationPending)
				{
					//Console.WriteLine(String.Format("PreCache: {0}, Worker: {1} -> {2}", index, WorkerIndex, WorkerIndex + WorkerCount)); 
					Worker.CancelAsync();
				}
				else if(!Worker.IsBusy)
				{
					int i = RestartPositions.BinarySearch(new RestartPosition(index, 0));
					if(i < 0)
					{
						i = ~i;
						if(i > 0)
							--i;
					}
					
					RunWorker(RestartPositions[i].Position, RestartPositions[i].Index, (int)(CACHE_SIZE + (index - RestartPositions[i].Index)));
				}
				
				Array.Clear(CachedResults, 0, CACHE_SIZE);
				FirstCachedResult = index;
			}
		}
		
		protected void RunWorker(long offset, long index, int count)
		{
			if(Worker != null)
			{
				Worker.DoWork -= OnWorkerDoWork;
				Worker.ProgressChanged -= OnWorkerProgressChanged;
				Worker.RunWorkerCompleted -= OnWorkerComplete;
				Worker.Dispose();
			}
			
			Worker = new BackgroundWorker();
			Worker.WorkerReportsProgress = true;
			Worker.WorkerSupportsCancellation = true;
			Worker.DoWork += OnWorkerDoWork;
			Worker.ProgressChanged += OnWorkerProgressChanged;
			Worker.RunWorkerCompleted += OnWorkerComplete;
			
			WorkerIndex = index;
			WorkerCount = count;
			Worker.RunWorkerAsync(new WorkerArgs(Document, Pattern, offset, index, count));
			if(ProgressChanged != null)
				ProgressChanged(this, new VirtualSearchProgressEventArgs(0, 0));
			NextWorkerCount = 0;
		}
		
		protected void OnWorkerDoWork(object sender, DoWorkEventArgs e)
		{
			WorkerArgs args = (WorkerArgs)e.Argument;
			
			int numResults = 0;
			long totalResultsDelivered = 0;
			SearchResult[] results = new SearchResult[BATCH_SIZE]; 

			
			PatternMatchBMH matcher = new PatternMatchBMH();
			matcher.Initialize(args.Pattern, true);

			int lastReportTime = Environment.TickCount;
			long lastReportBytes = 0;
			long offset = args.StartDataOffset;
			byte[] data = new byte[1024*1024];
			while(offset < args.Document.Length)
			{
				long len = (args.Document.Length - offset) > (1024 * 1024) ? (1024 * 1024) : (args.Document.Length - offset);
				args.Document.GetBytes(offset, data, len);
				
				foreach(int i in matcher.SearchBlock(data, 0, (int)len))
				{
					results[numResults++] = new SearchResult(offset + i, args.Pattern.Length);
					if(numResults >= BATCH_SIZE || Environment.TickCount > lastReportTime + 250)
					{
						float MBps = (float)(offset - lastReportBytes) / (1024 * 1024);
						MBps /= (float)(System.Environment.TickCount - lastReportTime) / 1000;
						Worker.ReportProgress(50, new WorkerProgress(args.StartDataOffset, offset, args.StartIndex + totalResultsDelivered, results, numResults, MBps, args, false));
						totalResultsDelivered += numResults;
						results = new SearchResult[BATCH_SIZE];
						numResults = 0;
						lastReportBytes = offset;
						lastReportTime = Environment.TickCount;
					}
					
					if(args.MaxResults > 0 && totalResultsDelivered + numResults >= args.MaxResults)
						break;
					if(Worker.CancellationPending)
						break;
				}

				if(args.MaxResults > 0 && totalResultsDelivered + numResults >= args.MaxResults)
					break;
				if(Worker.CancellationPending)
					break;
					
				offset += len;
			}

			
			WorkerProgress finalProgress = new WorkerProgress(args.StartDataOffset, offset, args.StartIndex + totalResultsDelivered, results, numResults, 0, args, Worker.CancellationPending); 
			Worker.ReportProgress(100, finalProgress);
			e.Result = finalProgress;

			// Don't set the Cancel flag otherwise we can't access the result in OnWorkerComplete
			// if(Worker.CancellationPending)
			// 	e.Cancel = true;
		}
		
		protected void OnWorkerProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			WorkerProgress progress = (WorkerProgress)e.UserState;
			long oldResultCount = _ResultCount;
			
			for(int i = 0; i < progress.NumNewResults; ++i)
			{
				long index = progress.StartIndex + i;
				
				if(progress.Args.MaxResults == -1)
					_ResultCount++;
				
				if(index >= FirstCachedResult && index < FirstCachedResult + CACHE_SIZE)
					CachedResults[index - FirstCachedResult] = progress.NewResults[i];
				else if(RestartPositions.Count == 0 || progress.NewResults[i].Position >= RestartPositions[RestartPositions.Count - 1].Position + RESTART_GAP)
					RestartPositions.Add(new RestartPosition(index, progress.NewResults[i].Position));
			}

			if(_ResultCount != oldResultCount && ResultCountChanged != null)
				ResultCountChanged(this, EventArgs.Empty);
			
			if(ProgressChanged != null)
				ProgressChanged(this, new VirtualSearchProgressEventArgs((int)((100.0 / Document.Length) * progress.EndOffset), progress.MBps));
		}
		
		protected void OnWorkerComplete(object sender, RunWorkerCompletedEventArgs e)
		{
			WorkerProgress result = (WorkerProgress)e.Result;

			if(result.Args.MaxResults == -1)
			{
				if(result.Cancelled)
				{
					ContinueFullScanIndex = result.StartIndex + result.NumNewResults;
					ContinueFullScanDataOffset = result.EndOffset;
				}
				else
					ContinueFullScanIndex = -1;
			}
			
			if(NextWorkerCount > 0)
			{
				int i = RestartPositions.BinarySearch(new RestartPosition(NextWorkerIndex, 0));
				if(i < 0)
				{
					i = ~i;
					if(i > 0)
						--i;
				}
				
				//Console.WriteLine(String.Format("VS: OnComplete: NextIdx: {0}, NextCount: {1}, RestartPos.Idx: {2}, RestartPos.Pos: {3}", NextWorkerIndex, NextWorkerCount, RestartPositions[i].Index, RestartPositions[i].Position));
				RunWorker(RestartPositions[i].Position, RestartPositions[i].Index, (int)(NextWorkerCount + (NextWorkerIndex - RestartPositions[i].Index)));
			}
			else if(ContinueFullScanIndex >= 0)
			{
				//Console.WriteLine(String.Format("VS: OnComplete: Continuing full scan, offset: {0}, index: {1}", ContinueFullScanDataOffset, ContinueFullScanIndex));
				RunWorker(ContinueFullScanDataOffset, ContinueFullScanIndex, -1);
			}
			
			if(ProgressChanged != null)
				ProgressChanged(this, new VirtualSearchProgressEventArgs(0, 0));
		}
	}
}
